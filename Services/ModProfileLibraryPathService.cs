using System;
using System.IO;

namespace WartalesEditor.Services;

public sealed class ModProfileLibraryPathService
{
    private const string ApplicationFolderName =
        "Wartales Editor";

    private const string ProfilesFolderName =
        "Profiles";

    public string GetLibraryDirectory()
    {
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