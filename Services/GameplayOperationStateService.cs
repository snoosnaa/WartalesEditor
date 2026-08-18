using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class GameplayOperationStateService
{
    private readonly ProgressionTableResolver
        tableResolver;

    public GameplayOperationStateService()
        : this(
            new ProjectMutationService())
    {
    }

    public GameplayOperationStateService(
        ProjectMutationService projectMutationService)
    {
        ArgumentNullException.ThrowIfNull(projectMutationService);
        tableResolver = new ProgressionTableResolver(projectMutationService);
    }

    public GameplayOperationStateModel AdoptCurrentBaseline(
        ProjectModel project,
        ProgressionType progressionType)
    {
        ArgumentNullException.ThrowIfNull(project);

        ProgressionTableBinding binding =
            tableResolver.Resolve(project, progressionType);

        JArray baseline =
            (JArray)binding.ArrayProperty
                .GetCurrentValueSnapshot();

        _ = binding.ReadValues(baseline);

        _ = ProgressionScalingService.ScaleValues(
            binding.ReadValues(baseline),
            progressionType,
            100);

        GameplayOperationStateModel state =
            CreateState(
                progressionType,
                binding,
                baseline,
                100,
                baseline);

        ReplaceState(project, state);
        return state.DeepClone();
    }

    public GameplayOperationStateModel CreateReplacementState(
        ProjectModel project,
        ProgressionType progressionType,
        int appliedPercentage,
        JArray expectedCurrentArray)
    {
        GameplayOperationStateModel existing =
            GetRequiredCompatibleState(project, progressionType);

        ProgressionTableBinding binding =
            tableResolver.Resolve(project, progressionType);

        return CreateState(
            progressionType,
            binding,
            existing.BaselineArray,
            appliedPercentage,
            expectedCurrentArray);
    }

    public GameplayOperationStateModel GetRequiredCompatibleState(
        ProjectModel project,
        ProgressionType progressionType)
    {
        GameplayOperationStateModel? state =
            FindState(project, progressionType);

        if (state == null)
        {
            if (progressionType == ProgressionType.StartingResources)
            {
                throw new InvalidOperationException(
                    "Starting Resources has not been initialized for this project.");
            }

            throw new InvalidOperationException(
                $"No trusted {GetDisplayName(progressionType)} baseline " +
                "is available. Use Current Values as Baseline before " +
                "applying a percentage.");
        }

        ValidateState(project, state);

        if (!state.IsCompatible)
        {
            throw new InvalidOperationException(
                state.CompatibilityMessage);
        }

        return state;
    }

    public GameplayOperationStateModel? FindState(
        ProjectModel project,
        ProgressionType progressionType)
    {
        ArgumentNullException.ThrowIfNull(project);

        return project.GameplayOperationStates
            .FirstOrDefault(state =>
                state.OperationType == progressionType);
    }

    public bool CanRestorePreviousValues(
        ProjectModel project,
        ProgressionType progressionType)
    {
        GameplayOperationStateModel? state =
            FindState(project, progressionType);

        if (state == null)
        {
            return false;
        }

        ValidateState(project, state);
        return state.IsCompatible;
    }

    public GameplayOperationStateModel GetRequiredPreviousValuesState(
        ProjectModel project,
        ProgressionType progressionType)
    {
        GameplayOperationStateModel? state =
            FindState(project, progressionType);

        if (state == null)
        {
            throw new InvalidOperationException(
                "No previous values are available for this gameplay tool.");
        }

        ValidateState(project, state);

        if (!state.IsCompatible)
        {
            throw new InvalidOperationException(
                "The saved previous values are not compatible with this project.");
        }

        return state;
    }

    public bool IsStateModified(
        ProjectModel project,
        ProgressionType progressionType)
    {
        GameplayOperationStateModel? state =
            FindState(project, progressionType);

        return state != null &&
               (string.IsNullOrEmpty(state.PersistedStateFingerprint) ||
                !string.Equals(
                    state.PersistedStateFingerprint,
                    CreatePersistedStateFingerprint(state),
                    StringComparison.Ordinal));
    }

    public void AcceptCurrentStates(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        foreach (GameplayOperationStateModel state in
                 project.GameplayOperationStates)
        {
            state.PersistedStateFingerprint =
                CreatePersistedStateFingerprint(state);
        }
    }

    public void ReplaceState(
        ProjectModel project,
        GameplayOperationStateModel state,
        bool markModified = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(state);

        GameplayOperationStateModel? existing =
            FindState(project, state.OperationType);

        if (markModified)
        {
            state.PersistedStateFingerprint =
                existing?.PersistedStateFingerprint
                ?? string.Empty;
        }

        if (existing != null)
        {
            project.GameplayOperationStates.Remove(existing);
        }

        project.GameplayOperationStates.Add(
            state.DeepClone());

        if (markModified)
        {
            project.IsGameplayOperationStateModified = true;
        }
    }

    public void RemoveState(
        ProjectModel project,
        ProgressionType progressionType,
        bool markModified = true)
    {
        GameplayOperationStateModel? existing =
            FindState(project, progressionType);

        if (existing != null)
        {
            project.GameplayOperationStates.Remove(existing);

            if (markModified)
            {
                project.IsGameplayOperationStateModified = true;
            }
        }
    }

    public void ValidateProjectStates(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        foreach (GameplayOperationStateModel state in
                 project.GameplayOperationStates)
        {
            ValidateState(project, state);
        }
    }

    private static string CreatePersistedStateFingerprint(
        GameplayOperationStateModel state)
    {
        JObject serializedState = JObject.FromObject(state);
        return GameplayOperationFingerprintService.CreateContentFingerprint(
            serializedState);
    }

    public void RestoreSnapshotStates(
        ProjectModel project,
        IEnumerable<GameplayOperationStateModel> states)
    {
        _ = RestoreSnapshotStatesWithMutations(
            project,
            states);
    }

    public ProjectMutationResult
        RestoreSnapshotStatesWithMutations(
            ProjectModel project,
            IEnumerable<GameplayOperationStateModel> states)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(states);

        GameplayOperationStateModel[] validatedStates =
            states
                .Select(sourceState =>
                    sourceState.DeepClone())
                .ToArray();

        foreach (GameplayOperationStateModel state in
                 validatedStates)
        {
            ValidateState(project, state);

            if (!state.IsCompatible)
            {
                throw new InvalidOperationException(
                    state.CompatibilityMessage);
            }

        }

        ProjectMutationResult mutationResult =
            new();

        foreach (GameplayOperationStateModel state in
                 validatedStates)
        {
            GameplayOperationStateModel? previousState =
                FindState(
                    project,
                    state.OperationType)
                ?.DeepClone();

            bool previousModified =
                project.IsGameplayOperationStateModified;

            ReplaceState(project, state);

            mutationResult.AddGameplayOperationState(
                project,
                previousState,
                state,
                previousModified);
        }

        ValidateProjectStates(project);
        return mutationResult;
    }

    public void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            ValidateStateCore(project, state);
            state.IsCompatible = true;
            state.CompatibilityMessage = string.Empty;
        }
        catch (Exception exception)
        {
            state.IsCompatible = false;
            state.CompatibilityMessage =
                $"Saved {GetDisplayName(state.OperationType)} state " +
                $"is incompatible: {exception.Message}";
        }
    }

    private void ValidateStateCore(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        if (state.FormatVersion !=
            GameplayOperationStateModel.CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                "The state format version is not supported.");
        }

        if (state.OperationType == ProgressionType.StartingResources)
        {
            StartingResourcesService.ValidateState(project, state);
            return;
        }

        if (state.OperationType is ProgressionType.VolunteerWages
            or ProgressionType.ValourPoints
            or ProgressionType.CarryingCapacity)
        {
            PartyEconomyService.ValidateState(project, state);
            return;
        }

        if (state.OperationType ==
            ProgressionType.OverworldMovementSpeed)
        {
            OverworldMovementSpeedService.ValidateState(project, state);
            return;
        }

        if (state.OperationType ==
            ProgressionType.RainFrequency)
        {
            RainFrequencyService.ValidateState(project, state);
            return;
        }

        if (state.OperationType ==
            ProgressionType.RandomTraitExclusions)
        {
            RandomTraitExclusionsService.ValidateState(project, state);
            return;
        }

        if (GameplayPresetCatalog.IsSupported(state.OperationType))
        {
            GameplayPresetService.ValidateState(project, state);
            return;
        }

        ProgressionTableBinding binding =
            tableResolver.Resolve(project, state.OperationType);

        if (!string.Equals(state.TargetSheet, "constant", StringComparison.Ordinal) ||
            !string.Equals(state.TargetEntry, binding.Entry.Id, StringComparison.Ordinal) ||
            !string.Equals(state.TargetPath, binding.ArrayPropertyPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The target sheet, entry, or array path does not match.");
        }

        if (state.BaselineArray.Count != state.ElementCount ||
            state.ElementCount !=
                ((JArray)binding.ArrayProperty.SourceProperty!.Value).Count)
        {
            throw new InvalidOperationException(
                "The progression array element count has changed.");
        }

        if (!string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(
                    state.BaselineArray),
                state.BaselineFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The preserved baseline fingerprint is invalid.");
        }

        if (!string.Equals(
                GameplayOperationFingerprintService.CreateShapeFingerprint(
                    state.BaselineArray),
                state.ElementShapeFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The preserved baseline structure is invalid.");
        }

        IReadOnlyList<long> baselineValues =
            binding.ReadValues(state.BaselineArray);

        IReadOnlyList<long> expectedValues =
            ProgressionScalingService.ScaleValues(
                baselineValues,
                state.OperationType,
                state.AppliedPercentage);

        JArray expectedArray =
            binding.CreateArray(
                state.BaselineArray,
                expectedValues);

        if (!string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(
                    expectedArray),
                state.ExpectedCurrentFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The recorded percentage does not reproduce the " +
                "expected progression result.");
        }

        JArray currentArray =
            (JArray)binding.ArrayProperty
                .GetCurrentValueSnapshot();

        if (!string.Equals(
                GameplayOperationFingerprintService.CreateShapeFingerprint(
                    currentArray),
                state.ElementShapeFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The loaded progression structure has changed.");
        }

        if (!string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(
                    currentArray),
                state.ExpectedCurrentFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The loaded progression values do not match the " +
                "recorded percentage result.");
        }
    }

    private static GameplayOperationStateModel CreateState(
        ProgressionType progressionType,
        ProgressionTableBinding binding,
        JArray baseline,
        int appliedPercentage,
        JArray expectedCurrentArray)
    {
        return new GameplayOperationStateModel
        {
            OperationType = progressionType,
            TargetSheet = "constant",
            TargetEntry = binding.Entry.Id,
            TargetPath = binding.ArrayPropertyPath,
            BaselineArray = (JArray)baseline.DeepClone(),
            AppliedPercentage = appliedPercentage,
            BaselineFingerprint =
                GameplayOperationFingerprintService
                    .CreateContentFingerprint(baseline),
            ExpectedCurrentFingerprint =
                GameplayOperationFingerprintService
                    .CreateContentFingerprint(expectedCurrentArray),
            ElementCount = baseline.Count,
            ElementShapeFingerprint =
                GameplayOperationFingerprintService
                    .CreateShapeFingerprint(baseline),
            ProjectCompatibilityIdentity = string.Empty,
            IsCompatible = true
        };
    }

    private static string GetDisplayName(
        ProgressionType progressionType)
    {
        return progressionType switch
        {
            ProgressionType.Character => "Character XP",
            ProgressionType.Profession => "Profession XP",
            ProgressionType.StartingResources => "Starting Resources",
            ProgressionType.VolunteerWages => "Volunteer Wage Reduction",
            ProgressionType.ValourPoints => "Valour Points",
            ProgressionType.CarryingCapacity => "Carrying Capacity",
            ProgressionType.OverworldMovementSpeed =>
                "Overworld Movement Speed",
            ProgressionType.RainFrequency =>
                "Rain Frequency",
            _ when GameplayPresetCatalog.IsSupported(progressionType) =>
                GameplayPresetCatalog.Get(progressionType).Title,
            _ => "gameplay operation"
        };
    }
}
