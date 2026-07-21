using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class AddCampFacilitiesOperationValidator
    : IProjectOperationValidator
{
    private const string ItemSheetName =
        "item";

    private const string CraftSheetName =
        "craft";

    private const string AnvilEntryId =
        "Anvil";

    private const string ApothecaryTableEntryId =
        "ApothecaryTable";

    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        ArgumentNullException.ThrowIfNull(
            project);

        ArgumentNullException.ThrowIfNull(
            mutationResult);

        if (operation is not AddCampFacilitiesOperation)
        {
            return OperationValidationResult.Failure(
                $"{nameof(AddCampFacilitiesOperationValidator)} " +
                $"cannot validate operation type " +
                $"'{operation.GetType().Name}'.");
        }

        List<string> errors =
            new();

        SheetModel? itemSheet =
            FindSheet(
                project,
                ItemSheetName);

        SheetModel? craftSheet =
            FindSheet(
                project,
                CraftSheetName);

        if (itemSheet == null)
        {
            errors.Add(
                "The required 'item' sheet was not found.");
        }

        if (craftSheet == null)
        {
            errors.Add(
                "The required 'craft' sheet was not found.");
        }

        if (itemSheet != null)
        {
            ValidateFacilityItem(
                itemSheet,
                AnvilEntryId,
                errors);

            ValidateFacilityItem(
                itemSheet,
                ApothecaryTableEntryId,
                errors);
        }

        if (craftSheet != null)
        {
            ValidateCraftEntry(
                craftSheet,
                AnvilEntryId,
                errors);

            ValidateCraftEntry(
                craftSheet,
                ApothecaryTableEntryId,
                errors);
        }

        ValidateMutationConnections(
            mutationResult,
            errors);

        return errors.Count == 0
            ? OperationValidationResult.Success()
            : OperationValidationResult.Failure(
                errors);
    }

    private static void ValidateFacilityItem(
        SheetModel itemSheet,
        string entryId,
        ICollection<string> errors)
    {
        EntryModel? entry =
            itemSheet.Entries.FirstOrDefault(
                candidate =>
                    string.Equals(
                        candidate.Id,
                        entryId,
                        StringComparison.Ordinal));

        if (entry == null)
        {
            errors.Add(
                $"Required item entry '{entryId}' was not found.");
            return;
        }

        if (entry.SourceEntry == null)
        {
            errors.Add(
                $"Item entry '{entryId}' is not connected " +
                "to a source JSON object.");
            return;
        }

        ValidateObjectProperty(
            entry,
            "props",
            errors);

        ValidateObjectProperty(
            entry,
            "tool",
            errors);

        ValidateObjectProperty(
            entry,
            "icon",
            errors);

        ValidatePropsStructure(
            entry,
            errors);

        ValidateToolStructure(
            entry,
            errors);

        ValidateIconStructure(
            entry,
            errors);
    }

    private static void ValidatePropsStructure(
        EntryModel entry,
        ICollection<string> errors)
    {
        JObject? props =
            GetObjectProperty(
                entry,
                "props");

        if (props == null)
        {
            return;
        }

        RequireTokenType(
            props,
            "model",
            JTokenType.String,
            entry.Id,
            "props",
            errors);

        RequireTokenType(
            props,
            "activity",
            JTokenType.String,
            entry.Id,
            "props",
            errors);

        RequireTokenType(
            props,
            "hideInCheatMenu",
            JTokenType.Boolean,
            entry.Id,
            "props",
            errors);

        RequireTokenType(
            props,
            "bonuses",
            JTokenType.Array,
            entry.Id,
            "props",
            errors);
    }

    private static void ValidateToolStructure(
        EntryModel entry,
        ICollection<string> errors)
    {
        JObject? tool =
            GetObjectProperty(
                entry,
                "tool");

        if (tool == null)
        {
            return;
        }

        string[] integerProperties =
        {
            "campWidth",
            "campHeight",
            "toolCapacity",
            "width",
            "height",
            "capacity",
            "tier"
        };

        foreach (string propertyName in integerProperties)
        {
            RequireTokenType(
                tool,
                propertyName,
                JTokenType.Integer,
                entry.Id,
                "tool",
                errors);
        }

        RequireTokenType(
            tool,
            "animation",
            JTokenType.String,
            entry.Id,
            "tool",
            errors);

        RequireTokenType(
            tool,
            "hideHandEquipment",
            JTokenType.Boolean,
            entry.Id,
            "tool",
            errors);

        RequireTokenType(
            tool,
            "bonusesIfAssigned",
            JTokenType.Array,
            entry.Id,
            "tool",
            errors);
    }

    private static void ValidateIconStructure(
        EntryModel entry,
        ICollection<string> errors)
    {
        JObject? icon =
            GetObjectProperty(
                entry,
                "icon");

        if (icon == null)
        {
            return;
        }

        RequireTokenType(
            icon,
            "file",
            JTokenType.String,
            entry.Id,
            "icon",
            errors);

        RequireTokenType(
            icon,
            "size",
            JTokenType.Integer,
            entry.Id,
            "icon",
            errors);

        RequireTokenType(
            icon,
            "x",
            JTokenType.Integer,
            entry.Id,
            "icon",
            errors);

        RequireTokenType(
            icon,
            "y",
            JTokenType.Integer,
            entry.Id,
            "icon",
            errors);
    }

    private static void ValidateCraftEntry(
        SheetModel craftSheet,
        string itemId,
        ICollection<string> errors)
    {
        EntryModel? craftEntry =
            craftSheet.Entries.FirstOrDefault(
                entry =>
                    string.Equals(
                        entry.SourceEntry?["item"]?.Value<string>(),
                        itemId,
                        StringComparison.Ordinal));

        if (craftEntry == null)
        {
            errors.Add(
                $"A Workshop crafting recipe for '{itemId}' " +
                "was not found.");
            return;
        }

        if (craftEntry.SourceEntry == null)
        {
            errors.Add(
                $"The crafting recipe for '{itemId}' is not " +
                "connected to a source JSON object.");
            return;
        }

        JObject sourceEntry =
            craftEntry.SourceEntry;

        RequireTokenType(
            sourceEntry,
            "item",
            JTokenType.String,
            itemId,
            "craft",
            errors);

        RequireTokenType(
            sourceEntry,
            "tool",
            JTokenType.String,
            itemId,
            "craft",
            errors);

        RequireTokenType(
            sourceEntry,
            "recipe",
            JTokenType.Array,
            itemId,
            "craft",
            errors);

        RequireTokenType(
            sourceEntry,
            "props",
            JTokenType.Object,
            itemId,
            "craft",
            errors);

        RequireTokenType(
            sourceEntry,
            "learnCost",
            JTokenType.Array,
            itemId,
            "craft",
            errors);

        RequireTokenType(
            sourceEntry,
            "jobLevel",
            JTokenType.Integer,
            itemId,
            "craft",
            errors);

        RequireTokenType(
            sourceEntry,
            "group",
            JTokenType.String,
            itemId,
            "craft",
            errors);

        string? tool =
            sourceEntry["tool"]?.Value<string>();

        if (!string.Equals(
                tool,
                "Workshop",
                StringComparison.Ordinal))
        {
            errors.Add(
                $"The crafting recipe for '{itemId}' does not " +
                "use the Workshop tool.");
        }
    }

    private static void ValidateMutationConnections(
        ProjectMutationResult mutationResult,
        ICollection<string> errors)
    {
        foreach (EntryModel entry
                 in mutationResult.CreatedEntries)
        {
            if (entry.SourceEntry == null)
            {
                errors.Add(
                    $"Created entry '{entry.Id}' is not connected " +
                    "to a source JSON object.");
            }
        }

        IEnumerable<PropertyModel> changedProperties =
            mutationResult.CreatedProperties.Concat(
                mutationResult.UpdatedProperties);

        foreach (PropertyModel property
                 in changedProperties)
        {
            if (property.SourceProperty == null)
            {
                errors.Add(
                    $"Changed property '{property.Name}' in sheet " +
                    $"'{property.SheetName}' is not connected to " +
                    "a source JSON property.");
            }
        }
    }

    private static void ValidateObjectProperty(
        EntryModel entry,
        string propertyName,
        ICollection<string> errors)
    {
        JToken? property =
            entry.SourceEntry?[propertyName];

        if (property == null)
        {
            errors.Add(
                $"Required property '{propertyName}' was not found " +
                $"on item entry '{entry.Id}'.");
            return;
        }

        if (property.Type != JTokenType.Object)
        {
            errors.Add(
                $"Property '{propertyName}' on item entry " +
                $"'{entry.Id}' must be a JSON object, but is " +
                $"'{property.Type}'.");
        }
    }

    private static JObject? GetObjectProperty(
        EntryModel entry,
        string propertyName)
    {
        return entry.SourceEntry?[propertyName]
            as JObject;
    }

    private static void RequireTokenType(
        JObject source,
        string propertyName,
        JTokenType requiredType,
        string entryName,
        string sectionName,
        ICollection<string> errors)
    {
        JToken? token =
            source[propertyName];

        if (token == null)
        {
            errors.Add(
                $"Required property '{propertyName}' is missing " +
                $"from '{sectionName}' for '{entryName}'.");
            return;
        }

        if (token.Type != requiredType)
        {
            errors.Add(
                $"Property '{propertyName}' in '{sectionName}' " +
                $"for '{entryName}' must be '{requiredType}', " +
                $"but is '{token.Type}'.");
        }
    }

    private static SheetModel? FindSheet(
        ProjectModel project,
        string sheetName)
    {
        return project.Sheets.FirstOrDefault(
            sheet =>
                string.Equals(
                    sheet.Name,
                    sheetName,
                    StringComparison.Ordinal));
    }
}
