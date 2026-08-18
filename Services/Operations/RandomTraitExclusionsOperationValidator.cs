using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class RandomTraitExclusionsOperationValidator
    : IProjectOperationValidator
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        List<string> errors = new();
        if (operation is not RandomTraitExclusionsOperation exclusionsOperation)
            return OperationValidationResult.Failure(
                "The Random Trait Exclusions validator received an unsupported operation.");

        try
        {
            GameplayOperationStateModel state = project.GameplayOperationStates.Single(candidate =>
                candidate.OperationType == ProgressionType.RandomTraitExclusions);
            RandomTraitExclusionsService.ValidateState(project, state);
            HashSet<string> owned = state.BaselineArray.OfType<Newtonsoft.Json.Linq.JObject>()
                .Select(record => record.Value<string>("id") ?? string.Empty)
                .ToHashSet(StringComparer.Ordinal);
            string[] requestedIds = exclusionsOperation.AllowedTraitIds.ToArray();
            if (requestedIds.Any(string.IsNullOrWhiteSpace) ||
                requestedIds.Distinct(StringComparer.Ordinal).Count() != requestedIds.Length)
                errors.Add("The requested random trait selection is invalid.");

            HashSet<string> requested = requestedIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> recorded = RandomTraitExclusionsService.ReadAllowedIds(state);
            HashSet<string> resolved = RandomTraitExclusionsService.ResolveCandidateIds(project);
            if (!requested.IsSubsetOf(owned))
                errors.Add("The requested random trait selection contains an unowned trait.");
            if (!recorded.SetEquals(requested))
                errors.Add("The recorded random trait selection does not match the requested selection.");
            if (!owned.SetEquals(resolved))
                errors.Add("The recorded random trait ownership does not match the resolved candidates.");

            IEnumerable<PropertyModel> changed = mutationResult.CreatedProperties
                .Concat(mutationResult.UpdatedProperties)
                .Concat(mutationResult.RemovedProperties);
            foreach (PropertyModel property in changed)
            {
                if (!string.Equals(property.EffectivePropertyPath, "done", StringComparison.Ordinal) ||
                    property.SourceProperty == null)
                    errors.Add("An unrelated project value was changed.");
            }

            foreach (var record in mutationResult.CreatedPropertyRollbackRecords)
                if (!owned.Contains(record.Entry.Id) ||
                    !string.Equals(record.Property.EffectivePropertyPath, "done", StringComparison.Ordinal))
                    errors.Add("A created trait exclusion was not owned by this operation.");
            SheetModel traitSheet = project.Sheets.Single(sheet =>
                string.Equals(sheet.Name, "trait", StringComparison.Ordinal));
            foreach (PropertyModel property in mutationResult.UpdatedProperties)
            {
                EntryModel[] owners = traitSheet.Entries
                    .Where(entry => entry.Properties.Contains(property))
                    .ToArray();
                if (owners.Length != 1 || !owned.Contains(owners[0].Id))
                    errors.Add("An updated trait exclusion was not owned by this operation.");
            }
            foreach (var record in mutationResult.RemovedPropertyRollbackRecords)
                if (!owned.Contains(record.Entry.Id) ||
                    !string.Equals(record.PropertyPath, "done", StringComparison.Ordinal))
                    errors.Add("A removed trait exclusion was not owned by this operation.");

            if (mutationResult.CreatedProperties.Count !=
                    mutationResult.CreatedPropertyRollbackRecords.Count ||
                mutationResult.UpdatedProperties.Count !=
                    mutationResult.PropertyRollbackRecords.Count ||
                mutationResult.RemovedProperties.Count !=
                    mutationResult.RemovedPropertyRollbackRecords.Count)
                errors.Add("Random trait mutation accounting is incomplete.");

            if (changed.Distinct().Count() != changed.Count())
                errors.Add("A trait eligibility value was changed more than once.");

            if (mutationResult.CreatedEntries.Count != 0 ||
                mutationResult.CreatedJsonPropertyRollbackRecords.Count != 0)
                errors.Add("Random Trait Exclusions unexpectedly created structural data.");

            int expectedStateChanges = mutationResult.WasModified ? 1 : 0;
            if (mutationResult.GameplayOperationStateRollbackRecords.Count != expectedStateChanges)
                errors.Add("The random trait selection was not recorded correctly.");
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
