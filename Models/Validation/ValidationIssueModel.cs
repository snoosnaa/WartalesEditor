using System;

namespace WartalesEditor.Models.Validation;

public sealed class ValidationIssueModel
{
    public ValidationIssueModel(
        string ruleId,
        ValidationSeverity severity,
        ValidationCategory category,
        string message,
        string? sheetName = null,
        string? entryId = null,
        string? entryName = null,
        string? propertyName = null,
        string? originalValue = null,
        string? currentValue = null)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new ArgumentException(
                "A validation rule identifier is required.",
                nameof(ruleId));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "A validation issue message is required.",
                nameof(message));
        }

        RuleId = ruleId;
        Severity = severity;
        Category = category;
        Message = message;
        SheetName = sheetName;
        EntryId = entryId;
        EntryName = entryName;
        PropertyName = propertyName;
        OriginalValue = originalValue;
        CurrentValue = currentValue;
    }

    public string RuleId { get; }

    public ValidationSeverity Severity { get; }

    public ValidationCategory Category { get; }

    public string Message { get; }

    public string? SheetName { get; }

    public string? EntryId { get; }

    public string? EntryName { get; }

    public string? PropertyName { get; }

    public string? OriginalValue { get; }

    public string? CurrentValue { get; }

    public bool BlocksOperation =>
        Severity == ValidationSeverity.Error;

    public bool HasNavigationTarget =>
        !string.IsNullOrWhiteSpace(SheetName)
        &&
        !string.IsNullOrWhiteSpace(EntryId);
}
