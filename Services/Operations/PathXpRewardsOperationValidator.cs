using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class PathXpRewardsOperationValidator : IProjectOperationValidator
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        if (operation is not PathXpRewardsOperation pathOperation)
            return OperationValidationResult.Failure("The Path XP Rewards validator received an unsupported operation.");
        List<string> errors = new();
        try
        {
            PathXpRewardTargets targets = PathXpRewardsService.ResolveTargets(project, pathOperation.PathId);
            HashSet<PropertyModel> allowed = targets.Targets.Select(target => target.Property).ToHashSet();
            if (mutationResult.UpdatedProperties.Any(property =>
                    property.SourceProperty == null || !allowed.Contains(property)))
                errors.Add("An unrelated project value was changed.");
            if (mutationResult.UpdatedProperties.Distinct().Count() > allowed.Count)
                errors.Add("Path XP Rewards changed too many project values.");
            if (mutationResult.CreatedEntries.Count != 0 || mutationResult.CreatedProperties.Count != 0 ||
                mutationResult.RemovedProperties.Count != 0 || mutationResult.CreatedJsonPropertyRollbackRecords.Count != 0)
                errors.Add("Path XP Rewards unexpectedly changed project structure.");

            ProgressionType type = PathXpRewardsService.GetOperationType(pathOperation.PathId);
            GameplayOperationStateModel? state = project.GameplayOperationStates.SingleOrDefault(
                candidate => candidate.OperationType == type);
            if (mutationResult.WasModified)
            {
                if (state == null) errors.Add("The Path XP Rewards selection was not recorded.");
                else
                {
                    PathXpRewardsService.ValidateState(project, state);
                    int expected = pathOperation.RestorePreviousValues ? 1 : pathOperation.Multiplier;
                    if (state.GameplaySettings?.Value<int>("multiplier") != expected)
                        errors.Add("The Path XP Rewards selection was not recorded correctly.");
                }
                if (mutationResult.GameplayOperationStateRollbackRecords.Count != 1)
                    errors.Add("The Path XP Rewards selection was not recorded atomically.");
            }
            else if (mutationResult.GameplayOperationStateRollbackRecords.Count != 0)
                errors.Add("Path XP Rewards recorded an unexpected state change.");
            else if (state != null)
                PathXpRewardsService.ValidateState(project, state);
        }
        catch (Exception exception) { errors.Add(exception.Message); }
        return errors.Count == 0
            ? OperationValidationResult.Success()
            : OperationValidationResult.Failure(errors);
    }
}
