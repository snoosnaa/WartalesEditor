namespace WartalesEditor.Models;

public class SearchResultModel
{
    public string CategoryName { get; set; } = "";

    public string SettingName { get; set; } = "";

    public string LocalizedName { get; set; } = "";

    public string MatchedProperty { get; set; } = "";

    public SheetModel? Category { get; set; }

    public EntryModel? Setting { get; set; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(LocalizedName)
            ? SettingName
            : $"{LocalizedName} ({SettingName})";
}