using System;
using System.Collections.Generic;
using WartalesEditor.Models;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation.Rules;

public sealed class PropertySourceConnectionValidationRule
    : IValidationRule
{
    public string RuleId =>
        ValidationRuleIds.PropertySourceConnection;

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

            foreach (EntryModel entry in
                     sheet.Entries)
            {
                if (entry?.Properties == null)
                {
                    continue;
                }

                foreach (PropertyModel property in
                         entry.Properties)
                {
                    if (property == null ||
                        property.SourceProperty != null)
                    {
                        continue;
                    }

                    yield return new ValidationIssueModel(
                        RuleId,
                        ValidationSeverity.Error,
                        ValidationCategory.OriginalState,
                        "The property is not connected to its " +
                        "source JSON property and cannot be " +
                        "safely edited or saved.",
                        sheet.Name,
                        entry.Id,
                        entry.DisplayName,
                        property.Name,
                        property.OriginalDisplayValue,
                        property.CurrentDisplayValue);
                }
            }
        }
    }
}
