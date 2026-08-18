using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class OverworldMovementSpeedOperationValidator
    : IProjectOperationValidator
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        List<string> errors = new();
        if (operation is not OverworldMovementSpeedOperation movement)
            return OperationValidationResult.Failure(
                "The movement validator received an unsupported operation.");
        try
        {
            if (!movement.RestorePreviousValues)
            {
                OverworldMovementPresetOption preset =
                    OverworldMovementSpeedService.Presets.Single(x =>
                        x.Preset == movement.Preset);
                if (preset.WalkSpeed <= 0 ||
                    preset.RunSpeed <= 0 ||
                    preset.WalkSpeed >= preset.RunSpeed)
                    errors.Add("The selected movement preset is invalid.");
            }

            (MovementTarget walk, MovementTarget run) =
                OverworldMovementSpeedService.ResolveTargets(project);
            GameplayOperationStateModel state =
                project.GameplayOperationStates.Single(x =>
                    x.OperationType ==
                    ProgressionType.OverworldMovementSpeed);
            OverworldMovementSpeedService.ValidateState(project, state);

            if (movement.RestorePreviousValues)
            {
                JArray current =
                    OverworldMovementSpeedService.CaptureTargets(walk, run);
                if (!JToken.DeepEquals(current, state.BaselineArray))
                    errors.Add("The previous movement values were not restored.");
            }

            HashSet<PropertyModel> allowed = new() { walk.Property, run.Property };
            foreach (PropertyModel property in mutationResult.UpdatedProperties)
                if (property.SourceProperty == null || !allowed.Contains(property))
                    errors.Add("An unrelated project value was changed.");

            if (mutationResult.CreatedEntries.Count != 0 ||
                mutationResult.CreatedProperties.Count != 0 ||
                mutationResult.CreatedJsonPropertyRollbackRecords.Count != 0)
                errors.Add("The movement change unexpectedly created project data.");

            if (mutationResult.UpdatedProperties.Count is not (0 or 2))
                errors.Add("Both movement values were not changed together.");
            int expectedStateChanges = mutationResult.WasModified ? 1 : 0;
            if (mutationResult.GameplayOperationStateRollbackRecords.Count !=
                expectedStateChanges)
                errors.Add("The movement selection was not recorded correctly.");
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
