using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Models.Validation;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.Services.Validation;
using WartalesEditor.Views;

namespace WartalesEditor.ViewModels;

public class MainViewModel : ObservableObject
{
    private const string ApplicationVersion =
        "0.7.0";

    private const string ProjectOpenFilter =
        "CDB Files (*.cdb)|*.cdb|" +
        "JSON Files (*.json)|*.json|" +
        "All Files (*.*)|*.*";

    private const string ProjectSaveFilter =
        "CDB Files (*.cdb)|*.cdb|" +
        "All Files (*.*)|*.*";

    private const string SnapshotFileFilter =
        "Wartales Snapshot Files (*.json)|*.json|" +
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

    private readonly ModProfileLibraryService
        modProfileLibraryService;

    private readonly ModProfileWorkflowService
        modProfileWorkflowService;

    private readonly ReferenceDataService referenceDataService;

    private readonly ValidationWorkflowService
        validationWorkflowService;

    private readonly ValidationPresentationService
        validationPresentationService;

    private readonly ProjectOperationService
        projectOperationService;

    private readonly ProjectOperationTransactionService
        projectOperationTransactionService;

    private readonly AddCampFacilitiesOperation
        addCampFacilitiesOperation;

    private readonly UpgradeAllEquipmentOperation
        upgradeAllEquipmentOperation;

    private readonly ProgressionScalingService
        progressionScalingService;

    private readonly GameplayOperationStateService
        gameplayOperationStateService;

    private readonly StartingResourcesService
        startingResourcesService;

    private readonly PartyEconomyService partyEconomyService;

    private readonly OverworldMovementSpeedService
        overworldMovementSpeedService;

    private readonly RainFrequencyService rainFrequencyService;

    private readonly IFileDialogService fileDialogService;

    private readonly IMessageDialogService messageDialogService;

    private readonly HashSet<PropertyModel> trackedProperties =
        new();

    private ChangeSummaryWindow? changeSummaryWindow;

    private ChangeSummaryViewModel? changeSummaryViewModel;

    private ProfileManagerWindow? profileManagerWindow;

    private ProfileManagerViewModel? profileManagerViewModel;

    private ValidationResultsWindow?
        validationResultsWindow;

    private ValidationResultsViewModel?
        validationResultsViewModel;

    private ProgressionScalingDialog?
        progressionScalingDialog;

    private StartingResourcesDialog?
        startingResourcesDialog;

    private readonly Dictionary<ProgressionType, PartyEconomyDialog>
        partyEconomyDialogs = new();

    private OverworldMovementSpeedDialog?
        overworldMovementSpeedDialog;

    private RainFrequencyDialog? rainFrequencyDialog;

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

            progressionScalingDialog?.Close();
            startingResourcesDialog?.Close();
            foreach (PartyEconomyDialog dialog in partyEconomyDialogs.Values.ToArray())
                dialog.Close();
            overworldMovementSpeedDialog?.Close();
            rainFrequencyDialog?.Close();

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
            RefreshProfileManagerProjectState();
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

            RefreshSearchResults();
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
                    : Path.GetFileName(
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

    public RelayCommand ShowProfileManagerCommand
    {
        get;
    }

    public RelayCommand ExportSnapshotCommand
    {
        get;
    }

    public RelayCommand PreviewSnapshotCommand
    {
        get;
    }

    public RelayCommand ImportSnapshotCommand
    {
        get;
    }

    public RelayCommand ValidateProjectCommand
    {
        get;
    }

    public RelayCommand ContentCreationCommand
    {
        get;
    }

    public RelayCommand GameplayProgressionCommand
    {
        get;
    }

    public RelayCommand StartingResourcesCommand
    {
        get;
    }

    public RelayCommand PartyEconomyCommand { get; }

    public RelayCommand OverworldMovementSpeedCommand { get; }

    public RelayCommand RainFrequencyCommand { get; }

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
    ModProfileLibraryService
        modProfileLibraryService,
    ModProfileWorkflowService
        modProfileWorkflowService,
    ReferenceDataService referenceDataService,
    ValidationWorkflowService
        validationWorkflowService,
    ValidationPresentationService
        validationPresentationService,
    ProjectOperationService projectOperationService,
    ProjectOperationTransactionService
        projectOperationTransactionService,
    AddCampFacilitiesOperation addCampFacilitiesOperation,
    UpgradeAllEquipmentOperation upgradeAllEquipmentOperation,
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

        this.modProfileLibraryService =
            modProfileLibraryService
            ?? throw new ArgumentNullException(
                nameof(modProfileLibraryService));

        this.modProfileWorkflowService =
            modProfileWorkflowService
            ?? throw new ArgumentNullException(
                nameof(modProfileWorkflowService));

        this.referenceDataService =
            referenceDataService
            ?? throw new ArgumentNullException(
                nameof(referenceDataService));

        this.validationWorkflowService =
            validationWorkflowService
            ?? throw new ArgumentNullException(
                nameof(validationWorkflowService));

        this.validationPresentationService =
            validationPresentationService
            ?? throw new ArgumentNullException(
                nameof(validationPresentationService));

        this.projectOperationService =
            projectOperationService
            ?? throw new ArgumentNullException(
                nameof(projectOperationService));

        this.projectOperationTransactionService =
            projectOperationTransactionService
            ?? throw new ArgumentNullException(
                nameof(
                    projectOperationTransactionService));

        this.addCampFacilitiesOperation =
            addCampFacilitiesOperation
            ?? throw new ArgumentNullException(
                nameof(addCampFacilitiesOperation));

        this.upgradeAllEquipmentOperation =
            upgradeAllEquipmentOperation
            ?? throw new ArgumentNullException(
                nameof(upgradeAllEquipmentOperation));

        this.fileDialogService =
            fileDialogService
            ?? throw new ArgumentNullException(
                nameof(fileDialogService));

        this.messageDialogService =
            messageDialogService
            ?? throw new ArgumentNullException(
                nameof(messageDialogService));

        ProjectMutationService progressionMutationService =
            new();

        gameplayOperationStateService =
            new GameplayOperationStateService(
                progressionMutationService);

        progressionScalingService =
            new ProgressionScalingService(
                progressionMutationService,
                gameplayOperationStateService);

        startingResourcesService =
            new StartingResourcesService(
                progressionMutationService,
                gameplayOperationStateService);

        partyEconomyService =
            new PartyEconomyService(
                progressionMutationService,
                gameplayOperationStateService);

        overworldMovementSpeedService =
            new OverworldMovementSpeedService(
                progressionMutationService,
                gameplayOperationStateService);

        rainFrequencyService =
            new RainFrequencyService(
                progressionMutationService,
                gameplayOperationStateService);

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

        ShowProfileManagerCommand =
            new RelayCommand(
                _ => ShowProfileManager());

        ExportSnapshotCommand =
            new RelayCommand(
                _ => ExportSnapshot(),
                _ => Project != null);

        PreviewSnapshotCommand =
            new RelayCommand(
                _ => PreviewSnapshot(),
                _ => Project != null);

        ImportSnapshotCommand =
            new RelayCommand(
                _ => ImportSnapshot(),
                _ => Project != null);

        ValidateProjectCommand =
            new RelayCommand(
                _ => ValidateProject(),
                _ => Project != null);

        ContentCreationCommand =
            new RelayCommand(
                ExecuteContentCreation,
                _ => Project != null);

        GameplayProgressionCommand =
            new RelayCommand(
                ExecuteGameplayProgression,
                _ => Project != null);

        StartingResourcesCommand =
            new RelayCommand(
                ExecuteStartingResources,
                _ => Project != null);

        PartyEconomyCommand =
            new RelayCommand(
                ExecutePartyEconomy,
                parameter => Project != null && parameter is ProgressionType);

        OverworldMovementSpeedCommand =
            new RelayCommand(
                ExecuteOverworldMovementSpeed,
                _ => Project != null);

        RainFrequencyCommand =
            new RelayCommand(
                ExecuteRainFrequency,
                _ => Project != null);
    }

    private void OpenProject()
    {
        string? fileName =
            fileDialogService.ShowOpenFileDialog(
                ProjectOpenFilter);

        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            return;
        }

        if (!ConfirmAbandonUnsavedChanges())
        {
            return;
        }

        try
        {
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
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "export_en.xml");

            if (File.Exists(
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
                $"{Path.GetFileName(CurrentFile)}";
        }
        catch (Exception exception)
        {
            messageDialogService.ShowError(
                $"The project could not be opened." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Open Project");

            Status =
                "Project open failed.";
        }
    }

    private bool SaveProject()
    {
        if (Project == null)
        {
            return false;
        }

        try
        {
            ValidationResultModel validationResult =
                validationWorkflowService
                    .ValidateForSave(Project);

            ValidationPresentationModel
                validationPresentation =
                    validationPresentationService
                        .BuildPresentation(
                            validationResult,
                            "Save");

            if (validationResult.HasErrors)
            {
                messageDialogService.ShowError(
                    validationPresentation.Summary,
                    validationPresentation.Title);

                Status =
                    "Save blocked by validation errors.";

                return false;
            }

            if (validationResult.HasWarnings)
            {
                messageDialogService.ShowWarning(
                    validationPresentation.Summary,
                    validationPresentation.Title);
            }

            string initialFileName =
                string.IsNullOrWhiteSpace(
                    CurrentFile)
                    ? "data.cdb"
                    : Path.GetFileName(
                        CurrentFile);

            string? fileName =
                fileDialogService.ShowSaveFileDialog(
                    ProjectSaveFilter,
                    initialFileName);

            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                Status =
                    "Save cancelled.";

                return false;
            }

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
                validationResult.HasWarnings
                    ? $"Saved with validation warnings: " +
                      $"{Path.GetFileName(fileName)}"
                    : $"Saved: " +
                      $"{Path.GetFileName(fileName)}";

            return true;
        }
        catch (Exception exception)
        {
            messageDialogService.ShowError(
                $"The project could not be saved." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Save Project");

            Status =
                "Project save failed.";

            return false;
        }
    }

    public bool ConfirmAbandonUnsavedChanges()
    {
        if (!HasModifications)
        {
            return true;
        }

        string fileName =
            string.IsNullOrWhiteSpace(
                CurrentFile)
                ? "the current project"
                : Path.GetFileName(
                    CurrentFile);

        UnsavedChangesResult result =
            messageDialogService.ShowUnsavedChanges(
                $"There are unsaved changes in {fileName}." +
                Environment.NewLine +
                Environment.NewLine +
                "Choose Yes to save before continuing, " +
                "No to discard the changes, or Cancel " +
                "to return to the editor.",
                "Unsaved Changes");

        return result switch
        {
            UnsavedChangesResult.Save =>
                SaveProject(),

            UnsavedChangesResult.Discard =>
                true,

            _ =>
                false
        };
    }

    private void ExportSnapshot()
    {
        if (Project == null)
            return;

        string initialFileName =
            BuildDefaultSnapshotFileName();

        string? fileName =
            fileDialogService.ShowSaveFileDialog(
                SnapshotFileFilter,
                initialFileName);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        try
        {
            ModificationSnapshotExportResultModel result =
                modificationSnapshotWorkflowService.Export(
                    Project,
                    fileName,
                    ApplicationVersion);

            string message =
                BuildExportSnapshotSummary(
                    result);

            messageDialogService.ShowInformation(
                message,
                "Snapshot Exported");

            Status =
                $"Snapshot exported: " +
                $"{Path.GetFileName(result.FileName)}";
        }
        catch (Exception exception)
        {
            messageDialogService.ShowError(
                $"The snapshot could not be exported." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Export Snapshot");

            Status =
                "Snapshot export failed.";
        }
    }

    private void PreviewSnapshot()
    {
        if (Project == null)
            return;

        string? fileName =
            fileDialogService.ShowOpenFileDialog(
                SnapshotFileFilter);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        try
        {
            ModificationSnapshotModel snapshot =
                modificationSnapshotWorkflowService.Load(
                    fileName);

            ModificationPreviewResultModel result =
                modificationSnapshotWorkflowService.Preview(
                    Project,
                    snapshot);

            string message =
                BuildPreviewSnapshotSummary(
                    result,
                    fileName);

            if (result.HasConflicts ||
                result.HasUnmatchedItems ||
                result.HasInvalidSnapshotChanges)
            {
                messageDialogService.ShowWarning(
                    message,
                    "Snapshot Preview");
            }
            else
            {
                messageDialogService.ShowInformation(
                    message,
                    "Snapshot Preview");
            }

            Status =
                $"Snapshot previewed: " +
                $"{Path.GetFileName(fileName)}";
        }
        catch (Exception exception)
        {
            messageDialogService.ShowError(
                $"The snapshot could not be previewed." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Preview Snapshot");

            Status =
                "Snapshot preview failed.";
        }
    }

    private void ImportSnapshot()
    {
        if (Project == null)
            return;

        string? fileName =
            fileDialogService.ShowOpenFileDialog(
                SnapshotFileFilter);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        try
        {
            ModificationSnapshotImportResultModel result =
                modificationSnapshotWorkflowService
                    .ImportAndApplySafely(
                        Project,
                        fileName);

            RefreshModificationState();
            RefreshChangeSummaryViewModel();
            RefreshSearchResults();
            RefreshHistoryState();
            RefreshCommandStates();

            string message =
                BuildImportSnapshotSummary(
                    result);

            if (result.HasConflicts ||
                result.HasUnmatchedItems ||
                result.HasFailures)
            {
                messageDialogService.ShowWarning(
                    message,
                    "Snapshot Import Complete");
            }
            else
            {
                messageDialogService.ShowInformation(
                    message,
                    "Snapshot Import Complete");
            }

            Status =
                result.HasAppliedChanges
                    ? $"Snapshot imported: " +
                      $"{result.AppliedCount:N0} " +
                      $"{GetSingularOrPlural(
                          result.AppliedCount +
                              result.OperationsAppliedCount,
                          "change",
                          "changes")} applied"
                    : "Snapshot imported: " +
                      "no changes were required.";
        }
        catch (Exception exception)
        {
            RefreshAfterProjectOperation();

            messageDialogService.ShowError(
                $"The snapshot could not be imported." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Import Snapshot");

            Status =
                "Snapshot import failed.";
        }
    }

    private void ExecuteContentCreation(
    object? parameter)
    {
        if (Project == null)
        {
            return;
        }

        if (parameter is not ContentCreationOperation
            operation)
        {
            messageDialogService.ShowError(
                "The requested content creation operation " +
                "was not recognized.",
                "Content Creation");

            Status =
                "Content creation operation was not recognized.";

            return;
        }

        switch (operation)
        {
            case ContentCreationOperation.AddCampFacilities:
                ExecuteAddCampFacilities();
                break;

            case ContentCreationOperation.UpgradeAllEquipment:
                ExecuteUpgradeAllEquipment();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(parameter),
                    operation,
                    "The content creation operation is not supported.");
        }
    }

    private void ExecuteAddCampFacilities()
    {
        if (Project == null)
        {
            return;
        }

        bool confirmed =
            messageDialogService.ShowConfirmation(
                "Add and enable the Anvil and Apothecary Table?" +
                Environment.NewLine +
                Environment.NewLine +
                "This will configure both camp facilities and add " +
                "their Workshop crafting recipes.",
                "Add Camp Facilities");

        if (!confirmed)
        {
            Status =
                "Add Camp Facilities cancelled.";

            return;
        }

        try
        {
            ProjectOperationResult operationResult;

            using (editHistoryService.SuppressRecording())
            {
                operationResult =
                    projectOperationService.Execute(
                        addCampFacilitiesOperation,
                        Project);
            }

            if (!operationResult.Succeeded)
            {
                RefreshAfterProjectOperation();

                messageDialogService.ShowError(
                    operationResult.Message
                    ?? "The operation failed validation.",
                    "Add Camp Facilities");

                Status =
                    "Add Camp Facilities failed validation.";

                return;
            }

            ProjectMutationResult result =
                operationResult.MutationResult;

            if (result.WasModified)
            {
                editHistoryService.Record(
                    new ProjectOperationHistoryAction(
                        addCampFacilitiesOperation.Name,
                        result,
                        projectOperationTransactionService));
            }

            RefreshAfterProjectOperation();

            int createdEntryCount =
                result.CreatedEntries.Count;

            int createdPropertyCount =
                result.CreatedProperties.Count;

            int updatedPropertyCount =
                result.UpdatedProperties.Count;

            if (!result.WasModified)
            {
                messageDialogService.ShowInformation(
                    "The Anvil and Apothecary Table are already " +
                    "configured. No changes were required.",
                    "Add Camp Facilities");

                Status =
                    "Camp facilities already configured.";

                return;
            }

            messageDialogService.ShowInformation(
                "Camp facilities were added successfully." +
                Environment.NewLine +
                Environment.NewLine +
                $"Created entries: {createdEntryCount:N0}" +
                Environment.NewLine +
                $"Created properties: {createdPropertyCount:N0}" +
                Environment.NewLine +
                $"Updated properties: {updatedPropertyCount:N0}" +
                Environment.NewLine +
                Environment.NewLine +
                "Save the project to write these changes to a CDB file.",
                "Add Camp Facilities");

            Status =
                "Camp facilities added successfully.";
        }
        catch (Exception exception)
        {
            RefreshAfterProjectOperation();

            messageDialogService.ShowError(
                "The camp facilities could not be added." +
                Environment.NewLine +
                Environment.NewLine +
                exception.Message,
                "Add Camp Facilities");

            Status =
                "Add Camp Facilities failed.";
        }
    }

    private void ExecuteUpgradeAllEquipment()
    {
        if (Project == null)
        {
            return;
        }

        bool confirmed =
            messageDialogService.ShowConfirmation(
                "Make normal obtainable equipment upgradeable " +
                "at the Brotherhood Training Grounds?" +
                Environment.NewLine +
                Environment.NewLine +
                "This modifies only the upgradeable flag. " +
                "Item stats, levels, prices, rarity, and all " +
                "other values will remain unchanged.",
                "Upgrade All Equipment");

        if (!confirmed)
        {
            Status =
                "Upgrade All Equipment cancelled.";

            return;
        }

        try
        {
            ProjectOperationResult operationResult;

            using (editHistoryService.SuppressRecording())
            {
                operationResult =
                    projectOperationService.Execute(
                        upgradeAllEquipmentOperation,
                        Project);
            }

            if (!operationResult.Succeeded)
            {
                RefreshAfterProjectOperation();

                messageDialogService.ShowError(
                    operationResult.Message
                    ?? "The operation failed validation.",
                    "Upgrade All Equipment");

                Status =
                    "Upgrade All Equipment failed validation.";

                return;
            }

            ProjectMutationResult result =
                operationResult.MutationResult;

            if (result.WasModified)
            {
                editHistoryService.Record(
                    new ProjectOperationHistoryAction(
                        upgradeAllEquipmentOperation.Name,
                        result,
                        projectOperationTransactionService));
            }

            RefreshAfterProjectOperation();

            int affectedEquipmentCount =
                result.UpdatedProperties.Count +
                result.CreatedProperties.Count;

            if (!result.WasModified)
            {
                messageDialogService.ShowInformation(
                    "All eligible equipment is already upgradeable. " +
                    "No changes were required.",
                    "Upgrade All Equipment");

                Status =
                    "All eligible equipment already upgradeable.";

                return;
            }

            messageDialogService.ShowInformation(
                "Eligible equipment was made upgradeable " +
                "successfully." +
                Environment.NewLine +
                Environment.NewLine +
                $"Updated equipment entries: " +
                $"{affectedEquipmentCount:N0}" +
                Environment.NewLine +
                Environment.NewLine +
                "Only the upgradeable flag was changed. " +
                "Save the project to write these changes " +
                "to a CDB file.",
                "Upgrade All Equipment");

            Status =
                "Eligible equipment made upgradeable.";
        }
        catch (Exception exception)
        {
            RefreshAfterProjectOperation();

            messageDialogService.ShowError(
                "Equipment could not be made upgradeable." +
                Environment.NewLine +
                Environment.NewLine +
                exception.Message,
                "Upgrade All Equipment");

            Status =
                "Upgrade All Equipment failed.";
        }
    }

    private void ExecuteGameplayProgression(
        object? parameter)
    {
        if (Project == null)
        {
            return;
        }

        if (progressionScalingDialog != null)
        {
            if (progressionScalingDialog.WindowState ==
                WindowState.Minimized)
            {
                progressionScalingDialog.WindowState =
                    WindowState.Normal;
            }

            progressionScalingDialog.Activate();
            return;
        }

        ProgressionScalingDialog? dialog =
            null;

        try
        {
            Trace.WriteLine(
                "XP Progression: command invoked; before construction.");

            Window? owner =
                Application.Current?.Windows
                    .OfType<Window>()
                    .FirstOrDefault(window =>
                        window.IsActive &&
                        window is MainWindow)
                ?? Application.Current?.MainWindow;

            if (owner == null)
            {
                throw new InvalidOperationException(
                    "The main application window is not available.");
            }

            ProgressionScalingDialogViewModel dialogViewModel =
                new(
                    Project,
                    progressionScalingService,
                    gameplayOperationStateService);

            dialog =
                new()
                {
                    Owner = owner,
                    DataContext = dialogViewModel,
                    WindowStartupLocation =
                        WindowStartupLocation.CenterOwner
                };

            dialog.ApplyRequested +=
                OnProgressionApplyRequested;

            dialog.BaselineAdoptionRequested +=
                OnProgressionBaselineAdoptionRequested;

            dialog.DisplayFailed +=
                OnProgressionDialogDisplayFailed;

            dialog.Closed +=
                OnProgressionDialogClosed;

            Trace.WriteLine(
                "XP Progression: after construction; before Show.");

            dialog.Show();

            progressionScalingDialog = dialog;

            Trace.WriteLine(
                "XP Progression: Show returned successfully.");

            Status = "XP Progression opened.";
        }
        catch (Exception exception)
        {
            Trace.WriteLine(
                "XP Progression: construction or Show failed: " +
                exception);

            if (dialog != null)
            {
                dialog.Close();
            }

            progressionScalingDialog = null;

            messageDialogService.ShowError(
                "XP Progression could not be opened." +
                Environment.NewLine +
                Environment.NewLine +
                exception.Message,
                "XP Progression");

            Status = "XP Progression failed to open.";
        }
        finally
        {
            Trace.WriteLine(
                "XP Progression: command handler finally path.");
        }
    }

    private void OnProgressionApplyRequested(
        object? sender,
        ProgressionApplyRequestedEventArgs e)
    {
        ExecuteProgressionOperation(
            e.ProgressionType,
            e.Percentage);

        if (sender is ProgressionScalingDialog dialog &&
            dialog.DataContext is
                ProgressionScalingDialogViewModel viewModel)
        {
            viewModel.RefreshFromProject();
        }
    }

    private void OnProgressionBaselineAdoptionRequested(
        object? sender,
        ProgressionBaselineAdoptionRequestedEventArgs e)
    {
        if (Project == null)
        {
            return;
        }

        string displayName =
            e.ProgressionType == ProgressionType.Character
                ? "Character XP"
                : "Profession XP";

        bool confirmed =
            messageDialogService.ShowConfirmation(
                $"The editor has no trusted {displayName} baseline " +
                "metadata for this CDB." +
                Environment.NewLine +
                Environment.NewLine +
                "The current values may already be modified. " +
                "Adopting them makes the current values the new " +
                "100% baseline and cannot reconstruct an earlier " +
                "clean baseline." +
                Environment.NewLine +
                Environment.NewLine +
                "Use the current values as the baseline?",
                $"Adopt {displayName} Baseline");

        if (!confirmed)
        {
            return;
        }

        GameplayOperationStateModel? previousState =
            gameplayOperationStateService.FindState(
                Project,
                e.ProgressionType)
                ?.DeepClone();

        bool previousStateWasModified =
            Project.IsGameplayOperationStateModified;

        try
        {
            gameplayOperationStateService.AdoptCurrentBaseline(
                Project,
                e.ProgressionType);

            jsonDataService.SaveGameplayOperationState(Project);

            if (sender is ProgressionScalingDialog dialog &&
                dialog.DataContext is
                    ProgressionScalingDialogViewModel viewModel)
            {
                viewModel.RefreshFromProject();
            }

            Status = $"{displayName} baseline adopted and saved.";
        }
        catch (Exception exception)
        {
            gameplayOperationStateService.RemoveState(
                Project,
                e.ProgressionType);

            if (previousState != null)
            {
                gameplayOperationStateService.ReplaceState(
                    Project,
                    previousState,
                    markModified: false);
            }

            Project.IsGameplayOperationStateModified =
                previousStateWasModified;

            messageDialogService.ShowError(
                $"The {displayName} baseline could not be adopted." +
                Environment.NewLine +
                Environment.NewLine +
                exception.Message,
                $"Adopt {displayName} Baseline");
        }
    }

    private void OnProgressionDialogDisplayFailed(
        Exception exception)
    {
        messageDialogService.ShowError(
            "XP Progression failed while loading or rendering." +
            Environment.NewLine +
            Environment.NewLine +
            exception.Message,
            "XP Progression");

        Status = "XP Progression failed to render.";
    }

    private void OnProgressionDialogClosed(
        object? sender,
        EventArgs e)
    {
        if (sender is ProgressionScalingDialog dialog)
        {
            dialog.ApplyRequested -=
                OnProgressionApplyRequested;

            dialog.BaselineAdoptionRequested -=
                OnProgressionBaselineAdoptionRequested;

            dialog.DisplayFailed -=
                OnProgressionDialogDisplayFailed;

            dialog.Closed -=
                OnProgressionDialogClosed;

            if (ReferenceEquals(
                    progressionScalingDialog,
                    dialog))
            {
                progressionScalingDialog = null;
            }
        }

        Trace.WriteLine(
            "XP Progression: Closed handler cleared tracking.");
    }

    private void ExecuteProgressionOperation(
        ProgressionType progressionType,
        int percentage)
    {
        if (Project == null)
        {
            return;
        }

        IProjectOperation operation =
            progressionType switch
            {
                ProgressionType.Character =>
                    new CharacterXpRequirementsOperation(
                        progressionScalingService,
                        percentage),

                ProgressionType.Profession =>
                    new ProfessionXpRequirementsOperation(
                        progressionScalingService,
                        percentage),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(progressionType),
                    progressionType,
                    "The progression type is not supported.")
            };

        try
        {
            ProjectOperationResult operationResult;

            using (editHistoryService.SuppressRecording())
            {
                operationResult =
                    projectOperationService.Execute(
                        operation,
                        Project);
            }

            if (!operationResult.Succeeded)
            {
                RefreshAfterProjectOperation();

                messageDialogService.ShowError(
                    operationResult.Message
                    ?? "The operation failed validation.",
                    operation.Name);

                Status = $"{operation.Name} failed validation.";
                return;
            }

            ProjectMutationResult result =
                operationResult.MutationResult;

            if (result.WasModified)
            {
                editHistoryService.Record(
                    new ProjectOperationHistoryAction(
                        operation.Name,
                        result,
                        projectOperationTransactionService));
            }

            RefreshAfterProjectOperation();

            messageDialogService.ShowInformation(
                operationResult.Message
                ?? $"{operation.Name} completed.",
                operation.Name);

            Status = result.WasModified
                ? $"{operation.Name} set to {percentage}%."
                : $"{operation.Name} already matched {percentage}%.";
        }
        catch (Exception exception)
        {
            RefreshAfterProjectOperation();

            messageDialogService.ShowError(
                $"{operation.Name} could not be applied." +
                Environment.NewLine +
                Environment.NewLine +
                exception.Message,
                operation.Name);

            Status = $"{operation.Name} failed.";
        }
    }

    private void ExecuteStartingResources(object? parameter)
    {
        if (Project == null) return;
        if (startingResourcesDialog != null)
        {
            if (startingResourcesDialog.WindowState == WindowState.Minimized)
                startingResourcesDialog.WindowState = WindowState.Normal;
            startingResourcesDialog.Activate();
            return;
        }

        StartingResourcesDialog? dialog = null;
        try
        {
            Window owner = Application.Current?.Windows.OfType<Window>()
                .FirstOrDefault(window => window.IsActive && window is MainWindow)
                ?? Application.Current?.MainWindow
                ?? throw new InvalidOperationException("The main application window is not available.");
            StartingResourcesDialogViewModel viewModel =
                new(Project, gameplayOperationStateService);
            dialog = new StartingResourcesDialog
            {
                Owner = owner,
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.InitializeRequested += OnStartingResourcesInitializeRequested;
            dialog.ApplyRequested += OnStartingResourcesApplyRequested;
            dialog.DisplayFailed += OnStartingResourcesDisplayFailed;
            dialog.Closed += OnStartingResourcesClosed;
            dialog.Show();
            startingResourcesDialog = dialog;
            Status = "Starting Resources opened.";
        }
        catch (Exception exception)
        {
            dialog?.Close();
            startingResourcesDialog = null;
            messageDialogService.ShowError(
                "Starting Resources could not be opened." + Environment.NewLine +
                Environment.NewLine + exception.Message,
                "Starting Resources");
            Status = "Starting Resources failed to open.";
        }
    }

    private void OnStartingResourcesInitializeRequested(object? sender, EventArgs e)
    {
        if (Project == null) return;
        bool confirmed = messageDialogService.ShowConfirmation(
            "The editor will remember the current starting supplies so future adjustments remain accurate." +
            Environment.NewLine + Environment.NewLine + "Continue?",
            "Initialize Starting Resources");
        if (!confirmed) return;

        GameplayOperationStateModel? previous = gameplayOperationStateService.FindState(
            Project, ProgressionType.StartingResources)?.DeepClone();
        bool previousModified = Project.IsGameplayOperationStateModified;
        try
        {
            startingResourcesService.Initialize(Project);
            jsonDataService.SaveGameplayOperationState(Project);
            if (sender is StartingResourcesDialog dialog &&
                dialog.DataContext is StartingResourcesDialogViewModel vm)
            {
                vm.RefreshFromProject(useFirstUseDefaults: true);
            }
            RefreshModificationState();
            Status = "Starting Resources initialized.";
        }
        catch (Exception exception)
        {
            gameplayOperationStateService.RemoveState(
                Project, ProgressionType.StartingResources, markModified: false);
            if (previous != null)
                gameplayOperationStateService.ReplaceState(Project, previous, markModified: false);
            Project.IsGameplayOperationStateModified = previousModified;
            messageDialogService.ShowError(
                "Starting Resources could not be initialized." + Environment.NewLine +
                Environment.NewLine + exception.Message,
                "Starting Resources");
        }
    }

    private void OnStartingResourcesApplyRequested(
        object? sender,
        StartingResourcesApplyEventArgs e)
    {
        if (Project == null) return;
        IProjectOperation operation = new StartingResourcesOperation(
            startingResourcesService,
            e.Settings);
        try
        {
            ProjectOperationResult operationResult;
            using (editHistoryService.SuppressRecording())
            {
                operationResult = projectOperationService.Execute(operation, Project);
            }
            if (!operationResult.Succeeded)
            {
                RefreshAfterProjectOperation();
                messageDialogService.ShowError(
                    operationResult.Message ?? "Starting Resources failed validation.",
                    operation.Name);
                return;
            }
            if (operationResult.MutationResult.WasModified)
            {
                editHistoryService.Record(new ProjectOperationHistoryAction(
                    operation.Name,
                    operationResult.MutationResult,
                    projectOperationTransactionService));
            }
            RefreshAfterProjectOperation();
            if (sender is StartingResourcesDialog dialog &&
                dialog.DataContext is StartingResourcesDialogViewModel vm)
            {
                vm.RefreshFromProject();
            }
            messageDialogService.ShowInformation(
                operationResult.Message ?? "Starting Resources completed.",
                operation.Name);
            Status = "Starting Resources updated.";
        }
        catch (Exception exception)
        {
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                "Starting Resources could not be applied." + Environment.NewLine +
                Environment.NewLine + exception.Message,
                operation.Name);
        }
    }

    private void OnStartingResourcesDisplayFailed(Exception exception)
    {
        messageDialogService.ShowError(
            "Starting Resources failed while loading or rendering." + Environment.NewLine +
            Environment.NewLine + exception.Message,
            "Starting Resources");
    }

    private void OnStartingResourcesClosed(object? sender, EventArgs e)
    {
        if (sender is not StartingResourcesDialog dialog) return;
        dialog.InitializeRequested -= OnStartingResourcesInitializeRequested;
        dialog.ApplyRequested -= OnStartingResourcesApplyRequested;
        dialog.DisplayFailed -= OnStartingResourcesDisplayFailed;
        dialog.Closed -= OnStartingResourcesClosed;
        if (ReferenceEquals(startingResourcesDialog, dialog))
            startingResourcesDialog = null;
    }

    private void ExecutePartyEconomy(object? parameter)
    {
        if (Project == null || parameter is not ProgressionType type) return;
        if (partyEconomyDialogs.TryGetValue(type, out PartyEconomyDialog? existing))
        {
            if (existing.WindowState == WindowState.Minimized)
                existing.WindowState = WindowState.Normal;
            existing.Activate();
            return;
        }

        PartyEconomyDialog? dialog = null;
        try
        {
            Window owner = Application.Current?.Windows.OfType<Window>()
                .FirstOrDefault(window => window.IsActive && window is MainWindow)
                ?? Application.Current?.MainWindow
                ?? throw new InvalidOperationException("The main application window is not available.");
            PartyEconomyDialogViewModel viewModel =
                new(Project, partyEconomyService, type);
            dialog = new PartyEconomyDialog
            {
                Owner = owner,
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.ApplyRequested += OnPartyEconomyApplyRequested;
            dialog.DisplayFailed += OnPartyEconomyDisplayFailed;
            dialog.Closed += OnPartyEconomyClosed;
            dialog.Show();
            partyEconomyDialogs[type] = dialog;
            Status = $"{viewModel.Title} opened.";
        }
        catch (Exception)
        {
            dialog?.Close();
            partyEconomyDialogs.Remove(type);
            messageDialogService.ShowError(
                "The gameplay tool could not be opened." + Environment.NewLine +
                Environment.NewLine + "The project was not changed.",
                "Gameplay Tools");
        }
    }

    private void OnPartyEconomyApplyRequested(object? sender, PartyEconomyApplyEventArgs e)
    {
        if (Project == null) return;
        IProjectOperation operation =
            new PartyEconomyOperation(partyEconomyService, e.OperationType, e.Settings);
        try
        {
            ProjectOperationResult result;
            using (editHistoryService.SuppressRecording())
                result = projectOperationService.Execute(operation, Project);
            if (!result.Succeeded)
            {
                RefreshAfterProjectOperation();
                messageDialogService.ShowError(
                    "The settings could not be applied." +
                    Environment.NewLine + Environment.NewLine +
                    "No changes were made.",
                    operation.Name);
                return;
            }
            if (result.MutationResult.WasModified)
                editHistoryService.Record(new ProjectOperationHistoryAction(
                    operation.Name, result.MutationResult, projectOperationTransactionService));
            RefreshAfterProjectOperation();
            if (sender is PartyEconomyDialog dialog &&
                dialog.DataContext is PartyEconomyDialogViewModel viewModel)
                viewModel.RefreshFromProject();
            messageDialogService.ShowInformation(
                result.Message ?? $"{operation.Name} completed.", operation.Name);
            Status = $"{operation.Name} updated.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                "The settings could not be applied." +
                Environment.NewLine + Environment.NewLine +
                "No changes were made.",
                operation.Name);
        }
    }

    private void OnPartyEconomyDisplayFailed(Exception exception)
    {
        Debug.WriteLine(exception);
        messageDialogService.ShowError(
            "The gameplay tool could not be displayed." +
            Environment.NewLine + Environment.NewLine +
            "The project was not changed.",
            "Gameplay Tools");
    }

    private void OnPartyEconomyClosed(object? sender, EventArgs e)
    {
        if (sender is not PartyEconomyDialog dialog) return;
        dialog.ApplyRequested -= OnPartyEconomyApplyRequested;
        dialog.DisplayFailed -= OnPartyEconomyDisplayFailed;
        dialog.Closed -= OnPartyEconomyClosed;
        ProgressionType? key = partyEconomyDialogs
            .Where(pair => ReferenceEquals(pair.Value, dialog))
            .Select(pair => (ProgressionType?)pair.Key)
            .FirstOrDefault();
        if (key.HasValue) partyEconomyDialogs.Remove(key.Value);
    }

    private void ExecuteOverworldMovementSpeed(object? parameter)
    {
        if (Project == null) return;
        if (overworldMovementSpeedDialog != null)
        {
            if (overworldMovementSpeedDialog.WindowState == WindowState.Minimized)
                overworldMovementSpeedDialog.WindowState = WindowState.Normal;
            overworldMovementSpeedDialog.Activate();
            return;
        }

        OverworldMovementSpeedDialog? dialog = null;
        try
        {
            Window owner = Application.Current?.Windows.OfType<Window>()
                .FirstOrDefault(window => window.IsActive && window is MainWindow)
                ?? Application.Current?.MainWindow
                ?? throw new InvalidOperationException(
                    "The main application window is not available.");
            OverworldMovementSpeedDialogViewModel viewModel =
                new(Project, overworldMovementSpeedService);
            dialog = new OverworldMovementSpeedDialog
            {
                Owner = owner,
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.ApplyRequested += OnOverworldMovementApplyRequested;
            dialog.DisplayFailed += OnOverworldMovementDisplayFailed;
            dialog.Closed += OnOverworldMovementClosed;
            dialog.Show();
            overworldMovementSpeedDialog = dialog;
            Status = "Overworld Movement Speed opened.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            dialog?.Close();
            overworldMovementSpeedDialog = null;
            messageDialogService.ShowError(
                "Overworld Movement Speed could not be opened." +
                Environment.NewLine + Environment.NewLine +
                "The project was not changed.",
                "Overworld Movement Speed");
        }
    }

    private void OnOverworldMovementApplyRequested(
        object? sender,
        OverworldMovementApplyEventArgs e)
    {
        if (Project == null) return;
        IProjectOperation operation =
            new OverworldMovementSpeedOperation(
                overworldMovementSpeedService,
                e.Preset);
        try
        {
            ProjectOperationResult result;
            using (editHistoryService.SuppressRecording())
                result = projectOperationService.Execute(operation, Project);
            if (!result.Succeeded)
            {
                RefreshAfterProjectOperation();
                messageDialogService.ShowError(
                    "The movement preset could not be applied." +
                    Environment.NewLine + Environment.NewLine +
                    "No changes were made.",
                    operation.Name);
                return;
            }

            if (result.MutationResult.WasModified)
                editHistoryService.Record(
                    new ProjectOperationHistoryAction(
                        operation.Name,
                        result.MutationResult,
                        projectOperationTransactionService));
            RefreshAfterProjectOperation();
            if (sender is OverworldMovementSpeedDialog dialog &&
                dialog.DataContext is
                    OverworldMovementSpeedDialogViewModel viewModel)
                viewModel.RefreshFromProject();
            messageDialogService.ShowInformation(
                result.Message ?? "Overworld Movement Speed was updated.",
                operation.Name);
            Status = "Overworld Movement Speed updated.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                "The movement preset could not be applied." +
                Environment.NewLine + Environment.NewLine +
                "No changes were made.",
                operation.Name);
        }
    }

    private void OnOverworldMovementDisplayFailed(Exception exception)
    {
        Debug.WriteLine(exception);
        messageDialogService.ShowError(
            "Overworld Movement Speed could not be displayed." +
            Environment.NewLine + Environment.NewLine +
            "The project was not changed.",
            "Overworld Movement Speed");
    }

    private void OnOverworldMovementClosed(object? sender, EventArgs e)
    {
        if (sender is not OverworldMovementSpeedDialog dialog) return;
        dialog.ApplyRequested -= OnOverworldMovementApplyRequested;
        dialog.DisplayFailed -= OnOverworldMovementDisplayFailed;
        dialog.Closed -= OnOverworldMovementClosed;
        if (ReferenceEquals(overworldMovementSpeedDialog, dialog))
            overworldMovementSpeedDialog = null;
    }

    private void ExecuteRainFrequency(object? parameter)
    {
        if (Project == null) return;
        if (rainFrequencyDialog != null)
        {
            if (rainFrequencyDialog.WindowState == WindowState.Minimized)
                rainFrequencyDialog.WindowState = WindowState.Normal;
            rainFrequencyDialog.Activate();
            return;
        }

        RainFrequencyDialog? dialog = null;
        try
        {
            Window owner = Application.Current?.Windows.OfType<Window>()
                .FirstOrDefault(window => window.IsActive && window is MainWindow)
                ?? Application.Current?.MainWindow
                ?? throw new InvalidOperationException(
                    "The main application window is not available.");
            RainFrequencyDialogViewModel viewModel =
                new(Project, rainFrequencyService);
            dialog = new RainFrequencyDialog
            {
                Owner = owner,
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.ApplyRequested += OnRainFrequencyApplyRequested;
            dialog.DisplayFailed += OnRainFrequencyDisplayFailed;
            dialog.Closed += OnRainFrequencyClosed;
            dialog.Show();
            rainFrequencyDialog = dialog;
            Status = "Rain Frequency opened.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            dialog?.Close();
            rainFrequencyDialog = null;
            messageDialogService.ShowError(
                "Rain Frequency could not be opened." +
                Environment.NewLine + Environment.NewLine +
                "The project was not changed.",
                "Rain Frequency");
        }
    }

    private void OnRainFrequencyApplyRequested(
        object? sender,
        RainFrequencyApplyEventArgs e)
    {
        if (Project == null) return;
        IProjectOperation operation =
            new RainFrequencyOperation(rainFrequencyService, e.Preset);
        try
        {
            ProjectOperationResult result;
            using (editHistoryService.SuppressRecording())
                result = projectOperationService.Execute(operation, Project);
            if (!result.Succeeded)
            {
                RefreshAfterProjectOperation();
                messageDialogService.ShowError(
                    "The rain preset could not be applied." +
                    Environment.NewLine + Environment.NewLine +
                    "No changes were made.",
                    operation.Name);
                return;
            }

            if (result.MutationResult.WasModified)
                editHistoryService.Record(
                    new ProjectOperationHistoryAction(
                        operation.Name,
                        result.MutationResult,
                        projectOperationTransactionService));
            RefreshAfterProjectOperation();
            if (sender is RainFrequencyDialog dialog &&
                dialog.DataContext is RainFrequencyDialogViewModel viewModel)
                viewModel.RefreshFromProject();
            messageDialogService.ShowInformation(
                result.Message ?? "Rain Frequency was updated.",
                operation.Name);
            Status = "Rain Frequency updated.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                "The rain preset could not be applied." +
                Environment.NewLine + Environment.NewLine +
                "No changes were made.",
                operation.Name);
        }
    }

    private void OnRainFrequencyDisplayFailed(Exception exception)
    {
        Debug.WriteLine(exception);
        messageDialogService.ShowError(
            "Rain Frequency could not be displayed." +
            Environment.NewLine + Environment.NewLine +
            "The project was not changed.",
            "Rain Frequency");
    }

    private void OnRainFrequencyClosed(object? sender, EventArgs e)
    {
        if (sender is not RainFrequencyDialog dialog) return;
        dialog.ApplyRequested -= OnRainFrequencyApplyRequested;
        dialog.DisplayFailed -= OnRainFrequencyDisplayFailed;
        dialog.Closed -= OnRainFrequencyClosed;
        if (ReferenceEquals(rainFrequencyDialog, dialog))
            rainFrequencyDialog = null;
    }

    private void ValidateProject()
    {
        if (Project == null)
        {
            return;
        }

        ValidationResultModel validationResult =
            validationWorkflowService
                .ValidateProject(Project);

        ShowValidationResults(
            validationResult);

        Status =
            validationResult.HasErrors
                ? "Validation completed with errors."
                : validationResult.HasWarnings
                    ? "Validation completed with warnings."
                    : "Validation completed successfully.";
    }

    private void RefreshAfterProjectOperation()
    {
        if (Project != null)
        {
            gameplayOperationStateService.ValidateProjectStates(
                Project);
        }

        StopTrackingProjectProperties();
        StartTrackingProjectProperties();

        OnPropertyChanged(nameof(Sheets));
        OnPropertyChanged(nameof(Entries));
        OnPropertyChanged(nameof(Properties));

        RefreshModificationState();
        RefreshChangeSummaryViewModel();
        RefreshSearchResults();
        RefreshHistoryState();
        RefreshCommandStates();
    }

    private void ShowValidationResults(
    ValidationResultModel validationResult)
    {
        ArgumentNullException.ThrowIfNull(
            validationResult);

        if (validationResultsWindow != null)
        {
            validationResultsViewModel?.Refresh(
                validationResult);

            if (validationResultsWindow.WindowState
                == WindowState.Minimized)
            {
                validationResultsWindow.WindowState =
                    WindowState.Normal;
            }

            validationResultsWindow.Activate();
            validationResultsWindow.Focus();

            return;
        }

        validationResultsViewModel =
            new ValidationResultsViewModel(
                validationResult,
                RerunProjectValidation,
                NavigateToValidationIssue,
                CopyValidationResults);

        validationResultsWindow =
            new ValidationResultsWindow
            {
                DataContext =
                    validationResultsViewModel
            };

        validationResultsWindow.Closed +=
            OnValidationResultsWindowClosed;

        validationResultsWindow.Show();
        validationResultsWindow.Activate();
    }

    private ValidationResultModel
        RerunProjectValidation()
    {
        if (Project == null)
        {
            return ValidationResultModel.Empty;
        }

        ValidationResultModel validationResult =
            validationWorkflowService
                .ValidateProject(Project);

        Status =
            validationResult.HasErrors
                ? "Validation completed with errors."
                : validationResult.HasWarnings
                    ? "Validation completed with warnings."
                    : "Validation completed successfully.";

        return validationResult;
    }

    private void CopyValidationResults(
        string resultsText)
    {
        if (string.IsNullOrWhiteSpace(
                resultsText))
        {
            return;
        }

        Clipboard.SetText(
            resultsText);

        Status =
            "Validation results copied to the clipboard.";
    }

    private void OnValidationResultsWindowClosed(
        object? sender,
        EventArgs e)
    {
        if (validationResultsWindow != null)
        {
            validationResultsWindow.Closed -=
                OnValidationResultsWindowClosed;
        }

        validationResultsWindow = null;
        validationResultsViewModel = null;
    }

    private string BuildDefaultSnapshotFileName()
    {
        string projectName =
            string.IsNullOrWhiteSpace(
                CurrentFile)
                ? "wartales"
                : Path.GetFileNameWithoutExtension(
                    CurrentFile);

        return
            $"{projectName}.snapshot.json";
    }

    private static string BuildExportSnapshotSummary(
        ModificationSnapshotExportResultModel result)
    {
        StringBuilder message =
            new();

        message.AppendLine(
            "The modification snapshot was exported successfully.");

        message.AppendLine();
        message.AppendLine(
            $"File: {Path.GetFileName(result.FileName)}");

        message.AppendLine(
            $"Categories: {result.CategoryCount:N0}");

        message.AppendLine(
            $"Settings: {result.SettingCount:N0}");

        message.AppendLine(
            $"Properties: {result.PropertyCount:N0}");

        if (!result.HasChanges)
        {
            message.AppendLine();
            message.Append(
                "The snapshot does not contain any " +
                "modified properties.");
        }

        return message.ToString();
    }

    private static string BuildPreviewSnapshotSummary(
        ModificationPreviewResultModel result,
        string fileName)
    {
        StringBuilder message =
            new();

        message.AppendLine(
            $"Snapshot: {Path.GetFileName(fileName)}");

        message.AppendLine();
        message.AppendLine(
            $"Total changes: {result.TotalCount:N0}");

        message.AppendLine(
            $"Safe to apply: " +
            $"{result.SafeToApplyCount:N0}");

        message.AppendLine(
            $"Already applied: " +
            $"{result.AlreadyAppliedCount:N0}");

        message.AppendLine(
            $"Conflicts: {result.ConflictCount:N0}");

        message.AppendLine(
            $"Not matched: " +
            $"{result.NotMatchedCount:N0}");

        message.AppendLine(
            $"Invalid snapshot changes: " +
            $"{result.InvalidSnapshotChangeCount:N0}");

        message.AppendLine();

        if (result.TotalCount == 0)
        {
            message.Append(
                "The snapshot does not contain any changes.");
        }
        else if (result.CanApplyWithoutConflicts)
        {
            message.Append(
                "This preview did not modify the project. " +
                "The snapshot can be imported without " +
                "unresolved conflicts.");
        }
        else
        {
            message.Append(
                "This preview did not modify the project. " +
                "Only changes considered safe by the " +
                "snapshot workflow will be applied during import.");
        }

        return message.ToString();
    }

    private static string BuildImportSnapshotSummary(
        ModificationSnapshotImportResultModel result)
    {
        StringBuilder message =
            new();

        if (result.OperationResults.Count > 0)
        {
            message.AppendLine("Gameplay tools");
            message.AppendLine(
                $"Applied: {result.OperationsAppliedCount:N0}");
            message.AppendLine(
                $"Already configured: " +
                $"{result.OperationsAlreadyConfiguredCount:N0}");
            message.AppendLine(
                $"Failed: {result.OperationsFailedCount:N0}");
            message.AppendLine();
            message.AppendLine("Property changes");
        }
        else
        {
            message.AppendLine(
                $"Snapshot: {Path.GetFileName(result.FileName)}");
            message.AppendLine();
        }

        message.AppendLine(
            $"Total changes: {result.TotalCount:N0}");

        message.AppendLine(
            $"Matched: {result.MatchedCount:N0}");

        message.AppendLine(
            $"Unmatched: {result.UnmatchedCount:N0}");

        message.AppendLine();
        message.AppendLine(
            $"Applied: {result.AppliedCount:N0}");

        message.AppendLine(
            $"No change required: " +
            $"{result.NoChangeRequiredCount:N0}");

        message.AppendLine(
            $"Conflicts: {result.ConflictCount:N0}");

        message.AppendLine(
            $"Invalid snapshot changes: " +
            $"{result.InvalidSnapshotChangeCount:N0}");

        message.AppendLine(
            $"Failed: {result.FailedCount:N0}");

        message.AppendLine();

        if (result.IsCompleteSuccess)
        {
            message.Append(
                "Every snapshot change was applied " +
                "or was already present.");
        }
        else if (result.HasAppliedChanges)
        {
            message.Append(
                "Safe changes were applied. Review the " +
                "conflict, unmatched, invalid, and failed " +
                "counts before saving the project.");
        }
        else
        {
            message.Append(
                "No new changes were applied. Review the " +
                "summary above for unresolved items.");
        }

        return message.ToString();
    }

    private static string BuildProfileApplySummary(
        ModificationSnapshotImportResultModel result)
    {
        StringBuilder message =
            new();

        int applied =
            result.AppliedEffectiveChangeCount;

        int alreadyPresent =
            result.AlreadyPresentEffectiveChangeCount;

        int unapplied =
            result.UnappliedEffectiveChangeCount;

        if (unapplied > 0)
        {
            message.AppendLine(
                $"{applied:N0} of " +
                $"{result.EffectiveChangeCount:N0} " +
                $"{GetSingularOrPlural(
                    result.EffectiveChangeCount,
                    "change",
                    "changes")} were applied.");

            message.AppendLine();
            message.Append(
                $"{unapplied:N0} " +
                $"{GetSingularOrPlural(
                    unapplied,
                    "change",
                    "changes")} could not be applied.");

            return message.ToString();
        }

        if (applied == 0)
        {
            message.AppendLine(
                "No changes were needed.");

            message.AppendLine();
            message.Append(
                "This profile is already applied.");

            return message.ToString();
        }

        message.AppendLine(
            $"{applied:N0} " +
            $"{GetSingularOrPlural(
                applied,
                "change",
                "changes")} applied.");

        message.AppendLine();

        if (alreadyPresent > 0)
        {
            message.AppendLine(
                $"{alreadyPresent:N0} " +
                $"{GetSingularOrPlural(
                    alreadyPresent,
                    "change was",
                    "changes were")} already present.");

            message.AppendLine();
            message.Append(
                "All profile changes are now active.");
        }
        else
        {
            message.Append(
                "Every profile change was applied successfully.");
        }

        return message.ToString();
    }

    private static string GetSingularOrPlural(
        int count,
        string singular,
        string plural)
    {
        return count == 1
            ? singular
            : plural;
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
        if (Project != null)
        {
            gameplayOperationStateService.ValidateProjectStates(
                Project);
        }

        int modifiedCount =
            trackedProperties.Count(
                property =>
                    property.IsModified);

        ModifiedPropertyCount =
            modifiedCount;

        bool projectIsModified =
            modifiedCount > 0 ||
            Project?.IsGameplayOperationStateModified == true;

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
        OpenCommand?.NotifyCanExecuteChanged();
        SaveCommand?.NotifyCanExecuteChanged();

        NavigateSearchResultCommand?
            .NotifyCanExecuteChanged();

        ResetSelectedPropertyCommand?
            .NotifyCanExecuteChanged();

        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();

        ShowChangeSummaryCommand?
            .NotifyCanExecuteChanged();

        ShowProfileManagerCommand?
            .NotifyCanExecuteChanged();

        ExportSnapshotCommand?
            .NotifyCanExecuteChanged();

        PreviewSnapshotCommand?
            .NotifyCanExecuteChanged();

        ImportSnapshotCommand?
            .NotifyCanExecuteChanged();

        ValidateProjectCommand?
            .NotifyCanExecuteChanged();

        ContentCreationCommand?
            .NotifyCanExecuteChanged();

        GameplayProgressionCommand?
            .NotifyCanExecuteChanged();

        StartingResourcesCommand?
            .NotifyCanExecuteChanged();

        PartyEconomyCommand?
            .NotifyCanExecuteChanged();

        OverworldMovementSpeedCommand?
            .NotifyCanExecuteChanged();

        RainFrequencyCommand?
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

    private void ShowProfileManager()
    {
        if (profileManagerWindow != null)
        {
            profileManagerViewModel?.Refresh();
            RefreshProfileManagerProjectState();

            if (profileManagerWindow.WindowState
                == WindowState.Minimized)
            {
                profileManagerWindow.WindowState =
                    WindowState.Normal;
            }

            profileManagerWindow.Activate();
            profileManagerWindow.Focus();
            return;
        }

        profileManagerViewModel =
            new ProfileManagerViewModel(
                modProfileLibraryService,
                fileDialogService,
                messageDialogService,
                ShowProfileDetailsDialog);

        profileManagerViewModel.OperationRequested +=
            OnProfileOperationRequested;

        RefreshProfileManagerProjectState();

        profileManagerWindow =
            new ProfileManagerWindow
            {
                DataContext =
                    profileManagerViewModel
            };

        profileManagerWindow.Closed +=
            OnProfileManagerWindowClosed;

        profileManagerWindow.Show();
        profileManagerWindow.Activate();

        Status =
            "Profile Manager opened.";
    }

    private bool? ShowProfileDetailsDialog(
        ProfileDetailsViewModel viewModel)
    {
        ProfileDetailsWindow window =
            new(viewModel);

        if (profileManagerWindow != null)
        {
            window.Owner =
                profileManagerWindow;
        }
        else
        {
            window.Owner =
                Application.Current.MainWindow;
        }

        return window.ShowDialog();
    }

    private void OnProfileOperationRequested(
        object? sender,
        ProfileManagerRequestModel request)
    {
        switch (request.Operation)
        {
            case ProfileManagerOperation.Create:
                CreateProfile(request);
                break;

            case ProfileManagerOperation.Apply:
                ApplyProfile(request);
                break;

            case ProfileManagerOperation.Rename:
                RenameProfile(request);
                break;

            case ProfileManagerOperation.Duplicate:
                DuplicateProfile(request);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request.Operation),
                    request.Operation,
                    "The profile manager operation is not supported.");
        }
    }

    private void CreateProfile(
        ProfileManagerRequestModel request)
    {
        if (Project == null)
        {
            messageDialogService.ShowWarning(
                "Open a project before creating a mod profile.",
                "Create Profile");

            Status =
                "Profile creation requires an open project.";

            return;
        }

        try
        {
            ModProfileModel profile =
                modProfileWorkflowService.CreateProfile(
                    Project,
                    request.ProfileName,
                    request.Description,
                    request.Author,
                    request.ProfileVersion,
                    ApplicationVersion);

            ModProfileSummaryModel createdProfile =
                modProfileLibraryService.AddProfile(
                    profile);

            profileManagerViewModel?.RefreshAndSelect(
                createdProfile.FilePath);

            Status =
                $"Created profile: {createdProfile.Name}";

            messageDialogService.ShowInformation(
                $"The profile was created successfully." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Profile: {createdProfile.Name}" +
                $"{Environment.NewLine}" +
                $"File: {createdProfile.FileName}",
                "Create Profile");
        }
        catch (Exception exception)
        {
            Status =
                "Profile creation failed.";

            messageDialogService.ShowError(
                $"The profile could not be created." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Create Profile");
        }
    }

    private void RenameProfile(
        ProfileManagerRequestModel request)
    {
        ModProfileSummaryModel? profile =
            request.Profile;

        if (profile == null)
        {
            return;
        }

        try
        {
            ModProfileSummaryModel renamedProfile =
                modProfileLibraryService.RenameProfile(
                    profile,
                    request.ProfileName,
                    request.Description,
                    request.Author,
                    request.ProfileVersion);

            profileManagerViewModel?.RefreshAndSelect(
                renamedProfile.FilePath);

            Status =
                $"Renamed profile: {renamedProfile.Name}";

            messageDialogService.ShowInformation(
                $"The profile was renamed successfully." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Profile: {renamedProfile.Name}" +
                $"{Environment.NewLine}" +
                $"File: {renamedProfile.FileName}",
                "Rename Profile");
        }
        catch (Exception exception)
        {
            Status =
                "Profile rename failed.";

            messageDialogService.ShowError(
                $"The profile could not be renamed." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Rename Profile");
        }
    }

    private void DuplicateProfile(
        ProfileManagerRequestModel request)
    {
        ModProfileSummaryModel? profile =
            request.Profile;

        if (profile == null)
        {
            return;
        }

        try
        {
            ModProfileSummaryModel duplicatedProfile =
                modProfileLibraryService.DuplicateProfile(
                    profile,
                    request.ProfileName,
                    request.Description,
                    request.Author,
                    request.ProfileVersion);

            profileManagerViewModel?.RefreshAndSelect(
                duplicatedProfile.FilePath);

            Status =
                $"Duplicated profile: {duplicatedProfile.Name}";

            messageDialogService.ShowInformation(
                $"The profile was duplicated successfully." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Profile: {duplicatedProfile.Name}" +
                $"{Environment.NewLine}" +
                $"File: {duplicatedProfile.FileName}",
                "Duplicate Profile");
        }
        catch (Exception exception)
        {
            Status =
                "Profile duplication failed.";

            messageDialogService.ShowError(
                $"The profile could not be duplicated." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Duplicate Profile");
        }
    }

    private void ApplyProfile(
        ProfileManagerRequestModel request)
    {
        ModProfileSummaryModel? profile =
            request.Profile;

        if (profile == null)
        {
            return;
        }

        OnProfileApplyRequested(
            this,
            profile);
    }

    private void RefreshProfileManagerProjectState()
    {
        if (profileManagerViewModel == null)
        {
            return;
        }

        profileManagerViewModel.CanApplyToCurrentProject =
            Project != null;
    }

    private void OnProfileApplyRequested(
        object? sender,
        ModProfileSummaryModel profile)
    {
        if (Project == null)
        {
            messageDialogService.ShowWarning(
                "Open a project before applying a mod profile.",
                "Apply Profile");

            Status =
                "Profile apply requires an open project.";

            return;
        }

        try
        {
            ModificationSnapshotImportResultModel result;

            using (editHistoryService.SuppressRecording())
            {
                result =
                    modProfileWorkflowService
                        .LoadAndApplyProfile(
                            Project,
                            profile.FilePath);
            }

            if (result.MutationResult.WasModified)
            {
                editHistoryService.Record(
                    new ProjectOperationHistoryAction(
                        $"Apply Profile: {profile.Name}",
                        result.MutationResult,
                        projectOperationTransactionService));
            }

            RefreshAfterProjectOperation();

            string message =
                BuildProfileApplySummary(
                    result);

            bool isIncomplete =
                result.UnappliedEffectiveChangeCount > 0;

            if (isIncomplete)
            {
                messageDialogService.ShowWarning(
                    message,
                    "Profile Apply Incomplete");
            }
            else
            {
                messageDialogService.ShowInformation(
                    message,
                    "Profile Apply Complete");
            }

            Status =
                result.MutationResult.WasModified
                    ? $"Profile applied: " +
                      $"{result.AppliedEffectiveChangeCount:N0} " +
                      $"{GetSingularOrPlural(
                          result.AppliedEffectiveChangeCount,
                          "change",
                          "changes")} applied"
                    : "Profile applied: " +
                      "no changes were required.";
        }
        catch (Exception exception)
        {
            messageDialogService.ShowError(
                $"The profile could not be applied." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Apply Profile");

            Status =
                "Profile apply failed.";

            RefreshAfterProjectOperation();
        }
    }

    private void OnProfileManagerWindowClosed(
        object? sender,
        EventArgs e)
    {
        if (profileManagerViewModel != null)
        {
            profileManagerViewModel.OperationRequested -=
                OnProfileOperationRequested;
        }

        if (profileManagerWindow != null)
        {
            profileManagerWindow.Closed -=
                OnProfileManagerWindowClosed;
        }

        profileManagerWindow = null;
        profileManagerViewModel = null;
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

    private void NavigateToValidationIssue(
        ValidationIssueModel issue)
    {
        ArgumentNullException.ThrowIfNull(
            issue);

        if (Project == null ||
            string.IsNullOrWhiteSpace(
                issue.SheetName))
        {
            return;
        }

        SheetModel? sheet =
            Project.Sheets.FirstOrDefault(
                candidate =>
                    string.Equals(
                        candidate.Name,
                        issue.SheetName,
                        StringComparison.Ordinal));

        if (sheet == null)
        {
            Status =
                "The validation issue location could not be found.";

            return;
        }

        EntryModel? entry =
            sheet.Entries.FirstOrDefault(
                candidate =>
                    string.Equals(
                        candidate.Id,
                        issue.EntryId,
                        StringComparison.Ordinal)
                    ||
                    string.Equals(
                        candidate.DisplayName,
                        issue.EntryName,
                        StringComparison.Ordinal));

        SelectedSheet =
            sheet;

        OnPropertyChanged(
            nameof(Entries));

        SelectedEntry =
            entry;

        if (entry != null &&
            !string.IsNullOrWhiteSpace(
                issue.PropertyName))
        {
            SelectedProperty =
                entry.Properties.FirstOrDefault(
                    property =>
                        string.Equals(
                            property.Name,
                            issue.PropertyName,
                            StringComparison.Ordinal));
        }

        RefreshCommandStates();

        Status =
            BuildValidationNavigationStatus(
                issue);

        Window? mainWindow =
            Application.Current.MainWindow;

        if (mainWindow == null)
        {
            return;
        }

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

    private static string
        BuildValidationNavigationStatus(
            ValidationIssueModel issue)
    {
        string location =
            string.Join(
                " → ",
                new[]
                {
                    issue.SheetName,
                    issue.EntryName
                    ?? issue.EntryId,
                    issue.PropertyName
                }
                .Where(value =>
                    !string.IsNullOrWhiteSpace(
                        value)));

        return string.IsNullOrWhiteSpace(
                location)
            ? "Validation issue selected."
            : $"Selected validation issue: {location}";
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
        {
            return;
        }

        RefreshAfterProjectOperation();

        Status =
            $"Undid: {description}";
    }

    private void Redo()
    {
        string description =
            editHistoryService.RedoDescription
            ?? "property change";

        if (!editHistoryService.Redo())
        {
            return;
        }

        RefreshAfterProjectOperation();

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
                     localizationService,
                     SearchScope))
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
