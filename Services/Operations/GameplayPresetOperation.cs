using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class GameplayPresetOperation : IProjectOperation
{
    private readonly GameplayPresetService service;

    public GameplayPresetOperation(
        GameplayPresetService service,
        ProgressionType operationType,
        string presetKey)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        _ = GameplayPresetCatalog.Get(operationType);
        OperationType = operationType;
        PresetKey = presetKey ?? throw new ArgumentNullException(nameof(presetKey));
    }

    public ProgressionType OperationType { get; }
    public string PresetKey { get; }
    public string Name => GameplayPresetCatalog.Get(OperationType).Title;
    public string Description => GameplayPresetCatalog.Get(OperationType).Description;
    public bool CanExecute(ProjectModel project) => project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result = service.Apply(project, OperationType, PresetKey);
        return ProjectOperationResult.Success(
            result,
            result.WasModified
                ? $"{Name} was updated."
                : "No changes were applied." + Environment.NewLine + Environment.NewLine +
                  "This preset already matches the current project.");
    }
}
