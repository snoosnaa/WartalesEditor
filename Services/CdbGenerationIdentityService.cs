using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class CdbGenerationIdentityService
{
    private const string Prefix = "sha256:";
    private readonly FileFingerprintService fingerprintService;

    public CdbGenerationIdentityService()
        : this(new FileFingerprintService())
    {
    }

    public CdbGenerationIdentityService(
        FileFingerprintService fingerprintService)
    {
        this.fingerprintService = fingerprintService
            ?? throw new ArgumentNullException(nameof(fingerprintService));
    }

    public string Calculate(ReadOnlySpan<byte> content)
    {
        return Normalize(fingerprintService.Calculate(content).Sha256);
    }

    public string Calculate(string fileName)
    {
        return Normalize(fingerprintService.Calculate(fileName).Sha256);
    }

    public bool IsValid(string? identity)
    {
        if (identity == null ||
            identity.Length != Prefix.Length + 64 ||
            !identity.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return identity.AsSpan(Prefix.Length).ToString().All(
            character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    public bool AreEqual(string? left, string? right)
    {
        return IsValid(left) &&
               IsValid(right) &&
               string.Equals(left, right, StringComparison.Ordinal);
    }

    public string Normalize(string sha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        string value = sha256.Trim().ToLowerInvariant();

        if (value.StartsWith(Prefix, StringComparison.Ordinal))
            value = value[Prefix.Length..];

        if (value.Length != 64 ||
            !value.All(character =>
                character is >= '0' and <= '9' or >= 'a' and <= 'f'))
        {
            throw new ArgumentException(
                "The SHA-256 identity is not valid.",
                nameof(sha256));
        }

        return Prefix + value;
    }
}
