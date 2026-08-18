using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ProfileSnapshotReconciliationService
{
    private readonly SnapshotPropertyResolutionService resolutionService;

    public ProfileSnapshotReconciliationService()
        : this(new SnapshotPropertyResolutionService())
    {
    }

    public ProfileSnapshotReconciliationService(
        SnapshotPropertyResolutionService resolutionService)
    {
        this.resolutionService = resolutionService
            ?? throw new ArgumentNullException(nameof(resolutionService));
    }

    public void Reconcile(
        ProjectModel project,
        ModificationSnapshotModel previousSnapshot,
        ModificationSnapshotModel currentSnapshot)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(previousSnapshot);
        ArgumentNullException.ThrowIfNull(currentSnapshot);

        foreach (ModificationSnapshotCategoryModel previousCategory in
                 previousSnapshot.Categories)
        {
            foreach (ModificationSnapshotSettingModel previousSetting in
                     previousCategory.Settings)
            {
                foreach (ModificationSnapshotPropertyModel previousProperty in
                         previousSetting.Properties)
                {
                    ReconcileProperty(
                        project,
                        currentSnapshot,
                        previousCategory,
                        previousSetting,
                        previousProperty);
                }
            }
        }

        RemoveEmptyContainers(currentSnapshot);
    }

    private void ReconcileProperty(
        ProjectModel project,
        ModificationSnapshotModel currentSnapshot,
        ModificationSnapshotCategoryModel previousCategory,
        ModificationSnapshotSettingModel previousSetting,
        ModificationSnapshotPropertyModel previousProperty)
    {
        EntryModel? liveSetting = FindSetting(
            project,
            previousCategory.Name,
            previousSetting.Id);
        SnapshotPropertyResolutionResult liveResolution =
            Resolve(liveSetting, previousProperty);

        ThrowIfAmbiguous(
            liveResolution,
            previousCategory.Name,
            previousSetting.Id,
            previousProperty);
        PropertyModel? liveProperty = liveResolution.Property;

        if (liveProperty == null)
        {
            bool? originalPropertyExisted =
                SnapshotPropertyHistoryService
                    .GetOriginalPropertyExistence(previousProperty);

            if (originalPropertyExisted != false)
            {
                throw new InvalidOperationException(
                    $"Profile property '{GetPropertyIdentity(previousProperty)}' " +
                    $"in '{previousCategory.Name}/{previousSetting.Id}' is " +
                    "missing from the current project. Its historical structural " +
                    (originalPropertyExisted == true
                        ? "presence is known, and arbitrary removal of a previously " +
                          "existing property is not supported by profiles."
                        : "presence cannot be distinguished from a JSON null value, " +
                          "so Update Profile cannot safely treat it as restoration " +
                          "to absence."));
            }

            RemoveProperty(
                currentSnapshot,
                previousCategory.Name,
                previousSetting.Id,
                GetPropertyIdentity(previousProperty));
            return;
        }

        JToken liveValue = liveProperty.GetCurrentValueSnapshot();

        if (JToken.DeepEquals(
                liveValue,
                previousProperty.OriginalValue))
        {
            RemoveProperty(
                currentSnapshot,
                previousCategory.Name,
                previousSetting.Id,
                GetPropertyIdentity(previousProperty),
                liveProperty.EffectivePropertyPath);
            return;
        }

        ModificationSnapshotSettingModel currentSetting = EnsureSetting(
            currentSnapshot,
            previousCategory,
            previousSetting);

        RemoveProperties(
            currentSetting,
            GetPropertyIdentity(previousProperty),
            liveProperty.EffectivePropertyPath);

        currentSetting.Properties.Add(
            new ModificationSnapshotPropertyModel
            {
                Name = previousProperty.Name,
                PropertyPath = liveProperty.EffectivePropertyPath,
                OriginalPropertyExisted =
                    SnapshotPropertyHistoryService
                        .GetOriginalPropertyExistence(previousProperty),
                OriginalValue = previousProperty.OriginalValue.DeepClone(),
                CurrentValue = liveValue
            });
    }

    private SnapshotPropertyResolutionResult Resolve(
        EntryModel? setting,
        ModificationSnapshotPropertyModel property)
    {
        return setting == null
            ? new SnapshotPropertyResolutionResult(
                SnapshotPropertyResolutionStatus.NotFound,
                Array.Empty<PropertyModel>())
            : resolutionService.Resolve(setting, property);
    }

    private static EntryModel? FindSetting(
        ProjectModel project,
        string categoryName,
        string settingId)
    {
        return project.Sheets
            .SingleOrDefault(category =>
                string.Equals(
                    category.Name,
                    categoryName,
                    StringComparison.Ordinal))
            ?.Entries
            .SingleOrDefault(entry =>
                string.Equals(
                    entry.Id,
                    settingId,
                    StringComparison.Ordinal));
    }

    private static void ThrowIfAmbiguous(
        SnapshotPropertyResolutionResult resolution,
        string categoryName,
        string settingId,
        ModificationSnapshotPropertyModel property)
    {
        if (resolution.Status != SnapshotPropertyResolutionStatus.Ambiguous)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Legacy profile property '{GetPropertyIdentity(property)}' in " +
            $"'{categoryName}/{settingId}' matches multiple project properties. " +
            "Update Profile cannot safely choose a target.");
    }

    private static ModificationSnapshotSettingModel EnsureSetting(
        ModificationSnapshotModel snapshot,
        ModificationSnapshotCategoryModel previousCategory,
        ModificationSnapshotSettingModel previousSetting)
    {
        ModificationSnapshotCategoryModel? category =
            snapshot.Categories.SingleOrDefault(candidate =>
                string.Equals(
                    candidate.Name,
                    previousCategory.Name,
                    StringComparison.Ordinal));

        if (category == null)
        {
            category = new ModificationSnapshotCategoryModel
            {
                Name = previousCategory.Name
            };
            snapshot.Categories.Add(category);
        }

        ModificationSnapshotSettingModel? setting =
            category.Settings.SingleOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    previousSetting.Id,
                    StringComparison.Ordinal));

        if (setting != null)
        {
            return setting;
        }

        setting = new ModificationSnapshotSettingModel
        {
            Id = previousSetting.Id,
            Name = previousSetting.Name,
            DisplayName = previousSetting.DisplayName
        };
        category.Settings.Add(setting);
        return setting;
    }

    private static void RemoveProperty(
        ModificationSnapshotModel snapshot,
        string categoryName,
        string settingId,
        params string[] propertyIdentities)
    {
        ModificationSnapshotSettingModel? setting = snapshot.Categories
            .SingleOrDefault(category =>
                string.Equals(
                    category.Name,
                    categoryName,
                    StringComparison.Ordinal))
            ?.Settings
            .SingleOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    settingId,
                    StringComparison.Ordinal));

        if (setting != null)
        {
            RemoveProperties(setting, propertyIdentities);
        }
    }

    private static void RemoveProperties(
        ModificationSnapshotSettingModel setting,
        params string[] propertyIdentities)
    {
        HashSet<string> identities = propertyIdentities
            .ToHashSet(StringComparer.Ordinal);

        setting.Properties.RemoveAll(property =>
            identities.Contains(GetPropertyIdentity(property)));
    }

    private static string GetPropertyIdentity(
        ModificationSnapshotPropertyModel property) =>
        string.IsNullOrWhiteSpace(property.PropertyPath)
            ? property.Name
            : property.PropertyPath;

    private static void RemoveEmptyContainers(
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
