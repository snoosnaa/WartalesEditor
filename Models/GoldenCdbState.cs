using System.IO;

namespace WartalesEditor.Models;

public enum GoldenCdbAvailability
{
    NotSet,
    Available,
    Invalid,
    Inaccessible
}

public sealed class GoldenCdbState
{
    private GoldenCdbState(
        GoldenCdbAvailability availability,
        string canonicalPath,
        string identity,
        string message,
        bool hasCleanupWarning = false)
    {
        Availability = availability;
        CanonicalPath = canonicalPath;
        Identity = identity;
        Message = message;
        HasCleanupWarning = hasCleanupWarning;
    }

    public GoldenCdbAvailability Availability { get; }

    public string CanonicalPath { get; }

    public string CanonicalFileName =>
        Path.GetFileName(CanonicalPath);

    public string Identity { get; }

    public string ShortIdentity =>
        Identity.StartsWith("sha256:", StringComparison.Ordinal) &&
        Identity.Length >= 19
            ? Identity[7..19]
            : string.Empty;

    public string Message { get; }

    public bool HasCleanupWarning { get; }

    public bool IsAvailable =>
        Availability == GoldenCdbAvailability.Available;

    public bool IsNotSet =>
        Availability == GoldenCdbAvailability.NotSet;

    public bool CanonicalFileExists =>
        Availability != GoldenCdbAvailability.NotSet;

    public static GoldenCdbState NotSet(string path) =>
        new(
            GoldenCdbAvailability.NotSet,
            path,
            string.Empty,
            "Golden CDB is not set.");

    public static GoldenCdbState Available(
        string path,
        string identity,
        string? cleanupWarning = null) =>
        new(
            GoldenCdbAvailability.Available,
            path,
            identity,
            string.IsNullOrWhiteSpace(cleanupWarning)
                ? "Golden CDB is set."
                : cleanupWarning,
            !string.IsNullOrWhiteSpace(cleanupWarning));

    public static GoldenCdbState Invalid(
        string path,
        string message) =>
        new(
            GoldenCdbAvailability.Invalid,
            path,
            string.Empty,
            message);

    public static GoldenCdbState Inaccessible(
        string path,
        string message) =>
        new(
            GoldenCdbAvailability.Inaccessible,
            path,
            string.Empty,
            message);
}

public sealed record GoldenCdbReference(
    ProjectModel Project,
    string Identity,
    long Length,
    string CanonicalPath);
