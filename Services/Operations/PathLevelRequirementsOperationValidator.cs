using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class PathLevelRequirementsOperationValidator : IProjectOperationValidator
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        if (operation is not PathLevelRequirementsOperation pathOperation)
            return OperationValidationResult.Failure("The Path Level Requirements validator received an unsupported operation.");
        List<string> errors = new();
        try
        {
            PathLevelRequirementTargets targets = PathLevelRequirementsService.ResolveTargets(project);
            HashSet<PropertyModel> allowed = [targets.Base.Property, targets.Next.Property];
            if (mutationResult.UpdatedProperties.Any(property =>
                    property.SourceProperty == null || !allowed.Contains(property)))
                errors.Add("An unrelated project value was changed.");
            if (mutationResult.UpdatedProperties.Distinct().Count() > 2)
                errors.Add("Path Level Requirements changed too many project values.");
            ValidateNoStructureChanges(mutationResult, errors);
            ValidateStateResult(project, mutationResult, pathOperation, errors);
        }
        catch (Exception exception) { errors.Add(exception.Message); }
        return errors.Count == 0
            ? OperationValidationResult.Success()
            : OperationValidationResult.Failure(errors);
    }

    private static void ValidateStateResult(
        ProjectModel project,
        ProjectMutationResult result,
        PathLevelRequirementsOperation operation,
        List<string> errors)
    {
        GameplayOperationStateModel? state = project.GameplayOperationStates.SingleOrDefault(
            candidate => candidate.OperationType == ProgressionType.PathLevelRequirements);
        if (result.WasModified)
        {
            if (state == null) errors.Add("The Path Level Requirements selection was not recorded.");
            else
            {
                PathLevelRequirementsService.ValidateState(project, state);
                int expected = operation.RestorePreviousValues ? 100 : operation.Percentage;
                if (state.GameplaySettings?.Value<int>("percentage") != expected)
                    errors.Add("The Path Level Requirements selection was not recorded correctly.");
            }
            if (result.GameplayOperationStateRollbackRecords.Count != 1)
                errors.Add("The Path Level Requirements selection was not recorded atomically.");
        }
        else if (result.GameplayOperationStateRollbackRecords.Count != 0)
            errors.Add("Path Level Requirements recorded an unexpected state change.");
        else if (state != null)
            PathLevelRequirementsService.ValidateState(project, state);
    }

    private static void ValidateNoStructureChanges(ProjectMutationResult result, List<string> errors)
    {
        if (result.CreatedEntries.Count != 0 || result.CreatedProperties.Count != 0 ||
            result.RemovedProperties.Count != 0 || result.CreatedJsonPropertyRollbackRecords.Count != 0)
            errors.Add("Path Level Requirements unexpectedly changed project structure.");
    }
}
