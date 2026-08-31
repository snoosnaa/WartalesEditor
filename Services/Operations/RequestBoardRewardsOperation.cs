using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class RequestBoardRewardsOperation
    : IProjectOperation, IContextualProjectOperation
{
    private readonly RequestBoardRewardsService service;

    public RequestBoardRewardsOperation(
        RequestBoardRewardsService service,
        int percentage)
        : this(service, percentage, false)
    {
    }

    public RequestBoardRewardsOperation(
        RequestBoardRewardsService service,
        int percentage,
        bool restorePreviousValues)
    {
        this.service = service
            ?? throw new ArgumentNullException(nameof(service));
        Percentage = percentage;
        RestorePreviousValues = restorePreviousValues;
    }

    public int Percentage { get; }
    public bool RestorePreviousValues { get; }
    public string Name => "Request Board Rewards";
    public string Description =>
        "Increase the base Krown rewards offered by Tavern Request Board missions.";

    public bool CanExecute(ProjectModel project) => project != null;

    public void Preflight(ProjectModel project)
    {
        _ = RequestBoardRewardsService.ResolveTargets(project);
    }

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result = RestorePreviousValues
            ? service.RestorePreviousValues(project)
            : service.Apply(project, Percentage);
        return CreateResult(result);
    }

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ProjectMutationResult result = RestorePreviousValues
            ? service.RestorePreviousValues(project, context)
            : service.Apply(project, Percentage, context);
        return CreateResult(result);
    }

    private ProjectOperationResult CreateResult(
        ProjectMutationResult result) =>
        ProjectOperationResult.Success(
            result,
            result.WasModified
                ? RestorePreviousValues
                    ? "Previous Request Board reward values were restored."
                    : "Request Board Rewards was updated."
                : "No changes were applied." +
                  Environment.NewLine +
                  Environment.NewLine +
                  (RestorePreviousValues
                      ? "The previous values already match the current project."
                      : "This preset already matches the current project."));
}
