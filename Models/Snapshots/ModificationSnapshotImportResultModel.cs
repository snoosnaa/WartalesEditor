namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationSnapshotImportResultModel
{
    public ModificationSnapshotImportResultModel(
        ModificationSnapshotModel snapshot,
        ModificationMatchResultModel matchResult,
        ModificationPreviewResultModel previewResult,
        ModificationApplyResultModel applyResult,
        string fileName)
        : this(
            snapshot,
            matchResult,
            previewResult,
            applyResult,
            fileName,
            System.Array.Empty<
                Profiles.ProfileOperationApplyItemResultModel>(),
            applyResult.MutationResult)
    {
    }

    public ModificationSnapshotImportResultModel(
        ModificationSnapshotModel snapshot,
        ModificationMatchResultModel matchResult,
        ModificationPreviewResultModel previewResult,
        ModificationApplyResultModel applyResult,
        string fileName,
        System.Collections.Generic.IReadOnlyList<
            Profiles.ProfileOperationApplyItemResultModel>
                operationResults,
        Services.ProjectMutationResult mutationResult,
        int? effectiveChangeCount = null)
    {
        Snapshot = snapshot;
        MatchResult = matchResult;
        PreviewResult = previewResult;
        ApplyResult = applyResult;
        FileName = fileName;
        OperationResults = operationResults;
        MutationResult = mutationResult;
        EffectiveChangeCount =
            effectiveChangeCount
            ?? TotalCount;
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

    public System.Collections.Generic.IReadOnlyList<
        Profiles.ProfileOperationApplyItemResultModel>
            OperationResults { get; }

    public Services.ProjectMutationResult MutationResult { get; }

    public int EffectiveChangeCount { get; }

    public int AppliedEffectiveChangeCount =>
        System.Linq.Enumerable.Count(
            System.Linq.Enumerable.Distinct(
                System.Linq.Enumerable.Concat(
                    MutationResult.CreatedProperties,
                    MutationResult.UpdatedProperties)));

    public int UnappliedEffectiveChangeCount =>
        UnmatchedCount +
        ConflictCount +
        InvalidSnapshotChangeCount +
        FailedCount +
        OperationsFailedCount;

    public int AlreadyPresentEffectiveChangeCount =>
        System.Math.Max(
            0,
            EffectiveChangeCount -
            AppliedEffectiveChangeCount -
            UnappliedEffectiveChangeCount);

    public int OperationsAppliedCount =>
        System.Linq.Enumerable.Count(
            OperationResults,
            result =>
            result.Status ==
                Profiles.ProfileOperationApplyStatus.Applied);

    public int OperationsAlreadyConfiguredCount =>
        System.Linq.Enumerable.Count(
            OperationResults,
            result =>
            result.Status ==
                Profiles.ProfileOperationApplyStatus
                    .AlreadyConfigured);

    public int OperationsFailedCount =>
        System.Linq.Enumerable.Count(
            OperationResults,
            result =>
            result.Status is
                Profiles.ProfileOperationApplyStatus.Failed
                or
                Profiles.ProfileOperationApplyStatus.Unsupported);

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
