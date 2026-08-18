using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class RandomTraitExclusionItemViewModel : ObservableObject
{
    private bool isAllowed;

    public RandomTraitExclusionItemViewModel(
        RandomTraitExclusionCandidate candidate,
        string displayName,
        Action selectionChanged)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? candidate.Id : displayName;
        this.selectionChanged = selectionChanged
            ?? throw new ArgumentNullException(nameof(selectionChanged));
        isAllowed = candidate.IsAllowed;
    }

    private readonly Action selectionChanged;
    public RandomTraitExclusionCandidate Candidate { get; }
    public string Id => Candidate.Id;
    public string DisplayName { get; }
    public bool IsDisabledByGameData => Candidate.BaselineDone == RandomTraitDoneBaseline.False;
    public string? StatusNote => IsDisabledByGameData
        ? "Disabled by current game data unless you choose to allow it."
        : null;

    public bool IsAllowed
    {
        get => isAllowed;
        set
        {
            if (!SetProperty(ref isAllowed, value)) return;
            selectionChanged();
        }
    }

    public void RestoreDefault() => IsAllowed =
        Candidate.BaselineDone != RandomTraitDoneBaseline.False;
}

public sealed class RandomTraitExclusionsDialogViewModel : ObservableObject
{
    private string searchText = string.Empty;

    public RandomTraitExclusionsDialogViewModel(
        ProjectModel project,
        RandomTraitExclusionsService service,
        LocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(localizationService);

        IReadOnlyList<RandomTraitExclusionCandidate> candidates = service.Discover(project);
        PositiveTraits = new ObservableCollection<RandomTraitExclusionItemViewModel>(
            CreateItems(candidates.Where(candidate =>
                candidate.Personality == RandomTraitPersonality.Positive), localizationService));
        NegativeTraits = new ObservableCollection<RandomTraitExclusionItemViewModel>(
            CreateItems(candidates.Where(candidate =>
                candidate.Personality == RandomTraitPersonality.Negative), localizationService));
        PositiveTraitsView = CollectionViewSource.GetDefaultView(PositiveTraits);
        NegativeTraitsView = CollectionViewSource.GetDefaultView(NegativeTraits);
        PositiveTraitsView.Filter = MatchesSearch;
        NegativeTraitsView.Filter = MatchesSearch;
    }

    public ObservableCollection<RandomTraitExclusionItemViewModel> PositiveTraits { get; }
    public ObservableCollection<RandomTraitExclusionItemViewModel> NegativeTraits { get; }
    public ICollectionView PositiveTraitsView { get; }
    public ICollectionView NegativeTraitsView { get; }
    public GameplayApplyFeedbackViewModel ApplyFeedback { get; } = new();
    public bool CanApply => PositiveTraits.Count + NegativeTraits.Count > 0;
    public string PositiveHeading => $"Positive Traits ({PositiveTraits.Count})";
    public string NegativeHeading => $"Negative Traits ({NegativeTraits.Count})";

    public string SearchText
    {
        get => searchText;
        set
        {
            if (!SetProperty(ref searchText, value ?? string.Empty)) return;
            PositiveTraitsView.Refresh();
            NegativeTraitsView.Refresh();
        }
    }

    public IReadOnlyCollection<string> GetAllowedTraitIds() =>
        PositiveTraits.Concat(NegativeTraits)
            .Where(item => item.IsAllowed)
            .Select(item => item.Id)
            .ToArray();

    public void SelectAll() => SetAll(true);
    public void ClearAll() => SetAll(false);

    public void RestoreDefaults()
    {
        foreach (RandomTraitExclusionItemViewModel item in PositiveTraits.Concat(NegativeTraits))
            item.RestoreDefault();
        ApplyFeedback.Clear();
    }

    public void RefreshFromProject(
        ProjectModel project,
        RandomTraitExclusionsService service)
    {
        IReadOnlyDictionary<string, RandomTraitExclusionCandidate> current =
            service.Discover(project).ToDictionary(candidate => candidate.Id, StringComparer.Ordinal);
        foreach (RandomTraitExclusionItemViewModel item in PositiveTraits.Concat(NegativeTraits))
            if (current.TryGetValue(item.Id, out RandomTraitExclusionCandidate? candidate))
                item.IsAllowed = candidate.IsAllowed;
    }

    private IEnumerable<RandomTraitExclusionItemViewModel> CreateItems(
        IEnumerable<RandomTraitExclusionCandidate> candidates,
        LocalizationService localizationService) =>
        candidates.Select(candidate => new RandomTraitExclusionItemViewModel(
            candidate,
            localizationService.GetLocalizedName(candidate.DisplayNameKey)
                ?? candidate.DisplayNameKey,
            OnSelectionChanged))
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase);

    private bool MatchesSearch(object item)
    {
        if (item is not RandomTraitExclusionItemViewModel trait) return false;
        string query = SearchText.Trim();
        return query.Length == 0 ||
            trait.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
            trait.Id.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void SetAll(bool allowed)
    {
        foreach (RandomTraitExclusionItemViewModel item in PositiveTraits.Concat(NegativeTraits))
            item.IsAllowed = allowed;
        ApplyFeedback.Clear();
    }

    private void OnSelectionChanged() => ApplyFeedback.Clear();
}
