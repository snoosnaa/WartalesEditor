namespace WartalesEditor.Models;

public sealed record PathLevelRequirementOption(
    int Percentage,
    string DisplayText);

public sealed record PathLevelRequirementsPreview(
    int CurrentMinimum,
    int CurrentMaximum,
    int ProposedMinimum,
    int ProposedMaximum,
    int CurrentBase,
    int CurrentNext,
    int ProposedBase,
    int ProposedNext,
    bool WasRounded);

public sealed record PathXpRewardOption(
    int Multiplier,
    string DisplayText);

public sealed record PathXpRewardsPreview(
    string PathId,
    string DisplayName,
    int TargetCount,
    int CurrentMinimum,
    int CurrentMaximum,
    int ProposedMinimum,
    int ProposedMaximum,
    long CurrentTotal,
    long ProposedTotal,
    int ExpectedChangedPropertyCount);
