using System.IO;
using System.Text;

namespace WartalesEditor.Services;

public sealed record QuickBmsExportWorkspace(
    ExtractionWorkspace Workspace,
    string MarkerPath,
    string ModdedDirectory,
    string StagedCdbPath,
    string VerificationDirectory);

internal interface IQuickBmsExportWorkspaceService
{
    QuickBmsExportWorkspace Create(string exportRootDirectory);

    void ValidatePrepared(QuickBmsExportWorkspace exportWorkspace);

    string CreateVerificationDirectory(
        QuickBmsExportWorkspace exportWorkspace);

    string ValidateVerificationResult(
        QuickBmsExportWorkspace exportWorkspace);

    bool TryClean(QuickBmsExportWorkspace exportWorkspace);
}

internal sealed class QuickBmsExportWorkspaceCreationException : Exception
{
    public QuickBmsExportWorkspaceCreationException(
        string preservedWorkspacePath,
        Exception innerException)
        : base(
            "The QuickBMS export workspace could not be initialized and could not be removed safely.",
            innerException)
    {
        PreservedWorkspacePath = preservedWorkspacePath;
    }

    public bool StagingCleaned => false;

    public string PreservedWorkspacePath { get; }
}

internal sealed class QuickBmsExportWorkspaceTestHooks
{
    public Action? AfterSessionCreated { get; init; }

    public Action? AfterMarkerCreated { get; init; }

    public Action? AfterModdedCreated { get; init; }

    public Func<ExtractionWorkspace, bool>? TryClean { get; init; }
}

public sealed class QuickBmsExportWorkspaceService :
    IQuickBmsExportWorkspaceService
{
    private const string MarkerName =
        ".wartales-editor-quickbms-export";

    private const string MarkerPrefix =
        "WartalesEditor QuickBMS Export\n1\n";

    private readonly ExtractionWorkspaceService workspaceService;
    private readonly QuickBmsExportWorkspaceTestHooks? testHooks;

    public QuickBmsExportWorkspaceService()
        : this(new ExtractionWorkspaceService(), null)
    {
    }

    public QuickBmsExportWorkspaceService(
        ExtractionWorkspaceService workspaceService)
        : this(workspaceService, null)
    {
    }

    internal QuickBmsExportWorkspaceService(
        ExtractionWorkspaceService workspaceService,
        QuickBmsExportWorkspaceTestHooks? testHooks)
    {
        this.workspaceService = workspaceService ??
            throw new ArgumentNullException(nameof(workspaceService));
        this.testHooks = testHooks;
    }

    public QuickBmsExportWorkspace Create(
        string exportRootDirectory)
    {
        string root = workspaceService.ValidateRoot(
            exportRootDirectory);

        ReconcileStaleSessions(root);

        ExtractionWorkspace workspace =
            workspaceService.Create(root);

        try
        {
            testHooks?.AfterSessionCreated?.Invoke();
            string marker = Path.Combine(
                workspace.DirectoryPath,
                MarkerName);
            File.WriteAllText(
                marker,
                GetMarkerContent(workspace.SessionId),
                new UTF8Encoding(false));
            workspaceService.ValidateContainedRegularFile(
                workspace,
                marker);
            testHooks?.AfterMarkerCreated?.Invoke();

            string modded = Path.Combine(
                workspace.DirectoryPath,
                "Modded");
            Directory.CreateDirectory(modded);
            workspaceService.ValidateContainedRegularDirectory(
                workspace,
                modded);
            testHooks?.AfterModdedCreated?.Invoke();

            return new QuickBmsExportWorkspace(
                workspace,
                marker,
                modded,
                Path.Combine(modded, "data.cdb"),
                Path.Combine(workspace.DirectoryPath, "Verify"));
        }
        catch (Exception exception)
        {
            bool cleaned = TryClean(workspace);

            if (!cleaned)
            {
                throw new QuickBmsExportWorkspaceCreationException(
                    workspace.DirectoryPath,
                    exception);
            }

            throw;
        }
    }

    public void ValidatePrepared(
        QuickBmsExportWorkspace exportWorkspace)
    {
        ArgumentNullException.ThrowIfNull(exportWorkspace);
        ValidateOwnership(exportWorkspace.Workspace);
        workspaceService.ValidateContainedRegularDirectory(
            exportWorkspace.Workspace,
            exportWorkspace.ModdedDirectory);
        workspaceService.ValidateContainedRegularFile(
            exportWorkspace.Workspace,
            exportWorkspace.StagedCdbPath);

        string[] entries = Directory.GetFileSystemEntries(
            exportWorkspace.ModdedDirectory);

        if (entries.Length != 1 ||
            !string.Equals(
                Path.GetFullPath(entries[0]),
                Path.GetFullPath(exportWorkspace.StagedCdbPath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException(
                "The QuickBMS export input folder did not contain exactly data.cdb.");
        }
    }

    public string CreateVerificationDirectory(
        QuickBmsExportWorkspace exportWorkspace)
    {
        ArgumentNullException.ThrowIfNull(exportWorkspace);
        ValidateOwnership(exportWorkspace.Workspace);

        if (Directory.Exists(exportWorkspace.VerificationDirectory) ||
            File.Exists(exportWorkspace.VerificationDirectory))
        {
            throw new IOException(
                "The QuickBMS verification folder was not empty.");
        }

        Directory.CreateDirectory(
            exportWorkspace.VerificationDirectory);
        workspaceService.ValidateContainedRegularDirectory(
            exportWorkspace.Workspace,
            exportWorkspace.VerificationDirectory);

        return exportWorkspace.VerificationDirectory;
    }

    public string ValidateVerificationResult(
        QuickBmsExportWorkspace exportWorkspace)
    {
        ArgumentNullException.ThrowIfNull(exportWorkspace);
        ValidateOwnership(exportWorkspace.Workspace);
        workspaceService.ValidateContainedRegularDirectory(
            exportWorkspace.Workspace,
            exportWorkspace.VerificationDirectory);

        string[] entries = Directory.GetFileSystemEntries(
            exportWorkspace.VerificationDirectory);
        string expected = Path.Combine(
            exportWorkspace.VerificationDirectory,
            "data.cdb");

        if (entries.Length != 1 ||
            !string.Equals(
                Path.GetFullPath(entries[0]),
                Path.GetFullPath(expected),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException(
                "QuickBMS verification did not produce exactly one data.cdb file.");
        }

        workspaceService.ValidateContainedRegularFile(
            exportWorkspace.Workspace,
            expected);

        return expected;
    }

    public bool TryClean(
        QuickBmsExportWorkspace exportWorkspace)
    {
        ArgumentNullException.ThrowIfNull(exportWorkspace);
        return TryClean(exportWorkspace.Workspace);
    }

    private void ReconcileStaleSessions(string root)
    {
        string[] entries = Directory.GetFileSystemEntries(root);
        List<ExtractionWorkspace> staleWorkspaces = new();

        foreach (string entry in entries)
        {
            if (!Directory.Exists(entry))
            {
                throw new IOException(
                    "The QuickBMS export workspace contains an unrecognized item and was not changed.");
            }

            string directoryName = Path.GetFileName(entry);
            if (!Guid.TryParseExact(
                    directoryName,
                    "N",
                    out Guid sessionGuid))
            {
                throw new IOException(
                    "The QuickBMS export workspace contains an unrecognized item and was not changed.");
            }

            string sessionId = sessionGuid.ToString("N");
            ExtractionWorkspace workspace = new(
                sessionId,
                root,
                entry);
            ValidateOwnership(workspace);

            workspaceService.ValidateTreeContainsNoReparsePoint(
                workspace);
            staleWorkspaces.Add(workspace);
        }

        foreach (ExtractionWorkspace workspace in staleWorkspaces)
        {
            if (!workspaceService.TryClean(workspace))
            {
                throw new IOException(
                    "A previous QuickBMS export workspace could not be removed safely.");
            }
        }
    }

    private void ValidateOwnership(
        ExtractionWorkspace workspace)
    {
        workspaceService.ValidateForUse(workspace);
        string marker = Path.Combine(
            workspace.DirectoryPath,
            MarkerName);
        workspaceService.ValidateContainedRegularFile(
            workspace,
            marker);

        string content = File.ReadAllText(marker, Encoding.UTF8);

        if (!string.Equals(
                content,
                GetMarkerContent(workspace.SessionId),
                StringComparison.Ordinal))
        {
            throw new IOException(
                "The QuickBMS export workspace ownership marker was invalid.");
        }
    }

    private static string GetMarkerContent(string sessionId)
    {
        return MarkerPrefix + sessionId + "\n";
    }

    private bool TryClean(ExtractionWorkspace workspace)
    {
        return testHooks?.TryClean?.Invoke(workspace)
            ?? workspaceService.TryClean(workspace);
    }
}
