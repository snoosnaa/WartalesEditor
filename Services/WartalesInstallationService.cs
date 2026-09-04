using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WartalesEditor.Services;

public sealed record WartalesPackageInfo(
    string InstallationDirectory,
    string PackagePath,
    long PackageSize);

public sealed record WartalesInstallationResolution(
    WartalesPackageInfo? Installation,
    string? SuggestedDirectory)
{
    public bool RequiresSelection => Installation == null;
}

public sealed class WartalesInstallationService
{
    private const string WartalesAppId = "1527950";
    private const int LocationSchemaVersion = 1;

    private static readonly byte[] ExpectedSignature =
    {
        (byte)'P', (byte)'A', (byte)'K', 0
    };

    private static readonly Regex QuotedPairPattern =
        new(
            "\\\"(?<key>[^\\\"]+)\\\"\\s*\\\"(?<value>[^\\\"]*)\\\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string locationFilePath;
    private readonly Func<IReadOnlyList<string>> steamRootsProvider;

    public WartalesInstallationService()
        : this(
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "Wartales Editor",
                "Wartales Installation",
                "location.json"),
            DiscoverSteamRoots)
    {
    }

    internal WartalesInstallationService(
        string locationFilePath,
        Func<IReadOnlyList<string>> steamRootsProvider)
    {
        this.locationFilePath =
            string.IsNullOrWhiteSpace(locationFilePath)
                ? throw new ArgumentException(
                    "A location file path is required.",
                    nameof(locationFilePath))
                : Path.GetFullPath(locationFilePath);
        this.steamRootsProvider =
            steamRootsProvider
            ?? throw new ArgumentNullException(
                nameof(steamRootsProvider));
    }

    internal string LocationFilePath => locationFilePath;

    public WartalesInstallationResolution Resolve(
        string? legacyInstallationDirectory = null)
    {
        string? savedDirectory = ReadSavedInstallationDirectory();
        if (TryValidate(savedDirectory, out WartalesPackageInfo? saved))
        {
            return new WartalesInstallationResolution(
                saved,
                saved.InstallationDirectory);
        }

        List<string> steamRoots = GetSteamRootsSafely();
        List<WartalesPackageInfo> discovered =
            DiscoverSteamInstallations(steamRoots);

        if (discovered.Count == 1)
        {
            WartalesPackageInfo installation = discovered[0];
            return new WartalesInstallationResolution(
                installation,
                installation.InstallationDirectory);
        }

        string? suggestedDirectory =
            GetSuggestedDirectory(
                savedDirectory,
                discovered,
                steamRoots,
                legacyInstallationDirectory);

        if (discovered.Count > 1)
        {
            return new WartalesInstallationResolution(
                null,
                suggestedDirectory);
        }

        if (TryValidate(
                legacyInstallationDirectory,
                out WartalesPackageInfo? legacy))
        {
            return new WartalesInstallationResolution(
                legacy,
                legacy.InstallationDirectory);
        }

        return new WartalesInstallationResolution(
            null,
            suggestedDirectory);
    }

    public WartalesPackageInfo ValidateAndRemember(
        string installationDirectory)
    {
        WartalesPackageInfo installation = Validate(installationDirectory);
        PersistInstallationDirectory(installation.InstallationDirectory);
        return installation;
    }

    public WartalesPackageInfo Validate(string installationDirectory)
    {
        if (string.IsNullOrWhiteSpace(installationDirectory)
            || !Directory.Exists(installationDirectory))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.WartalesInstallationInvalid,
                "The selected folder was not recognized as a Wartales installation. Select the folder containing Wartales.exe and res.pak.");
        }

        string fullDirectory;
        try
        {
            fullDirectory = NormalizeDirectory(installationDirectory);
            _ = Directory.EnumerateFileSystemEntries(fullDirectory)
                .Take(1)
                .ToArray();
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.WartalesInstallationInvalid,
                "The Wartales installation exists, but it could not be read. Check folder access and try again.",
                exception);
        }

        string executablePath = Path.Combine(fullDirectory, "Wartales.exe");
        if (!IsRegularFile(executablePath))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.WartalesInstallationInvalid,
                "The selected folder was not recognized as a Wartales installation. Select the folder containing Wartales.exe and res.pak.");
        }

        string packagePath = Path.Combine(fullDirectory, "res.pak");
        if (!File.Exists(packagePath))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.PackageMissing,
                "The selected Wartales installation is incomplete because res.pak is missing. Verify the game files and try again.");
        }

        FileInfo package;
        try
        {
            package = new FileInfo(packagePath);
            if (!IsRegularFile(package)
                || package.Length <= ExpectedSignature.Length)
            {
                throw new QuickBmsImportException(
                    QuickBmsImportFailureKind.PackageInvalid,
                    "The Wartales game package is empty or invalid. Verify the game files and try again.");
            }

            using FileStream stream = new(
                package.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            Span<byte> signature = stackalloc byte[4];

            if (stream.Read(signature) != signature.Length
                || !signature.SequenceEqual(ExpectedSignature))
            {
                throw new QuickBmsImportException(
                    QuickBmsImportFailureKind.PackageInvalid,
                    "The selected Wartales game package does not have the expected format. Verify the game files and try again.");
            }
        }
        catch (QuickBmsImportException)
        {
            throw;
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.PackageInvalid,
                "The Wartales game package could not be read. Close any program using it, check folder access, and try again.",
                exception);
        }

        return new WartalesPackageInfo(
            fullDirectory,
            package.FullName,
            package.Length);
    }

    public WartalesPackageInfo ValidateForExport(string installationDirectory)
    {
        WartalesPackageInfo package = Validate(installationDirectory);

        try
        {
            FileInfo file = new(package.PackagePath);
            if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException(
                    "The Wartales game package is a reparse point.");
            }

            using FileStream stream = new(
                package.PackagePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            if (stream.Length <= ExpectedSignature.Length)
                throw new IOException("The Wartales game package is empty.");

            Span<byte> signature = stackalloc byte[4];
            if (stream.Read(signature) != signature.Length
                || !signature.SequenceEqual(ExpectedSignature))
            {
                throw new IOException(
                    "The Wartales game package signature is invalid.");
            }
        }
        catch (Exception exception)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.PackageInvalid,
                "Wartales could not be updated. Close the game and check that Wartales Editor can write to the installation folder.",
                exception);
        }

        return package;
    }

    private List<WartalesPackageInfo> DiscoverSteamInstallations(
        IReadOnlyList<string> steamRoots)
    {
        HashSet<string> libraryRoots =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (string steamRoot in steamRoots)
        {
            AddNormalizedDirectory(libraryRoots, steamRoot);
            string metadataPath = Path.Combine(
                steamRoot,
                "steamapps",
                "libraryfolders.vdf");
            foreach (string libraryRoot in ReadLibraryRoots(metadataPath))
                AddNormalizedDirectory(libraryRoots, libraryRoot);
        }

        Dictionary<string, WartalesPackageInfo> installations =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (string libraryRoot in libraryRoots)
        {
            string manifestPath = Path.Combine(
                libraryRoot,
                "steamapps",
                $"appmanifest_{WartalesAppId}.acf");
            if (!TryReadText(manifestPath, out string manifest))
                continue;

            Dictionary<string, string> values = ReadQuotedPairs(manifest);
            if (!values.TryGetValue("appid", out string? appId)
                || !string.Equals(appId, WartalesAppId, StringComparison.Ordinal)
                || !values.TryGetValue("installdir", out string? installName)
                || string.IsNullOrWhiteSpace(installName))
            {
                continue;
            }

            if (!TryComposeManifestInstallationDirectory(
                    libraryRoot,
                    installName,
                    out string? candidate))
            {
                continue;
            }

            if (TryValidate(candidate, out WartalesPackageInfo? installation))
            {
                installations[installation.InstallationDirectory] =
                    installation;
            }
        }

        return installations.Values.ToList();
    }

    internal static bool TryComposeManifestInstallationDirectory(
        string libraryRoot,
        string installDirectoryName,
        [NotNullWhen(true)] out string? installationDirectory)
    {
        installationDirectory = null;

        if (string.IsNullOrWhiteSpace(installDirectoryName)
            || Path.IsPathRooted(installDirectoryName))
        {
            return false;
        }

        string[] segments =
            installDirectoryName.Split(
                new[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.None);
        char[] invalidFileNameCharacters =
            Path.GetInvalidFileNameChars();

        if (segments.Length == 0
            || segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment)
                || segment == "."
                || segment == ".."
                || segment.IndexOfAny(
                    invalidFileNameCharacters) >= 0))
        {
            return false;
        }

        try
        {
            string commonRoot = NormalizeDirectory(
                Path.Combine(
                    libraryRoot,
                    "steamapps",
                    "common"));
            string candidate = NormalizeDirectory(
                Path.Combine(
                    commonRoot,
                    installDirectoryName));
            string containmentPrefix =
                commonRoot + Path.DirectorySeparatorChar;

            if (!candidate.StartsWith(
                    containmentPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            installationDirectory = candidate;
            return true;
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
            return false;
        }
    }

    private static IReadOnlyList<string> ReadLibraryRoots(string metadataPath)
    {
        if (!TryReadText(metadataPath, out string contents))
            return Array.Empty<string>();

        return QuotedPairPattern.Matches(contents)
            .Cast<Match>()
            .Where(match => string.Equals(
                match.Groups["key"].Value,
                "path",
                StringComparison.OrdinalIgnoreCase))
            .Select(match => match.Groups["value"].Value.Replace("\\\\", "\\"))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }

    private static Dictionary<string, string> ReadQuotedPairs(string contents)
    {
        Dictionary<string, string> result =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in QuotedPairPattern.Matches(contents))
        {
            result[match.Groups["key"].Value] =
                match.Groups["value"].Value;
        }

        return result;
    }

    private string? ReadSavedInstallationDirectory()
    {
        try
        {
            if (!File.Exists(locationFilePath))
                return null;

            LocationDocument? document =
                JsonSerializer.Deserialize<LocationDocument>(
                    File.ReadAllText(locationFilePath));
            return document?.Version == LocationSchemaVersion
                   && !string.IsNullOrWhiteSpace(document.InstallationDirectory)
                ? document.InstallationDirectory
                : null;
        }
        catch (Exception exception)
            when (IsFilesystemException(exception) || exception is JsonException)
        {
            return null;
        }
    }

    private void PersistInstallationDirectory(string installationDirectory)
    {
        string? parent = Path.GetDirectoryName(locationFilePath);
        if (string.IsNullOrWhiteSpace(parent))
            throw new IOException("The Wartales installation location could not be saved.");

        string candidatePath = Path.Combine(
            parent,
            $".{Path.GetFileName(locationFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(parent);
            string json = JsonSerializer.Serialize(
                new LocationDocument(
                    LocationSchemaVersion,
                    installationDirectory),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(candidatePath, json);

            LocationDocument? verified =
                JsonSerializer.Deserialize<LocationDocument>(
                    File.ReadAllText(candidatePath));
            if (verified?.Version != LocationSchemaVersion
                || !string.Equals(
                    verified.InstallationDirectory,
                    installationDirectory,
                    StringComparison.Ordinal))
            {
                throw new IOException(
                    "The Wartales installation location could not be verified before saving.");
            }

            File.Move(candidatePath, locationFilePath, overwrite: true);
        }
        catch (Exception exception)
            when (IsFilesystemException(exception) || exception is JsonException)
        {
            throw new IOException(
                "The Wartales installation was found, but its location could not be saved.",
                exception);
        }
        finally
        {
            try
            {
                if (File.Exists(candidatePath))
                    File.Delete(candidatePath);
            }
            catch
            {
                // Best-effort cleanup of an unpublished candidate.
            }
        }
    }

    private List<string> GetSteamRootsSafely()
    {
        IReadOnlyList<string> suppliedRoots;
        try
        {
            suppliedRoots = steamRootsProvider();
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
            return new List<string>();
        }

        HashSet<string> roots =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (string path in suppliedRoots)
            AddNormalizedDirectory(roots, path);
        return roots.ToList();
    }

    private static IReadOnlyList<string> DiscoverSteamRoots()
    {
        HashSet<string> roots = new(StringComparer.OrdinalIgnoreCase);
        ReadSteamRegistryRoot(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            @"Software\Valve\Steam",
            roots);
        ReadSteamRegistryRoot(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"Software\Valve\Steam",
            roots);
        ReadSteamRegistryRoot(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            @"Software\Valve\Steam",
            roots);
        return roots.ToArray();
    }

    private static void ReadSteamRegistryRoot(
        RegistryHive hive,
        RegistryView view,
        string subKeyName,
        ISet<string> roots)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            using RegistryKey? key = baseKey.OpenSubKey(subKeyName);
            string? path = key?.GetValue("InstallPath") as string
                           ?? key?.GetValue("SteamPath") as string;
            AddNormalizedDirectory(roots, path);
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
            // One inaccessible registry view does not block other discovery.
        }
    }

    private static string? GetSuggestedDirectory(
        string? savedDirectory,
        IReadOnlyList<WartalesPackageInfo> discovered,
        IReadOnlyList<string> steamRoots,
        string? legacyDirectory)
    {
        if (discovered.Count > 0)
            return discovered[0].InstallationDirectory;

        foreach (string? candidate in new[] { savedDirectory, legacyDirectory })
        {
            string? existing = FindExistingDirectory(candidate);
            if (existing != null)
                return existing;
        }

        foreach (string steamRoot in steamRoots)
        {
            string common = Path.Combine(steamRoot, "steamapps", "common");
            if (Directory.Exists(common))
                return common;
        }

        return null;
    }

    private static string? FindExistingDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            DirectoryInfo? current = new(NormalizeDirectory(path));
            while (current != null)
            {
                if (current.Exists)
                    return current.FullName;
                current = current.Parent;
            }
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
        }

        return null;
    }

    private bool TryValidate(
        string? installationDirectory,
        [NotNullWhen(true)] out WartalesPackageInfo? installation)
    {
        installation = null;
        if (string.IsNullOrWhiteSpace(installationDirectory))
            return false;

        try
        {
            installation = Validate(installationDirectory);
            return true;
        }
        catch (QuickBmsImportException)
        {
            return false;
        }
    }

    private static bool TryReadText(string path, out string contents)
    {
        contents = string.Empty;
        try
        {
            if (!File.Exists(path))
                return false;

            contents = File.ReadAllText(path);
            return true;
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
            return false;
        }
    }

    private static bool IsRegularFile(string path)
    {
        try
        {
            return File.Exists(path) && IsRegularFile(new FileInfo(path));
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
            return false;
        }
    }

    private static bool IsRegularFile(FileInfo file)
    {
        FileAttributes attributes = file.Attributes;
        return (attributes &
                (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0;
    }

    private static void AddNormalizedDirectory(
        ISet<string> paths,
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            paths.Add(NormalizeDirectory(path));
        }
        catch (Exception exception)
            when (IsFilesystemException(exception))
        {
        }
    }

    private static string NormalizeDirectory(string path) =>
        Path.GetFullPath(path)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

    private static bool IsFilesystemException(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException
            or System.Security.SecurityException;

    private sealed record LocationDocument(
        int Version,
        string InstallationDirectory);
}
