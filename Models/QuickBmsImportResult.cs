namespace WartalesEditor.Models;

public sealed class QuickBmsImportResult
{
    public required ProjectModel Project { get; init; }

    public required string WartalesInstallationDirectory { get; init; }

    public required string SourcePackagePath { get; init; }

    public required FileFingerprint SourcePackageFingerprint { get; init; }

    public required string QuickBmsExecutablePath { get; init; }

    public required FileFingerprint QuickBmsExecutableFingerprint { get; init; }

    public required string ShiroScriptPath { get; init; }

    public required FileFingerprint ShiroScriptFingerprint { get; init; }

    public required string StagingDirectory { get; init; }

    public required string ExtractedCdbPath { get; init; }

    public required FileFingerprint ExtractedCdbFingerprint { get; init; }

    public required DateTimeOffset ExtractionStartedUtc { get; init; }

    public required string SessionId { get; init; }

    public required int ProcessExitCode { get; init; }

    public uint ContainedProcessCount { get; init; }

    public bool StagingCleaned { get; init; }
}

public sealed record FileFingerprint(
    long Size,
    string Sha256);
