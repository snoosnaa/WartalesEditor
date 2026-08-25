using System.IO;
using System.Security.Cryptography;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class FileFingerprintService
{
    public FileFingerprint Calculate(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FileInfo file = new(Path.GetFullPath(filePath));

        using FileStream stream =
            new(
                file.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

        string hash =
            Convert.ToHexString(
                SHA256.HashData(stream));

        return new FileFingerprint(
            file.Length,
            hash);
    }
}
