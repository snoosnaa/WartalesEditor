namespace WartalesEditor.Models;

public enum LanguageDataAvailability
{
    Unavailable,
    Available,
    Invalid
}

public sealed class LanguageDataState
{
    private LanguageDataState(
        LanguageDataAvailability availability,
        LanguageDataMetadata? metadata,
        int mappingCount,
        string failureMessage)
    {
        Availability = availability;
        Metadata = metadata;
        MappingCount = mappingCount;
        FailureMessage = failureMessage;
    }

    public LanguageDataAvailability Availability { get; }

    public LanguageDataMetadata? Metadata { get; }

    public int MappingCount { get; }

    public string FailureMessage { get; }

    public bool IsAvailable =>
        Availability == LanguageDataAvailability.Available;

    public static LanguageDataState Unavailable(
        string message = "Language data is not set up.") =>
        new(
            LanguageDataAvailability.Unavailable,
            null,
            0,
            message);

    public static LanguageDataState Invalid(
        string message) =>
        new(
            LanguageDataAvailability.Invalid,
            null,
            0,
            message);

    public static LanguageDataState Available(
        LanguageDataMetadata metadata,
        int mappingCount) =>
        new(
            LanguageDataAvailability.Available,
            metadata,
            mappingCount,
            string.Empty);
}
