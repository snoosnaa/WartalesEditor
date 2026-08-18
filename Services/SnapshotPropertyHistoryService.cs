using System;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public static class SnapshotPropertyHistoryService
{
    public static bool? GetOriginalPropertyExistence(
        ModificationSnapshotPropertyModel property)
    {
        ArgumentNullException.ThrowIfNull(property);

        return property.OriginalPropertyExisted
            ?? (property.OriginalValue.Type != JTokenType.Null
                ? true
                : null);
    }
}
