using System;
using System.Collections.ObjectModel;
using System.Linq;
using IdentityCore.App.Data;
using IdentityCore.App.Models;
using IdentityCore.App.Services;

namespace IdentityCore.App.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly PersonRepository _personRepository = new();
    private readonly FaceTemplateRepository _faceTemplateRepository = new();
    private readonly FingerprintTemplateRepository _fingerprintTemplateRepository = new();
    private readonly IdentificationRepository _identificationRepository = new();
    private readonly SystemSettingsService _settingsService = new();

    private int _totalPersons;
    private int _personsWithBiometrics;
    private int _faceTemplates;
    private int _fingerprintTemplates;
    private int _totalAttempts;
    private int _personsWithFaceTemplates;
    private int _personsWithFingerprintTemplates;
    private int _completeBiometricProfiles;

    private string _faceReadinessText = "0 / 0";
    private string _fingerprintReadinessText = "0 / 0";
    private string _completeProfilesReadinessText = "0 / 0";

    private string _faceThresholdDisplay = "-";
    private string _faceMarginDisplay = "-";
    private string _fingerprintThresholdDisplay = "-";
    private string _fingerprintMarginDisplay = "-";
    private string _lastAttemptSummary = "Brak zapisanych prób";

    public ObservableCollection<IdentificationAttempt> RecentAttempts { get; } = new();

    public int TotalPersons
    {
        get => _totalPersons;
        set
        {
            _totalPersons = value;
            OnPropertyChanged();
        }
    }

    public int PersonsWithBiometrics
    {
        get => _personsWithBiometrics;
        set
        {
            _personsWithBiometrics = value;
            OnPropertyChanged();
        }
    }

    public int FaceTemplates
    {
        get => _faceTemplates;
        set
        {
            _faceTemplates = value;
            OnPropertyChanged();
        }
    }

    public int FingerprintTemplates
    {
        get => _fingerprintTemplates;
        set
        {
            _fingerprintTemplates = value;
            OnPropertyChanged();
        }
    }

    public int TotalAttempts
    {
        get => _totalAttempts;
        set
        {
            _totalAttempts = value;
            OnPropertyChanged();
        }
    }

    public int PersonsWithFaceTemplates
    {
        get => _personsWithFaceTemplates;
        set
        {
            _personsWithFaceTemplates = value;
            OnPropertyChanged();
        }
    }

    public int PersonsWithFingerprintTemplates
    {
        get => _personsWithFingerprintTemplates;
        set
        {
            _personsWithFingerprintTemplates = value;
            OnPropertyChanged();
        }
    }

    public int CompleteBiometricProfiles
    {
        get => _completeBiometricProfiles;
        set
        {
            _completeBiometricProfiles = value;
            OnPropertyChanged();
        }
    }

    public string FaceReadinessText
    {
        get => _faceReadinessText;
        set
        {
            _faceReadinessText = value;
            OnPropertyChanged();
        }
    }

    public string FingerprintReadinessText
    {
        get => _fingerprintReadinessText;
        set
        {
            _fingerprintReadinessText = value;
            OnPropertyChanged();
        }
    }

    public string CompleteProfilesReadinessText
    {
        get => _completeProfilesReadinessText;
        set
        {
            _completeProfilesReadinessText = value;
            OnPropertyChanged();
        }
    }

    public string FaceThresholdDisplay
    {
        get => _faceThresholdDisplay;
        set
        {
            _faceThresholdDisplay = value;
            OnPropertyChanged();
        }
    }

    public string FaceMarginDisplay
    {
        get => _faceMarginDisplay;
        set
        {
            _faceMarginDisplay = value;
            OnPropertyChanged();
        }
    }

    public string FingerprintThresholdDisplay
    {
        get => _fingerprintThresholdDisplay;
        set
        {
            _fingerprintThresholdDisplay = value;
            OnPropertyChanged();
        }
    }

    public string FingerprintMarginDisplay
    {
        get => _fingerprintMarginDisplay;
        set
        {
            _fingerprintMarginDisplay = value;
            OnPropertyChanged();
        }
    }

    public string LastAttemptSummary
    {
        get => _lastAttemptSummary;
        set
        {
            _lastAttemptSummary = value;
            OnPropertyChanged();
        }
    }

    public DashboardViewModel()
    {
        Reload();
    }

    public void Reload()
    {
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        try
        {
            var persons = _personRepository.GetAll();
            var faceTemplates = _faceTemplateRepository.GetAll();
            var fingerprintTemplates = _fingerprintTemplateRepository.GetAll();
            var attempts = _identificationRepository.GetAllAttempts();

            var facePersonIds = faceTemplates.Select(template => template.PersonId).Distinct().ToHashSet();
            var fingerprintPersonIds = fingerprintTemplates.Select(template => template.PersonId).Distinct().ToHashSet();
            var biometricPersonIds = facePersonIds.Union(fingerprintPersonIds).ToHashSet();

            TotalPersons = persons.Count;
            PersonsWithBiometrics = biometricPersonIds.Count;
            FaceTemplates = faceTemplates.Count;
            FingerprintTemplates = fingerprintTemplates.Count;
            TotalAttempts = attempts.Count;

            PersonsWithFaceTemplates = facePersonIds.Count;
            PersonsWithFingerprintTemplates = fingerprintPersonIds.Count;
            CompleteBiometricProfiles = facePersonIds.Intersect(fingerprintPersonIds).Count();

            FaceReadinessText = $"{PersonsWithFaceTemplates} / {TotalPersons}";
            FingerprintReadinessText = $"{PersonsWithFingerprintTemplates} / {TotalPersons}";
            CompleteProfilesReadinessText = $"{CompleteBiometricProfiles} / {TotalPersons}";

            RecentAttempts.Clear();

            foreach (var attempt in attempts.Take(8))
            {
                RecentAttempts.Add(attempt);
            }

            LastAttemptSummary = attempts.Count == 0
                ? "Brak zapisanych prób identyfikacji"
                : $"{attempts[0].AttemptDate:dd.MM.yyyy HH:mm} • {attempts[0].BiometricTypeDisplay} • {attempts[0].DecisionDisplay}";

            var settings = _settingsService.GetBiometricDecisionSettings();
            FaceThresholdDisplay = $"{settings.FaceMatchThreshold:0.##}%";
            FaceMarginDisplay = $"{settings.FaceMinimumMargin:0.##} pp";
            FingerprintThresholdDisplay = $"{settings.FingerprintMatchThreshold:0.##} pkt";
            FingerprintMarginDisplay = $"{settings.FingerprintMinimumMargin:0.##} pkt";
        }
        catch
        {
            TotalPersons = 0;
            PersonsWithBiometrics = 0;
            FaceTemplates = 0;
            FingerprintTemplates = 0;
            TotalAttempts = 0;
            PersonsWithFaceTemplates = 0;
            PersonsWithFingerprintTemplates = 0;
            CompleteBiometricProfiles = 0;

            FaceReadinessText = "0 / 0";
            FingerprintReadinessText = "0 / 0";
            CompleteProfilesReadinessText = "0 / 0";
            FaceThresholdDisplay = "-";
            FaceMarginDisplay = "-";
            FingerprintThresholdDisplay = "-";
            FingerprintMarginDisplay = "-";
            LastAttemptSummary = "Brak danych pulpitu";

            RecentAttempts.Clear();
        }
    }
}
