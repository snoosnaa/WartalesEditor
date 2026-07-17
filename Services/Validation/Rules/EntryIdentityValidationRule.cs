using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation.Rules;

public sealed class EntryIdentityValidationRule
    : IValidationRule
{
    public string RuleId =>
        ValidationRuleIds.EntryIdentity;

    public bool AppliesTo(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return true;
    }

    public IEnumerable<ValidationIssueModel> Validate(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (SheetModel sheet in
                 context.Project.Sheets)
        {
            if (sheet?.Entries == null)
            {
                continue;
            }

            foreach (EntryModel entry in sheet.Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    yield return new ValidationIssueModel(
                        RuleId,
                        ValidationSeverity.Warning,
                        ValidationCategory.ProjectStructure,
                        "An entry does not have a usable identifier.",
                        sheet.Name,
                        entry.Id,
                        entry.DisplayName);
                }
            }

            IEnumerable<IGrouping<string, EntryModel>>
                duplicateGroups =
                    sheet.Entries
                        .Where(entry =>
                            entry != null
                            &&
                            !string.IsNullOrWhiteSpace(
                                entry.Id))
                        .GroupBy(
                            entry => entry.Id,
                            StringComparer.OrdinalIgnoreCase)
                        .Where(group =>
                            group.Count() > 1);

            foreach (IGrouping<string, EntryModel>
                     duplicateGroup in duplicateGroups)
            {
                foreach (EntryModel entry in
                         duplicateGroup)
                {
                    yield return new ValidationIssueModel(
                        RuleId,
                        ValidationSeverity.Warning,
                        ValidationCategory.InternalConsistency,
                        $"The identifier '{duplicateGroup.Key}' " +
                        "appears more than once in this sheet.",
                        sheet.Name,
                        entry.Id,
                        entry.DisplayName);
                }
            }
        }
    }
}