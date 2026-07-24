using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class StartingResourcesOperationValidator : IProjectOperationValidator
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(mutationResult);
        List<string> errors = new();

        if (operation is not StartingResourcesOperation startingOperation)
        {
            return OperationValidationResult.Failure(
                "Starting Resources validator received an unsupported operation.");
        }

        try
        {
            startingOperation.Settings.Validate();
            StartingResourcesTargets targets = StartingResourcesService.ResolveTargets(project);
            GameplayOperationStateModel state = project.GameplayOperationStates.Single(s =>
                s.OperationType == ProgressionType.StartingResources);
            StartingResourcesService.ValidateState(project, state);
            ValidateTargets(targets, errors);
            ValidateMutationScope(targets, mutationResult, errors);
        }
        catch (Exception exception)
        {
            errors.Add(exception.Message);
        }

        return errors.Count == 0
            ? OperationValidationResult.Success()
            : OperationValidationResult.Failure(errors);
    }

    private static void ValidateTargets(
        StartingResourcesTargets targets,
        ICollection<string> errors)
    {
        foreach (SharedTarget target in targets.Shared)
        {
            if (target.Property.SourceProperty?.Value.Type != JTokenType.Integer)
            {
                errors.Add($"Item '{target.Entry.Id}' starting quantity is not an integer.");
            }
        }

        foreach (OriginTarget origin in targets.Origins)
        {
            if (!string.Equals(
                    origin.Entry.SourceEntry?["props"]?["pattern"]?.Value<string>(),
                    origin.Pattern,
                    StringComparison.Ordinal))
            {
                errors.Add($"Origin '{origin.Entry.Id}' pattern changed.");
            }

            string[] itemIds = origin.Items.OfType<JObject>()
                .Select(item => item.Value<string>("item") ?? string.Empty)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray();
            if (itemIds.Distinct(StringComparer.Ordinal).Count() != itemIds.Length)
            {
                errors.Add($"Origin '{origin.Entry.Id}' contains duplicate item IDs.");
            }

            foreach (JObject item in origin.Items.OfType<JObject>())
            {
                if (item["count"]?.Type != JTokenType.Integer)
                {
                    errors.Add($"Origin '{origin.Entry.Id}' contains a non-integer item count.");
                }
            }
        }
    }

    private static void ValidateMutationScope(
        StartingResourcesTargets targets,
        ProjectMutationResult result,
        ICollection<string> errors)
    {
        HashSet<PropertyModel> allowed = targets.Shared
            .Select(target => target.Property)
            .Concat(targets.Origins.Select(origin =>
                origin.Entry.Properties.Single(property =>
                    property.EffectivePropertyPath == "props.items")))
            .ToHashSet();

        if (result.CreatedEntries.Count != 0 ||
            result.CreatedProperties.Count != 0 ||
            result.CreatedJsonPropertyRollbackRecords.Count != 0)
        {
            errors.Add("Starting Resources unexpectedly created project structure.");
        }

        foreach (PropertyModel property in result.UpdatedProperties)
        {
            if (property.SourceProperty == null || !allowed.Contains(property))
            {
                errors.Add(
                    $"Starting Resources modified an unapproved property '{property.EffectivePropertyPath}'.");
            }
        }

        int expectedStateRecords = result.WasModified ? 1 : 0;
        if (result.GameplayOperationStateRollbackRecords.Count != expectedStateRecords)
        {
            errors.Add("Starting Resources recorded an unexpected operation-state change count.");
        }
    }
}
