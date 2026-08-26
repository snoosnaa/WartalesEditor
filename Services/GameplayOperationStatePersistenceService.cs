using System.IO;
using System.Text;
using Newtonsoft.Json;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class GameplayOperationStatePersistenceService
{
    public const string SidecarExtension = ".wtstate";

    private static readonly JsonSerializerSettings serializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore
    };

    private readonly GameplayOperationStateService stateService;
    private readonly CdbGenerationIdentityService identityService;

    public GameplayOperationStatePersistenceService()
        : this(new GameplayOperationStateService(), new CdbGenerationIdentityService())
    {
    }

    public GameplayOperationStatePersistenceService(
        GameplayOperationStateService stateService)
        : this(stateService, new CdbGenerationIdentityService())
    {
    }

    public GameplayOperationStatePersistenceService(
        GameplayOperationStateService stateService,
        CdbGenerationIdentityService identityService)
    {
        this.stateService = stateService ??
            throw new ArgumentNullException(nameof(stateService));
        this.identityService = identityService ??
            throw new ArgumentNullException(nameof(identityService));
    }

    public string GetSidecarPath(string cdbFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cdbFileName);
        return Path.GetFullPath(cdbFileName) + SidecarExtension;
    }

    public void LoadIntoProject(ProjectModel project, string cdbFileName)
    {
        ArgumentNullException.ThrowIfNull(project);

        string actualIdentity = project.CurrentCdbContentIdentity;
        project.EstablishPersistedIdentity(
            actualIdentity, null, SourceProvenanceStatus.Unknown);

        string sidecarPath = GetSidecarPath(cdbFileName);
        if (!File.Exists(sidecarPath))
            return;

        try
        {
            GameplayOperationStateFileModel stateFile = ReadStateFile(sidecarPath);

            if (stateFile.FormatVersion ==
                GameplayOperationStateFileModel.LegacyFormatVersion)
            {
                RetainAsUnknownHistory(project, stateFile.Operations);
                project.RequiresGameplayStateManifestMigration = true;
                project.GameplayOperationStateWarnings.Add(
                    "Previous restore information uses an older format and could not be verified. It was retained as non-restorable history.");
                return;
            }

            if (stateFile.FormatVersion !=
                GameplayOperationStateFileModel.CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    "The gameplay-operation state format is not supported.");
            }

            ValidateCollection(stateFile.Operations, allowUnknownProvenance: true);
            ValidateCollection(stateFile.HistoricalOperations, allowUnknownProvenance: true);

            if (!identityService.IsValid(stateFile.CurrentCdbContentIdentity) ||
                !identityService.AreEqual(
                    actualIdentity, stateFile.CurrentCdbContentIdentity))
            {
                RetainAsUnknownHistory(project, stateFile.HistoricalOperations);
                RetainAsUnknownHistory(project, stateFile.Operations);
                project.EstablishPersistedIdentity(
                    actualIdentity, null, SourceProvenanceStatus.ContentMismatch);
                project.GameplayOperationStateWarnings.Add(
                    "Previous restore information could not be verified because the data file differs from the revision recorded by its settings file.");
                return;
            }

            string? sourceIdentity =
                identityService.IsValid(stateFile.SourceCdbGenerationIdentity)
                    ? stateFile.SourceCdbGenerationIdentity
                    : null;

            project.EstablishPersistedIdentity(
                actualIdentity,
                sourceIdentity,
                sourceIdentity == null
                    ? SourceProvenanceStatus.Unknown
                    : SourceProvenanceStatus.Verified);

            if (sourceIdentity == null)
            {
                RetainAsUnknownHistory(project, stateFile.HistoricalOperations);
                RetainAsUnknownHistory(project, stateFile.Operations);
                stateService.AcceptCurrentStates(project);
                return;
            }

            RetainAsHistory(project, stateFile.HistoricalOperations);

            foreach (GameplayOperationStateModel state in stateFile.Operations)
            {
                if (!identityService.AreEqual(
                        state.ProjectCompatibilityIdentity, sourceIdentity))
                {
                    RetainAsUnknownHistory(project, new[] { state });
                    project.GameplayOperationStateWarnings.Add(
                        "Previous restore information contained inconsistent source provenance and was retained without restoration authority.");
                    continue;
                }

                project.GameplayOperationStates.Add(state.DeepClone());
            }

            stateService.ValidateProjectStates(project);
            foreach (GameplayOperationStateModel state in
                     project.GameplayOperationStates.ToArray())
            {
                if (!state.IsCompatible)
                {
                    project.GameplayOperationStates.Remove(state);
                    AddHistorical(project, state);
                    project.GameplayOperationStateWarnings.Add(
                        state.CompatibilityMessage);
                }
            }

            stateService.AcceptCurrentStates(project);
        }
        catch (Exception exception)
        {
            project.GameplayOperationStates.Clear();
            project.HistoricalGameplayOperationStates.Clear();
            project.EstablishPersistedIdentity(
                actualIdentity, null, SourceProvenanceStatus.Unknown);
            project.RequiresUnverifiedGameplayStateNotice = true;
            project.GameplayOperationStateWarnings.Add(
                $"Gameplay-operation state could not be loaded from '{sidecarPath}': {exception.Message}");
        }
    }

    internal GameplayStateManifestSnapshot CaptureForReplacement(string cdbFileName)
    {
        string sidecarPath = GetSidecarPath(cdbFileName);
        if (!File.Exists(cdbFileName))
            return GameplayStateManifestSnapshot.NoPriorCanonical;

        if (!File.Exists(sidecarPath))
            return GameplayStateManifestSnapshot.NoSidecar;

        string actual = identityService.Calculate(File.ReadAllBytes(cdbFileName));
        try
        {
            GameplayOperationStateFileModel stateFile = ReadStateFile(sidecarPath);
            if (stateFile.FormatVersion ==
                GameplayOperationStateFileModel.LegacyFormatVersion)
            {
                return new GameplayStateManifestSnapshot(
                    PriorGameplayStateStatus.LegacyManifest,
                    null,
                    false,
                    stateFile.Operations.Select(x => x.DeepClone()).ToArray(),
                    stateFile.HistoricalOperations.Select(x => x.DeepClone()).ToArray());
            }

            if (stateFile.FormatVersion !=
                GameplayOperationStateFileModel.CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    "The gameplay-operation state format is not supported.");
            }

            ValidateCollection(stateFile.Operations, allowUnknownProvenance: true);
            ValidateCollection(stateFile.HistoricalOperations, allowUnknownProvenance: true);

            bool bound =
                identityService.AreEqual(actual, stateFile.CurrentCdbContentIdentity);
            string? source = bound &&
                identityService.IsValid(stateFile.SourceCdbGenerationIdentity)
                    ? stateFile.SourceCdbGenerationIdentity
                    : null;

            PriorGameplayStateStatus status = !bound
                ? PriorGameplayStateStatus.ContentMismatch
                : source == null
                    ? PriorGameplayStateStatus.ValidManifestUnknownSource
                    : PriorGameplayStateStatus.ValidVerifiedManifest;

            return new GameplayStateManifestSnapshot(
                status,
                source,
                bound,
                stateFile.Operations.Select(x => x.DeepClone()).ToArray(),
                stateFile.HistoricalOperations.Select(x => x.DeepClone()).ToArray());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return GameplayStateManifestSnapshot.UnreadableManifest;
        }
        catch
        {
            return GameplayStateManifestSnapshot.MalformedManifest;
        }
    }

    internal void ApplyAuthoritativeImport(
        ProjectModel project,
        string sourceIdentity,
        GameplayStateManifestSnapshot previous)
    {
        if (!identityService.IsValid(sourceIdentity))
            throw new ArgumentException("The source identity is not valid.", nameof(sourceIdentity));

        project.GameplayOperationStates.Clear();
        project.HistoricalGameplayOperationStates.Clear();
        project.SetUpdateCompatibilityReport(null);
        project.EstablishPersistedIdentity(
            project.CurrentCdbContentIdentity,
            sourceIdentity,
            SourceProvenanceStatus.Verified);

        GameplayOperationStateModel[] verifiedActive =
            previous.HasVerifiedSourceProvenance
                ? previous.ActiveOperations.Where(state =>
                    identityService.AreEqual(
                        state.ProjectCompatibilityIdentity,
                        previous.SourceIdentity)).ToArray()
                : Array.Empty<GameplayOperationStateModel>();
        GameplayOperationStateModel[] untrustedActive =
            previous.ActiveOperations.Except(verifiedActive).ToArray();

        RetainAsUnknownHistory(project, untrustedActive);

        if (previous.HasVerifiedSourceProvenance &&
            identityService.AreEqual(previous.SourceIdentity, sourceIdentity))
        {
            RetainAsHistory(project, previous.HistoricalOperations);
            ReactivateCompatibleStates(
                project,
                previous.HistoricalOperations,
                sourceIdentity);
            ReactivateCompatibleStates(
                project,
                verifiedActive,
                sourceIdentity);
        }
        else if (previous.HasVerifiedSourceProvenance)
        {
            RetainAsHistory(project, previous.HistoricalOperations);
            RetainAsHistory(project, verifiedActive);
            ReactivateCompatibleStates(
                project,
                previous.HistoricalOperations,
                sourceIdentity);
        }
        else
        {
            RetainAsUnknownHistory(project, previous.HistoricalOperations);
            RetainAsUnknownHistory(project, previous.ActiveOperations);
        }

        stateService.AcceptCurrentStates(project);
    }

    private void ReactivateCompatibleStates(
        ProjectModel project,
        IEnumerable<GameplayOperationStateModel> states,
        string sourceIdentity)
    {
        foreach (GameplayOperationStateModel prior in states)
        {
            if (!identityService.AreEqual(
                    prior.ProjectCompatibilityIdentity,
                    sourceIdentity))
            {
                AddHistorical(project, prior);
                continue;
            }

            GameplayOperationStateModel candidate = prior.DeepClone();
            stateService.ValidateState(project, candidate);
            if (!candidate.IsCompatible)
            {
                AddHistorical(project, candidate);
                continue;
            }

            GameplayOperationStateModel? existing =
                project.GameplayOperationStates.FirstOrDefault(state =>
                    state.OperationType == candidate.OperationType);
            if (existing != null)
                project.GameplayOperationStates.Remove(existing);
            project.GameplayOperationStates.Add(candidate);

            GameplayOperationStateModel? historical =
                project.HistoricalGameplayOperationStates.FirstOrDefault(state =>
                    state.OperationType == candidate.OperationType &&
                    identityService.AreEqual(
                        state.ProjectCompatibilityIdentity,
                        sourceIdentity));
            if (historical != null)
                project.HistoricalGameplayOperationStates.Remove(historical);
        }
    }

    public void Save(ProjectModel project, string cdbFileName)
    {
        if (!File.Exists(cdbFileName))
            throw new FileNotFoundException(
                "Gameplay-operation state cannot be saved before its CDB file exists.",
                cdbFileName);

        string actualIdentity =
            identityService.Calculate(File.ReadAllBytes(cdbFileName));
        if (!identityService.AreEqual(
                actualIdentity,
                project.CurrentCdbContentIdentity))
        {
            throw new InvalidOperationException(
                "The CDB file changed outside the editor, so its settings file was not updated.");
        }

        string sidecarPath = GetSidecarPath(cdbFileName);
        string temporaryPath = WriteTemporary(
            project, cdbFileName, project.CurrentCdbContentIdentity);

        try
        {
            CommitTemporary(temporaryPath, sidecarPath);
            stateService.AcceptCurrentStates(project);
            project.RequiresGameplayStateManifestMigration = false;
        }
        catch
        {
            TryDeleteTemporary(temporaryPath);
            throw;
        }
    }

    internal string WriteTemporary(
        ProjectModel project,
        string cdbFileName,
        string currentContentIdentity)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!identityService.IsValid(currentContentIdentity))
            throw new InvalidOperationException("The current CDB content identity is not valid.");

        stateService.ValidateProjectStates(project);
        bool hasVerifiedSource =
            project.SourceProvenanceStatus == SourceProvenanceStatus.Verified &&
            identityService.IsValid(project.SourceCdbGenerationIdentity);
        if (hasVerifiedSource)
            BindActiveStatesToSource(project);

        IEnumerable<GameplayOperationStateModel> history =
            hasVerifiedSource
                ? project.HistoricalGameplayOperationStates
                : project.HistoricalGameplayOperationStates.Concat(
                    project.GameplayOperationStates);

        GameplayOperationStateFileModel stateFile = new()
        {
            SourceFileName = Path.GetFileName(cdbFileName),
            SourceCdbGenerationIdentity = project.SourceCdbGenerationIdentity,
            CurrentCdbContentIdentity = currentContentIdentity,
            Operations = hasVerifiedSource
                ? project.GameplayOperationStates
                    .Select(state => state.DeepClone()).ToList()
                : new List<GameplayOperationStateModel>(),
            HistoricalOperations = hasVerifiedSource
                ? BoundedHistory(history)
                : BoundedUnknownHistory(history)
        };

        string json = JsonConvert.SerializeObject(stateFile, serializerSettings);
        string sidecarPath = GetSidecarPath(cdbFileName);
        string? directory = Path.GetDirectoryName(sidecarPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        string temporaryPath = sidecarPath + ".tmp";
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
        return temporaryPath;
    }

    internal string WriteTemporary(ProjectModel project, string cdbFileName)
    {
        return WriteTemporary(project, cdbFileName, project.CurrentCdbContentIdentity);
    }

    internal static void CommitTemporary(string temporaryPath, string destinationPath)
    {
        File.Move(temporaryPath, destinationPath, overwrite: true);
    }

    internal static void TryDeleteTemporary(string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
        catch
        {
        }
    }

    internal void AcceptCurrentStates(ProjectModel project)
    {
        stateService.AcceptCurrentStates(project);
    }

    private static GameplayOperationStateFileModel ReadStateFile(string sidecarPath)
    {
        string json = File.ReadAllText(sidecarPath, Encoding.UTF8);
        return JsonConvert.DeserializeObject<GameplayOperationStateFileModel>(
                   json, serializerSettings)
               ?? throw new InvalidDataException(
                   "The gameplay-operation state file is empty.");
    }

    private static void ValidateCollection(
        IEnumerable<GameplayOperationStateModel> states,
        bool allowUnknownProvenance)
    {
        GameplayOperationStateModel[] records = states.ToArray();
        if (records.GroupBy(state => state.OperationType).Any(group => group.Count() > 1))
            throw new InvalidDataException("The gameplay-operation state contains duplicate operation identities.");

        foreach (GameplayOperationStateModel state in records)
        {
            ValidateSerializedState(state);
            if (!allowUnknownProvenance &&
                string.IsNullOrWhiteSpace(state.ProjectCompatibilityIdentity))
            {
                throw new InvalidDataException("Active gameplay-operation state has no source provenance.");
            }
        }
    }

    private static void ValidateSerializedState(GameplayOperationStateModel state)
    {
        if (state == null ||
            state.FormatVersion != GameplayOperationStateModel.CurrentFormatVersion ||
            !Enum.IsDefined(state.OperationType) ||
            string.IsNullOrWhiteSpace(state.TargetSheet) ||
            string.IsNullOrWhiteSpace(state.TargetEntry) ||
            string.IsNullOrWhiteSpace(state.TargetPath) ||
            state.BaselineArray == null || state.ElementCount <= 0 ||
            string.IsNullOrWhiteSpace(state.BaselineFingerprint) ||
            string.IsNullOrWhiteSpace(state.ExpectedCurrentFingerprint) ||
            string.IsNullOrWhiteSpace(state.ElementShapeFingerprint))
        {
            throw new InvalidDataException("The gameplay-operation state contains an invalid operation record.");
        }

        if (state.OperationType == ProgressionType.StartingResources)
        {
            if (state.StartingResources == null)
                throw new InvalidDataException("The Starting Resources settings are missing.");
            state.StartingResources.Validate();
        }
        else if (state.OperationType is ProgressionType.VolunteerWages
                 or ProgressionType.ValourPoints
                 or ProgressionType.CarryingCapacity
                 or ProgressionType.OverworldMovementSpeed
                 or ProgressionType.RainFrequency
                 or ProgressionType.RandomTraitExclusions ||
                 GameplayPresetCatalog.IsSupported(state.OperationType))
        {
            if (state.GameplaySettings == null)
                throw new InvalidDataException("The gameplay settings are missing.");
        }
        else
        {
            ProgressionScalingService.ValidatePercentage(state.AppliedPercentage);
        }
    }

    private static void BindActiveStatesToSource(ProjectModel project)
    {
        foreach (GameplayOperationStateModel state in project.GameplayOperationStates)
        {
            state.ProjectCompatibilityIdentity =
                project.SourceCdbGenerationIdentity ?? string.Empty;
        }
    }

    private static void RetainAsHistory(
        ProjectModel project,
        IEnumerable<GameplayOperationStateModel> states)
    {
        foreach (GameplayOperationStateModel state in states)
            AddHistorical(project, state);
    }

    private static void RetainAsUnknownHistory(
        ProjectModel project,
        IEnumerable<GameplayOperationStateModel> states)
    {
        foreach (GameplayOperationStateModel state in states)
        {
            GameplayOperationStateModel unknown = state.DeepClone();
            unknown.ProjectCompatibilityIdentity = string.Empty;
            AddHistorical(project, unknown);
        }
    }

    private static void AddHistorical(ProjectModel project, GameplayOperationStateModel state)
    {
        GameplayOperationStateModel? existing =
            project.HistoricalGameplayOperationStates.FirstOrDefault(
                item => item.OperationType == state.OperationType);
        if (existing != null)
            project.HistoricalGameplayOperationStates.Remove(existing);
        project.HistoricalGameplayOperationStates.Add(state.DeepClone());
    }

    private static List<GameplayOperationStateModel> BoundedHistory(
        IEnumerable<GameplayOperationStateModel> states)
    {
        return states
            .GroupBy(state => state.OperationType)
            .Select(group => group.Last().DeepClone())
            .ToList();
    }

    private static List<GameplayOperationStateModel> BoundedUnknownHistory(
        IEnumerable<GameplayOperationStateModel> states)
    {
        List<GameplayOperationStateModel> bounded = BoundedHistory(states);
        foreach (GameplayOperationStateModel state in bounded)
            state.ProjectCompatibilityIdentity = string.Empty;
        return bounded;
    }
}

internal enum PriorGameplayStateStatus
{
    NoPriorCanonical,
    NoSidecar,
    ValidVerifiedManifest,
    ValidManifestUnknownSource,
    ContentMismatch,
    LegacyManifest,
    MalformedManifest,
    UnreadableManifest
}

internal sealed record GameplayStateManifestSnapshot(
    PriorGameplayStateStatus Status,
    string? SourceIdentity,
    bool HasValidContentBinding,
    IReadOnlyList<GameplayOperationStateModel> ActiveOperations,
    IReadOnlyList<GameplayOperationStateModel> HistoricalOperations)
{
    public bool HadPriorCanonical =>
        Status != PriorGameplayStateStatus.NoPriorCanonical;

    public bool HasVerifiedSourceProvenance =>
        Status == PriorGameplayStateStatus.ValidVerifiedManifest &&
        HasValidContentBinding &&
        !string.IsNullOrWhiteSpace(SourceIdentity);

    public static GameplayStateManifestSnapshot NoPriorCanonical { get; } =
        new(PriorGameplayStateStatus.NoPriorCanonical, null, false,
            Array.Empty<GameplayOperationStateModel>(),
            Array.Empty<GameplayOperationStateModel>());

    public static GameplayStateManifestSnapshot NoSidecar { get; } =
        new(PriorGameplayStateStatus.NoSidecar, null, false,
            Array.Empty<GameplayOperationStateModel>(),
            Array.Empty<GameplayOperationStateModel>());

    public static GameplayStateManifestSnapshot MalformedManifest { get; } =
        new(PriorGameplayStateStatus.MalformedManifest, null, false,
            Array.Empty<GameplayOperationStateModel>(),
            Array.Empty<GameplayOperationStateModel>());

    public static GameplayStateManifestSnapshot UnreadableManifest { get; } =
        new(PriorGameplayStateStatus.UnreadableManifest, null, false,
            Array.Empty<GameplayOperationStateModel>(),
            Array.Empty<GameplayOperationStateModel>());
}
