using System.Collections.Generic;

namespace IdentityCore.App.Models;

public class FingerprintIdentificationResult
{
    public bool Success { get; set; }

    public bool IsMatch { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? PersonId { get; set; }

    public string PersonFullName { get; set; } = string.Empty;

    public string PersonCode { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public double SimilarityScore { get; set; }

    public double MatchThreshold { get; set; }

    public double MatchMargin { get; set; }

    public double MinimumMatchMargin { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string InputImagePath { get; set; } = string.Empty;

    public List<FingerprintCandidateMatch> Candidates { get; set; } = new();
}
