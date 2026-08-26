using WartalesEditor.Helpers;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace WartalesEditor.Models;

public class ProjectModel : ObservableObject
{
    private bool isModified;

    private bool isGameplayOperationStateModified;

    private string? sourceCdbGenerationIdentity;

    private string currentCdbContentIdentity = string.Empty;

    private SourceProvenanceStatus sourceProvenanceStatus;

    private UpdateCompatibilityReport? updateCompatibilityReport;

    public string FileName { get; set; } = "";

    public string OriginalJson { get; set; } = "";

    public JObject RootDocument { get; set; } = new();

    public string? SourceCdbGenerationIdentity =>
        sourceCdbGenerationIdentity;

    public string CurrentCdbContentIdentity =>
        currentCdbContentIdentity;

    public SourceProvenanceStatus SourceProvenanceStatus =>
        sourceProvenanceStatus;

    public UpdateCompatibilityReport? UpdateCompatibilityReport =>
        updateCompatibilityReport;

    public bool IsModified
    {
        get => isModified;
        set => SetProperty(ref isModified, value);
    }

    public bool IsGameplayOperationStateModified
    {
        get => isGameplayOperationStateModified;
        set => SetProperty(
            ref isGameplayOperationStateModified,
            value);
    }

    public ObservableCollection<SheetModel> Sheets { get; }
        = new();

    public ObservableCollection<GameplayOperationStateModel>
        GameplayOperationStates
    {
        get;
    } = new();

    public ObservableCollection<GameplayOperationStateModel>
        HistoricalGameplayOperationStates
    {
        get;
    } = new();

    internal bool RequiresGameplayStateManifestMigration { get; set; }

    internal bool RequiresUnverifiedGameplayStateNotice { get; set; }

    internal void EstablishPersistedIdentity(
        string currentCdbContentIdentity,
        string? sourceCdbGenerationIdentity,
        SourceProvenanceStatus provenanceStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            currentCdbContentIdentity);

        this.currentCdbContentIdentity =
            currentCdbContentIdentity;
        this.sourceCdbGenerationIdentity =
            sourceCdbGenerationIdentity;
        sourceProvenanceStatus = provenanceStatus;
    }

    internal void AdvanceCurrentContentIdentity(
        string currentCdbContentIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            currentCdbContentIdentity);
        this.currentCdbContentIdentity =
            currentCdbContentIdentity;
    }

    internal void SetUpdateCompatibilityReport(
        UpdateCompatibilityReport? report)
    {
        updateCompatibilityReport = report;
    }

    public List<string> GameplayOperationStateWarnings
    {
        get;
    } = new();

    public List<string> ProjectLoadWarnings
    {
        get;
    } = new();
}
