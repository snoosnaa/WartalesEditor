namespace WartalesEditor.Models.Profiles;

public static class ModProfileFormat
{
    public const int LegacyVersion = 1;

    public const int ProvenanceVersion = 3;

    public const int ProfileOperationIntentVersion = 4;

    public const int ProfileImpactManifestVersion = 5;

    public const int CurrentVersion =
        ProfileImpactManifestVersion;

    public const string DefaultFileExtension =
        ".wtprofile";
}
