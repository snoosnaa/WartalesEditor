using System;
using System.IO;

namespace WartalesEditor.Services;

public sealed class ModProfileLibraryPathService
{
    private const string ApplicationFolderName =
        "Wartales Editor";

    private const string ProfilesFolderName =
        "Profiles";

    private readonly string? libraryDirectoryOverride;

    public ModProfileLibraryPathService()
    {
    }

    public ModProfileLibraryPathService(
        string libraryDirectory)
    {
        if (string.IsNullOrWhiteSpace(libraryDirectory))
        {
            throw new ArgumentException(
                "A profile library directory is required.",
                nameof(libraryDirectory));
        }

        libraryDirectoryOverride = Path.GetFullPath(libraryDirectory);
    }

    public string GetLibraryDirectory()
    {
        if (libraryDirectoryOverride != null)
        {
            return libraryDirectoryOverride;
        }

        string documentsDirectory =
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);

        if (string.IsNullOrWhiteSpace(
                documentsDirectory))
        {
            throw new InvalidOperationException(
                "The user's Documents folder could not be located.");
        }

        return Path.Combine(
            documentsDirectory,
            ApplicationFolderName,
            ProfilesFolderName);
    }

    public string EnsureLibraryDirectory()
    {
        string libraryDirectory =
            GetLibraryDirectory();

        Directory.CreateDirectory(
            libraryDirectory);

        return libraryDirectory;
    }
}
