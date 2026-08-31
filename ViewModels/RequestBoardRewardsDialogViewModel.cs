using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class RequestBoardRewardsDialogViewModel
    : ObservableObject
{
    private readonly ProjectModel project;
    private readonly RequestBoardRewardsService service;
    private RequestBoardRewardsPresetOption? selectedPreset;
    private int detectedPercentage;

    public RequestBoardRewardsDialogViewModel(
        ProjectModel project,
        RequestBoardRewardsService service)
    {
        this.project = project
            ?? throw new ArgumentNullException(nameof(project));
        this.service = service
            ?? throw new ArgumentNullException(nameof(service));
        RefreshFromProject();
    }

    public string Title => "Request Board Rewards";
    public GameplayApplyFeedbackViewModel ApplyFeedback { get; } = new();

    public IReadOnlyList<RequestBoardRewardsPresetOption> Presets =>
        RequestBoardRewardsService.Presets;

    public RequestBoardRewardsPresetOption? SelectedPreset
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

    public bool CanApply => SelectedPreset != null;

    public bool CanRestorePreviousValues =>
        service.CanRestorePreviousValues(project);

    public string CurrentStateText => $"{detectedPercentage}%";

    public string PreviewText
    {
        get
        {
            if (SelectedPreset == null)
                return "Choose a base reward preset.";

            RequestBoardRewardsPreview preview = service.CreatePreview(
                project,
                SelectedPreset.Percentage);
            string rangeWord = preview.DifficultyCount == 1
                ? "range"
                : "ranges";
            return $"Across {preview.DifficultyCount:N0} reward {rangeWord}, " +
                   $"current base rewards span " +
                   $"{preview.CurrentMinimum:N0}–{preview.CurrentMaximum:N0} Krowns. " +
                   $"This preset would produce " +
                   $"{preview.ProposedMinimum:N0}–{preview.ProposedMaximum:N0} Krowns " +
                   "from the captured previous values.";
        }
    }

    public void RefreshFromProject()
    {
        detectedPercentage = service.DetectPercentage(project);
        SelectedPreset = Presets.First(option =>
            option.Percentage == detectedPercentage);
        OnPropertyChanged(nameof(CurrentStateText));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanRestorePreviousValues));
    }
}
