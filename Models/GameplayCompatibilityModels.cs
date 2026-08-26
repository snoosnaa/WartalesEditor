namespace WartalesEditor.Models;

public enum GameplayCompatibilityStatus
{
    Compatible,
    PartiallyOutdated,
    MissingTarget,
    TypeChanged,
    StructureChanged,
    AmbiguousTarget,
    UnsupportedStructure,
    AssessmentFailed
}

public sealed record GameplayCompatibilityAssessment(
    string ToolName,
    GameplayCompatibilityStatus Status,
    string Message)
{
    public string DisplayStatus => Status switch
    {
        GameplayCompatibilityStatus.Compatible => "Compatible",
        GameplayCompatibilityStatus.PartiallyOutdated => "May need updating",
        GameplayCompatibilityStatus.MissingTarget => "Feature target not found",
        GameplayCompatibilityStatus.TypeChanged => "Game data type changed",
        GameplayCompatibilityStatus.StructureChanged => "Game data structure changed",
        GameplayCompatibilityStatus.AmbiguousTarget => "Multiple feature targets found",
        GameplayCompatibilityStatus.UnsupportedStructure => "Game data is not supported",
        GameplayCompatibilityStatus.AssessmentFailed => "Check could not complete",
        _ => "Compatibility issue"
    };
}

public enum SourceGenerationTransition
{
    NoPreviousGeneration,
    SameSourceGeneration,
    ChangedSourceGeneration,
    PreviousSourceGenerationUnknown,
    CurrentSourceGenerationUnknown,
    ExternalContentMismatch
}

public sealed record UpdateCompatibilityReport(
    SourceGenerationTransition Transition,
    int ActiveGameplayStateCount,
    int HistoricalGameplayStateCount,
    int UnknownProvenanceStateCount,
    IReadOnlyList<GameplayCompatibilityAssessment> GameplayTools,
    IReadOnlyList<string> ProjectWarnings,
    string PlayerSummary,
    string TechnicalSummary)
{
    public IReadOnlyList<GameplayCompatibilityAssessment>
        ProblematicGameplayTools => GameplayTools
            .Where(assessment =>
                assessment.Status != GameplayCompatibilityStatus.Compatible)
            .ToArray();

    public int GameplayIssueCount =>
        ProblematicGameplayTools.Count;

    public IReadOnlyList<string> PlayerWarnings => ProjectWarnings
        .Select(CreatePlayerWarning)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public int IssueCount =>
        GameplayIssueCount + PlayerWarnings.Count;

    public bool HasGameplayIssues =>
        GameplayIssueCount > 0;

    public bool HasProjectWarnings =>
        PlayerWarnings.Count > 0;

    public bool HasIssues =>
        IssueCount > 0;

    public bool HasNoIssues =>
        !HasIssues;

    public string ResultSummary => IssueCount switch
    {
        0 => "No compatibility issues detected.",
        1 => "1 issue found.",
        _ => $"{IssueCount} issues found."
    };

    public string ResultDetail => HasNoIssues
        ? "All supported gameplay features are compatible with the current game data."
        : "Review the affected gameplay features and warnings below.";

    private static string CreatePlayerWarning(string warning)
    {
        if (warning.Contains(
                "Previous restore information",
                StringComparison.OrdinalIgnoreCase))
        {
            return warning;
        }

        if (warning.Contains("sheet", StringComparison.OrdinalIgnoreCase))
        {
            return "Some game data could not be interpreted by this editor version. The original data remains preserved.";
        }

        return "Previous values for a gameplay feature are no longer compatible with the current game data.";
    }
}
