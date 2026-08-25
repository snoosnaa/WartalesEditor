using System.IO;

namespace WartalesEditor.Services;

public sealed record ExtractionWorkspace(
    string SessionId,
    string RootDirectory,
    string DirectoryPath);

public sealed class ExtractionWorkspaceService
{
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
}
