using System.IO;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

internal interface IQuickBmsExportService
{
    Task<QuickBmsExportPreparation> PrepareAsync(
        string sourceCdbPath,
        string expectedContentIdentity,
        QuickBmsImportOptions options,
        CancellationToken cancellationToken = default);

    Task<QuickBmsExportResult> ExportAsync(
        QuickBmsExportPreparation preparation,
        IProgress<QuickBmsExportStage>? progress = null);

    bool TryCancelPreparation(
        QuickBmsExportPreparation preparation);
}

internal sealed class QuickBmsExportTestHooks
{
    public Action? SourceSnapshotAccepted { get; init; }

    public Action? WorkspaceCreated { get; init; }
}

public sealed class QuickBmsExportService : IQuickBmsExportService
{
    private readonly WartalesInstallationService installationService;
    private readonly QuickBmsToolchainService toolchainService;
    private readonly IQuickBmsExportWorkspaceService workspaceService;
    private readonly IExternalProcessRunner processRunner;
    private readonly FileFingerprintService fingerprintService;
    private readonly CdbGenerationIdentityService identityService;
    private readonly QuickBmsReimportOutputParser outputParser;
    private readonly QuickBmsExportTestHooks? testHooks;

    public QuickBmsExportService()
        : this(
            new WartalesInstallationService(),
            new QuickBmsToolchainService(),
            new QuickBmsExportWorkspaceService(),
            new ExternalProcessRunner(),
            new FileFingerprintService(),
            new CdbGenerationIdentityService(),
            new QuickBmsReimportOutputParser())
    {
    }

    public QuickBmsExportService(
        WartalesInstallationService installationService,
        QuickBmsToolchainService toolchainService,
        QuickBmsExportWorkspaceService workspaceService,
        IExternalProcessRunner processRunner,
        FileFingerprintService fingerprintService,
        CdbGenerationIdentityService identityService,
        QuickBmsReimportOutputParser outputParser)
        : this(
            installationService,
            toolchainService,
            workspaceService,
            processRunner,
            fingerprintService,
            identityService,
            outputParser,
            null)
    {
    }

    internal QuickBmsExportService(
        WartalesInstallationService installationService,
        QuickBmsToolchainService toolchainService,
        IQuickBmsExportWorkspaceService workspaceService,
        IExternalProcessRunner processRunner,
        FileFingerprintService fingerprintService,
        CdbGenerationIdentityService identityService,
        QuickBmsReimportOutputParser outputParser,
        QuickBmsExportTestHooks? testHooks)
    {
        this.installationService = installationService ??
            throw new ArgumentNullException(nameof(installationService));
        this.toolchainService = toolchainService ??
            throw new ArgumentNullException(nameof(toolchainService));
        this.workspaceService = workspaceService ??
            throw new ArgumentNullException(nameof(workspaceService));
        this.processRunner = processRunner ??
            throw new ArgumentNullException(nameof(processRunner));
        this.fingerprintService = fingerprintService ??
            throw new ArgumentNullException(nameof(fingerprintService));
        this.identityService = identityService ??
            throw new ArgumentNullException(nameof(identityService));
        this.outputParser = outputParser ??
            throw new ArgumentNullException(nameof(outputParser));
        this.testHooks = testHooks;
    }

    public Task<QuickBmsExportPreparation> PrepareAsync(
        string sourceCdbPath,
        string expectedContentIdentity,
        QuickBmsImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCdbPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedContentIdentity);
        ArgumentNullException.ThrowIfNull(options);

        return Task.Run(
            () => Prepare(
                sourceCdbPath,
                expectedContentIdentity,
                options,
                cancellationToken),
            cancellationToken);
    }

    public async Task<QuickBmsExportResult> ExportAsync(
        QuickBmsExportPreparation preparation,
        IProgress<QuickBmsExportStage>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        bool cleanupIsSafe = true;
        QuickBmsExportOutcome outcome =
            QuickBmsExportOutcome.QuickBmsFailed;
        string detail = string.Empty;
        ExternalProcessResult? primaryProcess = null;
        FileFingerprint? verificationFingerprint = null;
        bool processInvocationInFlight = false;

        try
        {
            progress?.Report(QuickBmsExportStage.Exporting);
            workspaceService.ValidatePrepared(
                preparation.ExportWorkspace);

            processInvocationInFlight = true;
            primaryProcess = await processRunner.RunAsync(
                new ExternalProcessRequest
                {
                    ExecutablePath = preparation.QuickBmsExecutablePath,
                    Arguments = new[]
                    {
                        "-w",
                        "-r",
                        "-r",
                        "-f",
                        "{}data.cdb",
                        preparation.ShiroScriptPath,
                        preparation.PackagePath,
                        preparation.ModdedDirectory
                    },
                    WorkingDirectory =
                        Path.GetDirectoryName(
                            preparation.QuickBmsExecutablePath)
                        ?? preparation.ExportWorkspace.Workspace.DirectoryPath,
                    Timeout = preparation.ProcessTimeout
                },
                CancellationToken.None);
            processInvocationInFlight = false;

            if (primaryProcess.TerminationFailed)
            {
                cleanupIsSafe = false;
                outcome = QuickBmsExportOutcome.TerminationUnproven;
                detail =
                    "QuickBMS could not be confirmed stopped after export.";
                return Finish();
            }

            if (!IsSuccessfulProcess(primaryProcess))
            {
                outcome = QuickBmsExportOutcome.QuickBmsFailed;
                detail = GetProcessFailure(primaryProcess);
                return Finish();
            }

            QuickBmsReimportParseResult parse =
                outputParser.Parse(
                    primaryProcess.StandardOutput,
                    primaryProcess.StandardError);

            if (!parse.IsConfirmed)
            {
                outcome = QuickBmsExportOutcome.ReimportNotConfirmed;
                detail = parse.FailureReason;
                return Finish();
            }

            _ = installationService.ValidateForExport(
                Path.GetDirectoryName(preparation.PackagePath)
                ?? string.Empty);

            progress?.Report(QuickBmsExportStage.Verifying);
            string verificationDirectory =
                workspaceService.CreateVerificationDirectory(
                    preparation.ExportWorkspace);

            processInvocationInFlight = true;
            ExternalProcessResult verificationProcess =
                await processRunner.RunAsync(
                    new ExternalProcessRequest
                    {
                        ExecutablePath = preparation.QuickBmsExecutablePath,
                        Arguments = new[]
                        {
                            "-o",
                            "-f",
                            "{}data.cdb",
                            preparation.ShiroScriptPath,
                            preparation.PackagePath,
                            verificationDirectory
                        },
                        WorkingDirectory =
                            Path.GetDirectoryName(
                                preparation.QuickBmsExecutablePath)
                            ?? preparation.ExportWorkspace.Workspace.DirectoryPath,
                        Timeout = preparation.ProcessTimeout
                    },
                    CancellationToken.None);
            processInvocationInFlight = false;

            if (verificationProcess.TerminationFailed)
            {
                cleanupIsSafe = false;
                outcome = QuickBmsExportOutcome.TerminationUnproven;
                detail =
                    "QuickBMS verification could not be confirmed stopped.";
                return Finish();
            }

            if (!IsSuccessfulProcess(verificationProcess))
            {
                outcome = QuickBmsExportOutcome.VerificationFailed;
                detail = GetProcessFailure(verificationProcess);
                return Finish();
            }

            string verifiedCdb =
                workspaceService.ValidateVerificationResult(
                    preparation.ExportWorkspace);
            verificationFingerprint =
                fingerprintService.Calculate(verifiedCdb);

            if (verificationFingerprint !=
                preparation.StagedFingerprint)
            {
                outcome = QuickBmsExportOutcome.VerificationFailed;
                detail =
                    "The data extracted after export did not match the saved project bytes.";
                return Finish();
            }

            outcome = QuickBmsExportOutcome.Success;
            try
            {
                progress?.Report(QuickBmsExportStage.Completed);
            }
            catch (Exception exception)
            {
                detail =
                    "Export completed and was verified, but the completed progress notification failed: " +
                    exception.Message;
            }
            return Finish();
        }
        catch (Exception exception)
        {
            if (processInvocationInFlight)
            {
                cleanupIsSafe = false;
                outcome = QuickBmsExportOutcome.TerminationUnproven;
            }
            else
            {
                outcome = primaryProcess == null
                    ? QuickBmsExportOutcome.PreflightFailed
                    : QuickBmsExportOutcome.VerificationFailed;
            }
            detail = exception.Message;
            return Finish();
        }

        QuickBmsExportResult Finish()
        {
            bool cleaned = cleanupIsSafe &&
                workspaceService.TryClean(
                    preparation.ExportWorkspace);

            return new QuickBmsExportResult
            {
                Outcome = outcome,
                DiagnosticDetail = detail,
                SourceFingerprint = preparation.SourceFingerprint,
                StagedFingerprint = preparation.StagedFingerprint,
                VerificationFingerprint = verificationFingerprint,
                ProcessResult = primaryProcess,
                StagingCleaned = cleaned,
                PreservedWorkspacePath = cleaned
                    ? string.Empty
                    : preparation.ExportWorkspace.Workspace.DirectoryPath
            };
        }
    }

    public bool TryCancelPreparation(
        QuickBmsExportPreparation preparation)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        return workspaceService.TryClean(
            preparation.ExportWorkspace);
    }

    private QuickBmsExportPreparation Prepare(
        string sourceCdbPath,
        string expectedContentIdentity,
        QuickBmsImportOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string source = Path.GetFullPath(sourceCdbPath);

        if (!File.Exists(source))
        {
            throw new FileNotFoundException(
                "The saved Wartales project file could not be found.",
                source);
        }

        byte[] sourceBytes = File.ReadAllBytes(source);
        string actualIdentity = identityService.Calculate(sourceBytes);

        if (!identityService.AreEqual(
                actualIdentity,
                expectedContentIdentity))
        {
            throw new InvalidDataException(
                "The saved Wartales project changed outside the editor. Save it again or reopen it before exporting.");
        }

        FileFingerprint sourceFingerprint =
            fingerprintService.Calculate(sourceBytes);
        testHooks?.SourceSnapshotAccepted?.Invoke();
        QuickBmsToolchainInfo toolchain =
            toolchainService.Validate(
                options.QuickBmsExecutablePath,
                options.ShiroScriptPath);

        if (!string.Equals(
                Path.GetFileName(toolchain.ExecutablePath),
                "quickbms.exe",
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                Path.GetFileName(toolchain.ScriptPath),
                "Shiro_Games_PAK_script.bms",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ToolchainInvalid,
                "Export requires quickbms.exe and the Shiro Games PAK script.");
        }
        WartalesPackageInfo package =
            installationService.ValidateForExport(
                options.WartalesInstallationDirectory);
        string exportRoot = Path.Combine(
            Path.GetTempPath(),
            "WartalesEditor",
            "QuickBmsExport");
        QuickBmsExportWorkspace exportWorkspace;

        try
        {
            exportWorkspace =
                workspaceService.Create(exportRoot);
        }
        catch (QuickBmsExportWorkspaceCreationException exception)
        {
            throw new QuickBmsExportPreparationException(
                "Export could not be prepared. Wartales game files were not changed.",
                false,
                exception.PreservedWorkspacePath,
                exception);
        }

        try
        {
            testHooks?.WorkspaceCreated?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            File.WriteAllBytes(
                exportWorkspace.StagedCdbPath,
                sourceBytes);
            workspaceService.ValidatePrepared(exportWorkspace);
            FileFingerprint stagedFingerprint =
                fingerprintService.Calculate(
                    exportWorkspace.StagedCdbPath);

            if (stagedFingerprint != sourceFingerprint)
            {
                throw new InvalidDataException(
                    "The temporary export data did not match the saved project.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new QuickBmsExportPreparation(exportWorkspace)
            {
                SourceCdbPath = source,
                StagedCdbPath = exportWorkspace.StagedCdbPath,
                ModdedDirectory = exportWorkspace.ModdedDirectory,
                VerificationDirectory = exportWorkspace.VerificationDirectory,
                PackagePath = package.PackagePath,
                QuickBmsExecutablePath = toolchain.ExecutablePath,
                ShiroScriptPath = toolchain.ScriptPath,
                SourceFingerprint = sourceFingerprint,
                StagedFingerprint = stagedFingerprint,
                ProcessTimeout = options.ProcessTimeout
            };
        }
        catch (Exception exception)
        {
            bool cleaned = workspaceService.TryClean(exportWorkspace);

            if (cleaned)
                throw;

            throw new QuickBmsExportPreparationException(
                exception is OperationCanceledException
                    ? "Export was cancelled before Wartales was changed."
                    : "Export could not be prepared. Wartales game files were not changed.",
                exception is OperationCanceledException,
                exportWorkspace.Workspace.DirectoryPath,
                exception);
        }
    }

    private static bool IsSuccessfulProcess(
        ExternalProcessResult result)
    {
        return result.Started &&
               !result.TimedOut &&
               !result.Cancelled &&
               !result.TerminationFailed &&
               result.ContainedProcessCount == 0 &&
               result.ExitCode == 0;
    }

    private static string GetProcessFailure(
        ExternalProcessResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StartError))
            return result.StartError;
        if (!string.IsNullOrWhiteSpace(result.ExecutionError))
            return result.ExecutionError;
        if (!string.IsNullOrWhiteSpace(result.StandardError))
            return result.StandardError.Trim();
        if (result.TimedOut)
            return "QuickBMS timed out.";
        if (result.Cancelled)
            return "QuickBMS was cancelled.";

        return $"QuickBMS exited with code {result.ExitCode}.";
    }
}
