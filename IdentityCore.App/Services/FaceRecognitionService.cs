using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenCvSharp;
using IdentityCore.App.Data;
using IdentityCore.App.Models;

namespace IdentityCore.App.Services;

public class FaceRecognitionService
{
    private readonly ImageProcessingService _imageProcessingService = new();
    private readonly FaceEmbeddingService _faceEmbeddingService = new();
    private readonly FaceTemplateRepository _faceTemplateRepository = new();
    private readonly PersonRepository _personRepository = new();
    private readonly SystemSettingsService _systemSettingsService = new();

    // Zachowane dla starszych wywołań z ViewModeli. Próg przekazany z zewnątrz jest ignorowany,
    // żeby źródłem prawdy były ustawienia systemowe zapisane w SystemSettings.
    public FaceIdentificationResult Identify(string inputImagePath, double matchThreshold)
    {
        return Identify(inputImagePath);
    }

    public FaceIdentificationResult Identify(string inputImagePath)
    {
        var settings = _systemSettingsService.GetBiometricDecisionSettings();
        var matchThreshold = settings.FaceMatchThreshold;
        var minimumMargin = settings.FaceMinimumMargin;

        using var originalFrame = Cv2.ImRead(inputImagePath, ImreadModes.Color);
        if (originalFrame.Empty())
        {
            return CreateFailedResult(
                "Nie można odczytać obrazu ze wskazanej ścieżki.",
                "Nie wykonano analizy",
                matchThreshold,
                minimumMargin);
        }

        using var preparedFace = _imageProcessingService.ExtractAndPrepareFace(originalFrame, out var qualityScore);
        if (preparedFace == null)
        {
            return CreateFailedResult(
                "Nie wykryto poprawnej, frontalnej twarzy na obrazie. Ustaw twarz prosto do kamery, pokaż oba oczy i wykonaj zdjęcie ponownie.",
                "Nie znaleziono dopasowania",
                matchThreshold,
                minimumMargin);
        }

        var inputEmbedding = _faceEmbeddingService.GenerateEmbeddingFromMat(preparedFace);

        if (inputEmbedding.Length == 0)
        {
            return CreateFailedResult(
                "Nie udało się wygenerować szablonu biometrycznego twarzy.",
                "Nie wykonano analizy",
                matchThreshold,
                minimumMargin);
        }

        var templates = _faceTemplateRepository.GetAll();

        if (templates.Count == 0)
        {
            return CreateFailedResult(
                "W bazie nie ma zarejestrowanych szablonów twarzy. Najpierw zarejestruj próbki twarzy w profilu osoby.",
                "Nie znaleziono dopasowania",
                matchThreshold,
                minimumMargin);
        }

        var persons = _personRepository
            .GetAll()
            .ToDictionary(person => person.Id);

        var rawScores = new List<RawFaceCandidateScore>();

        foreach (var template in templates)
        {
            if (!persons.TryGetValue(template.PersonId, out var person))
            {
                continue;
            }

            var templateEmbedding = DeserializeEmbedding(template.EmbeddingJson);

            if (templateEmbedding.Length == 0)
            {
                continue;
            }

            var similarity = CalculateSimilarityPercentage(inputEmbedding, templateEmbedding);

            rawScores.Add(new RawFaceCandidateScore
            {
                Person = person,
                Template = template,
                SimilarityScore = similarity
            });
        }

        if (rawScores.Count == 0)
        {
            return CreateFailedResult(
                "Nie udało się odczytać żadnego poprawnego szablonu twarzy z bazy.",
                "Nie znaleziono dopasowania",
                matchThreshold,
                minimumMargin);
        }

        var candidates = rawScores
            .GroupBy(score => score.Person.Id)
            .Select(group => BuildCandidateForPerson(group.ToList()))
            .OrderByDescending(candidate => candidate.SimilarityScore)
            .Take(3)
            .Select((candidate, index) =>
            {
                candidate.Rank = index + 1;
                return candidate;
            })
            .ToList();

        var best = candidates[0];
        var second = candidates.Count > 1 ? candidates[1] : null;
        var hasSecondCandidate = second != null;
        var margin = hasSecondCandidate
            ? Math.Round(best.SimilarityScore - second!.SimilarityScore, 1)
            : double.PositiveInfinity;

        var scoreAccepted = best.SimilarityScore >= matchThreshold;
        var marginAccepted = !hasSecondCandidate || margin >= minimumMargin;
        var isMatch = scoreAccepted && marginAccepted;

        foreach (var candidate in candidates)
        {
            candidate.DecisionHint = candidate.Rank == 1
                ? BuildBestCandidateHint(second, margin, isMatch, matchThreshold, minimumMargin)
                : $"Różnica do najlepszego: {(best.SimilarityScore - candidate.SimilarityScore):0.0} pp";
        }

        if (!isMatch)
        {
            return new FaceIdentificationResult
            {
                Success = true,
                MatchFound = false,
                RequiresReview = false,
                PersonId = null,
                PersonFullName = "Nie znaleziono osoby w bazie",
                PersonCode = "-",
                Department = "-",
                Status = "-",
                SimilarityScore = best.SimilarityScore,
                SecondBestScore = second?.SimilarityScore,
                MarginScore = hasSecondCandidate ? margin : 0,
                HasSecondCandidate = hasSecondCandidate,
                QualityScore = qualityScore,
                MatchThreshold = matchThreshold,
                MinimumMatchMargin = minimumMargin,
                Decision = "Nie znaleziono dopasowania",
                Message = BuildNoMatchMessage(best, second, margin, matchThreshold, minimumMargin, scoreAccepted, marginAccepted),
                MatchedImagePath = string.Empty,
                Candidates = candidates
            };
        }

        return new FaceIdentificationResult
        {
            Success = true,
            MatchFound = true,
            RequiresReview = false,
            PersonId = best.PersonId,
            PersonFullName = best.PersonFullName,
            PersonCode = best.PersonCode,
            Department = best.Department,
            Status = best.Status,
            SimilarityScore = best.SimilarityScore,
            SecondBestScore = second?.SimilarityScore,
            MarginScore = hasSecondCandidate ? margin : 0,
            HasSecondCandidate = hasSecondCandidate,
            QualityScore = qualityScore,
            MatchThreshold = matchThreshold,
            MinimumMatchMargin = minimumMargin,
            Decision = "Znaleziono dopasowanie",
            Message = hasSecondCandidate
                ? $"Znaleziono dopasowanie twarzy. Wynik: {best.SimilarityScore:0.0}%. Przewaga nad drugim kandydatem: {margin:0.0} pp."
                : $"Znaleziono dopasowanie twarzy. Wynik: {best.SimilarityScore:0.0}%.",
            MatchedImagePath = best.SourceImagePath,
            Candidates = candidates
        };
    }

    private static FaceIdentificationResult CreateFailedResult(
        string message,
        string decision,
        double matchThreshold,
        double minimumMargin)
    {
        return new FaceIdentificationResult
        {
            Success = false,
            MatchFound = false,
            RequiresReview = false,
            Message = message,
            Decision = decision,
            PersonFullName = "Brak wyniku",
            PersonCode = "-",
            Department = "-",
            Status = "-",
            MatchThreshold = matchThreshold,
            MinimumMatchMargin = minimumMargin
        };
    }

    private static FaceCandidateMatch BuildCandidateForPerson(List<RawFaceCandidateScore> scores)
    {
        var orderedScores = scores
            .OrderByDescending(score => score.SimilarityScore)
            .ToList();

        var bestScore = orderedScores[0];

        var topScores = orderedScores
            .Take(Math.Min(3, orderedScores.Count))
            .Select(score => score.SimilarityScore)
            .ToList();

        var best = topScores[0];
        var average = topScores.Average();

        return new FaceCandidateMatch
        {
            PersonId = bestScore.Person.Id,
            PersonFullName = bestScore.Person.FullName,
            PersonCode = bestScore.Person.PersonCode,
            Department = bestScore.Person.Department,
            Status = bestScore.Person.Status,
            SourceImagePath = bestScore.Template.SourceImagePath,
            SimilarityScore = Math.Round(best, 1),
            BestTemplateScore = Math.Round(best, 1),
            AverageTopTemplatesScore = Math.Round(average, 1),
            TemplateCount = orderedScores.Count
        };
    }

    private static string BuildBestCandidateHint(
        FaceCandidateMatch? second,
        double margin,
        bool isMatch,
        double matchThreshold,
        double minimumMargin)
    {
        var marginText = second == null
            ? "brak drugiego kandydata"
            : $"przewaga: {margin:0.0} pp";

        return isMatch
            ? $"Najlepszy kandydat, {marginText}"
            : $"Poniżej progu {matchThreshold:0.0}% albo przewagi {minimumMargin:0.0} pp, {marginText}";
    }

    private static string BuildNoMatchMessage(
        FaceCandidateMatch best,
        FaceCandidateMatch? second,
        double margin,
        double matchThreshold,
        double minimumMargin,
        bool scoreAccepted,
        bool marginAccepted)
    {
        var reason = !scoreAccepted
            ? $"Najwyższy wynik twarzy ({best.SimilarityScore:0.0}%) jest poniżej progu {matchThreshold:0.0}%."
            : $"Najlepszy kandydat nie ma wystarczającej przewagi nad drugim wynikiem. Przewaga: {margin:0.0} pp, wymagane: {minimumMargin:0.0} pp.";

        var secondInfo = second == null
            ? "brak drugiego kandydata"
            : $"drugi kandydat: {second.PersonFullName} {second.SimilarityScore:0.0}%";

        return $"Nie znaleziono dopasowania. Najbliższy kandydat techniczny: {best.PersonFullName} ({best.SimilarityScore:0.0}%). {secondInfo}. {reason}";
    }

    private static float[] DeserializeEmbedding(string embeddingJson)
    {
        if (string.IsNullOrWhiteSpace(embeddingJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<float[]>(embeddingJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static double CalculateSimilarityPercentage(float[] first, float[] second)
    {
        if (first.Length == 0 || second.Length == 0 || first.Length != second.Length)
        {
            return 0;
        }

        double dot = 0;
        double normFirst = 0;
        double normSecond = 0;

        for (var i = 0; i < first.Length; i++)
        {
            dot += first[i] * second[i];
            normFirst += first[i] * first[i];
            normSecond += second[i] * second[i];
        }

        if (normFirst == 0 || normSecond == 0)
        {
            return 0;
        }

        var cosine = dot / (Math.Sqrt(normFirst) * Math.Sqrt(normSecond));
        var percentage = cosine * 100.0;

        return Math.Clamp(percentage, 0, 100);
    }

    private sealed class RawFaceCandidateScore
    {
        public Person Person { get; set; } = new();

        public FaceTemplate Template { get; set; } = new();

        public double SimilarityScore { get; set; }
    }
}
