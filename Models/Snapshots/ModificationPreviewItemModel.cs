using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationPreviewItemModel
{
    public ModificationPreviewItemModel(
        ModificationMatchItemModel matchItem,
        ModificationPreviewStatus status,
        string reason,
        JToken? targetValue)
    {
        MatchItem = matchItem;
        Status = status;
        Reason = reason;

        TargetValue =
            targetValue?.DeepClone()
            ?? JValue.CreateNull();
    }

    public ModificationMatchItemModel MatchItem
    {
        get;
    }

    public ModificationPreviewStatus Status
    {
        get;
    }

    public string Reason { get; }

    public JToken TargetValue { get; }

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

    public JToken SnapshotOriginalValue =>
        MatchItem.SnapshotProperty
            .OriginalValue;

    public JToken SnapshotCurrentValue =>
        MatchItem.SnapshotProperty
            .CurrentValue;

    public bool CanApplySafely =>
        Status ==
        ModificationPreviewStatus.SafeToApply;

    public bool RequiresUserDecision =>
        Status ==
        ModificationPreviewStatus.Conflict;

    public bool IsSuccessfulWithoutChange =>
        Status ==
        ModificationPreviewStatus.AlreadyApplied;
}