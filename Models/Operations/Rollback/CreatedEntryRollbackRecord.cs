using System;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Operations.Rollback;

public sealed class CreatedEntryRollbackRecord
{
    public SheetModel Sheet { get; }

    public EntryModel Entry { get; }

    public CreatedEntryRollbackRecord(
        SheetModel sheet,
        EntryModel entry)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(entry);

        Sheet = sheet;
        Entry = entry;
    }
}
