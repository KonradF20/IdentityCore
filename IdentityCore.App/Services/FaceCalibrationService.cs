using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace IdentityCore.App.Services;

public class FaceCalibrationService
{
    private readonly ImageProcessingService _imageProcessingService = new();
    private readonly FaceEmbeddingService _faceEmbeddingService = new();

    public FaceCalibrationResult RunCalibration()
    {
        var calibrationDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestData",
            "FaceCalibration");

        if (!Directory.Exists(calibrationDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Nie znaleziono folderu kalibracji: {calibrationDirectory}");
        }

        var outputDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestData",
            "FaceCalibrationResults");

        var cropsDirectory = Path.Combine(outputDirectory, "Crops");

        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(cropsDirectory);

        var samples = LoadSamples(calibrationDirectory, cropsDirectory);

        if (samples.Count < 2)
        {
            throw new InvalidOperationException("Za mało poprawnych próbek do kalibracji.");
        }

        var pairs = BuildPairs(samples);

        var samePersonPairs = pairs
            .Where(pair => pair.SamePerson)
            .ToList();

        var differentPersonPairs = pairs
            .Where(pair => !pair.SamePerson)
            .ToList();

        if (samePersonPairs.Count == 0)
        {
            throw new InvalidOperationException("Brak par tej samej osoby. Dodaj minimum 2 zdjęcia do jednego folderu osoby.");
        }

        if (differentPersonPairs.Count == 0)
        {
            throw new InvalidOperationException("Brak par różnych osób. Dodaj minimum 2 foldery osób.");
        }

        var csvPath = Path.Combine(
            outputDirectory,
            $"face_calibration_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        SaveCsv(csvPath, pairs);

        var minSame = samePersonPairs.Min(pair => pair.Score);
        var avgSame = samePersonPairs.Average(pair => pair.Score);
        var maxSame = samePersonPairs.Max(pair => pair.Score);

        var minDifferent = differentPersonPairs.Min(pair => pair.Score);
        var avgDifferent = differentPersonPairs.Average(pair => pair.Score);
        var maxDifferent = differentPersonPairs.Max(pair => pair.Score);

        var hasOverlap = maxDifferent >= minSame;

        var recommendedThreshold = hasOverlap
            ? 0
            : Math.Round((minSame + maxDifferent) / 2.0, 2);

        return new FaceCalibrationResult
        {
            SamplesCount = samples.Count,
            SamePersonPairsCount = samePersonPairs.Count,
            DifferentPersonPairsCount = differentPersonPairs.Count,

            SamePersonMin = Math.Round(minSame, 4),
            SamePersonAverage = Math.Round(avgSame, 4),
            SamePersonMax = Math.Round(maxSame, 4),

            DifferentPersonMin = Math.Round(minDifferent, 4),
            DifferentPersonAverage = Math.Round(avgDifferent, 4),
            DifferentPersonMax = Math.Round(maxDifferent, 4),

            HasOverlap = hasOverlap,
            RecommendedThreshold = recommendedThreshold,
            CsvPath = csvPath
        };
    }

    private List<FaceCalibrationSample> LoadSamples(string calibrationDirectory, string cropsDirectory)
    {
        var samples = new List<FaceCalibrationSample>();

        var personDirectories = Directory
            .GetDirectories(calibrationDirectory)
            .OrderBy(directory => directory)
            .ToList();

        foreach (var personDirectory in personDirectories)
        {
            var personLabel = Path.GetFileName(personDirectory);

            var imagePaths = Directory
                .EnumerateFiles(personDirectory)
                .Where(IsSupportedImage)
                .OrderBy(path => path)
                .ToList();

            foreach (var imagePath in imagePaths)
            {
                using var original = Cv2.ImRead(imagePath, ImreadModes.Color);

                if (original.Empty())
                {
                    continue;
                }

                using var preparedFace = _imageProcessingService.ExtractAndPrepareFace(original, out var qualityScore);

                if (preparedFace == null)
                {
                    continue;
                }

                var cropFileName =
                    $"{SanitizeFileName(personLabel)}_{Path.GetFileNameWithoutExtension(imagePath)}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";

                var cropPath = Path.Combine(cropsDirectory, cropFileName);

                Cv2.ImWrite(cropPath, preparedFace);

                var embedding = _faceEmbeddingService.GenerateEmbeddingFromMat(preparedFace);

                if (embedding.Length == 0)
                {
                    continue;
                }

                samples.Add(new FaceCalibrationSample
                {
                    PersonLabel = personLabel,
                    ImagePath = imagePath,
                    CropPath = cropPath,
                    QualityScore = qualityScore,
                    Embedding = embedding
                });
            }
        }

        return samples;
    }

    private static List<FaceCalibrationPair> BuildPairs(List<FaceCalibrationSample> samples)
    {
        var pairs = new List<FaceCalibrationPair>();

        for (var i = 0; i < samples.Count; i++)
        {
            for (var j = i + 1; j < samples.Count; j++)
            {
                var first = samples[i];
                var second = samples[j];

                var score = CalculateCosineSimilarity(first.Embedding, second.Embedding) * 100.0;

                pairs.Add(new FaceCalibrationPair
                {
                    FirstPerson = first.PersonLabel,
                    SecondPerson = second.PersonLabel,
                    FirstImagePath = first.ImagePath,
                    SecondImagePath = second.ImagePath,
                    FirstCropPath = first.CropPath,
                    SecondCropPath = second.CropPath,
                    SamePerson = first.PersonLabel == second.PersonLabel,
                    Score = Math.Round(score, 4),
                    FirstQuality = Math.Round(first.QualityScore, 4),
                    SecondQuality = Math.Round(second.QualityScore, 4)
                });
            }
        }

        return pairs;
    }

    private static double CalculateCosineSimilarity(float[] first, float[] second)
    {
        if (first.Length == 0 || second.Length == 0 || first.Length != second.Length)
        {
            return 0;
        }

        double dot = 0;
        double firstNorm = 0;
        double secondNorm = 0;

        for (var i = 0; i < first.Length; i++)
        {
            dot += first[i] * second[i];
            firstNorm += first[i] * first[i];
            secondNorm += second[i] * second[i];
        }

        if (firstNorm == 0 || secondNorm == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(firstNorm) * Math.Sqrt(secondNorm));
    }

    private static void SaveCsv(string csvPath, List<FaceCalibrationPair> pairs)
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            "SamePerson;Score;FirstPerson;SecondPerson;FirstImagePath;SecondImagePath;FirstCropPath;SecondCropPath;FirstQuality;SecondQuality");

        foreach (var pair in pairs.OrderByDescending(pair => pair.Score))
        {
            builder.AppendLine(string.Join(";",
                pair.SamePerson ? "TAK" : "NIE",
                pair.Score.ToString("0.0000", CultureInfo.InvariantCulture),
                pair.FirstPerson,
                pair.SecondPerson,
                pair.FirstImagePath,
                pair.SecondImagePath,
                pair.FirstCropPath,
                pair.SecondCropPath,
                pair.FirstQuality.ToString("0.0000", CultureInfo.InvariantCulture),
                pair.SecondQuality.ToString("0.0000", CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(csvPath, builder.ToString(), Encoding.UTF8);
    }

    private static bool IsSupportedImage(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp";
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value;
    }

    private sealed class FaceCalibrationSample
    {
        public required string PersonLabel { get; init; }
        public required string ImagePath { get; init; }
        public required string CropPath { get; init; }
        public required double QualityScore { get; init; }
        public required float[] Embedding { get; init; }
    }

    private sealed class FaceCalibrationPair
    {
        public required string FirstPerson { get; init; }
        public required string SecondPerson { get; init; }

        public required string FirstImagePath { get; init; }
        public required string SecondImagePath { get; init; }

        public required string FirstCropPath { get; init; }
        public required string SecondCropPath { get; init; }

        public required bool SamePerson { get; init; }

        public required double Score { get; init; }

        public required double FirstQuality { get; init; }
        public required double SecondQuality { get; init; }
    }
}

public class FaceCalibrationResult
{
    public required int SamplesCount { get; init; }

    public required int SamePersonPairsCount { get; init; }
    public required int DifferentPersonPairsCount { get; init; }

    public required double SamePersonMin { get; init; }
    public required double SamePersonAverage { get; init; }
    public required double SamePersonMax { get; init; }

    public required double DifferentPersonMin { get; init; }
    public required double DifferentPersonAverage { get; init; }
    public required double DifferentPersonMax { get; init; }

    public required bool HasOverlap { get; init; }

    public required double RecommendedThreshold { get; init; }

    public required string CsvPath { get; init; }

    public override string ToString()
    {
        var thresholdText = HasOverlap
            ? "NIE WYZNACZONO — zakresy się nakładają"
            : $"{RecommendedThreshold:0.00}%";

        var decision = HasOverlap
            ? "WYNIK: zakresy się nakładają. Trzeba poprawić crop/alignment albo jakość zdjęć."
            : "WYNIK: zakresy są rozdzielone. Można ustawić próg decyzyjny.";

        return
            "KALIBRACJA TWARZY\n" +
            $"Liczba próbek: {SamplesCount}\n" +
            $"Pary tej samej osoby: {SamePersonPairsCount}\n" +
            $"Pary różnych osób: {DifferentPersonPairsCount}\n\n" +

            $"TA SAMA OSOBA:\n" +
            $"min: {SamePersonMin:0.0000}%\n" +
            $"avg: {SamePersonAverage:0.0000}%\n" +
            $"max: {SamePersonMax:0.0000}%\n\n" +

            $"RÓŻNE OSOBY:\n" +
            $"min: {DifferentPersonMin:0.0000}%\n" +
            $"avg: {DifferentPersonAverage:0.0000}%\n" +
            $"max: {DifferentPersonMax:0.0000}%\n\n" +

            $"Rekomendowany próg: {thresholdText}\n" +
            $"{decision}\n\n" +

            $"CSV: {CsvPath}";
    }
}