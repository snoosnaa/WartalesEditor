using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class PathXpRewardsOperation
    : IProjectOperation, IContextualProjectOperation
{
    private readonly PathXpRewardsService service;

    public PathXpRewardsOperation(
        PathXpRewardsService service,
        string pathId,
        int multiplier,
        bool restorePreviousValues = false)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        PathId = pathId;
        Multiplier = multiplier;
        RestorePreviousValues = restorePreviousValues;
        _ = PathXpRewardsService.GetOperationType(pathId);
    }

    public string PathId { get; }
    public int Multiplier { get; }
    public bool RestorePreviousValues { get; }
    public string PathDisplayName => service.GetDisplayName(PathId);
    public string Name => $"{PathDisplayName} XP Rewards";
    public string Description =>
        "Increases the standard Path challenge reward values independently for this Path. Special rewards and other runtime bonuses are unchanged.";
    public bool CanExecute(ProjectModel project) => project != null;
    public void Preflight(ProjectModel project) =>
        _ = PathXpRewardsService.ResolveTargets(project, PathId);

    public ProjectOperationResult Execute(ProjectModel project) =>
        CreateResult(RestorePreviousValues
            ? service.RestorePreviousValues(project, PathId)
            : service.Apply(project, PathId, Multiplier));

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context) =>
        CreateResult(RestorePreviousValues
            ? service.RestorePreviousValues(project, PathId, context)
            : service.Apply(project, PathId, Multiplier, context));

    private ProjectOperationResult CreateResult(ProjectMutationResult result) =>
        ProjectOperationResult.Success(result,
            result.WasModified
                ? RestorePreviousValues
                    ? $"Previous {PathDisplayName} XP reward values were restored."
                    : $"{PathDisplayName} XP Rewards was configured at {Display(Multiplier)}."
                : $"{PathDisplayName} XP Rewards already matched {Display(Multiplier)}.");

    internal static string Display(int multiplier) =>
        multiplier == 1 ? "Original" : $"{multiplier}×";
}
