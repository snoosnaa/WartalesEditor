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
        : this(service, operationType, presetKey, false)
    {
    }

    public GameplayPresetOperation(
        GameplayPresetService service,
        ProgressionType operationType,
        string presetKey,
        bool restorePreviousValues)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        _ = GameplayPresetCatalog.Get(operationType);
        OperationType = operationType;
        PresetKey = presetKey ?? throw new ArgumentNullException(nameof(presetKey));
        RestorePreviousValues = restorePreviousValues;
    }

    public ProgressionType OperationType { get; }
    public string PresetKey { get; }
    public bool RestorePreviousValues { get; }
    public string Name => GameplayPresetCatalog.Get(OperationType).Title;
    public string Description => GameplayPresetCatalog.Get(OperationType).Description;
    public bool CanExecute(ProjectModel project) => project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result = RestorePreviousValues
            ? service.RestorePreviousValues(project, OperationType)
            : service.Apply(project, OperationType, PresetKey);
        return ProjectOperationResult.Success(
            result,
            result.WasModified
                ? RestorePreviousValues
                    ? $"{Name} previous values were restored."
                    : $"{Name} was updated."
                : "No changes were applied." + Environment.NewLine + Environment.NewLine +
                  (RestorePreviousValues
                      ? "The previous values already match the current project."
                      : "This preset already matches the current project."));
    }
}
