using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Views;

namespace WartalesEditor.ViewModels;

public class MainViewModel : ObservableObject
{
    private const string ProjectOpenFilter =
        "CDB Files (*.cdb)|*.cdb|" +
        "JSON Files (*.json)|*.json|" +
        "All Files (*.*)|*.*";

    private const string ProjectSaveFilter =
        "CDB Files (*.cdb)|*.cdb|" +
        "All Files (*.*)|*.*";

    private readonly JsonDataService jsonDataService;

    private readonly SearchService searchService;

    private readonly LocalizationService localizationService;

    private readonly EditHistoryService editHistoryService;

    private readonly ModificationSnapshotService
        modificationSnapshotService;

    private readonly ModificationSnapshotWorkflowService
        modificationSnapshotWorkflowService;

    private readonly ChangeSummaryService changeSummaryService;

    private readonly ReferenceDataService referenceDataService;

    private readonly IFileDialogService fileDialogService;

    private readonly IMessageDialogService messageDialogService;

    private readonly HashSet<PropertyModel> trackedProperties =
        new();

    private ChangeSummaryWindow? changeSummaryWindow;

    private ChangeSummaryViewModel? changeSummaryViewModel;

    private ProjectModel? project;

    public ProjectModel? Project
    {
        get => project;
        set
        {
            if (ReferenceEquals(
                    project,
                    value))
            {
                return;
            }

            StopTrackingProjectProperties();
            editHistoryService.Clear();

            if (!SetProperty(
                    ref project,
                    value))
            {
                return;
            }

            SelectedSheet = null;
            SelectedEntry = null;
            SelectedProperty = null;

            SearchResults.Clear();

            StartTrackingProjectProperties();

            OnPropertyChanged(nameof(Sheets));
            OnPropertyChanged(nameof(Entries));
            OnPropertyChanged(nameof(Properties));
            OnPropertyChanged(
                nameof(FindAnythingHeader));

            RefreshModificationState();
            RefreshSearchResults();
            RefreshHistoryState();
            RefreshCommandStates();
        }
    }

    public ObservableCollection<SheetModel> Sheets
    {
        get
        {
            if (Project == null)
            {
                return new ObservableCollection<SheetModel>();
            }

            IEnumerable<SheetModel> visibleSheets =
                Project.Sheets;

            if (!ShowEmptyCategories)
            {
                visibleSheets =
                    visibleSheets.Where(
                        sheet =>
                            sheet.Entries.Count > 0);
            }

            if (SearchScope == "Categories" &&
                !string.IsNullOrWhiteSpace(
                    SearchText))
            {
                visibleSheets =
                    visibleSheets.Where(sheet =>
                        sheet.Name.Contains(
                            SearchText,
                            StringComparison
                                .OrdinalIgnoreCase));
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
            if (SetProperty(
                    ref showEmptyCategories,
                    value))
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
            if (!SetProperty(
                    ref selectedSheet,
                    value))
            {
                return;
            }

            SelectedEntry = null;

            OnPropertyChanged(nameof(Entries));
            RefreshCommandStates();
        }
    }

    public ObservableCollection<EntryModel> Entries
    {
        get
        {
            if (SelectedSheet == null)
            {
                return new ObservableCollection<EntryModel>();
            }

            if (SearchScope != "Settings" ||
                string.IsNullOrWhiteSpace(
                    SearchText))
            {
                return SelectedSheet.Entries;
            }

            return new ObservableCollection<EntryModel>(
                SelectedSheet.Entries.Where(entry =>
                {
                    string localizedName =
                        localizationService
                            .GetLocalizedName(
                                entry.DisplayName)
                        ?? string.Empty;

                    return entry.DisplayName.Contains(
                               SearchText,
                               StringComparison
                                   .OrdinalIgnoreCase)
                           ||
                           localizedName.Contains(
                               SearchText,
                               StringComparison
                                   .OrdinalIgnoreCase);
                }));
        }
    }

    private EntryModel? selectedEntry;

    public EntryModel? SelectedEntry
    {
        get => selectedEntry;
        set
        {
            if (!SetProperty(
                    ref selectedEntry,
                    value))
            {
                return;
            }

            SelectedProperty = null;

            OnPropertyChanged(nameof(Properties));
            OnPropertyChanged(
                nameof(CanResetProperty));

            RefreshCommandStates();
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
            if (!SetProperty(
                    ref selectedProperty,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(CanResetProperty));

            RefreshCommandStates();
        }
    }

    public bool CanResetProperty =>
        GetResetTargetProperty() != null;

    public ObservableCollection<SearchResultModel>
        SearchResults
    {
        get;
    } = new();

    public bool HasSearchText =>
        !string.IsNullOrWhiteSpace(
            SearchText);

    public string FindAnythingHeader =>
        $"Find Anything ({SearchResults.Count})";

    private string localizationStatus =
        "Localization: Not loaded";

    public string LocalizationStatus
    {
        get => localizationStatus;
        set => SetProperty(
            ref localizationStatus,
            value);
    }

    private SearchResultModel? selectedSearchResult;

    public SearchResultModel? SelectedSearchResult
    {
        get => selectedSearchResult;
        set
        {
            if (!SetProperty(
                    ref selectedSearchResult,
                    value))
            {
                return;
            }

            RefreshCommandStates();

            if (value != null)
            {
                NavigateToSearchResult(value);
            }
        }
    }

    private string searchText =
        string.Empty;

    public string SearchText
    {
        get => searchText;
        set
        {
            if (!SetProperty(
                    ref searchText,
                    value))
            {
                return;
            }

            OnPropertyChanged(nameof(Sheets));
            OnPropertyChanged(nameof(Entries));
            OnPropertyChanged(
                nameof(HasSearchText));

            RefreshSearchResults();
        }
    }

    private string searchScope =
        "Settings";

    public string SearchScope
    {
        get => searchScope;
        set
        {
            if (!SetProperty(
                    ref searchScope,
                    value))
            {
                return;
            }

            OnPropertyChanged(nameof(Sheets));
            OnPropertyChanged(nameof(Entries));
        }
    }

    private string currentFile =
        string.Empty;

    public string CurrentFile
    {
        get => currentFile;
        set
        {
            if (SetProperty(
                    ref currentFile,
                    value))
            {
                OnPropertyChanged(
                    nameof(WindowTitle));
            }
        }
    }

    private string status =
        "Ready";

    public string Status
    {
        get => status;
        set => SetProperty(
            ref status,
            value);
    }

    private int modifiedPropertyCount;

    public int ModifiedPropertyCount
    {
        get => modifiedPropertyCount;
        private set
        {
            if (!SetProperty(
                    ref modifiedPropertyCount,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(HasModifications));

            OnPropertyChanged(
                nameof(ModificationStatus));

            OnPropertyChanged(
                nameof(WindowTitle));

            OnPropertyChanged(
                nameof(CanResetProperty));
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
                $"{ModifiedPropertyCount:N0} " +
                $"modified {propertyText}";
        }
    }

    public string WindowTitle
    {
        get
        {
            string fileName =
                string.IsNullOrWhiteSpace(
                    CurrentFile)
                    ? "No file loaded"
                    : System.IO.Path.GetFileName(
                        CurrentFile);

            string modifiedMarker =
                HasModifications
                    ? " *"
                    : string.Empty;

            return
                $"Wartales Editor - " +
                $"{fileName}{modifiedMarker}";
        }
    }

    public IReadOnlyList<PropertyModel>
        ModifiedProperties =>
            trackedProperties
                .Where(property =>
                    property.IsModified)
                .ToList();

    public bool CanUndo =>
        editHistoryService.CanUndo;

    public bool CanRedo =>
        editHistoryService.CanRedo;

    public string UndoDescription =>
        editHistoryService.UndoDescription
            is string description
            ? $"Undo {description}"
            : "Nothing to undo";

    public string RedoDescription =>
        editHistoryService.RedoDescription
            is string description
            ? $"Redo {description}"
            : "Nothing to redo";

    public RelayCommand OpenCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand NavigateSearchResultCommand
    {
        get;
    }

    public RelayCommand ResetSelectedPropertyCommand
    {
        get;
    }

    public RelayCommand UndoCommand { get; }

    public RelayCommand RedoCommand { get; }

    public RelayCommand ShowChangeSummaryCommand
    {
        get;
    }

    public MainViewModel(
        JsonDataService jsonDataService,
        SearchService searchService,
        LocalizationService localizationService,
        EditHistoryService editHistoryService,
        ModificationSnapshotService
            modificationSnapshotService,
        ModificationSnapshotWorkflowService
            modificationSnapshotWorkflowService,
        ChangeSummaryService changeSummaryService,
        ReferenceDataService referenceDataService,
        IFileDialogService fileDialogService,
        IMessageDialogService messageDialogService)
    {
        this.jsonDataService =
            jsonDataService
            ?? throw new ArgumentNullException(
                nameof(jsonDataService));

        this.searchService =
            searchService
            ?? throw new ArgumentNullException(
                nameof(searchService));

        this.localizationService =
            localizationService
            ?? throw new ArgumentNullException(
                nameof(localizationService));

        this.editHistoryService =
            editHistoryService
            ?? throw new ArgumentNullException(
                nameof(editHistoryService));

        this.modificationSnapshotService =
            modificationSnapshotService
            ?? throw new ArgumentNullException(
                nameof(modificationSnapshotService));

        this.modificationSnapshotWorkflowService =
            modificationSnapshotWorkflowService
            ?? throw new ArgumentNullException(
                nameof(
                    modificationSnapshotWorkflowService));

        this.changeSummaryService =
            changeSummaryService
            ?? throw new ArgumentNullException(
                nameof(changeSummaryService));

        this.referenceDataService =
            referenceDataService
            ?? throw new ArgumentNullException(
                nameof(referenceDataService));

        this.fileDialogService =
            fileDialogService
            ?? throw new ArgumentNullException(
                nameof(fileDialogService));

        this.messageDialogService =
            messageDialogService
            ?? throw new ArgumentNullException(
                nameof(messageDialogService));

        this.editHistoryService.HistoryChanged +=
            OnHistoryChanged;

        OpenCommand =
            new RelayCommand(
                _ => OpenProject());

        SaveCommand =
            new RelayCommand(
                _ => SaveProject(),
                _ => Project != null);

        NavigateSearchResultCommand =
            new RelayCommand(
                parameter =>
                {
                    SearchResultModel? result =
                        parameter
                            as SearchResultModel
                        ?? SelectedSearchResult;

                    NavigateToSearchResult(result);
                },
                parameter =>
                    parameter
                        is SearchResultModel
                    ||
                    SelectedSearchResult != null);

        ResetSelectedPropertyCommand =
            new RelayCommand(
                _ => ResetSelectedProperty(),
                _ => CanResetProperty);

        UndoCommand =
            new RelayCommand(
                _ => Undo(),
                _ => CanUndo);

        RedoCommand =
            new RelayCommand(
                _ => Redo(),
                _ => CanRedo);

        ShowChangeSummaryCommand =
            new RelayCommand(
                _ => ShowChangeSummary(),
                _ => Project != null);
    }

    private void OpenProject()
    {
        string? fileName =
            fileDialogService.ShowOpenFileDialog(
                ProjectOpenFilter);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        ProjectModel loadedProject =
            jsonDataService.LoadProject(
                fileName);

        referenceDataService.Initialize(
            loadedProject);

        CurrentFile =
            fileName;

        Project =
            loadedProject;

        string localizationFile =
            System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "export_en.xml");

        if (System.IO.File.Exists(
                localizationFile))
        {
            localizationService.Load(
                localizationFile);

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
        RefreshCommandStates();

        Status =
            $"Loaded: " +
            $"{System.IO.Path.GetFileName(CurrentFile)}";
    }

    private void SaveProject()
    {
        if (Project == null)
            return;

        string initialFileName =
            string.IsNullOrWhiteSpace(
                CurrentFile)
                ? "data.cdb"
                : System.IO.Path.GetFileName(
                    CurrentFile);

        string? fileName =
            fileDialogService.ShowSaveFileDialog(
                ProjectSaveFilter,
                initialFileName);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        jsonDataService.SaveProject(
            Project,
            fileName);

        Project.FileName =
            fileName;

        CurrentFile =
            fileName;

        RefreshModificationState();
        RefreshCommandStates();

        Status =
            $"Saved: " +
            $"{System.IO.Path.GetFileName(fileName)}";
    }

    private void StartTrackingProjectProperties()
    {
        if (Project == null)
            return;

        foreach (PropertyModel property in
                 EnumerateProjectProperties(
                     Project))
        {
            if (!trackedProperties.Add(
                    property))
            {
                continue;
            }

            property.ModifiedChanged +=
                OnPropertyModifiedChanged;

            property.ValueChanged +=
                OnPropertyValueChanged;
        }
    }

    private void StopTrackingProjectProperties()
    {
        foreach (PropertyModel property in
                 trackedProperties)
        {
            property.ModifiedChanged -=
                OnPropertyModifiedChanged;

            property.ValueChanged -=
                OnPropertyValueChanged;
        }

        trackedProperties.Clear();
    }

    private static IEnumerable<PropertyModel>
        EnumerateProjectProperties(
            ProjectModel project)
    {
        return project.Sheets
            .SelectMany(sheet =>
                sheet.Entries)
            .SelectMany(entry =>
                entry.Properties);
    }

    private void OnPropertyValueChanged(
        object? sender,
        PropertyValueChangedEventArgs e)
    {
        if (sender
            is not PropertyModel property)
        {
            return;
        }

        editHistoryService.Record(
            property,
            e.PreviousValue,
            e.NewValue);

        RefreshChangeSummaryViewModel();
        RefreshCommandStates();
    }

    private void OnPropertyModifiedChanged(
        object? sender,
        EventArgs e)
    {
        RefreshModificationState();

        if (sender
                is PropertyModel modifiedProperty
            &&
            modifiedProperty.IsModified
            &&
            SelectedProperty == null)
        {
            IReadOnlyList<PropertyModel>
                modifiedProperties =
                    GetModifiedProperties();

            if (modifiedProperties.Count == 1)
            {
                SelectedProperty =
                    modifiedProperty;
            }
        }

        OnPropertyChanged(
            nameof(CanResetProperty));

        RefreshCommandStates();
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

        OnPropertyChanged(
            nameof(UndoDescription));

        OnPropertyChanged(
            nameof(RedoDescription));

        RefreshCommandStates();
    }

    private void RefreshModificationState()
    {
        int modifiedCount =
            trackedProperties.Count(
                property =>
                    property.IsModified);

        ModifiedPropertyCount =
            modifiedCount;

        bool projectIsModified =
            modifiedCount > 0;

        if (Project != null &&
            Project.IsModified
                != projectIsModified)
        {
            Project.IsModified =
                projectIsModified;
        }

        RefreshChangeSummaryViewModel();

        OnPropertyChanged(
            nameof(HasModifications));

        OnPropertyChanged(
            nameof(ModificationStatus));

        OnPropertyChanged(
            nameof(WindowTitle));

        OnPropertyChanged(
            nameof(ModifiedProperties));

        OnPropertyChanged(
            nameof(CanResetProperty));

        RefreshCommandStates();
    }

    private void RefreshCommandStates()
    {
        SaveCommand?.NotifyCanExecuteChanged();

        NavigateSearchResultCommand?
            .NotifyCanExecuteChanged();

        ResetSelectedPropertyCommand?
            .NotifyCanExecuteChanged();

        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();

        ShowChangeSummaryCommand?
            .NotifyCanExecuteChanged();
    }

    private IReadOnlyList<ChangeSummaryItemModel>
        BuildChangeSummaryItems()
    {
        if (Project == null)
        {
            return Array.Empty<
                ChangeSummaryItemModel>();
        }

        ModificationSnapshotModel snapshot =
            modificationSnapshotService.CreateSnapshot(
                Project);

        return changeSummaryService.BuildItems(
            Project,
            snapshot,
            localizationService);
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
            if (changeSummaryWindow.WindowState
                == WindowState.Minimized)
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
                DataContext =
                    changeSummaryViewModel
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
        SelectedSheet =
            item.Category;

        OnPropertyChanged(nameof(Entries));

        SelectedEntry =
            item.Setting;

        SelectedProperty =
            item.Property;

        RefreshCommandStates();

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
                if (mainWindow.WindowState
                    == WindowState.Minimized)
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
            .Where(property =>
                property.IsModified)
            .ToList();
    }

    private PropertyModel? GetResetTargetProperty()
    {
        if (SelectedProperty?.CanReset == true)
        {
            return SelectedProperty;
        }

        IReadOnlyList<PropertyModel>
            modifiedProperties =
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
                    "Select a modified property " +
                    "before resetting.";
            }

            return;
        }

        string propertyName =
            property.Name;

        property.ResetToOriginal();

        SelectedProperty =
            property;

        RefreshCommandStates();

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

        RefreshCommandStates();

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

        RefreshCommandStates();

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

        OnPropertyChanged(
            nameof(FindAnythingHeader));

        RefreshCommandStates();
    }

    private void NavigateToSearchResult(
        SearchResultModel? result)
    {
        if (result?.Category == null ||
            result.Setting == null)
        {
            return;
        }

        SelectedSheet =
            result.Category;

        OnPropertyChanged(nameof(Entries));

        SelectedEntry =
            result.Setting;

        if (!string.IsNullOrWhiteSpace(
                result.MatchedProperty))
        {
            SelectedProperty =
                result.Setting.Properties
                    .FirstOrDefault(property =>
                        string.Equals(
                            property.Name,
                            result.MatchedProperty,
                            StringComparison
                                .OrdinalIgnoreCase));
        }

        RefreshCommandStates();

        Status =
            $"Selected: {result.CategoryName} " +
            $"→ {result.SettingName}";
    }
}