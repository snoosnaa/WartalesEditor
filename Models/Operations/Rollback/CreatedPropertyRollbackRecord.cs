using System;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Operations.Rollback;

public sealed class CreatedPropertyRollbackRecord
{
    public EntryModel Entry { get; }

    public PropertyModel Property { get; }

    public JObject ParentObject { get; }

    public CreatedPropertyRollbackRecord(
        EntryModel entry,
        PropertyModel property)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(property);

        if (property.SourceProperty?.Parent is not JObject parentObject)
        {
            throw new InvalidOperationException(
                $"Created property '{property.Name}' on entry " +
                $"'{entry.Id}' is not attached to a JSON object.");
        }

        Entry = entry;
        Property = property;
        ParentObject = parentObject;
    }
}
