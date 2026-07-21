using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
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

        Rollback(
            mutationResult.PropertyRollbackRecords,
            mutationResult.CreatedPropertyRollbackRecords,
            mutationResult.CreatedJsonPropertyRollbackRecords,
            mutationResult.CreatedEntryRollbackRecords);
    }

    public void Rollback(
        IReadOnlyList<PropertyRollbackRecord>
            propertyRollbackRecords,
        IReadOnlyList<CreatedPropertyRollbackRecord>
            createdPropertyRollbackRecords,
        IReadOnlyList<CreatedJsonPropertyRollbackRecord>
            createdJsonPropertyRollbackRecords,
        IReadOnlyList<CreatedEntryRollbackRecord>
            createdEntryRollbackRecords)
    {
        ArgumentNullException.ThrowIfNull(
            propertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdJsonPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdEntryRollbackRecords);

        foreach (PropertyRollbackRecord record in
                 propertyRollbackRecords.Reverse())
        {
            record.Property.ApplySnapshotValue(
                record.PreviousValue);
        }

        foreach (CreatedPropertyRollbackRecord record in
                 createdPropertyRollbackRecords.Reverse())
        {
            RemoveCreatedProperty(
                record);
        }

        foreach (CreatedJsonPropertyRollbackRecord record in
                 createdJsonPropertyRollbackRecords.Reverse())
        {
            RemoveCreatedJsonProperty(
                record);
        }

        foreach (CreatedEntryRollbackRecord record in
                 createdEntryRollbackRecords.Reverse())
        {
            RemoveCreatedEntry(
                record);
        }
    }

    public void Replay(
        IReadOnlyList<PropertyRollbackRecord>
            propertyRollbackRecords,
        IReadOnlyList<JToken>
            updatedPropertyValues,
        IReadOnlyList<CreatedPropertyRollbackRecord>
            createdPropertyRollbackRecords,
        IReadOnlyList<CreatedJsonPropertyRollbackRecord>
            createdJsonPropertyRollbackRecords,
        IReadOnlyList<CreatedEntryRollbackRecord>
            createdEntryRollbackRecords)
    {
        ArgumentNullException.ThrowIfNull(
            propertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            updatedPropertyValues);

        ArgumentNullException.ThrowIfNull(
            createdPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdJsonPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdEntryRollbackRecords);

        if (propertyRollbackRecords.Count !=
            updatedPropertyValues.Count)
        {
            throw new InvalidOperationException(
                "The operation history contains mismatched " +
                "updated-property records and replay values.");
        }

        foreach (CreatedEntryRollbackRecord record in
                 createdEntryRollbackRecords)
        {
            RestoreCreatedEntry(
                record);
        }

        foreach (CreatedJsonPropertyRollbackRecord record in
                 createdJsonPropertyRollbackRecords)
        {
            RestoreCreatedJsonProperty(
                record);
        }

        foreach (CreatedPropertyRollbackRecord record in
                 createdPropertyRollbackRecords)
        {
            RestoreCreatedProperty(
                record);
        }

        for (int index = 0;
             index < propertyRollbackRecords.Count;
             index++)
        {
            propertyRollbackRecords[index]
                .Property
                .ApplySnapshotValue(
                    updatedPropertyValues[index]);
        }
    }

    private static void RemoveCreatedProperty(
        CreatedPropertyRollbackRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        record.Entry.Properties.Remove(
            record.Property);

        record.Property.SourceProperty?.Remove();
    }

    private static void RemoveCreatedJsonProperty(
        CreatedJsonPropertyRollbackRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        record.Property.Remove();
    }

    private static void RemoveCreatedEntry(
        CreatedEntryRollbackRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        record.Sheet.Entries.Remove(
            record.Entry);

        record.Entry.SourceEntry?.Remove();
    }

    private static void RestoreCreatedEntry(
        CreatedEntryRollbackRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        if (record.Sheet.SourceSheet == null)
        {
            throw new InvalidOperationException(
                $"Sheet '{record.Sheet.Name}' is not connected " +
                "to a source JSON object.");
        }

        if (record.Entry.SourceEntry == null)
        {
            throw new InvalidOperationException(
                $"Created entry '{record.Entry.Id}' is not connected " +
                "to a source JSON object.");
        }

        if (record.Sheet.SourceSheet["lines"] is not
            JArray sourceLines)
        {
            throw new InvalidOperationException(
                $"Sheet '{record.Sheet.Name}' does not contain " +
                "a valid source 'lines' array.");
        }

        if (record.Sheet.Entries.Contains(
                record.Entry))
        {
            throw new InvalidOperationException(
                $"Created entry '{record.Entry.Id}' is already " +
                $"present in sheet '{record.Sheet.Name}'.");
        }

        if (record.Entry.SourceEntry.Parent != null)
        {
            throw new InvalidOperationException(
                $"The source JSON for created entry " +
                $"'{record.Entry.Id}' is already attached.");
        }

        sourceLines.Add(
            record.Entry.SourceEntry);

        try
        {
            record.Sheet.Entries.Add(
                record.Entry);
        }
        catch
        {
            record.Entry.SourceEntry.Remove();
            throw;
        }
    }

    private static void RestoreCreatedJsonProperty(
        CreatedJsonPropertyRollbackRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        if (record.ParentObject.Property(
                record.Property.Name) != null)
        {
            throw new InvalidOperationException(
                $"Source JSON property '{record.Property.Name}' " +
                "is already present during operation replay.");
        }

        if (record.Property.Parent != null)
        {
            throw new InvalidOperationException(
                $"The source JSON property '{record.Property.Name}' " +
                "is already attached during operation replay.");
        }

        record.ParentObject.Add(
            record.Property);
    }

    private static void RestoreCreatedProperty(
        CreatedPropertyRollbackRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        if (record.Entry.SourceEntry == null)
        {
            throw new InvalidOperationException(
                $"Entry '{record.Entry.Id}' is not connected " +
                "to a source JSON object.");
        }

        if (record.Property.SourceProperty == null)
        {
            throw new InvalidOperationException(
                $"Created property '{record.Property.Name}' on " +
                $"entry '{record.Entry.Id}' is not connected " +
                "to a source JSON property.");
        }

        if (record.Entry.Properties.Contains(
                record.Property))
        {
            throw new InvalidOperationException(
                $"Created property '{record.Property.Name}' is " +
                $"already present on entry '{record.Entry.Id}'.");
        }

        if (record.ParentObject.Property(
                record.Property.Name) != null)
        {
            throw new InvalidOperationException(
                $"Source JSON property '{record.Property.Name}' " +
                $"already exists at path " +
                $"'{record.Property.EffectivePropertyPath}'.");
        }

        if (record.Property.SourceProperty.Parent != null)
        {
            throw new InvalidOperationException(
                $"The source JSON for created property " +
                $"'{record.Property.Name}' is already attached.");
        }

        record.ParentObject.Add(
            record.Property.SourceProperty);

        try
        {
            record.Entry.Properties.Add(
                record.Property);
        }
        catch
        {
            record.Property.SourceProperty.Remove();
            throw;
        }
    }
}