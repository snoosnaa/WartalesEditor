using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class LanguageDataService
{
    private const string ApplicationFolderName =
        "Wartales Editor";

    private const string LanguageDataFolderName =
        "Language Data";

    private const string CanonicalFileName =
        "export.xml";

    private readonly LocalizationService localizationService;

    private readonly string? canonicalDirectoryOverride;

    private readonly ILanguageDataOperationHooks operationHooks;

    public LanguageDataService(
        LocalizationService localizationService)
        : this(
            localizationService,
            canonicalDirectory: null,
            NoOpLanguageDataOperationHooks.Instance)
    {
    }

    internal LanguageDataService(
        LocalizationService localizationService,
        string? canonicalDirectory,
        ILanguageDataOperationHooks operationHooks)
    {
        this.localizationService =
            localizationService
            ?? throw new ArgumentNullException(
                nameof(localizationService));

        this.operationHooks =
            operationHooks
            ?? throw new ArgumentNullException(
                nameof(operationHooks));

        if (canonicalDirectory != null)
        {
            if (string.IsNullOrWhiteSpace(
                    canonicalDirectory))
            {
                throw new ArgumentException(
                    "A language-data directory is required.",
                    nameof(canonicalDirectory));
            }

            canonicalDirectoryOverride =
                Path.GetFullPath(
                    canonicalDirectory);
        }
    }

    public LanguageDataService(
        LocalizationService localizationService,
        string canonicalDirectory)
        : this(
            localizationService,
            canonicalDirectory,
            NoOpLanguageDataOperationHooks.Instance)
    {
    }

    public LanguageDataState CurrentState { get; private set; } =
        LanguageDataState.Unavailable();

    public string GetCanonicalDirectory()
    {
        if (canonicalDirectoryOverride != null)
        {
            return canonicalDirectoryOverride;
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
            LanguageDataFolderName);
    }

    public string GetCanonicalPath() =>
        Path.Combine(
            GetCanonicalDirectory(),
            CanonicalFileName);

    public IReadOnlyList<string> DiscoverValidSources(
        string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            directory);

        string fullDirectory =
            Path.GetFullPath(directory);

        if (!Directory.Exists(fullDirectory))
        {
            return Array.Empty<string>();
        }

        List<string> validSources =
            new();

        IEnumerable<string> candidates =
            Directory.EnumerateFiles(
                    fullDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(path =>
                    Path.GetFileName(path)
                        .StartsWith(
                            "export_",
                            StringComparison.OrdinalIgnoreCase)
                    &&
                    string.Equals(
                        Path.GetExtension(path),
                        ".xml",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    path => path,
                    StringComparer.OrdinalIgnoreCase);

        foreach (string candidate in candidates)
        {
            try
            {
                _ = PrepareFile(candidate);
                validSources.Add(candidate);
            }
            catch (Exception exception)
                when (IsExpectedLanguageDataFailure(
                    exception))
            {
                // Invalid candidates are ignored. Manual selection remains
                // available and applies the same validation during install.
            }
        }

        return validSources;
    }

    public LanguageDataState LoadCanonical()
    {
        try
        {
            string canonicalPath =
                GetCanonicalPath();

            if (!File.Exists(canonicalPath))
            {
                localizationService.Clear();
                CurrentState =
                    LanguageDataState.Unavailable();

                return CurrentState;
            }

            PreparedLanguageData prepared =
                PrepareFile(canonicalPath);

            localizationService.Apply(
                prepared.Localization);

            CurrentState =
                LanguageDataState.Available(
                    prepared.Metadata,
                    prepared.Localization.Names.Count);
        }
        catch (Exception exception)
            when (IsExpectedLanguageDataFailure(
                exception))
        {
            localizationService.Clear();
            CurrentState =
                LanguageDataState.Invalid(
                    "Stored language data could not be used.");
        }

        return CurrentState;
    }

    public LanguageDataState Install(
        string sourceFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sourceFile);

        string fullSourcePath =
            Path.GetFullPath(sourceFile);

        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException(
                "The selected language-data file could not be found.",
                fullSourcePath);
        }

        _ = PrepareFile(fullSourcePath);

        string canonicalDirectory =
            GetCanonicalDirectory();
        string canonicalPath =
            GetCanonicalPath();
        string candidatePath =
            canonicalPath + ".tmp";
        string rollbackPath =
            canonicalPath + ".rollback.tmp";

        Directory.CreateDirectory(
            canonicalDirectory);

        DeleteTemporary(candidatePath);
        DeleteTemporary(rollbackPath);

        bool previousCanonicalExists =
            File.Exists(canonicalPath);
        string? previousCanonicalFingerprint =
            previousCanonicalExists
                ? ComputeFingerprint(canonicalPath)
                : null;
        bool canonicalPromoted = false;

        try
        {
            File.Copy(
                fullSourcePath,
                candidatePath,
                overwrite: false);

            PreparedLanguageData storedCandidate =
                PrepareFile(candidatePath);

            if (previousCanonicalExists)
            {
                File.Replace(
                    candidatePath,
                    canonicalPath,
                    rollbackPath,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(
                    candidatePath,
                    canonicalPath);
            }

            canonicalPromoted = true;

            try
            {
                operationHooks.AfterCanonicalPromotion(
                    canonicalPath,
                    rollbackPath);

                localizationService.Apply(
                    storedCandidate.Localization);

                CurrentState =
                    LanguageDataState.Available(
                        storedCandidate.Metadata,
                        storedCandidate.Localization.Names.Count);
            }
            catch (Exception publicationException)
            {
                try
                {
                    RestoreAfterPublicationFailure(
                        canonicalPath,
                        rollbackPath,
                        previousCanonicalExists,
                        previousCanonicalFingerprint);
                }
                catch (Exception recoveryException)
                {
                    localizationService.Clear();
                    CurrentState =
                        LanguageDataState.Invalid(
                            "Language-data replacement could not be recovered safely.");

                    throw new LanguageDataInstallException(
                        LanguageDataInstallFailureKind.RecoveryFailed,
                        "Language-data publication failed and the previous setup could not be restored.",
                        new AggregateException(
                            publicationException,
                            recoveryException));
                }

                throw new LanguageDataInstallException(
                    previousCanonicalExists
                        ? LanguageDataInstallFailureKind.PreviousSetupRestored
                        : LanguageDataInstallFailureKind.InitialSetupReverted,
                    previousCanonicalExists
                        ? "Language data could not be replaced. The previous setup was restored."
                        : "Language data could not be set up. The incomplete setup was removed.",
                    publicationException);
            }

            try
            {
                operationHooks.BeforeSuccessfulCleanup(
                    candidatePath,
                    rollbackPath);

                DeleteTemporary(candidatePath);
                DeleteTemporary(rollbackPath);
            }
            catch (Exception cleanupException)
            {
                throw new LanguageDataInstallException(
                    LanguageDataInstallFailureKind.CleanupFailed,
                    "Language data is active, but temporary recovery data could not be removed.",
                    cleanupException);
            }

            return CurrentState;
        }
        catch
        {
            if (!canonicalPromoted)
            {
                try
                {
                    DeleteTemporary(candidatePath);
                }
                catch
                {
                    // Preserve the original pre-promotion failure. A stale
                    // candidate is rejected or removed before the next attempt.
                }
            }

            throw;
        }
    }

    private PreparedLanguageData PrepareFile(
        string fileName)
    {
        XDocument document =
            XDocument.Load(
                fileName,
                LoadOptions.None);

        XElement? root =
            document.Root;

        if (root == null ||
            root.Name != XName.Get("cdb"))
        {
            throw new InvalidDataException(
                "The selected file is not a Wartales export language-data file.");
        }

        string project =
            root.Attribute("project")?.Value.Trim()
            ?? string.Empty;

        if (!string.Equals(
                project,
                "Wartales",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "The selected language data is not for Wartales.");
        }

        string languageCode =
            root.Attribute("lang")?.Value.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
                languageCode))
        {
            throw new InvalidDataException(
                "The selected language data does not identify its language.");
        }

        if (!root.Elements("sheet").Any())
        {
            throw new InvalidDataException(
                "The selected language data does not contain any localization sheets.");
        }

        LocalizationPreparation localization =
            localizationService.Prepare(document);

        if (localization.Names.Count == 0)
        {
            throw new InvalidDataException(
                "The selected language data does not contain any usable localized names.");
        }

        LanguageDataMetadata metadata =
            new(
                project,
                languageCode,
                ReadAttribute(root, "version"),
                ReadAttribute(root, "revision"),
                ReadAttribute(root, "softwareVersion"),
                ReadAttribute(root, "date"));

        return new PreparedLanguageData(
            localization,
            metadata);
    }

    private void RestoreAfterPublicationFailure(
        string canonicalPath,
        string rollbackPath,
        bool previousCanonicalExists,
        string? previousCanonicalFingerprint)
    {
        if (previousCanonicalExists)
        {
            if (string.IsNullOrWhiteSpace(
                    previousCanonicalFingerprint) ||
                !File.Exists(rollbackPath))
            {
                throw new InvalidDataException(
                    "The previous canonical language data is not available for recovery.");
            }

            _ = PrepareFile(rollbackPath);

            string rollbackFingerprint =
                ComputeFingerprint(rollbackPath);

            if (!string.Equals(
                    rollbackFingerprint,
                    previousCanonicalFingerprint,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The language-data recovery file does not match the previous canonical file.");
            }

            File.Replace(
                rollbackPath,
                canonicalPath,
                destinationBackupFileName: null,
                ignoreMetadataErrors: true);

            string restoredFingerprint =
                ComputeFingerprint(canonicalPath);

            if (!string.Equals(
                    restoredFingerprint,
                    previousCanonicalFingerprint,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The previous canonical language data could not be restored exactly.");
            }

            PreparedLanguageData restored =
                PrepareFile(canonicalPath);

            localizationService.Apply(
                restored.Localization);

            CurrentState =
                LanguageDataState.Available(
                    restored.Metadata,
                    restored.Localization.Names.Count);

            return;
        }

        if (File.Exists(canonicalPath))
        {
            File.Delete(canonicalPath);
        }

        if (File.Exists(canonicalPath))
        {
            throw new IOException(
                "The incomplete canonical language-data file could not be removed.");
        }

        localizationService.Clear();
        CurrentState =
            LanguageDataState.Unavailable();
    }

    private static string ReadAttribute(
        XElement root,
        string name) =>
        root.Attribute(name)?.Value.Trim()
        ?? string.Empty;

    private static bool IsExpectedLanguageDataFailure(
        Exception exception) =>
        exception is IOException
        or UnauthorizedAccessException
        or XmlException
        or InvalidDataException
        or InvalidOperationException
        or ArgumentException
        or NotSupportedException;

    private static string ComputeFingerprint(
        string path)
    {
        using FileStream stream =
            File.OpenRead(path);

        return Convert.ToHexString(
            SHA256.HashData(stream));
    }

    private static void DeleteTemporary(
        string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        File.Delete(path);

        if (File.Exists(path))
        {
            throw new IOException(
                $"Temporary language-data file could not be removed: {path}");
        }
    }

    private sealed class PreparedLanguageData
    {
        public PreparedLanguageData(
            LocalizationPreparation localization,
            LanguageDataMetadata metadata)
        {
            Localization = localization;
            Metadata = metadata;
        }

        public LocalizationPreparation Localization { get; }

        public LanguageDataMetadata Metadata { get; }
    }
}

internal enum LanguageDataInstallFailureKind
{
    InitialSetupReverted,
    PreviousSetupRestored,
    RecoveryFailed,
    CleanupFailed
}

internal sealed class LanguageDataInstallException :
    Exception
{
    public LanguageDataInstallException(
        LanguageDataInstallFailureKind failureKind,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public LanguageDataInstallFailureKind FailureKind { get; }
}

internal interface ILanguageDataOperationHooks
{
    void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath);

    void BeforeSuccessfulCleanup(
        string candidatePath,
        string rollbackPath);
}

internal sealed class NoOpLanguageDataOperationHooks :
    ILanguageDataOperationHooks
{
    public static NoOpLanguageDataOperationHooks Instance { get; } =
        new();

    private NoOpLanguageDataOperationHooks()
    {
    }

    public void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath)
    {
    }

    public void BeforeSuccessfulCleanup(
        string candidatePath,
        string rollbackPath)
    {
    }
}
