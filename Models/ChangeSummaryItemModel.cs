namespace WartalesEditor.Models;

public sealed class ChangeSummaryItemModel
{
    public ChangeSummaryItemModel(
        SheetModel category,
        EntryModel setting,
        PropertyModel property,
        string settingName,
        string originalValue,
        string currentValue)
    {
        Category = category;
        Setting = setting;
        Property = property;
        SettingName = settingName;
        OriginalValue = originalValue;
        CurrentValue = currentValue;
    }

    public SheetModel Category { get; }

    public EntryModel Setting { get; }

    public PropertyModel Property { get; }

    public string CategoryName =>
        Category.Name;

    public string SettingName { get; }

    public string PropertyName =>
        Property.Name;

    public string OriginalValue { get; }

    public string CurrentValue { get; }
}