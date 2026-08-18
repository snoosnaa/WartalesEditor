using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class EffectiveChangeCountService
{
    private const int UpgradeableEquipmentFlag = 128;

    private readonly CampFacilityJsonBuilder campBuilder;

    public EffectiveChangeCountService()
        : this(new CampFacilityJsonBuilder())
    {
    }

    public EffectiveChangeCountService(
        CampFacilityJsonBuilder campBuilder)
    {
        this.campBuilder = campBuilder
            ?? throw new ArgumentNullException(
                nameof(campBuilder));
    }

    public int Calculate(ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        HashSet<string> effectiveChanges =
            profile.Snapshot.Categories
                .SelectMany(category =>
                    category.Settings.SelectMany(setting =>
                        setting.Properties.Select(property =>
                            CreateIdentity(
                                category.Name,
                                setting.Id,
                                GetPropertyIdentity(property)))))
                .ToHashSet(StringComparer.Ordinal);

        AddRandomTraitExclusionChanges(
            profile.Snapshot,
            effectiveChanges);

        int count = effectiveChanges.Count;

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

    private static void AddRandomTraitExclusionChanges(
        ModificationSnapshotModel snapshot,
        ISet<string> effectiveChanges)
    {
        GameplayOperationStateModel? state =
            snapshot.GameplayOperationStates.SingleOrDefault(candidate =>
                candidate.OperationType ==
                ProgressionType.RandomTraitExclusions);
        if (state == null)
        {
            return;
        }

        HashSet<string> allowed =
            RandomTraitExclusionsService.ReadAllowedIds(state);

        foreach (JObject baseline in
                 state.BaselineArray.OfType<JObject>())
        {
            string? id = baseline.Value<string>("id");
            string? baselineState = baseline.Value<string>("doneState");
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(baselineState))
            {
                continue;
            }

            string expectedState = allowed.Contains(id)
                ? string.Equals(
                    baselineState,
                    "Absent",
                    StringComparison.Ordinal)
                    ? "Absent"
                    : "True"
                : "False";

            if (!string.Equals(
                    baselineState,
                    expectedState,
                    StringComparison.Ordinal))
            {
                effectiveChanges.Add(
                    CreateIdentity("trait", id, "done"));
            }
        }
    }

    public int Calculate(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        HashSet<string> modifiedProperties = project.Sheets
            .SelectMany(sheet => sheet.Entries.SelectMany(entry =>
                entry.Properties
                    .Where(property => property.IsModified)
                    .Select(property => CreateIdentity(
                        sheet.Name,
                        entry.Id,
                        property.EffectivePropertyPath))))
            .ToHashSet(StringComparer.Ordinal);

        return modifiedProperties.Count +
               (HasUnrepresentedRandomTraitExclusionChange(project)
                   ? 1
                   : 0);
    }

    public int Calculate(ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(mutationResult);

        return mutationResult.CreatedProperties
            .Concat(mutationResult.UpdatedProperties)
            .Concat(mutationResult.RemovedProperties)
            .Distinct()
            .Count();
    }

    public bool HasUnrepresentedRandomTraitExclusionChange(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        GameplayOperationStateService stateService = new();
        if (!stateService.IsStateModified(
                project,
                ProgressionType.RandomTraitExclusions))
        {
            return false;
        }

        GameplayOperationStateModel? state =
            project.GameplayOperationStates.FirstOrDefault(candidate =>
                candidate.OperationType ==
                ProgressionType.RandomTraitExclusions);
        if (state == null)
        {
            return false;
        }

        HashSet<string> owned = state.BaselineArray
            .OfType<JObject>()
            .Select(record => record.Value<string>("id") ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        SheetModel? traitSheet = project.Sheets.FirstOrDefault(sheet =>
            string.Equals(sheet.Name, "trait", StringComparison.Ordinal));

        return traitSheet == null || !traitSheet.Entries.Any(entry =>
            owned.Contains(entry.Id) &&
            entry.Properties.Any(property =>
                string.Equals(
                    property.EffectivePropertyPath,
                    "done",
                    StringComparison.Ordinal) &&
                property.IsModified));
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
                ["props.model"] = props["model"]!.DeepClone(),
                ["props.bonuses"] = props["bonuses"]!.DeepClone()
            };

        AddValues("tool", tool, ownedValues);
        AddValues("icon", icon, ownedValues);

        ModificationSnapshotSettingModel? setting =
            FindItemSetting(snapshot, entryId);

        return setting?.Properties
            .Where(property =>
                ownedValues.TryGetValue(
                    GetPropertyIdentity(property),
                    out JToken? expected)
                &&
                JToken.DeepEquals(
                    property.CurrentValue,
                    expected))
            .Select(GetPropertyIdentity)
            .Distinct(StringComparer.Ordinal)
            .Count()
            ?? 0;
    }

    private static void AddValues(
        string parentPath,
        JObject source,
        IDictionary<string, JToken> values)
    {
        foreach (JProperty property in source.Properties())
        {
            values[$"{parentPath}.{property.Name}"] =
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
            .SelectMany(setting => setting.Properties
                .Where(IsUpgradeOwnedFlagChange)
                .Select(property => CreateIdentity(
                    "item",
                    setting.Id,
                    GetPropertyIdentity(property))))
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    private static bool IsUpgradeOwnedFlagChange(
        ModificationSnapshotPropertyModel property)
    {
        if (!string.Equals(
                property.Name,
                "flags",
                StringComparison.Ordinal)
            ||
            !string.Equals(
                property.PropertyPath,
                "props.flags",
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

    private static string GetPropertyIdentity(
        ModificationSnapshotPropertyModel property) =>
        string.IsNullOrWhiteSpace(property.PropertyPath)
            ? property.Name
            : property.PropertyPath;

    private static string CreateIdentity(
        string categoryName,
        string settingId,
        string propertyPath) =>
        $"{categoryName}\u001f{settingId}\u001f{propertyPath}";

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
