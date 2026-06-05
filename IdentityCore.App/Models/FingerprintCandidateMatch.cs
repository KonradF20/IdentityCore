namespace IdentityCore.App.Models;

public class FingerprintCandidateMatch
{
    public int Rank { get; set; }

    public int? PersonId { get; set; }

    public string PersonFullName { get; set; } = string.Empty;

    public string PersonCode { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string SourceImagePath { get; set; } = string.Empty;

    public double SimilarityScore { get; set; }

    public string SimilarityScoreText => $"{SimilarityScore:0.0}";
}
