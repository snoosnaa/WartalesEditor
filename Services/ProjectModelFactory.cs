using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class ProjectModelFactory
{
    public SheetModel CreateSheetModel(
        JObject sourceSheet)
    {
        ArgumentNullException.ThrowIfNull(
            sourceSheet);

        string name =
            sourceSheet["name"]?.ToString()
            ?? string.Empty;

        SheetModel sheetModel =
            new()
            {
                Name =
                    name,

                SourceSheet =
                    sourceSheet
            };

        JArray? sourceEntries =
            sourceSheet["lines"] as JArray;

        if (sourceEntries == null)
        {
            return sheetModel;
        }

        int entryNumber =
            1;

        foreach (JObject sourceEntry in
                 sourceEntries.OfType<JObject>())
        {
            EntryModel entryModel =
                CreateEntryModel(
                    name,
                    sourceEntry,
                    entryNumber);

            sheetModel.Entries.Add(
                entryModel);

            entryNumber++;
        }

        return sheetModel;
    }

    public EntryModel CreateEntryModel(
        string sheetName,
        JObject sourceEntry,
        int entryNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sheetName);

        ArgumentNullException.ThrowIfNull(
            sourceEntry);

        if (entryNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryNumber),
                entryNumber,
                "Entry numbers must be greater than zero.");
        }

        string fallbackIdentifier =
            entryNumber.ToString(
                CultureInfo.InvariantCulture);

        string sourceIdentifier =
            sourceEntry["id"]?.ToString()
            ?? string.Empty;

        string entryIdentifier =
            string.IsNullOrWhiteSpace(
                sourceIdentifier)
                ? fallbackIdentifier
                : sourceIdentifier;

        string displayName =
            string.IsNullOrWhiteSpace(
                sourceIdentifier)
                ? fallbackIdentifier
                : sourceIdentifier;

        EntryModel entryModel =
            new()
            {
                Id =
                    entryIdentifier,

                DisplayName =
                    displayName,

                SourceEntry =
                    sourceEntry
            };

        foreach (JProperty sourceProperty in
                 sourceEntry.Properties())
        {
            foreach (PropertyModel propertyModel in
                     CreatePropertyModels(
                         sheetName,
                         sourceProperty,
                         string.Empty,
                         PropertyModelCreationMode.Existing))
            {
                entryModel.Properties.Add(
                    propertyModel);
            }
        }

        return entryModel;
    }

    private IEnumerable<PropertyModel> CreatePropertyModels(
        string sheetName,
        JProperty sourceProperty,
        string parentPath,
        PropertyModelCreationMode creationMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sheetName);

        ArgumentNullException.ThrowIfNull(
            sourceProperty);

        string propertyPath =
            string.IsNullOrWhiteSpace(
                parentPath)
                ? sourceProperty.Name
                : $"{parentPath}.{sourceProperty.Name}";

        switch (sourceProperty.Value.Type)
        {
            case JTokenType.Object:
                foreach (JProperty nestedProperty in
                         ((JObject)sourceProperty.Value)
                         .Properties())
                {
                    foreach (PropertyModel propertyModel in
                             CreatePropertyModels(
                                 sheetName,
                                 nestedProperty,
                                 propertyPath,
                                 creationMode))
                    {
                        yield return propertyModel;
                    }
                }

                yield break;

            case JTokenType.Array:
                yield break;

            default:
                yield return CreatePropertyModel(
                    sheetName,
                    sourceProperty,
                    propertyPath,
                    creationMode);
                yield break;
        }
    }

    public PropertyModel CreatePropertyModel(
        string sheetName,
        JProperty sourceProperty)
    {
        ArgumentNullException.ThrowIfNull(
            sourceProperty);

        return CreatePropertyModel(
            sheetName,
            sourceProperty,
            sourceProperty.Name,
            PropertyModelCreationMode.Existing);
    }

    public PropertyModel CreatePropertyModel(
        string sheetName,
        JProperty sourceProperty,
        PropertyModelCreationMode creationMode)
    {
        ArgumentNullException.ThrowIfNull(
            sourceProperty);

        return CreatePropertyModel(
            sheetName,
            sourceProperty,
            sourceProperty.Name,
            creationMode);
    }

    public PropertyModel CreatePropertyModel(
        string sheetName,
        JProperty sourceProperty,
        string propertyPath,
        PropertyModelCreationMode creationMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sheetName);

        ArgumentNullException.ThrowIfNull(
            sourceProperty);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyPath);

        PropertyModel propertyModel =
            new()
            {
                SheetName =
                    sheetName,

                Name =
                    sourceProperty.Name,

                PropertyPath =
                    propertyPath,

                SourceProperty =
                    sourceProperty,

                Value =
                    sourceProperty.Value.ToString()
            };

        switch (creationMode)
        {
            case PropertyModelCreationMode.Existing:
                propertyModel.CaptureOriginalValue();
                break;

            case PropertyModelCreationMode.NewlyCreated:
                propertyModel.CaptureNewPropertyBaseline();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(creationMode),
                    creationMode,
                    "The property-model creation mode is not supported.");
        }

        return propertyModel;
    }
}