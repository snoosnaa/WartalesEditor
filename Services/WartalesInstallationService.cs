using System.IO;

namespace WartalesEditor.Services;

public sealed record WartalesPackageInfo(
    string InstallationDirectory,
    string PackagePath,
    long PackageSize);

public sealed class WartalesInstallationService
{
    private static readonly byte[] ExpectedSignature =
    {
        (byte)'P', (byte)'A', (byte)'K', 0
    };

    public WartalesPackageInfo Validate(
        string installationDirectory)
    {
        if (string.IsNullOrWhiteSpace(installationDirectory)
            ||
            !Directory.Exists(installationDirectory))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.WartalesInstallationInvalid,
                "The Wartales installation could not be found. Check the game installation and try again.");
        }

        string fullDirectory =
            Path.GetFullPath(installationDirectory);
        string packagePath =
            Path.Combine(fullDirectory, "res.pak");

        if (!File.Exists(packagePath))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.PackageMissing,
                "The Wartales game package could not be found. Verify the game files and try again.");
        }

        FileInfo package = new(packagePath);

        if ((package.Attributes & FileAttributes.Directory) != 0
            ||
            package.Length <= ExpectedSignature.Length)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.PackageInvalid,
                "The Wartales game package is empty or invalid. Verify the game files and try again.");
        }

        try
        {
            using FileStream stream =
                new(
                    package.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

            Span<byte> signature = stackalloc byte[4];

            if (stream.Read(signature) != signature.Length
                ||
                !signature.SequenceEqual(ExpectedSignature))
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
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.PackageInvalid,
                "The Wartales game package could not be read. Close any program using it and try again.",
                exception);
        }

        return new WartalesPackageInfo(
            fullDirectory,
            package.FullName,
            package.Length);
    }
}
