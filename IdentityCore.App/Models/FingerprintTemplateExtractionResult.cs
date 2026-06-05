namespace IdentityCore.App.Models;

public class FingerprintTemplateExtractionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public byte[] TemplateData { get; set; } = [];

    public string SourceImagePath { get; set; } = string.Empty;

    public double QualityScore { get; set; }

    public string AlgorithmName { get; set; } = "SourceAFIS";

    public int Dpi { get; set; } = 500;
}
