using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationSnapshotPropertyModel
{
    public string Name { get; init; }
        = string.Empty;

    public string PropertyPath { get; init; }
        = string.Empty;

    public JToken OriginalValue { get; init; }
        = JValue.CreateNull();

    public JToken CurrentValue { get; init; }
        = JValue.CreateNull();
}
