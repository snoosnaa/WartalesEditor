using System.IO;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class QuickBmsImportService
{
    private readonly JsonDataService jsonDataService;
    private readonly WartalesInstallationService installationService;
    private readonly QuickBmsToolchainService toolchainService;
    private readonly IExternalProcessRunner processRunner;
    private readonly ExtractionWorkspaceService workspaceService;
    private readonly FileFingerprintService fingerprintService;
    private readonly CdbGenerationIdentityService identityService;

    public QuickBmsImportService(
        JsonDataService jsonDataService)
        : this(
            jsonDataService,
            new WartalesInstallationService(),
            new QuickBmsToolchainService(),
            new ExternalProcessRunner(),
            new ExtractionWorkspaceService(),
            new FileFingerprintService())
    {
    }

    public QuickBmsImportService(
        JsonDataService jsonDataService,
        WartalesInstallationService installationService,
        QuickBmsToolchainService toolchainService,
        IExternalProcessRunner processRunner,
        ExtractionWorkspaceService workspaceService,
        FileFingerprintService fingerprintService)
    {
        this.jsonDataService =
            jsonDataService
            ?? throw new ArgumentNullException(nameof(jsonDataService));
        this.installationService =
            installationService
            ?? throw new ArgumentNullException(nameof(installationService));
        this.toolchainService =
            toolchainService
            ?? throw new ArgumentNullException(nameof(toolchainService));
        this.processRunner =
            processRunner
            ?? throw new ArgumentNullException(nameof(processRunner));
        this.workspaceService =
            workspaceService
            ?? throw new ArgumentNullException(nameof(workspaceService));
        this.fingerprintService =
            fingerprintService
            ?? throw new ArgumentNullException(nameof(fingerprintService));
        identityService =
            new CdbGenerationIdentityService(fingerprintService);
    }

    public async Task<QuickBmsImportResult> ImportAsync(
        QuickBmsImportOptions options,
        CancellationToken cancellationToken = default)
    {
        return await ImportAsync(
            options,
            replaceExistingExtractedCdb: false,
            cancellationToken);
    }

    public async Task<QuickBmsImportResult> ImportAsync(
        QuickBmsImportOptions options,
        bool replaceExistingExtractedCdb,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        WartalesPackageInfo package =
            installationService.Validate(
                options.WartalesInstallationDirectory);
        string promotedCdbPath =
            GetPromotedCdbPath(
                package.InstallationDirectory);

        if (File.Exists(promotedCdbPath)
            &&
            !replaceExistingExtractedCdb)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ExtractedCdbAlreadyExists,
                "An existing extracted data file was found. Import was cancelled before extraction so the file was not replaced.");
        }

        GameplayStateManifestSnapshot previousState =
            replaceExistingExtractedCdb
                ? jsonDataService.CaptureGameplayStateForReplacement(
                    promotedCdbPath)
                : GameplayStateManifestSnapshot.NoPriorCanonical;

        QuickBmsToolchainInfo toolchain =
            toolchainService.Validate(
                options.QuickBmsExecutablePath,
                options.ShiroScriptPath);

        FileFingerprint packageBefore =
            fingerprintService.Calculate(
                package.PackagePath);
        FileFingerprint executableFingerprint =
            fingerprintService.Calculate(
                toolchain.ExecutablePath);
        FileFingerprint scriptFingerprint =
            fingerprintService.Calculate(
                toolchain.ScriptPath);

        EnsureStagingIsOutsideInstallation(
            options.StagingRootDirectory,
            package.InstallationDirectory);

        ExtractionWorkspace workspace =
            workspaceService.Create(
                options.StagingRootDirectory);
        DateTimeOffset startedUtc =
            DateTimeOffset.UtcNow;
        bool cleanupIsSafe = true;

        try
        {
            workspaceService.ValidateForUse(workspace);

            ExternalProcessResult processResult =
                await processRunner.RunAsync(
                    new ExternalProcessRequest
                    {
                        ExecutablePath = toolchain.ExecutablePath,
                        Arguments = new[]
                        {
                            toolchain.ScriptPath,
                            package.PackagePath,
                            workspace.DirectoryPath
                        },
                        WorkingDirectory =
                            Path.GetDirectoryName(
                                toolchain.ExecutablePath)
                            ?? workspace.DirectoryPath,
                        Timeout = options.ProcessTimeout
                    },
                    cancellationToken);

            if (processResult.TerminationFailed)
            {
                cleanupIsSafe = false;

                throw new QuickBmsImportException(
                    QuickBmsImportFailureKind.ProcessTerminationFailed,
                    "QuickBMS could not be confirmed stopped. No project was loaded, and the temporary extraction folder was left in place for safety.");
            }

            FileFingerprint packageAfter =
                fingerprintService.Calculate(
                    package.PackagePath);

            if (packageBefore != packageAfter)
            {
                throw new QuickBmsImportException(
                    QuickBmsImportFailureKind.SourcePackageChanged,
                    "The Wartales game package changed during import. The extracted data was not loaded.");
            }

            ValidateProcessResult(processResult);

            workspaceService.ValidateForUse(workspace);

            string extractedCdb =
                LocateExtractedCdb(
                    workspace);
            workspaceService.ValidateContainedRegularFile(
                workspace,
                extractedCdb);
            FileFingerprint cdbFingerprint =
                fingerprintService.Calculate(
                    extractedCdb);

            ProjectModel validationProject;

            try
            {
                validationProject =
                    jsonDataService.LoadProject(
                        extractedCdb);

                if (validationProject.Sheets.Count == 0)
                {
                    throw new InvalidDataException(
                        "The extracted data did not contain any project sheets.");
                }
            }
            catch (Exception exception)
            {
                throw new QuickBmsImportException(
                    QuickBmsImportFailureKind.ExtractedCdbInvalid,
                    "The extracted Wartales data could not be opened safely. No project was loaded.",
                    exception);
            }

            ProjectModel project =
                PromoteAndLoadExtractedCdb(
                    extractedCdb,
                    promotedCdbPath,
                    cdbFingerprint,
                    replaceExistingExtractedCdb,
                    previousState);
            FileFingerprint promotedCdbFingerprint =
                fingerprintService.Calculate(
                    promotedCdbPath);

            bool cleaned =
                workspaceService.TryClean(workspace);

            return new QuickBmsImportResult
            {
                Project = project,
                WartalesInstallationDirectory =
                    package.InstallationDirectory,
                SourcePackagePath = package.PackagePath,
                SourcePackageFingerprint = packageBefore,
                QuickBmsExecutablePath = toolchain.ExecutablePath,
                QuickBmsExecutableFingerprint = executableFingerprint,
                ShiroScriptPath = toolchain.ScriptPath,
                ShiroScriptFingerprint = scriptFingerprint,
                StagingDirectory = workspace.DirectoryPath,
                ExtractedCdbPath = promotedCdbPath,
                ExtractedCdbFingerprint = promotedCdbFingerprint,
                ExtractionStartedUtc = startedUtc,
                SessionId = workspace.SessionId,
                ProcessExitCode = processResult.ExitCode,
                ContainedProcessCount =
                    processResult.ContainedProcessCount
                    ?? 0,
                StagingCleaned = cleaned
            };
        }
        catch
        {
            if (cleanupIsSafe)
            {
                workspaceService.TryClean(workspace);
            }

            throw;
        }
    }

    public string GetPromotedCdbPath(
        QuickBmsImportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return GetPromotedCdbPath(
            options.WartalesInstallationDirectory);
    }

    private ProjectModel PromoteAndLoadExtractedCdb(
        string extractedCdb,
        string promotedCdbPath,
        FileFingerprint expectedFingerprint,
        bool replaceExistingExtractedCdb,
        GameplayStateManifestSnapshot previousState)
    {
        string extractedDirectory =
            Path.GetDirectoryName(promotedCdbPath)
            ?? throw new QuickBmsImportException(
                QuickBmsImportFailureKind.PromotionFailed,
                "The extracted Wartales data could not be placed in a durable project folder. No project was loaded.");
        string importingPath =
            promotedCdbPath + ".importing";

        try
        {
            EnsureSafePromotionDirectory(
                extractedDirectory);
            EnsureSafePromotionFile(
                promotedCdbPath,
                allowMissing: true);

            if (File.Exists(promotedCdbPath)
                &&
                !replaceExistingExtractedCdb)
            {
                throw new QuickBmsImportException(
                    QuickBmsImportFailureKind.ExtractedCdbAlreadyExists,
                    "An existing extracted data file was found. Import was cancelled before the file was replaced.");
            }

            PrepareImportingPath(importingPath);
            File.Copy(
                extractedCdb,
                importingPath,
                overwrite: false);
            EnsureSafePromotionFile(
                importingPath,
                allowMissing: false);

            FileFingerprint importingFingerprint =
                fingerprintService.Calculate(
                    importingPath);

            if (importingFingerprint != expectedFingerprint)
            {
                throw new InvalidDataException(
                    "The promoted data did not match the validated extraction.");
            }

            File.Move(
                importingPath,
                promotedCdbPath,
                overwrite: replaceExistingExtractedCdb);

            if (!File.Exists(promotedCdbPath))
            {
                throw new IOException(
                    "The durable extracted data file was not created.");
            }

            EnsureSafePromotionFile(
                promotedCdbPath,
                allowMissing: false);

            FileFingerprint promotedFingerprint =
                fingerprintService.Calculate(
                    promotedCdbPath);

            if (promotedFingerprint != expectedFingerprint)
            {
                throw new InvalidDataException(
                    "The durable extracted data did not match the validated extraction.");
            }

            ProjectModel project =
                jsonDataService.LoadProject(
                    promotedCdbPath);

            if (project.Sheets.Count == 0)
            {
                throw new InvalidDataException(
                    "The durable extracted data did not contain any project sheets.");
            }

            project.FileName = promotedCdbPath;

            string sourceIdentity =
                identityService.Normalize(
                    promotedFingerprint.Sha256);

            jsonDataService.ApplyAuthoritativeImportIdentity(
                project,
                sourceIdentity,
                previousState);

            SourceGenerationTransition transition =
                !previousState.HadPriorCanonical
                    ? SourceGenerationTransition.NoPreviousGeneration
                    : previousState.HasVerifiedSourceProvenance &&
                      identityService.AreEqual(
                          previousState.SourceIdentity,
                          sourceIdentity)
                        ? SourceGenerationTransition.SameSourceGeneration
                        : previousState.HasVerifiedSourceProvenance
                            ? SourceGenerationTransition.ChangedSourceGeneration
                            : SourceGenerationTransition.PreviousSourceGenerationUnknown;

            if (transition is
                SourceGenerationTransition.ChangedSourceGeneration or
                SourceGenerationTransition.PreviousSourceGenerationUnknown)
            {
                project.SetUpdateCompatibilityReport(
                    new UpdateCompatibilityReportService().Create(
                        project,
                        transition));
            }

            jsonDataService.PersistImportedGameplayState(
                project);

            return project;
        }
        catch (QuickBmsImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.PromotionFailed,
                "The extracted Wartales data could not be saved to the game's Extracted folder. No project was loaded. Check folder permissions and try again.",
                exception);
        }
        finally
        {
            TryDeleteImportingFile(importingPath);
        }
    }

    private static string GetPromotedCdbPath(
        string installationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            installationDirectory);

        return Path.Combine(
            Path.GetFullPath(installationDirectory),
            "Extracted",
            "data.cdb");
    }

    private static void EnsureSafePromotionDirectory(
        string directoryPath)
    {
        if (File.Exists(directoryPath))
        {
            throw new IOException(
                "The Extracted project folder path is occupied by a file.");
        }

        Directory.CreateDirectory(directoryPath);
        FileAttributes attributes =
            File.GetAttributes(directoryPath);

        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException(
                "The Extracted project folder cannot be a redirected path.");
        }
    }

    private static void EnsureSafePromotionFile(
        string filePath,
        bool allowMissing)
    {
        if (Directory.Exists(filePath))
        {
            throw new IOException(
                "The extracted project file path is occupied by a folder.");
        }

        if (!File.Exists(filePath))
        {
            if (allowMissing)
            {
                return;
            }

            throw new FileNotFoundException(
                "The extracted project file was not created.",
                filePath);
        }

        FileAttributes attributes =
            File.GetAttributes(filePath);

        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException(
                "The extracted project file cannot be a redirected path.");
        }
    }

    private static void PrepareImportingPath(
        string importingPath)
    {
        EnsureSafePromotionFile(
            importingPath,
            allowMissing: true);

        if (File.Exists(importingPath))
        {
            File.Delete(importingPath);
        }
    }

    private static void TryDeleteImportingFile(
        string importingPath)
    {
        try
        {
            if (File.Exists(importingPath))
            {
                FileAttributes attributes =
                    File.GetAttributes(importingPath);

                if ((attributes & FileAttributes.ReparsePoint) == 0)
                {
                    File.Delete(importingPath);
                }
            }
        }
        catch
        {
        }
    }

    private static void ValidateProcessResult(
        ExternalProcessResult result)
    {
        if (!result.Started)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ProcessStartFailed,
                "QuickBMS could not be started. Check the configured tool files and try again.");
        }

        if (result.TimedOut)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ProcessTimedOut,
                "QuickBMS did not finish within the allowed time. No project was loaded.");
        }

        if (result.Cancelled)
        {
            throw new OperationCanceledException(
                "QuickBMS extraction was cancelled after the process was stopped.");
        }

        if (!string.IsNullOrWhiteSpace(
                result.ExecutionError))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ProcessFailed,
                "QuickBMS could not complete safely. No project was loaded.");
        }

        if (result.ExitCode != 0)
        {
            string details =
                string.IsNullOrWhiteSpace(result.StandardError)
                    ? string.Empty
                    : $" Details: {TrimDetails(result.StandardError)}";

            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ProcessFailed,
                "QuickBMS could not extract the Wartales data. No project was loaded." + details);
        }
    }

    private string LocateExtractedCdb(
        ExtractionWorkspace workspace)
    {
        workspaceService.ValidateForUse(workspace);

        List<string> candidates = new();
        Stack<string> pendingDirectories = new();
        pendingDirectories.Push(
            workspace.DirectoryPath);

        while (pendingDirectories.Count > 0)
        {
            string directory =
                pendingDirectories.Pop();
            FileAttributes directoryAttributes =
                File.GetAttributes(directory);

            if ((directoryAttributes & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }

            foreach (string file in
                     Directory.EnumerateFiles(directory))
            {
                if (!string.Equals(
                        Path.GetFileName(file),
                        "data.cdb",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                FileAttributes attributes =
                    File.GetAttributes(file);

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new QuickBmsImportException(
                        QuickBmsImportFailureKind.ExtractedCdbInvalid,
                        "The extracted Wartales data file was not a safe regular file.");
                }

                workspaceService.ValidateContainedRegularFile(
                    workspace,
                    file);
                candidates.Add(
                    Path.GetFullPath(file));
            }

            foreach (string childDirectory in
                     Directory.EnumerateDirectories(directory))
            {
                FileAttributes attributes =
                    File.GetAttributes(childDirectory);

                if ((attributes & FileAttributes.ReparsePoint) == 0)
                {
                    pendingDirectories.Push(childDirectory);
                }
            }
        }

        if (candidates.Count == 0)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ExtractedCdbMissing,
                "QuickBMS finished, but the expected Wartales data file was not produced. No project was loaded.");
        }

        if (candidates.Count > 1)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ExtractedCdbAmbiguous,
                "QuickBMS produced more than one Wartales data file, so the editor could not choose one safely. No project was loaded.");
        }

        workspaceService.ValidateContainedRegularFile(
            workspace,
            candidates[0]);

        FileInfo candidate = new(candidates[0]);

        if (candidate.Length == 0)
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.ExtractedCdbInvalid,
                "The extracted Wartales data file was empty. No project was loaded.");
        }

        return candidate.FullName;
    }

    private static string TrimDetails(
        string details)
    {
        const int maximumLength = 500;
        string trimmed = details.Trim();

        return trimmed.Length <= maximumLength
            ? trimmed
            : trimmed[^maximumLength..];
    }

    private static void EnsureStagingIsOutsideInstallation(
        string stagingRootDirectory,
        string installationDirectory)
    {
        if (string.IsNullOrWhiteSpace(stagingRootDirectory))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.StagingFailed,
                "A safe temporary extraction folder could not be created.");
        }

        string stagingRoot =
            Path.GetFullPath(stagingRootDirectory)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
        string installation =
            Path.GetFullPath(installationDirectory)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
        string installationPrefix =
            installation + Path.DirectorySeparatorChar;

        if (string.Equals(
                stagingRoot,
                installation,
                StringComparison.OrdinalIgnoreCase)
            ||
            stagingRoot.StartsWith(
                installationPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new QuickBmsImportException(
                QuickBmsImportFailureKind.StagingFailed,
                "The temporary extraction folder must be outside the Wartales installation.");
        }
    }
}
