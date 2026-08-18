using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ProfileOperationCaptureService
{
    private const int UpgradeableEquipmentFlag = 128;

    private readonly IOperationValidatorProvider validatorProvider;
    private readonly AddCampFacilitiesOperation addCampOperation;
    private readonly UpgradeAllEquipmentOperation upgradeOperation;
    private readonly CampFacilityJsonBuilder campBuilder;

    public static ProfileOperationCaptureService CreateDefault()
    {
        ProjectMutationService mutationService = new();
        ContentCreationService contentCreationService = new(mutationService);

        return new ProfileOperationCaptureService(
            new OperationValidatorProvider(),
            new AddCampFacilitiesOperation(contentCreationService),
            new UpgradeAllEquipmentOperation(contentCreationService));
    }

    public ProfileOperationCaptureService(
        IOperationValidatorProvider validatorProvider,
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation)
        : this(
            validatorProvider,
            addCampOperation,
            upgradeOperation,
            new CampFacilityJsonBuilder())
    {
    }

    public ProfileOperationCaptureService(
        IOperationValidatorProvider validatorProvider,
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation,
        CampFacilityJsonBuilder campBuilder)
    {
        this.validatorProvider = validatorProvider
            ?? throw new ArgumentNullException(
                nameof(validatorProvider));
        this.addCampOperation = addCampOperation
            ?? throw new ArgumentNullException(
                nameof(addCampOperation));
        this.upgradeOperation = upgradeOperation
            ?? throw new ArgumentNullException(
                nameof(upgradeOperation));
        this.campBuilder = campBuilder
            ?? throw new ArgumentNullException(
                nameof(campBuilder));
    }

    public IReadOnlyList<ProfileOperationRequestModel> Capture(
        ProjectModel project,
        ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(snapshot);

        List<ProfileOperationRequestModel> requests = new();

        if (IsApplied(addCampOperation, project))
        {
            requests.Add(
                CreateRequest(
                    ProfileOperationIds.AddCampFacilities));
            FilterAddCampProperties(project, snapshot);
        }

        if (IsApplied(upgradeOperation, project))
        {
            requests.Add(
                CreateRequest(
                    ProfileOperationIds.UpgradeAllEquipment));
            FilterUpgradeProperties(snapshot);
        }

        RemoveEmptySnapshotContainers(snapshot);
        return requests;
    }

    private bool IsApplied(
        IProjectOperation operation,
        ProjectModel project)
    {
        OperationValidationResult result =
            validatorProvider.Validate(
                operation,
                project,
                new ProjectMutationResult());

        return result.IsValid;
    }

    private static ProfileOperationRequestModel CreateRequest(
        string operationId)
    {
        return new ProfileOperationRequestModel
        {
            OperationId = operationId
        };
    }

    private void FilterAddCampProperties(
        ProjectModel project,
        ModificationSnapshotModel snapshot)
    {
        SheetModel? itemSheet =
            project.Sheets.FirstOrDefault(sheet =>
                string.Equals(
                    sheet.Name,
                    "item",
                    StringComparison.Ordinal));

        if (itemSheet == null)
        {
            return;
        }

        FilterFacility(
            snapshot,
            itemSheet,
            "Anvil",
            campBuilder.BuildAnvilProps,
            campBuilder.BuildAnvilTool(),
            campBuilder.BuildAnvilIcon());

        FilterFacility(
            snapshot,
            itemSheet,
            "ApothecaryTable",
            campBuilder.BuildApothecaryProps,
            campBuilder.BuildApothecaryTool(),
            campBuilder.BuildApothecaryIcon());
    }

    private static void FilterFacility(
        ModificationSnapshotModel snapshot,
        SheetModel itemSheet,
        string entryId,
        Func<JObject, JObject> buildProps,
        JObject tool,
        JObject icon)
    {
        EntryModel? entry =
            itemSheet.Entries.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    entryId,
                    StringComparison.Ordinal));

        if (entry?.SourceEntry?["props"] is not JObject props)
        {
            return;
        }

        Dictionary<string, JToken> ownedValues =
            new(StringComparer.Ordinal);

        JObject builtProps =
            buildProps(props);

        AddNamedValues(
            builtProps,
            ownedValues,
            "props",
            "model",
            "activity",
            "hideInCheatMenu",
            "bonuses");
        AddLeafValues("tool", tool, ownedValues);
        AddLeafValues("icon", icon, ownedValues);

        ModificationSnapshotSettingModel? setting =
            FindSetting(snapshot, "item", entryId);

        setting?.Properties.RemoveAll(property =>
            ownedValues.TryGetValue(
                GetPropertyIdentity(property),
                out JToken? expectedValue)
            &&
            JToken.DeepEquals(
                property.CurrentValue,
                expectedValue));
    }

    private static void AddLeafValues(
        string parentPath,
        JObject source,
        IDictionary<string, JToken> values)
    {
        foreach (JProperty property in source.Properties())
        {
            if (property.Value is JObject nested)
            {
                AddLeafValues(
                    $"{parentPath}.{property.Name}",
                    nested,
                    values);
                continue;
            }

            values[$"{parentPath}.{property.Name}"] =
                property.Value.DeepClone();
        }
    }

    private static void AddNamedValues(
        JObject source,
        IDictionary<string, JToken> values,
        string parentPath,
        params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            JToken? value = source[propertyName];

            if (value != null)
            {
                values[$"{parentPath}.{propertyName}"] =
                    value.DeepClone();
            }
        }
    }

    private static void FilterUpgradeProperties(
        ModificationSnapshotModel snapshot)
    {
        ModificationSnapshotCategoryModel? itemCategory =
            snapshot.Categories.FirstOrDefault(category =>
                string.Equals(
                    category.Name,
                    "item",
                    StringComparison.Ordinal));

        if (itemCategory == null)
        {
            return;
        }

        foreach (ModificationSnapshotSettingModel setting in
                 itemCategory.Settings)
        {
            if (!UpgradeAllEquipmentTargetCatalog.Contains(
                    setting.Id))
            {
                continue;
            }

            setting.Properties.RemoveAll(property =>
                string.Equals(
                    GetPropertyIdentity(property),
                    "props.flags",
                    StringComparison.Ordinal)
                &&
                IsUpgradeOwnedFlagChange(property));
        }
    }

    private static bool IsUpgradeOwnedFlagChange(
        ModificationSnapshotPropertyModel property)
    {
        if (property.CurrentValue.Type != JTokenType.Integer)
        {
            return false;
        }

        int originalFlags =
            property.OriginalValue.Type == JTokenType.Integer
                ? property.OriginalValue.Value<int>()
                : 0;

        if (property.OriginalValue.Type is not
            (JTokenType.Integer or JTokenType.Null))
        {
            return false;
        }

        return property.CurrentValue.Value<int>() ==
               (originalFlags | UpgradeableEquipmentFlag);
    }

    private static string GetPropertyIdentity(
        ModificationSnapshotPropertyModel property) =>
        string.IsNullOrWhiteSpace(property.PropertyPath)
            ? property.Name
            : property.PropertyPath;

    private static ModificationSnapshotSettingModel? FindSetting(
        ModificationSnapshotModel snapshot,
        string sheetName,
        string entryId)
    {
        return snapshot.Categories
            .FirstOrDefault(category =>
                string.Equals(
                    category.Name,
                    sheetName,
                    StringComparison.Ordinal))
            ?.Settings
            .FirstOrDefault(setting =>
                string.Equals(
                    setting.Id,
                    entryId,
                    StringComparison.Ordinal));
    }

    private static void RemoveEmptySnapshotContainers(
        ModificationSnapshotModel snapshot)
    {
        foreach (ModificationSnapshotCategoryModel category in
                 snapshot.Categories)
        {
            category.Settings.RemoveAll(setting =>
                setting.Properties.Count == 0);
        }

        snapshot.Categories.RemoveAll(category =>
            category.Settings.Count == 0);
    }
}
