using System.IO;

namespace WartalesEditor.Services;

public sealed record QuickBmsToolchainInfo(
    string ExecutablePath,
    string ScriptPath);

public sealed class QuickBmsToolchainService
{
    public QuickBmsToolchainInfo Validate(
        string executablePath,
        string scriptPath)
    {
        string executable =
            ValidateReadableFile(
                executablePath,
                QuickBmsImportFailureKind.QuickBmsExecutableMissing,
                "QuickBMS could not be found. Place quickbms.exe in the configured QuickBMS folder and try again. For detailed QuickBMS setup instructions, see the User Guide.");

        string script =
            ValidateReadableFile(
                scriptPath,
                QuickBmsImportFailureKind.ShiroScriptMissing,
                "The Shiro Games PAK script could not be found. Place the script in the configured QuickBMS folder and try again.");

        return new QuickBmsToolchainInfo(
            executable,
            script);
    }

    private static string ValidateReadableFile(
        string filePath,
        QuickBmsImportFailureKind missingKind,
        string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(filePath)
            ||
            !File.Exists(filePath))
        {
            throw new QuickBmsImportException(
                missingKind,
                missingMessage);
        }

        string fullPath = Path.GetFullPath(filePath);

        try
        {
            FileInfo file = new(fullPath);

            if ((file.Attributes & FileAttributes.Directory) != 0
                ||
                file.Length == 0)
            {
                throw new IOException("The file is empty or is not a regular file.");
            }

            using FileStream stream =
                new(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

            _ = stream.ReadByte();
        }
        catch (Exception exception)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ToolchainInvalid,
                "The QuickBMS toolchain could not be read. Check its files and try again.",
                exception);
        }

        return fullPath;
    }
}
