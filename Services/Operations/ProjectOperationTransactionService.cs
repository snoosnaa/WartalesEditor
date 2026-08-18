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
            mutationResult.RemovedPropertyRollbackRecords,
            mutationResult.CreatedPropertyRollbackRecords,
            mutationResult.CreatedJsonPropertyRollbackRecords,
            mutationResult.CreatedEntryRollbackRecords,
            mutationResult.GameplayOperationStateRollbackRecords);
    }

    public void Rollback(
        IReadOnlyList<PropertyRollbackRecord>
            propertyRollbackRecords,
        IReadOnlyList<CreatedPropertyRollbackRecord>
            createdPropertyRollbackRecords,
        IReadOnlyList<CreatedJsonPropertyRollbackRecord>
            createdJsonPropertyRollbackRecords,
        IReadOnlyList<CreatedEntryRollbackRecord>
            createdEntryRollbackRecords,
        IReadOnlyList<GameplayOperationStateRollbackRecord>
            gameplayOperationStateRollbackRecords)
    {
        Rollback(
            propertyRollbackRecords,
            Array.Empty<RemovedPropertyRollbackRecord>(),
            createdPropertyRollbackRecords,
            createdJsonPropertyRollbackRecords,
            createdEntryRollbackRecords,
            gameplayOperationStateRollbackRecords);
    }

    public void Rollback(
        IReadOnlyList<PropertyRollbackRecord>
            propertyRollbackRecords,
        IReadOnlyList<RemovedPropertyRollbackRecord>
            removedPropertyRollbackRecords,
        IReadOnlyList<CreatedPropertyRollbackRecord>
            createdPropertyRollbackRecords,
        IReadOnlyList<CreatedJsonPropertyRollbackRecord>
            createdJsonPropertyRollbackRecords,
        IReadOnlyList<CreatedEntryRollbackRecord>
            createdEntryRollbackRecords,
        IReadOnlyList<GameplayOperationStateRollbackRecord>
            gameplayOperationStateRollbackRecords)
    {
        ArgumentNullException.ThrowIfNull(
            propertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            removedPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdJsonPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdEntryRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            gameplayOperationStateRollbackRecords);

        foreach (GameplayOperationStateRollbackRecord record in
                 gameplayOperationStateRollbackRecords.Reverse())
        {
            RestoreOperationState(
                record.Project,
                record.PreviousState,
                record.ReplacementState.OperationType,
                record.PreviousStateWasModified);
        }

        foreach (RemovedPropertyRollbackRecord record in
                 removedPropertyRollbackRecords.Reverse())
        {
            RestoreRemovedProperty(
                record);
        }

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
            createdEntryRollbackRecords,
        IReadOnlyList<GameplayOperationStateRollbackRecord>
            gameplayOperationStateRollbackRecords)
    {
        Replay(
            propertyRollbackRecords,
            updatedPropertyValues,
            Array.Empty<RemovedPropertyRollbackRecord>(),
            createdPropertyRollbackRecords,
            createdJsonPropertyRollbackRecords,
            createdEntryRollbackRecords,
            gameplayOperationStateRollbackRecords);
    }

    public void Replay(
        IReadOnlyList<PropertyRollbackRecord>
            propertyRollbackRecords,
        IReadOnlyList<JToken>
            updatedPropertyValues,
        IReadOnlyList<RemovedPropertyRollbackRecord>
            removedPropertyRollbackRecords,
        IReadOnlyList<CreatedPropertyRollbackRecord>
            createdPropertyRollbackRecords,
        IReadOnlyList<CreatedJsonPropertyRollbackRecord>
            createdJsonPropertyRollbackRecords,
        IReadOnlyList<CreatedEntryRollbackRecord>
            createdEntryRollbackRecords,
        IReadOnlyList<GameplayOperationStateRollbackRecord>
            gameplayOperationStateRollbackRecords)
    {
        ArgumentNullException.ThrowIfNull(
            propertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            updatedPropertyValues);

        ArgumentNullException.ThrowIfNull(
            removedPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdJsonPropertyRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            createdEntryRollbackRecords);

        ArgumentNullException.ThrowIfNull(
            gameplayOperationStateRollbackRecords);

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

        foreach (RemovedPropertyRollbackRecord record in
                 removedPropertyRollbackRecords)
        {
            RemoveRestoredProperty(
                record);
        }

        foreach (GameplayOperationStateRollbackRecord record in
                 gameplayOperationStateRollbackRecords)
        {
            RestoreOperationState(
                record.Project,
                record.ReplacementState,
                record.ReplacementState.OperationType,
                stateWasModified: true);
        }
    }

    private static void RestoreOperationState(
        ProjectModel project,
        GameplayOperationStateModel? state,
        ProgressionType operationType,
        bool stateWasModified)
    {
        GameplayOperationStateModel? existing =
            project.GameplayOperationStates.FirstOrDefault(
                candidate =>
                    candidate.OperationType == operationType);

        if (existing != null)
        {
            project.GameplayOperationStates.Remove(existing);
        }

        if (state != null)
        {
            project.GameplayOperationStates.Add(
                state.DeepClone());
        }

        project.IsGameplayOperationStateModified =
            stateWasModified;
    }

    private static void RestoreRemovedProperty(
        RemovedPropertyRollbackRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        JProperty? sourceProperty =
            record.Property.SourceProperty;

        if (sourceProperty == null)
        {
            throw new InvalidOperationException(
                $"Removed property '{record.PropertyPath}' is not " +
                "connected to a source JSON property.");
        }

        if (record.Entry.Properties.Contains(record.Property) ||
            sourceProperty.Parent != null)
        {
            throw new InvalidOperationException(
                $"Removed property '{record.PropertyPath}' is " +
                "already attached during rollback.");
        }

        if (record.Entry.Properties.Any(property =>
                string.Equals(
                    property.EffectivePropertyPath,
                    record.PropertyPath,
                    StringComparison.Ordinal)) ||
            record.ParentObject.Property(sourceProperty.Name) != null)
        {
            throw new InvalidOperationException(
                $"Property path '{record.PropertyPath}' already " +
                "exists during rollback.");
        }

        JProperty[] sourceProperties =
            record.ParentObject.Properties().ToArray();

        if (record.PropertyIndex > record.Entry.Properties.Count ||
            record.SourcePropertyIndex > sourceProperties.Length)
        {
            throw new InvalidOperationException(
                $"Property path '{record.PropertyPath}' cannot be " +
                "restored at its original position.");
        }

        if (record.SourcePropertyIndex == sourceProperties.Length)
        {
            record.ParentObject.Add(sourceProperty);
        }
        else
        {
            sourceProperties[record.SourcePropertyIndex]
                .AddBeforeSelf(sourceProperty);
        }

        try
        {
            record.Entry.Properties.Insert(
                record.PropertyIndex,
                record.Property);
        }
        catch
        {
            sourceProperty.Remove();
            throw;
        }

        if (record.Property.IsModified !=
            record.PropertyWasModified)
        {
            record.Entry.Properties.Remove(record.Property);
            sourceProperty.Remove();

            throw new InvalidOperationException(
                $"Property path '{record.PropertyPath}' did not " +
                "restore its prior modification state.");
        }
    }

    private static void RemoveRestoredProperty(
        RemovedPropertyRollbackRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        JProperty? sourceProperty =
            record.Property.SourceProperty;

        if (sourceProperty == null ||
            !ReferenceEquals(
                sourceProperty.Parent,
                record.ParentObject) ||
            !record.Entry.Properties.Contains(record.Property) ||
            record.Property.IsModified != record.PropertyWasModified)
        {
            throw new InvalidOperationException(
                $"Property path '{record.PropertyPath}' is not in " +
                "the expected state during replay.");
        }

        int propertyIndex =
            record.Entry.Properties.IndexOf(record.Property);

        try
        {
            record.Entry.Properties.RemoveAt(propertyIndex);

            sourceProperty.Remove();
        }
        catch
        {
            if (!record.Entry.Properties.Contains(record.Property))
            {
                record.Entry.Properties.Insert(
                    propertyIndex,
                    record.Property);
            }

            throw;
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
