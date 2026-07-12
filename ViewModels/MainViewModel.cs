using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;
using WartalesEditor.Views;

namespace WartalesEditor.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly JsonDataService jsonDataService = new();
    private readonly SearchService searchService = new();
    private readonly LocalizationService localizationService = new();
    private readonly EditHistoryService editHistoryService = new();

    private readonly ReferenceDataService referenceDataService =
        ReferenceDataService.Instance;

    private readonly HashSet<PropertyModel> trackedProperties = new();

    private ChangeSummaryWindow? changeSummaryWindow;

    private ChangeSummaryViewModel? changeSummaryViewModel;

    private ProjectModel? project;

    public ProjectModel? Project
    {
        get => project;
        set
        {
            if (ReferenceEquals(project, value))
                return;

            StopTrackingProjectProperties();
            editHistoryService.Clear();

            if (SetProperty(ref project, value))
            {
                SelectedSheet = null;
                SelectedEntry = null;
                SelectedProperty = null;

                SearchResults.Clear();

                StartTrackingProjectProperties();

                OnPropertyChanged(nameof(Sheets));
                OnPropertyChanged(nameof(Entries));
                OnPropertyChanged(nameof(Properties));
                OnPropertyChanged(nameof(FindAnythingHeader));

                RefreshModificationState();
                RefreshSearchResults();
                RefreshHistoryState();

                CommandManager.InvalidateRequerySuggested();
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
                    visibleSheets.Where(
                        sheet => sheet.Entries.Count > 0);
            }

            if (SearchScope == "Categories" &&
                !string.IsNullOrWhiteSpace(SearchText))
            {
                visibleSheets =
                    visibleSheets.Where(sheet =>
                        sheet.Name.Contains(
                            SearchText,
                            StringComparison.OrdinalIgnoreCase));
            }

            return new ObservableCollection<SheetModel>(
                visibleSheets);
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
                        localizationService.GetLocalizedName(
                            entry.DisplayName)
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
                OnPropertyChanged(nameof(CanResetProperty));

                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public ObservableCollection<PropertyModel> Properties =>
        SelectedEntry?.Properties
        ?? new ObservableCollection<PropertyModel>();

    private PropertyModel? selectedProperty;

    public PropertyModel? SelectedProperty
    {
        get => selectedProperty;
        set
        {
            if (SetProperty(ref selectedProperty, value))
            {
                OnPropertyChanged(nameof(CanResetProperty));

                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool CanResetProperty =>
        GetResetTargetProperty() != null;

    public ObservableCollection<SearchResultModel> SearchResults { get; }
        = new();

    public bool HasSearchText =>
        !string.IsNullOrWhiteSpace(SearchText);

    public string FindAnythingHeader =>
        $"Find Anything ({SearchResults.Count})";

    private string localizationStatus =
        "Localization: Not loaded";

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
        set
        {
            if (SetProperty(ref currentFile, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    private string status = "Ready";

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    private int modifiedPropertyCount;

    public int ModifiedPropertyCount
    {
        get => modifiedPropertyCount;
        private set
        {
            if (SetProperty(
                    ref modifiedPropertyCount,
                    value))
            {
                OnPropertyChanged(nameof(HasModifications));
                OnPropertyChanged(nameof(ModificationStatus));
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(CanResetProperty));
            }
        }
    }

    public bool HasModifications =>
        Project?.IsModified == true;

    public string ModificationStatus
    {
        get
        {
            if (!HasModifications)
                return "No unsaved changes";

            string propertyText =
                ModifiedPropertyCount == 1
                    ? "property"
                    : "properties";

            return
                $"{ModifiedPropertyCount:N0} modified {propertyText}";
        }
    }

    public string WindowTitle
    {
        get
        {
            string fileName =
                string.IsNullOrWhiteSpace(CurrentFile)
                    ? "No file loaded"
                    : System.IO.Path.GetFileName(CurrentFile);

            string modifiedMarker =
                HasModifications
                    ? " *"
                    : string.Empty;

            return
                $"Wartales Editor - {fileName}{modifiedMarker}";
        }
    }

    public IReadOnlyList<PropertyModel> ModifiedProperties =>
        trackedProperties
            .Where(property => property.IsModified)
            .ToList();

    public bool CanUndo =>
        editHistoryService.CanUndo;

    public bool CanRedo =>
        editHistoryService.CanRedo;

    public string UndoDescription =>
        editHistoryService.UndoDescription is string description
            ? $"Undo {description}"
            : "Nothing to undo";

    public string RedoDescription =>
        editHistoryService.RedoDescription is string description
            ? $"Redo {description}"
            : "Nothing to redo";

    public ICommand OpenCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand NavigateSearchResultCommand { get; }

    public ICommand ResetSelectedPropertyCommand { get; }

    public ICommand UndoCommand { get; }

    public ICommand RedoCommand { get; }

    public ICommand ShowChangeSummaryCommand { get; }

    public MainViewModel()
    {
        editHistoryService.HistoryChanged +=
            OnHistoryChanged;

        OpenCommand = new RelayCommand(_ =>
        {
            OpenFileDialog dialog = new()
            {
                Filter =
                    "CDB Files (*.cdb)|*.cdb|" +
                    "JSON Files (*.json)|*.json|" +
                    "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            ProjectModel loadedProject =
                jsonDataService.LoadProject(dialog.FileName);

            referenceDataService.Initialize(loadedProject);

            CurrentFile = dialog.FileName;
            Project = loadedProject;

            string localizationFile =
                System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "export_en.xml");

            if (System.IO.File.Exists(localizationFile))
            {
                localizationService.Load(localizationFile);

                LocalizationStatus =
                    $"Localization: English " +
                    $"({localizationService.EntryCount:N0})";
            }
            else
            {
                LocalizationStatus =
                    "Localization: English not found";
            }

            RefreshSearchResults();
            RefreshChangeSummaryViewModel();

            Status =
                $"Loaded: " +
                $"{System.IO.Path.GetFileName(CurrentFile)}";
        });

        SaveCommand = new RelayCommand(
            _ =>
            {
                if (Project == null)
                    return;

                SaveFileDialog dialog = new()
                {
                    Filter =
                        "CDB Files (*.cdb)|*.cdb|" +
                        "All Files (*.*)|*.*",
                    FileName =
                        string.IsNullOrWhiteSpace(CurrentFile)
                            ? "data.cdb"
                            : System.IO.Path.GetFileName(CurrentFile)
                };

                if (dialog.ShowDialog() != true)
                    return;

                jsonDataService.SaveProject(
                    Project,
                    dialog.FileName);

                Project.FileName = dialog.FileName;
                CurrentFile = dialog.FileName;

                RefreshModificationState();

                Status =
                    $"Saved: " +
                    $"{System.IO.Path.GetFileName(dialog.FileName)}";
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

        ResetSelectedPropertyCommand = new RelayCommand(
            _ => ResetSelectedProperty(),
            _ => CanResetProperty);

        UndoCommand = new RelayCommand(
            _ => Undo(),
            _ => CanUndo);

        RedoCommand = new RelayCommand(
            _ => Redo(),
            _ => CanRedo);

        ShowChangeSummaryCommand = new RelayCommand(
            _ => ShowChangeSummary(),
            _ => Project != null);
    }

    private void StartTrackingProjectProperties()
    {
        if (Project == null)
            return;

        foreach (PropertyModel property in
                 EnumerateProjectProperties(Project))
        {
            if (!trackedProperties.Add(property))
                continue;

            property.ModifiedChanged +=
                OnPropertyModifiedChanged;

            property.ValueChanged +=
                OnPropertyValueChanged;
        }
    }

    private void StopTrackingProjectProperties()
    {
        foreach (PropertyModel property in trackedProperties)
        {
            property.ModifiedChanged -=
                OnPropertyModifiedChanged;

            property.ValueChanged -=
                OnPropertyValueChanged;
        }

        trackedProperties.Clear();
    }

    private static IEnumerable<PropertyModel>
        EnumerateProjectProperties(ProjectModel project)
    {
        return project.Sheets
            .SelectMany(sheet => sheet.Entries)
            .SelectMany(entry => entry.Properties);
    }

    private void OnPropertyValueChanged(
        object? sender,
        PropertyValueChangedEventArgs e)
    {
        if (sender is not PropertyModel property)
            return;

        editHistoryService.Record(
            property,
            e.PreviousValue,
            e.NewValue);

        RefreshChangeSummaryViewModel();
    }

    private void OnPropertyModifiedChanged(
        object? sender,
        EventArgs e)
    {
        RefreshModificationState();

        if (sender is PropertyModel modifiedProperty &&
            modifiedProperty.IsModified &&
            SelectedProperty == null)
        {
            IReadOnlyList<PropertyModel> modifiedProperties =
                GetModifiedProperties();

            if (modifiedProperties.Count == 1)
            {
                SelectedProperty = modifiedProperty;
            }
        }

        OnPropertyChanged(nameof(CanResetProperty));

        CommandManager.InvalidateRequerySuggested();
    }

    private void OnHistoryChanged(
        object? sender,
        EventArgs e)
    {
        RefreshHistoryState();
    }

    private void RefreshHistoryState()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoDescription));
        OnPropertyChanged(nameof(RedoDescription));

        CommandManager.InvalidateRequerySuggested();
    }

    private void RefreshModificationState()
    {
        int modifiedCount =
            trackedProperties.Count(
                property => property.IsModified);

        ModifiedPropertyCount = modifiedCount;

        bool projectIsModified =
            modifiedCount > 0;

        if (Project != null &&
            Project.IsModified != projectIsModified)
        {
            Project.IsModified = projectIsModified;
        }

        RefreshChangeSummaryViewModel();

        OnPropertyChanged(nameof(HasModifications));
        OnPropertyChanged(nameof(ModificationStatus));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(ModifiedProperties));
        OnPropertyChanged(nameof(CanResetProperty));

        CommandManager.InvalidateRequerySuggested();
    }

    private IReadOnlyList<ChangeSummaryItemModel>
        BuildChangeSummaryItems()
    {
        List<ChangeSummaryItemModel> items = new();

        if (Project == null)
            return items;

        foreach (SheetModel category in Project.Sheets)
        {
            foreach (EntryModel setting in category.Entries)
            {
                string settingName =
                    GetChangeSummarySettingName(setting);

                foreach (PropertyModel property in setting.Properties)
                {
                    if (!property.IsModified)
                        continue;

                    items.Add(
                        new ChangeSummaryItemModel(
                            category,
                            setting,
                            property,
                            settingName,
                            property.OriginalDisplayValue,
                            property.CurrentDisplayValue));
                }
            }
        }

        return items;
    }

    private string GetChangeSummarySettingName(
        EntryModel setting)
    {
        string localizedName =
            localizationService.GetLocalizedName(
                setting.DisplayName)
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(localizedName))
            return localizedName;

        if (!string.IsNullOrWhiteSpace(setting.DisplayName))
            return setting.DisplayName;

        if (!string.IsNullOrWhiteSpace(setting.Name))
            return setting.Name;

        return setting.Id;
    }

    private void RefreshChangeSummaryViewModel()
    {
        if (changeSummaryViewModel == null)
            return;

        changeSummaryViewModel.Refresh(
            BuildChangeSummaryItems());
    }

    private void ShowChangeSummary()
    {
        if (Project == null)
            return;

        if (changeSummaryWindow != null)
        {
            if (changeSummaryWindow.WindowState ==
                WindowState.Minimized)
            {
                changeSummaryWindow.WindowState =
                    WindowState.Normal;
            }

            changeSummaryWindow.Activate();
            changeSummaryWindow.Focus();
            return;
        }

        changeSummaryViewModel =
            new ChangeSummaryViewModel(
                BuildChangeSummaryItems(),
                NavigateToChangeSummaryItem);

        changeSummaryWindow =
            new ChangeSummaryWindow
            {
                DataContext = changeSummaryViewModel
            };

        changeSummaryWindow.Closed +=
            OnChangeSummaryWindowClosed;

        changeSummaryWindow.Show();
        changeSummaryWindow.Activate();
    }

    private void OnChangeSummaryWindowClosed(
        object? sender,
        EventArgs e)
    {
        if (changeSummaryWindow != null)
        {
            changeSummaryWindow.Closed -=
                OnChangeSummaryWindowClosed;
        }

        changeSummaryWindow = null;
        changeSummaryViewModel = null;
    }

    private void NavigateToChangeSummaryItem(
        ChangeSummaryItemModel item)
    {
        SelectedSheet = item.Category;

        OnPropertyChanged(nameof(Entries));

        SelectedEntry = item.Setting;
        SelectedProperty = item.Property;

        Status =
            $"Selected: {item.CategoryName} " +
            $"→ {item.SettingName} " +
            $"→ {item.PropertyName}";

        Window? mainWindow =
            Application.Current.MainWindow;

        if (mainWindow == null)
            return;

        mainWindow.Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() =>
            {
                if (mainWindow.WindowState ==
                    WindowState.Minimized)
                {
                    mainWindow.WindowState =
                        WindowState.Normal;
                }

                mainWindow.Activate();
                mainWindow.Focus();
            }));
    }

    private IReadOnlyList<PropertyModel>
        GetModifiedProperties()
    {
        return trackedProperties
            .Where(property => property.IsModified)
            .ToList();
    }

    private PropertyModel? GetResetTargetProperty()
    {
        if (SelectedProperty?.CanReset == true)
        {
            return SelectedProperty;
        }

        IReadOnlyList<PropertyModel> modifiedProperties =
            GetModifiedProperties();

        if (modifiedProperties.Count == 1)
        {
            return modifiedProperties[0];
        }

        return null;
    }

    private void ResetSelectedProperty()
    {
        PropertyModel? property =
            GetResetTargetProperty();

        if (property == null)
        {
            if (ModifiedPropertyCount > 1)
            {
                Status =
                    "Select a modified property before resetting.";
            }

            return;
        }

        string propertyName = property.Name;

        property.ResetToOriginal();

        SelectedProperty = property;

        Status =
            $"Reset property: {propertyName}";
    }

    private void Undo()
    {
        string description =
            editHistoryService.UndoDescription
            ?? "property change";

        if (!editHistoryService.Undo())
            return;

        Status =
            $"Undid: {description}";
    }

    private void Redo()
    {
        string description =
            editHistoryService.RedoDescription
            ?? "property change";

        if (!editHistoryService.Redo())
            return;

        Status =
            $"Redid: {description}";
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

    private void NavigateToSearchResult(
        SearchResultModel? result)
    {
        if (result?.Category == null ||
            result.Setting == null)
        {
            return;
        }

        SelectedSheet = result.Category;

        OnPropertyChanged(nameof(Entries));

        SelectedEntry = result.Setting;

        if (!string.IsNullOrWhiteSpace(
                result.MatchedProperty))
        {
            SelectedProperty =
                result.Setting.Properties
                    .FirstOrDefault(property =>
                        string.Equals(
                            property.Name,
                            result.MatchedProperty,
                            StringComparison.OrdinalIgnoreCase));
        }

        Status =
            $"Selected: {result.CategoryName} " +
            $"→ {result.SettingName}";
    }
}