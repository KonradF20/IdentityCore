namespace IdentityCore.App.Models;

public class FaceEmbeddingResult
{
    public bool Success { get; set; }

    public float[] Embedding { get; set; } = [];

    public double QualityScore { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ProcessedImagePath { get; set; } = string.Empty;
}