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

    public PropertyModel? FindPropertyByPath(
    EntryModel entry,
    string propertyPath)
    {
        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyPath);

        return entry.Properties.FirstOrDefault(
            property =>
                string.Equals(
                    property.EffectivePropertyPath,
                    propertyPath,
                    StringComparison.Ordinal));
    }

    public ProjectMutationResult EnsurePropertyByPath(
        EntryModel entry,
        string propertyPath,
        JToken propertyValue)
    {
        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyPath);

        ArgumentNullException.ThrowIfNull(
            propertyValue);

        if (entry.SourceEntry == null)
        {
            throw new InvalidOperationException(
                $"Entry '{entry.Id}' is not connected " +
                "to a source JSON object.");
        }

        PropertyModel? existingProperty =
            FindPropertyByPath(
                entry,
                propertyPath);

        if (existingProperty != null)
        {
            return UpdateExistingProperty(
                existingProperty,
                propertyValue);
        }

        string[] pathSegments =
            propertyPath.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (pathSegments.Length == 0)
        {
            throw new InvalidOperationException(
                "A nested property path must contain at least " +
                "one path segment.");
        }

        ProjectMutationResult result =
            new();

        JObject parentObject =
            entry.SourceEntry;

        string currentPath =
            string.Empty;

        for (int index = 0;
             index < pathSegments.Length - 1;
             index++)
        {
            string segment =
                pathSegments[index];

            currentPath =
                string.IsNullOrEmpty(
                    currentPath)
                    ? segment
                    : $"{currentPath}.{segment}";

            JToken? existingToken =
                parentObject[segment];

            if (existingToken == null)
            {
                JProperty createdObjectProperty =
                    new(
                        segment,
                        new JObject());

                parentObject.Add(
                    createdObjectProperty);

                result.AddJsonProperty(
                    parentObject,
                    createdObjectProperty);

                parentObject =
                    (JObject)createdObjectProperty.Value;

                continue;
            }

            if (existingToken is not JObject existingObject)
            {
                throw new InvalidOperationException(
                    $"Cannot create property path '{propertyPath}' " +
                    $"because '{currentPath}' is not a JSON object.");
            }

            parentObject =
                existingObject;
        }

        string leafName =
            pathSegments[^1];

        JProperty? existingSourceProperty =
            parentObject.Property(
                leafName);

        if (existingSourceProperty != null)
        {
            if (existingSourceProperty.Value.Type ==
                JTokenType.Object)
            {
                throw new InvalidOperationException(
                    $"Property path '{propertyPath}' is not a " +
                    "scalar or array JSON property.");
            }

            PropertyModel propertyModel =
                projectModelFactory.CreatePropertyModel(
                    ResolveSheetName(
                        entry),
                    existingSourceProperty,
                    propertyPath,
                    PropertyModelCreationMode.Existing);

            entry.Properties.Add(
                propertyModel);

            return UpdateExistingProperty(
                propertyModel,
                propertyValue);
        }

        string sheetName =
            ResolveSheetName(
                entry);

        JProperty sourceProperty =
            new(
                leafName,
                propertyValue.DeepClone());

        parentObject.Add(
            sourceProperty);

        PropertyModel createdProperty =
            projectModelFactory.CreatePropertyModel(
                sheetName,
                sourceProperty,
                propertyPath,
                PropertyModelCreationMode.NewlyCreated);

        try
        {
            entry.Properties.Add(
                createdProperty);
        }
        catch
        {
            sourceProperty.Remove();
            throw;
        }

        result.AddProperty(
            entry,
            createdProperty);

        return result;
    }

    public ProjectMutationResult EnsureObjectByPath(
        EntryModel entry,
        string propertyPath,
        JObject objectValue)
    {
        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyPath);

        ArgumentNullException.ThrowIfNull(
            objectValue);

        if (entry.SourceEntry == null)
        {
            throw new InvalidOperationException(
                $"Entry '{entry.Id}' is not connected " +
                "to a source JSON object.");
        }

        string[] pathSegments =
            propertyPath.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (pathSegments.Length == 0)
        {
            throw new InvalidOperationException(
                "An object property path must contain at least " +
                "one path segment.");
        }

        ValidateObjectMutation(
            entry,
            pathSegments,
            propertyPath,
            objectValue);

        ProjectMutationResult result =
            new();

        JObject targetObject =
            EnsureObjectPath(
                entry,
                pathSegments,
                propertyPath,
                result);

        MergeObjectMembers(
            entry,
            targetObject,
            propertyPath,
            objectValue,
            result);

        return result;
    }

    public ProjectMutationResult RemovePropertyByPath(
        EntryModel entry,
        string propertyPath)
    {
        ArgumentNullException.ThrowIfNull(entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyPath);

        if (entry.SourceEntry == null)
        {
            throw new InvalidOperationException(
                $"Entry '{entry.Id}' is not connected " +
                "to a source JSON object.");
        }

        PropertyModel[] matchingProperties =
            entry.Properties
                .Where(property =>
                    string.Equals(
                        property.EffectivePropertyPath,
                        propertyPath,
                        StringComparison.Ordinal))
                .ToArray();

        if (matchingProperties.Length == 0)
        {
            throw new InvalidOperationException(
                $"Property path '{propertyPath}' was not found " +
                $"on entry '{entry.Id}'.");
        }

        if (matchingProperties.Length > 1)
        {
            throw new InvalidOperationException(
                $"Property path '{propertyPath}' is ambiguous " +
                $"on entry '{entry.Id}'.");
        }

        PropertyModel property =
            matchingProperties[0];

        if (property.SourceProperty == null)
        {
            throw new InvalidOperationException(
                $"Property path '{propertyPath}' is not connected " +
                "to a source JSON property.");
        }

        JProperty sourceProperty =
            ResolveSourcePropertyByPath(
                entry.SourceEntry,
                propertyPath);

        if (!ReferenceEquals(
                property.SourceProperty,
                sourceProperty))
        {
            throw new InvalidOperationException(
                $"Property model '{propertyPath}' is not connected " +
                "to the matching source JSON property.");
        }

        if (sourceProperty.Value.Type ==
            JTokenType.Object)
        {
            throw new InvalidOperationException(
                $"Property path '{propertyPath}' is an object " +
                "container and cannot be removed by the " +
                "known-property removal API.");
        }

        ProjectMutationResult result =
            new();

        result.AddRemovedProperty(
            entry,
            property);

        int propertyIndex =
            entry.Properties.IndexOf(property);

        try
        {
            entry.Properties.RemoveAt(
                propertyIndex);

            sourceProperty.Remove();
        }
        catch
        {
            if (!entry.Properties.Contains(property))
            {
                entry.Properties.Insert(
                    propertyIndex,
                    property);
            }

            throw;
        }

        return result;
    }

    private static JProperty ResolveSourcePropertyByPath(
        JObject sourceEntry,
        string propertyPath)
    {
        string[] pathSegments =
            propertyPath.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (pathSegments.Length == 0)
        {
            throw new InvalidOperationException(
                "A property path must contain at least one path segment.");
        }

        JObject parentObject =
            sourceEntry;

        string currentPath =
            string.Empty;

        for (int index = 0;
             index < pathSegments.Length - 1;
             index++)
        {
            string segment =
                pathSegments[index];

            currentPath =
                string.IsNullOrEmpty(currentPath)
                    ? segment
                    : $"{currentPath}.{segment}";

            if (parentObject[segment] is not JObject nestedObject)
            {
                throw new InvalidOperationException(
                    $"Property path '{propertyPath}' could not be " +
                    $"resolved because '{currentPath}' is missing " +
                    "or is not a JSON object.");
            }

            parentObject =
                nestedObject;
        }

        return parentObject.Property(pathSegments[^1])
            ?? throw new InvalidOperationException(
                $"Source JSON property path '{propertyPath}' was " +
                "not found.");
    }

    private void ValidateObjectMutation(
        EntryModel entry,
        string[] pathSegments,
        string propertyPath,
        JObject objectValue)
    {
        JObject currentObject =
            entry.SourceEntry!;

        string currentPath =
            string.Empty;

        foreach (string segment in pathSegments)
        {
            currentPath =
                string.IsNullOrEmpty(
                    currentPath)
                    ? segment
                    : $"{currentPath}.{segment}";

            JToken? existingToken =
                currentObject[segment];

            if (existingToken == null)
            {
                return;
            }

            if (existingToken is not JObject nestedObject)
            {
                throw new InvalidOperationException(
                    $"Cannot ensure object path '{propertyPath}' " +
                    $"because '{currentPath}' is not a JSON object.");
            }

            currentObject =
                nestedObject;
        }

        ValidateObjectMembers(
            entry,
            currentObject,
            propertyPath,
            objectValue);
    }

    private void ValidateObjectMembers(
        EntryModel entry,
        JObject targetObject,
        string targetPath,
        JObject objectValue)
    {
        foreach (JProperty incomingProperty in
                 objectValue.Properties())
        {
            string memberPath =
                $"{targetPath}.{incomingProperty.Name}";

            JProperty? sourceProperty =
                targetObject.Property(
                    incomingProperty.Name);

            if (incomingProperty.Value is JObject incomingObject)
            {
                if (sourceProperty == null)
                {
                    continue;
                }

                if (sourceProperty.Value is not JObject nestedObject)
                {
                    throw new InvalidOperationException(
                        $"Cannot merge object member '{memberPath}' " +
                        "because the existing value is not a JSON object.");
                }

                ValidateObjectMembers(
                    entry,
                    nestedObject,
                    memberPath,
                    incomingObject);

                continue;
            }

            if (sourceProperty?.Value is JObject)
            {
                throw new InvalidOperationException(
                    $"Cannot replace object member '{memberPath}' " +
                    "with a non-object value during a deep merge.");
            }

            PropertyModel? propertyModel =
                FindPropertyByPath(
                    entry,
                    memberPath);

            if (propertyModel != null &&
                sourceProperty != null &&
                !ReferenceEquals(
                    propertyModel.SourceProperty,
                    sourceProperty))
            {
                throw new InvalidOperationException(
                    $"Property model '{memberPath}' is not connected " +
                    "to the matching source JSON property.");
            }
        }
    }

    private JObject EnsureObjectPath(
        EntryModel entry,
        string[] pathSegments,
        string propertyPath,
        ProjectMutationResult result)
    {
        JObject currentObject =
            entry.SourceEntry!;

        string currentPath =
            string.Empty;

        foreach (string segment in pathSegments)
        {
            currentPath =
                string.IsNullOrEmpty(
                    currentPath)
                    ? segment
                    : $"{currentPath}.{segment}";

            JProperty? sourceProperty =
                currentObject.Property(
                    segment);

            if (sourceProperty == null)
            {
                sourceProperty =
                    new JProperty(
                        segment,
                        new JObject());

                currentObject.Add(
                    sourceProperty);

                result.AddJsonProperty(
                    currentObject,
                    sourceProperty);
            }

            if (sourceProperty.Value is not JObject nestedObject)
            {
                throw new InvalidOperationException(
                    $"Cannot ensure object path '{propertyPath}' " +
                    $"because '{currentPath}' is not a JSON object.");
            }

            currentObject =
                nestedObject;
        }

        return currentObject;
    }

    private void MergeObjectMembers(
        EntryModel entry,
        JObject targetObject,
        string targetPath,
        JObject objectValue,
        ProjectMutationResult result)
    {
        foreach (JProperty incomingProperty in
                 objectValue.Properties())
        {
            string memberPath =
                $"{targetPath}.{incomingProperty.Name}";

            if (incomingProperty.Value is JObject incomingObject)
            {
                JObject nestedTarget =
                    EnsureObjectMember(
                        targetObject,
                        incomingProperty.Name,
                        memberPath,
                        result);

                MergeObjectMembers(
                    entry,
                    nestedTarget,
                    memberPath,
                    incomingObject,
                    result);

                continue;
            }

            result.Merge(
                EnsureObjectMemberValue(
                    entry,
                    targetObject,
                    incomingProperty.Name,
                    memberPath,
                    incomingProperty.Value));
        }
    }

    private static JObject EnsureObjectMember(
        JObject parentObject,
        string memberName,
        string memberPath,
        ProjectMutationResult result)
    {
        JProperty? sourceProperty =
            parentObject.Property(
                memberName);

        if (sourceProperty == null)
        {
            sourceProperty =
                new JProperty(
                    memberName,
                    new JObject());

            parentObject.Add(
                sourceProperty);

            result.AddJsonProperty(
                parentObject,
                sourceProperty);
        }

        if (sourceProperty.Value is not JObject nestedObject)
        {
            throw new InvalidOperationException(
                $"Cannot merge object member '{memberPath}' " +
                "because the existing value is not a JSON object.");
        }

        return nestedObject;
    }

    private ProjectMutationResult EnsureObjectMemberValue(
        EntryModel entry,
        JObject parentObject,
        string memberName,
        string memberPath,
        JToken memberValue)
    {
        PropertyModel? propertyModel =
            FindPropertyByPath(
                entry,
                memberPath);

        JProperty? sourceProperty =
            parentObject.Property(
                memberName);

        if (sourceProperty == null)
        {
            if (propertyModel != null)
            {
                throw new InvalidOperationException(
                    $"Property model '{memberPath}' does not have " +
                    "a matching source JSON property.");
            }

            sourceProperty =
                new JProperty(
                    memberName,
                    memberValue.DeepClone());

            parentObject.Add(
                sourceProperty);

            PropertyModel createdProperty =
                projectModelFactory.CreatePropertyModel(
                    ResolveSheetName(
                        entry),
                    sourceProperty,
                    memberPath,
                    PropertyModelCreationMode.NewlyCreated);

            try
            {
                entry.Properties.Add(
                    createdProperty);
            }
            catch
            {
                sourceProperty.Remove();
                throw;
            }

            ProjectMutationResult createdResult =
                new();

            createdResult.AddProperty(
                entry,
                createdProperty);

            return createdResult;
        }

        if (propertyModel == null)
        {
            propertyModel =
                projectModelFactory.CreatePropertyModel(
                    ResolveSheetName(
                        entry),
                    sourceProperty,
                    memberPath,
                    PropertyModelCreationMode.Existing);

            entry.Properties.Add(
                propertyModel);
        }

        if (!ReferenceEquals(
                propertyModel.SourceProperty,
                sourceProperty))
        {
            throw new InvalidOperationException(
                $"Property model '{memberPath}' is not connected " +
                "to the matching source JSON property.");
        }

        return UpdateExistingProperty(
            propertyModel,
            memberValue);
    }

    private static ProjectMutationResult UpdateExistingProperty(
        PropertyModel property,
        JToken propertyValue)
    {
        if (property.SourceProperty == null)
        {
            throw new InvalidOperationException(
                $"Property path '{property.EffectivePropertyPath}' " +
                "is not connected to a source JSON property.");
        }

        if (JToken.DeepEquals(
                property.SourceProperty.Value,
                propertyValue))
        {
            return new ProjectMutationResult();
        }

        JToken previousValue =
            property.SourceProperty.Value.DeepClone();

        property.ApplySnapshotValue(
            propertyValue);

        ProjectMutationResult result =
            new();

        result.AddUpdatedProperty(
            property,
            previousValue);

        return result;
    }

    private static string ResolveSheetName(
        EntryModel entry)
    {
        JToken? current =
            entry.SourceEntry;

        while (current != null)
        {
            if (current is JObject sourceObject &&
                sourceObject["name"]?.Type ==
                    JTokenType.String &&
                sourceObject["lines"] is JArray)
            {
                return sourceObject["name"]!
                    .Value<string>()
                    ?? string.Empty;
            }

            current =
                current.Parent;
        }

        throw new InvalidOperationException(
            $"The sheet containing entry '{entry.Id}' " +
            "could not be resolved.");
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
