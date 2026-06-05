using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SourceAFIS;
using IdentityCore.App.Data;
using IdentityCore.App.Models;

namespace IdentityCore.App.Services;

public class FingerprintRecognitionService
{
    private const int DefaultDpi = 500;

    private readonly FingerprintTemplateRepository _fingerprintTemplateRepository = new();
    private readonly PersonRepository _personRepository = new();
    private readonly SystemSettingsService _systemSettingsService = new();

    public FingerprintTemplateExtractionResult CreateTemplate(string imagePath, int dpi = DefaultDpi)
    {
        try
        {
            var resolvedPath = ResolveImagePath(imagePath);

            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return new FingerprintTemplateExtractionResult
                {
                    Success = false,
                    Message = "Nie znaleziono obrazu odcisku palca."
                };
            }

            var imageBytes = File.ReadAllBytes(resolvedPath);

            var image = new FingerprintImage(
                imageBytes,
                new FingerprintImageOptions
                {
                    Dpi = dpi
                });

            var template = new SourceAFIS.FingerprintTemplate(image);
            var templateBytes = template.ToByteArray();

            if (templateBytes.Length == 0)
            {
                return new FingerprintTemplateExtractionResult
                {
                    Success = false,
                    Message = "Nie udało się utworzyć szablonu cech odcisku palca."
                };
            }

            return new FingerprintTemplateExtractionResult
            {
                Success = true,
                Message = "Szablon cech odcisku palca został utworzony przez SourceAFIS.",
                TemplateData = templateBytes,
                SourceImagePath = imagePath,
                QualityScore = EstimateTemplateQuality(templateBytes),
                AlgorithmName = "SourceAFIS",
                Dpi = dpi
            };
        }
        catch (Exception ex)
        {
            return new FingerprintTemplateExtractionResult
            {
                Success = false,
                Message = $"Błąd tworzenia szablonu cech odcisku: {ex.Message}"
            };
        }
    }

    public FingerprintIdentificationResult Identify(string inputImagePath)
    {
        var settings = _systemSettingsService.GetBiometricDecisionSettings();
        var matchThreshold = settings.FingerprintMatchThreshold;
        var minimumMatchMargin = settings.FingerprintMinimumMargin;

        var probeExtraction = CreateTemplate(inputImagePath);

        if (!probeExtraction.Success)
        {
            return new FingerprintIdentificationResult
            {
                Success = false,
                IsMatch = false,
                Message = probeExtraction.Message,
                Decision = "Nie znaleziono dopasowania",
                InputImagePath = inputImagePath,
                MatchThreshold = matchThreshold,
                MinimumMatchMargin = minimumMatchMargin
            };
        }

        var storedTemplates = _fingerprintTemplateRepository.GetAll();

        if (storedTemplates.Count == 0)
        {
            return new FingerprintIdentificationResult
            {
                Success = false,
                IsMatch = false,
                Message = "W bazie nie ma zarejestrowanych szablonów odcisku palca.",
                Decision = "Nie znaleziono dopasowania",
                InputImagePath = inputImagePath,
                MatchThreshold = matchThreshold,
                MinimumMatchMargin = minimumMatchMargin
            };
        }

        var persons = _personRepository
            .GetAll()
            .ToDictionary(person => person.Id);

        var probeTemplate = new SourceAFIS.FingerprintTemplate(probeExtraction.TemplateData);
        var matcher = new FingerprintMatcher(probeTemplate);

        var rawCandidates = new List<FingerprintCandidateMatch>();

        foreach (var storedTemplate in storedTemplates)
        {
            if (!persons.TryGetValue(storedTemplate.PersonId, out var person))
            {
                continue;
            }

            if (storedTemplate.TemplateData.Length == 0)
            {
                continue;
            }

            var candidateTemplate = new SourceAFIS.FingerprintTemplate(storedTemplate.TemplateData);
            var score = matcher.Match(candidateTemplate);

            rawCandidates.Add(new FingerprintCandidateMatch
            {
                PersonId = person.Id,
                PersonFullName = person.FullName,
                PersonCode = person.PersonCode,
                Department = person.Department,
                SourceImagePath = storedTemplate.SourceImagePath,
                SimilarityScore = Math.Round(score, 2)
            });
        }

        if (rawCandidates.Count == 0)
        {
            return new FingerprintIdentificationResult
            {
                Success = false,
                IsMatch = false,
                Message = "Nie udało się odczytać poprawnych szablonów odcisku z bazy.",
                Decision = "Nie znaleziono dopasowania",
                InputImagePath = inputImagePath,
                MatchThreshold = matchThreshold,
                MinimumMatchMargin = minimumMatchMargin
            };
        }

        // Dla każdej osoby bierzemy jej najlepszą próbkę, a potem sortujemy osoby.
        var candidates = rawCandidates
            .GroupBy(candidate => candidate.PersonId)
            .Select(group => group.OrderByDescending(candidate => candidate.SimilarityScore).First())
            .OrderByDescending(candidate => candidate.SimilarityScore)
            .Take(3)
            .Select((candidate, index) =>
            {
                candidate.Rank = index + 1;
                return candidate;
            })
            .ToList();

        var best = candidates[0];
        var secondScore = candidates.Count > 1 ? candidates[1].SimilarityScore : 0;
        var margin = candidates.Count > 1
            ? Math.Round(best.SimilarityScore - secondScore, 2)
            : double.PositiveInfinity;

        var scoreAccepted = best.SimilarityScore >= matchThreshold;
        var marginAccepted = candidates.Count <= 1 || margin >= minimumMatchMargin;
        var isMatch = scoreAccepted && marginAccepted;

        if (!isMatch)
        {
            var reason = !scoreAccepted
                ? $"Najwyższy wynik odcisku ({best.SimilarityScore:0.00}) jest poniżej progu {matchThreshold:0.00}."
                : $"Najlepszy kandydat nie ma wystarczającej przewagi nad drugim wynikiem. Przewaga: {margin:0.00}, wymagane: {minimumMatchMargin:0.00}.";

            return new FingerprintIdentificationResult
            {
                Success = true,
                IsMatch = false,
                PersonId = null,
                PersonFullName = "Nie znaleziono osoby w bazie",
                PersonCode = "-",
                Department = "-",
                SimilarityScore = best.SimilarityScore,
                MatchThreshold = matchThreshold,
                MatchMargin = double.IsPositiveInfinity(margin) ? 0 : margin,
                MinimumMatchMargin = minimumMatchMargin,
                Decision = "Nie znaleziono dopasowania",
                Message = reason,
                InputImagePath = inputImagePath,
                Candidates = candidates
            };
        }

        return new FingerprintIdentificationResult
        {
            Success = true,
            IsMatch = true,
            PersonId = best.PersonId,
            PersonFullName = best.PersonFullName,
            PersonCode = best.PersonCode,
            Department = best.Department,
            SimilarityScore = best.SimilarityScore,
            MatchThreshold = matchThreshold,
            MatchMargin = double.IsPositiveInfinity(margin) ? 0 : margin,
            MinimumMatchMargin = minimumMatchMargin,
            Decision = "Znaleziono dopasowanie",
            Message = candidates.Count > 1
                ? $"Znaleziono dopasowanie odcisku. Score: {best.SimilarityScore:0.00}. Przewaga nad drugim wynikiem: {margin:0.00}."
                : $"Znaleziono dopasowanie odcisku. Score: {best.SimilarityScore:0.00}.",
            InputImagePath = inputImagePath,
            Candidates = candidates
        };
    }

    private static double EstimateTemplateQuality(byte[] templateBytes)
    {
        // SourceAFIS nie zwraca prostego procentu jakości skanu.
        // To jest tylko pomocniczy wskaźnik diagnostyczny na podstawie rozmiaru template'u.
        if (templateBytes.Length <= 0)
        {
            return 0;
        }

        return Math.Clamp(templateBytes.Length / 80.0, 0, 100);
    }

    private static string ResolveImagePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (Path.IsPathFullyQualified(path) && File.Exists(path))
        {
            return path;
        }

        var baseCandidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

        if (File.Exists(baseCandidate))
        {
            return baseCandidate;
        }

        var currentCandidate = Path.Combine(Directory.GetCurrentDirectory(), path);

        if (File.Exists(currentCandidate))
        {
            return currentCandidate;
        }

        return path;
    }
}
