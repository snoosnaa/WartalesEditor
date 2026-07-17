using System;
using System.Collections.Generic;
using WartalesEditor.Models;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation.Rules;

public sealed class ProjectStructureValidationRule
    : IValidationRule
{
    public string RuleId =>
        ValidationRuleIds.ProjectStructure;

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

        ProjectModel project =
            context.Project;

        if (project.Sheets == null)
        {
            yield return new ValidationIssueModel(
                RuleId,
                ValidationSeverity.Error,
                ValidationCategory.ProjectStructure,
                "The project does not contain a sheet collection.");

            yield break;
        }

        foreach (SheetModel sheet in project.Sheets)
        {
            if (sheet == null)
            {
                yield return new ValidationIssueModel(
                    RuleId,
                    ValidationSeverity.Error,
                    ValidationCategory.ProjectStructure,
                    "The project contains a null sheet.");

                continue;
            }

            if (string.IsNullOrWhiteSpace(sheet.Name))
            {
                yield return new ValidationIssueModel(
                    RuleId,
                    ValidationSeverity.Warning,
                    ValidationCategory.ProjectStructure,
                    "A sheet does not have a usable name.");
            }

            if (sheet.Entries == null)
            {
                yield return new ValidationIssueModel(
                    RuleId,
                    ValidationSeverity.Error,
                    ValidationCategory.ProjectStructure,
                    "A sheet does not contain an entry collection.",
                    sheet.Name);

                continue;
            }

            foreach (EntryModel entry in sheet.Entries)
            {
                if (entry == null)
                {
                    yield return new ValidationIssueModel(
                        RuleId,
                        ValidationSeverity.Error,
                        ValidationCategory.ProjectStructure,
                        "A sheet contains a null entry.",
                        sheet.Name);

                    continue;
                }

                if (entry.Properties == null)
                {
                    yield return new ValidationIssueModel(
                        RuleId,
                        ValidationSeverity.Error,
                        ValidationCategory.ProjectStructure,
                        "An entry does not contain a property collection.",
                        sheet.Name,
                        entry.Id,
                        entry.DisplayName);
                }
            }
        }
    }
}