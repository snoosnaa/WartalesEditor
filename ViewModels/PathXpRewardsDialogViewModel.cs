using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class PathXpRewardsDialogViewModel
    : ObservableObject, IGameplayProjectRefreshable
{
    public PathXpRewardsDialogViewModel(
        ProjectModel project,
        PathXpRewardsService service)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(service);
        Sections = new ObservableCollection<PathXpRewardsSectionViewModel>(
            PathXpRewardsService.PathIds.Select(pathId =>
                new PathXpRewardsSectionViewModel(project, service, pathId)));
    }

    public string Title => "Path XP Rewards";
    public ObservableCollection<PathXpRewardsSectionViewModel> Sections { get; }

    public void RefreshAfterProjectOperation()
    {
        foreach (PathXpRewardsSectionViewModel section in Sections)
            section.RefreshAfterProjectOperation();
    }
}

public sealed class PathXpRewardsSectionViewModel : ObservableObject
{
    private readonly ProjectModel project;
    private readonly PathXpRewardsService service;
    private PathXpRewardOption? selectedOption;
    private int detectedMultiplier;
    private int? loadedMultiplier;

    internal PathXpRewardsSectionViewModel(
        ProjectModel project,
        PathXpRewardsService service,
        string pathId)
    {
        this.project = project;
        this.service = service;
        PathId = pathId;
        RefreshFromProject();
    }

    public string PathId { get; }
    public string DisplayName => service.GetDisplayName(PathId);
    public IReadOnlyList<PathXpRewardOption> Options => PathXpRewardsService.Options;
    public GameplayApplyFeedbackViewModel ApplyFeedback { get; } = new();

    public PathXpRewardOption? SelectedOption
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
    public bool CanRestorePreviousValues =>
        service.CanRestorePreviousValues(project, PathId);
    public string CurrentStateText => detectedMultiplier == 1
        ? "Original"
        : $"{detectedMultiplier}×";

    public string PreviewText
    {
        get
        {
            if (SelectedOption == null) return "Choose a challenge reward multiplier.";
            PathXpRewardsPreview preview = service.CreatePreview(
                project, PathId, SelectedOption.Multiplier);
            return preview.ExpectedChangedPropertyCount == 0
                ? $"Current reward range: {preview.CurrentMinimum:N0}–{preview.CurrentMaximum:N0} XP"
                : $"{preview.CurrentMinimum:N0}–{preview.CurrentMaximum:N0} XP → " +
                  $"{preview.ProposedMinimum:N0}–{preview.ProposedMaximum:N0} XP";
        }
    }

    public void RefreshFromProject()
    {
        detectedMultiplier = service.DetectMultiplier(project, PathId);
        SelectedOption = Options.First(option => option.Multiplier == detectedMultiplier);
        loadedMultiplier = SelectedOption.Multiplier;
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(CurrentStateText));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanRestorePreviousValues));
    }

    internal void RefreshAfterProjectOperation()
    {
        int? pending = SelectedOption?.Multiplier;
        bool preserve = pending != loadedMultiplier;
        RefreshFromProject();
        if (preserve && pending.HasValue)
            SelectedOption = Options.FirstOrDefault(option => option.Multiplier == pending.Value)
                ?? SelectedOption;
    }
}
