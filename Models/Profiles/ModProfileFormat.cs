namespace WartalesEditor.Models.Profiles;

public static class ModProfileFormat
{
    public const int LegacyVersion = 1;

    public const int ProvenanceVersion = 3;

    public const int ProfileOperationIntentVersion = 4;

    public const int CurrentVersion =
        ProfileOperationIntentVersion;

    public const string DefaultFileExtension =
        ".wtprofile";
}
