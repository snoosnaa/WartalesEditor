using System;
using System.Collections.Generic;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class RandomTraitExclusionsOperation : IProjectOperation, IContextualProjectOperation
{
    private readonly RandomTraitExclusionsService service;

    public RandomTraitExclusionsOperation(
        RandomTraitExclusionsService service,
        IReadOnlyCollection<string> allowedTraitIds)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        AllowedTraitIds = allowedTraitIds
            ?? throw new ArgumentNullException(nameof(allowedTraitIds));
    }

    public IReadOnlyCollection<string> AllowedTraitIds { get; }

    public string Name => "Random Trait Exclusions";

    public string Description =>
        "Choose which traits may appear when Wartales randomly generates traits " +
        "for future recruits and other eligible procedural units. Existing units are unchanged.";

    public bool CanExecute(ProjectModel project) => project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result = service.Apply(project, AllowedTraitIds);
        return ProjectOperationResult.Success(
            result,
            result.WasModified
                ? "Random trait exclusions were updated."
                : "No changes were applied." + Environment.NewLine + Environment.NewLine +
                  "The current project already matches this setting.");
    }

    public void Preflight(ProjectModel project) =>
        _ = RandomTraitExclusionsService.ResolveCandidateIds(project);

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ProjectMutationResult result = service.Apply(project, AllowedTraitIds, context);
        return ProjectOperationResult.Success(result,
            result.WasModified
                ? "Random trait exclusions were updated."
                : "No changes were applied." + Environment.NewLine + Environment.NewLine +
                  "The current project already matches this setting.");
    }
}
