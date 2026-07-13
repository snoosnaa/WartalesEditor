using System;
using System.Collections.Generic;
using System.Linq;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationPreviewResultModel
{
    private readonly IReadOnlyList<
        ModificationPreviewItemModel> items;

    public ModificationPreviewResultModel(
        IEnumerable<ModificationPreviewItemModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        this.items =
            items.ToList().AsReadOnly();
    }

    public IReadOnlyList<ModificationPreviewItemModel>
        Items =>
            items;

    public int TotalCount =>
        Items.Count;

    public int SafeToApplyCount =>
        Items.Count(item =>
            item.Status ==
            ModificationPreviewStatus.SafeToApply);

    public int AlreadyAppliedCount =>
        Items.Count(item =>
            item.Status ==
            ModificationPreviewStatus.AlreadyApplied);

    public int ConflictCount =>
        Items.Count(item =>
            item.Status ==
            ModificationPreviewStatus.Conflict);

    public int NotMatchedCount =>
        Items.Count(item =>
            item.Status ==
            ModificationPreviewStatus.NotMatched);

    public int InvalidSnapshotChangeCount =>
        Items.Count(item =>
            item.Status ==
            ModificationPreviewStatus.InvalidSnapshotChange);

    public bool HasSafeChanges =>
        SafeToApplyCount > 0;

    public bool HasConflicts =>
        ConflictCount > 0;

    public bool HasUnmatchedItems =>
        NotMatchedCount > 0;

    public bool HasInvalidSnapshotChanges =>
        InvalidSnapshotChangeCount > 0;

    public bool CanApplyWithoutConflicts =>
        ConflictCount == 0
        &&
        NotMatchedCount == 0
        &&
        InvalidSnapshotChangeCount == 0;

    public IReadOnlyList<ModificationPreviewItemModel>
        SafeToApplyItems =>
            Items
                .Where(item =>
                    item.Status ==
                    ModificationPreviewStatus.SafeToApply)
                .ToList();

    public IReadOnlyList<ModificationPreviewItemModel>
        ConflictItems =>
            Items
                .Where(item =>
                    item.Status ==
                    ModificationPreviewStatus.Conflict)
                .ToList();

    public IReadOnlyList<ModificationPreviewItemModel>
        UnresolvedItems =>
            Items
                .Where(item =>
                    item.Status ==
                        ModificationPreviewStatus.NotMatched
                    ||
                    item.Status ==
                        ModificationPreviewStatus
                            .InvalidSnapshotChange)
                .ToList();
}