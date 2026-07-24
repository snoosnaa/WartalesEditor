using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class PartyEconomyOperationValidator : IProjectOperationValidator
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        List<string> errors = new();
        if (operation is not PartyEconomyOperation economy)
            return OperationValidationResult.Failure("Party Economy validator received an unsupported operation.");
        try
        {
            economy.Settings.Validate(economy.OperationType);
            GameplayOperationStateModel state = project.GameplayOperationStates.Single(
                x => x.OperationType == economy.OperationType);
            PartyEconomyService.ValidateState(project, state);
            IReadOnlyList<Target> targets = PartyEconomyService.ResolveTargets(project, economy.OperationType);
            HashSet<string> allowed = targets.Select(x => $"{x.Entry.Id}|{x.Path}").ToHashSet(StringComparer.Ordinal);
            foreach (PropertyModel property in mutationResult.UpdatedProperties)
            {
                EntryModel? owner = targets.Select(x => x.Entry).FirstOrDefault(
                    x => x.Properties.Contains(property) || ReferenceEquals(property.SourceProperty?.Parent?.Parent, x.SourceEntry));
                string key = $"{owner?.Id}|{property.EffectivePropertyPath}";
                if (property.SourceProperty == null ||
                    (!allowed.Contains(key) && !targets.Any(x =>
                        x.Entry == owner && x.Context.Value<string>("arrayPath") == property.EffectivePropertyPath)))
                    errors.Add($"An unapproved property '{property.EffectivePropertyPath}' was modified.");
            }
            if (mutationResult.CreatedEntries.Count != 0 ||
                mutationResult.CreatedProperties.Count != 0 ||
                mutationResult.CreatedJsonPropertyRollbackRecords.Count != 0)
                errors.Add("Party Economy unexpectedly created project structure.");
            int expectedStateChanges = mutationResult.WasModified ? 1 : 0;
            if (mutationResult.GameplayOperationStateRollbackRecords.Count != expectedStateChanges)
                errors.Add("The settings change was not recorded correctly.");
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
