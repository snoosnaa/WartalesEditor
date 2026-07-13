using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ChangeSummaryService
{
    public IReadOnlyList<ChangeSummaryItemModel>
        BuildItems(
            ProjectModel project,
            ModificationSnapshotModel snapshot,
            LocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(
            localizationService);

        List<ChangeSummaryItemModel> items = new();

        foreach (ModificationSnapshotCategoryModel
                 snapshotCategory in snapshot.Categories)
        {
            SheetModel? category =
                FindCategory(
                    project,
                    snapshotCategory);

            if (category == null)
                continue;

            foreach (ModificationSnapshotSettingModel
                     snapshotSetting in
                     snapshotCategory.Settings)
            {
                EntryModel? setting =
                    FindSetting(
                        category,
                        snapshotSetting);

                if (setting == null)
                    continue;

                string settingName =
                    GetSettingName(
                        setting,
                        localizationService);

                foreach (ModificationSnapshotPropertyModel
                         snapshotProperty in
                         snapshotSetting.Properties)
                {
                    PropertyModel? property =
                        FindProperty(
                            setting,
                            snapshotProperty);

                    if (property == null)
                        continue;

                    items.Add(
                        new ChangeSummaryItemModel(
                            category,
                            setting,
                            property,
                            settingName,
                            FormatValue(
                                snapshotProperty
                                    .OriginalValue),
                            FormatValue(
                                snapshotProperty
                                    .CurrentValue)));
                }
            }
        }

        return items;
    }

    private static SheetModel? FindCategory(
        ProjectModel project,
        ModificationSnapshotCategoryModel snapshotCategory)
    {
        return project.Sheets.FirstOrDefault(category =>
            string.Equals(
                category.Name,
                snapshotCategory.Name,
                StringComparison.Ordinal));
    }

    private static EntryModel? FindSetting(
        SheetModel category,
        ModificationSnapshotSettingModel snapshotSetting)
    {
        if (!string.IsNullOrWhiteSpace(
                snapshotSetting.Id))
        {
            List<EntryModel> idMatches =
                category.Entries
                    .Where(entry =>
                        string.Equals(
                            entry.Id,
                            snapshotSetting.Id,
                            StringComparison.Ordinal))
                    .ToList();

            if (idMatches.Count == 1)
                return idMatches[0];

            EntryModel? exactIdMatch =
                idMatches.FirstOrDefault(entry =>
                    string.Equals(
                        entry.Name,
                        snapshotSetting.Name,
                        StringComparison.Ordinal)
                    &&
                    string.Equals(
                        entry.DisplayName,
                        snapshotSetting.DisplayName,
                        StringComparison.Ordinal));

            if (exactIdMatch != null)
                return exactIdMatch;
        }

        List<EntryModel> identityMatches =
            category.Entries
                .Where(entry =>
                    string.Equals(
                        entry.Name,
                        snapshotSetting.Name,
                        StringComparison.Ordinal)
                    &&
                    string.Equals(
                        entry.DisplayName,
                        snapshotSetting.DisplayName,
                        StringComparison.Ordinal))
                .ToList();

        if (identityMatches.Count == 1)
            return identityMatches[0];

        if (!string.IsNullOrWhiteSpace(
                snapshotSetting.Name))
        {
            List<EntryModel> nameMatches =
                category.Entries
                    .Where(entry =>
                        string.Equals(
                            entry.Name,
                            snapshotSetting.Name,
                            StringComparison.Ordinal))
                    .ToList();

            if (nameMatches.Count == 1)
                return nameMatches[0];
        }

        if (!string.IsNullOrWhiteSpace(
                snapshotSetting.DisplayName))
        {
            List<EntryModel> displayNameMatches =
                category.Entries
                    .Where(entry =>
                        string.Equals(
                            entry.DisplayName,
                            snapshotSetting.DisplayName,
                            StringComparison.Ordinal))
                    .ToList();

            if (displayNameMatches.Count == 1)
                return displayNameMatches[0];
        }

        return null;
    }

    private static PropertyModel? FindProperty(
        EntryModel setting,
        ModificationSnapshotPropertyModel snapshotProperty)
    {
        List<PropertyModel> matches =
            setting.Properties
                .Where(property =>
                    string.Equals(
                        property.Name,
                        snapshotProperty.Name,
                        StringComparison.Ordinal))
                .ToList();

        return matches.Count == 1
            ? matches[0]
            : null;
    }

    private static string GetSettingName(
        EntryModel setting,
        LocalizationService localizationService)
    {
        string localizedName =
            localizationService.GetLocalizedName(
                setting.DisplayName)
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(
                localizedName))
        {
            return localizedName;
        }

        if (!string.IsNullOrWhiteSpace(
                setting.DisplayName))
        {
            return setting.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(
                setting.Name))
        {
            return setting.Name;
        }

        return setting.Id;
    }

    private static string FormatValue(
        JToken? token)
    {
        if (token == null)
            return string.Empty;

        return token.Type switch
        {
            JTokenType.Null =>
                "null",

            JTokenType.String =>
                token.Value<string>()
                ?? string.Empty,

            JTokenType.Integer =>
                Convert.ToString(
                    token.Value<long>(),
                    CultureInfo.InvariantCulture)
                ?? string.Empty,

            JTokenType.Float =>
                Convert.ToString(
                    token.Value<double>(),
                    CultureInfo.InvariantCulture)
                ?? string.Empty,

            JTokenType.Boolean =>
                token.Value<bool>()
                    ? "true"
                    : "false",

            JTokenType.Array =>
                token.ToString(
                    Formatting.None),

            JTokenType.Object =>
                token.ToString(
                    Formatting.None),

            _ =>
                token.ToString()
        };
    }
}