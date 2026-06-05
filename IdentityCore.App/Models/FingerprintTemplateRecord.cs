using System;

namespace IdentityCore.App.Models;

public class FingerprintTemplateRecord
{
    public int Id { get; set; }

    public int PersonId { get; set; }

    public string FingerPosition { get; set; } = "Prawy palec wskazujący";

    public string SourceImagePath { get; set; } = string.Empty;

    public string SourceType { get; set; } = "Plik";

    public byte[] TemplateData { get; set; } = Array.Empty<byte>();

    public double QualityScore { get; set; }

    public string AlgorithmName { get; set; } = "SourceAFIS";

    public int Dpi { get; set; } = 500;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
