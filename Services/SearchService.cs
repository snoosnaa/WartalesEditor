using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public class SearchService
{
    public List<SearchResultModel> Search(
    ProjectModel? project,
    string searchText,
    LocalizationService? localizationService = null)
    {
        List<SearchResultModel> results = new();

        if (project == null)
            return results;

        if (string.IsNullOrWhiteSpace(searchText))
            return results;

        foreach (SheetModel sheet in project.Sheets)
        {
            foreach (EntryModel entry in sheet.Entries)
            {
                bool matched = false;

                string localizedName =
                    localizationService?.GetLocalizedName(entry.DisplayName)
                    ?? string.Empty;

                // Search the internal Setting name or localized display name.
                if (entry.DisplayName.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    localizedName.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchResultModel
                    {
                        CategoryName = sheet.Name,
                        SettingName = entry.DisplayName,
                        LocalizedName = localizedName,
                        Category = sheet,
                        Setting = entry
                    });

                    matched = true;
                }

                // Search each Property name and value
                if (!matched)
                {
                    foreach (PropertyModel property in entry.Properties)
                    {
                        if (property.Name.Contains(
                                searchText,
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            property.Value?.ToString()?.Contains(
                                searchText,
                                StringComparison.OrdinalIgnoreCase) == true)
                        {
                            results.Add(new SearchResultModel
                            {
                                CategoryName = sheet.Name,
                                SettingName = entry.DisplayName,
                                LocalizedName = localizedName,
                                MatchedProperty = property.Name,
                                Category = sheet,
                                Setting = entry
                            });

                            break;
                        }
                    }
                }
            }
        }

        return results;
    }
}
