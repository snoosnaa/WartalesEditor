using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class OverworldMovementSpeedOperation : IProjectOperation, IContextualProjectOperation
{
    private readonly OverworldMovementSpeedService service;

    public OverworldMovementSpeedOperation(
        OverworldMovementSpeedService service,
        OverworldMovementPreset preset)
        : this(service, preset, false)
    {
    }

    public OverworldMovementSpeedOperation(
        OverworldMovementSpeedService service,
        OverworldMovementPreset preset,
        bool restorePreviousValues)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        Preset = preset;
        RestorePreviousValues = restorePreviousValues;
    }

    public OverworldMovementPreset Preset { get; }
    public bool RestorePreviousValues { get; }
    public string Name => "Overworld Movement Speed";
    public string Description =>
        "Changes how quickly the player's party travels across the world map.";
    public bool CanExecute(ProjectModel project) => project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result = RestorePreviousValues
            ? service.RestorePreviousValues(project)
            : service.Apply(project, Preset);
        return ProjectOperationResult.Success(
            result,
            result.WasModified
                ? RestorePreviousValues
                    ? "Previous movement values were restored."
                    : "Overworld Movement Speed was updated."
                : "No changes were applied." + Environment.NewLine +
                  Environment.NewLine +
                  (RestorePreviousValues
                      ? "The previous values already match the current project."
                      : "This preset already matches the current project."));
    }

    public void Preflight(ProjectModel project) =>
        _ = OverworldMovementSpeedService.ResolveTargets(project);

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ProjectMutationResult result = RestorePreviousValues
            ? service.RestorePreviousValues(project, context)
            : service.Apply(project, Preset, context);
        return ProjectOperationResult.Success(result,
            result.WasModified
                ? RestorePreviousValues
                    ? "Previous movement values were restored."
                    : "Overworld Movement Speed was updated."
                : "No changes were applied." + Environment.NewLine + Environment.NewLine +
                  (RestorePreviousValues
                      ? "The previous values already match the current project."
                      : "This preset already matches the current project."));
    }
}
