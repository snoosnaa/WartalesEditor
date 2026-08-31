namespace WartalesEditor.Models;

public sealed record RequestBoardRewardsPresetOption(
    int Percentage,
    string Name,
    string Preview);

public sealed record RequestBoardRewardsPreview(
    int DifficultyCount,
    long CurrentMinimum,
    long CurrentMaximum,
    long ProposedMinimum,
    long ProposedMaximum);
