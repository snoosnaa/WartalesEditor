using System.Collections.Generic;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationSnapshotSettingModel
{
    public string Id { get; init; }
        = string.Empty;

    public string Name { get; init; }
        = string.Empty;

    public string DisplayName { get; init; }
        = string.Empty;

    public List<ModificationSnapshotPropertyModel> Properties
    {
        get;
        init;
    } = new();
}