using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class GoldenCdbService
{
    private const string ApplicationFolderName = "Wartales Editor";
    private const string GoldenFolderName = "Golden CDB";
    private const string CanonicalFileName = "data.cdb";

    private readonly JsonDataService jsonDataService;
    private readonly CdbGenerationIdentityService identityService;
    private readonly string? canonicalDirectoryOverride;
    private readonly IGoldenCdbOperationHooks operationHooks;
    private GoldenCdbReference? cachedReference;
    private string? unrecoverableIdentity;

    public GoldenCdbService(JsonDataService jsonDataService)
        : this(jsonDataService, null, NoOpGoldenCdbOperationHooks.Instance)
    {
    }

    public GoldenCdbService(
        JsonDataService jsonDataService,
        string canonicalDirectory)
        : this(
            jsonDataService,
            canonicalDirectory,
            NoOpGoldenCdbOperationHooks.Instance)
    {
    }

    internal GoldenCdbService(
        JsonDataService jsonDataService,
        string? canonicalDirectory,
        IGoldenCdbOperationHooks operationHooks)
    {
        this.jsonDataService = jsonDataService ??
            throw new ArgumentNullException(nameof(jsonDataService));
        this.operationHooks = operationHooks ??
            throw new ArgumentNullException(nameof(operationHooks));
        identityService = new CdbGenerationIdentityService();

        if (canonicalDirectory != null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(canonicalDirectory);
            canonicalDirectoryOverride = Path.GetFullPath(canonicalDirectory);
        }
    }

    public string GetCanonicalDirectory()
    {
        if (canonicalDirectoryOverride != null)
            return canonicalDirectoryOverride;

        string documents = Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documents))
        {
            throw new InvalidOperationException(
                "The user's Documents folder could not be located.");
        }

        return Path.Combine(
            documents,
            ApplicationFolderName,
            GoldenFolderName);
    }

    public string GetCanonicalPath() =>
        Path.Combine(GetCanonicalDirectory(), CanonicalFileName);

    public bool IsCanonicalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            return string.Equals(
                Path.GetFullPath(path),
                Path.GetFullPath(GetCanonicalPath()),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    public GoldenCdbState GetState()
    {
        string path;
        try
        {
            path = GetCanonicalPath();
            if (!File.Exists(path))
            {
                cachedReference = null;
                unrecoverableIdentity = null;
                return GoldenCdbState.NotSet(path);
            }

            string actualIdentity = identityService.Calculate(path);
            if (unrecoverableIdentity != null)
            {
                if (identityService.AreEqual(
                        actualIdentity,
                        unrecoverableIdentity))
                {
                    return GoldenCdbState.Invalid(
                        path,
                        "Golden CDB replacement could not be recovered safely. Replace or remove the stored reference.");
                }

                unrecoverableIdentity = null;
            }

            GoldenCdbReference reference = LoadReference();
            return GoldenCdbState.Available(
                path,
                reference.Identity,
                GetCleanupWarning(path));
        }
        catch (InvalidDataException)
        {
            cachedReference = null;
            path = SafeCanonicalPath();
            return GoldenCdbState.Invalid(
                path,
                "The stored Golden CDB could not be used.");
        }
        catch (JsonException)
        {
            cachedReference = null;
            path = SafeCanonicalPath();
            return GoldenCdbState.Invalid(
                path,
                "The stored Golden CDB could not be used.");
        }
        catch (UnauthorizedAccessException)
        {
            cachedReference = null;
            path = SafeCanonicalPath();
            return GoldenCdbState.Inaccessible(
                path,
                "The stored Golden CDB could not be accessed.");
        }
        catch (IOException)
        {
            cachedReference = null;
            path = SafeCanonicalPath();
            return GoldenCdbState.Inaccessible(
                path,
                "The stored Golden CDB could not be accessed.");
        }
        catch
        {
            cachedReference = null;
            path = SafeCanonicalPath();
            return GoldenCdbState.Invalid(
                path,
                "The stored Golden CDB could not be used.");
        }
    }

    public GoldenCdbReference LoadReference()
    {
        string canonicalPath = GetCanonicalPath();
        if (!File.Exists(canonicalPath))
        {
            cachedReference = null;
            throw new FileNotFoundException(
                "Golden CDB is not set.",
                canonicalPath);
        }

        string actualIdentity = identityService.Calculate(canonicalPath);
        if (unrecoverableIdentity != null &&
            identityService.AreEqual(
                actualIdentity,
                unrecoverableIdentity))
        {
            throw new InvalidDataException(
                "Golden CDB replacement could not be recovered safely.");
        }

        if (unrecoverableIdentity != null)
            unrecoverableIdentity = null;
        if (cachedReference != null &&
            identityService.AreEqual(cachedReference.Identity, actualIdentity))
        {
            return cachedReference;
        }

        cachedReference = null;
        unrecoverableIdentity = null;
        ReferenceProjectLoadResult loaded =
            jsonDataService.LoadReferenceProjectWithBytes(canonicalPath);
        GoldenCdbReference reference = new(
            loaded.Project,
            loaded.ContentIdentity,
            loaded.ExactBytes.LongLength,
            canonicalPath);
        cachedReference = reference;
        return reference;
    }

    public GoldenCdbState SetFromProject(ProjectModel project)
    {
        ReferenceProjectLoadResult source =
            ValidateProjectSourceCore(project);
        return SetValidatedSource(source);
    }

    public void ValidateProjectSource(ProjectModel project)
    {
        _ = ValidateProjectSourceCore(project);
    }

    private ReferenceProjectLoadResult ValidateProjectSourceCore(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.IsNullOrWhiteSpace(project.FileName))
        {
            throw new InvalidOperationException(
                "Save the current project before setting it as Golden, or select another CDB.");
        }

        string sourcePath = Path.GetFullPath(project.FileName);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                "The current project's saved CDB could not be found.",
                sourcePath);
        }

        ReferenceProjectLoadResult source =
            jsonDataService.LoadReferenceProjectWithBytes(sourcePath);
        if (!identityService.AreEqual(
                source.ContentIdentity,
                project.CurrentCdbContentIdentity))
        {
            throw new InvalidOperationException(
                "The saved CDB changed outside the editor. Reopen it or save to a new file before setting Golden.");
        }

        if (!JToken.DeepEquals(
                project.RootDocument,
                source.Project.RootDocument))
        {
            throw new InvalidOperationException(
                "Save the current project before setting it as Golden, or select another CDB.");
        }

        return source;
    }

    public void ValidateSourceFile(string sourceFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);
        _ = jsonDataService.LoadReferenceProjectWithBytes(
            Path.GetFullPath(sourceFile));
    }

    public GoldenCdbState SetFromFile(string sourceFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);
        string sourcePath = Path.GetFullPath(sourceFile);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                "The selected CDB could not be found.",
                sourcePath);
        }

        ReferenceProjectLoadResult source =
            jsonDataService.LoadReferenceProjectWithBytes(sourcePath);
        return SetValidatedSource(source);
    }

    private GoldenCdbState SetValidatedSource(
        ReferenceProjectLoadResult source)
    {
        string canonicalPath = GetCanonicalPath();
        PrepareForTransaction(canonicalPath);

        if (File.Exists(canonicalPath))
        {
            string currentIdentity = identityService.Calculate(canonicalPath);
            if (identityService.AreEqual(
                    currentIdentity,
                    source.ContentIdentity))
            {
                unrecoverableIdentity = null;
                cachedReference = null;
                GoldenCdbReference reference = LoadReference();
                return GoldenCdbState.Available(
                    canonicalPath,
                    reference.Identity);
            }
        }

        string? cleanupWarning = Publish(source, canonicalPath);
        GoldenCdbReference published = LoadReference();
        return GoldenCdbState.Available(
            canonicalPath,
            published.Identity,
            cleanupWarning);
    }

    public GoldenCdbState Remove()
    {
        string canonicalPath = GetCanonicalPath();
        cachedReference = null;
        unrecoverableIdentity = null;
        if (File.Exists(canonicalPath))
            File.Delete(canonicalPath);

        DeleteRecognizedTemporary(canonicalPath + ".candidate.tmp");
        DeleteRecognizedTemporary(canonicalPath + ".rollback.tmp");
        return GoldenCdbState.NotSet(canonicalPath);
    }

    public void InvalidateCache() => cachedReference = null;

    public ProjectModel LoadDetachedProject()
    {
        return jsonDataService.LoadReferenceProject(GetCanonicalPath());
    }

    public GoldenCdbState ReconcileAfterCanonicalWrite()
    {
        cachedReference = null;
        return GetState();
    }

    internal GoldenCdbReference? CachedReference => cachedReference;

    private string? Publish(
        ReferenceProjectLoadResult source,
        string canonicalPath)
    {
        string directory = Path.GetDirectoryName(canonicalPath) ??
            throw new InvalidOperationException(
                "The Golden CDB directory could not be resolved.");
        string candidatePath = canonicalPath + ".candidate.tmp";
        string rollbackPath = canonicalPath + ".rollback.tmp";
        Directory.CreateDirectory(directory);

        bool previousExists = File.Exists(canonicalPath);
        string? previousIdentity = previousExists
            ? identityService.Calculate(canonicalPath)
            : null;
        GoldenCdbReference? previousCache = cachedReference;
        bool promoted = false;
        string? cleanupWarning = null;

        try
        {
            File.WriteAllBytes(candidatePath, source.ExactBytes);
            ReferenceProjectLoadResult candidate =
                jsonDataService.LoadReferenceProjectWithBytes(candidatePath);
            VerifyEquivalent(source, candidate, "staged Golden CDB");
            operationHooks.AfterCandidateValidated(candidatePath);

            if (previousExists)
            {
                File.Replace(
                    candidatePath,
                    canonicalPath,
                    rollbackPath,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(candidatePath, canonicalPath);
            }

            promoted = true;
            operationHooks.AfterCanonicalPromotion(
                canonicalPath,
                rollbackPath);

            ReferenceProjectLoadResult canonical =
                jsonDataService.LoadReferenceProjectWithBytes(canonicalPath);
            VerifyEquivalent(source, canonical, "stored Golden CDB");
            cachedReference = new GoldenCdbReference(
                canonical.Project,
                canonical.ContentIdentity,
                canonical.ExactBytes.LongLength,
                canonicalPath);
            unrecoverableIdentity = null;
        }
        catch (Exception publicationException)
        {
            if (promoted)
            {
                try
                {
                    if (previousExists)
                    {
                        RestorePrevious(
                            canonicalPath,
                            rollbackPath,
                            previousIdentity!);
                        cachedReference = previousCache;
                        if (cachedReference == null)
                            _ = LoadReference();
                    }
                    else
                    {
                        if (File.Exists(canonicalPath))
                            File.Delete(canonicalPath);
                        cachedReference = null;
                    }
                }
                catch (Exception recoveryException)
                {
                    cachedReference = null;
                    unrecoverableIdentity = File.Exists(canonicalPath)
                        ? identityService.Calculate(canonicalPath)
                        : null;
                    throw new GoldenCdbPublicationException(
                        "Golden CDB replacement failed and the previous Golden CDB could not be restored safely.",
                        new AggregateException(
                            publicationException,
                            recoveryException));
                }
            }
            else
            {
                cachedReference = previousCache;
            }

            throw new GoldenCdbPublicationException(
                previousExists
                    ? "Golden CDB could not be replaced. The previous Golden CDB was preserved."
                    : "Golden CDB could not be set. No Golden CDB was created.",
                publicationException);
        }
        finally
        {
            bool candidateCleaned =
                TryDeleteRecognizedTemporary(candidatePath);
            bool rollbackCleaned =
                TryDeleteRecognizedTemporary(rollbackPath);
            if (!candidateCleaned || !rollbackCleaned)
            {
                cleanupWarning =
                    "Golden CDB was stored and remains usable, but temporary recovery files could not be removed. Try Set or Replace again to clean them up.";
            }
        }

        return cleanupWarning;
    }

    private void RestorePrevious(
        string canonicalPath,
        string rollbackPath,
        string previousIdentity)
    {
        if (!File.Exists(rollbackPath) ||
            !identityService.AreEqual(
                identityService.Calculate(rollbackPath),
                previousIdentity))
        {
            throw new InvalidDataException(
                "The Golden CDB recovery file does not match the previous Golden CDB.");
        }

        operationHooks.BeforeRollbackRestore(
            canonicalPath,
            rollbackPath);
        File.Replace(
            rollbackPath,
            canonicalPath,
            destinationBackupFileName: null,
            ignoreMetadataErrors: true);
        ReferenceProjectLoadResult restored =
            jsonDataService.LoadReferenceProjectWithBytes(canonicalPath);
        if (!identityService.AreEqual(
                restored.ContentIdentity,
                previousIdentity))
        {
            throw new InvalidDataException(
                "The previous Golden CDB could not be restored exactly.");
        }
    }

    private static void VerifyEquivalent(
        ReferenceProjectLoadResult expected,
        ReferenceProjectLoadResult actual,
        string description)
    {
        if (expected.ExactBytes.LongLength != actual.ExactBytes.LongLength ||
            !string.Equals(
                expected.ContentIdentity,
                actual.ContentIdentity,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"The {description} does not match the validated source.");
        }
    }

    private string SafeCanonicalPath()
    {
        try
        {
            return GetCanonicalPath();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void DeleteRecognizedTemporary(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private void PrepareForTransaction(string canonicalPath)
    {
        string directory = Path.GetDirectoryName(canonicalPath) ??
            throw new InvalidOperationException(
                "The Golden CDB directory could not be resolved.");
        Directory.CreateDirectory(directory);
        DeleteRecognizedTemporary(canonicalPath + ".candidate.tmp");
        DeleteRecognizedTemporary(canonicalPath + ".rollback.tmp");
    }

    private string? GetCleanupWarning(string canonicalPath)
    {
        return File.Exists(canonicalPath + ".candidate.tmp") ||
               File.Exists(canonicalPath + ".rollback.tmp")
            ? "Golden CDB is usable, but temporary recovery files remain. Set or Replace Golden again to clean them up."
            : null;
    }

    private bool TryDeleteRecognizedTemporary(string path)
    {
        try
        {
            operationHooks.BeforeTemporaryCleanup(path);
            DeleteRecognizedTemporary(path);
            return !File.Exists(path);
        }
        catch
        {
            return false;
        }
    }
}

public sealed class GoldenCdbPublicationException : IOException
{
    public GoldenCdbPublicationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}

internal interface IGoldenCdbOperationHooks
{
    void AfterCandidateValidated(string candidatePath);

    void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath);

    void BeforeRollbackRestore(
        string canonicalPath,
        string rollbackPath);

    void BeforeTemporaryCleanup(string temporaryPath);
}

internal sealed class NoOpGoldenCdbOperationHooks :
    IGoldenCdbOperationHooks
{
    public static NoOpGoldenCdbOperationHooks Instance { get; } = new();

    private NoOpGoldenCdbOperationHooks()
    {
    }

    public void AfterCandidateValidated(string candidatePath)
    {
    }

    public void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath)
    {
    }

    public void BeforeRollbackRestore(
        string canonicalPath,
        string rollbackPath)
    {
    }

    public void BeforeTemporaryCleanup(string temporaryPath)
    {
    }
}
