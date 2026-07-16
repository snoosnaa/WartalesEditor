using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModificationSnapshotWorkflowService
{
    private readonly ModificationSnapshotService
        snapshotService;

    private readonly ModificationSnapshotSerializationService
        serializationService;

    private readonly ModificationSnapshotMatcher
        matcher;

    private readonly ModificationSnapshotPreviewService
        previewService;

    private readonly ModificationSnapshotApplyService
        applyService;

    public ModificationSnapshotWorkflowService()
        : this(
            new ModificationSnapshotService(),
            new ModificationSnapshotSerializationService(),
            new ModificationSnapshotMatcher(),
            new ModificationSnapshotPreviewService(),
            new ModificationSnapshotApplyService())
    {
    }

    public ModificationSnapshotWorkflowService(
        ModificationSnapshotService snapshotService,
        ModificationSnapshotSerializationService
            serializationService,
        ModificationSnapshotMatcher matcher,
        ModificationSnapshotPreviewService previewService,
        ModificationSnapshotApplyService applyService)
    {
        this.snapshotService =
            snapshotService
            ?? throw new ArgumentNullException(
                nameof(snapshotService));

        this.serializationService =
            serializationService
            ?? throw new ArgumentNullException(
                nameof(serializationService));

        this.matcher =
            matcher
            ?? throw new ArgumentNullException(
                nameof(matcher));

        this.previewService =
            previewService
            ?? throw new ArgumentNullException(
                nameof(previewService));

        this.applyService =
            applyService
            ?? throw new ArgumentNullException(
                nameof(applyService));
    }

    public ModificationSnapshotExportResultModel
        Export(
            ProjectModel project,
            string fileName,
            string editorVersion = "")
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A snapshot file name is required.",
                nameof(fileName));
        }

        ModificationSnapshotModel snapshot =
            snapshotService.CreateSnapshot(
                project,
                editorVersion);

        serializationService.Save(
            snapshot,
            fileName);

        return new ModificationSnapshotExportResultModel(
            snapshot,
            fileName);
    }

    public ModificationSnapshotModel Load(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A snapshot file name is required.",
                nameof(fileName));
        }

        return serializationService.Load(
            fileName);
    }

    public ModificationMatchResultModel Match(
        ProjectModel targetProject,
        ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            snapshot);

        return matcher.Match(
            targetProject,
            snapshot);
    }

    public ModificationPreviewResultModel Preview(
        ProjectModel targetProject,
        ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            snapshot);

        ModificationMatchResultModel matchResult =
            matcher.Match(
                targetProject,
                snapshot);

        return previewService.Preview(
            matchResult);
    }

    public ModificationSnapshotImportResultModel
        ApplySafely(
            ProjectModel targetProject,
            ModificationSnapshotModel snapshot,
            string sourceName = "")
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            snapshot);

        ModificationMatchResultModel matchResult =
            matcher.Match(
                targetProject,
                snapshot);

        ModificationPreviewResultModel previewResult =
            previewService.Preview(
                matchResult);

        ModificationApplyResultModel applyResult =
            applyService.Apply(
                previewResult);

        return new ModificationSnapshotImportResultModel(
            snapshot,
            matchResult,
            previewResult,
            applyResult,
            sourceName);
    }

    public ModificationSnapshotImportResultModel
        ImportAndApplySafely(
            ProjectModel targetProject,
            string fileName)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A snapshot file name is required.",
                nameof(fileName));
        }

        ModificationSnapshotModel snapshot =
            serializationService.Load(
                fileName);

        return ApplySafely(
            targetProject,
            snapshot,
            fileName);
    }
}