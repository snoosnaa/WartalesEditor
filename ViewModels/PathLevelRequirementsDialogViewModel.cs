using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class PathLevelRequirementsDialogViewModel
    : ObservableObject, IGameplayProjectRefreshable
{
    private readonly ProjectModel project;
    private readonly PathLevelRequirementsService service;
    private PathLevelRequirementOption? selectedOption;
    private int detectedPercentage;
    private int? loadedPercentage;

    public PathLevelRequirementsDialogViewModel(
        ProjectModel project,
        PathLevelRequirementsService service)
    {
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        RefreshFromProject();
    }

    public string Title => "Path Level Requirements";
    public IReadOnlyList<PathLevelRequirementOption> Options =>
        PathLevelRequirementsService.Options;
    public GameplayApplyFeedbackViewModel ApplyFeedback { get; } = new();

    public PathLevelRequirementOption? SelectedOption
    {
        get => selectedOption;
        set
        {
            if (SetProperty(ref selectedOption, value))
            {
                ApplyFeedback.Clear();
                OnPropertyChanged(nameof(CanApply));
                OnPropertyChanged(nameof(PreviewText));
            }
        }
    }

    public bool CanApply => SelectedOption != null;
    public bool CanRestorePreviousValues => service.CanRestorePreviousValues(project);
    public string CurrentStateText => detectedPercentage == 100
        ? "Original"
        : $"{detectedPercentage}%";

    public string PreviewText
    {
        get
        {
            if (SelectedOption == null) return "Choose a Path requirement setting.";
            PathLevelRequirementsPreview preview = service.CreatePreview(
                project, SelectedOption.Percentage);
            string rounding = preview.WasRounded
                ? " Integer rounding is reflected in these values."
                : string.Empty;
            return $"Current requirements: {preview.CurrentMinimum:N0}–{preview.CurrentMaximum:N0} XP. " +
                   $"Selected: {preview.ProposedMinimum:N0}–{preview.ProposedMaximum:N0} XP." + rounding;
        }
    }

    public void RefreshFromProject()
    {
        detectedPercentage = service.DetectPercentage(project);
        SelectedOption = Options.First(option => option.Percentage == detectedPercentage);
        loadedPercentage = SelectedOption.Percentage;
        OnPropertyChanged(nameof(CurrentStateText));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanRestorePreviousValues));
    }

    public void RefreshAfterProjectOperation()
    {
        int? pending = SelectedOption?.Percentage;
        bool preserve = pending != loadedPercentage;
        RefreshFromProject();
        if (preserve && pending.HasValue)
            SelectedOption = Options.FirstOrDefault(option => option.Percentage == pending.Value)
                ?? SelectedOption;
    }
}
