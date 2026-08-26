using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class UpdateCompatibilityReportService
{
    private readonly GameplayCompatibilityAssessmentService assessmentService;

    public UpdateCompatibilityReportService()
        : this(new GameplayCompatibilityAssessmentService())
    {
    }

    public UpdateCompatibilityReportService(
        GameplayCompatibilityAssessmentService assessmentService)
    {
        this.assessmentService = assessmentService ??
            throw new ArgumentNullException(nameof(assessmentService));
    }

    public UpdateCompatibilityReport Create(
        ProjectModel project,
        SourceGenerationTransition transition)
    {
        ArgumentNullException.ThrowIfNull(project);
        IReadOnlyList<GameplayCompatibilityAssessment> tools;
        try
        {
            tools = assessmentService.Assess(project);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.WriteLine(exception);
            tools = new[]
            {
                new GameplayCompatibilityAssessment(
                    "Gameplay Tools",
                    GameplayCompatibilityStatus.AssessmentFailed,
                    "Gameplay-tool compatibility could not be checked."),
            };
        }

        string summary = transition switch
        {
            SourceGenerationTransition.ChangedSourceGeneration =>
                "Game data changed. Gameplay tools and saved previous values were checked against the new data.",
            SourceGenerationTransition.ExternalContentMismatch =>
                "This game data differs from the file revision used by previous settings.",
            SourceGenerationTransition.PreviousSourceGenerationUnknown =>
                "Previous restore information could not be verified.",
            _ => "Compatibility information is available for this project."
        };

        return new UpdateCompatibilityReport(
            transition,
            project.GameplayOperationStates.Count,
            project.HistoricalGameplayOperationStates.Count,
            project.HistoricalGameplayOperationStates.Count(state =>
                string.IsNullOrWhiteSpace(state.ProjectCompatibilityIdentity)),
            tools,
            project.ProjectLoadWarnings
                .Concat(project.GameplayOperationStateWarnings)
                .ToArray(),
            summary,
            "Unknown game data remains preserved in the project document. Profiles remain available for three-way review.");
    }
}
