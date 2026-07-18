using System;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Operations.Rollback;
using WartalesEditor.Services;

namespace WartalesEditor.Services.Operations;

public sealed class ProjectOperationTransactionService
{
    public ProjectOperationSnapshot Capture(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        if (project.RootDocument == null)
        {
            throw new InvalidOperationException(
                "The project does not contain a root JSON document.");
        }

        return new ProjectOperationSnapshot(
            project.RootDocument,
            project.OriginalJson,
            project.FileName);
    }

    public void Rollback(
        ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(
            mutationResult);

        foreach (PropertyRollbackRecord record in
                 mutationResult.PropertyRollbackRecords.Reverse())
        {
            record.Property.ApplySnapshotValue(
                record.PreviousValue);
        }

        foreach (CreatedPropertyRollbackRecord record in
                 mutationResult
                     .CreatedPropertyRollbackRecords
                     .Reverse())
        {
            record.Entry.Properties.Remove(
                record.Property);

            record.Property.SourceProperty?.Remove();
        }

        foreach (CreatedEntryRollbackRecord record in
                 mutationResult
                     .CreatedEntryRollbackRecords
                     .Reverse())
        {
            record.Sheet.Entries.Remove(
                record.Entry);

            record.Entry.SourceEntry?.Remove();
        }
    }
}