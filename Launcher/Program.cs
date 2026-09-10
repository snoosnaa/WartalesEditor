using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WartalesEditor.Launcher;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args) =>
        LauncherRuntime.Run(
            AppContext.BaseDirectory,
            args,
            StartAndWait,
            NativeErrorDialog.Show);

    private static int StartAndWait(ProcessStartInfo startInfo)
    {
        using Process process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Windows did not start Wartales Editor.");

        process.WaitForExit();
        return process.ExitCode;
    }
}

internal static class LauncherRuntime
{
    internal const int IncompletePackageExitCode = 2;
    internal const int ProcessStartFailureExitCode = 3;
    internal const string ErrorTitle = "Wartales Editor";

    internal static int Run(
        string launcherBaseDirectory,
        IReadOnlyList<string> arguments,
        Func<ProcessStartInfo, int> startAndWait,
        Action<string, string> showError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            launcherBaseDirectory);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(startAndWait);
        ArgumentNullException.ThrowIfNull(showError);

        string launcherDirectory =
            Path.GetFullPath(launcherBaseDirectory);
        string applicationDirectory =
            Path.GetFullPath(
                Path.Combine(launcherDirectory, "App"));
        string applicationPath =
            Path.GetFullPath(
                Path.Combine(
                    applicationDirectory,
                    "WartalesEditor.exe"));

        if (!Directory.Exists(applicationDirectory) ||
            !File.Exists(applicationPath))
        {
            showError(
                BuildIncompletePackageMessage(applicationPath),
                ErrorTitle);
            return IncompletePackageExitCode;
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = applicationPath,
            WorkingDirectory = applicationDirectory,
            UseShellExecute = false
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            return startAndWait(startInfo);
        }
        catch (Exception exception)
        {
            showError(
                "Wartales Editor could not be started. The package may be " +
                "incomplete or damaged." + Environment.NewLine +
                Environment.NewLine +
                "Extract a fresh copy of the official ZIP into a new folder " +
                "and try again." + Environment.NewLine +
                Environment.NewLine +
                "Expected application:" + Environment.NewLine +
                applicationPath + Environment.NewLine +
                Environment.NewLine +
                $"Details: {exception.Message}",
                ErrorTitle);
            return ProcessStartFailureExitCode;
        }
    }

    private static string BuildIncompletePackageMessage(
        string applicationPath) =>
        "Wartales Editor could not be started because the package appears " +
        "incomplete or damaged." + Environment.NewLine +
        Environment.NewLine +
        "Extract a fresh copy of the official ZIP into a new folder and try " +
        "again." + Environment.NewLine +
        Environment.NewLine +
        "Expected application:" + Environment.NewLine +
        applicationPath;
}

internal static class NativeErrorDialog
{
    private const uint ErrorIcon = 0x00000010;
    private const uint OkButton = 0x00000000;
    private const uint SetForeground = 0x00010000;

    internal static void Show(string message, string title)
    {
        _ = MessageBox(
            IntPtr.Zero,
            message,
            title,
            OkButton | ErrorIcon | SetForeground);
    }

    [DllImport(
        "user32.dll",
        EntryPoint = "MessageBoxW",
        CharSet = CharSet.Unicode)]
    private static extern int MessageBox(
        IntPtr windowHandle,
        string text,
        string caption,
        uint type);
}
