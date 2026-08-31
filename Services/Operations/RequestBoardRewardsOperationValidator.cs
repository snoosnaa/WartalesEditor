using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class RequestBoardRewardsOperationValidator
    : IProjectOperationValidator
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        if (operation is not RequestBoardRewardsOperation rewardsOperation)
        {
            return OperationValidationResult.Failure(
                "The Request Board Rewards validator received an unsupported operation.");
        }

        List<string> errors = new();
        try
        {
            RequestBoardRewardTargets targets =
                RequestBoardRewardsService.ResolveTargets(project);
            HashSet<PropertyModel> allowed = new()
            {
                targets.Minimum.Property,
                targets.Maximum.Property
            };

            foreach (PropertyModel property in
                     mutationResult.UpdatedProperties)
            {
                if (property.SourceProperty == null ||
                    !allowed.Contains(property))
                {
                    errors.Add(
                        "An unrelated project value was changed.");
                }
            }

            if (mutationResult.UpdatedProperties.Distinct().Count() >
                allowed.Count)
            {
                errors.Add(
                    "Request Board Rewards changed too many project values.");
            }

            if (mutationResult.CreatedEntries.Count != 0 ||
                mutationResult.CreatedProperties.Count != 0 ||
                mutationResult.RemovedProperties.Count != 0 ||
                mutationResult.CreatedJsonPropertyRollbackRecords.Count != 0)
            {
                errors.Add(
                    "Request Board Rewards unexpectedly changed project structure.");
            }

            GameplayOperationStateModel? state =
                project.GameplayOperationStates.SingleOrDefault(candidate =>
                    candidate.OperationType ==
                    ProgressionType.RequestBoardRewards);
            if (mutationResult.WasModified)
            {
                if (state == null)
                {
                    errors.Add(
                        "The Request Board Rewards selection was not recorded.");
                }
                else
                {
                    RequestBoardRewardsService.ValidateState(project, state);
                    int expectedPercentage = rewardsOperation.RestorePreviousValues
                        ? 100
                        : rewardsOperation.Percentage;
                    if (state.AppliedPercentage != expectedPercentage)
                    {
                        errors.Add(
                            "The Request Board Rewards selection was not recorded correctly.");
                    }
                }

                if (mutationResult.GameplayOperationStateRollbackRecords.Count != 1)
                {
                    errors.Add(
                        "The Request Board Rewards selection was not recorded atomically.");
                }
            }
            else if (mutationResult.GameplayOperationStateRollbackRecords.Count != 0)
            {
                errors.Add(
                    "Request Board Rewards recorded an unexpected state change.");
            }
            else if (state != null)
            {
                RequestBoardRewardsService.ValidateState(project, state);
            }
        }
        catch (Exception exception)
        {
            errors.Add(exception.Message);
        }

        return errors.Count == 0
            ? OperationValidationResult.Success()
            : OperationValidationResult.Failure(errors);
    }
}
