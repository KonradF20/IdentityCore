using System;
using System.IO;

namespace IdentityCore.App.Models;

public class IdentificationAttempt
{
    public int Id { get; set; }

    public DateTime AttemptDate { get; set; } = DateTime.Now;

    public string BiometricType { get; set; } = string.Empty;

    public string InputFilePath { get; set; } = string.Empty;

    public int? BestMatchPersonId { get; set; }

    public string BestMatchPersonName { get; set; } = string.Empty;

    public double SimilarityScore { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string OperatorUsername { get; set; } = "admin";

    public string Status { get; set; } = string.Empty;

    public string BiometricTypeDisplay
    {
        get
        {
            if (BiometricType.Contains("finger", StringComparison.OrdinalIgnoreCase) ||
                BiometricType.Contains("odcisk", StringComparison.OrdinalIgnoreCase))
            {
                return "Odcisk";
            }

            if (BiometricType.Contains("face", StringComparison.OrdinalIgnoreCase) ||
                BiometricType.Contains("twarz", StringComparison.OrdinalIgnoreCase))
            {
                return "Twarz";
            }

            return string.IsNullOrWhiteSpace(BiometricType) ? "-" : BiometricType;
        }
    }

    public bool IsFingerprint => BiometricTypeDisplay == "Odcisk";

    public bool IsFace => BiometricTypeDisplay == "Twarz";

    public bool IsMatchFound
    {
        get
        {
            if (BestMatchPersonId.HasValue)
            {
                return true;
            }

            if (Status.Contains("Nie znaleziono", StringComparison.OrdinalIgnoreCase) ||
                Decision.Contains("Nie znaleziono", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Status.Contains("Znaleziono", StringComparison.OrdinalIgnoreCase) ||
                   Status.Contains("Zatwierdz", StringComparison.OrdinalIgnoreCase) ||
                   Decision.StartsWith("Znaleziono", StringComparison.OrdinalIgnoreCase);
        }
    }

    public string DecisionDisplay => IsMatchFound ? "Znaleziono dopasowanie" : "Nie znaleziono dopasowania";

    public string BestMatchDisplay => IsMatchFound && !string.IsNullOrWhiteSpace(BestMatchPersonName)
        ? BestMatchPersonName
        : "Brak dopasowania";

    public string ScoreDisplay => IsFingerprint
        ? $"{SimilarityScore:0.0} pkt"
        : $"{SimilarityScore:0}%";

    public string AttemptDateDisplay => AttemptDate.ToString("dd.MM.yyyy HH:mm:ss");

    public string InputFileName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(InputFilePath))
            {
                return "Brak pliku";
            }

            try
            {
                return Path.GetFileName(InputFilePath);
            }
            catch
            {
                return InputFilePath;
            }
        }
    }

    public string InputFolder
    {
        get
        {
            if (string.IsNullOrWhiteSpace(InputFilePath))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetDirectoryName(InputFilePath) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
