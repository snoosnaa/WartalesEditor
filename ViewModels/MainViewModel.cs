using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly JsonDataService jsonDataService = new();
    private readonly SearchService searchService = new();
    private readonly LocalizationService localizationService = new();

    private ProjectModel? project;

    public ProjectModel? Project
    {
        get => project;
        set
        {
            if (SetProperty(ref project, value))
            {
                SelectedSheet = null;
                SearchResults.Clear();

                OnPropertyChanged(nameof(Sheets));
                OnPropertyChanged(nameof(FindAnythingHeader));

                RefreshSearchResults();
            }
        }
    }

    public ObservableCollection<SheetModel> Sheets
    {
        get
        {
            if (Project == null)
                return new ObservableCollection<SheetModel>();

            IEnumerable<SheetModel> visibleSheets = Project.Sheets;

            if (!ShowEmptyCategories)
            {
                visibleSheets =
                    visibleSheets.Where(sheet => sheet.Entries.Count > 0);
            }

            if (SearchScope == "Categories" &&
                !string.IsNullOrWhiteSpace(SearchText))
            {
                visibleSheets = visibleSheets.Where(sheet =>
                    sheet.Name.Contains(
                        SearchText,
                        StringComparison.OrdinalIgnoreCase));
            }

            return new ObservableCollection<SheetModel>(visibleSheets);
        }
    }

    private bool showEmptyCategories;

    public bool ShowEmptyCategories
    {
        get => showEmptyCategories;
        set
        {
            if (SetProperty(ref showEmptyCategories, value))
            {
                OnPropertyChanged(nameof(Sheets));
            }
        }
    }

    private SheetModel? selectedSheet;

    public SheetModel? SelectedSheet
    {
        get => selectedSheet;
        set
        {
            if (SetProperty(ref selectedSheet, value))
            {
                SelectedEntry = null;
                OnPropertyChanged(nameof(Entries));
            }
        }
    }

    public ObservableCollection<EntryModel> Entries
    {
        get
        {
            if (SelectedSheet == null)
                return new ObservableCollection<EntryModel>();

            if (SearchScope != "Settings" ||
                string.IsNullOrWhiteSpace(SearchText))
            {
                return SelectedSheet.Entries;
            }

            return new ObservableCollection<EntryModel>(
                SelectedSheet.Entries.Where(entry =>
                {
                    string localizedName =
                        localizationService.GetLocalizedName(entry.DisplayName)
                        ?? string.Empty;

                    return entry.DisplayName.Contains(
                               SearchText,
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           localizedName.Contains(
                               SearchText,
                               StringComparison.OrdinalIgnoreCase);
                }));
        }
    }

    private EntryModel? selectedEntry;

    public EntryModel? SelectedEntry
    {
        get => selectedEntry;
        set
        {
            if (SetProperty(ref selectedEntry, value))
            {
                SelectedProperty = null;
                OnPropertyChanged(nameof(Properties));
            }
        }
    }

    public ObservableCollection<PropertyModel> Properties =>
        SelectedEntry?.Properties ?? new ObservableCollection<PropertyModel>();

    private PropertyModel? selectedProperty;

    public PropertyModel? SelectedProperty
    {
        get => selectedProperty;
        set => SetProperty(ref selectedProperty, value);
    }

    public ObservableCollection<SearchResultModel> SearchResults { get; }
        = new();

    public bool HasSearchText =>
        !string.IsNullOrWhiteSpace(SearchText);

    public string FindAnythingHeader =>
    $"Find Anything ({SearchResults.Count})";

    private string localizationStatus = "Localization: Not loaded";

    public string LocalizationStatus
    {
        get => localizationStatus;
        set => SetProperty(ref localizationStatus, value);
    }

    private SearchResultModel? selectedSearchResult;

    public SearchResultModel? SelectedSearchResult
    {
        get => selectedSearchResult;
        set
        {
            if (SetProperty(ref selectedSearchResult, value) &&
                value != null)
            {
                NavigateToSearchResult(value);
            }
        }
    }

    private string searchText = string.Empty;

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                OnPropertyChanged(nameof(Sheets));
                OnPropertyChanged(nameof(Entries));
                OnPropertyChanged(nameof(HasSearchText));

                RefreshSearchResults();
            }
        }
    }

    private string searchScope = "Settings";

    public string SearchScope
    {
        get => searchScope;
        set
        {
            if (SetProperty(ref searchScope, value))
            {
                OnPropertyChanged(nameof(Sheets));
                OnPropertyChanged(nameof(Entries));
            }
        }
    }

    private string currentFile = string.Empty;

    public string CurrentFile
    {
        get => currentFile;
        set => SetProperty(ref currentFile, value);
    }

    private string status = "Ready";

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    public ICommand OpenCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand NavigateSearchResultCommand { get; }

    public MainViewModel()
    {
        OpenCommand = new RelayCommand(_ =>
        {
            OpenFileDialog dialog = new()
            {
                Filter =
                    "CDB Files (*.cdb)|*.cdb|" +
                    "JSON Files (*.json)|*.json|" +
                    "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                CurrentFile = dialog.FileName;

                Project = jsonDataService.LoadProject(CurrentFile);

                string localizationFile =
                    System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "export_en.xml");

                if (System.IO.File.Exists(localizationFile))
                {
                    localizationService.Load(localizationFile);

                    LocalizationStatus =
                        $"Localization: English ({localizationService.EntryCount:N0})";
                }
                else
                {
                    LocalizationStatus =
                        "Localization: English not found";
                }

                RefreshSearchResults();

                Status =
                    $"Loaded: {System.IO.Path.GetFileName(CurrentFile)}";
            }
        });

        SaveCommand = new RelayCommand(
            _ =>
            {
                SaveFileDialog dialog = new()
                {
                    Filter =
                        "CDB Files (*.cdb)|*.cdb|" +
                        "All Files (*.*)|*.*",
                    FileName = "data.cdb"
                };

                if (dialog.ShowDialog() == true && Project != null)
                {
                    jsonDataService.SaveProject(
                        Project,
                        dialog.FileName);

                    Status =
                        $"Saved: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
            },
            _ => Project != null);

        NavigateSearchResultCommand = new RelayCommand(
            parameter =>
            {
                SearchResultModel? result =
                    parameter as SearchResultModel
                    ?? SelectedSearchResult;

                NavigateToSearchResult(result);
            },
            parameter =>
                parameter is SearchResultModel ||
                SelectedSearchResult != null);
    }

    private void RefreshSearchResults()
    {
        SearchResults.Clear();
        SelectedSearchResult = null;

        foreach (SearchResultModel result in
                 searchService.Search(
                     Project,
                     SearchText,
                     localizationService))
        {
            SearchResults.Add(result);
        }

        OnPropertyChanged(nameof(FindAnythingHeader));
    }

    private void NavigateToSearchResult(SearchResultModel? result)
    {
        if (result?.Category == null || result.Setting == null)
            return;

        SelectedSheet = result.Category;

        OnPropertyChanged(nameof(Entries));

        SelectedEntry = result.Setting;

        if (!string.IsNullOrWhiteSpace(result.MatchedProperty))
        {
            SelectedProperty = result.Setting.Properties
                .FirstOrDefault(property =>
                    string.Equals(
                        property.Name,
                        result.MatchedProperty,
                        StringComparison.OrdinalIgnoreCase));
        }

        Status =
            $"Selected: {result.CategoryName} → {result.SettingName}";
    }
}