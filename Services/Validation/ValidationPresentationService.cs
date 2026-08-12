using System;
using System.Linq;
using System.Text;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation;

public sealed class ValidationPresentationService
{
    private const int MaximumDisplayedIssues =
        20;

    public ValidationPresentationModel BuildPresentation(
        ValidationResultModel result,
        string operationName)
    {
        ArgumentNullException.ThrowIfNull(
            result);

        if (string.IsNullOrWhiteSpace(
                operationName))
        {
            throw new ArgumentException(
                "An operation name is required.",
                nameof(operationName));
        }

        ValidationSeverity highestSeverity =
            GetHighestSeverity(result);

        string title =
            BuildTitle(
                operationName,
                result);

        string summary =
            BuildSummary(
                operationName,
                result);

        return new ValidationPresentationModel(
            title,
            summary,
            highestSeverity,
            result.CanContinue);
    }

    private static ValidationSeverity
        GetHighestSeverity(
            ValidationResultModel result)
    {
        if (result.HasErrors)
        {
            return ValidationSeverity.Error;
        }

        if (result.HasWarnings)
        {
            return ValidationSeverity.Warning;
        }

        return ValidationSeverity.Information;
    }

    private static string BuildTitle(
        string operationName,
        ValidationResultModel result)
    {
        if (result.HasErrors)
        {
            return
                $"{operationName} Check Found Errors";
        }

        if (result.HasWarnings)
        {
            return
                $"{operationName} Check Warnings";
        }

        return
            $"{operationName} Check Complete";
    }

    private static string BuildSummary(
        string operationName,
        ValidationResultModel result)
    {
        StringBuilder message =
            new();

        if (!result.HasIssues)
        {
            message.Append(
                $"{operationName} is ready to continue.");

            message.AppendLine();
            message.AppendLine();

            message.Append(
                "No issues were found.");

            return message.ToString();
        }

        message.AppendLine(
            $"{operationName} check found " +
            $"{result.TotalCount:N0} " +
            $"{GetSingularOrPlural(
                result.TotalCount,
                "issue",
                "issues")}.");

        message.AppendLine();
        message.AppendLine(
            $"Errors: {result.ErrorCount:N0}");

        message.AppendLine(
            $"Warnings: {result.WarningCount:N0}");

        message.AppendLine(
            $"Information: " +
            $"{result.InformationCount:N0}");

        message.AppendLine();

        if (result.HasErrors)
        {
            message.AppendLine(
                "The action cannot continue until the errors are resolved.");
        }
        else
        {
            message.AppendLine(
                "The action can continue, but review the issues below.");
        }

        message.AppendLine();
        message.AppendLine(
            "Issues:");

        int displayedIssueCount =
            Math.Min(
                result.TotalCount,
                MaximumDisplayedIssues);

        foreach (ValidationIssueModel issue in
                 result.Issues.Take(
                     displayedIssueCount))
        {
            message.AppendLine(
                BuildIssueSummary(issue));
        }

        int hiddenIssueCount =
            result.TotalCount
            - displayedIssueCount;

        if (hiddenIssueCount > 0)
        {
            message.AppendLine();

            message.Append(
                $"{hiddenIssueCount:N0} additional " +
                $"{GetSingularOrPlural(
                    hiddenIssueCount,
                    "issue was",
                    "issues were")} not shown.");
        }

        return message.ToString();
    }

    private static string BuildIssueSummary(
        ValidationIssueModel issue)
    {
        StringBuilder message =
            new();

        message.Append("- ");
        message.Append(
            issue.Severity);

        message.Append(": ");
        message.Append(
            issue.Message);

        string location =
            BuildIssueLocation(issue);

        if (!string.IsNullOrWhiteSpace(
                location))
        {
            message.Append(" [");
            message.Append(location);
            message.Append(']');
        }

        return message.ToString();
    }

    private static string BuildIssueLocation(
        ValidationIssueModel issue)
    {
        return string.Join(
            " → ",
            new[]
            {
                issue.SheetName,
                issue.EntryName
                ?? issue.EntryId,
                issue.PropertyName
            }
            .Where(value =>
                !string.IsNullOrWhiteSpace(
                    value)));
    }

    private static string GetSingularOrPlural(
        int count,
        string singular,
        string plural)
    {
        return count == 1
            ? singular
            : plural;
    }
}
