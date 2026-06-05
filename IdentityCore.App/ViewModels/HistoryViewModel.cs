using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using IdentityCore.App.Data;
using IdentityCore.App.Models;

namespace IdentityCore.App.ViewModels;

public class HistoryViewModel : ViewModelBase
{
    private readonly IdentificationRepository _identificationRepository = new();
    private readonly List<IdentificationAttempt> _allAttempts = new();

    private IdentificationAttempt? _selectedAttempt;
    private string _errorMessage = string.Empty;
    private string _selectedBiometricFilter = "Wszystkie";
    private string _selectedDecisionFilter = "Wszystkie";

    private int _totalAttempts;
    private int _foundAttempts;
    private int _notFoundAttempts;

    public ObservableCollection<IdentificationAttempt> Attempts { get; } = new();

    public ObservableCollection<string> BiometricFilterOptions { get; } = new()
    {
        "Wszystkie",
        "Twarz",
        "Odcisk"
    };

    public ObservableCollection<string> DecisionFilterOptions { get; } = new()
    {
        "Wszystkie",
        "Znaleziono",
        "Nie znaleziono"
    };

    public IdentificationAttempt? SelectedAttempt
    {
        get => _selectedAttempt;
        set
        {
            _selectedAttempt = value;
            OnPropertyChanged();
        }
    }

    public string SelectedBiometricFilter
    {
        get => _selectedBiometricFilter;
        set
        {
            if (_selectedBiometricFilter == value)
            {
                return;
            }

            _selectedBiometricFilter = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public string SelectedDecisionFilter
    {
        get => _selectedDecisionFilter;
        set
        {
            if (_selectedDecisionFilter == value)
            {
                return;
            }

            _selectedDecisionFilter = value;
            OnPropertyChanged();
            ApplyFilters();
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

    public int FoundAttempts
    {
        get => _foundAttempts;
        set
        {
            _foundAttempts = value;
            OnPropertyChanged();
        }
    }

    public int NotFoundAttempts
    {
        get => _notFoundAttempts;
        set
        {
            _notFoundAttempts = value;
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

    public ICommand RefreshCommand { get; }

    public HistoryViewModel()
    {
        RefreshCommand = new RelayCommand(_ => LoadAttempts());
        LoadAttempts();
    }

    private void LoadAttempts()
    {
        try
        {
            _allAttempts.Clear();
            _allAttempts.AddRange(_identificationRepository.GetAllAttempts());

            TotalAttempts = _allAttempts.Count;
            FoundAttempts = _allAttempts.Count(a => a.IsMatchFound);
            NotFoundAttempts = _allAttempts.Count(a => !a.IsMatchFound);

            ErrorMessage = string.Empty;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Attempts.Clear();
            SelectedAttempt = null;
            TotalAttempts = 0;
            FoundAttempts = 0;
            NotFoundAttempts = 0;
            ErrorMessage = $"Nie udało się odczytać historii prób: {ex.Message}";
        }
    }

    private void ApplyFilters()
    {
        var selectedId = SelectedAttempt?.Id;

        IEnumerable<IdentificationAttempt> filtered = _allAttempts;

        if (SelectedBiometricFilter != "Wszystkie")
        {
            filtered = filtered.Where(a => a.BiometricTypeDisplay == SelectedBiometricFilter);
        }

        if (SelectedDecisionFilter == "Znaleziono")
        {
            filtered = filtered.Where(a => a.IsMatchFound);
        }
        else if (SelectedDecisionFilter == "Nie znaleziono")
        {
            filtered = filtered.Where(a => !a.IsMatchFound);
        }

        var result = filtered.ToList();

        Attempts.Clear();
        foreach (var attempt in result)
        {
            Attempts.Add(attempt);
        }

        SelectedAttempt = Attempts.FirstOrDefault(a => a.Id == selectedId) ?? Attempts.FirstOrDefault();
    }
}
