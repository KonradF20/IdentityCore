using System.Windows.Input;

namespace IdentityCore.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly PersonRegistryViewModel _personRegistryViewModel = new();

    private DashboardViewModel? _dashboardViewModel;
    private FaceIdentificationViewModel? _faceIdentificationViewModel;
    private FingerprintIdentificationViewModel? _fingerprintIdentificationViewModel;
    private HistoryViewModel? _historyViewModel;
    private SettingsViewModel? _settingsViewModel;

    private ViewModelBase? _currentViewModel;
    private string _currentPageTitle = string.Empty;
    private string _currentPageSubtitle = string.Empty;
    private string _activePageKey = string.Empty;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            _currentViewModel = value;
            OnPropertyChanged();
        }
    }

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        set
        {
            _currentPageTitle = value;
            OnPropertyChanged();
        }
    }

    public string CurrentPageSubtitle
    {
        get => _currentPageSubtitle;
        set
        {
            _currentPageSubtitle = value;
            OnPropertyChanged();
        }
    }

    public string ActivePageKey
    {
        get => _activePageKey;
        set
        {
            _activePageKey = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDashboardActive));
            OnPropertyChanged(nameof(IsPersonRegistryActive));
            OnPropertyChanged(nameof(IsFaceIdentificationActive));
            OnPropertyChanged(nameof(IsFingerprintIdentificationActive));
            OnPropertyChanged(nameof(IsHistoryActive));
            OnPropertyChanged(nameof(IsSettingsActive));
        }
    }

    public bool IsDashboardActive => ActivePageKey == "Dashboard";
    public bool IsPersonRegistryActive => ActivePageKey == "PersonRegistry";
    public bool IsFaceIdentificationActive => ActivePageKey == "FaceIdentification";
    public bool IsFingerprintIdentificationActive => ActivePageKey == "FingerprintIdentification";
    public bool IsHistoryActive => ActivePageKey == "History";
    public bool IsSettingsActive => ActivePageKey == "Settings";

    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowPersonRegistryCommand { get; }
    public ICommand ShowFaceIdentificationCommand { get; }
    public ICommand ShowFingerprintIdentificationCommand { get; }
    public ICommand ShowHistoryCommand { get; }
    public ICommand ShowSettingsCommand { get; }

    public MainViewModel()
    {
        ShowDashboardCommand = new RelayCommand(_ => ShowDashboard());
        ShowPersonRegistryCommand = new RelayCommand(_ => ShowPersonRegistry());
        ShowFaceIdentificationCommand = new RelayCommand(_ => ShowFaceIdentification());
        ShowFingerprintIdentificationCommand = new RelayCommand(_ => ShowFingerprintIdentification());
        ShowHistoryCommand = new RelayCommand(_ => ShowHistory());
        ShowSettingsCommand = new RelayCommand(_ => ShowSettings());

        ShowDashboard();
    }

    public void OpenPersonProfile(int personId)
    {
        ShowPersonRegistry(personId);
    }

    private void ShowDashboard()
    {
        _dashboardViewModel ??= new DashboardViewModel();
        _dashboardViewModel.Reload();

        CurrentViewModel = _dashboardViewModel;
        ActivePageKey = "Dashboard";
        CurrentPageTitle = "Panel główny";
        CurrentPageSubtitle = "Podsumowanie działania systemu identyfikacji osób";
    }

    private void ShowPersonRegistry()
    {
        ShowPersonRegistry(null);
    }

    private void ShowPersonRegistry(int? personId)
    {
        CurrentViewModel = _personRegistryViewModel;
        ActivePageKey = "PersonRegistry";
        CurrentPageTitle = "Rejestr osób";
        CurrentPageSubtitle = "Zarządzanie osobami, zdjęciem profilowym i próbkami biometrycznymi";

        if (personId.HasValue)
        {
            _personRegistryViewModel.SelectPersonById(personId.Value);
        }
    }

    private void ShowFaceIdentification()
    {
        _faceIdentificationViewModel ??= new FaceIdentificationViewModel();

        CurrentViewModel = _faceIdentificationViewModel;
        ActivePageKey = "FaceIdentification";
        CurrentPageTitle = "Identyfikacja twarzy";
        CurrentPageSubtitle = "Rozpoznawanie osoby z kamery lub pliku przez ML.NET/ONNX";
    }

    private void ShowFingerprintIdentification()
    {
        _fingerprintIdentificationViewModel ??= new FingerprintIdentificationViewModel();

        CurrentViewModel = _fingerprintIdentificationViewModel;
        ActivePageKey = "FingerprintIdentification";
        CurrentPageTitle = "Identyfikacja odcisku";
        CurrentPageSubtitle = "Analiza obrazu odcisku palca przez template cech SourceAFIS";
    }

    private void ShowHistory()
    {
        _historyViewModel ??= new HistoryViewModel();

        CurrentViewModel = _historyViewModel;
        ActivePageKey = "History";
        CurrentPageTitle = "Historia prób";
        CurrentPageSubtitle = "Rejestr wykonanych prób identyfikacji";
    }

    private void ShowSettings()
    {
        _settingsViewModel ??= new SettingsViewModel();

        CurrentViewModel = _settingsViewModel;
        ActivePageKey = "Settings";
        CurrentPageTitle = "Ustawienia";
        CurrentPageSubtitle = "Parametry decyzyjne i informacje o modułach biometrycznych";
    }
}
