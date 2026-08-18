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
        : this(service, preset, false)
    {
    }

    public RainFrequencyOperation(
        RainFrequencyService service,
        RainFrequencyPreset preset,
        bool restorePreviousValues)
    {
        this.service = service
            ?? throw new ArgumentNullException(nameof(service));
        Preset = preset;
        RestorePreviousValues = restorePreviousValues;
    }

    public RainFrequencyPreset Preset { get; }
    public bool RestorePreviousValues { get; }

    public string Name => "Rain Frequency";

    public string Description =>
        "Controls how often ordinary rain occurs across supported regions.";

    public bool CanExecute(ProjectModel project) =>
        project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result =
            RestorePreviousValues
                ? service.RestorePreviousValues(project)
                : service.Apply(project, Preset);
        return ProjectOperationResult.Success(
            result,
            result.WasModified
                ? RestorePreviousValues
                    ? "Previous rain values were restored."
                    : "Rain Frequency was updated."
                : "No changes were applied." +
                  Environment.NewLine +
                  Environment.NewLine +
                  (RestorePreviousValues
                      ? "The previous values already match the current project."
                      : "This preset already matches the current project."));
    }
}
