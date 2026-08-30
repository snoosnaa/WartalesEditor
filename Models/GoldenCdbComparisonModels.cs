namespace WartalesEditor.Models;

public enum GoldenCdbComparisonCategory
{
    ChangedValue,
    MissingFromCurrent,
    NewInCurrent,
    TypeChanged,
    StructureChanged,
    AmbiguousIdentity,
    UnsupportedIdentity,
    UnsupportedStructure
}

public enum GoldenCdbComparisonScope
{
    Sheet,
    Entry,
    Property,
    CoverageSummary
}

public sealed record GoldenCdbComparisonItem(
    GoldenCdbComparisonCategory Category,
    GoldenCdbComparisonScope Scope,
    string Sheet,
    string Entry,
    string Property,
    string GoldenValue,
    string CurrentValue,
    string Details)
{
    public bool IsCoverageIssue => Category is
        GoldenCdbComparisonCategory.AmbiguousIdentity or
        GoldenCdbComparisonCategory.UnsupportedIdentity or
        GoldenCdbComparisonCategory.UnsupportedStructure;

    public string Difference => Category switch
    {
        GoldenCdbComparisonCategory.ChangedValue => "Changed",
        GoldenCdbComparisonCategory.MissingFromCurrent => "Missing",
        GoldenCdbComparisonCategory.NewInCurrent => "New",
        GoldenCdbComparisonCategory.TypeChanged => "Type changed",
        GoldenCdbComparisonCategory.StructureChanged => "Structure changed",
        GoldenCdbComparisonCategory.AmbiguousIdentity => "Ambiguous identity",
        GoldenCdbComparisonCategory.UnsupportedIdentity => "No stable identity",
        GoldenCdbComparisonCategory.UnsupportedStructure => "Unsupported structure",
        _ => "Difference"
    };
}

public sealed class GoldenCdbComparisonResult
{
    public GoldenCdbComparisonResult(
        bool isExactMatch,
        IEnumerable<GoldenCdbComparisonItem> items)
    {
        IsExactMatch = isExactMatch;
        Items = items.ToArray();
        Differences = Items.Where(item => !item.IsCoverageIssue).ToArray();
        CoverageIssues = Items.Where(item => item.IsCoverageIssue).ToArray();
    }

    public bool IsExactMatch { get; }

    public IReadOnlyList<GoldenCdbComparisonItem> Items { get; }

    public IReadOnlyList<GoldenCdbComparisonItem> Differences { get; }

    public IReadOnlyList<GoldenCdbComparisonItem> CoverageIssues { get; }

    public int DifferenceCount => Differences.Count;

    public int CoverageIssueCount => CoverageIssues.Count;

    public bool HasCoverageIssues => CoverageIssueCount > 0;

    public string Summary => IsExactMatch
        ? "This project exactly matches your Golden CDB."
        : DifferenceCount > 0
            ? DifferenceCount == 1
                ? "1 difference was found between this project and your Golden CDB."
                : $"{DifferenceCount:N0} differences were found between this project and your Golden CDB."
            : HasCoverageIssues
                ? "No differences were found among safely compared values. Some records could not be compared safely."
                : "No supported editor values differ. The files are not byte-for-byte identical.";

    public string CoverageMessage => HasCoverageIssues
        ? "Some records could not be compared safely because they do not have stable identifiers or use unsupported structures."
        : string.Empty;
}
