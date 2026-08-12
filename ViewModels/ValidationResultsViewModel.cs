using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using WartalesEditor.Helpers;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.ViewModels;

public sealed class ValidationResultsViewModel :
    ObservableObject
{
    private readonly Func<ValidationResultModel>
        rerunValidation;

    private readonly Action<ValidationIssueModel>
        navigateToIssue;

    private readonly Action<string>
        copyResults;

    private ValidationIssueModel? selectedIssue;

    private ValidationIssueFilter selectedFilter =
        ValidationIssueFilter.All;

    public ValidationResultsViewModel(
        ValidationResultModel result,
        Func<ValidationResultModel> rerunValidation,
        Action<ValidationIssueModel> navigateToIssue,
        Action<string> copyResults)
    {
        ArgumentNullException.ThrowIfNull(result);

        this.rerunValidation =
            rerunValidation
            ?? throw new ArgumentNullException(
                nameof(rerunValidation));

        this.navigateToIssue =
            navigateToIssue
            ?? throw new ArgumentNullException(
                nameof(navigateToIssue));

        this.copyResults =
            copyResults
            ?? throw new ArgumentNullException(
                nameof(copyResults));

        Issues =
            new ObservableCollection<
                ValidationIssueModel>();

        FilteredIssues =
            CollectionViewSource.GetDefaultView(
                Issues);

        FilteredIssues.Filter =
            ShouldDisplayIssue;

        NavigateCommand =
            new RelayCommand(
                _ => NavigateToSelectedIssue(),
                _ =>
                    SelectedIssue?.HasNavigationTarget
                    == true);

        RerunValidationCommand =
            new RelayCommand(
                _ => RerunValidation());

        CopyResultsCommand =
            new RelayCommand(
                _ => CopyResults());

        ShowAllCommand =
            new RelayCommand(
                _ => SelectedFilter =
                    ValidationIssueFilter.All);

        ShowErrorsCommand =
            new RelayCommand(
                _ => SelectedFilter =
                    ValidationIssueFilter.Errors);

        ShowWarningsCommand =
            new RelayCommand(
                _ => SelectedFilter =
                    ValidationIssueFilter.Warnings);

        ShowInformationCommand =
            new RelayCommand(
                _ => SelectedFilter =
                    ValidationIssueFilter.Information);

        Refresh(result);
    }

    public ObservableCollection<ValidationIssueModel>
        Issues
    {
        get;
    }

    public ICollectionView FilteredIssues
    {
        get;
    }

    public ValidationIssueModel? SelectedIssue
    {
        get => selectedIssue;
        set
        {
            if (!SetProperty(
                    ref selectedIssue,
                    value))
            {
                return;
            }

            NavigateCommand
                .NotifyCanExecuteChanged();
        }
    }

    public ValidationIssueFilter SelectedFilter
    {
        get => selectedFilter;
        private set
        {
            if (!SetProperty(
                    ref selectedFilter,
                    value))
            {
                return;
            }

            FilteredIssues.Refresh();

            OnPropertyChanged(
                nameof(IsShowingAll));

            OnPropertyChanged(
                nameof(IsShowingErrors));

            OnPropertyChanged(
                nameof(IsShowingWarnings));

            OnPropertyChanged(
                nameof(IsShowingInformation));
        }
    }

    public int TotalCount =>
        Issues.Count;

    public int ErrorCount =>
        Issues.Count(issue =>
            issue.Severity
            == ValidationSeverity.Error);

    public int WarningCount =>
        Issues.Count(issue =>
            issue.Severity
            == ValidationSeverity.Warning);

    public int InformationCount =>
        Issues.Count(issue =>
            issue.Severity
            == ValidationSeverity.Information);

    public bool HasIssues =>
        TotalCount > 0;

    public bool HasErrors =>
        ErrorCount > 0;

    public bool HasWarnings =>
        WarningCount > 0;

    public bool HasInformation =>
        InformationCount > 0;

    public bool IsSuccessful =>
        !HasErrors;

    public bool IsShowingAll =>
        SelectedFilter
        == ValidationIssueFilter.All;

    public bool IsShowingErrors =>
        SelectedFilter
        == ValidationIssueFilter.Errors;

    public bool IsShowingWarnings =>
        SelectedFilter
        == ValidationIssueFilter.Warnings;

    public bool IsShowingInformation =>
        SelectedFilter
        == ValidationIssueFilter.Information;

    public string Header
    {
        get
        {
            if (!HasIssues)
            {
                return
                    "Ready to Save";
            }

            return TotalCount == 1
                ? "1 Issue Found"
                : $"{TotalCount:N0} Issues Found";
        }
    }

    public string StatusText
    {
        get
        {
            if (HasErrors)
            {
                return
                    "Not ready to save. Fix the errors below and check the project again.";
            }

            if (HasWarnings)
            {
                return
                    "Ready to save, but review the warnings.";
            }

            if (HasInformation)
            {
                return
                    "Ready to save. Additional information is available.";
            }

            return
                "Ready to save. No issues were found.";
        }
    }

    public RelayCommand NavigateCommand
    {
        get;
    }

    public RelayCommand RerunValidationCommand
    {
        get;
    }

    public RelayCommand CopyResultsCommand
    {
        get;
    }

    public RelayCommand ShowAllCommand
    {
        get;
    }

    public RelayCommand ShowErrorsCommand
    {
        get;
    }

    public RelayCommand ShowWarningsCommand
    {
        get;
    }

    public RelayCommand ShowInformationCommand
    {
        get;
    }

    public void Refresh(
        ValidationResultModel result)
    {
        ArgumentNullException.ThrowIfNull(result);

        ValidationIssueModel? previousSelection =
            SelectedIssue;

        Issues.Clear();

        foreach (ValidationIssueModel issue in
                 result.Issues)
        {
            Issues.Add(issue);
        }

        FilteredIssues.Refresh();

        SelectedIssue =
            previousSelection == null
                ? null
                : FindMatchingIssue(
                    previousSelection);

        RaiseResultPropertiesChanged();

        NavigateCommand
            .NotifyCanExecuteChanged();
    }

    private void RerunValidation()
    {
        ValidationResultModel result =
            rerunValidation();

        Refresh(result);
    }

    private void NavigateToSelectedIssue()
    {
        if (SelectedIssue == null ||
            !SelectedIssue.HasNavigationTarget)
        {
            return;
        }

        navigateToIssue(
            SelectedIssue);
    }

    private void CopyResults()
    {
        copyResults(
            BuildCopyText());
    }

    private bool ShouldDisplayIssue(
        object issueObject)
    {
        if (issueObject
            is not ValidationIssueModel issue)
        {
            return false;
        }

        return SelectedFilter switch
        {
            ValidationIssueFilter.Errors =>
                issue.Severity
                == ValidationSeverity.Error,

            ValidationIssueFilter.Warnings =>
                issue.Severity
                == ValidationSeverity.Warning,

            ValidationIssueFilter.Information =>
                issue.Severity
                == ValidationSeverity.Information,

            _ =>
                true
        };
    }

    private ValidationIssueModel? FindMatchingIssue(
        ValidationIssueModel previousSelection)
    {
        return Issues.FirstOrDefault(issue =>
            string.Equals(
                issue.RuleId,
                previousSelection.RuleId,
                StringComparison.Ordinal)
            &&
            string.Equals(
                issue.SheetName,
                previousSelection.SheetName,
                StringComparison.Ordinal)
            &&
            string.Equals(
                issue.EntryId,
                previousSelection.EntryId,
                StringComparison.Ordinal)
            &&
            string.Equals(
                issue.PropertyName,
                previousSelection.PropertyName,
                StringComparison.Ordinal)
            &&
            string.Equals(
                issue.Message,
                previousSelection.Message,
                StringComparison.Ordinal));
    }

    private string BuildCopyText()
    {
        StringBuilder text =
            new();

        text.AppendLine(
            "Wartales Editor Project Check Details");

        text.AppendLine();
        text.AppendLine(
            $"Errors: {ErrorCount:N0}");

        text.AppendLine(
            $"Warnings: {WarningCount:N0}");

        text.AppendLine(
            $"Information: {InformationCount:N0}");

        text.AppendLine();

        if (!HasIssues)
        {
            text.Append(
                "No validation issues were found.");

            return text.ToString();
        }

        foreach (ValidationIssueModel issue in
                 Issues)
        {
            text.Append('[');
            text.Append(issue.Severity);
            text.Append("] ");

            text.Append(issue.Category);
            text.Append(": ");
            text.AppendLine(issue.Message);

            string location =
                BuildIssueLocation(issue);

            if (!string.IsNullOrWhiteSpace(
                    location))
            {
                text.Append("Location: ");
                text.AppendLine(location);
            }

            text.Append("Rule: ");
            text.AppendLine(issue.RuleId);

            if (!string.IsNullOrWhiteSpace(
                    issue.OriginalValue))
            {
                text.Append("Original: ");
                text.AppendLine(
                    issue.OriginalValue);
            }

            if (!string.IsNullOrWhiteSpace(
                    issue.CurrentValue))
            {
                text.Append("Current: ");
                text.AppendLine(
                    issue.CurrentValue);
            }

            text.AppendLine();
        }

        return text.ToString().TrimEnd();
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

    private void RaiseResultPropertiesChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(InformationCount));
        OnPropertyChanged(nameof(HasIssues));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(HasInformation));
        OnPropertyChanged(nameof(IsSuccessful));
        OnPropertyChanged(nameof(Header));
        OnPropertyChanged(nameof(StatusText));
    }
}
