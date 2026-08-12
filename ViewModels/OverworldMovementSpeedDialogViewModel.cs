using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class OverworldMovementSpeedDialogViewModel : ObservableObject
{
    private readonly ProjectModel project;
    private readonly OverworldMovementSpeedService service;
    private OverworldMovementPresetOption? selectedPreset;
    private OverworldMovementPreset detectedPreset;

    public OverworldMovementSpeedDialogViewModel(
        ProjectModel project,
        OverworldMovementSpeedService service)
    {
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        RefreshFromProject();
    }

    public string Title => "Overworld Movement Speed";
    public IReadOnlyList<OverworldMovementPresetOption> Presets =>
        OverworldMovementSpeedService.Presets;

    public OverworldMovementPresetOption? SelectedPreset
    {
        get => selectedPreset;
        set
        {
            if (SetProperty(ref selectedPreset, value))
            {
                OnPropertyChanged(nameof(CanApply));
                OnPropertyChanged(nameof(PreviewText));
            }
        }
    }

    public bool CanApply =>
        detectedPreset != OverworldMovementPreset.Unavailable &&
        SelectedPreset != null;

    public string CurrentStateText => detectedPreset switch
    {
        OverworldMovementPreset.VeryFast => "Very Fast",
        OverworldMovementPreset.Custom => "Custom",
        OverworldMovementPreset.Unavailable => "Unavailable",
        _ => detectedPreset.ToString()
    };

    public string PreviewText => SelectedPreset == null
        ? detectedPreset == OverworldMovementPreset.Unavailable
            ? "Movement settings are not available for this project."
            : "Choose a preset to replace the current movement values."
        : $"{SelectedPreset.Name}: your party travels faster across the world map. " +
          "Other roaming parties keep their normal speeds.";

    public void SelectVanilla() =>
        SelectedPreset = FindPreset(OverworldMovementPreset.Vanilla);

    public void RefreshFromProject()
    {
        detectedPreset = service.DetectPreset(project);
        SelectedPreset = detectedPreset is
            OverworldMovementPreset.Vanilla or
            OverworldMovementPreset.Faster or
            OverworldMovementPreset.Fast or
            OverworldMovementPreset.VeryFast
                ? FindPreset(detectedPreset)
                : null;
        OnPropertyChanged(nameof(CurrentStateText));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
    }

    private static OverworldMovementPresetOption FindPreset(
        OverworldMovementPreset preset) =>
        (OverworldMovementPresetOption)
        OverworldMovementSpeedService.Presets
            .First(x => x.Preset == preset);
}
