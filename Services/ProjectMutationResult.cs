using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations.Rollback;

namespace WartalesEditor.Services;

public sealed class ProjectMutationResult
{
    private readonly List<EntryModel> createdEntries =
        new();

    private readonly List<PropertyModel> createdProperties =
        new();

    private readonly List<PropertyModel> updatedProperties =
        new();

    private readonly List<PropertyModel> removedProperties =
        new();

    private readonly List<CreatedEntryRollbackRecord>
        createdEntryRollbackRecords =
            new();

    private readonly List<CreatedPropertyRollbackRecord>
        createdPropertyRollbackRecords =
            new();

    private readonly List<CreatedJsonPropertyRollbackRecord>
        createdJsonPropertyRollbackRecords =
            new();

    private readonly List<PropertyRollbackRecord>
        propertyRollbackRecords =
            new();

    private readonly List<RemovedPropertyRollbackRecord>
        removedPropertyRollbackRecords =
            new();

    private readonly List<GameplayOperationStateRollbackRecord>
        gameplayOperationStateRollbackRecords =
            new();

    public IReadOnlyList<EntryModel> CreatedEntries =>
        createdEntries;

    public IReadOnlyList<PropertyModel> CreatedProperties =>
        createdProperties;

    public IReadOnlyList<PropertyModel> UpdatedProperties =>
        updatedProperties;

    public IReadOnlyList<PropertyModel> RemovedProperties =>
        removedProperties;

    public IReadOnlyList<CreatedEntryRollbackRecord>
        CreatedEntryRollbackRecords =>
            createdEntryRollbackRecords;

    public IReadOnlyList<CreatedPropertyRollbackRecord>
        CreatedPropertyRollbackRecords =>
            createdPropertyRollbackRecords;

    public IReadOnlyList<CreatedJsonPropertyRollbackRecord>
        CreatedJsonPropertyRollbackRecords =>
            createdJsonPropertyRollbackRecords;

    public IReadOnlyList<PropertyRollbackRecord>
        PropertyRollbackRecords =>
            propertyRollbackRecords;

    public IReadOnlyList<RemovedPropertyRollbackRecord>
        RemovedPropertyRollbackRecords =>
            removedPropertyRollbackRecords;

    public IReadOnlyList<GameplayOperationStateRollbackRecord>
        GameplayOperationStateRollbackRecords =>
            gameplayOperationStateRollbackRecords;

    public bool WasModified =>
        createdEntries.Count > 0 ||
        createdProperties.Count > 0 ||
        updatedProperties.Count > 0 ||
        removedProperties.Count > 0 ||
        createdJsonPropertyRollbackRecords.Count > 0 ||
        gameplayOperationStateRollbackRecords.Count > 0;

    public void AddEntry(
        SheetModel sheet,
        EntryModel entry)
    {
        ArgumentNullException.ThrowIfNull(
            sheet);

        ArgumentNullException.ThrowIfNull(
            entry);

        createdEntries.Add(
            entry);

        createdEntryRollbackRecords.Add(
            new CreatedEntryRollbackRecord(
                sheet,
                entry));
    }

    public void AddProperty(
        EntryModel entry,
        PropertyModel property)
    {
        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentNullException.ThrowIfNull(
            property);

        createdProperties.Add(
            property);

        createdPropertyRollbackRecords.Add(
            new CreatedPropertyRollbackRecord(
                entry,
                property));
    }

    public void AddJsonProperty(
        JObject parentObject,
        JProperty property)
    {
        ArgumentNullException.ThrowIfNull(
            parentObject);

        ArgumentNullException.ThrowIfNull(
            property);

        createdJsonPropertyRollbackRecords.Add(
            new CreatedJsonPropertyRollbackRecord(
                parentObject,
                property));
    }

    public void AddUpdatedProperty(
        PropertyModel property,
        JToken previousValue)
    {
        ArgumentNullException.ThrowIfNull(
            property);

        ArgumentNullException.ThrowIfNull(
            previousValue);

        updatedProperties.Add(
            property);

        propertyRollbackRecords.Add(
            new PropertyRollbackRecord(
                property,
                previousValue));
    }

    public void AddRemovedProperty(
        EntryModel entry,
        PropertyModel property)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(property);

        removedProperties.Add(property);

        removedPropertyRollbackRecords.Add(
            new RemovedPropertyRollbackRecord(
                entry,
                property));
    }

    public void AddGameplayOperationState(
        ProjectModel project,
        GameplayOperationStateModel? previousState,
        GameplayOperationStateModel replacementState,
        bool previousStateWasModified)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(replacementState);

        replacementState.ProjectCompatibilityIdentity =
            project.SourceCdbGenerationIdentity
            ?? string.Empty;

        gameplayOperationStateRollbackRecords.Add(
            new GameplayOperationStateRollbackRecord(
                project,
                previousState,
                replacementState,
                previousStateWasModified));
    }

    public void Merge(
        ProjectMutationResult other)
    {
        ArgumentNullException.ThrowIfNull(
            other);

        createdEntries.AddRange(
            other.createdEntries);

        createdProperties.AddRange(
            other.createdProperties);

        updatedProperties.AddRange(
            other.updatedProperties);

        removedProperties.AddRange(
            other.removedProperties);

        createdEntryRollbackRecords.AddRange(
            other.createdEntryRollbackRecords);

        createdPropertyRollbackRecords.AddRange(
            other.createdPropertyRollbackRecords);

        createdJsonPropertyRollbackRecords.AddRange(
            other.createdJsonPropertyRollbackRecords);

        propertyRollbackRecords.AddRange(
            other.propertyRollbackRecords);

        removedPropertyRollbackRecords.AddRange(
            other.removedPropertyRollbackRecords);

        gameplayOperationStateRollbackRecords.AddRange(
            other.gameplayOperationStateRollbackRecords);
    }
}
