using System;
using System.Collections.Generic;
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
            targetCategory.Entries
                .Where(setting =>
                    string.Equals(
                        setting.Id,
                        snapshotSetting.Id,
                        StringComparison.Ordinal))
                .ToList();

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
            AddSettingFailureResults(
                snapshotCategory,
                snapshotSetting,
                targetCategory,
                ModificationMatchStatus.SettingAmbiguous,
                $"Setting ID '{snapshotSetting.Id}' matched " +
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
                results);
        }
    }

    private static void MatchProperty(
        SheetModel targetCategory,
        EntryModel targetSetting,
        ModificationSnapshotCategoryModel snapshotCategory,
        ModificationSnapshotSettingModel snapshotSetting,
        ModificationSnapshotPropertyModel snapshotProperty,
        ICollection<ModificationMatchItemModel> results)
    {
        List<PropertyModel> propertyMatches =
            targetSetting.Properties
                .Where(property =>
                    string.Equals(
                        property.Name,
                        snapshotProperty.Name,
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
                    $"Property '{snapshotProperty.Name}' was not " +
                    $"found in setting ID " +
                    $"'{snapshotSetting.Id}'.",
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
                    $"Property '{snapshotProperty.Name}' matched " +
                    $"{propertyMatches.Count} properties in " +
                    $"setting ID '{snapshotSetting.Id}'.",
                    targetCategory,
                    targetSetting));

            return;
        }

        results.Add(
            new ModificationMatchItemModel(
                snapshotCategory,
                snapshotSetting,
                snapshotProperty,
                ModificationMatchStatus.Matched,
                "Exact match found.",
                targetCategory,
                targetSetting,
                propertyMatches[0]));
    }

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
