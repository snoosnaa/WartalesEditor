using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ProfileEffectiveChangeCountService
{
    private const int UpgradeableEquipmentFlag = 128;

    private readonly CampFacilityJsonBuilder campBuilder;

    public ProfileEffectiveChangeCountService()
        : this(new CampFacilityJsonBuilder())
    {
    }

    public ProfileEffectiveChangeCountService(
        CampFacilityJsonBuilder campBuilder)
    {
        this.campBuilder = campBuilder
            ?? throw new ArgumentNullException(
                nameof(campBuilder));
    }

    public int Calculate(ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        int count = profile.Snapshot.Categories.Sum(category =>
            category.Settings.Sum(setting =>
                setting.Properties.Count));

        foreach (ProfileOperationRequestModel request in
                 profile.OperationRequests)
        {
            count += request.OperationId switch
            {
                ProfileOperationIds.AddCampFacilities =>
                    campBuilder.GetEffectivePropertyChangeCount()
                    - CountCampSnapshotOverlap(profile.Snapshot),

                ProfileOperationIds.UpgradeAllEquipment =>
                    UpgradeAllEquipmentTargetCatalog.Count
                    - CountUpgradeSnapshotOverlap(profile.Snapshot),

                _ => 0
            };
        }

        return Math.Max(0, count);
    }

    private int CountCampSnapshotOverlap(
        ModificationSnapshotModel snapshot)
    {
        return CountFacilityOverlap(
                   snapshot,
                   "Anvil",
                   campBuilder.BuildAnvilProps(
                       new JObject()),
                   campBuilder.BuildAnvilTool(),
                   campBuilder.BuildAnvilIcon())
               +
               CountFacilityOverlap(
                   snapshot,
                   "ApothecaryTable",
                   campBuilder.BuildApothecaryProps(
                       new JObject()),
                   campBuilder.BuildApothecaryTool(),
                   campBuilder.BuildApothecaryIcon());
    }

    private static int CountFacilityOverlap(
        ModificationSnapshotModel snapshot,
        string entryId,
        JObject props,
        JObject tool,
        JObject icon)
    {
        Dictionary<string, JToken> ownedValues =
            new(StringComparer.Ordinal)
            {
                ["model"] = props["model"]!.DeepClone(),
                ["bonuses"] = props["bonuses"]!.DeepClone()
            };

        AddValues(tool, ownedValues);
        AddValues(icon, ownedValues);

        ModificationSnapshotSettingModel? setting =
            FindItemSetting(snapshot, entryId);

        return setting?.Properties.Count(property =>
            ownedValues.TryGetValue(
                property.Name,
                out JToken? expected)
            &&
            JToken.DeepEquals(
                property.CurrentValue,
                expected))
            ?? 0;
    }

    private static void AddValues(
        JObject source,
        IDictionary<string, JToken> values)
    {
        foreach (JProperty property in source.Properties())
        {
            values[property.Name] =
                property.Value.DeepClone();
        }
    }

    private static int CountUpgradeSnapshotOverlap(
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
            return 0;
        }

        return itemCategory.Settings
            .Where(setting =>
                UpgradeAllEquipmentTargetCatalog.Contains(
                    setting.Id))
            .SelectMany(setting =>
                setting.Properties)
            .Count(IsUpgradeOwnedFlagChange);
    }

    private static bool IsUpgradeOwnedFlagChange(
        ModificationSnapshotPropertyModel property)
    {
        if (!string.Equals(
                property.Name,
                "flags",
                StringComparison.Ordinal)
            ||
            property.CurrentValue.Type != JTokenType.Integer
            ||
            property.OriginalValue.Type is not
                (JTokenType.Integer or JTokenType.Null))
        {
            return false;
        }

        int originalFlags =
            property.OriginalValue.Type == JTokenType.Integer
                ? property.OriginalValue.Value<int>()
                : 0;

        return property.CurrentValue.Value<int>() ==
               (originalFlags | UpgradeableEquipmentFlag);
    }

    private static ModificationSnapshotSettingModel?
        FindItemSetting(
            ModificationSnapshotModel snapshot,
            string entryId)
    {
        return snapshot.Categories
            .FirstOrDefault(category =>
                string.Equals(
                    category.Name,
                    "item",
                    StringComparison.Ordinal))
            ?.Settings
            .FirstOrDefault(setting =>
                string.Equals(
                    setting.Id,
                    entryId,
                    StringComparison.Ordinal));
    }
}
