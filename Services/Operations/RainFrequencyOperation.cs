using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class RainFrequencyOperation : IProjectOperation
{
    private readonly RainFrequencyService service;

    public RainFrequencyOperation(
        RainFrequencyService service,
        RainFrequencyPreset preset)
    {
        this.service = service
            ?? throw new ArgumentNullException(nameof(service));
        Preset = preset;
    }

    public RainFrequencyPreset Preset { get; }

    public string Name => "Rain Frequency";

    public string Description =>
        "Controls how often ordinary rain occurs across supported regions.";

    public bool CanExecute(ProjectModel project) =>
        project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result =
            service.Apply(project, Preset);
        return ProjectOperationResult.Success(
            result,
            result.WasModified
                ? "Rain Frequency was updated."
                : "No changes were applied." +
                  Environment.NewLine +
                  Environment.NewLine +
                  "This preset already matches the current project.");
    }
}
