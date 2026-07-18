using System;
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
            PropertyModel propertyModel =
                CreatePropertyModel(
                    sheetName,
                    sourceProperty,
                    PropertyModelCreationMode.Existing);

            entryModel.Properties.Add(
                propertyModel);
        }

        return entryModel;
    }

    public PropertyModel CreatePropertyModel(
        string sheetName,
        JProperty sourceProperty)
    {
        return CreatePropertyModel(
            sheetName,
            sourceProperty,
            PropertyModelCreationMode.Existing);
    }

    public PropertyModel CreatePropertyModel(
        string sheetName,
        JProperty sourceProperty,
        PropertyModelCreationMode creationMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sheetName);

        ArgumentNullException.ThrowIfNull(
            sourceProperty);

        PropertyModel propertyModel =
            new()
            {
                SheetName =
                    sheetName,

                Name =
                    sourceProperty.Name,

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