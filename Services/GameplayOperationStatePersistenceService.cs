using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class GameplayOperationStatePersistenceService
{
    public const string SidecarExtension =
        ".wtstate";

    private static readonly JsonSerializerSettings
        serializerSettings =
            new()
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                MetadataPropertyHandling =
                    MetadataPropertyHandling.Ignore
            };

    private readonly GameplayOperationStateService
        stateService;

    public GameplayOperationStatePersistenceService()
        : this(
            new GameplayOperationStateService())
    {
    }

    public GameplayOperationStatePersistenceService(
        GameplayOperationStateService stateService)
    {
        ArgumentNullException.ThrowIfNull(stateService);
        this.stateService = stateService;
    }

    public string GetSidecarPath(
        string cdbFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cdbFileName);
        return Path.GetFullPath(cdbFileName) + SidecarExtension;
    }

    public void LoadIntoProject(
        ProjectModel project,
        string cdbFileName)
    {
        ArgumentNullException.ThrowIfNull(project);

        string sidecarPath =
            GetSidecarPath(cdbFileName);

        if (!File.Exists(sidecarPath))
        {
            return;
        }

        try
        {
            string json =
                File.ReadAllText(
                    sidecarPath,
                    Encoding.UTF8);

            GameplayOperationStateFileModel? stateFile =
                JsonConvert.DeserializeObject<
                    GameplayOperationStateFileModel>(
                    json,
                    serializerSettings);

            if (stateFile == null ||
                stateFile.FormatVersion !=
                    GameplayOperationStateFileModel
                        .CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    "The gameplay-operation state format is not supported.");
            }

            GameplayOperationStateModel[] operations =
                (stateFile.Operations ?? new())
                    .ToArray();

            if (operations
                    .GroupBy(state => state.OperationType)
                    .Any(group => group.Count() > 1))
            {
                throw new InvalidDataException(
                    "The gameplay-operation state contains duplicate " +
                    "operation identities.");
            }

            foreach (GameplayOperationStateModel state in operations)
            {
                ValidateSerializedState(state);

                project.GameplayOperationStates.Add(
                    state.DeepClone());
            }

            stateService.ValidateProjectStates(project);
            stateService.AcceptCurrentStates(project);

            foreach (GameplayOperationStateModel state in
                     project.GameplayOperationStates
                         .Where(state => !state.IsCompatible))
            {
                project.GameplayOperationStateWarnings.Add(
                    state.CompatibilityMessage);
            }
        }
        catch (Exception exception)
        {
            project.GameplayOperationStates.Clear();

            project.GameplayOperationStateWarnings.Add(
                $"Gameplay-operation state could not be loaded from " +
                $"'{sidecarPath}': {exception.Message}");
        }
    }

    private static void ValidateSerializedState(
        GameplayOperationStateModel state)
    {
        if (state == null ||
            state.FormatVersion !=
                GameplayOperationStateModel.CurrentFormatVersion ||
            !Enum.IsDefined(state.OperationType) ||
            string.IsNullOrWhiteSpace(state.TargetSheet) ||
            string.IsNullOrWhiteSpace(state.TargetEntry) ||
            string.IsNullOrWhiteSpace(state.TargetPath) ||
            state.BaselineArray == null ||
            state.ElementCount <= 0 ||
            string.IsNullOrWhiteSpace(state.BaselineFingerprint) ||
            string.IsNullOrWhiteSpace(state.ExpectedCurrentFingerprint) ||
            string.IsNullOrWhiteSpace(state.ElementShapeFingerprint))
        {
            throw new InvalidDataException(
                "The gameplay-operation state contains an invalid " +
                "operation record.");
        }

        if (state.OperationType == ProgressionType.StartingResources)
        {
            if (state.StartingResources == null)
            {
                throw new InvalidDataException(
                    "The Starting Resources settings are missing.");
            }

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
            ProgressionScalingService.ValidatePercentage(
                state.AppliedPercentage);
        }
    }

    public void Save(
        ProjectModel project,
        string cdbFileName)
    {
        string sidecarPath =
            GetSidecarPath(cdbFileName);

        string temporaryPath =
            WriteTemporary(project, cdbFileName);

        try
        {
            CommitTemporary(
                temporaryPath,
                sidecarPath);

            stateService.AcceptCurrentStates(project);
        }
        catch
        {
            TryDeleteTemporary(temporaryPath);
            throw;
        }
    }

    internal string WriteTemporary(
        ProjectModel project,
        string cdbFileName)
    {
        ArgumentNullException.ThrowIfNull(project);

        stateService.ValidateProjectStates(project);

        GameplayOperationStateFileModel stateFile =
            new()
            {
                SourceFileName =
                    Path.GetFileName(cdbFileName),

                Operations =
                    project.GameplayOperationStates
                        .Select(state => state.DeepClone())
                        .ToList()
            };

        string json =
            JsonConvert.SerializeObject(
                stateFile,
                serializerSettings);

        string sidecarPath =
            GetSidecarPath(cdbFileName);

        string? directory =
            Path.GetDirectoryName(sidecarPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath =
            sidecarPath + ".tmp";

        File.WriteAllText(
            temporaryPath,
            json,
            new UTF8Encoding(false));

        return temporaryPath;
    }

    internal static void CommitTemporary(
        string temporaryPath,
        string destinationPath)
    {
        File.Move(
            temporaryPath,
            destinationPath,
            overwrite: true);
    }

    internal static void TryDeleteTemporary(
        string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch
        {
        }
    }

    internal void AcceptCurrentStates(ProjectModel project)
    {
        stateService.AcceptCurrentStates(project);
    }
}
