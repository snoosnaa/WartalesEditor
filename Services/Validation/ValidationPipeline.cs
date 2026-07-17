using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation;

public sealed class ValidationPipeline
{
    private readonly IReadOnlyList<IValidationRule>
        rules;

    public ValidationPipeline(
        IEnumerable<IValidationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        this.rules =
            rules
                .Where(rule =>
                    rule != null)
                .ToList();
    }

    public ValidationResultModel Run(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<ValidationIssueModel> issues =
            new();

        foreach (IValidationRule rule in rules)
        {
            if (!rule.AppliesTo(context))
            {
                continue;
            }

            IEnumerable<ValidationIssueModel> ruleIssues =
                rule.Validate(context)
                ?? Array.Empty<ValidationIssueModel>();

            issues.AddRange(
                ruleIssues.Where(issue =>
                    issue != null));
        }

        IReadOnlyList<ValidationIssueModel>
            orderedIssues =
                issues
                    .OrderByDescending(issue =>
                        issue.Severity)
                    .ThenBy(issue =>
                        issue.Category)
                    .ThenBy(issue =>
                        issue.SheetName)
                    .ThenBy(issue =>
                        issue.EntryId)
                    .ThenBy(issue =>
                        issue.PropertyName)
                    .ThenBy(issue =>
                        issue.Message)
                    .ToList();

        return new ValidationResultModel(
            orderedIssues);
    }
}

