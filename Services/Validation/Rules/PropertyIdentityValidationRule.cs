using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation.Rules;

public sealed class PropertyIdentityValidationRule
    : IValidationRule
{
    public string RuleId =>
        ValidationRuleIds.PropertyIdentity;

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
                if (entry?.Properties == null)
                {
                    continue;
                }

                foreach (PropertyModel property in
                         entry.Properties)
                {
                    if (property == null)
                    {
                        yield return new ValidationIssueModel(
                            RuleId,
                            ValidationSeverity.Error,
                            ValidationCategory.ProjectStructure,
                            "An entry contains a null property.",
                            sheet.Name,
                            entry.Id,
                            entry.DisplayName);

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(
                            property.Name))
                    {
                        yield return new ValidationIssueModel(
                            RuleId,
                            ValidationSeverity.Warning,
                            ValidationCategory.ProjectStructure,
                            "A property does not have a usable name.",
                            sheet.Name,
                            entry.Id,
                            entry.DisplayName);
                    }
                }

                IEnumerable<IGrouping<string, PropertyModel>>
                    duplicateGroups =
                        entry.Properties
                            .Where(property =>
                                property != null
                                &&
                                !string.IsNullOrWhiteSpace(
                                    property.Name))
                            .GroupBy(
                                property => property.Name,
                                StringComparer.OrdinalIgnoreCase)
                            .Where(group =>
                                group.Count() > 1);

                foreach (IGrouping<string, PropertyModel>
                         duplicateGroup in duplicateGroups)
                {
                    foreach (PropertyModel property in
                             duplicateGroup)
                    {
                        yield return new ValidationIssueModel(
                            RuleId,
                            ValidationSeverity.Warning,
                            ValidationCategory.InternalConsistency,
                            $"The property '{duplicateGroup.Key}' " +
                            "appears more than once in this entry.",
                            sheet.Name,
                            entry.Id,
                            entry.DisplayName,
                            property.Name);
                    }
                }
            }
        }
    }
}