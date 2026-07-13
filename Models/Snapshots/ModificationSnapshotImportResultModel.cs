namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationSnapshotImportResultModel
{
    public ModificationSnapshotImportResultModel(
        ModificationSnapshotModel snapshot,
        ModificationMatchResultModel matchResult,
        ModificationPreviewResultModel previewResult,
        ModificationApplyResultModel applyResult,
        string fileName)
    {
        Snapshot = snapshot;
        MatchResult = matchResult;
        PreviewResult = previewResult;
        ApplyResult = applyResult;
        FileName = fileName;
    }

    public ModificationSnapshotModel Snapshot
    {
        get;
    }

    public ModificationMatchResultModel MatchResult
    {
        get;
    }

    public ModificationPreviewResultModel PreviewResult
    {
        get;
    }

    public ModificationApplyResultModel ApplyResult
    {
        get;
    }

    public string FileName { get; }

    public int TotalCount =>
        PreviewResult.TotalCount;

    public int MatchedCount =>
        MatchResult.MatchedCount;

    public int UnmatchedCount =>
        MatchResult.UnmatchedCount;

    public int SafeToApplyCount =>
        PreviewResult.SafeToApplyCount;

    public int AlreadyAppliedCount =>
        PreviewResult.AlreadyAppliedCount;

    public int ConflictCount =>
        PreviewResult.ConflictCount;

    public int InvalidSnapshotChangeCount =>
        PreviewResult.InvalidSnapshotChangeCount;

    public int AppliedCount =>
        ApplyResult.AppliedCount;

    public int NoChangeRequiredCount =>
        ApplyResult.NoChangeRequiredCount;

    public int FailedCount =>
        ApplyResult.FailedCount;

    public bool HasAppliedChanges =>
        ApplyResult.HasAppliedChanges;

    public bool HasConflicts =>
        PreviewResult.HasConflicts;

    public bool HasUnmatchedItems =>
        MatchResult.HasUnmatchedItems;

    public bool HasFailures =>
        ApplyResult.HasFailures;

    public bool IsCompleteSuccess =>
        ApplyResult.IsCompleteSuccess;
}