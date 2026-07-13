namespace WartalesEditor.Models.Snapshots;

public enum ModificationMatchStatus
{
    Matched,

    CategoryNotFound,

    CategoryAmbiguous,

    SettingIdentifierMissing,

    SettingNotFound,

    SettingAmbiguous,

    PropertyNotFound,

    PropertyAmbiguous
}