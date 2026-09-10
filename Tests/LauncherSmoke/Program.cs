using System.Diagnostics;
using WartalesEditor.Launcher;

int checks = 0;
string repositoryRoot = FindRepositoryRoot();
string testRoot = Path.Combine(
    Path.GetTempPath(),
    "Wartales Editor Launcher Smoke",
    Guid.NewGuid().ToString("N"));
string originalCurrentDirectory = Directory.GetCurrentDirectory();

try
{
    Directory.CreateDirectory(testRoot);
    VerifyLauncherBehavior();
    VerifyPackagingPathSafety();
    VerifyPackageLayoutValidator();
}
finally
{
    Directory.SetCurrentDirectory(originalCurrentDirectory);
    if (Directory.Exists(testRoot))
    {
        Directory.Delete(testRoot, recursive: true);
    }
}

Console.WriteLine(
    $"Launcher and package-layout smoke checks passed: {checks}");

void VerifyLauncherBehavior()
{
    string packageRoot = Path.Combine(
        testRoot,
        "Package With Spaces Ω測試");
    string appDirectory = Path.Combine(packageRoot, "App");
    string applicationPath =
        Path.Combine(appDirectory, "WartalesEditor.exe");
    Directory.CreateDirectory(appDirectory);
    File.WriteAllText(applicationPath, "test application marker");

    File.WriteAllText(
        Path.Combine(packageRoot, "WartalesEditor.dll"),
        "stale root runtime marker");
    File.WriteAllText(
        Path.Combine(packageRoot, "WartalesEditor.runtimeconfig.json"),
        "stale root configuration marker");

    string unrelatedWorkingDirectory =
        Path.Combine(testRoot, "Unrelated Working Directory");
    Directory.CreateDirectory(unrelatedWorkingDirectory);
    Directory.SetCurrentDirectory(unrelatedWorkingDirectory);

    ProcessStartInfo? captured = null;
    string[] arguments =
    {
        "ordinary",
        string.Empty,
        "value with spaces",
        "quoted\"value",
        "&|<>%"
    };
    int result = LauncherRuntime.Run(
        packageRoot,
        arguments,
        startInfo =>
        {
            captured = startInfo;
            return 37;
        },
        (_, _) => throw new InvalidOperationException(
            "No launch error was expected."));

    Check(result == 37, "real application exit code is propagated");
    Check(captured != null, "valid payload reaches the process-start seam");
    Check(
        string.Equals(
            captured!.FileName,
            Path.GetFullPath(applicationPath),
            StringComparison.OrdinalIgnoreCase),
        "launcher selects only the exact App payload executable");
    Check(
        string.Equals(
            captured.WorkingDirectory,
            Path.GetFullPath(appDirectory),
            StringComparison.OrdinalIgnoreCase),
        "launcher sets the App working directory");
    Check(!captured.UseShellExecute, "launcher disables shell execution");
    Check(
        captured.ArgumentList.SequenceEqual(arguments),
        "empty, Unicode-path, spaced, and special-character arguments remain structured and unchanged");
    Check(
        !captured.FileName.StartsWith(
            Directory.GetCurrentDirectory(),
            StringComparison.OrdinalIgnoreCase),
        "launcher resolution is independent of current working directory");

    captured = null;
    result = LauncherRuntime.Run(
        packageRoot,
        Array.Empty<string>(),
        startInfo =>
        {
            captured = startInfo;
            return 0;
        },
        (_, _) => throw new InvalidOperationException(
            "No launch error was expected."));
    Check(
        result == 0 && captured?.ArgumentList.Count == 0,
        "zero arguments are forwarded as an empty structured list");

    string missingAppRoot = Path.Combine(testRoot, "Missing App");
    Directory.CreateDirectory(missingAppRoot);
    string errorMessage = string.Empty;
    result = LauncherRuntime.Run(
        missingAppRoot,
        Array.Empty<string>(),
        _ => throw new InvalidOperationException(
            "Missing App must not start a process."),
        (message, title) =>
        {
            errorMessage = message;
            Check(
                title == LauncherRuntime.ErrorTitle,
                "missing App error uses the player-facing title");
        });
    Check(
        result == LauncherRuntime.IncompletePackageExitCode &&
        errorMessage.Contains("incomplete or damaged", StringComparison.Ordinal) &&
        errorMessage.Contains(
            Path.Combine(missingAppRoot, "App", "WartalesEditor.exe"),
            StringComparison.OrdinalIgnoreCase),
        "missing App produces a controlled error with the exact expected path");

    string missingExecutableRoot =
        Path.Combine(testRoot, "Missing Executable");
    Directory.CreateDirectory(Path.Combine(missingExecutableRoot, "App"));
    result = LauncherRuntime.Run(
        missingExecutableRoot,
        Array.Empty<string>(),
        _ => throw new InvalidOperationException(
            "Missing executable must not start a process."),
        (_, _) => { });
    Check(
        result == LauncherRuntime.IncompletePackageExitCode,
        "missing payload executable returns the incomplete-package code");

    errorMessage = string.Empty;
    result = LauncherRuntime.Run(
        packageRoot,
        Array.Empty<string>(),
        _ => throw new UnauthorizedAccessException("test access denied"),
        (message, _) => errorMessage = message);
    Check(
        result == LauncherRuntime.ProcessStartFailureExitCode &&
        errorMessage.Contains("test access denied", StringComparison.Ordinal) &&
        !errorMessage.Contains(" at ", StringComparison.Ordinal),
        "process-start failure is player-facing and omits a stack trace");
}

void VerifyPackagingPathSafety()
{
    string helperPath = Path.Combine(
        repositoryRoot,
        "Scripts",
        "PortablePackagePathSafety.ps1");
    string runnerPath = Path.Combine(testRoot, "RunPathSafety.ps1");
    File.WriteAllText(
        runnerPath,
        "param([string] $HelperPath, [string] $RepositoryRoot, " +
        "[string] $OutputDirectory, [string] $RequiredInputPath)\n" +
        ". $HelperPath\n" +
        "$inputs = if ([string]::IsNullOrWhiteSpace($RequiredInputPath)) " +
        "{ @() } else { @($RequiredInputPath) }\n" +
        "$ErrorActionPreference = 'Stop'\n" +
        "Assert-SafePortablePackageOutput " +
        "-RepositoryRoot $RepositoryRoot " +
        "-OutputDirectory $OutputDirectory " +
        "-RequiredInputPaths $inputs | Out-Null\n");

    string ordinaryRepository = Path.Combine(testRoot, "Safe Repository");
    string ordinaryOutput = Path.Combine(
        ordinaryRepository,
        "output",
        "package");
    Directory.CreateDirectory(Path.Combine(ordinaryRepository, "output"));
    ProcessResult ordinary = RunPathSafetyValidator(
        runnerPath,
        helperPath,
        ordinaryRepository,
        ordinaryOutput);
    Check(
        ordinary.ExitCode == 0,
        "ordinary child output passes path-safety validation");

    ProcessResult authorityDeletion = RunPathSafetyValidator(
        runnerPath,
        helperPath,
        ordinaryRepository,
        Path.Combine(ordinaryRepository, "output"));
    Check(
        authorityDeletion.ExitCode != 0,
        "repository output authority cannot be selected for deletion");

    ProcessResult escaped = RunPathSafetyValidator(
        runnerPath,
        helperPath,
        ordinaryRepository,
        Path.Combine(ordinaryRepository, "outside"));
    Check(
        escaped.ExitCode != 0,
        "output paths outside repository output are rejected");

    Directory.CreateDirectory(ordinaryOutput);
    string nestedInput = Path.Combine(ordinaryOutput, "README.pdf");
    File.WriteAllText(nestedInput, "input inside fresh output");
    ProcessResult nestedInputResult = RunPathSafetyValidator(
        runnerPath,
        helperPath,
        ordinaryRepository,
        ordinaryOutput,
        nestedInput);
    Check(
        nestedInputResult.ExitCode != 0,
        "package inputs inside the directory to be recreated are rejected");

    string authorityRepository = Path.Combine(
        testRoot,
        "Authority Junction Repository");
    string authorityTarget = Path.Combine(
        testRoot,
        "Authority Junction Target");
    Directory.CreateDirectory(authorityRepository);
    Directory.CreateDirectory(authorityTarget);
    string authoritySentinel = Path.Combine(authorityTarget, "sentinel.txt");
    File.WriteAllText(authoritySentinel, "must survive");
    string authorityJunction = Path.Combine(authorityRepository, "output");
    CreateDirectoryJunction(authorityJunction, authorityTarget);
    try
    {
        ProcessResult authorityJunctionResult = RunPathSafetyValidator(
            runnerPath,
            helperPath,
            authorityRepository,
            Path.Combine(authorityJunction, "package"));
        Check(
            authorityJunctionResult.ExitCode != 0 &&
            CombinedOutput(authorityJunctionResult).Contains(
                "redirected directory",
                StringComparison.OrdinalIgnoreCase) &&
            File.Exists(authoritySentinel),
            "repository output authority junction is rejected before deletion");
    }
    finally
    {
        Directory.Delete(authorityJunction);
    }

    string descendantRepository = Path.Combine(
        testRoot,
        "Descendant Junction Repository");
    string descendantOutput = Path.Combine(descendantRepository, "output");
    string descendantTarget = Path.Combine(
        testRoot,
        "Descendant Junction Target");
    Directory.CreateDirectory(descendantOutput);
    Directory.CreateDirectory(descendantTarget);
    string descendantSentinel = Path.Combine(descendantTarget, "sentinel.txt");
    File.WriteAllText(descendantSentinel, "must survive");
    string descendantJunction = Path.Combine(descendantOutput, "redirected");
    CreateDirectoryJunction(descendantJunction, descendantTarget);
    try
    {
        ProcessResult descendantJunctionResult = RunPathSafetyValidator(
            runnerPath,
            helperPath,
            descendantRepository,
            Path.Combine(descendantJunction, "package"));
        Check(
            descendantJunctionResult.ExitCode != 0 &&
            CombinedOutput(descendantJunctionResult).Contains(
                "redirected directory",
                StringComparison.OrdinalIgnoreCase) &&
            File.Exists(descendantSentinel),
            "descendant output junction remains rejected before deletion");
    }
    finally
    {
        Directory.Delete(descendantJunction);
    }
}

void VerifyPackageLayoutValidator()
{
    string fixtureRoot = Path.Combine(testRoot, "Layout Fixture");
    string sourcePublish = Path.Combine(fixtureRoot, "Publish");
    string staging = Path.Combine(fixtureRoot, "Staging");
    string app = Path.Combine(staging, "App");
    string launcherPublish = Path.Combine(fixtureRoot, "Launcher Publish");
    Directory.CreateDirectory(sourcePublish);
    Directory.CreateDirectory(app);
    Directory.CreateDirectory(launcherPublish);

    string[] applicationFiles =
    {
        "WartalesEditor.exe",
        "WartalesEditor.dll",
        "WartalesEditor.deps.json",
        "WartalesEditor.runtimeconfig.json",
        "System.Runtime.dll"
    };
    foreach (string file in applicationFiles)
    {
        File.WriteAllText(Path.Combine(sourcePublish, file), file);
        File.WriteAllText(Path.Combine(app, file), file);
    }

    string cultureRelativePath = Path.Combine("fr", "WartalesEditor.resources.dll");
    Directory.CreateDirectory(Path.Combine(sourcePublish, "fr"));
    Directory.CreateDirectory(Path.Combine(app, "fr"));
    File.WriteAllText(
        Path.Combine(sourcePublish, cultureRelativePath),
        "culture payload");
    File.WriteAllText(
        Path.Combine(app, cultureRelativePath),
        "culture payload");
    File.WriteAllText(
        Path.Combine(sourcePublish, "private-symbols.pdb"),
        "excluded symbols");

    foreach (string rootFile in new[]
             {
                 "WartalesEditor.exe",
                 "README.pdf",
                 "USER-GUIDE.pdf",
                 "LICENSE",
                 "THIRD-PARTY-NOTICES.txt",
                 "CHANGELOG.md"
             })
    {
        File.WriteAllText(Path.Combine(staging, rootFile), rootFile);
    }

    string sourceLauncher = Path.Combine(
        launcherPublish,
        "WartalesEditor.Launcher.exe");
    File.WriteAllText(sourceLauncher, "WartalesEditor.exe");

    ProcessResult valid = RunLayoutValidator(
        staging,
        sourcePublish,
        sourceLauncher);
    Check(
        valid.ExitCode == 0,
        "approved package layout passes validation: " +
        valid.StandardError);
    Check(
        valid.StandardOutput.Contains("IsValid", StringComparison.Ordinal),
        "package validator reports a successful result");

    string rootRuntime = Path.Combine(staging, "System.Runtime.dll");
    File.WriteAllText(rootRuntime, "invalid root runtime");
    ProcessResult invalidRoot = RunLayoutValidator(
        staging,
        sourcePublish,
        sourceLauncher);
    Check(
        invalidRoot.ExitCode != 0,
        "root runtime clutter fails package validation");
    File.Delete(rootRuntime);

    string leakedPdb = Path.Combine(app, "leaked.pdb");
    File.WriteAllText(leakedPdb, "invalid public symbols");
    ProcessResult invalidPdb = RunLayoutValidator(
        staging,
        sourcePublish,
        sourceLauncher);
    Check(
        invalidPdb.ExitCode != 0,
        "PDB leakage fails package validation");
    File.Delete(leakedPdb);

    string requiredApplication =
        Path.Combine(app, "WartalesEditor.runtimeconfig.json");
    File.Delete(requiredApplication);
    ProcessResult missingApplication =
        RunLayoutValidator(staging, sourcePublish, sourceLauncher);
    Check(
        missingApplication.ExitCode != 0,
        "missing required App payload fails package validation");
    File.WriteAllText(
        requiredApplication,
        "WartalesEditor.runtimeconfig.json");

    string stagedApplicationAssembly =
        Path.Combine(app, "WartalesEditor.dll");
    File.WriteAllText(
        stagedApplicationAssembly,
        "same name, corrupted content");
    ProcessResult corruptedPayload = RunLayoutValidator(
        staging,
        sourcePublish,
        sourceLauncher);
    Check(
        corruptedPayload.ExitCode != 0 &&
        CombinedOutput(corruptedPayload).Contains(
            "content does not match",
            StringComparison.OrdinalIgnoreCase),
        "same-name App payload corruption fails content validation");
    File.WriteAllText(stagedApplicationAssembly, "WartalesEditor.dll");

    string stagedLauncher = Path.Combine(staging, "WartalesEditor.exe");
    File.WriteAllText(stagedLauncher, "corrupted launcher content");
    ProcessResult corruptedLauncher = RunLayoutValidator(
        staging,
        sourcePublish,
        sourceLauncher);
    Check(
        corruptedLauncher.ExitCode != 0 &&
        CombinedOutput(corruptedLauncher).Contains(
            "launcher content does not match",
            StringComparison.OrdinalIgnoreCase),
        "altered root launcher fails publish-provenance validation");
    File.WriteAllText(stagedLauncher, "WartalesEditor.exe");

    string wrapperDirectory = Path.Combine(staging, "Wrapper");
    Directory.CreateDirectory(wrapperDirectory);
    ProcessResult unexpectedWrapper =
        RunLayoutValidator(staging, sourcePublish, sourceLauncher);
    Check(
        unexpectedWrapper.ExitCode != 0,
        "unexpected outer wrapper directory fails package validation");
}

ProcessResult RunLayoutValidator(
    string stagingDirectory,
    string sourcePublishDirectory,
    string sourceLauncherExecutable)
{
    ProcessStartInfo startInfo = new()
    {
        FileName = "powershell.exe",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    startInfo.ArgumentList.Add("-NoProfile");
    startInfo.ArgumentList.Add("-ExecutionPolicy");
    startInfo.ArgumentList.Add("Bypass");
    startInfo.ArgumentList.Add("-File");
    startInfo.ArgumentList.Add(
        Path.Combine(
            repositoryRoot,
            "Scripts",
            "Test-PortablePackageLayout.ps1"));
    startInfo.ArgumentList.Add("-StagingDirectory");
    startInfo.ArgumentList.Add(stagingDirectory);
    startInfo.ArgumentList.Add("-SourcePublishDirectory");
    startInfo.ArgumentList.Add(sourcePublishDirectory);
    startInfo.ArgumentList.Add("-SourceLauncherExecutable");
    startInfo.ArgumentList.Add(sourceLauncherExecutable);

    using Process process = Process.Start(startInfo)
        ?? throw new InvalidOperationException(
            "PowerShell package validator did not start.");
    string standardOutput = process.StandardOutput.ReadToEnd();
    string standardError = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return new ProcessResult(
        process.ExitCode,
        standardOutput,
        standardError);
}

ProcessResult RunPathSafetyValidator(
    string runnerPath,
    string helperPath,
    string repositoryRootPath,
    string outputDirectory,
    string? requiredInputPath = null)
{
    ProcessStartInfo startInfo = CreatePowerShellStartInfo();
    startInfo.ArgumentList.Add("-File");
    startInfo.ArgumentList.Add(runnerPath);
    startInfo.ArgumentList.Add("-HelperPath");
    startInfo.ArgumentList.Add(helperPath);
    startInfo.ArgumentList.Add("-RepositoryRoot");
    startInfo.ArgumentList.Add(repositoryRootPath);
    startInfo.ArgumentList.Add("-OutputDirectory");
    startInfo.ArgumentList.Add(outputDirectory);
    if (!string.IsNullOrWhiteSpace(requiredInputPath))
    {
        startInfo.ArgumentList.Add("-RequiredInputPath");
        startInfo.ArgumentList.Add(requiredInputPath);
    }

    return RunProcess(startInfo);
}

void CreateDirectoryJunction(string junctionPath, string targetPath)
{
    string junctionRunner = Path.Combine(testRoot, "CreateJunction.ps1");
    File.WriteAllText(
        junctionRunner,
        "param([string] $JunctionPath, [string] $TargetPath)\n" +
        "$ErrorActionPreference = 'Stop'\n" +
        "New-Item -ItemType Junction -Path $JunctionPath " +
        "-Target $TargetPath | Out-Null\n");
    ProcessStartInfo startInfo = CreatePowerShellStartInfo();
    startInfo.ArgumentList.Add("-File");
    startInfo.ArgumentList.Add(junctionRunner);
    startInfo.ArgumentList.Add("-JunctionPath");
    startInfo.ArgumentList.Add(junctionPath);
    startInfo.ArgumentList.Add("-TargetPath");
    startInfo.ArgumentList.Add(targetPath);
    ProcessResult result = RunProcess(startInfo);
    if (result.ExitCode != 0)
    {
        throw new InvalidOperationException(
            "A directory junction could not be created for the required " +
            "path-safety regression: " + CombinedOutput(result));
    }
}

ProcessStartInfo CreatePowerShellStartInfo() =>
    new()
    {
        FileName = "powershell.exe",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

ProcessResult RunProcess(ProcessStartInfo startInfo)
{
    startInfo.ArgumentList.Insert(0, "Bypass");
    startInfo.ArgumentList.Insert(0, "-ExecutionPolicy");
    startInfo.ArgumentList.Insert(0, "-NoProfile");

    using Process process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("PowerShell did not start.");
    string standardOutput = process.StandardOutput.ReadToEnd();
    string standardError = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return new ProcessResult(
        process.ExitCode,
        standardOutput,
        standardError);
}

string CombinedOutput(ProcessResult result) =>
    result.StandardOutput + Environment.NewLine + result.StandardError;

string FindRepositoryRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory != null)
    {
        if (File.Exists(
                Path.Combine(
                    directory.FullName,
                    "Scripts",
                    "Test-PortablePackageLayout.ps1")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException(
        "The repository root could not be located.");
}

void Check(bool condition, string description)
{
    if (!condition)
    {
        throw new InvalidOperationException(
            $"FAILED: {description}");
    }

    checks++;
}

readonly record struct ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
