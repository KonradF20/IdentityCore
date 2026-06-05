using System;

namespace IdentityCore.App.Models;

public class BiometricSample
{
    public int Id { get; set; }

    public int PersonId { get; set; }

    public string BiometricType { get; set; } = string.Empty;
    // Face albo Fingerprint

    public string FilePath { get; set; } = string.Empty;

    public double QualityScore { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}