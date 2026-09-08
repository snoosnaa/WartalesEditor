using System.IO;
using System.Text.Json;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed record QuickBmsLocationResolution(
    QuickBmsToolchainInfo? Toolchain,
    string SuggestedDirectory);

public sealed class QuickBmsLocationService
{
    private const int LocationSchemaVersion = 1;
    private const string ExecutableFileName = "quickbms.exe";
    private const string ScriptFileName = "Shiro_Games_PAK_script.bms";

    private readonly string locationFilePath;
    private readonly string defaultDirectory;
    private readonly QuickBmsToolchainService toolchainService;

    public QuickBmsLocationService()
        : this(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Wartales Editor",
                "QuickBMS",
                "location.json"),
            Path.GetDirectoryName(
                QuickBmsImportOptions.CreateDefault()
                    .QuickBmsExecutablePath)!,
            new QuickBmsToolchainService())
    {
    }

    internal QuickBmsLocationService(
        string locationFilePath,
        string defaultDirectory,
        QuickBmsToolchainService toolchainService)
    {
        this.locationFilePath = Path.GetFullPath(
            string.IsNullOrWhiteSpace(locationFilePath)
                ? throw new ArgumentException("A location file path is required.", nameof(locationFilePath))
                : locationFilePath);
        this.defaultDirectory = NormalizeDirectory(
            string.IsNullOrWhiteSpace(defaultDirectory)
                ? throw new ArgumentException("A default directory is required.", nameof(defaultDirectory))
                : defaultDirectory);
        this.toolchainService = toolchainService
            ?? throw new ArgumentNullException(nameof(toolchainService));
    }

    internal string LocationFilePath => locationFilePath;

    public QuickBmsLocationResolution Resolve()
    {
        string? savedDirectory = ReadSavedDirectory();
        if (TryValidate(savedDirectory, out QuickBmsToolchainInfo? saved))
            return new QuickBmsLocationResolution(saved, savedDirectory!);

        if (TryValidate(defaultDirectory, out QuickBmsToolchainInfo? fallback))
            return new QuickBmsLocationResolution(fallback, defaultDirectory);

        return new QuickBmsLocationResolution(
            null,
            FindExistingDirectory(savedDirectory) ??
            FindExistingDirectory(defaultDirectory) ??
            defaultDirectory);
    }

    public QuickBmsToolchainInfo ValidateAndRemember(string directory)
    {
        QuickBmsToolchainInfo toolchain = Validate(directory);
        PersistDirectory(NormalizeDirectory(directory));
        return toolchain;
    }

    public QuickBmsToolchainInfo Validate(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ToolchainInvalid,
                "Select the folder containing both quickbms.exe and Shiro_Games_PAK_script.bms.");
        }

        string normalized = NormalizeDirectory(directory);
        return toolchainService.Validate(
            Path.Combine(normalized, ExecutableFileName),
            Path.Combine(normalized, ScriptFileName));
    }

    public QuickBmsImportOptions Apply(
        QuickBmsImportOptions options,
        QuickBmsToolchainInfo toolchain)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(toolchain);
        string directory = Path.GetDirectoryName(toolchain.ExecutablePath)
            ?? throw new InvalidOperationException("The QuickBMS folder could not be resolved.");
        return options.WithQuickBmsDirectory(directory);
    }

    private bool TryValidate(
        string? directory,
        out QuickBmsToolchainInfo? toolchain)
    {
        toolchain = null;
        if (string.IsNullOrWhiteSpace(directory)) return false;
        try
        {
            toolchain = Validate(directory);
            return true;
        }
        catch (QuickBmsImportException)
        {
            return false;
        }
    }

    private string? ReadSavedDirectory()
    {
        try
        {
            if (!File.Exists(locationFilePath)) return null;
            LocationDocument? document = JsonSerializer.Deserialize<LocationDocument>(
                File.ReadAllText(locationFilePath));
            return document?.Version == LocationSchemaVersion &&
                   !string.IsNullOrWhiteSpace(document.Directory)
                ? document.Directory
                : null;
        }
        catch (Exception exception) when (IsFilesystemException(exception) || exception is JsonException)
        {
            return null;
        }
    }

    private void PersistDirectory(string directory)
    {
        string parent = Path.GetDirectoryName(locationFilePath)
            ?? throw new IOException("The QuickBMS location could not be saved.");
        string candidatePath = Path.Combine(
            parent,
            $".{Path.GetFileName(locationFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(parent);
            string json = JsonSerializer.Serialize(
                new LocationDocument(LocationSchemaVersion, directory),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(candidatePath, json);
            LocationDocument? verified = JsonSerializer.Deserialize<LocationDocument>(
                File.ReadAllText(candidatePath));
            if (verified?.Version != LocationSchemaVersion ||
                !string.Equals(verified.Directory, directory, StringComparison.Ordinal))
            {
                throw new IOException("The QuickBMS location could not be verified before saving.");
            }

            File.Move(candidatePath, locationFilePath, overwrite: true);
        }
        catch (Exception exception) when (IsFilesystemException(exception) || exception is JsonException)
        {
            throw new IOException(
                "The QuickBMS files were found, but their location could not be saved.",
                exception);
        }
        finally
        {
            try
            {
                if (File.Exists(candidatePath)) File.Delete(candidatePath);
            }
            catch
            {
                // Best-effort cleanup of an unpublished candidate.
            }
        }
    }

    private static string? FindExistingDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            DirectoryInfo? current = new(NormalizeDirectory(path));
            while (current != null)
            {
                if (current.Exists) return current.FullName;
                current = current.Parent;
            }
        }
        catch (Exception exception) when (IsFilesystemException(exception))
        {
        }

        return null;
    }

    private static string NormalizeDirectory(string path) =>
        Path.GetFullPath(path).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

    private static bool IsFilesystemException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException or System.Security.SecurityException;

    private sealed record LocationDocument(int Version, string Directory);
}
