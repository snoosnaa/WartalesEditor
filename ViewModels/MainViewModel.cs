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
    private static string ApplicationVersion =>
        typeof(MainViewModel).Assembly
            .GetName()
            .Version?
            .ToString(3)
        ?? "Unknown";

    private const string ProjectOpenFilter =
        "Wartales Data Files (*.cdb)|*.cdb|" +
        "All Files (*.*)|*.*";

    private const string ProjectSaveFilter =
        "Wartales Data Files (*.cdb)|*.cdb|" +
        "All Files (*.*)|*.*";

    private const string SnapshotFileFilter =
        "Wartales Snapshot Files (*.json)|*.json|" +
        "All Files (*.*)|*.*";

    private const string LanguageDataFileFilter =
        "Wartales Export Language Data (export_*.xml)|export_*.xml|" +
        "XML Files (*.xml)|*.xml|" +
        "All Files (*.*)|*.*";

    private readonly JsonDataService jsonDataService;

    private readonly SearchService searchService;

    private readonly LocalizationService localizationService;

    private readonly LanguageDataService languageDataService;

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

    private readonly GameplayPresetService gameplayPresetService;

    private readonly RandomTraitExclusionsService
        randomTraitExclusionsService;

    private readonly IFileDialogService fileDialogService;

    private readonly IMessageDialogService messageDialogService;

    private QuickBmsImportService quickBmsImportService;

    private GoldenCdbService goldenCdbService;

    private GoldenCdbComparisonService goldenCdbComparisonService;

    private readonly QuickBmsImportOptions quickBmsImportOptions;

    private readonly WartalesInstallationService
        wartalesInstallationService;

    private readonly HashSet<PropertyModel> trackedProperties =
        new();

    private readonly EffectiveChangeCountService
        effectiveChangeCountService = new();

    private ChangeSummaryWindow? changeSummaryWindow;

    private ChangeSummaryViewModel? changeSummaryViewModel;

    private ProfileManagerWindow? profileManagerWindow;

    private ProfileManagerViewModel? profileManagerViewModel;

    private LanguageDataDialog? languageDataDialog;

    private LanguageDataDialogViewModel?
        languageDataDialogViewModel;

    private GoldenCdbWindow? goldenCdbWindow;

    private GoldenCdbViewModel? goldenCdbViewModel;

    private Action? projectPublicationFailureForTesting;

    private Action? saveValidationStartedForTesting;

    private ValidationResultsWindow?
        validationResultsWindow;

    private ValidationResultModel?
        lastValidationResult;

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

    private readonly Dictionary<ProgressionType, GameplayPresetDialog>
        gameplayPresetDialogs = new();

    private RandomTraitExclusionsDialog?
        randomTraitExclusionsDialog;

    private UpdateCompatibilityWindow?
        updateCompatibilityWindow;

    private ProjectModel? project;

    private bool isImportInProgress;

    private MainWorkspace activeWorkspace =
        MainWorkspace.GameplayTools;

    public MainWorkspace ActiveWorkspace
    {
        get => activeWorkspace;
        set
        {
            if (!SetProperty(
                    ref activeWorkspace,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(IsGameplayToolsWorkspace));
            OnPropertyChanged(
                nameof(IsDetailedEditorWorkspace));
            OnPropertyChanged(
                nameof(IsGameplayToolsAvailable));
            OnPropertyChanged(
                nameof(IsDetailedEditorAvailable));
        }
    }

    public bool IsGameplayToolsWorkspace =>
        ActiveWorkspace ==
        MainWorkspace.GameplayTools;

    public bool IsDetailedEditorWorkspace =>
        ActiveWorkspace ==
        MainWorkspace.DetailedEditor;

    public bool HasProject =>
        Project != null;

    public bool IsGameplayToolsAvailable =>
        HasProject &&
        IsGameplayToolsWorkspace;

    public bool IsDetailedEditorAvailable =>
        HasProject &&
        IsDetailedEditorWorkspace;

    public bool ShowWelcomeState =>
        !HasProject;

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

            validationResultsWindow?.Close();
            progressionScalingDialog?.Close();
            startingResourcesDialog?.Close();
            foreach (PartyEconomyDialog dialog in partyEconomyDialogs.Values.ToArray())
                dialog.Close();
            overworldMovementSpeedDialog?.Close();
            rainFrequencyDialog?.Close();
            foreach (GameplayPresetDialog dialog in gameplayPresetDialogs.Values.ToArray())
                dialog.Close();
            randomTraitExclusionsDialog?.Close();
            updateCompatibilityWindow?.Close();

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
                nameof(HasVisibleCategories));
            OnPropertyChanged(
                nameof(HasSelectedCategory));
            OnPropertyChanged(
                nameof(SelectedCategoryHasSettings));
            OnPropertyChanged(
                nameof(HasVisibleSettings));
            OnPropertyChanged(
                nameof(HasVisibleProperties));
            OnPropertyChanged(nameof(HasProject));
            OnPropertyChanged(nameof(IsGameplayToolsAvailable));
            OnPropertyChanged(nameof(IsDetailedEditorAvailable));
            OnPropertyChanged(nameof(ShowWelcomeState));
            OnPropertyChanged(
                nameof(FindAnythingHeader));

            RefreshModificationState();
            RefreshSearchResults();
            RefreshHistoryState();
            RefreshCommandStates();
            RefreshProfileManagerProjectState();
            RefreshGoldenCdbWindowState();
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
                OnPropertyChanged(
                    nameof(HasVisibleCategories));
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
            OnPropertyChanged(
                nameof(HasSelectedCategory));
            OnPropertyChanged(
                nameof(SelectedCategoryHasSettings));
            OnPropertyChanged(
                nameof(HasVisibleSettings));
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
                nameof(HasVisibleProperties));
            OnPropertyChanged(
                nameof(CanResetProperty));
            NotifySelectedSettingPresentationChanged();

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
            OnPropertyChanged(
                nameof(CanResetSelectedProperty));

            RefreshCommandStates();
        }
    }

    public bool CanResetProperty =>
        GetResetTargetProperty() != null;

    public bool CanResetSelectedProperty =>
        SelectedProperty?.CanReset == true;

    public bool HasSelectedSetting =>
        SelectedEntry != null;

    public bool HasSelectedCategory =>
        SelectedSheet != null;

    public bool HasVisibleCategories =>
        Sheets.Count > 0;

    public bool SelectedCategoryHasSettings =>
        SelectedSheet?.Entries.Count > 0;

    public bool HasVisibleSettings =>
        Entries.Count > 0;

    public bool HasVisibleProperties =>
        Properties.Count > 0;

    public string SelectedSettingTitle
    {
        get
        {
            if (SelectedEntry == null)
            {
                return string.Empty;
            }

            string? localizedName =
                localizationService.GetLocalizedName(
                    SelectedEntry.DisplayName);

            return string.IsNullOrWhiteSpace(
                    localizedName)
                ? SelectedEntry.DisplayName
                : localizedName;
        }
    }

    public string SelectedSettingContext
    {
        get
        {
            if (SelectedEntry == null)
            {
                return string.Empty;
            }

            string categoryName =
                SelectedSheet?.Name
                ?? string.Empty;
            string internalName =
                SelectedEntry.DisplayName;
            string? localizedName =
                localizationService.GetLocalizedName(
                    internalName);

            if (string.IsNullOrWhiteSpace(
                    localizedName)
                ||
                string.Equals(
                    localizedName,
                    internalName,
                    StringComparison.Ordinal))
            {
                return categoryName;
            }

            return string.IsNullOrWhiteSpace(
                    categoryName)
                ? internalName
                : $"{categoryName} · {internalName}";
        }
    }

    public int SelectedSettingModifiedCount =>
        SelectedEntry?.Properties.Count(
            property =>
                property.IsModified)
        ?? 0;

    public string SelectedSettingModificationStatus =>
        SelectedSettingModifiedCount switch
        {
            0 => string.Empty,
            1 => "1 change in this setting",
            int count =>
                $"{count:N0} changes in this setting"
        };

    public ObservableCollection<SearchResultModel>
        SearchResults
    {
        get;
    } = new();

    public bool HasSearchText =>
        !string.IsNullOrWhiteSpace(
            SearchText);

    public string FindAnythingHeader =>
        $"Search Results ({SearchResults.Count})";

    private string localizationStatus =
        "Language Data: unavailable";

    public string LocalizationStatus
    {
        get => localizationStatus;
        set
        {
            if (!SetProperty(
                    ref localizationStatus,
                    value))
            {
                return;
            }

            NotifySelectedSettingPresentationChanged();
        }
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
                nameof(HasVisibleCategories));
            OnPropertyChanged(
                nameof(HasVisibleSettings));
            OnPropertyChanged(
                nameof(HasSearchText));

            RefreshSearchResults();
            RefreshCommandStates();
        }
    }

    public bool ShouldShowLanguageDataSetup =>
        !languageDataService.CurrentState.IsAvailable;

    public string LanguageDataSetupMessage =>
        languageDataService.CurrentState.Availability ==
            LanguageDataAvailability.Invalid
            ? "Stored language data could not be used. Internal IDs are still available."
            : "Language data is not set up. Internal IDs are still available.";

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
            OnPropertyChanged(
                nameof(HasVisibleCategories));
            OnPropertyChanged(
                nameof(HasVisibleSettings));

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

    public bool IsImportInProgress
    {
        get => isImportInProgress;
        private set
        {
            if (!SetProperty(
                    ref isImportInProgress,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(IsEditorInteractionEnabled));
            RefreshCommandStates();
        }
    }

    public bool IsEditorInteractionEnabled =>
        !IsImportInProgress;

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

            string changeText =
                ModifiedPropertyCount == 1
                    ? "change"
                    : "changes";

            return
                $"{ModifiedPropertyCount:N0} {changeText}";
        }
    }

    public string WindowTitle
    {
        get
        {
            string fileName =
                string.IsNullOrWhiteSpace(
                    CurrentFile)
                    ? "No Wartales file open"
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

    public RelayCommand ImportFromWartalesCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand ShowGameplayToolsWorkspaceCommand
    {
        get;
    }

    public RelayCommand ShowDetailedEditorWorkspaceCommand
    {
        get;
    }

    public RelayCommand NavigateSearchResultCommand
    {
        get;
    }

    public RelayCommand ClearSearchCommand
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

    public RelayCommand CheckCompatibilityCommand
    {
        get;
    }

    public RelayCommand ReviewUpdateCompatibilityCommand =>
        CheckCompatibilityCommand;

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

    public RelayCommand ShowAboutCommand
    {
        get;
    }

    public RelayCommand ShowLanguageDataCommand
    {
        get;
    }

    public RelayCommand ShowGoldenCdbCommand { get; }

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

    public RelayCommand GameplayPresetCommand { get; }

    public RelayCommand RandomTraitExclusionsCommand { get; }

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
        : this(
            jsonDataService,
            searchService,
            localizationService,
            editHistoryService,
            modificationSnapshotService,
            modificationSnapshotWorkflowService,
            changeSummaryService,
            modProfileLibraryService,
            modProfileWorkflowService,
            referenceDataService,
            validationWorkflowService,
            validationPresentationService,
            projectOperationService,
            projectOperationTransactionService,
            addCampFacilitiesOperation,
            upgradeAllEquipmentOperation,
            fileDialogService,
            messageDialogService,
            CreateDefaultLanguageDataService(
                localizationService))
    {
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
    IMessageDialogService messageDialogService,
    LanguageDataService languageDataService)
        : this(
            jsonDataService,
            searchService,
            localizationService,
            editHistoryService,
            modificationSnapshotService,
            modificationSnapshotWorkflowService,
            changeSummaryService,
            modProfileLibraryService,
            modProfileWorkflowService,
            referenceDataService,
            validationWorkflowService,
            validationPresentationService,
            projectOperationService,
            projectOperationTransactionService,
            addCampFacilitiesOperation,
            upgradeAllEquipmentOperation,
            fileDialogService,
            messageDialogService,
            languageDataService,
            new WartalesInstallationService(),
            QuickBmsImportOptions.CreateDefault())
    {
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
    IMessageDialogService messageDialogService,
    LanguageDataService languageDataService,
    WartalesInstallationService wartalesInstallationService,
    QuickBmsImportOptions quickBmsImportOptions)
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

        this.languageDataService =
            languageDataService
            ?? throw new ArgumentNullException(
                nameof(languageDataService));

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

        this.wartalesInstallationService =
            wartalesInstallationService
            ?? throw new ArgumentNullException(
                nameof(wartalesInstallationService));

        this.quickBmsImportOptions =
            quickBmsImportOptions
            ?? throw new ArgumentNullException(
                nameof(quickBmsImportOptions));

        localizationStatus =
            CreateLanguageDataStatus(
                this.languageDataService.CurrentState);

        quickBmsImportService =
            new QuickBmsImportService(
                this.jsonDataService);

        goldenCdbService =
            new GoldenCdbService(this.jsonDataService);
        goldenCdbComparisonService =
            new GoldenCdbComparisonService();

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

        gameplayPresetService =
            new GameplayPresetService(
                progressionMutationService,
                gameplayOperationStateService);

        randomTraitExclusionsService =
            new RandomTraitExclusionsService(
                progressionMutationService,
                gameplayOperationStateService);

        this.editHistoryService.HistoryChanged +=
            OnHistoryChanged;

        OpenCommand =
            new RelayCommand(
                _ => OpenProject(),
                _ => !IsImportInProgress);

        ImportFromWartalesCommand =
            new RelayCommand(
                _ => ImportFromWartales(),
                _ => !IsImportInProgress);

        SaveCommand =
            new RelayCommand(
                _ => SaveProject(),
                _ => Project != null);

        ShowGameplayToolsWorkspaceCommand =
            new RelayCommand(
                _ => ActivateWorkspace(
                    MainWorkspace.GameplayTools));

        ShowDetailedEditorWorkspaceCommand =
            new RelayCommand(
                _ => ActivateWorkspace(
                    MainWorkspace.DetailedEditor));

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

        ClearSearchCommand =
            new RelayCommand(
                _ => SearchText = string.Empty,
                _ => HasSearchText);

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

        CheckCompatibilityCommand =
            new RelayCommand(
                _ => CheckCompatibility(),
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

        ShowAboutCommand =
            new RelayCommand(
                _ => ShowAbout());

        ShowLanguageDataCommand =
            new RelayCommand(
                _ => ShowLanguageData());

        ShowGoldenCdbCommand =
            new RelayCommand(
                _ => ShowGoldenCdb());

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

        GameplayPresetCommand =
            new RelayCommand(
                ExecuteGameplayPreset,
                parameter =>
                    Project != null &&
                    parameter is ProgressionType type &&
                    GameplayPresetCatalog.IsSupported(type));

        RandomTraitExclusionsCommand =
            new RelayCommand(
                _ => ExecuteRandomTraitExclusions(),
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

            PromoteLoadedProject(
                loadedProject,
                fileName);

            Status =
                $"Opened: " +
                $"{Path.GetFileName(CurrentFile)}";
        }
        catch (Exception exception)
        {
            messageDialogService.ShowError(
                $"The Wartales file could not be opened." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "No project was loaded. Check that the file is an extracted .cdb file and try again." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Details: {exception.Message}",
                "Open Wartales File");

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
            string initialFileName =
                string.IsNullOrWhiteSpace(
                    CurrentFile)
                    ? "data.cdb"
                    : Path.GetFileName(
                        CurrentFile);

            string? fileName =
                ResolveProjectSaveDestination(
                    initialFileName);

            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                Status =
                    "Save cancelled.";

                return false;
            }

            saveValidationStartedForTesting?.Invoke();
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

            bool targetsGolden =
                goldenCdbService.IsCanonicalPath(fileName);
            GoldenCdbState? reconciledGoldenState = null;

            if (targetsGolden)
            {
                goldenCdbService.InvalidateCache();
                goldenCdbComparisonService.Invalidate();
            }

            try
            {
                jsonDataService.SaveProject(
                    Project,
                    fileName);
            }
            finally
            {
                if (targetsGolden)
                {
                    reconciledGoldenState =
                        goldenCdbService
                            .ReconcileAfterCanonicalWrite();
                    goldenCdbComparisonService.Invalidate();
                    RefreshGoldenCdbWindowState(
                        reconciledGoldenState);
                }
            }

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

            if (targetsGolden &&
                reconciledGoldenState?.IsAvailable != true)
            {
                messageDialogService.ShowWarning(
                    "The project was saved, but the Golden CDB status could not be refreshed. The stored reference will be checked again the next time it is used.",
                    "Golden CDB Status");
            }

            return true;
        }
        catch (Exception exception)
        {
            messageDialogService.ShowError(
                $"The Wartales file could not be saved." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "Your unsaved changes remain open in the editor. Check the destination and try again." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Details: {exception.Message}",
                "Save Modded File");

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
                $"Save changes to {fileName} before continuing?" +
                Environment.NewLine + Environment.NewLine +
                "Choose Yes to save, " +
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
                "Add the Anvil and Apothecary Table to the camp, including their Workshop recipes?",
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
                "Camp facilities added." +
                Environment.NewLine + Environment.NewLine +
                "The Anvil and Apothecary Table are now configured with their Workshop recipes.",
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
                $"Details: {exception.Message}",
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
                "Equipment can now be upgraded." +
                Environment.NewLine +
                Environment.NewLine +
                $"{affectedEquipmentCount:N0} equipment items were updated.",
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
                $"Details: {exception.Message}",
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
            RestoreAndActivateWindow(
                progressionScalingDialog);
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

            ShowFeatureWindow(dialog);

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
                $"Details: {exception.Message}",
                "XP Progression");

            Status = "XP Progression failed to open.";
        }
        finally
        {
            Trace.WriteLine(
                "XP Progression: command handler finally path.");
        }
    }

    private string? ResolveProjectSaveDestination(
        string initialFileName)
    {
        bool activeProjectIsGolden =
            Project != null &&
            goldenCdbService.IsCanonicalPath(
                Project.FileName);

        if (activeProjectIsGolden)
        {
            GoldenSaveChoice choice =
                PromptGoldenSaveChoice();

            if (choice == GoldenSaveChoice.Cancel)
                return null;

            if (choice ==
                GoldenSaveChoice.SaveGoldenAnyway)
            {
                return goldenCdbService
                    .GetCanonicalPath();
            }
        }

        string? selected =
            fileDialogService.ShowSaveFileDialog(
                ProjectSaveFilter,
                initialFileName);

        if (string.IsNullOrWhiteSpace(selected) ||
            !goldenCdbService.IsCanonicalPath(selected))
        {
            return selected;
        }

        return messageDialogService.ShowConfirmation(
            "This destination is your designated Golden CDB. Saving here will replace the reference bytes. Choose Yes only if you intend to change Golden; choose No to return without saving.",
            "Save Over Golden CDB?")
            ? selected
            : null;
    }

    private GoldenSaveChoice PromptGoldenSaveChoice()
    {
        UnsavedChangesResult result =
            messageDialogService.ShowUnsavedChanges(
                "This project is the designated Golden CDB. Saving over it will change your reference baseline." +
                Environment.NewLine + Environment.NewLine +
                "Choose Yes to Save Golden Anyway, No to choose another location, or Cancel to return to the editor.",
                "Save Golden CDB?");

        return result switch
        {
            UnsavedChangesResult.Save =>
                GoldenSaveChoice.SaveGoldenAnyway,
            UnsavedChangesResult.Discard =>
                GoldenSaveChoice.ChooseAnotherLocation,
            _ => GoldenSaveChoice.Cancel
        };
    }

    public bool ConfirmApplicationClose()
    {
        if (IsImportInProgress)
        {
            messageDialogService.ShowWarning(
                "Wartales data is still being imported. Wait for the import to finish before closing the editor.",
                "Import In Progress");

            return false;
        }

        return ConfirmAbandonUnsavedChanges();
    }

    private async void ImportFromWartales()
    {
        _ = await ImportFromWartalesAsync();
    }

    private async Task<QuickBmsImportResult?>
        ImportFromWartalesAsync()
    {
        if (IsImportInProgress
            ||
            !ConfirmAbandonUnsavedChanges())
        {
            return null;
        }

        IsImportInProgress = true;

        try
        {
            string promotedCdbPath =
                quickBmsImportService.GetPromotedCdbPath(
                    quickBmsImportOptions);
            bool replaceExistingExtractedCdb =
                File.Exists(promotedCdbPath);

            if (replaceExistingExtractedCdb
                &&
                !ConfirmReplaceExistingExtractedCdb())
            {
                Status =
                    "Wartales import cancelled. The existing extracted data file was preserved.";
                return null;
            }

            Status =
                "Importing Wartales data safely. The game package will not be changed...";

            QuickBmsImportResult result =
                await quickBmsImportService.ImportAsync(
                    quickBmsImportOptions,
                    replaceExistingExtractedCdb);

            PromoteLoadedProject(
                result.Project,
                result.ExtractedCdbPath);

            Status =
                $"Imported Wartales data ({result.Project.Sheets.Count:N0} sheets).";

            string cleanupNote =
                result.StagingCleaned
                    ? string.Empty
                    : Environment.NewLine + Environment.NewLine +
                      "The temporary extraction folder could not be removed automatically.";

            messageDialogService.ShowInformation(
                "Wartales data was imported successfully and opened from the game's Extracted folder. The original game package was not changed." +
                cleanupNote,
                "Import From Wartales");

            return result;
        }
        catch (QuickBmsImportException exception)
        {
            messageDialogService.ShowError(
                exception.Message,
                "Import From Wartales");

            Status =
                "Wartales import failed. The current project was preserved.";

            return null;
        }
        catch (Exception exception)
        {
            messageDialogService.ShowError(
                "Wartales data could not be imported. No project was replaced." +
                Environment.NewLine + Environment.NewLine +
                $"Details: {exception.Message}",
                "Import From Wartales");

            Status =
                "Wartales import failed. The current project was preserved.";

            return null;
        }
        finally
        {
            IsImportInProgress = false;
        }
    }

    internal bool ConfirmReplaceExistingExtractedCdb()
    {
        return messageDialogService.ShowConfirmation(
            "An existing extracted data file was found. The editor cannot guarantee that this file is a fresh extraction from the current Wartales installation. Continuing will replace it with a new extraction.",
            "Replace Existing Extracted Data?");
    }

    internal void PromoteLoadedProject(
        ProjectModel loadedProject,
        string displayFileName)
    {
        ArgumentNullException.ThrowIfNull(loadedProject);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayFileName);

        ReferenceDataPreparation preparedReferences =
            referenceDataService.Prepare(
                loadedProject);

        ReferenceDataPreparation previousReferences =
            referenceDataService.Capture();
        ProjectModel? previousProject = Project;
        string previousFile = CurrentFile;

        try
        {
            referenceDataService.Apply(
                preparedReferences);

            projectPublicationFailureForTesting?.Invoke();

            CurrentFile = displayFileName;
            Project = loadedProject;

            jsonDataService.CompletePostPublicationMigration(
                loadedProject);

        }
        catch
        {
            referenceDataService.Apply(
                previousReferences);
            CurrentFile = previousFile;

            if (!ReferenceEquals(
                    Project,
                    previousProject))
            {
                Project = previousProject;
            }

            throw;
        }
    }

    private void OnProgressionApplyRequested(
        object? sender,
        ProgressionApplyRequestedEventArgs e)
    {
        if (sender is ProgressionScalingDialog feedbackDialog &&
            feedbackDialog.DataContext is ProgressionScalingDialogViewModel feedbackViewModel)
            feedbackViewModel.ApplyFeedback.Clear();

        bool? wasModified = ExecuteProgressionOperation(
            e.ProgressionType,
            e.Percentage);

        if (sender is ProgressionScalingDialog dialog &&
            dialog.DataContext is
                ProgressionScalingDialogViewModel viewModel)
        {
            viewModel.RefreshFromProject();
            if (wasModified == true)
            {
                viewModel.ApplyFeedback.ShowApplied(
                    $"{(e.ProgressionType == ProgressionType.Character ? "Character XP" : "Profession XP")} was updated.");
            }
            else if (wasModified == false)
            {
                viewModel.ApplyFeedback.ShowAlreadyApplied();
            }
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
                $"The editor does not have the original {displayName} values for this file." +
                Environment.NewLine +
                Environment.NewLine +
                "The current values may already be modified. If you continue, " +
                "they will become the new 100% values. Earlier values cannot " +
                "be recovered automatically." +
                Environment.NewLine +
                Environment.NewLine +
                "Use the current values as 100%?",
                $"Use Current {displayName} Values?");

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

            Status = $"Current {displayName} values are now 100%.";
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
                $"Details: {exception.Message}",
                $"Use Current {displayName} Values?");
        }
    }

    private void OnProgressionDialogDisplayFailed(
        Exception exception)
    {
        messageDialogService.ShowError(
            "XP Progression failed while loading or rendering." +
            Environment.NewLine +
            Environment.NewLine +
            $"Details: {exception.Message}",
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

    private bool? ExecuteProgressionOperation(
        ProgressionType progressionType,
        int percentage)
    {
        if (Project == null)
        {
            return null;
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
                return null;
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

            Status = result.WasModified
                ? $"{operation.Name} set to {percentage}%."
                : $"{operation.Name} already matched {percentage}%.";
            return result.WasModified;
        }
        catch (Exception exception)
        {
            RefreshAfterProjectOperation();

            messageDialogService.ShowError(
                $"{operation.Name} could not be applied." +
                Environment.NewLine +
                Environment.NewLine +
                $"Details: {exception.Message}",
                operation.Name);

            Status = $"{operation.Name} failed.";
            return null;
        }
    }

    private void ExecuteStartingResources(object? parameter)
    {
        if (Project == null) return;
        if (startingResourcesDialog != null)
        {
            RestoreAndActivateWindow(
                startingResourcesDialog);
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
            ShowFeatureWindow(dialog);
            startingResourcesDialog = dialog;
            Status = "Starting Resources opened.";
        }
        catch (Exception exception)
        {
            dialog?.Close();
            startingResourcesDialog = null;
            messageDialogService.ShowError(
                "Starting Resources could not be opened." + Environment.NewLine +
                Environment.NewLine + $"Details: {exception.Message}",
                "Starting Resources");
            Status = "Starting Resources failed to open.";
        }
    }

    private void OnStartingResourcesInitializeRequested(object? sender, EventArgs e)
    {
        if (Project == null) return;
        bool confirmed = messageDialogService.ShowConfirmation(
            "Remember the current starting supplies so future adjustments remain accurate?",
            "Set Up Starting Resources");
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
                Environment.NewLine + $"Details: {exception.Message}",
                "Starting Resources");
        }
    }

    private void OnStartingResourcesApplyRequested(
        object? sender,
        StartingResourcesApplyEventArgs e)
    {
        if (Project == null) return;
        if (sender is StartingResourcesDialog feedbackDialog &&
            feedbackDialog.DataContext is StartingResourcesDialogViewModel feedbackViewModel)
            feedbackViewModel.ApplyFeedback.Clear();
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
                if (operationResult.MutationResult.WasModified)
                    vm.ApplyFeedback.ShowApplied("Starting resources were updated.");
                else
                    vm.ApplyFeedback.ShowAlreadyApplied();
            }
            Status = "Starting Resources updated.";
        }
        catch (Exception exception)
        {
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                "Starting Resources could not be applied." + Environment.NewLine +
                Environment.NewLine + $"Details: {exception.Message}",
                operation.Name);
        }
    }

    private void OnStartingResourcesDisplayFailed(Exception exception)
    {
        messageDialogService.ShowError(
            "Starting Resources failed while loading or rendering." + Environment.NewLine +
            Environment.NewLine + $"Details: {exception.Message}",
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
            RestoreAndActivateWindow(
                existing);
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
            ShowFeatureWindow(dialog);
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
        if (sender is PartyEconomyDialog feedbackDialog &&
            feedbackDialog.DataContext is PartyEconomyDialogViewModel feedbackViewModel)
            feedbackViewModel.ApplyFeedback.Clear();
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
            {
                viewModel.RefreshFromProject();
                if (result.MutationResult.WasModified)
                    viewModel.ApplyFeedback.ShowApplied(
                        e.RestorePreviousValues
                            ? "Previous values were restored."
                            : e.OperationType switch
                            {
                                ProgressionType.VolunteerWages => "Volunteer wage settings were updated.",
                                ProgressionType.ValourPoints => "Valour Point settings were updated.",
                                _ => "Carrying Capacity settings were updated."
                            });
                else
                    viewModel.ApplyFeedback.ShowAlreadyApplied();
            }
            Status = e.RestorePreviousValues
                ? $"{operation.Name} previous values restored."
                : e.OperationType switch
                {
                    ProgressionType.VolunteerWages => "Volunteer Trait updated.",
                    ProgressionType.ValourPoints => "Valour Points updated.",
                    _ => "Carrying Capacity updated."
                };
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

    private void ExecuteGameplayPreset(object? parameter)
    {
        if (Project == null ||
            parameter is not ProgressionType type ||
            !GameplayPresetCatalog.IsSupported(type))
            return;

        if (gameplayPresetDialogs.TryGetValue(type, out GameplayPresetDialog? existing))
        {
            RestoreAndActivateWindow(existing);
            return;
        }

        GameplayPresetDialog? dialog = null;
        try
        {
            GameplayPresetDialogViewModel viewModel =
                new(Project, gameplayPresetService, type);
            dialog = new GameplayPresetDialog
            {
                Owner = GetMainWindowOwner(),
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.ApplyRequested += OnGameplayPresetApplyRequested;
            dialog.DisplayFailed += OnGameplayPresetDisplayFailed;
            dialog.Closed += OnGameplayPresetClosed;
            ShowFeatureWindow(dialog);
            gameplayPresetDialogs[type] = dialog;
            Status = $"{viewModel.Title} opened.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            dialog?.Close();
            gameplayPresetDialogs.Remove(type);
            messageDialogService.ShowError(
                "The gameplay tool could not be opened." + Environment.NewLine +
                Environment.NewLine + "The project was not changed.",
                "Gameplay Tools");
        }
    }

    private void OnGameplayPresetApplyRequested(
        object? sender,
        GameplayPresetApplyEventArgs e)
    {
        if (Project == null) return;
        if (sender is GameplayPresetDialog feedbackDialog &&
            feedbackDialog.DataContext is GameplayPresetDialogViewModel feedbackViewModel)
            feedbackViewModel.ApplyFeedback.Clear();
        IProjectOperation operation = new GameplayPresetOperation(
            gameplayPresetService,
            e.OperationType,
            e.PresetKey,
            e.RestorePreviousValues);
        try
        {
            ProjectOperationResult result;
            using (editHistoryService.SuppressRecording())
                result = projectOperationService.Execute(operation, Project);

            if (!result.Succeeded)
            {
                RefreshAfterProjectOperation();
                messageDialogService.ShowError(
                    "The settings could not be applied." + Environment.NewLine +
                    Environment.NewLine + "No changes were made.",
                    operation.Name);
                return;
            }

            if (result.MutationResult.WasModified)
                editHistoryService.Record(new ProjectOperationHistoryAction(
                    operation.Name,
                    result.MutationResult,
                    projectOperationTransactionService));
            RefreshAfterProjectOperation();
            if (sender is GameplayPresetDialog dialog &&
                dialog.DataContext is GameplayPresetDialogViewModel viewModel)
            {
                viewModel.RefreshFromProject();
                if (result.MutationResult.WasModified)
                    viewModel.ApplyFeedback.ShowApplied(
                        e.RestorePreviousValues
                            ? "Previous values were restored."
                            : $"{viewModel.Title} was updated.");
                else
                    viewModel.ApplyFeedback.ShowAlreadyApplied();
            }

            Status = result.MutationResult.WasModified
                ? e.RestorePreviousValues
                    ? $"{operation.Name} previous values restored."
                    : $"{operation.Name} updated."
                : "No changes were applied. These settings already match the current project.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                "The settings could not be applied." + Environment.NewLine +
                Environment.NewLine + "No changes were made.",
                operation.Name);
        }
    }

    private void OnGameplayPresetDisplayFailed(Exception exception)
    {
        Debug.WriteLine(exception);
        messageDialogService.ShowError(
            "The gameplay tool could not be displayed." + Environment.NewLine +
            Environment.NewLine + "The project was not changed.",
            "Gameplay Tools");
    }

    private void OnGameplayPresetClosed(object? sender, EventArgs e)
    {
        if (sender is not GameplayPresetDialog dialog) return;
        dialog.ApplyRequested -= OnGameplayPresetApplyRequested;
        dialog.DisplayFailed -= OnGameplayPresetDisplayFailed;
        dialog.Closed -= OnGameplayPresetClosed;
        ProgressionType? key = gameplayPresetDialogs
            .Where(pair => ReferenceEquals(pair.Value, dialog))
            .Select(pair => (ProgressionType?)pair.Key)
            .FirstOrDefault();
        if (key.HasValue) gameplayPresetDialogs.Remove(key.Value);
    }

    private void ExecuteRandomTraitExclusions()
    {
        if (Project == null) return;
        if (randomTraitExclusionsDialog != null)
        {
            RestoreAndActivateWindow(randomTraitExclusionsDialog);
            return;
        }

        RandomTraitExclusionsDialog? dialog = null;
        try
        {
            RandomTraitExclusionsDialogViewModel viewModel =
                new(
                    Project,
                    randomTraitExclusionsService,
                    localizationService);
            dialog = new RandomTraitExclusionsDialog
            {
                Owner = GetMainWindowOwner(),
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.ApplyRequested += OnRandomTraitExclusionsApplyRequested;
            dialog.DisplayFailed += OnRandomTraitExclusionsDisplayFailed;
            dialog.Closed += OnRandomTraitExclusionsClosed;
            ShowFeatureWindow(dialog);
            randomTraitExclusionsDialog = dialog;
            Status = "Random Trait Exclusions opened.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            dialog?.Close();
            randomTraitExclusionsDialog = null;
            messageDialogService.ShowError(
                "Random Trait Exclusions could not be opened." + Environment.NewLine +
                Environment.NewLine + "The project was not changed.",
                "Random Trait Exclusions");
        }
    }

    private void OnRandomTraitExclusionsApplyRequested(
        object? sender,
        RandomTraitExclusionsApplyEventArgs e)
    {
        if (Project == null) return;
        if (sender is RandomTraitExclusionsDialog feedbackDialog &&
            feedbackDialog.DataContext is RandomTraitExclusionsDialogViewModel feedbackViewModel)
            feedbackViewModel.ApplyFeedback.Clear();

        IProjectOperation operation = new RandomTraitExclusionsOperation(
            randomTraitExclusionsService,
            e.AllowedTraitIds);
        try
        {
            ProjectOperationResult result;
            using (editHistoryService.SuppressRecording())
                result = projectOperationService.Execute(operation, Project);

            if (!result.Succeeded)
            {
                RefreshAfterProjectOperation();
                messageDialogService.ShowError(
                    "The exclusions could not be applied." + Environment.NewLine +
                    Environment.NewLine + "No changes were made.",
                    operation.Name);
                return;
            }

            if (result.MutationResult.WasModified)
                editHistoryService.Record(new ProjectOperationHistoryAction(
                    operation.Name,
                    result.MutationResult,
                    projectOperationTransactionService));
            RefreshAfterProjectOperation();

            if (sender is RandomTraitExclusionsDialog dialog &&
                dialog.DataContext is RandomTraitExclusionsDialogViewModel viewModel)
            {
                viewModel.RefreshFromProject(Project, randomTraitExclusionsService);
                if (result.MutationResult.WasModified)
                    viewModel.ApplyFeedback.ShowApplied(
                        "Random trait exclusions were updated.");
                else
                    viewModel.ApplyFeedback.ShowAlreadyApplied();
            }

            Status = result.MutationResult.WasModified
                ? "Random trait exclusions updated."
                : "No changes were applied. These settings already match the current project.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                "The exclusions could not be applied." + Environment.NewLine +
                Environment.NewLine + "No changes were made.",
                operation.Name);
        }
    }

    private void OnRandomTraitExclusionsDisplayFailed(Exception exception)
    {
        Debug.WriteLine(exception);
        messageDialogService.ShowError(
            "Random Trait Exclusions could not be displayed." + Environment.NewLine +
            Environment.NewLine + "The project was not changed.",
            "Random Trait Exclusions");
    }

    private void OnRandomTraitExclusionsClosed(object? sender, EventArgs e)
    {
        if (sender is not RandomTraitExclusionsDialog dialog) return;
        dialog.ApplyRequested -= OnRandomTraitExclusionsApplyRequested;
        dialog.DisplayFailed -= OnRandomTraitExclusionsDisplayFailed;
        dialog.Closed -= OnRandomTraitExclusionsClosed;
        if (ReferenceEquals(randomTraitExclusionsDialog, dialog))
            randomTraitExclusionsDialog = null;
    }

    private void ExecuteOverworldMovementSpeed(object? parameter)
    {
        if (Project == null) return;
        if (overworldMovementSpeedDialog != null)
        {
            RestoreAndActivateWindow(
                overworldMovementSpeedDialog);
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
            ShowFeatureWindow(dialog);
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
        if (sender is OverworldMovementSpeedDialog feedbackDialog &&
            feedbackDialog.DataContext is OverworldMovementSpeedDialogViewModel feedbackViewModel)
            feedbackViewModel.ApplyFeedback.Clear();
        IProjectOperation operation =
            new OverworldMovementSpeedOperation(
                overworldMovementSpeedService,
                e.Preset,
                e.RestorePreviousValues);
        try
        {
            ProjectOperationResult result;
            using (editHistoryService.SuppressRecording())
                result = projectOperationService.Execute(operation, Project);
            if (!result.Succeeded)
            {
                RefreshAfterProjectOperation();
                messageDialogService.ShowError(
                    (e.RestorePreviousValues
                        ? "The previous movement values could not be restored."
                        : "The movement preset could not be applied.") +
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
            {
                viewModel.RefreshFromProject();
                if (result.MutationResult.WasModified)
                    viewModel.ApplyFeedback.ShowApplied(
                        e.RestorePreviousValues
                            ? "Previous values were restored."
                            : "Movement speed was updated.");
                else
                    viewModel.ApplyFeedback.ShowAlreadyApplied();
            }
            Status = result.MutationResult.WasModified
                ? e.RestorePreviousValues
                    ? "Movement Speed previous values restored."
                    : "Movement Speed updated."
                : "Movement Speed already matched the selected setting.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                (e.RestorePreviousValues
                    ? "The previous movement values could not be restored."
                    : "The movement preset could not be applied.") +
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
            RestoreAndActivateWindow(
                rainFrequencyDialog);
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
            ShowFeatureWindow(dialog);
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
        if (sender is RainFrequencyDialog feedbackDialog &&
            feedbackDialog.DataContext is RainFrequencyDialogViewModel feedbackViewModel)
            feedbackViewModel.ApplyFeedback.Clear();
        IProjectOperation operation =
            new RainFrequencyOperation(
                rainFrequencyService,
                e.Preset,
                e.RestorePreviousValues);
        try
        {
            ProjectOperationResult result;
            using (editHistoryService.SuppressRecording())
                result = projectOperationService.Execute(operation, Project);
            if (!result.Succeeded)
            {
                RefreshAfterProjectOperation();
                messageDialogService.ShowError(
                    (e.RestorePreviousValues
                        ? "The previous rain values could not be restored."
                        : "The rain preset could not be applied.") +
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
            {
                viewModel.RefreshFromProject();
                if (result.MutationResult.WasModified)
                    viewModel.ApplyFeedback.ShowApplied(
                        e.RestorePreviousValues
                            ? "Previous values were restored."
                            : "Rain frequency was updated.");
                else
                    viewModel.ApplyFeedback.ShowAlreadyApplied();
            }
            Status = result.MutationResult.WasModified
                ? e.RestorePreviousValues
                    ? "Rain Frequency previous values restored."
                    : "Rain Frequency updated."
                : "Rain Frequency already matched the selected setting.";
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            RefreshAfterProjectOperation();
            messageDialogService.ShowError(
                (e.RestorePreviousValues
                    ? "The previous rain values could not be restored."
                    : "The rain preset could not be applied.") +
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

        try
        {
            ValidationResultModel validationResult =
                validationWorkflowService
                    .ValidateProject(Project);

            ShowValidationResults(
                validationResult);

            SetProjectCheckStatus(
                validationResult);
        }
        catch (Exception exception)
        {
            ShowProjectCheckFailure(
                exception,
                "No validation result is available.");
        }
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
        OnPropertyChanged(
            nameof(HasVisibleCategories));
        OnPropertyChanged(
            nameof(HasVisibleSettings));
        OnPropertyChanged(
            nameof(HasVisibleProperties));

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

        lastValidationResult =
            validationResult;

        if (validationResultsWindow != null)
        {
            validationResultsViewModel?.Refresh(
                validationResult);

            RestoreAndActivateWindow(
                validationResultsWindow);

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
                Owner =
                    GetMainWindowOwner(),
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,
                DataContext =
                    validationResultsViewModel
            };

        validationResultsWindow.Closed +=
            OnValidationResultsWindowClosed;

        ShowFeatureWindow(validationResultsWindow);
    }

    private ValidationResultModel
        RerunProjectValidation()
    {
        if (Project == null)
        {
            return ValidationResultModel.Empty;
        }

        try
        {
            ValidationResultModel validationResult =
                validationWorkflowService
                    .ValidateProject(Project);

            lastValidationResult =
                validationResult;

            SetProjectCheckStatus(
                validationResult);

            return validationResult;
        }
        catch (Exception exception)
        {
            ShowProjectCheckFailure(
                exception,
                "The previous results are still displayed.");

            return lastValidationResult
                ?? ValidationResultModel.Empty;
        }
    }

    private void SetProjectCheckStatus(
        ValidationResultModel validationResult)
    {
        Status =
            validationResult.HasErrors
                ? "Project check found errors."
                : validationResult.HasWarnings
                    ? "Ready to save, but review the warnings."
                    : validationResult.HasInformation
                        ? "Ready to save. Additional information is available."
                        : "Ready to save. No issues were found.";
    }

    private void ShowProjectCheckFailure(
        Exception exception,
        string resultState)
    {
        messageDialogService.ShowError(
            "The project could not be checked." +
            Environment.NewLine + Environment.NewLine +
            resultState + " Try again; if the problem continues, reopen the Wartales file." +
            Environment.NewLine + Environment.NewLine +
            $"Details: {exception.Message}",
            "Check Project");

        Status = "Project check failed.";
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
            "Project check details copied to the clipboard.";
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
                    "changes")} are not available in this Wartales file.");

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
                "Every profile change was applied.");
        }

        return message.ToString();
    }

    private void ShowLanguageData()
    {
        if (languageDataDialog != null)
        {
            languageDataDialogViewModel?.Refresh(
                languageDataService.CurrentState);

            RestoreAndActivateWindow(
                languageDataDialog);
            return;
        }

        languageDataDialogViewModel =
            new LanguageDataDialogViewModel(
                languageDataService.CurrentState);

        languageDataDialog =
            new LanguageDataDialog
            {
                Owner =
                    GetMainWindowOwner(),
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,
                DataContext =
                    languageDataDialogViewModel
            };

        languageDataDialog.SelectionRequested +=
            OnLanguageDataSelectionRequested;
        languageDataDialog.Closed +=
            OnLanguageDataDialogClosed;

        ShowFeatureWindow(
            languageDataDialog);
    }

    private void ShowGoldenCdb()
    {
        if (goldenCdbWindow != null)
        {
            RefreshGoldenCdbWindowState();
            RestoreAndActivateWindow(goldenCdbWindow);
            return;
        }

        goldenCdbViewModel = new GoldenCdbViewModel(
            goldenCdbService.GetState(),
            Project != null);
        goldenCdbWindow = new GoldenCdbWindow
        {
            Owner = GetMainWindowOwner(),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DataContext = goldenCdbViewModel
        };
        goldenCdbWindow.SetCurrentRequested +=
            OnGoldenSetCurrentRequested;
        goldenCdbWindow.SelectRequested +=
            OnGoldenSelectRequested;
        goldenCdbWindow.ImportCurrentWartalesRequested +=
            OnGoldenImportCurrentWartalesRequested;
        goldenCdbWindow.LoadRequested +=
            OnGoldenLoadRequested;
        goldenCdbWindow.CompareRequested +=
            OnGoldenCompareRequested;
        goldenCdbWindow.RemoveRequested +=
            OnGoldenRemoveRequested;
        goldenCdbWindow.Closed += OnGoldenCdbWindowClosed;
        ShowFeatureWindow(goldenCdbWindow);
    }

    private void OnGoldenSetCurrentRequested(
        object? sender,
        EventArgs e)
    {
        if (Project == null)
            return;

        try
        {
            goldenCdbService.ValidateProjectSource(Project);
            if (!ConfirmGoldenDesignation(
                    goldenCdbService.GetState()))
            {
                return;
            }

            GoldenCdbState state =
                goldenCdbService.SetFromProject(Project);
            goldenCdbComparisonService.Invalidate();
            RefreshGoldenCdbWindowState(state);
            ShowGoldenDesignationResult(
                state,
                "Golden CDB was set from the current project.",
                "The exact saved CDB bytes are now your Golden reference. Wartales Editor does not certify that this file is vanilla or pristine.");
        }
        catch (Exception exception)
        {
            ShowGoldenFailure(
                "The current project could not be set as Golden.",
                exception);
        }
    }

    private void OnGoldenSelectRequested(
        object? sender,
        EventArgs e)
    {
        string? fileName =
            fileDialogService.ShowOpenFileDialog(
                ProjectOpenFilter);
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        try
        {
            goldenCdbService.ValidateSourceFile(fileName);
            if (!ConfirmGoldenDesignation(
                    goldenCdbService.GetState()))
            {
                return;
            }

            GoldenCdbState state =
                goldenCdbService.SetFromFile(fileName);
            goldenCdbComparisonService.Invalidate();
            RefreshGoldenCdbWindowState(state);
            ShowGoldenDesignationResult(
                state,
                "Golden CDB was set from the selected file.",
                "The selected CDB was copied into editor-owned storage. The original file is no longer required. Wartales Editor does not certify that it is vanilla or pristine.");
        }
        catch (Exception exception)
        {
            ShowGoldenFailure(
                "The selected CDB could not be set as Golden.",
                exception);
        }
    }

    private async void OnGoldenImportCurrentWartalesRequested(
        object? sender,
        EventArgs e)
    {
        _ = await ImportCurrentWartalesAsGoldenAsync();
    }

    internal async Task<bool>
        ImportCurrentWartalesAsGoldenAsync()
    {
        QuickBmsImportResult? importResult =
            await ImportFromWartalesAsync();
        if (importResult == null)
            return false;

        GoldenCdbState previousGolden =
            goldenCdbService.GetState();
        if (!ConfirmGoldenDesignation(previousGolden))
        {
            RefreshGoldenCdbWindowState(previousGolden);
            Status =
                "Wartales import succeeded. Golden CDB was not changed.";
            messageDialogService.ShowInformation(
                previousGolden.CanonicalFileExists
                    ? "Wartales data was imported successfully and remains open. Your existing Golden CDB was not replaced."
                    : "Wartales data was imported successfully and remains open. No Golden CDB was set.",
                "Golden CDB Unchanged");
            return false;
        }

        try
        {
            GoldenCdbState state =
                goldenCdbService.SetFromProject(
                    importResult.Project);
            goldenCdbComparisonService.Invalidate();
            RefreshGoldenCdbWindowState(state);
            ShowGoldenDesignationResult(
                state,
                "Current Wartales data was imported and set as Golden.",
                "The exact imported CDB bytes are now your Golden reference. Wartales Editor does not certify that this file is vanilla or pristine.");
            return true;
        }
        catch (Exception exception)
        {
            RefreshGoldenCdbWindowState();
            messageDialogService.ShowError(
                "Wartales data was imported successfully and remains open, but it could not be designated as Golden." +
                Environment.NewLine + Environment.NewLine +
                $"Details: {exception.Message}",
                "Golden CDB");
            Status =
                "Wartales import succeeded; Golden CDB designation failed.";
            return false;
        }
    }

    private bool ConfirmGoldenDesignation(
        GoldenCdbState state)
    {
        string action = state.CanonicalFileExists
            ? "replace your existing Golden CDB"
            : "set this CDB as your Golden reference";

        return messageDialogService.ShowConfirmation(
            $"This will {action}. Golden is a reference you designate; Wartales Editor verifies structural usability but does not certify that it is vanilla, pristine, or current." +
            Environment.NewLine + Environment.NewLine +
            "Continue?",
            state.CanonicalFileExists
                ? "Replace Golden CDB?"
                : "Set Golden CDB?");
    }

    private void ShowGoldenDesignationResult(
        GoldenCdbState state,
        string successStatus,
        string successMessage)
    {
        if (state.HasCleanupWarning)
        {
            Status =
                "Golden CDB was stored, but temporary cleanup needs attention.";
            messageDialogService.ShowWarning(
                state.Message,
                "Golden CDB Cleanup");
            return;
        }

        Status = successStatus;
        messageDialogService.ShowInformation(
            successMessage,
            "Golden CDB Set");
    }

    private void OnGoldenLoadRequested(
        object? sender,
        EventArgs e)
    {
        _ = LoadGoldenCdb();
    }

    internal bool LoadGoldenCdb()
    {
        try
        {
            ProjectModel detached =
                goldenCdbService.LoadDetachedProject();

            if (!ConfirmAbandonUnsavedChanges())
                return false;

            string canonicalPath =
                goldenCdbService.GetCanonicalPath();
            PromoteLoadedProject(detached, canonicalPath);
            Status = "Golden CDB opened as the current project.";
            RefreshGoldenCdbWindowState();
            return true;
        }
        catch (Exception exception)
        {
            ShowGoldenFailure(
                "Golden CDB could not be opened. The current project was preserved.",
                exception);
            return false;
        }
    }

    private void OnGoldenCompareRequested(
        object? sender,
        EventArgs e)
    {
        if (Project == null)
            return;

        try
        {
            GoldenCdbReference golden =
                goldenCdbService.LoadReference();
            GoldenCdbComparisonResult result =
                goldenCdbComparisonService.Compare(
                    Project,
                    golden);
            goldenCdbViewModel?.ShowComparison(result);
            Status = result.Summary;
        }
        catch (Exception exception)
        {
            RefreshGoldenCdbWindowState();
            ShowGoldenFailure(
                "The current project could not be compared to Golden.",
                exception);
        }
    }

    private void OnGoldenRemoveRequested(
        object? sender,
        EventArgs e)
    {
        GoldenCdbState state = goldenCdbService.GetState();
        if (!state.CanonicalFileExists)
        {
            RefreshGoldenCdbWindowState(state);
            return;
        }

        if (!messageDialogService.ShowConfirmation(
                "Remove the stored Golden CDB? The current project, profiles, gameplay state, and Undo history will not be changed.",
                "Remove Golden CDB?"))
        {
            return;
        }

        try
        {
            GoldenCdbState removed = goldenCdbService.Remove();
            goldenCdbComparisonService.Invalidate();
            RefreshGoldenCdbWindowState(removed);
            Status = "Golden CDB was removed.";
        }
        catch (Exception exception)
        {
            ShowGoldenFailure(
                "Golden CDB could not be removed.",
                exception);
        }
    }

    private void RefreshGoldenCdbWindowState(
        GoldenCdbState? state = null)
    {
        goldenCdbViewModel?.RefreshState(
            state ?? goldenCdbService.GetState(),
            Project != null);
    }

    private void ShowGoldenFailure(
        string message,
        Exception exception)
    {
        messageDialogService.ShowError(
            message + Environment.NewLine + Environment.NewLine +
            $"Details: {exception.Message}",
            "Golden CDB");
        Status = "Golden CDB operation failed.";
    }

    private void OnGoldenCdbWindowClosed(
        object? sender,
        EventArgs e)
    {
        if (goldenCdbWindow != null)
        {
            goldenCdbWindow.SetCurrentRequested -=
                OnGoldenSetCurrentRequested;
            goldenCdbWindow.SelectRequested -=
                OnGoldenSelectRequested;
            goldenCdbWindow.ImportCurrentWartalesRequested -=
                OnGoldenImportCurrentWartalesRequested;
            goldenCdbWindow.LoadRequested -=
                OnGoldenLoadRequested;
            goldenCdbWindow.CompareRequested -=
                OnGoldenCompareRequested;
            goldenCdbWindow.RemoveRequested -=
                OnGoldenRemoveRequested;
            goldenCdbWindow.Closed -=
                OnGoldenCdbWindowClosed;
        }

        goldenCdbWindow = null;
        goldenCdbViewModel = null;
    }

    internal void UseGoldenCdbServicesForTesting(
        GoldenCdbService service,
        GoldenCdbComparisonService comparisonService)
    {
        if (goldenCdbWindow != null)
        {
            throw new InvalidOperationException(
                "Golden CDB services cannot be changed while its window is open.");
        }

        goldenCdbService = service ??
            throw new ArgumentNullException(nameof(service));
        goldenCdbComparisonService = comparisonService ??
            throw new ArgumentNullException(nameof(comparisonService));
    }

    internal void UseQuickBmsImportServiceForTesting(
        QuickBmsImportService service)
    {
        quickBmsImportService = service ??
            throw new ArgumentNullException(nameof(service));
    }

    internal void UseProjectPublicationFailureForTesting(
        Action? failure)
    {
        projectPublicationFailureForTesting = failure;
    }

    internal void UseSaveValidationStartedForTesting(
        Action? callback)
    {
        saveValidationStartedForTesting = callback;
    }

    internal bool IsGoldenCdbWindowOpen =>
        goldenCdbWindow != null;

    private void OnLanguageDataSelectionRequested(
        object? sender,
        EventArgs e)
    {
        (string? initialFileName,
         string? initialDirectory) =
            ResolveLanguageDataSourceContext();

        string? sourceFile =
            fileDialogService.ShowOpenFileDialog(
                LanguageDataFileFilter,
                initialFileName,
                initialDirectory);

        if (string.IsNullOrWhiteSpace(
                sourceFile))
        {
            return;
        }

        bool hadPreviousSetup =
            languageDataService.CurrentState.IsAvailable;

        try
        {
            LanguageDataState state =
                InstallLanguageData(
                    sourceFile);

            languageDataDialogViewModel?.Refresh(
                state);

            Status =
                $"Language data ({state.Metadata!.LanguageCode}) is ready.";
        }
        catch (LanguageDataInstallException exception)
        {
            RefreshLanguageDataPresentation();

            languageDataDialogViewModel?.Refresh(
                languageDataService.CurrentState);

            ShowLanguageDataInstallFailure(
                exception);
        }
        catch (Exception exception)
        {
            RefreshLanguageDataPresentation();

            languageDataDialogViewModel?.Refresh(
                languageDataService.CurrentState);

            messageDialogService.ShowError(
                hadPreviousSetup
                    ? "Language data could not be replaced. Your previous language data was preserved." +
                      Environment.NewLine + Environment.NewLine +
                      $"Details: {exception.Message}"
                    : "Language data could not be set up. The editor will continue using internal IDs." +
                      Environment.NewLine + Environment.NewLine +
                      $"Details: {exception.Message}",
                "Language Data");

            Status =
                hadPreviousSetup
                    ? "Language data was not replaced."
                    : "Language data was not set up.";
        }
    }

    private (string? InitialFileName,
             string? InitialDirectory)
        ResolveLanguageDataSourceContext()
    {
        try
        {
            WartalesPackageInfo installation =
                wartalesInstallationService.Validate(
                    quickBmsImportOptions
                        .WartalesInstallationDirectory);

            IReadOnlyList<string> candidates =
                languageDataService.DiscoverValidSources(
                    installation.InstallationDirectory);

            return (
                candidates.FirstOrDefault(),
                installation.InstallationDirectory);
        }
        catch (Exception exception)
            when (exception is QuickBmsImportException
                  or IOException
                  or UnauthorizedAccessException
                  or ArgumentException
                  or NotSupportedException
                  or System.Security.SecurityException)
        {
            return (null, null);
        }
    }

    private void ShowLanguageDataInstallFailure(
        LanguageDataInstallException exception)
    {
        switch (exception.FailureKind)
        {
            case LanguageDataInstallFailureKind.CleanupFailed:
                messageDialogService.ShowWarning(
                    "Language data is ready, but a temporary recovery file could not be removed. The new language data remains active. The editor will retry cleanup the next time language data is changed.",
                    "Language Data");

                Status =
                    "Language data is ready; temporary cleanup is still required.";
                break;

            case LanguageDataInstallFailureKind.PreviousSetupRestored:
                messageDialogService.ShowError(
                    "Language data could not be replaced. Your previous language data was restored.",
                    "Language Data");

                Status =
                    "Language data was not replaced.";
                break;

            case LanguageDataInstallFailureKind.RecoveryFailed:
                messageDialogService.ShowError(
                    "Language data replacement failed and the previous setup could not be restored. The editor will continue using internal IDs until language data is set up again.",
                    "Language Data");

                Status =
                    "Language data must be set up again.";
                break;

            default:
                messageDialogService.ShowError(
                    "Language data could not be set up. The editor will continue using internal IDs.",
                    "Language Data");

                Status =
                    "Language data was not set up.";
                break;
        }
    }

    internal LanguageDataState InstallLanguageData(
        string sourceFile)
    {
        LanguageDataState state =
            languageDataService.Install(
                sourceFile);

        RefreshLanguageDataPresentation();

        return state;
    }

    private void OnLanguageDataDialogClosed(
        object? sender,
        EventArgs e)
    {
        if (languageDataDialog != null)
        {
            languageDataDialog.SelectionRequested -=
                OnLanguageDataSelectionRequested;
            languageDataDialog.Closed -=
                OnLanguageDataDialogClosed;
        }

        languageDataDialog = null;
        languageDataDialogViewModel = null;
    }

    private void RefreshLanguageDataPresentation()
    {
        LocalizationStatus =
            CreateLanguageDataStatus(
                languageDataService.CurrentState);

        OnPropertyChanged(
            nameof(ShouldShowLanguageDataSetup));
        OnPropertyChanged(
            nameof(LanguageDataSetupMessage));
        OnPropertyChanged(
            nameof(Entries));
        OnPropertyChanged(
            nameof(HasVisibleSettings));

        NotifySelectedSettingPresentationChanged();
        RefreshSearchResults();
        RefreshChangeSummaryViewModel();
    }

    private static string CreateLanguageDataStatus(
        LanguageDataState state)
    {
        if (state.IsAvailable)
        {
            return
                $"Language Data: " +
                $"{state.Metadata!.LanguageCode}";
        }

        return state.Availability ==
            LanguageDataAvailability.Invalid
            ? "Language Data: invalid"
            : "Language Data: unavailable";
    }

    private static LanguageDataService
        CreateDefaultLanguageDataService(
            LocalizationService localizationService)
    {
        LanguageDataService service =
            new(localizationService);

        service.LoadCanonical();

        return service;
    }

    private void ShowAbout()
    {
        messageDialogService.ShowInformation(
            $"Wartales Editor" +
            Environment.NewLine +
            $"Version {ApplicationVersion}" +
            Environment.NewLine + Environment.NewLine +
            "A companion application for safely customizing Wartales game data." +
            Environment.NewLine + Environment.NewLine +
            "Wartales Editor is an unofficial community project and is not affiliated with or endorsed by Shiro Games.",
            "About Wartales Editor");
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
        OnPropertyChanged(
            nameof(CanResetSelectedProperty));
        NotifySelectedSettingPresentationChanged();

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

        int modifiedCount = Project == null
            ? 0
            : effectiveChangeCountService.Calculate(Project);

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
        OnPropertyChanged(
            nameof(CanResetSelectedProperty));
        NotifySelectedSettingPresentationChanged();

        RefreshCommandStates();
    }

    private void NotifySelectedSettingPresentationChanged()
    {
        OnPropertyChanged(
            nameof(HasSelectedSetting));
        OnPropertyChanged(
            nameof(SelectedSettingTitle));
        OnPropertyChanged(
            nameof(SelectedSettingContext));
        OnPropertyChanged(
            nameof(SelectedSettingModifiedCount));
        OnPropertyChanged(
            nameof(SelectedSettingModificationStatus));
    }

    private void RefreshCommandStates()
    {
        OpenCommand?.NotifyCanExecuteChanged();
        ImportFromWartalesCommand?
            .NotifyCanExecuteChanged();
        SaveCommand?.NotifyCanExecuteChanged();

        NavigateSearchResultCommand?
            .NotifyCanExecuteChanged();

        ClearSearchCommand?
            .NotifyCanExecuteChanged();

        ResetSelectedPropertyCommand?
            .NotifyCanExecuteChanged();

        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();

        ShowChangeSummaryCommand?
            .NotifyCanExecuteChanged();

        CheckCompatibilityCommand?
            .NotifyCanExecuteChanged();

        ShowProfileManagerCommand?
            .NotifyCanExecuteChanged();

        ShowLanguageDataCommand?
            .NotifyCanExecuteChanged();

        ShowGoldenCdbCommand?
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

        GameplayPresetCommand?
            .NotifyCanExecuteChanged();

        RandomTraitExclusionsCommand?
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

    internal bool IsUpdateCompatibilityWindowOpen =>
        updateCompatibilityWindow != null;

    internal UpdateCompatibilityReport CheckCurrentProjectCompatibility()
    {
        ProjectModel currentProject = Project
            ?? throw new InvalidOperationException(
                "A project must be loaded before compatibility can be checked.");

        SourceGenerationTransition transition =
            currentProject.UpdateCompatibilityReport?.Transition
            ?? currentProject.SourceProvenanceStatus switch
            {
                SourceProvenanceStatus.ContentMismatch =>
                    SourceGenerationTransition.ExternalContentMismatch,
                SourceProvenanceStatus.Unknown =>
                    SourceGenerationTransition.CurrentSourceGenerationUnknown,
                _ => SourceGenerationTransition.SameSourceGeneration
            };

        UpdateCompatibilityReport report =
            new UpdateCompatibilityReportService().Create(
                currentProject,
                transition);

        currentProject.SetUpdateCompatibilityReport(report);
        return report;
    }

    private void CheckCompatibility()
    {
        UpdateCompatibilityReport report =
            CheckCurrentProjectCompatibility();

        ShowUpdateCompatibility(report);
        Status = report.HasIssues
            ? $"Compatibility check completed: {report.ResultSummary}"
            : "Compatibility check completed. No issues were detected.";
    }

    private void ShowUpdateCompatibility(
        UpdateCompatibilityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (updateCompatibilityWindow != null)
        {
            updateCompatibilityWindow.DataContext = report;
            RestoreAndActivateWindow(updateCompatibilityWindow);
            return;
        }

        updateCompatibilityWindow = new UpdateCompatibilityWindow
        {
            Owner = GetMainWindowOwner(),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DataContext = report
        };
        updateCompatibilityWindow.Closed += OnUpdateCompatibilityClosed;
        ShowFeatureWindow(updateCompatibilityWindow);
    }

    private void OnUpdateCompatibilityClosed(object? sender, EventArgs e)
    {
        if (updateCompatibilityWindow != null)
            updateCompatibilityWindow.Closed -= OnUpdateCompatibilityClosed;
        updateCompatibilityWindow = null;
    }

    private void ShowChangeSummary()
    {
        if (Project == null)
            return;

        if (changeSummaryWindow != null)
        {
            RestoreAndActivateWindow(
                changeSummaryWindow);
            return;
        }

        changeSummaryViewModel =
            new ChangeSummaryViewModel(
                BuildChangeSummaryItems(),
                NavigateToChangeSummaryItem);

        changeSummaryWindow =
            new ChangeSummaryWindow
            {
                Owner =
                    GetMainWindowOwner(),
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,
                DataContext =
                    changeSummaryViewModel
            };

        changeSummaryWindow.Closed +=
            OnChangeSummaryWindowClosed;

        ShowFeatureWindow(changeSummaryWindow);
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

            RestoreAndActivateWindow(
                profileManagerWindow);
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
                Owner =
                    GetMainWindowOwner(),
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,
                DataContext =
                    profileManagerViewModel
            };

        profileManagerWindow.Closed +=
            OnProfileManagerWindowClosed;

        ShowFeatureWindow(profileManagerWindow);

        Status =
            "Profiles opened.";
    }

    private static Window GetMainWindowOwner()
    {
        return Application.Current?.MainWindow
            ?? throw new InvalidOperationException(
                "The main application window is not available.");
    }

    private static void RestoreAndActivateWindow(
        Window window)
    {
        ArgumentNullException.ThrowIfNull(
            window);

        if (window.WindowState ==
            WindowState.Minimized)
        {
            window.WindowState =
                WindowState.Normal;
        }

        window.Show();
        window.Activate();
        window.Focus();
    }

    private static void ShowFeatureWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        Window owner = window.Owner ?? GetMainWindowOwner();
        WindowState ownerState = owner.WindowState == WindowState.Minimized
            ? WindowState.Normal
            : owner.WindowState;
        EventHandler? restoreOwner = null;
        restoreOwner = (_, _) =>
        {
            window.Closed -= restoreOwner;
            owner.Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() =>
                {
                    if (owner.Dispatcher.HasShutdownStarted || !owner.IsLoaded)
                        return;
                    if (!owner.IsVisible)
                        owner.Show();
                    if (owner.WindowState == WindowState.Minimized)
                        owner.WindowState = ownerState;
                    owner.Activate();
                    owner.Focus();
                }));
        };

        window.Closed += restoreOwner;
        try
        {
            window.Show();
            window.Activate();
        }
        catch
        {
            window.Closed -= restoreOwner;
            throw;
        }
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

            case ProfileManagerOperation.Update:
                UpdateProfile(request);
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
                "Open a Wartales file before creating a profile.",
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
                $"Profile created: {createdProfile.Name}";

        }
        catch (Exception exception)
        {
            Status =
                "Profile creation failed.";

            messageDialogService.ShowError(
                $"The profile could not be created." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Details: {exception.Message}",
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
                $"Profile renamed: {renamedProfile.Name}";

        }
        catch (Exception exception)
        {
            Status =
                "Profile rename failed.";

            messageDialogService.ShowError(
                $"The profile could not be renamed." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Details: {exception.Message}",
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
                $"Profile duplicated: {duplicatedProfile.Name}";

        }
        catch (Exception exception)
        {
            Status =
                "Profile duplication failed.";

            messageDialogService.ShowError(
                $"The profile could not be duplicated." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Details: {exception.Message}",
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
                "Open a Wartales file before applying a profile.",
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
                "Review the opened Wartales file and try again." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Details: {exception.Message}",
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
        ActivateDetailedEditorWorkspace();

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

        ActivateDetailedEditorWorkspace();

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
                    "Select a changed value " +
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
            $"Restored original value: {propertyName}";
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

        ActivateDetailedEditorWorkspace();

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

    private void UpdateProfile(
        ProfileManagerRequestModel request)
    {
        if (Project == null)
        {
            messageDialogService.ShowWarning(
                "Open a Wartales file before updating a profile.",
                "Update Profile");
            Status = "Profile update requires an open project.";
            return;
        }

        ModProfileSummaryModel? selectedProfile = request.Profile;
        if (selectedProfile == null)
        {
            return;
        }

        try
        {
            ModProfileModel existingProfile =
                modProfileLibraryService.LoadProfile(selectedProfile);
            ModProfileModel updatedProfile =
                modProfileWorkflowService.CreateUpdatedProfile(
                    Project,
                    existingProfile,
                    ApplicationVersion);
            ModProfileSummaryModel updatedSummary =
                modProfileLibraryService.UpdateProfile(
                    selectedProfile,
                    updatedProfile,
                    candidate =>
                        modProfileWorkflowService
                            .ValidateUpdatedProfileCandidate(
                                Project,
                                existingProfile,
                                candidate));

            profileManagerViewModel?.ReportProfileUpdated(
                updatedSummary.FilePath);
            Status = $"Profile updated: {updatedSummary.Name}";
        }
        catch (Exception exception)
        {
            Status = "Profile update failed.";
            messageDialogService.ShowError(
                "The selected profile could not be updated." +
                Environment.NewLine + Environment.NewLine +
                "The existing profile was not changed." +
                Environment.NewLine + Environment.NewLine +
                $"Details: {exception.Message}",
                "Update Profile");
        }
    }

    private void ActivateDetailedEditorWorkspace()
    {
        ActivateWorkspace(
            MainWorkspace.DetailedEditor);
    }

    private void ActivateWorkspace(
        MainWorkspace workspace)
    {
        if (ActiveWorkspace != workspace)
        {
            ActiveWorkspace =
                workspace;

            return;
        }

        OnPropertyChanged(
            nameof(IsGameplayToolsWorkspace));
        OnPropertyChanged(
            nameof(IsDetailedEditorWorkspace));
        OnPropertyChanged(
            nameof(IsGameplayToolsAvailable));
        OnPropertyChanged(
            nameof(IsDetailedEditorAvailable));
    }
}
