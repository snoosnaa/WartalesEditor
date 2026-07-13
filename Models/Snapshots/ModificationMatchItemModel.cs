namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationMatchItemModel
{
    public ModificationMatchItemModel(
        ModificationSnapshotCategoryModel snapshotCategory,
        ModificationSnapshotSettingModel snapshotSetting,
        ModificationSnapshotPropertyModel snapshotProperty,
        ModificationMatchStatus status,
        string reason,
        SheetModel? targetCategory = null,
        EntryModel? targetSetting = null,
        PropertyModel? targetProperty = null)
    {
        SnapshotCategory = snapshotCategory;
        SnapshotSetting = snapshotSetting;
        SnapshotProperty = snapshotProperty;

        Status = status;
        Reason = reason;

        TargetCategory = targetCategory;
        TargetSetting = targetSetting;
        TargetProperty = targetProperty;
    }

    public ModificationSnapshotCategoryModel
        SnapshotCategory
    { get; }

    public ModificationSnapshotSettingModel
        SnapshotSetting
    { get; }

    public ModificationSnapshotPropertyModel
        SnapshotProperty
    { get; }

    public ModificationMatchStatus Status { get; }

    public string Reason { get; }

    public SheetModel? TargetCategory { get; }

    public EntryModel? TargetSetting { get; }

    public PropertyModel? TargetProperty { get; }

    public string CategoryName =>
        SnapshotCategory.Name;

    public string SettingId =>
        SnapshotSetting.Id;

    public string SettingName =>
        SnapshotSetting.Name;

    public string SettingDisplayName =>
        SnapshotSetting.DisplayName;

    public string PropertyName =>
        SnapshotProperty.Name;

    public bool IsMatched =>
        Status == ModificationMatchStatus.Matched
        &&
        TargetCategory != null
        &&
        TargetSetting != null
        &&
        TargetProperty != null;
}