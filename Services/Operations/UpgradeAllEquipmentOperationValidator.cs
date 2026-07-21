using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class UpgradeAllEquipmentOperationValidator :
    IProjectOperationValidator
{
    private const string ItemSheetName =
        "item";

    private const string PropsPropertyName =
        "props";

    private const string FlagsPropertyName =
        "flags";

    private const string FlagsPropertyPath =
    "props.flags";

    private const int UpgradeableEquipmentFlag =
        128;

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

        if (operation is not UpgradeAllEquipmentOperation)
        {
            return OperationValidationResult.Failure(
                $"{nameof(UpgradeAllEquipmentOperationValidator)} " +
                $"cannot validate operation type " +
                $"'{operation.GetType().Name}'.");
        }

        List<string> errors =
            new();

        SheetModel? itemSheet =
            project.Sheets.FirstOrDefault(
                sheet =>
                    string.Equals(
                        sheet.Name,
                        ItemSheetName,
                        StringComparison.Ordinal));

        if (itemSheet == null)
        {
            errors.Add(
                "The required 'item' sheet was not found.");

            return OperationValidationResult.Failure(
                errors);
        }

        int recognizedTargetCount =
            0;

        foreach (EntryModel entry in itemSheet.Entries)
        {
            if (!UpgradeAllEquipmentTargetCatalog.Contains(
                    entry.Id))
            {
                continue;
            }

            recognizedTargetCount++;

            JObject? props =
                entry.SourceEntry?[PropsPropertyName]
                    as JObject;

            if (props == null)
            {
                errors.Add(
                    $"Target item '{entry.Id}' does not contain " +
                    "a valid 'props' object.");

                continue;
            }

            JToken? flagsToken =
                props[FlagsPropertyName];

            if (flagsToken?.Type != JTokenType.Integer)
            {
                errors.Add(
                    $"Target item '{entry.Id}' does not contain " +
                    "an integer 'props.flags' value.");

                continue;
            }

            int flags =
                flagsToken.Value<int>();

            if ((flags & UpgradeableEquipmentFlag) == 0)
            {
                errors.Add(
                    $"Target item '{entry.Id}' was not marked " +
                    "as upgradeable.");
            }
        }

        if (recognizedTargetCount !=
            UpgradeAllEquipmentTargetCatalog.Count)
        {
            errors.Add(
                "The loaded CDB does not contain the complete " +
                "Upgrade All Equipment target catalog. " +
                $"Expected {UpgradeAllEquipmentTargetCatalog.Count:N0} " +
                $"items but found {recognizedTargetCount:N0}.");
        }

        if (mutationResult.CreatedEntries.Count > 0)
        {
            errors.Add(
                "Upgrade All Equipment unexpectedly created entries.");
        }

        IEnumerable<PropertyModel> changedProperties =
            mutationResult.CreatedProperties.Concat(
                mutationResult.UpdatedProperties);

        foreach (PropertyModel property in
                 changedProperties)
        {
            if (!string.Equals(
                    property.SheetName,
                    ItemSheetName,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"Upgrade All Equipment unexpectedly modified " +
                    $"sheet '{property.SheetName}'.");

                continue;
            }

            if (!string.Equals(
                    property.EffectivePropertyPath,
                    FlagsPropertyPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"Upgrade All Equipment unexpectedly modified " +
                    $"property path " +
                    $"'{property.EffectivePropertyPath}'.");
            }

            string entryId =
                GetEntryId(
                    property);

            if (string.IsNullOrWhiteSpace(
                    entryId) ||
                !UpgradeAllEquipmentTargetCatalog.Contains(
                    entryId))
            {
                errors.Add(
                    "Upgrade All Equipment modified a property " +
                    "outside the approved target catalog.");
            }
        }

        return errors.Count == 0
            ? OperationValidationResult.Success()
            : OperationValidationResult.Failure(
                errors);
    }

    private static string GetEntryId(
        PropertyModel property)
    {
        JToken? current =
            property.SourceProperty?.Parent;

        while (current != null)
        {
            if (current is JObject sourceObject)
            {
                string? entryId =
                    sourceObject["id"]?
                        .Value<string>();

                if (!string.IsNullOrWhiteSpace(
                        entryId))
                {
                    return entryId;
                }
            }

            current =
                current.Parent;
        }

        return string.Empty;
    }
}
