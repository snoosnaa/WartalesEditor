using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.Services.Validation;
using WartalesEditor.ViewModels;

if (args.Length == 5 &&
    string.Equals(args[0], "--real-copied-package", StringComparison.Ordinal))
{
    await RunRealCopiedPackageExportAsync(
        args[1],
        args[2],
        args[3],
        args[4]);
    return;
}

int checks = 0;
string testRoot = Path.Combine(
    Path.GetTempPath(),
    "WartalesEditorQuickBmsExportTests",
    Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testRoot);

try
{
    TestParser();
    TestWorkspace();
    TestExportReparseMatrix();
    TestPackagePreflight();
    TestProgressAndUiContract();
    await TestSourceSnapshotAuthorityAsync();
    await TestSuccessfulExportAsync();
    await TestFailureOutcomesAsync();
    await TestTimeoutAndCleanupMatrixAsync();
    await TestCompletedProgressFailureAsync();
    await TestPartialWorkspaceCreationFailuresAsync();
    TestSourceIdentityFailures();
    await RunMainViewModelBehavioralTestsAsync();

    Console.WriteLine(
        $"QuickBMS Export smoke tests passed: {checks}/{checks}");
}

finally
{
    if (Directory.Exists(testRoot))
        Directory.Delete(testRoot, recursive: true);
}

static async Task RunRealCopiedPackageExportAsync(
    string sourceCdb,
    string copiedInstallation,
    string quickBmsExecutable,
    string shiroScript)
{
    string identity = new CdbGenerationIdentityService().Calculate(sourceCdb);
    QuickBmsExportService service = new();
    QuickBmsExportPreparation preparation = await service.PrepareAsync(
        sourceCdb,
        identity,
        new QuickBmsImportOptions
        {
            WartalesInstallationDirectory = copiedInstallation,
            QuickBmsExecutablePath = quickBmsExecutable,
            ShiroScriptPath = shiroScript,
            StagingRootDirectory = Path.Combine(copiedInstallation, "unused-import"),
            ProcessTimeout = TimeSpan.FromMinutes(5)
        });
    QuickBmsExportResult result = await service.ExportAsync(preparation);

    if (result.Outcome != QuickBmsExportOutcome.Success ||
        result.SourceFingerprint != result.StagedFingerprint ||
        result.StagedFingerprint != result.VerificationFingerprint ||
        !result.StagingCleaned)
    {
        throw new InvalidOperationException(
            $"Real copied-package export failed: {result.Outcome}; {result.DiagnosticDetail}");
    }

    Console.WriteLine(
        $"REAL COPIED-PACKAGE EXPORT PASSED: {preparation.SourceFingerprint.Size} bytes, " +
        $"{preparation.SourceFingerprint.Sha256}, cleanup={result.StagingCleaned}");
}

void TestParser()
{
    QuickBmsReimportOutputParser parser = new();
    QuickBmsReimportParseResult valid = parser.Parse(
        "< 00000000 123 data.cdb\rspinner\r",
        "- 1 files reimported in 0 seconds\r\n");
    Check(valid.IsConfirmed, "parser accepts split streams");
    Check(valid.ReimportCount == 1, "parser captures count");
    Check(valid.FileNames.SequenceEqual(new[] { "data.cdb" }),
        "parser captures exact file");

    Check(!parser.Parse("< 00000000 1 data.cdb", "").IsConfirmed,
        "missing summary rejected");
    Check(!parser.Parse("", "- 1 files reimported in 0 seconds").IsConfirmed,
        "missing record rejected");
    Check(!parser.Parse("< 00000000 1 data.cdb", "- 0 files reimported in 0 seconds").IsConfirmed,
        "zero count rejected");
    Check(!parser.Parse("< 00000000 1 data.cdb", "- 2 files reimported in 0 seconds").IsConfirmed,
        "multiple count rejected");
    Check(!parser.Parse("< 00000000 1 other.cdb", "- 1 files reimported in 0 seconds").IsConfirmed,
        "wrong file rejected");
    Check(!parser.Parse("< 00000000 1 folder/data.cdb", "- 1 files reimported in 0 seconds").IsConfirmed,
        "nested file rejected");
    Check(!parser.Parse(
        "< 00000000 1 data.cdb\n< 00000000 1 other.cdb",
        "- 1 files reimported in 0 seconds").IsConfirmed,
        "multiple records rejected");
    Check(!parser.Parse(
        "< 00000000 1 data.cdb",
        "- 1 files reimported in 0 seconds\n- 1 files reimported in 0 seconds").IsConfirmed,
        "duplicate summaries rejected");
    Check(!parser.Parse("unstructured", "unstructured").IsConfirmed,
        "malformed output rejected");
}

void TestWorkspace()
{
    string root = Path.Combine(testRoot, "workspace");
    ExtractionWorkspaceService extraction = new();
    QuickBmsExportWorkspaceService service = new(extraction);
    QuickBmsExportWorkspace workspace = service.Create(root);
    Check(Guid.TryParseExact(workspace.Workspace.SessionId, "N", out _),
        "workspace uses N GUID");
    Check(Path.GetDirectoryName(workspace.Workspace.DirectoryPath) ==
          Path.GetFullPath(root), "workspace is under configured root");
    Check(File.ReadAllText(workspace.MarkerPath, Encoding.UTF8) ==
          "WartalesEditor QuickBMS Export\n1\n" +
          workspace.Workspace.SessionId + "\n", "marker contract");
    Check(Directory.Exists(workspace.ModdedDirectory),
        "Modded exists");
    Check(!Directory.Exists(workspace.VerificationDirectory),
        "Verify is deferred");

    File.WriteAllBytes(workspace.StagedCdbPath, new byte[] { 1, 2, 3 });
    service.ValidatePrepared(workspace);
    Check(Directory.GetFileSystemEntries(workspace.ModdedDirectory).Length == 1,
        "Modded contains exact one file");

    string verify = service.CreateVerificationDirectory(workspace);
    File.WriteAllBytes(Path.Combine(verify, "data.cdb"), new byte[] { 1 });
    Check(service.ValidateVerificationResult(workspace).EndsWith("data.cdb"),
        "verification exact file accepted");
    Check(service.TryClean(workspace), "owned workspace cleaned");
    Check(!Directory.Exists(workspace.Workspace.DirectoryPath),
        "session removed");

    QuickBmsExportWorkspace stale = service.Create(root);
    File.WriteAllBytes(stale.StagedCdbPath, new byte[] { 1 });
    QuickBmsExportWorkspace next = service.Create(root);
    Check(!Directory.Exists(stale.Workspace.DirectoryPath),
        "safe stale workspace reconciled");
    Check(service.TryClean(next), "replacement workspace cleaned");

    File.WriteAllText(Path.Combine(root, "unknown.txt"), "unknown");
    CheckThrows(() => service.Create(root),
        "unrecognized root child blocks without deletion");
    Check(File.Exists(Path.Combine(root, "unknown.txt")),
        "unrecognized child preserved");
    File.Delete(Path.Combine(root, "unknown.txt"));
}

void TestExportReparseMatrix()
{
    string matrixRoot = Path.Combine(testRoot, "export-reparse-matrix");
    Directory.CreateDirectory(matrixRoot);
    QuickBmsExportWorkspaceService service = new();

    string rootTarget = Path.Combine(matrixRoot, "root-target");
    string rootLink = Path.Combine(matrixRoot, "root-link");
    Directory.CreateDirectory(rootTarget);
    string rootSentinel = Path.Combine(rootTarget, "sentinel.txt");
    File.WriteAllText(rootSentinel, "preserve");
    CreateDirectoryJunction(rootLink, rootTarget);
    CheckThrows(() => service.Create(rootLink),
        "export root reparse is rejected");
    Check(File.Exists(rootSentinel) &&
          Directory.GetFileSystemEntries(rootTarget).Length == 1,
        "export root reparse rejection does not traverse its target");
    Directory.Delete(rootLink);

    string staleRoot = Path.Combine(matrixRoot, "stale-root");
    string staleTarget = Path.Combine(matrixRoot, "stale-target");
    Directory.CreateDirectory(staleRoot);
    Directory.CreateDirectory(staleTarget);
    string staleSentinel = Path.Combine(staleTarget, "sentinel.txt");
    File.WriteAllText(staleSentinel, "preserve");
    string staleLink = Path.Combine(staleRoot, Guid.NewGuid().ToString("N"));
    CreateDirectoryJunction(staleLink, staleTarget);
    CheckThrows(() => service.Create(staleRoot),
        "stale export session reparse is rejected");
    Check(File.Exists(staleSentinel),
        "stale session reparse rejection preserves external target");
    Directory.Delete(staleLink);

    QuickBmsExportWorkspace modded = service.Create(
        Path.Combine(matrixRoot, "modded-root"));
    Directory.Delete(modded.ModdedDirectory);
    string moddedTarget = Path.Combine(matrixRoot, "modded-target");
    Directory.CreateDirectory(moddedTarget);
    string moddedSentinel = Path.Combine(moddedTarget, "sentinel.txt");
    File.WriteAllText(moddedSentinel, "preserve");
    CreateDirectoryJunction(modded.ModdedDirectory, moddedTarget);
    CheckThrows(() => service.ValidatePrepared(modded),
        "Modded directory reparse is rejected");
    Check(!service.TryClean(modded) && File.Exists(moddedSentinel),
        "Modded reparse blocks cleanup without traversing target");
    Directory.Delete(modded.ModdedDirectory);
    Directory.CreateDirectory(modded.ModdedDirectory);
    Check(service.TryClean(modded),
        "Modded reparse test workspace cleans after link removal");

    QuickBmsExportWorkspace verify = service.Create(
        Path.Combine(matrixRoot, "verify-root"));
    File.WriteAllBytes(verify.StagedCdbPath, new byte[] { 1 });
    string verifyTarget = Path.Combine(matrixRoot, "verify-target");
    Directory.CreateDirectory(verifyTarget);
    string verifySentinel = Path.Combine(verifyTarget, "sentinel.txt");
    File.WriteAllText(verifySentinel, "preserve");
    CreateDirectoryJunction(verify.VerificationDirectory, verifyTarget);
    CheckThrows(() => service.CreateVerificationDirectory(verify),
        "Verify directory reparse is rejected");
    Check(!service.TryClean(verify) && File.Exists(verifySentinel),
        "Verify reparse blocks cleanup without traversing target");
    Directory.Delete(verify.VerificationDirectory);
    Check(service.TryClean(verify),
        "Verify reparse test workspace cleans after link removal");

    QuickBmsExportWorkspace descendant = service.Create(
        Path.Combine(matrixRoot, "descendant-root"));
    string descendantTarget = Path.Combine(matrixRoot, "descendant-target");
    Directory.CreateDirectory(descendantTarget);
    string descendantSentinel = Path.Combine(descendantTarget, "sentinel.txt");
    File.WriteAllText(descendantSentinel, "preserve");
    string descendantLink = Path.Combine(descendant.ModdedDirectory, "external");
    CreateDirectoryJunction(descendantLink, descendantTarget);
    CheckThrows(
        () => service.Create(descendant.Workspace.RootDirectory),
        "descendant reparse in stale export tree blocks next session");
    Check(File.Exists(descendantSentinel),
        "stale descendant reparse is not traversed or deleted");
    Directory.Delete(descendantLink);
    Check(service.TryClean(descendant),
        "descendant reparse workspace cleans after link removal");

    TestFileReparseBoundaries(service, matrixRoot);
}

void TestFileReparseBoundaries(
    QuickBmsExportWorkspaceService service,
    string matrixRoot)
{
    string externalFile = Path.Combine(matrixRoot, "external-file.cdb");
    File.WriteAllBytes(externalFile, new byte[] { 7, 8, 9 });
    bool supported = false;

    try
    {
        QuickBmsExportWorkspace marker = service.Create(
            Path.Combine(matrixRoot, "marker-root"));
        string markerContent = File.ReadAllText(marker.MarkerPath, Encoding.UTF8);
        string externalMarker = Path.Combine(matrixRoot, "external-marker");
        File.WriteAllText(externalMarker, markerContent, new UTF8Encoding(false));
        File.Delete(marker.MarkerPath);
        File.CreateSymbolicLink(marker.MarkerPath, externalMarker);
        supported = true;
        CheckThrows(() => service.ValidatePrepared(marker),
            "ownership marker reparse is rejected");
        Check(!service.TryClean(marker) && File.Exists(externalMarker),
            "marker reparse cleanup preserves external file");
        File.Delete(marker.MarkerPath);
        File.WriteAllText(marker.MarkerPath, markerContent, new UTF8Encoding(false));
        Check(service.TryClean(marker),
            "marker reparse workspace cleans after link removal");

        QuickBmsExportWorkspace staged = service.Create(
            Path.Combine(matrixRoot, "staged-root"));
        File.CreateSymbolicLink(staged.StagedCdbPath, externalFile);
        CheckThrows(() => service.ValidatePrepared(staged),
            "staged data.cdb reparse is rejected");
        Check(!service.TryClean(staged) && File.Exists(externalFile),
            "staged data.cdb reparse cleanup preserves external file");
        File.Delete(staged.StagedCdbPath);
        Check(service.TryClean(staged),
            "staged reparse workspace cleans after link removal");

        QuickBmsExportWorkspace verification = service.Create(
            Path.Combine(matrixRoot, "verification-file-root"));
        File.WriteAllBytes(verification.StagedCdbPath, new byte[] { 1 });
        string verificationDirectory =
            service.CreateVerificationDirectory(verification);
        File.CreateSymbolicLink(
            Path.Combine(verificationDirectory, "data.cdb"),
            externalFile);
        CheckThrows(() => service.ValidateVerificationResult(verification),
            "verification data.cdb reparse is rejected");
        Check(!service.TryClean(verification) && File.Exists(externalFile),
            "verification reparse cleanup preserves external file");
        File.Delete(Path.Combine(verificationDirectory, "data.cdb"));
        Check(service.TryClean(verification),
            "verification reparse workspace cleans after link removal");

        string install = Path.Combine(matrixRoot, "package-link-install");
        Directory.CreateDirectory(install);
        File.WriteAllText(Path.Combine(install, "Wartales.exe"), "fixture");
        string externalPackage = Path.Combine(matrixRoot, "external-res.pak");
        File.WriteAllBytes(externalPackage,
            new byte[] { (byte)'P', (byte)'A', (byte)'K', 0, 1 });
        File.CreateSymbolicLink(Path.Combine(install, "res.pak"), externalPackage);
        CheckThrows(() => new WartalesInstallationService().ValidateForExport(install),
            "live res.pak reparse is rejected before write");
        Check(File.Exists(externalPackage),
            "package reparse rejection preserves external package");
        File.Delete(Path.Combine(install, "res.pak"));
    }
    catch (Exception exception)
        when (!supported &&
              exception is UnauthorizedAccessException or IOException)
    {
        Console.WriteLine(
            "SKIP file-reparse matrix: Windows symbolic-link privilege is unavailable");
    }
}

void TestPackagePreflight()
{
    WartalesInstallationService service = new();
    string install = Path.Combine(testRoot, "package-preflight");
    Directory.CreateDirectory(install);
    File.WriteAllText(Path.Combine(install, "Wartales.exe"), "fixture");
    string package = Path.Combine(install, "res.pak");
    File.WriteAllBytes(package, new byte[] { (byte)'P', (byte)'A', (byte)'K', 0, 1 });
    WartalesPackageInfo info = service.ValidateForExport(install);
    Check(info.PackagePath == package, "preflight targets exact res.pak");
    Check(info.PackageSize == 5, "preflight reports package size");

    File.WriteAllBytes(package, new byte[] { 1, 2, 3, 4, 5 });
    CheckThrows(() => service.ValidateForExport(install),
        "invalid PAK signature rejected");
    File.Delete(package);
    CheckThrows(() => service.ValidateForExport(install),
        "missing package rejected");
}

void TestProgressAndUiContract()
{
    QuickBmsExportProgressViewModel progress = new();
    int cancellations = 0;
    progress.CancellationRequested += (_, _) => cancellations++;
    progress.SetStage(QuickBmsExportStage.Preparing);
    Check(progress.CanCancel, "preparation cancellation enabled");
    Check(progress.StageText.Contains("Preparing", StringComparison.Ordinal),
        "preparation stage text");
    progress.RequestCancellation();
    Check(cancellations == 1, "preparation cancellation event");
    progress.SetStage(QuickBmsExportStage.Exporting);
    progress.RequestCancellation();
    Check(!progress.CanCancel && cancellations == 1,
        "write cancellation disabled");
    progress.SetStage(QuickBmsExportStage.Verifying);
    Check(!progress.CanCancel &&
          progress.StageText.Contains("Verifying", StringComparison.Ordinal),
        "verification cancellation disabled");
    progress.SetStage(QuickBmsExportStage.Completed);
    Check(progress.StageText.Contains("completed", StringComparison.Ordinal),
        "completed stage text");

    string repository = FindRepositoryRoot();
    string mainXaml = File.ReadAllText(Path.Combine(repository, "MainWindow.xaml"));
    int importIndex = mainXaml.IndexOf("ImportFromWartalesCommand", StringComparison.Ordinal);
    int exportIndex = mainXaml.IndexOf("ExportBackToWartalesCommand", StringComparison.Ordinal);
    int openIndex = mainXaml.IndexOf("OpenCommand", exportIndex, StringComparison.Ordinal);
    Check(importIndex >= 0 && exportIndex > importIndex && openIndex > exportIndex,
        "File menu Import Export Open ordering");

    string dialogXaml = File.ReadAllText(Path.Combine(
        repository, "Views", "QuickBmsExportProgressDialog.xaml"));
    Check(dialogXaml.Contains("WindowStartupLocation=\"CenterOwner\"", StringComparison.Ordinal) &&
          dialogXaml.Contains("ShowInTaskbar=\"False\"", StringComparison.Ordinal) &&
          dialogXaml.Contains("IsIndeterminate=\"True\"", StringComparison.Ordinal),
        "progress window placement and indeterminate contract");

    string mainViewModel = File.ReadAllText(Path.Combine(
        repository, "ViewModels", "MainViewModel.cs"));
    Check(mainViewModel.Contains("progressDialog.Owner = mainWindow", StringComparison.Ordinal) &&
          mainViewModel.Contains("ApplicationCloseReady", StringComparison.Ordinal),
        "owner and deferred-close wiring");
    Check(mainViewModel.Contains("Project.IsGameplayOperationStateModified", StringComparison.Ordinal) &&
          mainViewModel.Contains("if (!SaveProject())", StringComparison.Ordinal),
        "save-first gameplay-state wiring");
    Check(mainViewModel.Contains("!IsQuickBmsOperationInProgress", StringComparison.Ordinal) &&
          mainViewModel.Contains("QuickBmsOperationKind.Importing", StringComparison.Ordinal),
        "shared QuickBMS gate wiring");
}

string FindRepositoryRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "WartalesEditor.csproj")))
            return directory.FullName;
        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Repository root not found.");
}

async Task TestSuccessfulExportAsync()
{
    Fixture fixture = CreateFixture("success");
    FakeRunner runner = new(fixture.SourcePath);
    QuickBmsExportService service = CreateService(runner);
    QuickBmsExportPreparation preparation = await service.PrepareAsync(
        fixture.SourcePath, fixture.Identity, fixture.Options);

    Check(File.ReadAllBytes(preparation.StagedCdbPath)
            .SequenceEqual(File.ReadAllBytes(fixture.SourcePath)),
        "exact persisted bytes staged");
    Check(preparation.SourceFingerprint == preparation.StagedFingerprint,
        "staged fingerprint equals source");
    Check(Directory.GetFiles(preparation.ModdedDirectory)
            .Select(Path.GetFileName).SequenceEqual(new[] { "data.cdb" }),
        "no extra staged content");
    Check(preparation.PackagePath == Path.Combine(fixture.Install, "res.pak"),
        "isolated package selected");
    Check(preparation.QuickBmsExecutablePath.EndsWith("quickbms.exe"),
        "accepted executable selected");
    Check(preparation.ShiroScriptPath.EndsWith("Shiro_Games_PAK_script.bms"),
        "accepted script selected");

    List<QuickBmsExportStage> stages = new();
    QuickBmsExportResult result = await service.ExportAsync(
        preparation,
        new DirectProgress<QuickBmsExportStage>(stages.Add));
    Check(result.Outcome == QuickBmsExportOutcome.Success,
        "verified success outcome");
    Check(result.StagingCleaned, "success staging cleaned");
    Check(string.IsNullOrEmpty(result.PreservedWorkspacePath),
        "clean success does not expose workspace");
    Check(result.SourceFingerprint == result.StagedFingerprint &&
          result.StagedFingerprint == result.VerificationFingerprint,
        "source staged and verified fingerprints equal");
    Check(stages.SequenceEqual(new[]
        {
            QuickBmsExportStage.Exporting,
            QuickBmsExportStage.Verifying,
            QuickBmsExportStage.Completed
        }), "progress stage sequence");
    Check(runner.Requests.Count == 2, "write and verification use runner");

    string[] write = runner.Requests[0].Arguments.ToArray();
    Check(write.SequenceEqual(new[]
        {
            "-w", "-r", "-r", "-f", "{}data.cdb",
            fixture.Options.ShiroScriptPath,
            Path.Combine(fixture.Install, "res.pak"),
            preparation.ModdedDirectory
        }), "exact write arguments and order");
    Check(Path.IsPathFullyQualified(write[5]) &&
          Path.IsPathFullyQualified(write[6]) &&
          Path.IsPathFullyQualified(write[7]),
        "write paths absolute");

    string[] verify = runner.Requests[1].Arguments.ToArray();
    Check(verify.SequenceEqual(new[]
        {
            "-o", "-f", "{}data.cdb",
            fixture.Options.ShiroScriptPath,
            Path.Combine(fixture.Install, "res.pak"),
            preparation.VerificationDirectory
        }), "exact verification arguments and order");
    Check(!verify.Contains("-w") && !verify.Contains("-r"),
        "verification remains read-only");
    Check(runner.CancellationTokens.All(token => !token.CanBeCanceled),
        "active write and verification are not user cancellable");
}

async Task TestSourceSnapshotAuthorityAsync()
{
    Fixture fixture = CreateFixture("source-snapshot");
    byte[] acceptedBytes = File.ReadAllBytes(fixture.SourcePath);
    byte[] replacementBytes = Encoding.UTF8.GetBytes(
        "{\"sheets\":[{\"name\":\"changed-on-disk\",\"lines\":[]}]}" );
    FakeRunner runner = new(fixture.SourcePath);
    QuickBmsExportService service = CreateService(
        runner,
        hooks: new QuickBmsExportTestHooks
        {
            SourceSnapshotAccepted = () =>
                File.WriteAllBytes(fixture.SourcePath, replacementBytes)
        });

    QuickBmsExportPreparation preparation = await service.PrepareAsync(
        fixture.SourcePath,
        fixture.Identity,
        fixture.Options);
    byte[] staged = File.ReadAllBytes(preparation.StagedCdbPath);
    Check(staged.SequenceEqual(acceptedBytes),
        "post-snapshot disk replacement cannot change staged authority");
    Check(!staged.SequenceEqual(replacementBytes) &&
          File.ReadAllBytes(fixture.SourcePath).SequenceEqual(replacementBytes),
        "changed disk bytes are not exported after snapshot acceptance");
    Check(preparation.SourceFingerprint ==
          new FileFingerprintService().Calculate(acceptedBytes),
        "snapshot fingerprint derives from accepted exact bytes");
    Check(preparation.SourceFingerprint == preparation.StagedFingerprint,
        "staged fingerprint equals authoritative snapshot");
    Check(service.TryCancelPreparation(preparation),
        "snapshot regression workspace cleaned");
}

async Task TestFailureOutcomesAsync()
{
    Fixture zeroFixture = CreateFixture("zero");
    FakeRunner zeroRunner = new(zeroFixture.SourcePath)
    {
        WriteOutput = "< 00000000 1 data.cdb",
        WriteError = "- 0 files reimported in 0 seconds"
    };
    QuickBmsExportService zeroService = CreateService(zeroRunner);
    QuickBmsExportResult zero = await zeroService.ExportAsync(
        await zeroService.PrepareAsync(
            zeroFixture.SourcePath, zeroFixture.Identity, zeroFixture.Options));
    Check(zero.Outcome == QuickBmsExportOutcome.ReimportNotConfirmed,
        "zero reimports not confirmed");
    Check(zero.StagingCleaned, "not-confirmed staging cleaned");
    Check(zeroRunner.Requests.Count == 1,
        "not-confirmed write is not retried or verified");

    Fixture failedFixture = CreateFixture("failed");
    FakeRunner failedRunner = new(failedFixture.SourcePath)
    {
        WriteExitCode = 7
    };
    QuickBmsExportService failedService = CreateService(failedRunner);
    QuickBmsExportResult failed = await failedService.ExportAsync(
        await failedService.PrepareAsync(
            failedFixture.SourcePath, failedFixture.Identity, failedFixture.Options));
    Check(failed.Outcome == QuickBmsExportOutcome.QuickBmsFailed,
        "nonzero write fails");
    Check(failed.StagingCleaned, "ordinary write failure cleaned");

    Fixture terminationFixture = CreateFixture("termination");
    FakeRunner terminationRunner = new(terminationFixture.SourcePath)
    {
        WriteTerminationFailed = true
    };
    QuickBmsExportService terminationService = CreateService(terminationRunner);
    QuickBmsExportResult termination = await terminationService.ExportAsync(
        await terminationService.PrepareAsync(
            terminationFixture.SourcePath,
            terminationFixture.Identity,
            terminationFixture.Options));
    Check(termination.Outcome == QuickBmsExportOutcome.TerminationUnproven,
        "unproven termination outcome");
    Check(!termination.StagingCleaned &&
          Directory.Exists(termination.PreservedWorkspacePath),
        "unproven termination preserves workspace");

    Fixture mismatchFixture = CreateFixture("mismatch");
    FakeRunner mismatchRunner = new(mismatchFixture.SourcePath)
    {
        CorruptVerification = true
    };
    QuickBmsExportService mismatchService = CreateService(mismatchRunner);
    QuickBmsExportResult mismatch = await mismatchService.ExportAsync(
        await mismatchService.PrepareAsync(
            mismatchFixture.SourcePath,
            mismatchFixture.Identity,
            mismatchFixture.Options));
    Check(mismatch.Outcome == QuickBmsExportOutcome.VerificationFailed,
        "verification mismatch fails");
    Check(mismatch.StagingCleaned, "verification mismatch cleaned");

    Fixture verifyTerminationFixture = CreateFixture("verify-termination");
    FakeRunner verifyTerminationRunner = new(verifyTerminationFixture.SourcePath)
    {
        VerificationTerminationFailed = true
    };
    QuickBmsExportService verifyTerminationService =
        CreateService(verifyTerminationRunner);
    QuickBmsExportResult verifyTermination =
        await verifyTerminationService.ExportAsync(
            await verifyTerminationService.PrepareAsync(
                verifyTerminationFixture.SourcePath,
                verifyTerminationFixture.Identity,
                verifyTerminationFixture.Options));
    Check(verifyTermination.Outcome ==
          QuickBmsExportOutcome.TerminationUnproven,
        "verification termination must be proven");
    Check(!verifyTermination.StagingCleaned,
        "unproven verification preserves workspace");

    Fixture packageFixture = CreateFixture("package-corruption");
    FakeRunner packageRunner = new(packageFixture.SourcePath)
    {
        CorruptPackageAfterWrite = true
    };
    QuickBmsExportService packageService = CreateService(packageRunner);
    QuickBmsExportResult packageResult = await packageService.ExportAsync(
        await packageService.PrepareAsync(
            packageFixture.SourcePath,
            packageFixture.Identity,
            packageFixture.Options));
    Check(packageResult.Outcome == QuickBmsExportOutcome.VerificationFailed,
        "post-write package signature corruption fails");
    Check(packageRunner.Requests.Count == 1,
        "corrupt package is not verification-extracted");
}

async Task TestTimeoutAndCleanupMatrixAsync()
{
    Fixture cleanCancelFixture = CreateFixture("cleanup-preparation-cancel-clean");
    QuickBmsExportService cleanCancelService = CreateService(
        new FakeRunner(cleanCancelFixture.SourcePath),
        hooks: new QuickBmsExportTestHooks
        {
            WorkspaceCreated = () => throw new OperationCanceledException()
        });
    bool cleanCancellationObserved = false;
    try
    {
        _ = await cleanCancelService.PrepareAsync(
            cleanCancelFixture.SourcePath,
            cleanCancelFixture.Identity,
            cleanCancelFixture.Options);
    }
    catch (OperationCanceledException)
    {
        cleanCancellationObserved = true;
    }
    Check(cleanCancellationObserved,
        "preparation cancellation is preserved when cleanup succeeds");
    string sharedExportRoot = Path.Combine(
        Path.GetTempPath(), "WartalesEditor", "QuickBmsExport");
    Check(!Directory.Exists(sharedExportRoot) ||
          Directory.GetFileSystemEntries(sharedExportRoot).Length == 0,
        "successful preparation-cancellation cleanup leaves no session");

    Fixture writeTimeoutFixture = CreateFixture("write-timeout");
    FakeRunner writeTimeoutRunner = new(writeTimeoutFixture.SourcePath)
    {
        WriteTimedOut = true
    };
    QuickBmsExportService writeTimeoutService = CreateService(writeTimeoutRunner);
    QuickBmsExportResult writeTimeout = await writeTimeoutService.ExportAsync(
        await writeTimeoutService.PrepareAsync(
            writeTimeoutFixture.SourcePath,
            writeTimeoutFixture.Identity,
            writeTimeoutFixture.Options));
    Check(writeTimeout.Outcome == QuickBmsExportOutcome.QuickBmsFailed,
        "write timeout with proven termination is a write failure");
    Check(writeTimeout.StagingCleaned && writeTimeoutRunner.Requests.Count == 1,
        "proven write timeout cleans and never verifies");

    Fixture verifyTimeoutFixture = CreateFixture("verify-timeout");
    FakeRunner verifyTimeoutRunner = new(verifyTimeoutFixture.SourcePath)
    {
        VerificationTimedOut = true
    };
    QuickBmsExportService verifyTimeoutService = CreateService(verifyTimeoutRunner);
    QuickBmsExportResult verifyTimeout = await verifyTimeoutService.ExportAsync(
        await verifyTimeoutService.PrepareAsync(
            verifyTimeoutFixture.SourcePath,
            verifyTimeoutFixture.Identity,
            verifyTimeoutFixture.Options));
    Check(verifyTimeout.Outcome == QuickBmsExportOutcome.VerificationFailed,
        "verification timeout is verification failure");
    Check(verifyTimeout.StagingCleaned,
        "proven verification timeout cleans staging");

    await CheckCleanupFailureOutcome(
        "cleanup-success",
        new FakeRunnerFactory(),
        QuickBmsExportOutcome.Success,
        "verified success survives cleanup failure");
    await CheckCleanupFailureOutcome(
        "cleanup-write-failure",
        new FakeRunnerFactory { WriteExitCode = 9 },
        QuickBmsExportOutcome.QuickBmsFailed,
        "write failure survives cleanup failure");
    await CheckCleanupFailureOutcome(
        "cleanup-unconfirmed",
        new FakeRunnerFactory
        {
            WriteError = "- 0 files reimported in 0 seconds"
        },
        QuickBmsExportOutcome.ReimportNotConfirmed,
        "unconfirmed reimport survives cleanup failure");
    await CheckCleanupFailureOutcome(
        "cleanup-verification",
        new FakeRunnerFactory { CorruptVerification = true },
        QuickBmsExportOutcome.VerificationFailed,
        "verification failure survives cleanup failure");

    Fixture cancelFixture = CreateFixture("cleanup-preparation-cancel");
    FaultingWorkspaceService cancelWorkspace = new(cleanupResult: false);
    QuickBmsExportService cancelService = CreateService(
        new FakeRunner(cancelFixture.SourcePath),
        cancelWorkspace,
        new QuickBmsExportTestHooks
        {
            WorkspaceCreated = () =>
                throw new OperationCanceledException()
        });
    QuickBmsExportPreparationException cancelException =
        await CapturePreparationException(() => cancelService.PrepareAsync(
            cancelFixture.SourcePath,
            cancelFixture.Identity,
            cancelFixture.Options));
    Check(cancelException.WasCancelled && !cancelException.StagingCleaned,
        "preparation cancellation preserves cleanup failure");
    Check(!string.IsNullOrWhiteSpace(cancelException.PreservedWorkspacePath),
        "cancelled preparation retains diagnostic workspace path");
    cancelWorkspace.CleanupResult = true;
    Check(cancelWorkspace.TryClean(cancelWorkspace.LastWorkspace!),
        "cancelled preparation retained workspace reconciled");

    Fixture exceptionFixture = CreateFixture("cleanup-preparation-error");
    FaultingWorkspaceService exceptionWorkspace = new(cleanupResult: false);
    QuickBmsExportService exceptionService = CreateService(
        new FakeRunner(exceptionFixture.SourcePath),
        exceptionWorkspace,
        new QuickBmsExportTestHooks
        {
            WorkspaceCreated = () => throw new IOException("injected preparation failure")
        });
    QuickBmsExportPreparationException preparationException =
        await CapturePreparationException(() => exceptionService.PrepareAsync(
            exceptionFixture.SourcePath,
            exceptionFixture.Identity,
            exceptionFixture.Options));
    Check(!preparationException.WasCancelled &&
          !preparationException.StagingCleaned,
        "unexpected preparation failure preserves cleanup result");
    exceptionWorkspace.CleanupResult = true;
    Check(exceptionWorkspace.TryClean(exceptionWorkspace.LastWorkspace!),
        "failed preparation retained workspace reconciled");
}

async Task TestCompletedProgressFailureAsync()
{
    Fixture fixture = CreateFixture("completed-progress-failure");
    FakeRunner runner = new(fixture.SourcePath);
    QuickBmsExportService service = CreateService(runner);
    QuickBmsExportResult result = await service.ExportAsync(
        await service.PrepareAsync(
            fixture.SourcePath,
            fixture.Identity,
            fixture.Options),
        new DirectProgress<QuickBmsExportStage>(stage =>
        {
            if (stage == QuickBmsExportStage.Completed)
                throw new InvalidOperationException("injected completed observer failure");
        }));
    Check(result.Outcome == QuickBmsExportOutcome.Success,
        "Completed observer failure cannot rewrite verified Success");
    Check(result.SourceFingerprint == result.StagedFingerprint &&
          result.StagedFingerprint == result.VerificationFingerprint,
        "Completed observer failure preserves exact verified fingerprints");
    Check(result.StagingCleaned && runner.Requests.Count == 2,
        "Completed observer failure cleans and performs one write and verification");
    Check(result.DiagnosticDetail.Contains(
            "completed progress notification failed",
            StringComparison.OrdinalIgnoreCase),
        "Completed observer failure remains secondary diagnostic detail");

    Fixture cleanupFixture = CreateFixture("completed-progress-cleanup-failure");
    FakeRunner cleanupRunner = new(cleanupFixture.SourcePath);
    FaultingWorkspaceService cleanupWorkspace = new(cleanupResult: false);
    QuickBmsExportService cleanupService = CreateService(
        cleanupRunner,
        cleanupWorkspace);
    QuickBmsExportResult cleanupResult = await cleanupService.ExportAsync(
        await cleanupService.PrepareAsync(
            cleanupFixture.SourcePath,
            cleanupFixture.Identity,
            cleanupFixture.Options),
        new DirectProgress<QuickBmsExportStage>(stage =>
        {
            if (stage == QuickBmsExportStage.Completed)
                throw new InvalidOperationException("injected completed observer failure");
        }));
    Check(cleanupResult.Outcome == QuickBmsExportOutcome.Success &&
          !cleanupResult.StagingCleaned,
        "Completed observer and cleanup failures preserve Success as primary");
    Check(cleanupRunner.Requests.Count == 2 &&
          !string.IsNullOrWhiteSpace(cleanupResult.PreservedWorkspacePath),
        "Completed observer cleanup failure does not retry transport");
    cleanupWorkspace.CleanupResult = true;
    Check(cleanupWorkspace.TryClean(cleanupWorkspace.LastWorkspace!),
        "Completed observer retained workspace remains safely reconcilable");
}

async Task TestPartialWorkspaceCreationFailuresAsync()
{
    string sessionRoot = Path.Combine(
        testRoot,
        "partial-workspace",
        "session-clean");
    IOException sessionPrimary = new("injected post-session failure");
    QuickBmsExportWorkspaceService sessionService = new(
        new ExtractionWorkspaceService(),
        new QuickBmsExportWorkspaceTestHooks
        {
            AfterSessionCreated = () => throw sessionPrimary
        });
    Exception sessionException = CaptureException(() =>
        sessionService.Create(sessionRoot));
    Check(ReferenceEquals(sessionException, sessionPrimary) &&
          (!Directory.Exists(sessionRoot) ||
           Directory.GetFileSystemEntries(sessionRoot).Length == 0),
        "post-session creation failure preserves primary error and cleans partial state");

    CheckWorkspaceCreationFailure(
        "marker-clean",
        cleanupResult: true,
        afterMarker: true);
    _ = CheckWorkspaceCreationFailure(
        "marker-retained",
        cleanupResult: false,
        afterMarker: true);
    CheckWorkspaceCreationFailure(
        "modded-clean",
        cleanupResult: true,
        afterMarker: false);
    _ = CheckWorkspaceCreationFailure(
        "modded-retained",
        cleanupResult: false,
        afterMarker: false);

    Fixture fixture = CreateFixture("partial-workspace-service-propagation");
    QuickBmsExportWorkspaceService failingWorkspace = new(
        new ExtractionWorkspaceService(),
        new QuickBmsExportWorkspaceTestHooks
        {
            AfterMarkerCreated = () =>
                throw new IOException("injected service creation failure"),
            TryClean = _ => false
        });
    QuickBmsExportService export = CreateService(
        new FakeRunner(fixture.SourcePath),
        failingWorkspace);
    QuickBmsExportPreparationException exception =
        await CapturePreparationException(() => export.PrepareAsync(
            fixture.SourcePath,
            fixture.Identity,
            fixture.Options));
    Check(exception.InnerException is QuickBmsExportWorkspaceCreationException &&
          !exception.StagingCleaned &&
          Directory.Exists(exception.PreservedWorkspacePath),
        "partial workspace cleanup failure reaches preparation warning contract");
    string sharedRoot = Path.GetDirectoryName(exception.PreservedWorkspacePath)!;
    QuickBmsExportWorkspaceService reconcile = new();
    QuickBmsExportWorkspace next = reconcile.Create(sharedRoot);
    Check(!Directory.Exists(exception.PreservedWorkspacePath) &&
          Directory.GetDirectories(sharedRoot).Length == 1,
        "next Export workspace reconciles structured partial-creation residue");
    Check(reconcile.TryClean(next),
        "structured partial-creation retry leaves no accumulated session");
}

string CheckWorkspaceCreationFailure(
    string name,
    bool cleanupResult,
    bool afterMarker)
{
    string root = Path.Combine(testRoot, "partial-workspace", name);
    IOException primary = new($"injected {name} initialization failure");
    QuickBmsExportWorkspaceTestHooks hooks = new()
    {
        AfterMarkerCreated = afterMarker
            ? () => throw primary
            : null,
        AfterModdedCreated = afterMarker
            ? null
            : () => throw primary,
        TryClean = cleanupResult
            ? null
            : _ => false
    };
    QuickBmsExportWorkspaceService service = new(
        new ExtractionWorkspaceService(),
        hooks);

    if (cleanupResult)
    {
        Exception exception = CaptureException(() => service.Create(root));
        Check(ReferenceEquals(exception, primary),
            $"{name} preserves primary creation error when cleanup succeeds");
        Check(!Directory.Exists(root) ||
              Directory.GetFileSystemEntries(root).Length == 0,
            $"{name} cleanup success leaves no session");
        return string.Empty;
    }

    Exception captured = CaptureException(() => service.Create(root));
    Check(captured is QuickBmsExportWorkspaceCreationException creation &&
          ReferenceEquals(creation.InnerException, primary) &&
          !creation.StagingCleaned &&
          Directory.Exists(creation.PreservedWorkspacePath),
        $"{name} preserves primary error and independent cleanup failure");
    string retained =
        ((QuickBmsExportWorkspaceCreationException)captured).PreservedWorkspacePath;
    QuickBmsExportWorkspaceService retry = new();
    QuickBmsExportWorkspace replacement = retry.Create(root);
    Check(!Directory.Exists(retained) &&
          Directory.GetDirectories(root).Length == 1,
        $"{name} next creation reconciles one retained safe session");
    Check(retry.TryClean(replacement),
        $"{name} replacement workspace cleans without accumulation");
    return retained;
}

Exception CaptureException(Action action)
{
    try
    {
        action();
    }
    catch (Exception exception)
    {
        return exception;
    }

    throw new InvalidOperationException("FAILED: expected exception");
}

async Task CheckCleanupFailureOutcome(
    string name,
    FakeRunnerFactory factory,
    QuickBmsExportOutcome expected,
    string checkName)
{
    Fixture fixture = CreateFixture(name);
    FaultingWorkspaceService workspace = new(cleanupResult: false);
    FakeRunner runner = factory.Create(fixture.SourcePath);
    QuickBmsExportService service = CreateService(runner, workspace);
    QuickBmsExportResult result = await service.ExportAsync(
        await service.PrepareAsync(
            fixture.SourcePath,
            fixture.Identity,
            fixture.Options));
    Check(result.Outcome == expected && !result.StagingCleaned,
        checkName);
    Check(!string.IsNullOrWhiteSpace(result.PreservedWorkspacePath),
        $"{name} preserves diagnostic workspace");
    workspace.CleanupResult = true;
    Check(workspace.TryClean(workspace.LastWorkspace!),
        $"{name} retained workspace can be reconciled");
}

async Task<QuickBmsExportPreparationException> CapturePreparationException(
    Func<Task<QuickBmsExportPreparation>> action)
{
    try
    {
        _ = await action();
    }
    catch (QuickBmsExportPreparationException exception)
    {
        return exception;
    }

    throw new InvalidOperationException(
        "FAILED: expected preparation exception");
}

void TestSourceIdentityFailures()
{
    Fixture fixture = CreateFixture("identity");
    QuickBmsExportService service = CreateService(new FakeRunner(fixture.SourcePath));
    CheckThrowsAsync(
        () => service.PrepareAsync(
            fixture.SourcePath,
            "sha256:" + new string('0', 64),
            fixture.Options),
        "disk/current identity mismatch rejected");
    File.Delete(fixture.SourcePath);
    CheckThrowsAsync(
        () => service.PrepareAsync(
            fixture.SourcePath,
            fixture.Identity,
            fixture.Options),
        "missing persisted source rejected");

    Fixture toolFixture = CreateFixture("wrong-tool");
    string wrongTool = Path.Combine(toolFixture.Tool, "quickbms_4gb_files.exe");
    File.Move(toolFixture.Options.QuickBmsExecutablePath, wrongTool);
    QuickBmsImportOptions wrongOptions = new()
    {
        WartalesInstallationDirectory = toolFixture.Install,
        QuickBmsExecutablePath = wrongTool,
        ShiroScriptPath = toolFixture.Options.ShiroScriptPath,
        ProcessTimeout = TimeSpan.FromSeconds(5)
    };
    CheckThrowsAsync(
        () => service.PrepareAsync(
            toolFixture.SourcePath,
            toolFixture.Identity,
            wrongOptions),
        "4GB executable rejected");
}

async Task RunMainViewModelBehavioralTestsAsync()
{
    TaskCompletionSource completion = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    Thread thread = new(() =>
    {
        Application application = new()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        application.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(
                    "pack://application:,,,/WartalesEditor;component/Resources/SharedUiResources.xaml")
            });
        Window owner = new()
        {
            Width = 640,
            Height = 480,
            ShowInTaskbar = false
        };
        application.MainWindow = owner;
        owner.Show();

        application.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await TestMainViewModelBehaviorAsync(owner);
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
            finally
            {
                foreach (Window window in
                         application.Windows.Cast<Window>().ToArray())
                {
                    window.Close();
                }
                application.Shutdown();
            }
        });

        application.Run();
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    await completion.Task;
    thread.Join();
}

async Task TestMainViewModelBehaviorAsync(Window owner)
{
    string uiRoot = Path.Combine(testRoot, "main-view-model");
    Directory.CreateDirectory(uiRoot);

    await TestImportBlocksExportAsync(uiRoot);
    await TestOwnerResolutionFailureAsync(owner, uiRoot);
    await TestMainWindowClientCoverageAsync(owner, uiRoot);
    await TestActualMainWindowCloseLifecycleAsync(owner, uiRoot);
    await TestCompleteStateNeutralityAsync(uiRoot);

    string manualDirectory =
        Path.Combine(uiRoot, "manual-export");
    Directory.CreateDirectory(manualDirectory);
    string manualSource =
        Path.Combine(manualDirectory, "source.cdb");
    File.WriteAllText(
        manualSource,
        BaseJson(3),
        new UTF8Encoding(false));
    string manualInstallation =
        CreateValidWartalesInstallation(
            Path.Combine(manualDirectory, "Selected Wartales"));
    UiFileDialogs manualDialogs = new()
    {
        FolderName = manualInstallation
    };
    UiMessages manualMessages = new();
    JsonDataService manualJson = new();
    MainViewModel manualViewModel = CreateMainViewModel(
        manualJson,
        manualDialogs,
        manualMessages,
        Path.Combine(manualDirectory, "language"),
        new QuickBmsImportOptions
        {
            WartalesInstallationDirectory =
                Path.Combine(manualDirectory, "Missing Wartales")
        });
    manualViewModel.PromoteLoadedProject(
        manualJson.LoadProject(manualSource),
        manualSource);
    UiExportService manualExport = new(manualDirectory);
    manualViewModel.UseQuickBmsExportServiceForTesting(manualExport);
    await manualViewModel.ExportBackToWartalesAsync();
    Check(manualDialogs.FolderCount == 1
          && manualExport.PrepareCount == 1
          && manualExport.ExportCount == 1
          && manualExport.LastWartalesInstallationDirectory ==
             Path.GetFullPath(manualInstallation)
          && manualExport.LastPreparedPackagePath ==
             Path.Combine(manualInstallation, "res.pak"),
        "valid manual selection continues the same Export action with the exact selected root");

    string invalidInstallation = Path.Combine(
        manualDirectory,
        "Invalid Wartales");
    Directory.CreateDirectory(invalidInstallation);
    UiFileDialogs resolutionFailureDialogs = new()
    {
        FolderName = invalidInstallation
    };
    UiMessages resolutionFailureMessages = new();
    MainViewModel resolutionFailureViewModel = CreateMainViewModel(
        manualJson,
        resolutionFailureDialogs,
        resolutionFailureMessages,
        Path.Combine(manualDirectory, "failure-language"),
        new QuickBmsImportOptions
        {
            WartalesInstallationDirectory =
                Path.Combine(manualDirectory, "Missing Failure Wartales")
        });
    resolutionFailureViewModel.PromoteLoadedProject(
        manualJson.LoadProject(manualSource),
        manualSource);
    UiExportService resolutionFailureExport = new(manualDirectory);
    resolutionFailureViewModel.UseQuickBmsExportServiceForTesting(
        resolutionFailureExport);
    await resolutionFailureViewModel.ExportBackToWartalesAsync();
    Check(resolutionFailureDialogs.FolderCount == 1
          && resolutionFailureMessages.Errors.Count == 1
          && resolutionFailureExport.PrepareCount == 0
          && resolutionFailureExport.ExportCount == 0
          && resolutionFailureMessages.ConfirmationCount == 0
          && resolutionFailureViewModel.Status ==
             "The Wartales installation location could not be resolved.",
        "Export installation-resolution failure presents one error and performs no export work");

    UiFileDialogs resolutionCancelDialogs = new();
    UiMessages resolutionCancelMessages = new();
    MainViewModel resolutionCancelViewModel = CreateMainViewModel(
        manualJson,
        resolutionCancelDialogs,
        resolutionCancelMessages,
        Path.Combine(manualDirectory, "cancel-language"),
        new QuickBmsImportOptions
        {
            WartalesInstallationDirectory =
                Path.Combine(manualDirectory, "Missing Cancel Wartales")
        });
    resolutionCancelViewModel.PromoteLoadedProject(
        manualJson.LoadProject(manualSource),
        manualSource);
    UiExportService resolutionCancelExport = new(manualDirectory);
    resolutionCancelViewModel.UseQuickBmsExportServiceForTesting(
        resolutionCancelExport);
    await resolutionCancelViewModel.ExportBackToWartalesAsync();
    Check(resolutionCancelDialogs.FolderCount == 1
          && resolutionCancelMessages.Errors.Count == 0
          && resolutionCancelExport.PrepareCount == 0
          && resolutionCancelExport.ExportCount == 0
          && resolutionCancelMessages.ConfirmationCount == 0
          && resolutionCancelViewModel.Status ==
             "Export Back to Wartales cancelled.",
        "Export installation selection cancellation is silent and performs no export work");

    // Dirty CDB and missing-durable-path Save success.
    UiFixture dirty = CreateUiFixture(uiRoot, "dirty-save");
    ProjectTransportState? dirtyBeforeTransport = null;
    dirty.Export.PreparationStarted = () =>
        dirtyBeforeTransport = CaptureProjectTransportState(dirty.Project);
    dirty.Project.FileName = string.Empty;
    dirty.ViewModel.CurrentFile = string.Empty;
    dirty.Project.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 9L;
    await dirty.ViewModel.ExportBackToWartalesAsync();
    Check(dirty.Dialogs.SaveCount == 1 && File.Exists(dirty.Dialogs.SaveFileName),
        "dirty project uses normal Save destination workflow");
    Check(dirty.Export.PrepareCount == 1 && dirty.Export.ExportCount == 1,
        "successful Save continues to Export");
    Check(dirty.Messages.ConfirmationCount == 1 &&
          dirty.Export.Events.SequenceEqual(new[] { "prepare", "write" }),
        "confirmation follows completed preparation before write");
    Check(!dirty.ViewModel.IsQuickBmsOperationInProgress &&
          !dirty.ViewModel.IsQuickBmsExportProgressDialogOpen,
        "success restores busy and dialog tracking");
    Check(dirty.Project.SourceProvenanceStatus == SourceProvenanceStatus.Unknown &&
          dirty.Export.ExportCount == 1,
        "unknown provenance remains exportable without a gate");
    Check(dirtyBeforeTransport != null &&
          ProjectTransportStateMatches(dirty.Project, dirtyBeforeTransport),
        "verified transport leaves saved project state neutral");

    // Save cancellation.
    UiFixture cancelledSave = CreateUiFixture(uiRoot, "save-cancel");
    cancelledSave.Project.FileName = string.Empty;
    cancelledSave.ViewModel.CurrentFile = string.Empty;
    cancelledSave.Dialogs.SaveFileName = null;
    cancelledSave.Project.IsModified = true;
    await cancelledSave.ViewModel.ExportBackToWartalesAsync();
    Check(cancelledSave.Dialogs.SaveCount == 1 &&
          cancelledSave.Export.PrepareCount == 0 &&
          cancelledSave.Messages.ConfirmationCount == 0,
        "Save cancellation stops before preparation and confirmation");

    // Save write failure.
    UiFixture saveFailure = CreateUiFixture(uiRoot, "save-write-failure");
    string blocker = Path.Combine(uiRoot, "save-write-failure", "blocker");
    File.WriteAllText(blocker, "file blocks directory creation");
    saveFailure.Dialogs.SaveFileName = Path.Combine(blocker, "data.cdb");
    saveFailure.Project.FileName = string.Empty;
    saveFailure.ViewModel.CurrentFile = string.Empty;
    saveFailure.Project.IsModified = true;
    await saveFailure.ViewModel.ExportBackToWartalesAsync();
    Check(saveFailure.Export.PrepareCount == 0 &&
          saveFailure.Messages.ConfirmationCount == 0 &&
          saveFailure.Messages.Errors.Any(),
        "Save write failure stops Export before QuickBMS");

    // Partial Save failure after CDB commit.
    UiFixture partial = CreateUiFixture(uiRoot, "partial-save");
    string partialDestination = partial.Dialogs.SaveFileName!;
    Directory.CreateDirectory(partialDestination + ".wtstate");
    partial.Project.IsGameplayOperationStateModified = true;
    partial.Project.FileName = string.Empty;
    partial.ViewModel.CurrentFile = string.Empty;
    await partial.ViewModel.ExportBackToWartalesAsync();
    Check(partial.Export.PrepareCount == 0 &&
          partial.Messages.ConfirmationCount == 0 &&
          partial.Project.IsGameplayOperationStateModified,
        "partial Save failure stops Export and retains state dirtiness");

    // Gameplay-state-only Save.
    UiFixture stateOnly = CreateUiFixture(uiRoot, "state-only");
    byte[]? stateSidecarBeforeTransport = null;
    stateOnly.Export.PreparationStarted = () =>
        stateSidecarBeforeTransport = File.ReadAllBytes(
            stateOnly.Project.FileName + ".wtstate");
    stateOnly.Project.IsGameplayOperationStateModified = true;
    await stateOnly.ViewModel.ExportBackToWartalesAsync();
    Check(stateOnly.Dialogs.SaveCount == 1 &&
          !stateOnly.Project.IsGameplayOperationStateModified &&
          stateOnly.Export.PrepareCount == 1,
        "gameplay-state-only dirtiness saves fully before Export");
    Check(File.Exists(stateOnly.Dialogs.SaveFileName + ".wtstate"),
        "gameplay-state sidecar committed before Export");
    Check(stateSidecarBeforeTransport != null &&
          File.ReadAllBytes(stateOnly.Project.FileName + ".wtstate")
              .SequenceEqual(stateSidecarBeforeTransport),
        "Export transport leaves persisted gameplay state bytes unchanged");

    // Validation failure with no Save required.
    UiFixture validation = CreateUiFixture(uiRoot, "validation-failure");
    ProjectModel invalidProject = new()
    {
        FileName = validation.SourcePath
    };
    SheetModel invalidSheet = new() { Name = "invalid" };
    EntryModel invalidEntry = new() { Id = "invalid" };
    invalidEntry.Properties.Add(
        new PropertyModel { Name = "invalid" });
    invalidSheet.Entries.Add(invalidEntry);
    invalidProject.Sheets.Add(invalidSheet);
    validation.ViewModel.Project = invalidProject;
    await validation.ViewModel.ExportBackToWartalesAsync();
    Check(validation.Export.PrepareCount == 0 &&
          validation.Messages.ConfirmationCount == 0 &&
          validation.Messages.Errors.Any(),
        "validation failure stops before preparation and confirmation");

    // Confirmation cancellation and cleanup warning.
    UiFixture confirmationCancel = CreateUiFixture(uiRoot, "confirmation-cancel");
    confirmationCancel.Messages.ConfirmationResult = false;
    confirmationCancel.Export.CancelCleanupResult = false;
    await confirmationCancel.ViewModel.ExportBackToWartalesAsync();
    Check(confirmationCancel.Export.ExportCount == 0 &&
          confirmationCancel.Export.CancelCount == 1,
        "confirmation cancellation launches no write");
    Check(confirmationCancel.Messages.Warnings.Any(message =>
              message.Contains("checked before the next export", StringComparison.Ordinal)),
        "confirmation cancellation surfaces cleanup failure warning");
    confirmationCancel.Export.ForceCleanup();

    UiFixture preparationCleanup = CreateUiFixture(uiRoot, "preparation-cleanup-failure");
    preparationCleanup.Export.PrepareException =
        new QuickBmsExportPreparationException(
            "injected preparation failure",
            false,
            "retained-workspace",
            new IOException("injected"));
    await preparationCleanup.ViewModel.ExportBackToWartalesAsync();
    Check(preparationCleanup.Messages.Errors.Any(message =>
              message.Contains("checked before the next export", StringComparison.Ordinal)),
        "preparation cleanup failure surfaces independent warning");

    await CheckCleanupPresentation(
        CreateUiFixture(uiRoot, "cleanup-success"),
        QuickBmsExportOutcome.Success,
        "verified success");
    await CheckCleanupPresentation(
        CreateUiFixture(uiRoot, "cleanup-write-failure"),
        QuickBmsExportOutcome.QuickBmsFailed,
        "write failure");
    await CheckCleanupPresentation(
        CreateUiFixture(uiRoot, "cleanup-unconfirmed"),
        QuickBmsExportOutcome.ReimportNotConfirmed,
        "unconfirmed reimport");
    await CheckCleanupPresentation(
        CreateUiFixture(uiRoot, "cleanup-verification-failure"),
        QuickBmsExportOutcome.VerificationFailed,
        "verification failure");

    // Success presentation exception preserves known result and state.
    UiFixture presentationSuccess = CreateUiFixture(uiRoot, "presentation-success");
    presentationSuccess.Messages.ThrowOnInformation = true;
    await presentationSuccess.ViewModel.ExportBackToWartalesAsync();
    Check(presentationSuccess.ViewModel.LastQuickBmsExportResultForTesting?.Outcome ==
          QuickBmsExportOutcome.Success,
        "success survives result-presentation exception");
    Check(presentationSuccess.ViewModel.Status.Contains("verified", StringComparison.OrdinalIgnoreCase) &&
          !presentationSuccess.ViewModel.IsQuickBmsOperationInProgress &&
          !presentationSuccess.ViewModel.IsQuickBmsExportProgressDialogOpen,
        "presentation exception preserves success status and UI cleanup");
    Check(presentationSuccess.Messages.AllMessages.All(message =>
              !message.Contains("game files were not changed", StringComparison.OrdinalIgnoreCase)),
        "post-success presentation failure never claims unchanged game files");

    // Known failure presentation exceptions preserve outcomes.
    await CheckPresentationFailure(
        CreateUiFixture(uiRoot, "presentation-write-failure"),
        QuickBmsExportOutcome.QuickBmsFailed,
        "write failure presentation exception");
    await CheckPresentationFailure(
        CreateUiFixture(uiRoot, "presentation-verification-failure"),
        QuickBmsExportOutcome.VerificationFailed,
        "verification failure presentation exception");

    UiFixture neutralFailure = CreateUiFixture(uiRoot, "neutral-verification-failure");
    ProjectTransportState? failureBeforeTransport = null;
    neutralFailure.Export.PreparationStarted = () =>
        failureBeforeTransport = CaptureProjectTransportState(neutralFailure.Project);
    neutralFailure.Export.ResultOutcome = QuickBmsExportOutcome.VerificationFailed;
    await neutralFailure.ViewModel.ExportBackToWartalesAsync();
    Check(failureBeforeTransport != null &&
          ProjectTransportStateMatches(neutralFailure.Project, failureBeforeTransport),
        "verification failure after potential package write leaves project state neutral");

    // Pre-write exception is the only unchanged-files path.
    UiFixture prewrite = CreateUiFixture(uiRoot, "prewrite-failure");
    prewrite.Export.PrepareException = new IOException("injected pre-write failure");
    await prewrite.ViewModel.ExportBackToWartalesAsync();
    Check(prewrite.Export.ExportCount == 0 &&
          prewrite.Messages.Errors.Any(message =>
              message.Contains("game files were not changed", StringComparison.OrdinalIgnoreCase)),
        "pre-write failure truthfully claims game files unchanged");

    // Preparing busy state, reentrancy, progress close, and deferred app close.
    UiFixture preparing = CreateUiFixture(uiRoot, "preparing-close");
    preparing.Export.BlockPreparation = true;
    int closeReady = 0;
    preparing.ViewModel.ApplicationCloseReady += (_, _) =>
    {
        closeReady++;
        throw new InvalidOperationException("injected close-ready callback failure");
    };
    Task preparingTask = preparing.ViewModel.ExportBackToWartalesAsync();
    await WaitUntil(() => preparing.Export.PrepareCount == 1);
    Check(preparing.ViewModel.IsExportInProgress &&
          !preparing.ViewModel.IsImportInProgress &&
          !preparing.ViewModel.ImportFromWartalesCommand.CanExecute(null) &&
          !preparing.ViewModel.ExportBackToWartalesCommand.CanExecute(null),
        "preparing state blocks Import and reentrant Export");
    Task reentrant = preparing.ViewModel.ExportBackToWartalesAsync();
    await reentrant;
    Check(preparing.Export.PrepareCount == 1,
        "reentrant Export starts exactly one workflow");
    Check(preparing.ViewModel.QuickBmsExportProgressDialogForTesting?.IsVisible == true &&
          preparing.ViewModel.QuickBmsExportProgressViewModelForTesting?.CanCancel == true,
        "Preparing dialog visible with Cancel enabled");
    Check(!preparing.ViewModel.ConfirmApplicationClose(),
        "main close during Preparing is initially deferred");
    await preparingTask;
    Check(closeReady == 1 && !preparing.ViewModel.IsQuickBmsOperationInProgress &&
          !preparing.ViewModel.IsQuickBmsExportProgressDialogOpen,
        "Preparing close cancels, cleans, clears state, and retries once");

    // Title close during Preparing requests cancellation.
    UiFixture titleCancel = CreateUiFixture(uiRoot, "title-cancel");
    titleCancel.Export.BlockPreparation = true;
    Task titleTask = titleCancel.ViewModel.ExportBackToWartalesAsync();
    await WaitUntil(() => titleCancel.Export.PrepareCount == 1);
    titleCancel.ViewModel.QuickBmsExportProgressDialogForTesting!.Close();
    await titleTask;
    Check(titleCancel.Export.ExportCount == 0 &&
          !titleCancel.ViewModel.IsQuickBmsExportProgressDialogOpen,
        "progress title close during Preparing cancels without write");

    // Write critical phase.
    UiFixture writing = CreateUiFixture(uiRoot, "writing-close");
    writing.Export.BlockExport = true;
    Task writingTask = writing.ViewModel.ExportBackToWartalesAsync();
    await WaitUntil(() => writing.Export.ExportCount == 1);
    Check(writing.ViewModel.QuickBmsExportProgressViewModelForTesting?.CanCancel == false &&
          !writing.ViewModel.ConfirmApplicationClose(),
        "write phase disables Cancel and blocks main close");
    writing.ViewModel.QuickBmsExportProgressDialogForTesting!.Close();
    Check(writing.ViewModel.QuickBmsExportProgressDialogForTesting!.IsVisible,
        "write phase rejects progress title close");
    writing.Export.ReleaseExport();
    await writingTask;
    Check(writing.Export.ExportCancellationTokens.All(token => !token.CanBeCanceled),
        "active write receives no user cancellation token");

    // Verification critical phase.
    UiFixture verifying = CreateUiFixture(uiRoot, "verifying-close");
    verifying.Export.BlockVerification = true;
    Task verifyingTask = verifying.ViewModel.ExportBackToWartalesAsync();
    await WaitUntil(() => verifying.Export.VerificationBlocked);
    await Dispatcher.Yield();
    Check(verifying.ViewModel.IsExportInProgress &&
          verifying.ViewModel.QuickBmsExportProgressViewModelForTesting?.CanCancel == false &&
          !verifying.ViewModel.ConfirmApplicationClose(),
        "verification phase disables Cancel and blocks main close");
    verifying.ViewModel.QuickBmsExportProgressDialogForTesting!.Close();
    Check(verifying.ViewModel.QuickBmsExportProgressDialogForTesting!.IsVisible,
        "verification phase rejects progress title close");
    verifying.Export.ReleaseVerification();
    await verifyingTask;
    Check(!verifying.ViewModel.IsQuickBmsOperationInProgress &&
          !verifying.ViewModel.IsQuickBmsExportProgressDialogOpen,
        "verification completion restores UI tracking");
}

async Task TestOwnerResolutionFailureAsync(Window owner, string uiRoot)
{
    UiFixture fixture = CreateUiFixture(uiRoot, "owner-resolution-failure");
    int visibleBefore = Application.Current.Windows
        .Cast<Window>()
        .Count(window => window.IsVisible);
    Application.Current.MainWindow = null;
    try
    {
        fixture.ViewModel.ExportBackToWartalesCommand.Execute(null);
        await WaitUntil(() => fixture.Messages.Errors.Count == 1);
        Check(fixture.Export.PrepareCount == 0 &&
              fixture.Export.ExportCount == 0 &&
              fixture.Messages.ConfirmationCount == 0,
            "owner resolution failure launches no preparation, confirmation, or write");
        Check(!fixture.ViewModel.IsExportInProgress &&
              !fixture.ViewModel.IsQuickBmsOperationInProgress &&
              !fixture.ViewModel.IsQuickBmsExportProgressDialogOpen &&
              fixture.ViewModel.QuickBmsExportProgressViewModelForTesting == null,
            "owner resolution failure leaves no busy or dialog tracking state");
        Check(Application.Current.Windows.Cast<Window>()
                  .Count(window => window.IsVisible) == visibleBefore,
            "owner resolution failure creates no visible progress window");
    }
    finally
    {
        Application.Current.MainWindow = owner;
    }
}

async Task TestMainWindowClientCoverageAsync(Window harnessOwner, string uiRoot)
{
    ActualWindowFixture fixture = CreateActualWindowFixture(
        uiRoot,
        "actual-main-client-coverage");
    MainWindow window = fixture.Window;
    Grid root = (Grid)window.Content;
    Menu menu = root.Children.OfType<Menu>().Single();
    ToolBar toolbar = root.Children.OfType<ToolBar>().Single();
    StatusBar statusBar = root.Children.OfType<StatusBar>().Single();

    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    Check(double.IsPositiveInfinity(window.MaxWidth) &&
          double.IsPositiveInfinity(window.MaxHeight),
        "MainWindow retains unconstrained maxima for native maximization");
    Check(root.ActualWidth > 0 && root.ActualHeight > 0 &&
          Math.Abs(menu.ActualWidth - root.ActualWidth) < 1 &&
          Math.Abs(toolbar.ActualWidth - root.ActualWidth) < 1 &&
          Math.Abs(statusBar.ActualWidth - root.ActualWidth) < 1,
        "restored MainWindow root, menu, toolbar, and status bar fill client width");

    double restoredWidth = root.ActualWidth;
    double restoredHeight = root.ActualHeight;
    window.WindowState = WindowState.Maximized;
    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    Check(root.ActualWidth >= restoredWidth &&
          root.ActualHeight >= restoredHeight &&
          Math.Abs(statusBar.ActualWidth - root.ActualWidth) < 1,
        "maximized MainWindow expands full root and status-bar coverage");

    window.WindowState = WindowState.Normal;
    window.Width = 1050;
    window.Height = 650;
    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    double wideWidth = root.ActualWidth;
    double tallHeight = root.ActualHeight;
    window.Width = 850;
    window.Height = 500;
    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    Check(root.ActualWidth < wideWidth && root.ActualHeight < tallHeight &&
          root.ActualWidth > 0 && root.ActualHeight > 0,
        "restored MainWindow responds to wider, narrower, taller, and shorter sizes");

    window.ViewModel.ShowDetailedEditorWorkspaceCommand.Execute(null);
    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    Check(window.ViewModel.IsDetailedEditorWorkspace &&
          root.ActualWidth > 0 && root.ActualHeight > 0,
        "Detailed Editor preserves full client layout after resize transitions");
    window.ViewModel.ShowGameplayToolsWorkspaceCommand.Execute(null);
    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    Check(window.ViewModel.IsGameplayToolsWorkspace &&
          root.ActualWidth > 0 && root.ActualHeight > 0,
        "Gameplay Tools preserves full client layout after resize transitions");

    window.Close();
    await WaitUntil(() => !window.IsVisible);
    Application.Current.MainWindow = harnessOwner;
}

async Task TestActualMainWindowCloseLifecycleAsync(Window harnessOwner, string uiRoot)
{
    ActualWindowFixture preparing = CreateActualWindowFixture(
        uiRoot,
        "actual-main-preparing");
    preparing.Export.BlockPreparation = true;
    int preparingClosing = 0;
    int preparingClosed = 0;
    int closeReady = 0;
    preparing.Window.Closing += (_, _) => preparingClosing++;
    preparing.Window.Closed += (_, _) => preparingClosed++;
    preparing.Window.ViewModel.ApplicationCloseReady += (_, _) =>
    {
        closeReady++;
        throw new InvalidOperationException("injected close-ready subscriber failure");
    };
    Task preparingTask = preparing.Window.ViewModel.ExportBackToWartalesAsync();
    await WaitUntil(() => preparing.Export.PrepareCount == 1);
    preparing.Window.Close();
    Check(preparing.Window.IsVisible && preparingClosing == 1,
        "actual MainWindow first Preparing close is cancelled");
    await preparingTask;
    await WaitUntil(() => preparingClosed == 1);
    Check(preparingClosing == 2 && closeReady == 1 &&
          !preparing.Window.IsVisible &&
          preparing.Export.ExportCount == 0 &&
          !preparing.Window.ViewModel.IsQuickBmsOperationInProgress &&
          !preparing.Window.ViewModel.IsQuickBmsExportProgressDialogOpen,
        "actual MainWindow Preparing close posts exactly one safe retry");
    Application.Current.MainWindow = harnessOwner;

    ActualWindowFixture writing = CreateActualWindowFixture(
        uiRoot,
        "actual-main-writing");
    writing.Export.BlockExport = true;
    int writingClosing = 0;
    int writingCloseReady = 0;
    writing.Window.Closing += (_, _) => writingClosing++;
    writing.Window.ViewModel.ApplicationCloseReady += (_, _) => writingCloseReady++;
    Task writingTask = writing.Window.ViewModel.ExportBackToWartalesAsync();
    await WaitUntil(() => writing.Export.ExportCount == 1);
    writing.Window.Close();
    Check(writing.Window.IsVisible && writingClosing == 1 &&
          writing.Messages.Warnings.Count == 1 &&
          writingCloseReady == 0,
        "actual MainWindow Writing close is rejected without retry");
    writing.Export.ReleaseExport();
    await writingTask;
    writing.Window.Close();
    await WaitUntil(() => !writing.Window.IsVisible);
    Check(writingClosing == 2 && writingCloseReady == 0,
        "actual MainWindow closes normally after Writing completes");
    Application.Current.MainWindow = harnessOwner;

    ActualWindowFixture verifying = CreateActualWindowFixture(
        uiRoot,
        "actual-main-verifying");
    verifying.Export.BlockVerification = true;
    int verifyingClosing = 0;
    int verifyingCloseReady = 0;
    verifying.Window.Closing += (_, _) => verifyingClosing++;
    verifying.Window.ViewModel.ApplicationCloseReady += (_, _) => verifyingCloseReady++;
    Task verifyingTask = verifying.Window.ViewModel.ExportBackToWartalesAsync();
    await WaitUntil(() => verifying.Export.VerificationBlocked);
    await Dispatcher.Yield();
    verifying.Window.Close();
    Check(verifying.Window.IsVisible && verifyingClosing == 1 &&
          verifying.Messages.Warnings.Count == 1 &&
          verifyingCloseReady == 0,
        "actual MainWindow Verifying close is rejected without retry");
    verifying.Export.ReleaseVerification();
    await verifyingTask;
    verifying.Window.Close();
    await WaitUntil(() => !verifying.Window.IsVisible);
    Check(verifyingClosing == 2 && verifyingCloseReady == 0,
        "actual MainWindow closes normally after Verifying completes");
    Application.Current.MainWindow = harnessOwner;
}

ActualWindowFixture CreateActualWindowFixture(string uiRoot, string name)
{
    string directory = Path.Combine(uiRoot, name);
    Directory.CreateDirectory(directory);
    string source = Path.Combine(directory, "source.cdb");
    File.WriteAllText(source, BaseJson(3), new UTF8Encoding(false));
    MainWindow window = new()
    {
        ShowInTaskbar = false,
        WindowStartupLocation = WindowStartupLocation.Manual,
        Left = 40,
        Top = 40
    };
    UiMessages messages = new();
    UiExportService export = new(directory);
    window.ViewModel.UseMessageDialogServiceForTesting(messages);
    window.ViewModel.UseQuickBmsExportServiceForTesting(export);
    window.ViewModel.PromoteLoadedProject(
        new JsonDataService().LoadProject(source),
        source);
    Application.Current.MainWindow = window;
    window.Show();
    return new ActualWindowFixture(window, messages, export);
}

async Task TestCompleteStateNeutralityAsync(string uiRoot)
{
    foreach ((string name, QuickBmsExportOutcome outcome) in new[]
             {
                 ("complete-neutrality-success", QuickBmsExportOutcome.Success),
                 ("complete-neutrality-verification-failure", QuickBmsExportOutcome.VerificationFailed)
             })
    {
        UiFixture fixture = CreateStateNeutralityFixture(uiRoot, name);
        CompleteApplicationState? before = null;
        fixture.Export.ResultOutcome = outcome;
        fixture.Export.PreparationStarted = () =>
            before = CaptureCompleteApplicationState(fixture);

        await fixture.ViewModel.ExportBackToWartalesAsync();

        CompleteApplicationState after = CaptureCompleteApplicationState(fixture);
        Check(before != null && before == after,
            $"{outcome} preserves complete application state after Save boundary");
    }
}

CompleteApplicationState CaptureCompleteApplicationState(UiFixture fixture)
{
    ProjectModel project = fixture.Project;
    UiStateServices state = fixture.StateServices ??
        throw new InvalidOperationException("Complete state services are required.");
    string sidecar = project.FileName + GameplayOperationStatePersistenceService.SidecarExtension;
    GoldenCdbState golden = state.Golden.GetState();
    return new CompleteApplicationState(
        RuntimeHelpers.GetHashCode(project),
        RuntimeHelpers.GetHashCode(project.RootDocument),
        project.FileName,
        project.RootDocument.ToString(Formatting.None),
        SerializeSheetStructure(project),
        project.SourceCdbGenerationIdentity,
        project.CurrentCdbContentIdentity,
        project.SourceProvenanceStatus,
        project.IsModified,
        project.IsGameplayOperationStateModified,
        string.Join("|", project.Sheets.SelectMany(sheet => sheet.Entries)
            .SelectMany(entry => entry.Properties)
            .Select(property => property.EffectivePropertyPath + ":" + property.IsModified)),
        SerializeGameplayStates(project),
        CaptureHistory(state.History),
        CaptureDirectory(state.ProfilesPath),
        CaptureProfileViewState(fixture.ViewModel),
        Convert.ToBase64String(File.ReadAllBytes(state.SnapshotPath)),
        $"{golden.Availability}|{golden.CanonicalPath}|{golden.Identity}|{golden.Message}|{golden.HasCleanupWarning}",
        Convert.ToBase64String(File.ReadAllBytes(state.Golden.GetCanonicalPath())),
        project.UpdateCompatibilityReport == null
            ? 0
            : RuntimeHelpers.GetHashCode(project.UpdateCompatibilityReport),
        JsonConvert.SerializeObject(project.UpdateCompatibilityReport, Formatting.None),
        Convert.ToBase64String(File.ReadAllBytes(sidecar)),
        string.Join("|", project.GameplayOperationStateWarnings),
        string.Join("|", project.ProjectLoadWarnings));
}

string CaptureProfileViewState(MainViewModel viewModel)
{
    ProfileManagerViewModel? manager =
        (ProfileManagerViewModel?)typeof(MainViewModel)
            .GetField("profileManagerViewModel", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(viewModel);
    Window? window = (Window?)typeof(MainViewModel)
        .GetField("profileManagerWindow", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(viewModel);
    return manager == null
        ? $"none|{window == null}"
        : $"open|{manager.SelectedProfile?.FilePath}|{window?.IsVisible}";
}

string SerializeSheetStructure(ProjectModel project) =>
    new JArray(project.Sheets.Select(sheet => new JObject
    {
        ["name"] = sheet.Name,
        ["entries"] = new JArray(sheet.Entries.Select(entry => new JObject
        {
            ["id"] = entry.Id,
            ["sourceReference"] = entry.SourceEntry == null
                ? 0
                : RuntimeHelpers.GetHashCode(entry.SourceEntry),
            ["properties"] = new JArray(entry.Properties.Select(property => new JObject
            {
                ["name"] = property.Name,
                ["path"] = property.EffectivePropertyPath,
                ["value"] = property.SourceProperty?.Value.DeepClone() ??
                    (property.Value == null
                        ? JValue.CreateNull()
                        : JToken.FromObject(property.Value)),
                ["sourceReference"] = property.SourceProperty == null
                    ? 0
                    : RuntimeHelpers.GetHashCode(property.SourceProperty),
                ["modified"] = property.IsModified
            }))
        }))
    })).ToString(Formatting.None);

string SerializeGameplayStates(ProjectModel project)
{
    JArray Serialize(IEnumerable<GameplayOperationStateModel> states) =>
        new(states.Select(state => new JObject
        {
            ["serialized"] = JObject.FromObject(state),
            ["compatible"] = state.IsCompatible,
            ["compatibilityMessage"] = state.CompatibilityMessage,
            ["persistedFingerprint"] = state.PersistedStateFingerprint
        }));

    return new JObject
    {
        ["active"] = Serialize(project.GameplayOperationStates),
        ["historical"] = Serialize(project.HistoricalGameplayOperationStates),
        ["migration"] = project.RequiresGameplayStateManifestMigration,
        ["unverifiedNotice"] = project.RequiresUnverifiedGameplayStateNotice
    }.ToString(Formatting.None);
}

string CaptureHistory(EditHistoryService history)
{
    static string Stack(EditHistoryService service, string fieldName)
    {
        object value = typeof(EditHistoryService)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(service)!;
        return string.Join(",", ((System.Collections.IEnumerable)value)
            .Cast<IEditAction>()
            .Select(action =>
                RuntimeHelpers.GetHashCode(action) + ":" + action.Description));
    }

    return $"{history.CanUndo}|{history.CanRedo}|{history.UndoDescription}|" +
           $"{history.RedoDescription}|{Stack(history, "undoStack")}|" +
           Stack(history, "redoStack");
}

string CaptureDirectory(string directory) => string.Join("|",
    Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => Path.GetRelativePath(directory, path) + ":" +
                        Convert.ToBase64String(File.ReadAllBytes(path))));

async Task TestImportBlocksExportAsync(string uiRoot)
{
    string root = Path.Combine(uiRoot, "import-blocks-export");
    string install = Path.Combine(root, "Wartales");
    string tool = Path.Combine(root, "tool");
    Directory.CreateDirectory(install);
    Directory.CreateDirectory(tool);
    File.WriteAllText(Path.Combine(install, "Wartales.exe"), "fixture");
    File.WriteAllBytes(Path.Combine(install, "res.pak"),
        new byte[] { (byte)'P', (byte)'A', (byte)'K', 0, 1 });
    string executable = Path.Combine(tool, "quickbms.exe");
    string script = Path.Combine(tool, "Shiro_Games_PAK_script.bms");
    File.WriteAllText(executable, "tool");
    File.WriteAllText(script, "script");
    QuickBmsImportOptions options = new()
    {
        WartalesInstallationDirectory = install,
        QuickBmsExecutablePath = executable,
        ShiroScriptPath = script,
        StagingRootDirectory = Path.Combine(root, "staging"),
        ProcessTimeout = TimeSpan.FromSeconds(5)
    };

    string source = Path.Combine(root, "source.cdb");
    File.WriteAllText(source, BaseJson(3), new UTF8Encoding(false));
    JsonDataService json = new();
    UiFileDialogs dialogs = new();
    UiMessages messages = new();
    MainViewModel viewModel = CreateMainViewModel(
        json,
        dialogs,
        messages,
        Path.Combine(root, "language"),
        options);
    viewModel.PromoteLoadedProject(json.LoadProject(source), source);
    UiExportService export = new(root);
    viewModel.UseQuickBmsExportServiceForTesting(export);
    BlockingImportRunner runner = new();
    viewModel.UseQuickBmsImportServiceForTesting(
        new QuickBmsImportService(
            json,
            new WartalesInstallationService(),
            new QuickBmsToolchainService(),
            runner,
            new ExtractionWorkspaceService(),
            new FileFingerprintService()));

    viewModel.ImportFromWartalesCommand.Execute(null);
    await WaitUntil(() => runner.Started);
    Check(viewModel.IsImportInProgress &&
          !viewModel.ExportBackToWartalesCommand.CanExecute(null),
        "active Import disables Export command");
    await viewModel.ExportBackToWartalesAsync();
    Check(export.PrepareCount == 0,
        "active Import rejects direct reentrant Export workflow");
    runner.Release();
    await WaitUntil(() => !viewModel.IsImportInProgress);
    Check(!viewModel.IsQuickBmsOperationInProgress,
        "completed Import restores shared QuickBMS busy state");
}

async Task CheckPresentationFailure(
    UiFixture fixture,
    QuickBmsExportOutcome outcome,
    string name)
{
    fixture.Export.ResultOutcome = outcome;
    fixture.Messages.ThrowOnError = true;
    await fixture.ViewModel.ExportBackToWartalesAsync();
    Check(fixture.ViewModel.LastQuickBmsExportResultForTesting?.Outcome == outcome,
        $"{name} preserves structured outcome");
    Check(!fixture.ViewModel.IsQuickBmsOperationInProgress &&
          !fixture.ViewModel.IsQuickBmsExportProgressDialogOpen,
        $"{name} restores busy and dialog state");
    Check(fixture.Messages.AllMessages.All(message =>
              !message.Contains("game files were not changed", StringComparison.OrdinalIgnoreCase)),
        $"{name} never claims unchanged game files");
}

async Task CheckCleanupPresentation(
    UiFixture fixture,
    QuickBmsExportOutcome outcome,
    string name)
{
    fixture.Export.ResultOutcome = outcome;
    fixture.Export.TransportCleanupResult = false;
    await fixture.ViewModel.ExportBackToWartalesAsync();
    Check(fixture.ViewModel.LastQuickBmsExportResultForTesting?.Outcome == outcome,
        $"{name} remains primary when cleanup fails");
    Check(fixture.Messages.AllMessages.Any(message =>
              message.Contains("checked before the next export", StringComparison.Ordinal)),
        $"{name} surfaces cleanup warning");
    fixture.Export.ForceCleanup();
}

async Task WaitUntil(Func<bool> condition)
{
    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
    while (!condition())
    {
        if (DateTime.UtcNow >= deadline)
            throw new TimeoutException("Timed out waiting for UI test condition.");
        await Task.Delay(10);
    }
}

ProjectTransportState CaptureProjectTransportState(ProjectModel project) => new(
    project.RootDocument,
    project.RootDocument.ToString(Newtonsoft.Json.Formatting.None),
    project.SourceCdbGenerationIdentity,
    project.CurrentCdbContentIdentity,
    project.SourceProvenanceStatus,
    project.IsModified,
    project.IsGameplayOperationStateModified,
    string.Join("|", project.Sheets
        .SelectMany(sheet => sheet.Entries)
        .SelectMany(entry => entry.Properties)
        .Select(property => property.IsModified ? "1" : "0")),
    project.GameplayOperationStates.Count,
    project.HistoricalGameplayOperationStates.Count,
    project.UpdateCompatibilityReport);

bool ProjectTransportStateMatches(
    ProjectModel project,
    ProjectTransportState before) =>
    ReferenceEquals(project.RootDocument, before.RootDocument) &&
    string.Equals(
        project.RootDocument.ToString(Newtonsoft.Json.Formatting.None),
        before.RootJson,
        StringComparison.Ordinal) &&
    string.Equals(project.SourceCdbGenerationIdentity,
        before.SourceGenerationIdentity, StringComparison.Ordinal) &&
    string.Equals(project.CurrentCdbContentIdentity,
        before.CurrentContentIdentity, StringComparison.Ordinal) &&
    project.SourceProvenanceStatus == before.ProvenanceStatus &&
    project.IsModified == before.IsModified &&
    project.IsGameplayOperationStateModified == before.IsGameplayStateModified &&
    string.Equals(
        string.Join("|", project.Sheets
            .SelectMany(sheet => sheet.Entries)
            .SelectMany(entry => entry.Properties)
            .Select(property => property.IsModified ? "1" : "0")),
        before.PropertyModificationFlags,
        StringComparison.Ordinal) &&
    project.GameplayOperationStates.Count == before.GameplayStateCount &&
    project.HistoricalGameplayOperationStates.Count == before.HistoricalStateCount &&
    ReferenceEquals(project.UpdateCompatibilityReport, before.UpdateCompatibilityReport);

UiFixture CreateUiFixture(string root, string name)
{
    string directory = Path.Combine(root, name);
    Directory.CreateDirectory(directory);
    string source = Path.Combine(directory, "source.cdb");
    File.WriteAllText(source, BaseJson(3), new UTF8Encoding(false));
    JsonDataService json = new();
    ProjectModel project = json.LoadProject(source);
    UiFileDialogs dialogs = new()
    {
        SaveFileName = Path.Combine(directory, "saved.cdb")
    };
    UiMessages messages = new();
    MainViewModel viewModel = CreateMainViewModel(
        json,
        dialogs,
        messages,
        Path.Combine(directory, "language"));
    viewModel.PromoteLoadedProject(project, source);
    UiExportService export = new(directory);
    viewModel.UseQuickBmsExportServiceForTesting(export);
    return new UiFixture(
        viewModel,
        project,
        source,
        dialogs,
        messages,
        export);
}

UiFixture CreateStateNeutralityFixture(string root, string name)
{
    string directory = Path.Combine(root, name);
    Directory.CreateDirectory(directory);
    string source = Path.Combine(directory, "source.cdb");
    File.WriteAllText(source, BaseJson(3), new UTF8Encoding(false));
    JsonDataService json = new();
    ProjectModel project = json.LoadProject(source);
    UiFileDialogs dialogs = new()
    {
        SaveFileName = Path.Combine(directory, "saved.cdb")
    };
    UiMessages messages = new();
    EditHistoryService history = new();
    ModificationSnapshotService snapshotService = new();
    ModificationSnapshotWorkflowService snapshotWorkflow = new();
    string profilesPath = Path.Combine(directory, "profiles");
    ModProfileLibraryService profiles = new(
        new ModProfileLibraryPathService(profilesPath),
        new ModProfileSerializationService(),
        new ProfileEffectiveChangeCountService());
    UiStateServices state = new(
        history,
        snapshotService,
        snapshotWorkflow,
        profiles,
        profilesPath,
        Path.Combine(directory, "snapshot.wtsnapshot"),
        new GoldenCdbService(json, Path.Combine(directory, "golden")));
    MainViewModel viewModel = CreateMainViewModel(
        json,
        dialogs,
        messages,
        Path.Combine(directory, "language"),
        stateServices: state);
    viewModel.PromoteLoadedProject(project, source);
    viewModel.UseGoldenCdbServicesForTesting(
        state.Golden,
        new GoldenCdbComparisonService());

    state.Golden.SetFromProject(project);
    new ModificationSnapshotSerializationService().Save(
        new ModificationSnapshotModel
        {
            CreatedAtUtc = DateTimeOffset.UnixEpoch,
            EditorVersion = "neutrality",
            SourceFileName = "source.cdb",
            GameVersion = "test"
        },
        state.SnapshotPath);
    state.Profiles.AddProfile(new ModProfileModel
    {
        Metadata = new ModProfileMetadataModel
        {
            Name = "Neutrality Profile",
            Description = "Must remain byte-identical.",
            Author = "Test",
            CreatedAtUtc = DateTimeOffset.UnixEpoch,
            ModifiedAtUtc = DateTimeOffset.UnixEpoch
        },
        Snapshot = new ModificationSnapshotModel
        {
            CreatedAtUtc = DateTimeOffset.UnixEpoch,
            EditorVersion = "neutrality",
            SourceFileName = "source.cdb",
            GameVersion = "test"
        }
    });
    history.Record(new NoOpEditAction("history-one"));
    history.Record(new NoOpEditAction("history-two"));
    _ = history.Undo();

    project.GameplayOperationStates.Add(CreateState("active", 75, true));
    project.HistoricalGameplayOperationStates.Add(CreateState("historical", 40, false));
    project.GameplayOperationStateWarnings.Add("state warning");
    project.ProjectLoadWarnings.Add("load warning");
    project.SetUpdateCompatibilityReport(new UpdateCompatibilityReport(
        SourceGenerationTransition.ChangedSourceGeneration,
        1,
        1,
        0,
        new[]
        {
            new GameplayCompatibilityAssessment(
                "Neutrality Tool",
                GameplayCompatibilityStatus.PartiallyOutdated,
                "test assessment")
        },
        new[] { "test project warning" },
        "player summary",
        "technical summary"));
    project.IsGameplayOperationStateModified = true;

    UiExportService export = new(directory);
    viewModel.UseQuickBmsExportServiceForTesting(export);
    return new UiFixture(
        viewModel,
        project,
        source,
        dialogs,
        messages,
        export,
        state);
}

GameplayOperationStateModel CreateState(
    string suffix,
    int percentage,
    bool compatible) => new()
{
    OperationType = ProgressionType.Character,
    TargetSheet = "constant",
    TargetEntry = "A-" + suffix,
    TargetPath = "value",
    BaselineArray = new JArray(10, 20, 30),
    AppliedPercentage = percentage,
    BaselineFingerprint = "baseline-" + suffix,
    ExpectedCurrentFingerprint = "current-" + suffix,
    ElementCount = 3,
    ElementShapeFingerprint = "shape-" + suffix,
    ProjectCompatibilityIdentity = "compatibility-" + suffix,
    GameplaySettings = new JObject
    {
        ["mode"] = suffix,
        ["nested"] = new JArray(1, 2)
    },
    IsCompatible = compatible,
    CompatibilityMessage = "compatibility message " + suffix,
    PersistedStateFingerprint = "persisted-" + suffix
};

MainViewModel CreateMainViewModel(
    JsonDataService json,
    IFileDialogService dialogs,
    IMessageDialogService messages,
    string languagePath,
    QuickBmsImportOptions? importOptions = null,
    UiStateServices? stateServices = null)
{
    QuickBmsImportOptions resolvedImportOptions =
        importOptions
        ?? new QuickBmsImportOptions
        {
            WartalesInstallationDirectory =
                CreateValidWartalesInstallation(
                    Path.Combine(languagePath, "Wartales"))
        };
    LocalizationService localization = new();
    ModificationSnapshotWorkflowService snapshotWorkflow =
        stateServices?.SnapshotWorkflow ?? new();
    ModificationSnapshotService snapshotService =
        stateServices?.SnapshotService ?? new();
    EditHistoryService history = stateServices?.History ?? new();
    ModProfileLibraryService profileLibrary =
        stateServices?.Profiles ?? new();
    ProjectMutationService mutation = new();
    ContentCreationService content = new(mutation);
    AddCampFacilitiesOperation addCamp = new(content);
    UpgradeAllEquipmentOperation upgrade = new(content);
    ProjectOperationTransactionService transaction = new();
    ProjectOperationService operations = new(
        new OperationValidatorProvider(),
        transaction);
    ProfileOperationCaptureService capture = new(
        new OperationValidatorProvider(),
        addCamp,
        upgrade);
    ModProfileService profiles = new(
        snapshotService,
        capture);
    return new MainViewModel(
        json,
        new SearchService(),
        localization,
        history,
        snapshotService,
        snapshotWorkflow,
        new ChangeSummaryService(),
        profileLibrary,
        new ModProfileWorkflowService(
            profiles,
            new ModProfileSerializationService(),
            snapshotWorkflow,
            new ProfileOperationResolver(addCamp, upgrade),
            operations,
            transaction),
        ReferenceDataService.Instance,
        new ValidationWorkflowService(new ValidationService(json)),
        new ValidationPresentationService(),
        operations,
        transaction,
        addCamp,
        upgrade,
        dialogs,
        messages,
        new LanguageDataService(localization, languagePath),
        new WartalesInstallationService(
            Path.Combine(
                languagePath,
                "Wartales Location",
                "location.json"),
            () => Array.Empty<string>()),
        resolvedImportOptions);
}

string CreateValidWartalesInstallation(string installationDirectory)
{
    Directory.CreateDirectory(installationDirectory);
    File.WriteAllText(
        Path.Combine(installationDirectory, "Wartales.exe"),
        "fixture executable");
    File.WriteAllBytes(
        Path.Combine(installationDirectory, "res.pak"),
        new byte[] { (byte)'P', (byte)'A', (byte)'K', 0, 1 });
    return Path.GetFullPath(installationDirectory);
}

string BaseJson(object value) =>
    "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{" +
    "\"id\":\"A\",\"arr\":[{\"x\":1}],\"props\":{\"nested\":5}," +
    $"\"value\":{JToken.FromObject(value).ToString(Newtonsoft.Json.Formatting.None)}" +
    "}]}]}";

Fixture CreateFixture(string name)
{
    string root = Path.Combine(testRoot, name);
    string install = Path.Combine(root, "Wartales");
    string tool = Path.Combine(root, "tool");
    Directory.CreateDirectory(install);
    Directory.CreateDirectory(tool);
    File.WriteAllText(Path.Combine(install, "Wartales.exe"), "fixture");
    string package = Path.Combine(install, "res.pak");
    File.WriteAllBytes(package,
        new byte[] { (byte)'P', (byte)'A', (byte)'K', 0, 10, 20, 30 });
    string executable = Path.Combine(tool, "quickbms.exe");
    string script = Path.Combine(tool, "Shiro_Games_PAK_script.bms");
    File.WriteAllText(executable, "test executable");
    File.WriteAllText(script, "test script");
    string source = Path.Combine(root, "project.cdb");
    File.WriteAllBytes(source, Encoding.UTF8.GetBytes(
        "{\"sheets\":[{\"name\":\"test\",\"lines\":[]}]}"));
    string identity = new CdbGenerationIdentityService().Calculate(source);
    return new Fixture(
        install,
        tool,
        source,
        identity,
        new QuickBmsImportOptions
        {
            WartalesInstallationDirectory = install,
            QuickBmsExecutablePath = executable,
            ShiroScriptPath = script,
            StagingRootDirectory = Path.Combine(root, "unused-import-staging"),
            ProcessTimeout = TimeSpan.FromSeconds(5)
        });
}

QuickBmsExportService CreateService(
    FakeRunner runner,
    IQuickBmsExportWorkspaceService? workspace = null,
    QuickBmsExportTestHooks? hooks = null) => new(
        new WartalesInstallationService(),
        new QuickBmsToolchainService(),
        workspace ?? new QuickBmsExportWorkspaceService(),
        runner,
        new FileFingerprintService(),
        new CdbGenerationIdentityService(),
        new QuickBmsReimportOutputParser(),
        hooks);

void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException("FAILED: " + name);
    checks++;
}

void CheckThrows(Action action, string name)
{
    try
    {
        action();
        throw new InvalidOperationException("FAILED: " + name);
    }
    catch (InvalidOperationException exception)
        when (exception.Message.StartsWith("FAILED:", StringComparison.Ordinal))
    {
        throw;
    }
    catch
    {
        checks++;
    }
}

void CheckThrowsAsync(Func<Task> action, string name)
{
    try
    {
        action().GetAwaiter().GetResult();
        throw new InvalidOperationException("FAILED: " + name);
    }
    catch (InvalidOperationException exception)
        when (exception.Message.StartsWith("FAILED:", StringComparison.Ordinal))
    {
        throw;
    }
    catch
    {
        checks++;
    }
}

void CreateDirectoryJunction(string linkPath, string targetPath)
{
    ProcessStartInfo startInfo = new()
    {
        FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    startInfo.ArgumentList.Add("/d");
    startInfo.ArgumentList.Add("/c");
    startInfo.ArgumentList.Add("mklink");
    startInfo.ArgumentList.Add("/J");
    startInfo.ArgumentList.Add(linkPath);
    startInfo.ArgumentList.Add(targetPath);

    using Process process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("The junction helper did not start.");
    string standardError = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"The junction helper failed: {standardError}");
    }
}

sealed record Fixture(
    string Install,
    string Tool,
    string SourcePath,
    string Identity,
    QuickBmsImportOptions Options);

sealed class DirectProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}

sealed class BlockingImportRunner : IExternalProcessRunner
{
    private readonly TaskCompletionSource release = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public bool Started { get; private set; }

    public void Release() => release.TrySetResult();

    public async Task<ExternalProcessResult> RunAsync(
        ExternalProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        Started = true;
        await release.Task.WaitAsync(cancellationToken);
        string outputDirectory = request.Arguments[^1];
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(
            Path.Combine(outputDirectory, "data.cdb"),
            "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"A\",\"value\":3}]}]}",
            new UTF8Encoding(false));
        return new ExternalProcessResult
        {
            Started = true,
            ExitCode = 0,
            ContainedProcessCount = 0
        };
    }
}

sealed class FakeRunner(string sourcePath) : IExternalProcessRunner
{
    public List<ExternalProcessRequest> Requests { get; } = new();
    public List<CancellationToken> CancellationTokens { get; } = new();
    public string WriteOutput { get; init; } = "< 00000000 123 data.cdb";
    public string WriteError { get; init; } = "- 1 files reimported in 0 seconds";
    public int WriteExitCode { get; init; }
    public bool WriteTerminationFailed { get; init; }
    public bool CorruptVerification { get; init; }
    public bool VerificationTerminationFailed { get; init; }
    public bool CorruptPackageAfterWrite { get; init; }
    public bool WriteTimedOut { get; init; }
    public bool VerificationTimedOut { get; init; }

    public Task<ExternalProcessResult> RunAsync(
        ExternalProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        CancellationTokens.Add(cancellationToken);
        bool verification = request.Arguments.Contains("-o");

        if (!verification && CorruptPackageAfterWrite)
        {
            File.WriteAllBytes(
                request.Arguments[6],
                new byte[] { 1, 2, 3, 4, 5 });
        }

        if (verification)
        {
            string output = request.Arguments[^1];
            Directory.CreateDirectory(output);
            byte[] bytes = File.ReadAllBytes(sourcePath);
            if (CorruptVerification)
                bytes[^1] ^= 0x01;
            File.WriteAllBytes(Path.Combine(output, "data.cdb"), bytes);
        }

        return Task.FromResult(new ExternalProcessResult
        {
            Started = true,
            ExitCode = verification ? 0 : WriteExitCode,
            TimedOut = verification
                ? VerificationTimedOut
                : WriteTimedOut,
            TerminationFailed = verification
                ? VerificationTerminationFailed
                : WriteTerminationFailed,
            ContainedProcessCount = 0,
            StandardOutput = verification ? string.Empty : WriteOutput,
            StandardError = verification ? string.Empty : WriteError
        });
    }
}

sealed class FakeRunnerFactory
{
    public int WriteExitCode { get; init; }
    public string WriteError { get; init; } =
        "- 1 files reimported in 0 seconds";
    public bool CorruptVerification { get; init; }

    public FakeRunner Create(string sourcePath) => new(sourcePath)
    {
        WriteExitCode = WriteExitCode,
        WriteError = WriteError,
        CorruptVerification = CorruptVerification
    };
}

sealed class FaultingWorkspaceService : IQuickBmsExportWorkspaceService
{
    private readonly QuickBmsExportWorkspaceService inner = new();

    public FaultingWorkspaceService(bool cleanupResult)
    {
        CleanupResult = cleanupResult;
    }

    public bool CleanupResult { get; set; }

    public QuickBmsExportWorkspace? LastWorkspace { get; private set; }

    public QuickBmsExportWorkspace Create(string exportRootDirectory)
    {
        LastWorkspace = inner.Create(exportRootDirectory);
        return LastWorkspace;
    }

    public void ValidatePrepared(QuickBmsExportWorkspace exportWorkspace) =>
        inner.ValidatePrepared(exportWorkspace);

    public string CreateVerificationDirectory(
        QuickBmsExportWorkspace exportWorkspace) =>
        inner.CreateVerificationDirectory(exportWorkspace);

    public string ValidateVerificationResult(
        QuickBmsExportWorkspace exportWorkspace) =>
        inner.ValidateVerificationResult(exportWorkspace);

    public bool TryClean(QuickBmsExportWorkspace exportWorkspace) =>
        CleanupResult && inner.TryClean(exportWorkspace);
}

sealed record UiFixture(
    MainViewModel ViewModel,
    ProjectModel Project,
    string SourcePath,
    UiFileDialogs Dialogs,
    UiMessages Messages,
    UiExportService Export,
    UiStateServices? StateServices = null);

sealed record UiStateServices(
    EditHistoryService History,
    ModificationSnapshotService SnapshotService,
    ModificationSnapshotWorkflowService SnapshotWorkflow,
    ModProfileLibraryService Profiles,
    string ProfilesPath,
    string SnapshotPath,
    GoldenCdbService Golden);

sealed record CompleteApplicationState(
    int ProjectReference,
    int RootReference,
    string FileName,
    string RootJson,
    string SheetStructure,
    string? SourceIdentity,
    string CurrentIdentity,
    SourceProvenanceStatus Provenance,
    bool IsModified,
    bool IsGameplayStateModified,
    string PropertyFlags,
    string GameplayState,
    string History,
    string Profiles,
    string ProfileViewState,
    string Snapshot,
    string GoldenState,
    string GoldenBytes,
    int CompatibilityReportReference,
    string CompatibilityReport,
    string StateSidecar,
    string StateWarnings,
    string LoadWarnings);

sealed record ActualWindowFixture(
    MainWindow Window,
    UiMessages Messages,
    UiExportService Export);

sealed class NoOpEditAction(string description) : IEditAction
{
    public string Description { get; } = description;
    public void Undo() { }
    public void Redo() { }
}

sealed record ProjectTransportState(
    JObject RootDocument,
    string RootJson,
    string? SourceGenerationIdentity,
    string CurrentContentIdentity,
    SourceProvenanceStatus ProvenanceStatus,
    bool IsModified,
    bool IsGameplayStateModified,
    string PropertyModificationFlags,
    int GameplayStateCount,
    int HistoricalStateCount,
    UpdateCompatibilityReport? UpdateCompatibilityReport);

sealed class UiFileDialogs : IFileDialogService
{
    public string? SaveFileName { get; set; }
    public string? FolderName { get; set; }
    public int SaveCount { get; private set; }
    public int FolderCount { get; private set; }

    public string? ShowOpenFileDialog(
        string filter,
        string? initialFileName = null) => null;

    public string? ShowSaveFileDialog(
        string filter,
        string? initialFileName = null)
    {
        SaveCount++;
        return SaveFileName;
    }

    public string? ShowOpenFolderDialog(
        string title,
        string? initialDirectory = null)
    {
        FolderCount++;
        return FolderName;
    }
}

sealed class UiMessages : IMessageDialogService
{
    public bool ConfirmationResult { get; set; } = true;
    public bool ThrowOnInformation { get; set; }
    public bool ThrowOnError { get; set; }
    public int ConfirmationCount { get; private set; }
    public List<string> Information { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();
    public IEnumerable<string> AllMessages =>
        Information.Concat(Warnings).Concat(Errors);

    public void ShowInformation(string message, string title)
    {
        Information.Add(message);
        if (ThrowOnInformation)
            throw new InvalidOperationException("injected information presentation failure");
    }

    public void ShowWarning(string message, string title)
    {
        Warnings.Add(message);
    }

    public void ShowError(string message, string title)
    {
        Errors.Add(message);
        if (ThrowOnError)
            throw new InvalidOperationException("injected error presentation failure");
    }

    public bool ShowConfirmation(string message, string title)
    {
        ConfirmationCount++;
        return ConfirmationResult;
    }

    public UnsavedChangesResult ShowUnsavedChanges(
        string message,
        string title) => UnsavedChangesResult.Cancel;
}

sealed class UiExportService : IQuickBmsExportService
{
    private readonly string root;
    private readonly QuickBmsExportWorkspaceService workspaces = new();
    private TaskCompletionSource? preparationGate;
    private TaskCompletionSource? exportGate;
    private TaskCompletionSource? verificationGate;

    public UiExportService(string root)
    {
        this.root = root;
    }

    public int PrepareCount { get; private set; }
    public int ExportCount { get; private set; }
    public int CancelCount { get; private set; }
    public bool CancelCleanupResult { get; set; } = true;
    public bool TransportCleanupResult { get; set; } = true;
    public Exception? PrepareException { get; set; }
    public Action? PreparationStarted { get; set; }
    public QuickBmsExportOutcome ResultOutcome { get; set; } =
        QuickBmsExportOutcome.Success;
    public bool BlockPreparation
    {
        get => preparationGate != null;
        set => preparationGate = value
            ? new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously)
            : null;
    }
    public bool BlockExport
    {
        get => exportGate != null;
        set => exportGate = value
            ? new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously)
            : null;
    }
    public bool BlockVerification
    {
        get => verificationGate != null;
        set => verificationGate = value
            ? new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously)
            : null;
    }
    public bool VerificationBlocked { get; private set; }
    public List<string> Events { get; } = new();
    public List<CancellationToken> ExportCancellationTokens { get; } = new();
    public QuickBmsExportWorkspace? LastWorkspace { get; private set; }
    public string? LastWartalesInstallationDirectory { get; private set; }
    public string? LastPreparedPackagePath { get; private set; }

    public async Task<QuickBmsExportPreparation> PrepareAsync(
        string sourceCdbPath,
        string expectedContentIdentity,
        QuickBmsImportOptions options,
        CancellationToken cancellationToken = default)
    {
        PrepareCount++;
        LastWartalesInstallationDirectory =
            options.WartalesInstallationDirectory;
        Events.Add("prepare");
        PreparationStarted?.Invoke();
        if (PrepareException != null)
            throw PrepareException;
        if (preparationGate != null)
            await preparationGate.Task.WaitAsync(cancellationToken);

        string workspaceRoot = Path.Combine(root, "fake-export-workspaces");
        LastWorkspace = workspaces.Create(workspaceRoot);
        byte[] bytes = File.Exists(sourceCdbPath)
            ? File.ReadAllBytes(sourceCdbPath)
            : Encoding.UTF8.GetBytes(BaseUiJson());
        File.WriteAllBytes(LastWorkspace.StagedCdbPath, bytes);
        FileFingerprint fingerprint =
            new FileFingerprintService().Calculate(bytes);
        LastPreparedPackagePath = Path.Combine(
            options.WartalesInstallationDirectory,
            "res.pak");
        return new QuickBmsExportPreparation(LastWorkspace)
        {
            SourceCdbPath = sourceCdbPath,
            StagedCdbPath = LastWorkspace.StagedCdbPath,
            ModdedDirectory = LastWorkspace.ModdedDirectory,
            VerificationDirectory = LastWorkspace.VerificationDirectory,
            PackagePath = LastPreparedPackagePath,
            QuickBmsExecutablePath = "quickbms.exe",
            ShiroScriptPath = "Shiro_Games_PAK_script.bms",
            SourceFingerprint = fingerprint,
            StagedFingerprint = fingerprint,
            ProcessTimeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<QuickBmsExportResult> ExportAsync(
        QuickBmsExportPreparation preparation,
        IProgress<QuickBmsExportStage>? progress = null)
    {
        ExportCount++;
        Events.Add("write");
        ExportCancellationTokens.Add(CancellationToken.None);
        progress?.Report(QuickBmsExportStage.Exporting);
        if (exportGate != null)
            await exportGate.Task;
        if (verificationGate != null)
        {
            progress?.Report(QuickBmsExportStage.Verifying);
            VerificationBlocked = true;
            await verificationGate.Task;
            VerificationBlocked = false;
        }
        progress?.Report(QuickBmsExportStage.Completed);
        bool cleaned = TransportCleanupResult &&
            workspaces.TryClean(preparation.ExportWorkspace);
        return new QuickBmsExportResult
        {
            Outcome = ResultOutcome,
            SourceFingerprint = preparation.SourceFingerprint,
            StagedFingerprint = preparation.StagedFingerprint,
            VerificationFingerprint = ResultOutcome == QuickBmsExportOutcome.Success
                ? preparation.StagedFingerprint
                : null,
            StagingCleaned = cleaned,
            PreservedWorkspacePath = cleaned
                ? string.Empty
                : preparation.ExportWorkspace.Workspace.DirectoryPath
        };
    }

    public bool TryCancelPreparation(
        QuickBmsExportPreparation preparation)
    {
        CancelCount++;
        return CancelCleanupResult &&
               workspaces.TryClean(preparation.ExportWorkspace);
    }

    public void ReleaseExport() => exportGate?.TrySetResult();

    public void ReleaseVerification() => verificationGate?.TrySetResult();

    public void ForceCleanup()
    {
        if (LastWorkspace != null)
            workspaces.TryClean(LastWorkspace);
    }

    private static string BaseUiJson() =>
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[]}]}";
}
