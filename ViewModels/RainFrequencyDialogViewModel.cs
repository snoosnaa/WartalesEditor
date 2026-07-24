using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class RainFrequencyDialogViewModel
    : ObservableObject
{
    private readonly ProjectModel project;
    private readonly RainFrequencyService service;
    private RainFrequencyPresetOption? selectedPreset;
    private RainFrequencyPreset detectedPreset;

    public RainFrequencyDialogViewModel(
        ProjectModel project,
        RainFrequencyService service)
    {
        this.project = project
            ?? throw new ArgumentNullException(nameof(project));
        this.service = service
            ?? throw new ArgumentNullException(nameof(service));
        RefreshFromProject();
    }

    public string Title => "Rain Frequency";

    public IReadOnlyList<RainFrequencyPresetOption> Presets =>
        RainFrequencyService.Presets;

    public RainFrequencyPresetOption? SelectedPreset
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
        detectedPreset != RainFrequencyPreset.Unavailable &&
        SelectedPreset != null;

    public string CurrentStateText => detectedPreset switch
    {
        RainFrequencyPreset.LessRain => "Less Rain",
        RainFrequencyPreset.RareRain => "Rare Rain",
        RainFrequencyPreset.NoRain => "No Rain",
        RainFrequencyPreset.Custom => "Custom",
        RainFrequencyPreset.Unavailable => "Unavailable",
        _ => "Vanilla"
    };

    public string PreviewText => SelectedPreset == null
        ? detectedPreset == RainFrequencyPreset.Unavailable
            ? "Rain Frequency settings are not available for this project."
            : "Choose a preset to replace the current custom values."
        : SelectedPreset.Preview;

    public void SelectVanilla()
    {
        SelectedPreset =
            FindPreset(RainFrequencyPreset.Vanilla);
    }

    public void RefreshFromProject()
    {
        detectedPreset = service.DetectPreset(project);
        SelectedPreset = detectedPreset is
            RainFrequencyPreset.Vanilla or
            RainFrequencyPreset.LessRain or
            RainFrequencyPreset.RareRain or
            RainFrequencyPreset.NoRain
                ? FindPreset(detectedPreset)
                : null;
        OnPropertyChanged(nameof(CurrentStateText));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
    }

    private static RainFrequencyPresetOption FindPreset(
        RainFrequencyPreset preset)
    {
        return RainFrequencyService.Presets
            .First(option => option.Preset == preset);
    }
}
