using System;
using System.Collections.Generic;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationSnapshotModel
{
    public int FormatVersion { get; init; } =
        ModificationSnapshotFormat.CurrentVersion;

    public DateTimeOffset CreatedAtUtc { get; init; }
        = DateTimeOffset.UtcNow;

    public string EditorVersion { get; init; }
        = string.Empty;

    public string SourceFileName { get; init; }
        = string.Empty;

    public string GameVersion { get; init; }
        = string.Empty;

    public string? SourceCdbGenerationIdentity { get; init; }

    public List<ModificationSnapshotCategoryModel> Categories
    {
        get;
        init;
    } = new();

    public List<GameplayOperationStateModel>
        GameplayOperationStates
    {
        get;
        init;
    } = new();
}
