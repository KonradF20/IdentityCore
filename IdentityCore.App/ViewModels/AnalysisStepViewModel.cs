namespace IdentityCore.App.ViewModels;

public class AnalysisStepViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _status = "oczekuje";
    private string _icon = "○";
    private string _brush = "#6B7280";

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public string Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            OnPropertyChanged();
        }
    }

    public string Brush
    {
        get => _brush;
        set
        {
            _brush = value;
            OnPropertyChanged();
        }
    }

    public void SetWaiting()
    {
        Icon = "○";
        Status = "oczekuje";
        Brush = "#6B7280";
    }

    public void SetRunning()
    {
        Icon = "●";
        Status = "w toku";
        Brush = "#FBBF24";
    }

    public void SetCompleted()
    {
        Icon = "✓";
        Status = "zakończono";
        Brush = "#00D2FF";
    }

    public void SetFailed()
    {
        Icon = "!";
        Status = "błąd";
        Brush = "#F87171";
    }
}