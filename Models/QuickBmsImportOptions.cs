using System.IO;

namespace WartalesEditor.Models;

public sealed class QuickBmsImportOptions
{
    public string WartalesInstallationDirectory { get; init; } =
        string.Empty;

    public string QuickBmsExecutablePath { get; init; } =
        string.Empty;

    public string ShiroScriptPath { get; init; } =
        string.Empty;

    public string StagingRootDirectory { get; init; } =
        string.Empty;

    public TimeSpan ProcessTimeout { get; init; } =
        TimeSpan.FromMinutes(10);

    public QuickBmsImportOptions WithWartalesInstallationDirectory(
        string installationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationDirectory);

        return new QuickBmsImportOptions
        {
            WartalesInstallationDirectory =
                Path.GetFullPath(installationDirectory),
            QuickBmsExecutablePath = QuickBmsExecutablePath,
            ShiroScriptPath = ShiroScriptPath,
            StagingRootDirectory = StagingRootDirectory,
            ProcessTimeout = ProcessTimeout
        };
    }

    public static QuickBmsImportOptions CreateDefault()
    {
        string quickBmsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory),
                "quickbms");

        string programFilesX86 =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86);

        return new QuickBmsImportOptions
        {
            WartalesInstallationDirectory =
                Path.Combine(
                    programFilesX86,
                    "Steam",
                    "steamapps",
                    "common",
                    "Wartales"),
            QuickBmsExecutablePath =
                Path.Combine(
                    quickBmsDirectory,
                    "quickbms.exe"),
            ShiroScriptPath =
                Path.Combine(
                    quickBmsDirectory,
                    "Shiro_Games_PAK_script.bms"),
            StagingRootDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "WartalesEditor",
                    "QuickBmsImports")
        };
    }
}
