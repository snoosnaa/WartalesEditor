using System;
using System.Collections.Generic;
using System.Linq;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationMatchResultModel
{
    private readonly IReadOnlyList<
        ModificationMatchItemModel> items;

    public ModificationMatchResultModel(
        IEnumerable<ModificationMatchItemModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        this.items =
            items.ToList().AsReadOnly();
    }

    public IReadOnlyList<ModificationMatchItemModel>
        Items =>
            items;

    public int TotalCount =>
        Items.Count;

    public int MatchedCount =>
        Items.Count(item =>
            item.IsMatched);

    public int UnmatchedCount =>
        TotalCount - MatchedCount;

    public bool HasMatches =>
        MatchedCount > 0;

    public bool HasUnmatchedItems =>
        UnmatchedCount > 0;

    public IReadOnlyList<ModificationMatchItemModel>
        MatchedItems =>
            Items
                .Where(item => item.IsMatched)
                .ToList();

    public IReadOnlyList<ModificationMatchItemModel>
        UnmatchedItems =>
            Items
                .Where(item => !item.IsMatched)
                .ToList();
}