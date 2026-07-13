namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationApplyItemResultModel
{
    public ModificationApplyItemResultModel(
        ModificationMatchItemModel matchItem,
        ModificationApplyStatus status,
        string reason)
    {
        MatchItem = matchItem;
        Status = status;
        Reason = reason;
    }

    public ModificationMatchItemModel MatchItem
    {
        get;
    }

    public ModificationApplyStatus Status
    {
        get;
    }

    public string Reason { get; }

    public string CategoryName =>
        MatchItem.CategoryName;

    public string SettingId =>
        MatchItem.SettingId;

    public string SettingName =>
        MatchItem.SettingName;

    public string SettingDisplayName =>
        MatchItem.SettingDisplayName;

    public string PropertyName =>
        MatchItem.PropertyName;

    public bool WasApplied =>
        Status ==
        ModificationApplyStatus.Applied;

    public bool IsSuccessful =>
        Status ==
            ModificationApplyStatus.Applied
        ||
        Status ==
            ModificationApplyStatus.NoChangeRequired;
}