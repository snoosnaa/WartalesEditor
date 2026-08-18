namespace WartalesEditor.Models;

public enum RandomTraitPersonality
{
    Positive,
    Negative
}

public enum RandomTraitDoneBaseline
{
    Absent,
    True,
    False
}

public sealed class RandomTraitExclusionCandidate
{
    public required string Id { get; init; }

    public required string DisplayNameKey { get; init; }

    public required RandomTraitPersonality Personality { get; init; }

    public required RandomTraitDoneBaseline BaselineDone { get; init; }

    public required bool IsAllowed { get; init; }
}
