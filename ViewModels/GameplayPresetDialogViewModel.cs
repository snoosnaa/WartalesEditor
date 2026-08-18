using System;
using System.Collections.Generic;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class GameplayPresetDialogViewModel : ObservableObject
{
    private readonly ProjectModel project;
    private readonly GameplayPresetService service;
    private GameplayPresetOption? selectedPreset;
    private string currentStateText = "Unavailable";

    public GameplayPresetDialogViewModel(
        ProjectModel project,
        GameplayPresetService service,
        ProgressionType operationType)
    {
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        Definition = GameplayPresetCatalog.Get(operationType);
        RefreshFromProject();
    }

    public GameplayPresetDefinition Definition { get; }
    public GameplayApplyFeedbackViewModel ApplyFeedback { get; } = new();
    public ProgressionType OperationType => Definition.OperationType;
    public string Title => Definition.Title;
    public string Description => Definition.Description;
    public IReadOnlyList<GameplayPresetOption> Presets => Definition.Presets;
    public string CurrentStateText => currentStateText;

    public GameplayPresetOption? SelectedPreset
    {
        get => selectedPreset;
        set
        {
            if (!SetProperty(ref selectedPreset, value)) return;
            ApplyFeedback.Clear();
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(PreviewText));
        }
    }

    public bool CanApply => SelectedPreset != null && currentStateText != "Unavailable";
    public bool CanRestorePreviousValues =>
        service.CanRestorePreviousValues(project, OperationType);

    public string PreviewText => SelectedPreset == null
        ? "Choose a preset to preview its gameplay effect."
        : $"{SelectedPreset.DisplayText}{Environment.NewLine}{Environment.NewLine}" +
          SelectedPreset.Description;

    public void SelectVanilla() =>
        SelectedPreset = Definition.Presets[0];

    public void RefreshFromProject()
    {
        try
        {
            GameplayPresetOption? detected =
                service.DetectPreset(project, OperationType);
            currentStateText = detected?.Name ?? "Custom";
            selectedPreset = detected;
        }
        catch
        {
            currentStateText = "Unavailable";
            selectedPreset = null;
        }

        OnPropertyChanged(nameof(CurrentStateText));
        OnPropertyChanged(nameof(SelectedPreset));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanRestorePreviousValues));
    }
}
