using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModificationSnapshotMatcher
{
    public ModificationMatchResultModel Match(
        ProjectModel targetProject,
        ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            snapshot);

        List<ModificationMatchItemModel> results =
            new();

        foreach (ModificationSnapshotCategoryModel
                 snapshotCategory in snapshot.Categories)
        {
            MatchCategory(
                targetProject,
                snapshotCategory,
                results);
        }

        return new ModificationMatchResultModel(
            results);
    }

    private static void MatchCategory(
        ProjectModel targetProject,
        ModificationSnapshotCategoryModel snapshotCategory,
        ICollection<ModificationMatchItemModel> results)
    {
        List<SheetModel> categoryMatches =
            targetProject.Sheets
                .Where(category =>
                    string.Equals(
                        category.Name,
                        snapshotCategory.Name,
                        StringComparison.Ordinal))
                .ToList();

        if (categoryMatches.Count == 0)
        {
            AddCategoryFailureResults(
                snapshotCategory,
                ModificationMatchStatus.CategoryNotFound,
                $"Category '{snapshotCategory.Name}' " +
                "was not found in the target project.",
                results);

            return;
        }

        if (categoryMatches.Count > 1)
        {
            AddCategoryFailureResults(
                snapshotCategory,
                ModificationMatchStatus.CategoryAmbiguous,
                $"Category '{snapshotCategory.Name}' matched " +
                $"{categoryMatches.Count} target categories.",
                results);

            return;
        }

        SheetModel targetCategory =
            categoryMatches[0];

        foreach (ModificationSnapshotSettingModel
                 snapshotSetting in snapshotCategory.Settings)
        {
            MatchSetting(
                targetCategory,
                snapshotCategory,
                snapshotSetting,
                results);
        }
    }

    private static void MatchSetting(
        SheetModel targetCategory,
        ModificationSnapshotCategoryModel snapshotCategory,
        ModificationSnapshotSettingModel snapshotSetting,
        ICollection<ModificationMatchItemModel> results)
    {
        if (string.IsNullOrWhiteSpace(
                snapshotSetting.Id))
        {
            AddSettingFailureResults(
                snapshotCategory,
                snapshotSetting,
                targetCategory,
                ModificationMatchStatus
                    .SettingIdentifierMissing,
                "The snapshot setting does not contain an " +
                "internal setting identifier.",
                results);

            return;
        }

        List<EntryModel> settingMatches =
            FindStableSettingMatches(
                targetCategory,
                snapshotSetting);

        bool usedLegacyMatching =
            false;

        if (settingMatches.Count == 0 &&
            CanUseLegacySettingMatch(
                snapshotSetting))
        {
            settingMatches =
                FindLegacySettingMatches(
                    targetCategory,
                    snapshotSetting);

            usedLegacyMatching =
                settingMatches.Count > 0;
        }

        if (settingMatches.Count == 0)
        {
            AddSettingFailureResults(
                snapshotCategory,
                snapshotSetting,
                targetCategory,
                ModificationMatchStatus.SettingNotFound,
                $"Setting ID '{snapshotSetting.Id}' was not " +
                $"found in category " +
                $"'{snapshotCategory.Name}'.",
                results);

            return;
        }

        if (settingMatches.Count > 1)
        {
            string matchingMethod =
                usedLegacyMatching
                    ? "legacy display identifier"
                    : "setting identifier";

            AddSettingFailureResults(
                snapshotCategory,
                snapshotSetting,
                targetCategory,
                ModificationMatchStatus.SettingAmbiguous,
                $"Snapshot {matchingMethod} matched " +
                $"{settingMatches.Count} settings in category " +
                $"'{snapshotCategory.Name}'.",
                results);

            return;
        }

        EntryModel targetSetting =
            settingMatches[0];

        foreach (ModificationSnapshotPropertyModel
                 snapshotProperty in snapshotSetting.Properties)
        {
            MatchProperty(
                targetCategory,
                targetSetting,
                snapshotCategory,
                snapshotSetting,
                snapshotProperty,
                results,
                usedLegacyMatching);
        }
    }

    private static List<EntryModel>
        FindStableSettingMatches(
            SheetModel targetCategory,
            ModificationSnapshotSettingModel snapshotSetting)
    {
        return targetCategory.Entries
            .Where(setting =>
                string.Equals(
                    setting.Id,
                    snapshotSetting.Id,
                    StringComparison.Ordinal))
            .ToList();
    }

    private static List<EntryModel>
        FindLegacySettingMatches(
            SheetModel targetCategory,
            ModificationSnapshotSettingModel snapshotSetting)
    {
        return targetCategory.Entries
            .Where(setting =>
                string.Equals(
                    setting.DisplayName,
                    snapshotSetting.DisplayName,
                    StringComparison.Ordinal))
            .ToList();
    }

    private static bool CanUseLegacySettingMatch(
        ModificationSnapshotSettingModel snapshotSetting)
    {
        if (string.IsNullOrWhiteSpace(
                snapshotSetting.DisplayName))
        {
            return false;
        }

        if (string.Equals(
                snapshotSetting.Id,
                snapshotSetting.DisplayName,
                StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(
                   snapshotSetting.Id,
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out int legacyEntryNumber)
               &&
               legacyEntryNumber > 0;
    }

    private static void MatchProperty(
        SheetModel targetCategory,
        EntryModel targetSetting,
        ModificationSnapshotCategoryModel snapshotCategory,
        ModificationSnapshotSettingModel snapshotSetting,
        ModificationSnapshotPropertyModel snapshotProperty,
        ICollection<ModificationMatchItemModel> results,
        bool usedLegacyMatching)
    {
        List<PropertyModel> propertyMatches =
            targetSetting.Properties
                .Where(property =>
                    string.IsNullOrWhiteSpace(snapshotProperty.PropertyPath)
                        ? string.Equals(
                            property.Name,
                            snapshotProperty.Name,
                            StringComparison.Ordinal)
                        : string.Equals(
                            property.EffectivePropertyPath,
                            snapshotProperty.PropertyPath,
                            StringComparison.Ordinal))
                .ToList();

        if (propertyMatches.Count == 0)
        {
            results.Add(
                new ModificationMatchItemModel(
                    snapshotCategory,
                    snapshotSetting,
                    snapshotProperty,
                    ModificationMatchStatus.PropertyNotFound,
                    $"Property '{GetPropertyIdentity(snapshotProperty)}' was not " +
                    $"found in setting ID " +
                    $"'{targetSetting.Id}'.",
                    targetCategory,
                    targetSetting));

            return;
        }

        if (propertyMatches.Count > 1)
        {
            results.Add(
                new ModificationMatchItemModel(
                    snapshotCategory,
                    snapshotSetting,
                    snapshotProperty,
                    ModificationMatchStatus.PropertyAmbiguous,
                    $"Property '{GetPropertyIdentity(snapshotProperty)}' matched " +
                    $"{propertyMatches.Count} properties in " +
                    $"setting ID '{targetSetting.Id}'.",
                    targetCategory,
                    targetSetting));

            return;
        }

        string reason =
            usedLegacyMatching
                ? "Exact property match found using the " +
                  "legacy snapshot setting identifier."
                : "Exact match found.";

        results.Add(
            new ModificationMatchItemModel(
                snapshotCategory,
                snapshotSetting,
                snapshotProperty,
                ModificationMatchStatus.Matched,
                reason,
                targetCategory,
                targetSetting,
            propertyMatches[0]));
    }

    private static string GetPropertyIdentity(
        ModificationSnapshotPropertyModel property) =>
        string.IsNullOrWhiteSpace(property.PropertyPath)
            ? property.Name
            : property.PropertyPath;

    private static void AddCategoryFailureResults(
        ModificationSnapshotCategoryModel snapshotCategory,
        ModificationMatchStatus status,
        string reason,
        ICollection<ModificationMatchItemModel> results)
    {
        foreach (ModificationSnapshotSettingModel
                 snapshotSetting in snapshotCategory.Settings)
        {
            foreach (ModificationSnapshotPropertyModel
                     snapshotProperty in
                     snapshotSetting.Properties)
            {
                results.Add(
                    new ModificationMatchItemModel(
                        snapshotCategory,
                        snapshotSetting,
                        snapshotProperty,
                        status,
                        reason));
            }
        }
    }

    private static void AddSettingFailureResults(
        ModificationSnapshotCategoryModel snapshotCategory,
        ModificationSnapshotSettingModel snapshotSetting,
        SheetModel targetCategory,
        ModificationMatchStatus status,
        string reason,
        ICollection<ModificationMatchItemModel> results)
    {
        foreach (ModificationSnapshotPropertyModel
                 snapshotProperty in
                 snapshotSetting.Properties)
        {
            results.Add(
                new ModificationMatchItemModel(
                    snapshotCategory,
                    snapshotSetting,
                    snapshotProperty,
                    status,
                    reason,
                    targetCategory));
        }
    }
}
