using System;
using System.Collections.Generic;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public class SearchService
{
    private const string ItemNamesScope =
        "Item Names";

    private const string ItemPropertyName =
        "item";

    public List<SearchResultModel> Search(
        ProjectModel? project,
        string searchText,
        LocalizationService? localizationService = null,
        string? searchScope = null)
    {
        List<SearchResultModel> results =
            new();

        if (project == null)
        {
            return results;
        }

        if (string.IsNullOrWhiteSpace(
                searchText))
        {
            return results;
        }

        bool searchItemNamesOnly =
            string.Equals(
                searchScope,
                ItemNamesScope,
                StringComparison.Ordinal);

        foreach (SheetModel sheet in project.Sheets)
        {
            foreach (EntryModel entry in sheet.Entries)
            {
                string localizedName =
                    localizationService
                        ?.GetLocalizedName(
                            entry.DisplayName)
                    ?? string.Empty;

                if (searchItemNamesOnly)
                {
                    AddItemNameMatch(
                        results,
                        sheet,
                        entry,
                        localizedName,
                        searchText);

                    continue;
                }

                AddGeneralMatch(
                    results,
                    sheet,
                    entry,
                    localizedName,
                    searchText);
            }
        }

        return results;
    }

    private static void AddItemNameMatch(
        ICollection<SearchResultModel> results,
        SheetModel sheet,
        EntryModel entry,
        string localizedName,
        string searchText)
    {
        foreach (PropertyModel property in entry.Properties)
        {
            if (!string.Equals(
                    property.Name,
                    ItemPropertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string propertyValue =
                property.Value?.ToString()
                ?? string.Empty;

            if (!propertyValue.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(
                new SearchResultModel
                {
                    CategoryName =
                        sheet.Name,
                    SettingName =
                        entry.DisplayName,
                    LocalizedName =
                        localizedName,
                    MatchedProperty =
                        property.Name,
                    MatchedValue =
                        propertyValue,
                    Category =
                        sheet,
                    Setting =
                        entry
                });

            return;
        }
    }

    private static void AddGeneralMatch(
        ICollection<SearchResultModel> results,
        SheetModel sheet,
        EntryModel entry,
        string localizedName,
        string searchText)
    {
        if (entry.DisplayName.Contains(
                searchText,
                StringComparison.OrdinalIgnoreCase)
            ||
            localizedName.Contains(
                searchText,
                StringComparison.OrdinalIgnoreCase))
        {
            results.Add(
                new SearchResultModel
                {
                    CategoryName =
                        sheet.Name,
                    SettingName =
                        entry.DisplayName,
                    LocalizedName =
                        localizedName,
                    Category =
                        sheet,
                    Setting =
                        entry
                });

            return;
        }

        foreach (PropertyModel property in entry.Properties)
        {
            string propertyValue =
                property.Value?.ToString()
                ?? string.Empty;

            if (!property.Name.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase)
                &&
                !propertyValue.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(
                new SearchResultModel
                {
                    CategoryName =
                        sheet.Name,
                    SettingName =
                        entry.DisplayName,
                    LocalizedName =
                        localizedName,
                    MatchedProperty =
                        property.Name,
                    Category =
                        sheet,
                    Setting =
                        entry
                });

            return;
        }
    }
}