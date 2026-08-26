using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class ProfessionXpRequirementsOperation :
    IProjectOperation, IContextualProjectOperation
{
    private readonly ProgressionScalingService
        progressionScalingService;

    public ProfessionXpRequirementsOperation(
        ProgressionScalingService progressionScalingService,
        int percentage)
    {
        ArgumentNullException.ThrowIfNull(
            progressionScalingService);

        this.progressionScalingService =
            progressionScalingService;

        Percentage = percentage;
    }

    public int Percentage { get; }

    public string Name =>
        "Profession XP Requirements";

    public string Description =>
        "Scales profession XP requirements from the project's " +
        "original values. Lower percentages reduce the XP " +
        "required to advance professions.";

    public bool CanExecute(
        ProjectModel project)
    {
        return project != null;
    }

    public ProjectOperationResult Execute(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        ProjectMutationResult mutationResult =
            progressionScalingService.Scale(
                project,
                ProgressionType.Profession,
                Percentage);

        return ProjectOperationResult.Success(
            mutationResult,
            mutationResult.WasModified
                ? $"Profession XP requirements were set to {Percentage}%."
                : "Profession XP requirements already match the requested percentage.");
    }

    public void Preflight(ProjectModel project) =>
        _ = progressionScalingService.ResolveProgressionTable(
            project, ProgressionType.Profession);

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ProjectMutationResult result = progressionScalingService.Scale(
            project, ProgressionType.Profession, Percentage, context);
        return ProjectOperationResult.Success(result,
            result.WasModified
                ? $"Profession XP requirements were set to {Percentage}%."
                : "Profession XP requirements already match the requested percentage.");
    }
}
