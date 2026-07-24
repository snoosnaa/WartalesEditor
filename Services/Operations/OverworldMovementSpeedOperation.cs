using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class OverworldMovementSpeedOperation : IProjectOperation
{
    private readonly OverworldMovementSpeedService service;

    public OverworldMovementSpeedOperation(
        OverworldMovementSpeedService service,
        OverworldMovementPreset preset)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        Preset = preset;
    }

    public OverworldMovementPreset Preset { get; }
    public string Name => "Overworld Movement Speed";
    public string Description =>
        "Changes how quickly the player's party travels across the world map.";
    public bool CanExecute(ProjectModel project) => project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result = service.Apply(project, Preset);
        return ProjectOperationResult.Success(
            result,
            result.WasModified
                ? "Overworld Movement Speed was updated."
                : "No changes were applied." + Environment.NewLine +
                  Environment.NewLine +
                  "This preset already matches the current project.");
    }
}
