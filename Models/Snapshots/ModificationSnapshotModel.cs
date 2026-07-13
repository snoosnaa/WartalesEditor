using System;
using System.Collections.Generic;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationSnapshotModel
{
    public int FormatVersion { get; init; } = 1;

    public DateTimeOffset CreatedAtUtc { get; init; }
        = DateTimeOffset.UtcNow;

    public string EditorVersion { get; init; }
        = string.Empty;

    public string SourceFileName { get; init; }
        = string.Empty;

    public string GameVersion { get; init; }
        = string.Empty;

    public List<ModificationSnapshotCategoryModel> Categories
    {
        get;
        init;
    } = new();
}