using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class CharacterXpRequirementsOperation :
    IProjectOperation, IContextualProjectOperation
{
    private readonly ProgressionScalingService
        progressionScalingService;

    public CharacterXpRequirementsOperation(
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
        "Character XP Requirements";

    public string Description =>
        "Scales character level XP requirements from the " +
        "project's original values. Lower percentages reduce " +
        "the XP required to gain levels.";

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
                ProgressionType.Character,
                Percentage);

        return ProjectOperationResult.Success(
            mutationResult,
            mutationResult.WasModified
                ? $"Character XP requirements were set to {Percentage}%."
                : "Character XP requirements already match the requested percentage.");
    }

    public void Preflight(ProjectModel project) =>
        _ = progressionScalingService.ResolveProgressionTable(
            project, ProgressionType.Character);

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ProjectMutationResult result = progressionScalingService.Scale(
            project, ProgressionType.Character, Percentage, context);
        return ProjectOperationResult.Success(result,
            result.WasModified
                ? $"Character XP requirements were set to {Percentage}%."
                : "Character XP requirements already match the requested percentage.");
    }
}
