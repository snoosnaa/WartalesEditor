namespace WartalesEditor.Models;

public class SearchResultModel
{
    public string CategoryName { get; set; } =
        string.Empty;

    public string SettingName { get; set; } =
        string.Empty;

    public string LocalizedName { get; set; } =
        string.Empty;

    public string MatchedProperty { get; set; } =
        string.Empty;

    public string MatchedValue { get; set; } =
        string.Empty;

    public SheetModel? Category { get; set; }

    public EntryModel? Setting { get; set; }

    public string DisplayName
    {
        get
        {
            string settingDisplayName =
                string.IsNullOrWhiteSpace(
                    LocalizedName)
                    ? SettingName
                    : $"{LocalizedName} ({SettingName})";

            if (string.IsNullOrWhiteSpace(
                    MatchedValue))
            {
                return settingDisplayName;
            }

            return
                $"{settingDisplayName} — " +
                $"{MatchedProperty}: {MatchedValue}";
        }
    }
}