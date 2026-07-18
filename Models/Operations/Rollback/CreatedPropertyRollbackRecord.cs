using System;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Operations.Rollback;

public sealed class CreatedPropertyRollbackRecord
{
    public EntryModel Entry { get; }

    public PropertyModel Property { get; }

    public CreatedPropertyRollbackRecord(
        EntryModel entry,
        PropertyModel property)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(property);

        Entry = entry;
        Property = property;
    }
}
