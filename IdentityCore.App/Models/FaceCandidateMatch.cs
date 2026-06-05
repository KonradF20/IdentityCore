namespace IdentityCore.App.Models;

public class FaceCandidateMatch
{
    public int Rank { get; set; }

    public int? PersonId { get; set; }

    public string PersonFullName { get; set; } = string.Empty;

    public string PersonCode { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string SourceImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Najlepszy surowy wynik podobieństwa dla danej osoby. Nie jest to procent pewności.
    /// </summary>
    public double SimilarityScore { get; set; }

    public string SimilarityScoreText => $"{SimilarityScore:0.0}%";

    public double BestTemplateScore { get; set; }

    public double AverageTopTemplatesScore { get; set; }

    public int TemplateCount { get; set; }

    public string DecisionHint { get; set; } = string.Empty;
}
