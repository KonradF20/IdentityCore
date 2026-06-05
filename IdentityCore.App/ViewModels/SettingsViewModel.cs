using System;
using System.Windows.Input;
using IdentityCore.App.Models;
using IdentityCore.App.Services;

namespace IdentityCore.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SystemSettingsService _systemSettingsService = new();

    private double _faceMatchThreshold = 60.0;
    private double _faceMinimumMargin = 15.0;
    private double _fingerprintMatchThreshold = 85.0;
    private double _fingerprintMinimumMargin = 15.0;

    public double FaceMatchThreshold
    {
        get => _faceMatchThreshold;
        set
        {
            _faceMatchThreshold = Clamp(value, 0, 100);
            OnPropertyChanged();
            OnPropertyChanged(nameof(FaceMatchThresholdText));
        }
    }

    public double FaceMinimumMargin
    {
        get => _faceMinimumMargin;
        set
        {
            _faceMinimumMargin = Clamp(value, 0, 100);
            OnPropertyChanged();
            OnPropertyChanged(nameof(FaceMinimumMarginText));
        }
    }

    public double FingerprintMatchThreshold
    {
        get => _fingerprintMatchThreshold;
        set
        {
            _fingerprintMatchThreshold = Clamp(value, 0, 250);
            OnPropertyChanged();
            OnPropertyChanged(nameof(FingerprintMatchThresholdText));
        }
    }

    public double FingerprintMinimumMargin
    {
        get => _fingerprintMinimumMargin;
        set
        {
            _fingerprintMinimumMargin = Clamp(value, 0, 250);
            OnPropertyChanged();
            OnPropertyChanged(nameof(FingerprintMinimumMarginText));
        }
    }

    public string FaceMatchThresholdText => $"{FaceMatchThreshold:0.0}%";

    public string FaceMinimumMarginText => $"{FaceMinimumMargin:0.0} pp";

    public string FingerprintMatchThresholdText => $"{FingerprintMatchThreshold:0.0}";

    public string FingerprintMinimumMarginText => $"{FingerprintMinimumMargin:0.0}";

    public ICommand SaveSettingsCommand { get; }

    public ICommand ResetSettingsCommand { get; }

    public SettingsViewModel()
    {
        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
        ResetSettingsCommand = new RelayCommand(_ => ResetSettings());

        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _systemSettingsService.GetBiometricDecisionSettings();

        FaceMatchThreshold = settings.FaceMatchThreshold;
        FaceMinimumMargin = settings.FaceMinimumMargin;
        FingerprintMatchThreshold = settings.FingerprintMatchThreshold;
        FingerprintMinimumMargin = settings.FingerprintMinimumMargin;
    }

    private void SaveSettings()
    {
        var settings = new BiometricDecisionSettings
        {
            FaceMatchThreshold = FaceMatchThreshold,
            FaceMinimumMargin = FaceMinimumMargin,
            FingerprintMatchThreshold = FingerprintMatchThreshold,
            FingerprintMinimumMargin = FingerprintMinimumMargin
        };

        _systemSettingsService.SaveBiometricDecisionSettings(settings);
        LoadSettings();
    }

    private void ResetSettings()
    {
        _systemSettingsService.ResetBiometricDecisionSettings();
        LoadSettings();
    }

    private static double Clamp(double value, double min, double max)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return min;
        }

        return Math.Round(Math.Clamp(value, min, max), 1);
    }
}
