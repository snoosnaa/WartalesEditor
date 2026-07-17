using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WartalesEditor.Models.Validation;

public sealed class ValidationResultModel
{
    public ValidationResultModel(
        IEnumerable<ValidationIssueModel> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        Issues =
            new ReadOnlyCollection<ValidationIssueModel>(
                issues.ToList());

        ErrorCount =
            Issues.Count(issue =>
                issue.Severity
                == ValidationSeverity.Error);

        WarningCount =
            Issues.Count(issue =>
                issue.Severity
                == ValidationSeverity.Warning);

        InformationCount =
            Issues.Count(issue =>
                issue.Severity
                == ValidationSeverity.Information);
    }

    public IReadOnlyList<ValidationIssueModel> Issues
    {
        get;
    }

    public int TotalCount =>
        Issues.Count;

    public int ErrorCount { get; }

    public int WarningCount { get; }

    public int InformationCount { get; }

    public bool HasIssues =>
        TotalCount > 0;

    public bool HasErrors =>
        ErrorCount > 0;

    public bool HasWarnings =>
        WarningCount > 0;

    public bool HasInformation =>
        InformationCount > 0;

    public bool CanContinue =>
        !HasErrors;

    public static ValidationResultModel Empty { get; } =
        new(Array.Empty<ValidationIssueModel>());
}
