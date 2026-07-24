using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class ProgressionScalingService
{
    public const int MinimumPercentage = 10;

    public const int MaximumPercentage = 300;

    private const string CharacterTableId =
        "LevelXpValues";

    private const string ProfessionTableId =
        "JobXpLevels";

    private readonly ProjectMutationService
        projectMutationService;

    private readonly ProgressionTableResolver
        tableResolver;

    private readonly GameplayOperationStateService
        stateService;

    public ProgressionScalingService(
        ProjectMutationService projectMutationService)
        : this(
            projectMutationService,
            new GameplayOperationStateService(
                projectMutationService))
    {
    }

    public ProgressionScalingService(
        ProjectMutationService projectMutationService,
        GameplayOperationStateService stateService)
    {
        ArgumentNullException.ThrowIfNull(projectMutationService);
        ArgumentNullException.ThrowIfNull(stateService);

        this.projectMutationService = projectMutationService;
        this.stateService = stateService;
        tableResolver = new ProgressionTableResolver(projectMutationService);
    }

    public ProgressionScalingPreview CreatePreview(
        ProjectModel project,
        ProgressionType progressionType,
        int percentage)
    {
        ArgumentNullException.ThrowIfNull(project);
        ValidatePercentage(percentage);

        ProgressionTableBinding binding =
            tableResolver.Resolve(project, progressionType);

        GameplayOperationStateModel state =
            stateService.GetRequiredCompatibleState(
                project,
                progressionType);

        IReadOnlyList<long> baselineValues =
            binding.ReadValues(state.BaselineArray);

        IReadOnlyList<long> scaledValues =
            ScaleValues(
                baselineValues,
                progressionType,
                percentage);

        return new ProgressionScalingPreview(
            baselineValues,
            scaledValues);
    }

    public ProjectMutationResult Scale(
        ProjectModel project,
        ProgressionType progressionType,
        int percentage)
    {
        ArgumentNullException.ThrowIfNull(project);
        ValidatePercentage(percentage);

        ProgressionTableBinding binding =
            tableResolver.Resolve(project, progressionType);

        GameplayOperationStateModel previousState =
            stateService.GetRequiredCompatibleState(
                project,
                progressionType)
                .DeepClone();

        bool previousStateWasModified =
            project.IsGameplayOperationStateModified;

        IReadOnlyList<long> baselineValues =
            binding.ReadValues(previousState.BaselineArray);

        IReadOnlyList<long> scaledValues =
            ScaleValues(
                baselineValues,
                progressionType,
                percentage);

        JArray scaledArray =
            binding.CreateArray(
                previousState.BaselineArray,
                scaledValues);

        if (previousState.AppliedPercentage == percentage &&
            JToken.DeepEquals(
                binding.ArrayProperty.SourceProperty!.Value,
                scaledArray))
        {
            return new ProjectMutationResult();
        }

        GameplayOperationStateModel replacementState =
            stateService.CreateReplacementState(
                project,
                progressionType,
                percentage,
                scaledArray);

        ProjectMutationResult result =
            projectMutationService.EnsurePropertyByPath(
                binding.Entry,
                binding.ArrayPropertyPath,
                scaledArray);

        stateService.ReplaceState(
            project,
            replacementState);

        result.AddGameplayOperationState(
            project,
            previousState,
            replacementState,
            previousStateWasModified);

        return result;
    }

    internal ProgressionTableBinding ResolveProgressionTable(
        ProjectModel project,
        ProgressionType progressionType)
    {
        return tableResolver.Resolve(project, progressionType);
    }

    public static string GetTableId(
        ProgressionType progressionType)
    {
        return progressionType switch
        {
            ProgressionType.Character => CharacterTableId,
            ProgressionType.Profession => ProfessionTableId,
            _ => throw new ArgumentOutOfRangeException(
                nameof(progressionType),
                progressionType,
                "The progression type is not supported.")
        };
    }

    public static void ValidatePercentage(
        int percentage)
    {
        if (percentage < MinimumPercentage ||
            percentage > MaximumPercentage)
        {
            throw new ArgumentOutOfRangeException(
                nameof(percentage),
                percentage,
                $"Percentage must be between {MinimumPercentage}% " +
                $"and {MaximumPercentage}%.");
        }
    }

    internal static IReadOnlyList<long> ScaleValues(
        IReadOnlyList<long> baselineValues,
        ProgressionType progressionType,
        int percentage)
    {
        ValidatePercentage(percentage);

        List<long> scaledValues =
            new(baselineValues.Count);

        for (int index = 0;
             index < baselineValues.Count;
             index++)
        {
            long baselineValue = baselineValues[index];

            if (index > 0 &&
                baselineValue <= baselineValues[index - 1])
            {
                throw new InvalidOperationException(
                    $"Progression table '{GetTableId(progressionType)}' " +
                    $"is not strictly increasing at index {index}.");
            }

            if (progressionType == ProgressionType.Character &&
                index == 0)
            {
                if (baselineValue != 0)
                {
                    throw new InvalidOperationException(
                        "Character XP progression must begin with zero.");
                }

                scaledValues.Add(0);
                continue;
            }

            if (progressionType == ProgressionType.Character &&
                baselineValue <= 0)
            {
                throw new InvalidOperationException(
                    "Character XP progression contains a zero or " +
                    "negative XP requirement.");
            }

            long scaledValue = checked((long)Math.Round(
                baselineValue * (decimal)percentage / 100m,
                0,
                MidpointRounding.AwayFromZero));

            long minimumValue =
                scaledValues.Count == 0
                    ? scaledValue
                    : checked(scaledValues[^1] + 1);

            scaledValues.Add(
                Math.Max(minimumValue, scaledValue));
        }

        return scaledValues;
    }
}
