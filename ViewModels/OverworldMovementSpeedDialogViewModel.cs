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
    public GameplayApplyFeedbackViewModel ApplyFeedback { get; } = new();
    public IReadOnlyList<OverworldMovementPresetOption> Presets =>
        OverworldMovementSpeedService.Presets;

    public OverworldMovementPresetOption? SelectedPreset
    {
        get => selectedPreset;
        set
        {
            if (SetProperty(ref selectedPreset, value))
            {
                ApplyFeedback.Clear();
                OnPropertyChanged(nameof(CanApply));
                OnPropertyChanged(nameof(PreviewText));
            }
        }
    }

    public bool CanApply =>
        detectedPreset != OverworldMovementPreset.Unavailable &&
        SelectedPreset != null;

    public bool CanRestorePreviousValues =>
        service.CanRestorePreviousValues(project);

    public string CurrentStateText => detectedPreset switch
    {
        OverworldMovementPreset.Custom => "Custom",
        OverworldMovementPreset.Unavailable => "Unavailable",
        _ => FindPreset(detectedPreset).Name
    };

    public string PreviewText => SelectedPreset == null
        ? detectedPreset == OverworldMovementPreset.Unavailable
            ? "Movement settings are not available for this project."
            : "Choose a preset to replace the current movement values."
        : SelectedPreset.Preset switch
        {
            OverworldMovementPreset.Vanilla =>
                "Uses the standard Wartales movement speeds.",
            OverworldMovementPreset.Faster =>
                "Moderately increases overworld walking and running speed.",
            OverworldMovementPreset.Fast =>
                "Further increases overworld walking and running speed.",
            OverworldMovementPreset.VeryFast =>
                "Greatly increases overworld walking and running speed.",
            _ =>
                "Choose a supported movement speed preset."
        };

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
        OnPropertyChanged(nameof(CanRestorePreviousValues));
    }

    private static OverworldMovementPresetOption FindPreset(
        OverworldMovementPreset preset) =>
        (OverworldMovementPresetOption)
        OverworldMovementSpeedService.Presets
            .First(x => x.Preset == preset);
}
