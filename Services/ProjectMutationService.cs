using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class ProjectMutationService
{
    private readonly ProjectModelFactory projectModelFactory;

    public ProjectMutationService()
        : this(
            new ProjectModelFactory())
    {
    }

    public ProjectMutationService(
        ProjectModelFactory projectModelFactory)
    {
        ArgumentNullException.ThrowIfNull(
            projectModelFactory);

        this.projectModelFactory =
            projectModelFactory;
    }

    public SheetModel FindSheet(
        ProjectModel project,
        string sheetName)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            sheetName);

        return project.Sheets.FirstOrDefault(
                   sheet =>
                       string.Equals(
                           sheet.Name,
                           sheetName,
                           StringComparison.Ordinal))
               ?? throw new InvalidOperationException(
                   $"Sheet '{sheetName}' was not found.");
    }

    public EntryModel FindEntry(
        SheetModel sheet,
        string entryId)
    {
        ArgumentNullException.ThrowIfNull(
            sheet);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            entryId);

        return sheet.Entries.FirstOrDefault(
                   entry =>
                       string.Equals(
                           entry.Id,
                           entryId,
                           StringComparison.Ordinal))
               ?? throw new InvalidOperationException(
                   $"Entry '{entryId}' was not found " +
                   $"in sheet '{sheet.Name}'.");
    }

    public EntryModel? FindEntryByProperty(
    SheetModel sheet,
    string propertyName,
    JToken propertyValue)
    {
        ArgumentNullException.ThrowIfNull(
            sheet);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyName);

        ArgumentNullException.ThrowIfNull(
            propertyValue);

        return sheet.Entries.FirstOrDefault(
            entry =>
            {
                PropertyModel? property =
                    FindProperty(
                        entry,
                        propertyName);

                if (property?.SourceProperty == null)
                {
                    return false;
                }

                return JToken.DeepEquals(
                    property.SourceProperty.Value,
                    propertyValue);
            });
    }

    public PropertyModel? FindProperty(
        EntryModel entry,
        string propertyName)
    {
        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyName);

        return entry.Properties.FirstOrDefault(
            property =>
                string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.Ordinal));
    }

    public ProjectMutationResult CreateProperty(
        string sheetName,
        EntryModel entry,
        string propertyName,
        JToken propertyValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sheetName);

        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyName);

        ArgumentNullException.ThrowIfNull(
            propertyValue);

        if (entry.SourceEntry == null)
        {
            throw new InvalidOperationException(
                $"Entry '{entry.Id}' is not connected " +
                "to a source JSON object.");
        }

        if (FindProperty(
                entry,
                propertyName) != null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' already exists " +
                $"on entry '{entry.Id}'.");
        }

        if (entry.SourceEntry.Property(
                propertyName) != null)
        {
            throw new InvalidOperationException(
                $"Source JSON property '{propertyName}' " +
                $"already exists on entry '{entry.Id}', " +
                "but no matching editor property model was found.");
        }

        JProperty sourceProperty =
            new(
                propertyName,
                propertyValue.DeepClone());

        PropertyModel propertyModel =
            projectModelFactory.CreatePropertyModel(
                sheetName,
                sourceProperty,
                PropertyModelCreationMode.NewlyCreated);

        entry.SourceEntry.Add(
            sourceProperty);

        try
        {
            entry.Properties.Add(
                propertyModel);
        }
        catch
        {
            sourceProperty.Remove();
            throw;
        }

        ProjectMutationResult result =
            new();

        result.AddProperty(
            entry,
            propertyModel);

        return result;
    }

    public ProjectMutationResult EnsureProperty(
        string sheetName,
        EntryModel entry,
        string propertyName,
        JToken propertyValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sheetName);

        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyName);

        ArgumentNullException.ThrowIfNull(
            propertyValue);

        PropertyModel? existingProperty =
            FindProperty(
                entry,
                propertyName);

        if (existingProperty == null)
        {
            return CreateProperty(
                sheetName,
                entry,
                propertyName,
                propertyValue);
        }

        if (existingProperty.SourceProperty == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' on entry " +
                $"'{entry.Id}' is not connected to a " +
                "source JSON property.");
        }

        if (JToken.DeepEquals(
                existingProperty.SourceProperty.Value,
                propertyValue))
        {
            return new ProjectMutationResult();
        }

        JToken previousValue =
            existingProperty.SourceProperty.Value.DeepClone();

        existingProperty.ApplySnapshotValue(
            propertyValue);

        ProjectMutationResult result =
            new();

        result.AddUpdatedProperty(
            existingProperty,
            previousValue);

        return result;
    }

    public ProjectMutationResult CreateEntry(
        SheetModel sheet,
        JObject sourceEntry)
    {
        ArgumentNullException.ThrowIfNull(
            sheet);

        ArgumentNullException.ThrowIfNull(
            sourceEntry);

        if (sheet.SourceSheet == null)
        {
            throw new InvalidOperationException(
                $"Sheet '{sheet.Name}' is not connected " +
                "to a source JSON object.");
        }

        JToken? sourceLinesToken =
            sheet.SourceSheet["lines"];

        if (sourceLinesToken is not JArray sourceLines)
        {
            throw new InvalidOperationException(
                $"Sheet '{sheet.Name}' does not contain " +
                "a valid source 'lines' array.");
        }

        JObject clonedSourceEntry =
            (JObject)sourceEntry.DeepClone();

        string sourceIdentifier =
            clonedSourceEntry["id"]?.ToString()
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(
                sourceIdentifier))
        {
            bool modelDuplicate =
                sheet.Entries.Any(
                    entry =>
                        string.Equals(
                            entry.Id,
                            sourceIdentifier,
                            StringComparison.Ordinal));

            if (modelDuplicate)
            {
                throw new InvalidOperationException(
                    $"Entry '{sourceIdentifier}' already exists " +
                    $"in sheet '{sheet.Name}'.");
            }

            bool sourceDuplicate =
                sourceLines
                    .OfType<JObject>()
                    .Any(
                        entry =>
                            string.Equals(
                                entry["id"]?.ToString(),
                                sourceIdentifier,
                                StringComparison.Ordinal));

            if (sourceDuplicate)
            {
                throw new InvalidOperationException(
                    $"Source JSON entry '{sourceIdentifier}' " +
                    $"already exists in sheet '{sheet.Name}'.");
            }
        }

        int entryNumber =
            sourceLines.Count + 1;

        EntryModel entryModel =
            projectModelFactory.CreateEntryModel(
                sheet.Name,
                clonedSourceEntry,
                entryNumber);

        sourceLines.Add(
            clonedSourceEntry);

        try
        {
            sheet.Entries.Add(
                entryModel);
        }
        catch
        {
            clonedSourceEntry.Remove();
            throw;
        }

        ProjectMutationResult result =
            new();

        result.AddEntry(
            sheet,
            entryModel);

        return result;
    }
}