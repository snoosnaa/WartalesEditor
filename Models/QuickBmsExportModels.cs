namespace WartalesEditor.Models;

public enum QuickBmsOperationKind
{
    None,
    Importing,
    ExportPreparing,
    ExportWriting,
    ExportVerifying
}

public enum QuickBmsExportOutcome
{
    CancelledBeforeWrite,
    PreflightFailed,
    QuickBmsFailed,
    ReimportNotConfirmed,
    TerminationUnproven,
    VerificationFailed,
    Success
}

public enum QuickBmsExportStage
{
    Preparing,
    Exporting,
    Verifying,
    Completed
}

public sealed class QuickBmsExportPreparation
{
    internal QuickBmsExportPreparation(
        Services.QuickBmsExportWorkspace exportWorkspace)
    {
        ExportWorkspace = exportWorkspace;
    }

    internal Services.QuickBmsExportWorkspace ExportWorkspace { get; }

    public required string SourceCdbPath { get; init; }

    public required string StagedCdbPath { get; init; }

    public required string ModdedDirectory { get; init; }

    public required string VerificationDirectory { get; init; }

    public required string PackagePath { get; init; }

    public required string QuickBmsExecutablePath { get; init; }

    public required string ShiroScriptPath { get; init; }

    public required FileFingerprint SourceFingerprint { get; init; }

    public required FileFingerprint StagedFingerprint { get; init; }

    public required TimeSpan ProcessTimeout { get; init; }
}

internal sealed class QuickBmsExportPreparationException : Exception
{
    public QuickBmsExportPreparationException(
        string message,
        bool wasCancelled,
        string preservedWorkspacePath,
        Exception innerException)
        : base(message, innerException)
    {
        WasCancelled = wasCancelled;
        PreservedWorkspacePath = preservedWorkspacePath;
    }

    public bool WasCancelled { get; }

    public bool StagingCleaned => false;

    public string PreservedWorkspacePath { get; }
}

public sealed class QuickBmsExportResult
{
    public required QuickBmsExportOutcome Outcome { get; init; }

    public string DiagnosticDetail { get; init; } = string.Empty;

    public FileFingerprint? SourceFingerprint { get; init; }

    public FileFingerprint? StagedFingerprint { get; init; }

    public FileFingerprint? VerificationFingerprint { get; init; }

    public Services.ExternalProcessResult? ProcessResult { get; init; }

    public bool StagingCleaned { get; init; }

    public string PreservedWorkspacePath { get; init; } = string.Empty;
}

public sealed record QuickBmsReimportParseResult(
    bool IsConfirmed,
    int? ReimportCount,
    IReadOnlyList<string> FileNames,
    string FailureReason);
