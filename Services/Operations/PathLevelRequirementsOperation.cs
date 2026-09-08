using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class PathLevelRequirementsOperation
    : IProjectOperation, IContextualProjectOperation
{
    private readonly PathLevelRequirementsService service;

    public PathLevelRequirementsOperation(
        PathLevelRequirementsService service,
        int percentage,
        bool restorePreviousValues = false)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        Percentage = percentage;
        RestorePreviousValues = restorePreviousValues;
    }

    public int Percentage { get; }
    public bool RestorePreviousValues { get; }
    public string Name => "Path Level Requirements";
    public string Description =>
        "Lowers the XP required to reach each Path level for all Paths.";
    public bool CanExecute(ProjectModel project) => project != null;
    public void Preflight(ProjectModel project) =>
        _ = PathLevelRequirementsService.ResolveTargets(project);

    public ProjectOperationResult Execute(ProjectModel project) =>
        CreateResult(RestorePreviousValues
            ? service.RestorePreviousValues(project)
            : service.Apply(project, Percentage));

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context) =>
        CreateResult(RestorePreviousValues
            ? service.RestorePreviousValues(project, context)
            : service.Apply(project, Percentage, context));

    private ProjectOperationResult CreateResult(ProjectMutationResult result) =>
        ProjectOperationResult.Success(result,
            result.WasModified
                ? RestorePreviousValues
                    ? "Previous Path level requirement values were restored."
                    : $"Path Level Requirements was configured at {Display(Percentage)}."
                : $"Path Level Requirements already matched {Display(Percentage)}.");

    internal static string Display(int percentage) =>
        percentage == 100 ? "Original" : $"{percentage}%";
}
