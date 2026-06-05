namespace IdentityCore.App.Models;

public class MatchResult
{
    public int Id { get; set; }

    public int IdentificationAttemptId { get; set; }

    public int? PersonId { get; set; }

    public string PersonFullName { get; set; } = string.Empty;

    public string PersonCode { get; set; } = string.Empty;

    public double SimilarityScore { get; set; }

    public string Decision { get; set; } = string.Empty;

    public int Rank { get; set; }
}