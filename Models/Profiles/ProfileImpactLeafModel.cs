using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WartalesEditor.Models.Profiles;

[JsonConverter(typeof(StringEnumConverter))]
public enum ProfileImpactMutationKind
{
    Created,
    Updated,
    Removed
}

public sealed class ProfileImpactLeafModel
{
    public string SheetName { get; init; } = string.Empty;

    public string EntryId { get; init; } = string.Empty;

    public string PropertyPath { get; init; } = string.Empty;

    public ProfileImpactMutationKind MutationKind { get; init; }

    public ProfileImpactLeafModel DeepClone() => new()
    {
        SheetName = SheetName,
        EntryId = EntryId,
        PropertyPath = PropertyPath,
        MutationKind = MutationKind
    };
}
