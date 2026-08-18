using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class RainFrequencyOperationValidator
    : IProjectOperationValidator
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        if (operation is not RainFrequencyOperation rainOperation)
            return OperationValidationResult.Failure(
                "The Rain Frequency validator received an unsupported operation.");

        List<string> errors = new();
        try
        {
            RainFrequencyPresetOption? selection =
                rainOperation.RestorePreviousValues
                    ? null
                    : RainFrequencyService.Presets.Single(option =>
                        option.Preset == rainOperation.Preset);
            IReadOnlyList<RainTarget> targets =
                RainFrequencyService.ResolveTargets(project);
            GameplayOperationStateModel state =
                project.GameplayOperationStates.Single(candidate =>
                    candidate.OperationType ==
                    ProgressionType.RainFrequency);
            RainFrequencyService.ValidateState(project, state);

            HashSet<PropertyModel> allowed =
                targets.Select(target => target.Property).ToHashSet();
            foreach (PropertyModel property in
                     mutationResult.UpdatedProperties)
            {
                if (property.SourceProperty == null ||
                    !allowed.Contains(property))
                    errors.Add(
                        "An unrelated weather value was changed.");
            }

            if (mutationResult.CreatedEntries.Count != 0 ||
                mutationResult.CreatedProperties.Count != 0 ||
                mutationResult.CreatedJsonPropertyRollbackRecords.Count != 0)
                errors.Add(
                    "Rain Frequency unexpectedly created project data.");

            if (mutationResult.UpdatedProperties.Count >
                RainFrequencyService.Regions.Count)
                errors.Add(
                    "Rain Frequency changed too many regional values.");

            JArray expected = rainOperation.RestorePreviousValues
                ? (JArray)state.BaselineArray.DeepClone()
                : RainFrequencyService.BuildExpected(
                    RainFrequencyService.CreateBaseline(),
                    selection!);
            for (int index = 0; index < targets.Count; index++)
            {
                if (!JToken.DeepEquals(
                        targets[index].Property.SourceProperty!.Value,
                        expected[index]!["value"]))
                    errors.Add(
                        $"{targets[index].Definition.DisplayName} " +
                        "does not match the selected rain preset.");
            }

            int expectedStateChanges =
                mutationResult.WasModified ? 1 : 0;
            if (mutationResult.GameplayOperationStateRollbackRecords.Count !=
                expectedStateChanges)
                errors.Add(
                    "The Rain Frequency selection was not recorded correctly.");
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
