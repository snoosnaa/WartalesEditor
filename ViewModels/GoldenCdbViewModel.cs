using WartalesEditor.Helpers;
using WartalesEditor.Models;

namespace WartalesEditor.ViewModels;

public sealed class GoldenCdbViewModel : ObservableObject
{
    private GoldenCdbState state;
    private GoldenCdbComparisonResult? comparison;
    private bool hasProject;
    private string operationStatus = string.Empty;
    private GoldenCdbOperationStatusKind operationStatusKind;
    private bool isOperationBusy;

    public GoldenCdbViewModel(
        GoldenCdbState state,
        bool hasProject)
    {
        this.state = state ??
            throw new ArgumentNullException(nameof(state));
        this.hasProject = hasProject;
    }

    public bool IsAvailable => state.IsAvailable;

    public bool IsNotAvailable => !state.IsAvailable;

    public bool HasCanonicalFile => state.CanonicalFileExists;

    public bool HasProject => hasProject;

    public bool CanSetCurrent => hasProject && !isOperationBusy;

    public bool CanLoad => state.IsAvailable && !isOperationBusy;

    public bool CanCompare =>
        state.IsAvailable && hasProject && !isOperationBusy;

    public bool CanSelect => !isOperationBusy;

    public bool CanImport => !isOperationBusy;

    public bool CanRemove =>
        state.CanonicalFileExists && !isOperationBusy;

    public string Heading => state.Availability switch
    {
        GoldenCdbAvailability.Available => "Golden CDB is set",
        GoldenCdbAvailability.Invalid => "Stored Golden CDB could not be used",
        GoldenCdbAvailability.Inaccessible => "Stored Golden CDB could not be accessed",
        _ => "Golden CDB is not set"
    };

    public string Explanation =>
        "Golden CDB is a reference copy you designate. Wartales Editor checks that it can be used, but does not certify that it is vanilla, pristine, or current.";

    public string FileName =>
        state.IsAvailable ? state.CanonicalFileName : string.Empty;

    public string ShortIdentity =>
        state.IsAvailable ? state.ShortIdentity : string.Empty;

    public string FullIdentity => state.Identity;

    public string StatusMessage => state.Message;

    public string OperationStatus => operationStatus;

    public bool HasOperationStatus =>
        !string.IsNullOrWhiteSpace(operationStatus);

    public bool IsOperationStatusSuccess =>
        operationStatusKind == GoldenCdbOperationStatusKind.Success;

    public bool IsOperationStatusWarning =>
        operationStatusKind == GoldenCdbOperationStatusKind.Warning;

    public bool IsOperationStatusError =>
        operationStatusKind == GoldenCdbOperationStatusKind.Error;

    public bool IsOperationBusy => isOperationBusy;

    public string SetCurrentText => state.IsAvailable
        ? "Replace with Current Project"
        : "Set Current Project as Golden";

    public string SelectText => state.IsAvailable
        ? "Select Replacement CDB..."
        : "Select CDB...";

    public GoldenCdbComparisonResult? Comparison => comparison;

    public bool HasComparison => comparison != null;

    public bool HasCoverageWarning =>
        comparison?.HasCoverageIssues == true;

    public IReadOnlyList<GoldenCdbComparisonItem> Differences =>
        comparison?.Differences ?? Array.Empty<GoldenCdbComparisonItem>();

    public string ComparisonSummary => comparison?.Summary ?? string.Empty;

    public string CoverageMessage => comparison?.CoverageMessage ?? string.Empty;

    public void RefreshState(
        GoldenCdbState newState,
        bool projectAvailable)
    {
        state = newState ??
            throw new ArgumentNullException(nameof(newState));
        hasProject = projectAvailable;
        comparison = null;
        OnPropertyChanged(string.Empty);
    }

    public void ShowComparison(
        GoldenCdbComparisonResult result)
    {
        comparison = result ??
            throw new ArgumentNullException(nameof(result));
        OnPropertyChanged(string.Empty);
    }

    internal void BeginOperation(string message)
    {
        isOperationBusy = true;
        SetOperationStatus(
            message,
            GoldenCdbOperationStatusKind.Information);
        OnPropertyChanged(string.Empty);
    }

    internal void ShowOperationSuccess(string message) =>
        CompleteOperation(
            message,
            GoldenCdbOperationStatusKind.Success);

    internal void ShowOperationWarning(string message) =>
        CompleteOperation(
            message,
            GoldenCdbOperationStatusKind.Warning);

    internal void ShowOperationError(string message) =>
        CompleteOperation(
            message,
            GoldenCdbOperationStatusKind.Error);

    internal void ShowOperationInformation(string message) =>
        CompleteOperation(
            message,
            GoldenCdbOperationStatusKind.Information);

    private void CompleteOperation(
        string message,
        GoldenCdbOperationStatusKind kind)
    {
        isOperationBusy = false;
        SetOperationStatus(message, kind);
        OnPropertyChanged(string.Empty);
    }

    private void SetOperationStatus(
        string message,
        GoldenCdbOperationStatusKind kind)
    {
        operationStatus = message ?? string.Empty;
        operationStatusKind = kind;
    }

    private enum GoldenCdbOperationStatusKind
    {
        Information,
        Success,
        Warning,
        Error
    }
}
