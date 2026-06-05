using System.Collections.Generic;

namespace IdentityCore.App.Models;

public class FaceIdentificationResult
{
    public bool Success { get; set; }

    public bool MatchFound { get; set; }

    public bool RequiresReview { get; set; }

    public int? PersonId { get; set; }

    public string PersonFullName { get; set; } = string.Empty;

    public string PersonCode { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Najlepszy wynik podobieństwa z rankingu kandydatów. Nie jest to procent pewności.
    /// </summary>
    public double SimilarityScore { get; set; }

    public double? SecondBestScore { get; set; }

    public double MarginScore { get; set; }

    public bool HasSecondCandidate { get; set; }

    public double QualityScore { get; set; }

    public double MatchThreshold { get; set; }

    public double MinimumMatchMargin { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string MatchedImagePath { get; set; } = string.Empty;

    public List<FaceCandidateMatch> Candidates { get; set; } = new();
}
