using System.Collections.Generic;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationSnapshotCategoryModel
{
    public string Name { get; init; }
        = string.Empty;

    public List<ModificationSnapshotSettingModel> Settings
    {
        get;
        init;
    } = new();
}