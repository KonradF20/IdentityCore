using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using IdentityCore.App.Data;
using IdentityCore.App.Models;
using IdentityCore.App.Services;

namespace IdentityCore.App.ViewModels;

public class FaceIdentificationViewModel : ViewModelBase
{
    private readonly IdentificationRepository _identificationRepository = new();
    private readonly PersonRepository _personRepository = new();
    private readonly FaceRecognitionService _faceRecognitionService = new();
    private readonly CameraService _cameraService = new();
    private readonly ImageProcessingService _imageProcessingService = new();
    private bool _isCameraRunning = false;

    private string _inputFilePath = string.Empty;
    private string _previewImagePath = string.Empty;
    private ImageSource? _previewImage;
    private ImageSource? _analysisCropImage;

    private Visibility _previewOverlayVisibility = Visibility.Visible;
    private Visibility _analysisCropImageVisibility = Visibility.Collapsed;
    private Visibility _analysisCropPlaceholderVisibility = Visibility.Visible;
    private Visibility _scanAnimationVisibility = Visibility.Collapsed;
    private Visibility _toastVisibility = Visibility.Collapsed;

    private string _cameraPreviewStatus = "Oczekiwanie na sygnał wideo...";
    private string _analysisProgressText = "0%";
    private double _analysisProgress = 0;

    private string _bestMatchName = "Brak wyniku";
    private string _bestMatchCode = "-";
    private string _bestMatchDepartment = "-";
    private string _bestMatchStatus = "-";
    private string _matchedProfileImagePath = string.Empty;
    private Visibility _profileImageVisibility = Visibility.Collapsed;
    private Visibility _profilePlaceholderVisibility = Visibility.Visible;
    private bool _isOpenProfileEnabled;
    private int? _matchedPersonId;
    private double _similarityScore = 0;
    private string _decision = "Nie wykonano analizy";
    private string _marginScoreText = "Przewaga: -";
    private string _secondBestScoreText = "Drugi kandydat: -";
    private string _sampleQualityText = "Nie oceniono";

    private string _statusMessage = string.Empty;
    private string _errorMessage = string.Empty;

    private string _toastMessage = string.Empty;
    private string _toastIcon = "✓";
    private Brush _toastBackground = CreateBrush("#17191F");
    private Brush _toastAccentBrush = CreateBrush("#00D2FF");
    private int _toastVersion;

    private IdentificationAttempt? _lastAttempt;

    public ObservableCollection<AnalysisStepViewModel> AnalysisSteps { get; } = new();

    public ObservableCollection<FaceCandidateMatch> TopCandidates { get; } = new();

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

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        set
        {
            _previewImage = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? AnalysisCropImage
    {
        get => _analysisCropImage;
        set
        {
            _analysisCropImage = value;
            OnPropertyChanged();
        }
    }

    public Visibility PreviewOverlayVisibility
    {
        get => _previewOverlayVisibility;
        set
        {
            _previewOverlayVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility AnalysisCropImageVisibility
    {
        get => _analysisCropImageVisibility;
        set
        {
            _analysisCropImageVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility AnalysisCropPlaceholderVisibility
    {
        get => _analysisCropPlaceholderVisibility;
        set
        {
            _analysisCropPlaceholderVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility ScanAnimationVisibility
    {
        get => _scanAnimationVisibility;
        set
        {
            _scanAnimationVisibility = value;
            OnPropertyChanged();
        }
    }

    public string CameraPreviewStatus
    {
        get => _cameraPreviewStatus;
        set
        {
            _cameraPreviewStatus = value;
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

    public string BestMatchStatus
    {
        get => _bestMatchStatus;
        set
        {
            _bestMatchStatus = value;
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

    public string SimilarityScoreText => $"{SimilarityScore:0.0}%";

    public string Decision
    {
        get => _decision;
        set
        {
            _decision = value;
            OnPropertyChanged();
        }
    }

    public string MarginScoreText
    {
        get => _marginScoreText;
        set
        {
            _marginScoreText = value;
            OnPropertyChanged();
        }
    }

    public string SecondBestScoreText
    {
        get => _secondBestScoreText;
        set
        {
            _secondBestScoreText = value;
            OnPropertyChanged();
        }
    }

    public string SampleQualityText
    {
        get => _sampleQualityText;
        set
        {
            _sampleQualityText = value;
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

            if (!string.IsNullOrWhiteSpace(value))
            {
                ShowToast(value, isError: false);
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();

            if (!string.IsNullOrWhiteSpace(value))
            {
                ShowToast(value, isError: true);
            }
        }
    }

    public string ToastMessage
    {
        get => _toastMessage;
        set
        {
            _toastMessage = value;
            OnPropertyChanged();
        }
    }

    public string ToastIcon
    {
        get => _toastIcon;
        set
        {
            _toastIcon = value;
            OnPropertyChanged();
        }
    }

    public Brush ToastBackground
    {
        get => _toastBackground;
        set
        {
            _toastBackground = value;
            OnPropertyChanged();
        }
    }

    public Brush ToastAccentBrush
    {
        get => _toastAccentBrush;
        set
        {
            _toastAccentBrush = value;
            OnPropertyChanged();
        }
    }

    public Visibility ToastVisibility
    {
        get => _toastVisibility;
        set
        {
            _toastVisibility = value;
            OnPropertyChanged();
        }
    }

    public ICommand StartCameraCommand { get; }
    public ICommand StopCameraCommand { get; }
    public ICommand CapturePhotoCommand { get; }
    public ICommand LoadImageCommand { get; }
    public ICommand StartIdentificationCommand { get; }
    public ICommand SaveAttemptCommand { get; }
    public ICommand OpenPersonProfileCommand { get; }

    public FaceIdentificationViewModel()
    {
        StartCameraCommand = new RelayCommand(_ => StartCamera());
        StopCameraCommand = new RelayCommand(_ => StopCamera());
        CapturePhotoCommand = new RelayCommand(_ => CapturePhoto());
        LoadImageCommand = new RelayCommand(_ => LoadImage());
        StartIdentificationCommand = new RelayCommand(async _ => await StartIdentificationAsync());
        SaveAttemptCommand = new RelayCommand(_ => SaveAttempt(), _ => _lastAttempt != null);
        OpenPersonProfileCommand = new RelayCommand(_ => OpenMatchedPersonProfile(), _ => IsOpenProfileEnabled);

        ResetAnalysisSteps();
    }

    private void StartCamera()
    {
        try
        {
            _cameraService.Start(0);

            _isCameraRunning = true;
            _ = CameraLoopAsync();

            InputFilePath = string.Empty;
            PreviewImagePath = string.Empty;
            PreviewOverlayVisibility = Visibility.Collapsed;

            ResetAnalysis();

            CameraPreviewStatus = "Podgląd z kamery aktywny";
            StatusMessage = "Kamera została uruchomiona. Wykonaj zdjęcie, aby rozpocząć identyfikację.";
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się uruchomić kamery: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void StopCamera()
    {
        try
        {
            StopCameraPreview();

            PreviewImage = null;
            PreviewImagePath = string.Empty;
            InputFilePath = string.Empty;

            ResetAnalysis();

            PreviewOverlayVisibility = Visibility.Visible;
            CameraPreviewStatus = "Kamera wyłączona";
            StatusMessage = "Podgląd z kamery został wyłączony.";
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się wyłączyć kamery: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private async Task CameraLoopAsync()
    {
        while (_isCameraRunning)
        {
            try
            {
                // Praca w tle na innym wątku, dzięki czemu UI nie laguje!
                var frame = await Task.Run(() => _cameraService.CaptureFrame());

                if (frame != null)
                {
                    PreviewImage = frame;
                }
            }
            catch
            {
                _isCameraRunning = false;
                CameraPreviewStatus = "Błąd podglądu kamery";
            }

            await Task.Delay(30); // Oczekujemy 30ms między klatkami
        }
    }

    private void CapturePhoto()
    {
        try
        {
            if (!_cameraService.IsRunning)
            {
                ErrorMessage = "Najpierw uruchom kamerę.";
                StatusMessage = string.Empty;
                return;
            }

            var inputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Input");
            Directory.CreateDirectory(inputDirectory);

            var fileName = $"face_capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(inputDirectory, fileName);

            _cameraService.SaveCurrentFrame(filePath);

            _isCameraRunning = false;
            _cameraService.Stop();

            InputFilePath = filePath;
            PreviewImagePath = filePath;
            PreviewImage = LoadBitmapImage(filePath);
            PreviewOverlayVisibility = Visibility.Collapsed;

            ResetAnalysis();

            CameraPreviewStatus = "Zapisano zdjęcie z kamery";
            StatusMessage = "Zdjęcie zostało zapisane. Możesz rozpocząć identyfikację.";
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się wykonać zdjęcia: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void LoadImage()
    {
        try
        {
            StopCameraPreview();

            var openFileDialog = new OpenFileDialog
            {
                Title = "Wybierz obraz twarzy",
                Filter = "Obrazy (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Wszystkie pliki (*.*)|*.*",
                Multiselect = false
            };

            var result = openFileDialog.ShowDialog();

            if (result != true)
            {
                StatusMessage = "Anulowano wybór obrazu.";
                ErrorMessage = string.Empty;
                return;
            }

            InputFilePath = openFileDialog.FileName;
            PreviewImagePath = openFileDialog.FileName;
            PreviewImage = LoadBitmapImage(openFileDialog.FileName);
            PreviewOverlayVisibility = Visibility.Collapsed;

            ResetAnalysis();

            CameraPreviewStatus = "Wczytano obraz z pliku";
            StatusMessage = "Obraz został wczytany. Możesz rozpocząć identyfikację.";
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się wczytać obrazu: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private async Task StartIdentificationAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InputFilePath))
            {
                ErrorMessage = "Najpierw zrób zdjęcie z kamery albo wczytaj obraz z pliku.";
                StatusMessage = string.Empty;
                return;
            }

            ResetAnalysisSteps();
            ResetResultData();

            AnalysisProgress = 0;
            AnalysisProgressText = "0%";
            StatusMessage = "Rozpoczęto analizę obrazu twarzy.";
            ErrorMessage = string.Empty;

            SetStepRunning(0);
            ShowAnalysisCropPreview();
            ScanAnimationVisibility = Visibility.Visible;
            AnalysisProgress = 15;
            AnalysisProgressText = "15%";
            await Task.Delay(260);
            SetStepCompleted(0);

            SetStepRunning(1);
            AnalysisProgress = 35;
            AnalysisProgressText = "35%";
            await Task.Delay(260);
            SetStepCompleted(1);

            SetStepRunning(2);
            AnalysisProgress = 58;
            AnalysisProgressText = "58%";
            await Task.Delay(260);
            SetStepCompleted(2);

            SetStepRunning(3);
            AnalysisProgress = 78;
            AnalysisProgressText = "78%";

            var result = await Task.Run(() => _faceRecognitionService.Identify(InputFilePath));

            await Task.Delay(260);

            if (!result.Success)
            {
                SetStepFailed(3);
                ScanAnimationVisibility = Visibility.Collapsed;

                AnalysisProgress = 100;
                AnalysisProgressText = "100%";

                ErrorMessage = result.Message;
                StatusMessage = string.Empty;

                ResetResultData();
                _lastAttempt = null;
                CommandManager.InvalidateRequerySuggested();
                return;
            }

            SetStepCompleted(3);

            SetStepRunning(4);
            AnalysisProgress = 94;
            AnalysisProgressText = "94%";
            await Task.Delay(260);

            BestMatchName = result.PersonFullName;
            BestMatchCode = result.PersonCode;
            BestMatchDepartment = result.Department;
            BestMatchStatus = result.Status;
            ApplyMatchedPersonProfile(result.MatchFound ? result.PersonId : null);
            SimilarityScore = result.SimilarityScore;
            Decision = result.Decision;
            MarginScoreText = result.HasSecondCandidate
                ? $"Przewaga: {result.MarginScore:0.0} pp"
                : "Przewaga: brak drugiego kandydata";
            SecondBestScoreText = result.SecondBestScore.HasValue
                ? $"Drugi kandydat: {result.SecondBestScore.Value:0.0}%"
                : "Drugi kandydat: -";
            SampleQualityText = FormatQualityScore(result.QualityScore);

            UpdateTopCandidates(result.Candidates);

            var attemptStatus = result.MatchFound
                ? "Znaleziono"
                : result.RequiresReview
                    ? "Do weryfikacji"
                    : "Nie znaleziono";

            var shouldSaveCandidate = result.MatchFound || result.RequiresReview;

            _lastAttempt = new IdentificationAttempt
            {
                AttemptDate = DateTime.Now,
                BiometricType = "Twarz",
                InputFilePath = InputFilePath,
                BestMatchPersonId = shouldSaveCandidate ? result.PersonId : null,
                BestMatchPersonName = shouldSaveCandidate ? result.PersonFullName : "Nie znaleziono osoby w bazie",
                SimilarityScore = result.SimilarityScore,
                Decision = result.Decision,
                OperatorUsername = "admin",
                Status = attemptStatus
            };

            SetStepCompleted(4);
            ScanAnimationVisibility = Visibility.Collapsed;

            AnalysisProgress = 100;
            AnalysisProgressText = "100%";

            StatusMessage = result.Message;
            ErrorMessage = string.Empty;

            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            ScanAnimationVisibility = Visibility.Collapsed;
            MarkCurrentStepAsFailed();

            ErrorMessage = $"Błąd podczas identyfikacji: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void SaveAttempt()
    {
        if (_lastAttempt == null)
        {
            ErrorMessage = "Najpierw wykonaj identyfikację.";
            StatusMessage = string.Empty;
            return;
        }

        try
        {
            var newId = _identificationRepository.AddAttempt(_lastAttempt);
            _lastAttempt.Id = newId;

            StatusMessage = $"Zapisano próbę identyfikacji w historii. ID próby: {newId}.";
            ErrorMessage = string.Empty;
            _lastAttempt = null;

            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się zapisać próby identyfikacji: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void ResetAnalysis()
    {
        AnalysisProgress = 0;
        AnalysisProgressText = "0%";

        AnalysisCropImage = null;
        AnalysisCropImageVisibility = Visibility.Collapsed;
        AnalysisCropPlaceholderVisibility = Visibility.Visible;
        ScanAnimationVisibility = Visibility.Collapsed;

        ResetResultData();

        _lastAttempt = null;

        ResetAnalysisSteps();

        CommandManager.InvalidateRequerySuggested();
    }

    private void ResetResultData()
    {
        BestMatchName = "Brak wyniku";
        BestMatchCode = "-";
        BestMatchDepartment = "-";
        BestMatchStatus = "-";
        SimilarityScore = 0;
        Decision = "Nie wykonano analizy";
        MarginScoreText = "Przewaga: -";
        SecondBestScoreText = "Drugi kandydat: -";
        SampleQualityText = "Nie oceniono";
        TopCandidates.Clear();
        ClearMatchedPersonProfile();
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

    private void ResetAnalysisSteps()
    {
        AnalysisSteps.Clear();

        AnalysisSteps.Add(new AnalysisStepViewModel { Name = "Detekcja twarzy" });
        AnalysisSteps.Add(new AnalysisStepViewModel { Name = "Normalizacja obrazu" });
        AnalysisSteps.Add(new AnalysisStepViewModel { Name = "Ekstrakcja cech" });
        AnalysisSteps.Add(new AnalysisStepViewModel { Name = "Porównanie z bazą" });
        AnalysisSteps.Add(new AnalysisStepViewModel { Name = "Decyzja systemu" });

        foreach (var step in AnalysisSteps)
        {
            step.SetWaiting();
        }
    }

    private void SetStepRunning(int index)
    {
        if (index >= 0 && index < AnalysisSteps.Count)
        {
            AnalysisSteps[index].SetRunning();
        }
    }

    private void SetStepCompleted(int index)
    {
        if (index >= 0 && index < AnalysisSteps.Count)
        {
            AnalysisSteps[index].SetCompleted();
        }
    }

    private void SetStepFailed(int index)
    {
        if (index >= 0 && index < AnalysisSteps.Count)
        {
            AnalysisSteps[index].SetFailed();
        }
    }

    private void MarkCurrentStepAsFailed()
    {
        var runningStep = AnalysisSteps.FirstOrDefault(step => step.Status == "w toku");

        if (runningStep != null)
        {
            runningStep.SetFailed();
            return;
        }

        if (AnalysisSteps.Count > 0)
        {
            AnalysisSteps[^1].SetFailed();
        }
    }

    private void UpdateTopCandidates(IEnumerable<FaceCandidateMatch> candidates)
    {
        TopCandidates.Clear();

        foreach (var candidate in candidates)
        {
            TopCandidates.Add(candidate);
        }
    }

    private void ShowAnalysisCropPreview()
    {
        AnalysisCropImage = TryCreateAnalysisCropImage(InputFilePath);

        if (AnalysisCropImage != null)
        {
            AnalysisCropImageVisibility = Visibility.Visible;
            AnalysisCropPlaceholderVisibility = Visibility.Collapsed;
        }
        else
        {
            AnalysisCropImageVisibility = Visibility.Collapsed;
            AnalysisCropPlaceholderVisibility = Visibility.Visible;
        }
    }

    private ImageSource? TryCreateAnalysisCropImage(string filePath)
    {
        try
        {
            using var originalFrame = Cv2.ImRead(filePath, ImreadModes.Color);

            if (originalFrame.Empty())
            {
                return null;
            }

            using var preparedFace = _imageProcessingService.ExtractAndPrepareFace(originalFrame, out var qualityScore);

            if (preparedFace == null)
            {
                return null;
            }

            SampleQualityText = FormatQualityScore(qualityScore);

            var bitmap = BitmapSourceConverter.ToBitmapSource(preparedFace);
            bitmap.Freeze();

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatQualityScore(double qualityScore)
    {
        var label = qualityScore switch
        {
            >= 15 => "Dobra",
            >= 7 => "Średnia",
            > 0 => "Niska",
            _ => "Nie oceniono"
        };

        return qualityScore > 0
            ? $"{label} ({qualityScore:0.0})"
            : label;
    }

    private void ShowToast(string message, bool isError)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var currentVersion = ++_toastVersion;

        ToastMessage = message;
        ToastIcon = isError ? "!" : "✓";
        ToastBackground = isError ? CreateBrush("#2A1115") : CreateBrush("#10231D");
        ToastAccentBrush = isError ? CreateBrush("#F87171") : CreateBrush("#00E096");
        ToastVisibility = Visibility.Visible;

        _ = Task.Run(async () =>
        {
            await Task.Delay(isError ? 6500 : 4500);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_toastVersion == currentVersion)
                {
                    ToastVisibility = Visibility.Collapsed;
                }
            });
        });
    }

    private static Brush CreateBrush(string color)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private void StopCameraPreview()
    {
        _isCameraRunning = false;
        _cameraService.Stop();
    }

    private static ImageSource LoadBitmapImage(string filePath)
    {
        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }
}
