using System;
using System.Collections.Generic;
using System.Linq;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationApplyResultModel
{
    private readonly IReadOnlyList<
        ModificationApplyItemResultModel> items;

    public ModificationApplyResultModel(
        IEnumerable<
            ModificationApplyItemResultModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        this.items =
            items.ToList().AsReadOnly();
    }

    public IReadOnlyList<
        ModificationApplyItemResultModel> Items =>
            items;

    public int TotalCount =>
        Items.Count;

    public int AppliedCount =>
        Items.Count(item =>
            item.Status ==
            ModificationApplyStatus.Applied);

    public int NoChangeRequiredCount =>
        Items.Count(item =>
            item.Status ==
            ModificationApplyStatus
                .NoChangeRequired);

    public int NotMatchedCount =>
        Items.Count(item =>
            item.Status ==
            ModificationApplyStatus.NotMatched);

    public int FailedCount =>
        Items.Count(item =>
            item.Status ==
            ModificationApplyStatus.Failed);

    public int SuccessfulCount =>
        AppliedCount +
        NoChangeRequiredCount;

    public bool HasAppliedChanges =>
        AppliedCount > 0;

    public bool HasFailures =>
        FailedCount > 0;

    public bool HasUnmatchedItems =>
        NotMatchedCount > 0;

    public bool IsCompleteSuccess =>
        TotalCount > 0
        &&
        SuccessfulCount == TotalCount;

    public IReadOnlyList<
        ModificationApplyItemResultModel>
        AppliedItems =>
            Items
                .Where(item =>
                    item.Status ==
                    ModificationApplyStatus.Applied)
                .ToList();

    public IReadOnlyList<
        ModificationApplyItemResultModel>
        UnsuccessfulItems =>
            Items
                .Where(item =>
                    !item.IsSuccessful)
                .ToList();
}