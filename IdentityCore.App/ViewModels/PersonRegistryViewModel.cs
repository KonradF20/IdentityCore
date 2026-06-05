using System;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OpenCvSharp;
using IdentityCore.App.Data;
using IdentityCore.App.Models;
using IdentityCore.App.Services;

namespace IdentityCore.App.ViewModels;

public class PersonRegistryViewModel : ViewModelBase
{
    private readonly PersonRepository _personRepository = new();
    private readonly BiometricSampleRepository _biometricSampleRepository = new();
    private readonly FaceTemplateRepository _faceTemplateRepository = new();
    private readonly FingerprintTemplateRepository _fingerprintTemplateRepository = new();
    private readonly FingerprintRecognitionService _fingerprintRecognitionService = new();

    private Person? _selectedPerson;
    private Person? _selectedListPerson;
    private ImageSource? _selectedProfilePreviewImage;
    private ImageSource? _selectedFacePreviewImage;
    private ImageSource? _selectedFingerprintPreviewImage;

    private string _errorMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private int _selectedPersonFaceTemplatesCount;
    private int _selectedPersonFingerprintTemplatesCount;
    private int _personsWithBiometrics;
    private int _totalFaceTemplates;
    private int _totalFingerprintTemplates;

    public ObservableCollection<Person> Persons { get; } = new();

    public Person? SelectedPerson
    {
        get => _selectedPerson;
        set
        {
            _selectedPerson = value;
            OnPropertyChanged();
            RefreshSelectedPersonImages();
            RefreshSelectedPersonTemplatesCount();
        }
    }

    public Person? SelectedListPerson
    {
        get => _selectedListPerson;
        set
        {
            _selectedListPerson = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? SelectedProfilePreviewImage
    {
        get => _selectedProfilePreviewImage;
        set
        {
            _selectedProfilePreviewImage = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? SelectedFacePreviewImage
    {
        get => _selectedFacePreviewImage;
        set
        {
            _selectedFacePreviewImage = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? SelectedFingerprintPreviewImage
    {
        get => _selectedFingerprintPreviewImage;
        set
        {
            _selectedFingerprintPreviewImage = value;
            OnPropertyChanged();
        }
    }

    public int TotalPersons => Persons.Count;

    public int PersonsWithBiometrics
    {
        get => _personsWithBiometrics;
        set
        {
            _personsWithBiometrics = value;
            OnPropertyChanged();
        }
    }

    public int TotalFaceTemplates
    {
        get => _totalFaceTemplates;
        set
        {
            _totalFaceTemplates = value;
            OnPropertyChanged();
        }
    }

    public int TotalFingerprintTemplates
    {
        get => _totalFingerprintTemplates;
        set
        {
            _totalFingerprintTemplates = value;
            OnPropertyChanged();
        }
    }

    public int SelectedPersonFaceTemplatesCount
    {
        get => _selectedPersonFaceTemplatesCount;
        set
        {
            _selectedPersonFaceTemplatesCount = value;
            OnPropertyChanged();
        }
    }

    public int SelectedPersonFingerprintTemplatesCount
    {
        get => _selectedPersonFingerprintTemplatesCount;
        set
        {
            _selectedPersonFingerprintTemplatesCount = value;
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

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand AddNewPersonCommand { get; }
    public ICommand SaveChangesCommand { get; }
    public ICommand DeleteSelectedPersonCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SetProfileImageCommand { get; }
    public ICommand RemoveProfileImageCommand { get; }
    public ICommand RegisterFingerprintSampleCommand { get; }
    public ICommand RegisterFaceSampleCommand { get; }
    public ICommand OpenPersonDetailsCommand { get; }

    public PersonRegistryViewModel()
    {
        AddNewPersonCommand = new RelayCommand(_ => AddNewPerson());
        SaveChangesCommand = new RelayCommand(_ => SaveChanges(), _ => SelectedPerson != null);
        DeleteSelectedPersonCommand = new RelayCommand(_ => DeleteSelectedPerson(), _ => SelectedPerson != null);
        RefreshCommand = new RelayCommand(_ => LoadPersons());

        SetProfileImageCommand = new RelayCommand(_ => SetProfileImage(), _ => SelectedPerson != null);
        RemoveProfileImageCommand = new RelayCommand(_ => RemoveProfileImage(), _ => SelectedPerson != null);

        RegisterFingerprintSampleCommand = new RelayCommand(_ => RegisterFingerprintSamples(), _ => SelectedPerson != null);
        RegisterFaceSampleCommand = new RelayCommand(_ => RegisterFaceSample(), _ => SelectedPerson != null);
        OpenPersonDetailsCommand = new RelayCommand(parameter => OpenPersonDetails(parameter as Person), parameter => parameter is Person);

        LoadPersons();
    }

    public bool SelectPersonById(int personId)
    {
        var person = Persons.FirstOrDefault(p => p.Id == personId);

        if (person == null)
        {
            LoadPersons();
            person = Persons.FirstOrDefault(p => p.Id == personId);
        }

        if (person == null)
        {
            ErrorMessage = "Nie znaleziono profilu osoby w rejestrze.";
            StatusMessage = string.Empty;
            return false;
        }

        SelectedPerson = person;
        SelectedListPerson = person;
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        return true;
    }

    private void OpenPersonDetails(Person? person)
    {
        if (person == null)
        {
            return;
        }

        SelectPersonById(person.Id);
    }

    private void LoadPersons()
    {
        try
        {
            Persons.Clear();

            var personsFromDatabase = _personRepository.GetAll();

            foreach (var person in personsFromDatabase)
            {
                Persons.Add(person);
            }

            SelectedPerson = Persons.Count > 0 ? Persons[0] : null;
            SelectedListPerson = SelectedPerson;

            RefreshGlobalTemplateCounters();

            OnPropertyChanged(nameof(TotalPersons));
            PersonsWithBiometrics = CalculatePersonsWithBiometrics();

            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się wczytać rejestru osób: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void AddNewPerson()
    {
        try
        {
            var nextNumber = Persons.Count == 0
                ? 1
                : Persons.Max(p => p.Id) + 1;

            var person = new Person
            {
                PersonCode = $"P-{nextNumber:000000}",
                FirstName = "Nowa",
                LastName = "Osoba",
                DateOfBirth = DateTime.Today.AddYears(-25),
                Gender = "Nie podano",
                Department = "Nie przypisano",
                Status = "Aktywny",
                Description = "Nowy profil osoby dodany z poziomu aplikacji.",
                ProfileImagePath = string.Empty,
                FaceImagePath = string.Empty,
                FingerprintImagePath = string.Empty,
                CreatedAt = DateTime.Now
            };

            var newId = _personRepository.Add(person);
            person.Id = newId;

            Persons.Add(person);
            SelectedPerson = person;
            SelectedListPerson = person;

            OnPropertyChanged(nameof(TotalPersons));
            PersonsWithBiometrics = CalculatePersonsWithBiometrics();

            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się dodać osoby: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void SaveChanges()
    {
        if (SelectedPerson == null)
        {
            return;
        }

        try
        {
            _personRepository.Update(SelectedPerson);

            var selectedId = SelectedPerson.Id;
            LoadPersons();
            SelectedPerson = Persons.FirstOrDefault(p => p.Id == selectedId);
            SelectedListPerson = SelectedPerson;

            PersonsWithBiometrics = CalculatePersonsWithBiometrics();

            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się zapisać zmian: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void DeleteSelectedPerson()
    {
        if (SelectedPerson == null)
        {
            return;
        }

        try
        {
            var personToDelete = SelectedPerson;

            _personRepository.Delete(personToDelete.Id);
            Persons.Remove(personToDelete);

            SelectedPerson = Persons.Count > 0 ? Persons[0] : null;
            SelectedListPerson = SelectedPerson;

            RefreshGlobalTemplateCounters();

            OnPropertyChanged(nameof(TotalPersons));
            PersonsWithBiometrics = CalculatePersonsWithBiometrics();

            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się usunąć osoby: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void SetProfileImage()
    {
        if (SelectedPerson == null)
        {
            return;
        }

        try
        {
            var sourcePath = SelectImageFile("Wybierz zdjęcie profilowe osoby");

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                StatusMessage = string.Empty;
                ErrorMessage = string.Empty;
                return;
            }

            var relativePath = CopyImageToTestData(sourcePath, "ProfilePhotos", SelectedPerson.PersonCode);

            SelectedPerson.ProfileImagePath = relativePath;
            _personRepository.Update(SelectedPerson);

            SelectedProfilePreviewImage = LoadImageIfExists(relativePath);

            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się ustawić zdjęcia profilowego: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void RemoveProfileImage()
    {
        if (SelectedPerson == null)
        {
            return;
        }

        try
        {
            SelectedPerson.ProfileImagePath = string.Empty;
            _personRepository.Update(SelectedPerson);
            SelectedProfilePreviewImage = null;

            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się usunąć zdjęcia profilowego: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void RegisterFaceSample()
    {
        if (SelectedPerson == null)
        {
            return;
        }

        try
        {
            var sourcePaths = SelectImageFiles("Wybierz zdjęcia twarzy do rejestracji próbek");

            if (sourcePaths.Length == 0)
            {
                StatusMessage = string.Empty;
                ErrorMessage = string.Empty;
                return;
            }

            StatusMessage = string.Empty;
            ErrorMessage = string.Empty;

            var imageProcessingService = new ImageProcessingService();
            var embeddingService = new FaceEmbeddingService();

            var successCount = 0;
            var failedCount = 0;
            var lastRelativePath = string.Empty;
            var lastQualityScore = 0.0;
            var errorMessages = new List<string>();

            foreach (var sourcePath in sourcePaths)
            {
                try
                {
                    var relativePath = CopyImageToTestData(sourcePath, "Faces", SelectedPerson.PersonCode);
                    var absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

                    using var originalFrame = Cv2.ImRead(absolutePath, ImreadModes.Color);
                    if (originalFrame.Empty())
                    {
                        failedCount++;
                        errorMessages.Add($"{Path.GetFileName(sourcePath)}: Nie można odczytać obrazu.");
                        continue;
                    }

                    using var preparedFace = imageProcessingService.ExtractAndPrepareFace(originalFrame, out double qualityScore);
                    if (preparedFace == null)
                    {
                        failedCount++;
                        errorMessages.Add($"{Path.GetFileName(sourcePath)}: Nie wykryto twarzy na obrazie.");
                        continue;
                    }

                    var embedding = embeddingService.GenerateEmbeddingFromMat(preparedFace);

                    if (embedding == null || embedding.Length == 0)
                    {
                        failedCount++;
                        errorMessages.Add($"{Path.GetFileName(sourcePath)}: Błąd przetwarzania modelu ONNX.");
                        continue;
                    }

                    var embeddingJson = JsonSerializer.Serialize(embedding);

                    var template = new FaceTemplate
                    {
                        PersonId = SelectedPerson.Id,
                        SourceImagePath = relativePath,
                        SourceType = "Plik",
                        EmbeddingJson = embeddingJson,
                        QualityScore = qualityScore,
                        ModelName = "arcface.onnx",
                        CreatedAt = DateTime.Now
                    };

                    _faceTemplateRepository.Add(template);

                    successCount++;
                    lastRelativePath = relativePath;
                    lastQualityScore = qualityScore;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    errorMessages.Add($"{Path.GetFileName(sourcePath)}: {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                SelectedPerson.FaceImagePath = lastRelativePath;
                _personRepository.Update(SelectedPerson);

                _biometricSampleRepository.SaveOrUpdateSample(
                    SelectedPerson.Id,
                    "Twarz",
                    lastRelativePath,
                    lastQualityScore,
                    $"Zarejestrowano {successCount} próbek twarzy jako szablony biometryczne.");

                RefreshSelectedPersonImages();
                RefreshSelectedPersonTemplatesCount();
                RefreshGlobalTemplateCounters();
            }

            StatusMessage = string.Empty;

            ErrorMessage = errorMessages.Count > 0
                ? string.Join(Environment.NewLine, errorMessages.Take(3))
                : string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się zarejestrować próbek twarzy: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private void RegisterFingerprintSamples()
    {
        if (SelectedPerson == null)
        {
            return;
        }

        try
        {
            var sourcePaths = SelectImageFiles("Wybierz obrazy odcisku palca do rejestracji próbek");

            if (sourcePaths.Length == 0)
            {
                StatusMessage = string.Empty;
                ErrorMessage = string.Empty;
                return;
            }

            StatusMessage = string.Empty;
            ErrorMessage = string.Empty;

            var successCount = 0;
            var failedCount = 0;
            var lastRelativePath = string.Empty;
            var errorMessages = new List<string>();

            foreach (var sourcePath in sourcePaths)
            {
                try
                {
                    var relativePath = CopyImageToTestData(sourcePath, "Fingerprints", SelectedPerson.PersonCode);
                    var extractionResult = _fingerprintRecognitionService.CreateTemplate(relativePath);

                    if (!extractionResult.Success || extractionResult.TemplateData.Length == 0)
                    {
                        failedCount++;
                        errorMessages.Add($"{Path.GetFileName(sourcePath)}: {extractionResult.Message}");
                        continue;
                    }

                    var template = new FingerprintTemplateRecord
                    {
                        PersonId = SelectedPerson.Id,
                        FingerPosition = "Prawy palec wskazujący",
                        SourceImagePath = relativePath,
                        SourceType = "Plik",
                        TemplateData = extractionResult.TemplateData,
                        QualityScore = extractionResult.QualityScore,
                        AlgorithmName = extractionResult.AlgorithmName,
                        Dpi = extractionResult.Dpi,
                        CreatedAt = DateTime.Now
                    };

                    _fingerprintTemplateRepository.Add(template);

                    successCount++;
                    lastRelativePath = relativePath;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    errorMessages.Add($"{Path.GetFileName(sourcePath)}: {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                SelectedPerson.FingerprintImagePath = lastRelativePath;
                _personRepository.Update(SelectedPerson);

                _biometricSampleRepository.SaveOrUpdateSample(
                    SelectedPerson.Id,
                    "Odcisk palca",
                    lastRelativePath,
                    0,
                    $"Zarejestrowano {successCount} próbek odcisku jako szablony cech SourceAFIS.");

                RefreshSelectedPersonImages();
                RefreshSelectedPersonTemplatesCount();
                RefreshGlobalTemplateCounters();
            }

            StatusMessage = string.Empty;

            ErrorMessage = errorMessages.Count > 0
                ? string.Join(Environment.NewLine, errorMessages.Take(3))
                : string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nie udało się zarejestrować próbek odcisku: {ex.Message}";
            StatusMessage = string.Empty;
        }
    }

    private static string? SelectImageFile(string title)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = title,
            Filter = "Obrazy (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Wszystkie pliki (*.*)|*.*",
            Multiselect = false
        };

        var result = openFileDialog.ShowDialog();

        return result == true ? openFileDialog.FileName : null;
    }

    private static string[] SelectImageFiles(string title)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = title,
            Filter = "Obrazy (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Wszystkie pliki (*.*)|*.*",
            Multiselect = true
        };

        var result = openFileDialog.ShowDialog();

        return result == true ? openFileDialog.FileNames : [];
    }

    private static string CopyImageToTestData(string sourcePath, string targetFolderName, string personCode)
    {
        var extension = Path.GetExtension(sourcePath);
        var safePersonCode = personCode
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace(":", "_");

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var fileName = $"{safePersonCode}_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{uniqueId}{extension}";

        var relativePath = Path.Combine("TestData", targetFolderName, fileName);

        var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.Copy(sourcePath, outputPath, overwrite: false);

        return relativePath.Replace("\\", "/");
    }

    private void RefreshSelectedPersonImages()
    {
        if (SelectedPerson == null)
        {
            SelectedProfilePreviewImage = null;
            SelectedFacePreviewImage = null;
            SelectedFingerprintPreviewImage = null;
            return;
        }

        SelectedProfilePreviewImage = LoadImageIfExists(SelectedPerson.ProfileImagePath);
        SelectedFacePreviewImage = LoadImageIfExists(SelectedPerson.FaceImagePath);
        SelectedFingerprintPreviewImage = LoadImageIfExists(SelectedPerson.FingerprintImagePath);
    }

    private void RefreshSelectedPersonTemplatesCount()
    {
        if (SelectedPerson == null)
        {
            SelectedPersonFaceTemplatesCount = 0;
            SelectedPersonFingerprintTemplatesCount = 0;
            return;
        }

        try
        {
            SelectedPersonFaceTemplatesCount = _faceTemplateRepository.CountByPersonId(SelectedPerson.Id);
        }
        catch
        {
            SelectedPersonFaceTemplatesCount = 0;
        }

        try
        {
            SelectedPersonFingerprintTemplatesCount = _fingerprintTemplateRepository.CountByPersonId(SelectedPerson.Id);
        }
        catch
        {
            SelectedPersonFingerprintTemplatesCount = 0;
        }
    }


    private int CalculatePersonsWithBiometrics()
    {
        var count = 0;

        foreach (var person in Persons)
        {
            try
            {
                var faceCount = _faceTemplateRepository.CountByPersonId(person.Id);
                var fingerprintCount = _fingerprintTemplateRepository.CountByPersonId(person.Id);

                if (faceCount > 0 || fingerprintCount > 0)
                {
                    count++;
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(person.FaceImagePath) ||
                    !string.IsNullOrWhiteSpace(person.FingerprintImagePath))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void RefreshGlobalTemplateCounters()
    {
        try
        {
            TotalFaceTemplates = _faceTemplateRepository.Count();
        }
        catch
        {
            TotalFaceTemplates = 0;
        }

        try
        {
            TotalFingerprintTemplates = _fingerprintTemplateRepository.Count();
        }
        catch
        {
            TotalFingerprintTemplates = 0;
        }

        PersonsWithBiometrics = CalculatePersonsWithBiometrics();
    }

    private static ImageSource? LoadImageIfExists(string path)
    {
        var resolvedPath = ResolvePath(path);

        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            return null;
        }

        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(resolvedPath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }

    private static string ResolvePath(string path)
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

        return baseCandidate;
    }
}
