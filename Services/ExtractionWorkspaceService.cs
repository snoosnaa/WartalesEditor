using System.IO;

namespace WartalesEditor.Services;

public sealed record ExtractionWorkspace(
    string SessionId,
    string RootDirectory,
    string DirectoryPath)
{
    internal bool IsReconciledOwnedSession { get; init; }
}

public sealed class ExtractionWorkspaceService
{
    private const string OwnedSessionMarkerFileName =
        ".wartales-editor-golden-import";
    private const string OwnedSessionMarkerContent =
        "WartalesEditor GoldenImport Session v1";

    public string ValidateRoot(
        string stagingRootDirectory,
        bool createIfMissing = true)
    {
        if (string.IsNullOrWhiteSpace(stagingRootDirectory))
        {
            throw CreateStagingFailure();
        }

        try
        {
            string root = Path.GetFullPath(stagingRootDirectory);
            ValidateExistingComponents(root);

            if (createIfMissing)
            {
                Directory.CreateDirectory(root);
            }

            ValidateDirectory(root);
            return root;
        }
        catch (QuickBmsImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateStagingFailure(exception);
        }
    }

    public ExtractionWorkspace Create(
        string stagingRootDirectory)
    {
        if (string.IsNullOrWhiteSpace(stagingRootDirectory))
        {
            throw CreateStagingFailure();
        }

        try
        {
            string root =
                Path.GetFullPath(stagingRootDirectory);

            ValidateExistingComponents(root);
            Directory.CreateDirectory(root);
            ValidateDirectory(root);

            for (int attempt = 0; attempt < 5; attempt++)
            {
                string sessionId =
                    Guid.NewGuid().ToString("N");
                string directory =
                    Path.Combine(root, sessionId);

                if (Directory.Exists(directory)
                    ||
                    File.Exists(directory))
                {
                    continue;
                }

                Directory.CreateDirectory(directory);

                ExtractionWorkspace workspace =
                    new(
                        sessionId,
                        root,
                        directory);

                ValidateForUse(workspace);

                return workspace;
            }
        }
        catch (QuickBmsImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateStagingFailure(exception);
        }

        throw new QuickBmsImportException(
            QuickBmsImportFailureKind.StagingFailed,
            "A unique temporary extraction folder could not be created.");
    }

    internal ExtractionWorkspace CreateReconciledOwnedSession(
        string stagingRootDirectory)
    {
        string root = ValidateRoot(stagingRootDirectory);
        ReconcileOwnedSessions(root);

        ExtractionWorkspace createdWorkspace =
            Create(root);
        ExtractionWorkspace workspace =
            createdWorkspace with
            {
                IsReconciledOwnedSession = true
            };

        try
        {
            string markerPath = Path.Combine(
                workspace.DirectoryPath,
                OwnedSessionMarkerFileName);
            File.WriteAllText(
                markerPath,
                OwnedSessionMarkerContent);
            ValidateContainedRegularFile(
                workspace,
                markerPath);
            return workspace;
        }
        catch (Exception exception)
        {
            _ = TryClean(createdWorkspace);
            throw CreateStagingFailure(exception);
        }
    }

    public void ValidateForUse(
        ExtractionWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        try
        {
            (string root, string directory) =
                ValidateWorkspaceIdentity(workspace);

            ValidateExistingComponents(root);
            ValidateDirectory(root);
            ValidateExistingComponents(directory);
            ValidateDirectory(directory);
        }
        catch (QuickBmsImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateStagingFailure(exception);
        }
    }

    public void ValidateContainedRegularFile(
        ExtractionWorkspace workspace,
        string filePath)
    {
        ValidateForUse(workspace);

        string directory =
            Path.GetFullPath(workspace.DirectoryPath);
        string candidate =
            Path.GetFullPath(filePath);

        if (!IsContained(directory, candidate))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ExtractedCdbInvalid,
                "The extracted Wartales data file was outside the safe extraction folder.");
        }

        ValidateExistingComponents(candidate);

        FileAttributes attributes =
            File.GetAttributes(candidate);

        if ((attributes & FileAttributes.Directory) != 0
            ||
            (attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ExtractedCdbInvalid,
                "The extracted Wartales data file was not a safe regular file.");
        }
    }

    public void ValidateContainedRegularDirectory(
        ExtractionWorkspace workspace,
        string directoryPath)
    {
        ValidateForUse(workspace);

        try
        {
            string directory =
                Path.GetFullPath(workspace.DirectoryPath);
            string candidate =
                Path.GetFullPath(directoryPath);

            if (!IsContained(directory, candidate))
            {
                throw CreateStagingFailure();
            }

            ValidateExistingComponents(candidate);
            ValidateDirectory(candidate);
        }
        catch (QuickBmsImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateStagingFailure(exception);
        }
    }

    public bool TryClean(
        ExtractionWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        try
        {
            (_, string directory) =
                ValidateWorkspaceIdentity(workspace);

            ValidateForUse(workspace);

            if (ContainsReparsePoint(directory))
            {
                return false;
            }

            ValidateForUse(workspace);

            if (workspace.IsReconciledOwnedSession)
            {
                return TryCleanOwnedSession(
                    workspace,
                    directory);
            }

            Directory.Delete(
                directory,
                recursive: true);

            return !Directory.Exists(directory)
                &&
                !File.Exists(directory);
        }
        catch
        {
            return false;
        }
    }

    public void ValidateTreeContainsNoReparsePoint(
        ExtractionWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ValidateForUse(workspace);

        if (ContainsReparsePoint(workspace.DirectoryPath))
        {
            throw CreateStagingFailure();
        }
    }

    private void ReconcileOwnedSessions(
        string rootDirectory)
    {
        try
        {
            foreach (string entry in
                     Directory.EnumerateFileSystemEntries(
                         rootDirectory))
            {
                if (!Directory.Exists(entry) ||
                    File.Exists(entry))
                {
                    throw CreateOwnedReconciliationFailure();
                }

                FileAttributes attributes =
                    File.GetAttributes(entry);
                string sessionId = Path.GetFileName(entry);

                if ((attributes & FileAttributes.Directory) == 0 ||
                    (attributes & FileAttributes.ReparsePoint) != 0 ||
                    !Guid.TryParseExact(
                        sessionId,
                        "N",
                        out _))
                {
                    throw CreateOwnedReconciliationFailure();
                }

                ExtractionWorkspace workspace = new(
                    sessionId,
                    rootDirectory,
                    entry)
                {
                    IsReconciledOwnedSession = true
                };
                ValidateForUse(workspace);

                string markerPath = Path.Combine(
                    entry,
                    OwnedSessionMarkerFileName);

                if (!File.Exists(markerPath) ||
                    Directory.Exists(markerPath))
                {
                    throw CreateOwnedReconciliationFailure();
                }

                FileAttributes markerAttributes =
                    File.GetAttributes(markerPath);

                if ((markerAttributes & FileAttributes.Directory) != 0 ||
                    (markerAttributes & FileAttributes.ReparsePoint) != 0 ||
                    !string.Equals(
                        File.ReadAllText(markerPath),
                        OwnedSessionMarkerContent,
                        StringComparison.Ordinal))
                {
                    throw CreateOwnedReconciliationFailure();
                }

                ValidateTreeContainsNoReparsePoint(workspace);

                if (!TryClean(workspace))
                {
                    throw CreateOwnedReconciliationFailure();
                }
            }
        }
        catch (QuickBmsImportException exception)
            when (exception.FailureKind !=
                  QuickBmsImportFailureKind.StagingFailed ||
                  !exception.Message.Contains(
                      "retained temporary Golden acquisition folder",
                      StringComparison.Ordinal))
        {
            throw CreateOwnedReconciliationFailure(exception);
        }
        catch (Exception exception)
        {
            throw CreateOwnedReconciliationFailure(exception);
        }
    }

    private static bool TryCleanOwnedSession(
        ExtractionWorkspace workspace,
        string directory)
    {
        string markerPath = Path.Combine(
            directory,
            OwnedSessionMarkerFileName);

        if (!File.Exists(markerPath) ||
            Directory.Exists(markerPath))
        {
            return false;
        }

        FileAttributes markerAttributes =
            File.GetAttributes(markerPath);

        if ((markerAttributes & FileAttributes.Directory) != 0 ||
            (markerAttributes & FileAttributes.ReparsePoint) != 0 ||
            !string.Equals(
                File.ReadAllText(markerPath),
                OwnedSessionMarkerContent,
                StringComparison.Ordinal))
        {
            return false;
        }

        foreach (string entry in
                 Directory.EnumerateFileSystemEntries(directory))
        {
            if (string.Equals(
                    entry,
                    markerPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            FileAttributes attributes =
                File.GetAttributes(entry);

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                return false;
            }

            if ((attributes & FileAttributes.Directory) != 0)
            {
                Directory.Delete(
                    entry,
                    recursive: true);
            }
            else
            {
                File.Delete(entry);
            }
        }

        File.Delete(markerPath);
        Directory.Delete(directory);

        return !Directory.Exists(workspace.DirectoryPath) &&
               !File.Exists(workspace.DirectoryPath);
    }

    private static (
        string Root,
        string Directory)
        ValidateWorkspaceIdentity(
            ExtractionWorkspace workspace)
    {
        string root =
            Path.GetFullPath(workspace.RootDirectory)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
        string directory =
            Path.GetFullPath(workspace.DirectoryPath)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
        string expectedDirectory =
            Path.Combine(
                root,
                workspace.SessionId);

        if (!Guid.TryParseExact(
                workspace.SessionId,
                "N",
                out _)
            ||
            !string.Equals(
                directory,
                expectedDirectory,
                StringComparison.OrdinalIgnoreCase))
        {
            throw CreateStagingFailure();
        }

        return (root, directory);
    }

    private static void ValidateExistingComponents(
        string path)
    {
        string fullPath = Path.GetFullPath(path);
        string root =
            Path.GetPathRoot(fullPath)
            ?? throw new IOException(
                "The path does not have a filesystem root.");
        string current = root;
        string remainder =
            fullPath[root.Length..];

        foreach (string component in
                 remainder.Split(
                     new[]
                     {
                         Path.DirectorySeparatorChar,
                         Path.AltDirectorySeparatorChar
                     },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, component);

            if (!Directory.Exists(current)
                &&
                !File.Exists(current))
            {
                break;
            }

            FileAttributes attributes =
                File.GetAttributes(current);

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException(
                    "The extraction path contains a reparse point.");
            }
        }
    }

    private static void ValidateDirectory(
        string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                "The extraction directory does not exist.");
        }

        FileAttributes attributes =
            File.GetAttributes(path);

        if ((attributes & FileAttributes.Directory) == 0
            ||
            (attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException(
                "The extraction path is not a safe regular directory.");
        }
    }

    private static bool ContainsReparsePoint(
        string rootDirectory)
    {
        Stack<string> pending = new();
        pending.Push(rootDirectory);

        while (pending.Count > 0)
        {
            string directory = pending.Pop();

            foreach (string entry in
                     Directory.EnumerateFileSystemEntries(directory))
            {
                FileAttributes attributes =
                    File.GetAttributes(entry);

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pending.Push(entry);
                }
            }
        }

        return false;
    }

    private static bool IsContained(
        string rootDirectory,
        string candidate)
    {
        string root =
            rootDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
        string rootPrefix =
            root + Path.DirectorySeparatorChar;

        return candidate.StartsWith(
            rootPrefix,
            StringComparison.OrdinalIgnoreCase);
    }

    private static QuickBmsImportException CreateStagingFailure(
        Exception? innerException = null)
    {
        return new QuickBmsImportException(
            QuickBmsImportFailureKind.StagingFailed,
            "A safe temporary extraction folder could not be created or verified.",
            innerException);
    }

    private static QuickBmsImportException
        CreateOwnedReconciliationFailure(
            Exception? innerException = null)
    {
        return new QuickBmsImportException(
            QuickBmsImportFailureKind.StagingFailed,
            "A retained temporary Golden acquisition folder could not be recognized or removed safely. Golden refresh was stopped before a new temporary folder was created.",
            innerException);
    }
}
