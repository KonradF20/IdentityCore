using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using IdentityCore.App.Data;
using IdentityCore.App.Models;
using IdentityCore.App.Services;

namespace IdentityCore.App.ViewModels;

public class FingerprintIdentificationViewModel : ViewModelBase
{
    private readonly IdentificationRepository _identificationRepository = new();
    private readonly PersonRepository _personRepository = new();
    private readonly FingerprintRecognitionService _fingerprintRecognitionService = new();

    private string _inputFilePath = string.Empty;
    private string _previewImagePath = string.Empty;
    private string _previewStatus = "Wczytaj obraz odcisku palca z pliku PNG/JPG/BMP.";
    private string _analysisProgressText = "0%";
    private double _analysisProgress = 0;

    private string _bestMatchName = "Brak wyniku";
    private string _bestMatchCode = "-";
    private string _bestMatchDepartment = "-";
    private string _matchedProfileImagePath = string.Empty;
    private Visibility _profileImageVisibility = Visibility.Collapsed;
    private Visibility _profilePlaceholderVisibility = Visibility.Visible;
    private bool _isOpenProfileEnabled;
    private int? _matchedPersonId;
    private double _similarityScore = 0;
    private string _decision = "Nie wykonano analizy";

    private string _ridgeQuality = "-";
    private string _minutiaeCount = "-";
    private string _matchMarginText = "-";
    private string _thresholdText = "Próg: 85,0";

    private string _statusMessage = string.Empty;
    private string _errorMessage = string.Empty;

    private string _step1Icon = "○";
    private string _step1Status = "oczekuje";
    private string _step1Brush = "#6B7280";
    private string _step1TextBrush = "#7E8597";

    private string _step2Icon = "○";
    private string _step2Status = "oczekuje";
    private string _step2Brush = "#6B7280";
    private string _step2TextBrush = "#7E8597";

    private string _step3Icon = "○";
    private string _step3Status = "oczekuje";
    private string _step3Brush = "#6B7280";
    private string _step3TextBrush = "#7E8597";

    private string _step4Icon = "○";
    private string _step4Status = "oczekuje";
    private string _step4Brush = "#6B7280";
    private string _step4TextBrush = "#7E8597";

    private string _step5Icon = "○";
    private string _step5Status = "oczekuje";
    private string _step5Brush = "#6B7280";
    private string _step5TextBrush = "#7E8597";

    private bool _isSaveAttemptEnabled;
    private bool _isAnalysisRunning;

    private IdentificationAttempt? _lastAttempt;

    public string InputFilePath
    {
        get => _inputFilePath;
        set
        {
            _inputFilePath = value;
            OnPropertyChanged();
        }
    }

    public string PreviewImagePath
    {
        get => _previewImagePath;
        set
        {
            _previewImagePath = value;
            OnPropertyChanged();
        }
    }

    public string PreviewStatus
    {
        get => _previewStatus;
        set
        {
            _previewStatus = value;
            OnPropertyChanged();
        }
    }

    public string AnalysisProgressText
    {
        get => _analysisProgressText;
        set
        {
            _analysisProgressText = value;
            OnPropertyChanged();
        }
    }

    public double AnalysisProgress
    {
        get => _analysisProgress;
        set
        {
            _analysisProgress = value;
            AnalysisProgressText = $"{Math.Round(value):0}%";
            OnPropertyChanged();
        }
    }

    public string BestMatchName
    {
        get => _bestMatchName;
        set
        {
            _bestMatchName = value;
            OnPropertyChanged();
        }
    }

    public string BestMatchCode
    {
        get => _bestMatchCode;
        set
        {
            _bestMatchCode = value;
            OnPropertyChanged();
        }
    }

    public string BestMatchDepartment
    {
        get => _bestMatchDepartment;
        set
        {
            _bestMatchDepartment = value;
            OnPropertyChanged();
        }
    }

    public string MatchedProfileImagePath
    {
        get => _matchedProfileImagePath;
        set
        {
            _matchedProfileImagePath = value;
            OnPropertyChanged();
        }
    }

    public Visibility ProfileImageVisibility
    {
        get => _profileImageVisibility;
        set
        {
            _profileImageVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility ProfilePlaceholderVisibility
    {
        get => _profilePlaceholderVisibility;
        set
        {
            _profilePlaceholderVisibility = value;
            OnPropertyChanged();
        }
    }

    public bool IsOpenProfileEnabled
    {
        get => _isOpenProfileEnabled;
        set
        {
            _isOpenProfileEnabled = value;
            OnPropertyChanged();
        }
    }

    public double SimilarityScore
    {
        get => _similarityScore;
        set
        {
            _similarityScore = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SimilarityScoreText));
        }
    }

    public string SimilarityScoreText => $"{SimilarityScore:0.0}";

    public string Decision
    {
        get => _decision;
        set
        {
            _decision = value;
            OnPropertyChanged();
        }
    }

    public string RidgeQuality
    {
        get => _ridgeQuality;
        set
        {
            _ridgeQuality = value;
            OnPropertyChanged();
        }
    }

    public string MinutiaeCount
    {
        get => _minutiaeCount;
        set
        {
            _minutiaeCount = value;
            OnPropertyChanged();
        }
    }

    public string MatchMarginText
    {
        get => _matchMarginText;
        set
        {
            _matchMarginText = value;
            OnPropertyChanged();
        }
    }

    public string ThresholdText
    {
        get => _thresholdText;
        set
        {
            _thresholdText = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public string Step1Icon { get => _step1Icon; set { _step1Icon = value; OnPropertyChanged(); } }
    public string Step1Status { get => _step1Status; set { _step1Status = value; OnPropertyChanged(); } }
    public string Step1Brush { get => _step1Brush; set { _step1Brush = value; OnPropertyChanged(); } }
    public string Step1TextBrush { get => _step1TextBrush; set { _step1TextBrush = value; OnPropertyChanged(); } }

    public string Step2Icon { get => _step2Icon; set { _step2Icon = value; OnPropertyChanged(); } }
    public string Step2Status { get => _step2Status; set { _step2Status = value; OnPropertyChanged(); } }
    public string Step2Brush { get => _step2Brush; set { _step2Brush = value; OnPropertyChanged(); } }
    public string Step2TextBrush { get => _step2TextBrush; set { _step2TextBrush = value; OnPropertyChanged(); } }

    public string Step3Icon { get => _step3Icon; set { _step3Icon = value; OnPropertyChanged(); } }
    public string Step3Status { get => _step3Status; set { _step3Status = value; OnPropertyChanged(); } }
    public string Step3Brush { get => _step3Brush; set { _step3Brush = value; OnPropertyChanged(); } }
    public string Step3TextBrush { get => _step3TextBrush; set { _step3TextBrush = value; OnPropertyChanged(); } }

    public string Step4Icon { get => _step4Icon; set { _step4Icon = value; OnPropertyChanged(); } }
    public string Step4Status { get => _step4Status; set { _step4Status = value; OnPropertyChanged(); } }
    public string Step4Brush { get => _step4Brush; set { _step4Brush = value; OnPropertyChanged(); } }
    public string Step4TextBrush { get => _step4TextBrush; set { _step4TextBrush = value; OnPropertyChanged(); } }

    public string Step5Icon { get => _step5Icon; set { _step5Icon = value; OnPropertyChanged(); } }
    public string Step5Status { get => _step5Status; set { _step5Status = value; OnPropertyChanged(); } }
    public string Step5Brush { get => _step5Brush; set { _step5Brush = value; OnPropertyChanged(); } }
    public string Step5TextBrush { get => _step5TextBrush; set { _step5TextBrush = value; OnPropertyChanged(); } }

    public bool IsSaveAttemptEnabled
    {
        get => _isSaveAttemptEnabled;
        set
        {
            _isSaveAttemptEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsAnalysisRunning
    {
        get => _isAnalysisRunning;
        set
        {
            _isAnalysisRunning = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadFingerprintCommand { get; }
    public ICommand StartAnalysisCommand { get; }
    public ICommand SaveAttemptCommand { get; }
    public ICommand OpenPersonProfileCommand { get; }

    public FingerprintIdentificationViewModel()
    {
        LoadFingerprintCommand = new RelayCommand(_ => LoadFingerprint());
        StartAnalysisCommand = new RelayCommand(async _ => await StartAnalysisAsync());
        SaveAttemptCommand = new RelayCommand(_ => SaveAttempt());
        OpenPersonProfileCommand = new RelayCommand(_ => OpenMatchedPersonProfile(), _ => IsOpenProfileEnabled);

        ResetAnalysis(clearLoadedImageStep: true);
    }

    private void LoadFingerprint()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Wybierz obraz odcisku palca",
                Filter = "Obrazy odcisku (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Wszystkie pliki (*.*)|*.*",
                Multiselect = false
            };

            var result = openFileDialog.ShowDialog();

            if (result != true)
            {
                StatusMessage = "Anulowano wybór obrazu odcisku.";
                ErrorMessage = string.Empty;
                return;
            }

            InputFilePath = openFileDialog.FileName;
            PreviewImagePath = openFileDialog.FileName;
            PreviewStatus = $"Wczytano: {System.IO.Path.GetFileName(openFileDialog.FileName)}";

            ResetAnalysis(clearLoadedImageStep: false);
            MarkStepDone(1);
            AnalysisProgress = 20;

            StatusMessage = "Obraz odcisku został wczytany. Możesz rozpocząć analizę.";
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się wczytać obrazu odcisku: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private async Task StartAnalysisAsync()
    {
        if (IsAnalysisRunning)
        {
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(InputFilePath))
            {
                ErrorMessage = "Najpierw wczytaj obraz odcisku palca.";
                StatusMessage = string.Empty;
                return;
            }

            IsAnalysisRunning = true;
            IsSaveAttemptEnabled = false;
            _lastAttempt = null;

            StatusMessage = "Trwa analiza odcisku palca...";
            ErrorMessage = string.Empty;

            ResetAnalysis(clearLoadedImageStep: false);
            MarkStepDone(1);
            AnalysisProgress = 20;

            await Task.Delay(180);
            MarkStepRunning(2);
            AnalysisProgress = 40;

            var result = await Task.Run(() => _fingerprintRecognitionService.Identify(InputFilePath));

            MarkStepDone(2);
            RidgeQuality = "Template SourceAFIS";

            await Task.Delay(180);
            MarkStepRunning(3);
            AnalysisProgress = 60;

            await Task.Delay(180);
            MarkStepDone(3);
            MinutiaeCount = result.IsMatch ? "Cechy zgodne z bazą" : "Brak zgodności z bazą";

            await Task.Delay(180);
            MarkStepRunning(4);
            AnalysisProgress = 80;

            ApplyResult(result);

            MarkStepDone(4);

            await Task.Delay(180);
            MarkStepRunning(5);
            AnalysisProgress = 95;

            await Task.Delay(180);
            MarkStepDone(5);
            AnalysisProgress = 100;

            StatusMessage = result.Message;
            ErrorMessage = result.Success ? string.Empty : result.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Błąd podczas analizy odcisku: {ex.Message}";
            StatusMessage = string.Empty;
            MarkStepError(5);
        }
        finally
        {
            IsAnalysisRunning = false;
        }
    }

    private void ApplyResult(FingerprintIdentificationResult result)
    {
        BestMatchName = result.IsMatch ? result.PersonFullName : "Nie znaleziono osoby w bazie";
        BestMatchCode = result.IsMatch ? result.PersonCode : "-";
        BestMatchDepartment = result.IsMatch ? result.Department : "-";
        ApplyMatchedPersonProfile(result.IsMatch ? result.PersonId : null);
        SimilarityScore = result.SimilarityScore;
        Decision = result.Decision;
        ThresholdText = $"Próg: {result.MatchThreshold:0.0}";
        MatchMarginText = result.Candidates.Count > 1
            ? $"Przewaga Top1-Top2: {result.MatchMargin:0.0}"
            : "Przewaga Top1-Top2: -";

        _lastAttempt = new IdentificationAttempt
        {
            AttemptDate = DateTime.Now,
            BiometricType = "Odcisk",
            InputFilePath = result.InputImagePath,
            BestMatchPersonId = result.IsMatch ? result.PersonId : null,
            BestMatchPersonName = result.IsMatch ? result.PersonFullName : string.Empty,
            SimilarityScore = result.SimilarityScore,
            Decision = result.Decision,
            OperatorUsername = "admin",
            Status = result.IsMatch ? "Znaleziono" : "Nie znaleziono"
        };

        IsSaveAttemptEnabled = true;
    }

    private void SaveAttempt()
    {
        if (_lastAttempt == null)
        {
            ErrorMessage = "Najpierw wykonaj analizę odcisku palca.";
            StatusMessage = string.Empty;
            return;
        }

        try
        {
            var newId = _identificationRepository.AddAttempt(_lastAttempt);
            _lastAttempt.Id = newId;

            StatusMessage = $"Zapisano próbę identyfikacji odcisku w bazie danych. ID próby: {newId}.";
            ErrorMessage = string.Empty;
            _lastAttempt = null;
            IsSaveAttemptEnabled = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się zapisać próby identyfikacji: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void ResetAnalysis(bool clearLoadedImageStep)
    {
        AnalysisProgress = clearLoadedImageStep ? 0 : 20;

        BestMatchName = "Brak wyniku";
        BestMatchCode = "-";
        BestMatchDepartment = "-";
        SimilarityScore = 0;
        Decision = "Nie wykonano analizy";
        ClearMatchedPersonProfile();

        RidgeQuality = "-";
        MinutiaeCount = "-";
        MatchMarginText = "-";
        ThresholdText = "Próg: 85,0";

        _lastAttempt = null;
        IsSaveAttemptEnabled = false;

        ResetStep(1);
        ResetStep(2);
        ResetStep(3);
        ResetStep(4);
        ResetStep(5);

        if (!clearLoadedImageStep)
        {
            MarkStepDone(1);
        }
    }


    private void ApplyMatchedPersonProfile(int? personId)
    {
        _matchedPersonId = null;
        IsOpenProfileEnabled = false;
        MatchedProfileImagePath = string.Empty;
        ProfileImageVisibility = Visibility.Collapsed;
        ProfilePlaceholderVisibility = Visibility.Visible;

        if (!personId.HasValue)
        {
            CommandManager.InvalidateRequerySuggested();
            return;
        }

        var person = _personRepository.GetById(personId.Value);
        if (person == null)
        {
            CommandManager.InvalidateRequerySuggested();
            return;
        }

        _matchedPersonId = person.Id;
        IsOpenProfileEnabled = true;

        var profileImagePath = ResolveDisplayImagePath(person.ProfileImagePath);
        if (!string.IsNullOrWhiteSpace(profileImagePath))
        {
            MatchedProfileImagePath = profileImagePath;
            ProfileImageVisibility = Visibility.Visible;
            ProfilePlaceholderVisibility = Visibility.Collapsed;
        }

        CommandManager.InvalidateRequerySuggested();
    }

    private void ClearMatchedPersonProfile()
    {
        _matchedPersonId = null;
        IsOpenProfileEnabled = false;
        MatchedProfileImagePath = string.Empty;
        ProfileImageVisibility = Visibility.Collapsed;
        ProfilePlaceholderVisibility = Visibility.Visible;
        CommandManager.InvalidateRequerySuggested();
    }

    private void OpenMatchedPersonProfile()
    {
        if (!_matchedPersonId.HasValue)
        {
            ErrorMessage = "Najpierw wykonaj identyfikację zakończoną dopasowaniem osoby.";
            StatusMessage = string.Empty;
            return;
        }

        if (TryNavigateToPersonProfile(_matchedPersonId.Value))
        {
            StatusMessage = "Otworzono profil osoby w Rejestrze osób.";
            ErrorMessage = string.Empty;
            return;
        }

        StatusMessage = $"Profil osoby {BestMatchCode} znajduje się w zakładce Rejestr osób. Automatyczne przejście podepniemy przez MainViewModel.";
        ErrorMessage = string.Empty;
    }

    private static bool TryNavigateToPersonProfile(int personId)
    {
        var mainViewModel = Application.Current?.MainWindow?.DataContext;
        var method = mainViewModel?.GetType().GetMethod("OpenPersonProfile");

        if (method == null)
        {
            return false;
        }

        method.Invoke(mainViewModel, new object[] { personId });
        return true;
    }

    private static string ResolveDisplayImagePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (Path.IsPathFullyQualified(path) && File.Exists(path))
        {
            return path;
        }

        var baseCandidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        if (File.Exists(baseCandidate))
        {
            return baseCandidate;
        }

        var currentCandidate = Path.Combine(Directory.GetCurrentDirectory(), path);
        if (File.Exists(currentCandidate))
        {
            return currentCandidate;
        }

        return string.Empty;
    }

    private void ResetStep(int stepNumber)
    {
        SetStep(stepNumber, "○", "oczekuje", "#6B7280", "#7E8597");
    }

    private void MarkStepRunning(int stepNumber)
    {
        SetStep(stepNumber, "●", "w toku", "#22D3EE", "White");
    }

    private void MarkStepDone(int stepNumber)
    {
        SetStep(stepNumber, "✓", "gotowe", "#FBBF24", "White");
    }

    private void MarkStepError(int stepNumber)
    {
        SetStep(stepNumber, "!", "błąd", "#F87171", "#F87171");
    }

    private void SetStep(int stepNumber, string icon, string status, string brush, string textBrush)
    {
        switch (stepNumber)
        {
            case 1:
                Step1Icon = icon;
                Step1Status = status;
                Step1Brush = brush;
                Step1TextBrush = textBrush;
                break;
            case 2:
                Step2Icon = icon;
                Step2Status = status;
                Step2Brush = brush;
                Step2TextBrush = textBrush;
                break;
            case 3:
                Step3Icon = icon;
                Step3Status = status;
                Step3Brush = brush;
                Step3TextBrush = textBrush;
                break;
            case 4:
                Step4Icon = icon;
                Step4Status = status;
                Step4Brush = brush;
                Step4TextBrush = textBrush;
                break;
            case 5:
                Step5Icon = icon;
                Step5Status = status;
                Step5Brush = brush;
                Step5TextBrush = textBrush;
                break;
        }
    }
}
