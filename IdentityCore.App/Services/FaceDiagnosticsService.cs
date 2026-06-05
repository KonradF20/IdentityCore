using System;
using System.IO;
using OpenCvSharp;

namespace IdentityCore.App.Services;

public class FaceDiagnosticsService
{
    private readonly ImageProcessingService _imageProcessingService = new();
    private readonly FaceEmbeddingService _faceEmbeddingService = new();

    public FaceDiagnosticsResult CompareTwoImages(string firstImagePath, string secondImagePath)
    {
        var first = PrepareImage(firstImagePath, "first");
        var second = PrepareImage(secondImagePath, "second");

        var similarity = CalculateCosineSimilarity(first.Embedding, second.Embedding) * 100.0;

        return new FaceDiagnosticsResult
        {
            FirstImagePath = firstImagePath,
            SecondImagePath = secondImagePath,
            FirstCropPath = first.CropPath,
            SecondCropPath = second.CropPath,
            FirstQuality = first.QualityScore,
            SecondQuality = second.QualityScore,
            SimilarityScore = Math.Round(similarity, 4),
            FirstEmbeddingNorm = Math.Round(CalculateNorm(first.Embedding), 6),
            SecondEmbeddingNorm = Math.Round(CalculateNorm(second.Embedding), 6)
        };
    }

    private PreparedFaceData PrepareImage(string imagePath, string label)
    {
        using var original = Cv2.ImRead(imagePath, ImreadModes.Color);

        if (original.Empty())
        {
            throw new InvalidOperationException($"Nie można odczytać obrazu: {imagePath}");
        }

        using var preparedFace = _imageProcessingService.ExtractAndPrepareFace(original, out var qualityScore);

        if (preparedFace == null)
        {
            throw new InvalidOperationException($"Nie wykryto twarzy na obrazie: {imagePath}");
        }

        var diagnosticsDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestData",
            "FaceDiagnostics");

        Directory.CreateDirectory(diagnosticsDirectory);

        var fileName = $"{label}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var cropPath = Path.Combine(diagnosticsDirectory, fileName);

        Cv2.ImWrite(cropPath, preparedFace);

        var embedding = _faceEmbeddingService.GenerateEmbeddingFromMat(preparedFace);

        if (embedding.Length == 0)
        {
            throw new InvalidOperationException($"Nie wygenerowano embeddingu dla obrazu: {imagePath}");
        }

        return new PreparedFaceData
        {
            CropPath = cropPath,
            QualityScore = qualityScore,
            Embedding = embedding
        };
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

    private static double CalculateNorm(float[] embedding)
    {
        double sum = 0;

        foreach (var value in embedding)
        {
            sum += value * value;
        }

        return Math.Sqrt(sum);
    }

    private sealed class PreparedFaceData
    {
        public required string CropPath { get; init; }
        public required double QualityScore { get; init; }
        public required float[] Embedding { get; init; }
    }
}

public class FaceDiagnosticsResult
{
    public required string FirstImagePath { get; init; }
    public required string SecondImagePath { get; init; }

    public required string FirstCropPath { get; init; }
    public required string SecondCropPath { get; init; }

    public required double FirstQuality { get; init; }
    public required double SecondQuality { get; init; }

    public required double SimilarityScore { get; init; }

    public required double FirstEmbeddingNorm { get; init; }
    public required double SecondEmbeddingNorm { get; init; }

    public override string ToString()
    {
        return
            $"Wynik podobieństwa: {SimilarityScore:0.0000}%\n" +
            $"Jakość 1: {FirstQuality:0.00}\n" +
            $"Jakość 2: {SecondQuality:0.00}\n" +
            $"Norma embeddingu 1: {FirstEmbeddingNorm:0.000000}\n" +
            $"Norma embeddingu 2: {SecondEmbeddingNorm:0.000000}\n" +
            $"Crop 1: {FirstCropPath}\n" +
            $"Crop 2: {SecondCropPath}";
    }
}