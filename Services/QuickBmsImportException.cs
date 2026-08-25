namespace WartalesEditor.Services;

public enum QuickBmsImportFailureKind
{
    WartalesInstallationInvalid,
    PackageMissing,
    PackageInvalid,
    QuickBmsExecutableMissing,
    ShiroScriptMissing,
    ToolchainInvalid,
    ProcessStartFailed,
    ProcessTimedOut,
    ProcessTerminationFailed,
    ProcessFailed,
    ExtractedCdbMissing,
    ExtractedCdbAmbiguous,
    ExtractedCdbInvalid,
    ExtractedCdbAlreadyExists,
    PromotionFailed,
    SourcePackageChanged,
    StagingFailed
}

public sealed class QuickBmsImportException : Exception
{
    public QuickBmsImportFailureKind FailureKind { get; }

    public QuickBmsImportException(
        QuickBmsImportFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }
}
