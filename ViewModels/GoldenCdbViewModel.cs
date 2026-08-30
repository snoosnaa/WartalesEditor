using WartalesEditor.Helpers;
using WartalesEditor.Models;

namespace WartalesEditor.ViewModels;

public sealed class GoldenCdbViewModel : ObservableObject
{
    private GoldenCdbState state;
    private GoldenCdbComparisonResult? comparison;
    private bool hasProject;

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

    public bool CanSetCurrent => hasProject;

    public bool CanLoad => state.IsAvailable;

    public bool CanCompare => state.IsAvailable && hasProject;

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
}
