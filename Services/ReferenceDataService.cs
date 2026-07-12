using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class ReferenceDataService
{
    private const char ReferenceKeySeparator = '\u001F';

    private readonly Dictionary<string, IReadOnlyList<ReferenceValueModel>>
        fallbackValues =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IReadOnlyList<ReferenceValueModel>>
        discoveredValues =
            new(StringComparer.OrdinalIgnoreCase);

    public static ReferenceDataService Instance { get; } = new();

    private ReferenceDataService()
    {
    }

    public void Initialize(ProjectModel? project)
    {
        discoveredValues.Clear();

        if (project == null)
            return;

        Dictionary<string, HashSet<string>> collectedValues =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (SheetModel sheet in project.Sheets)
        {
            foreach (EntryModel entry in sheet.Entries)
            {
                foreach (PropertyModel property in entry.Properties)
                {
                    JToken? token = property.SourceProperty?.Value;

                    if (token == null ||
                        !IsSupportedReferenceValue(token))
                    {
                        continue;
                    }

                    string value = GetTokenText(token);

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    string referenceKey =
                        CreateReferenceKey(
                            sheet.Name,
                            property.Name);

                    if (!collectedValues.TryGetValue(
                            referenceKey,
                            out HashSet<string>? values))
                    {
                        values = new HashSet<string>(
                            StringComparer.OrdinalIgnoreCase);

                        collectedValues[referenceKey] = values;
                    }

                    values.Add(value);
                }
            }
        }

        foreach ((string referenceKey, HashSet<string> values)
                 in collectedValues)
        {
            discoveredValues[referenceKey] = values
                .OrderBy(
                    value => value,
                    StringComparer.OrdinalIgnoreCase)
                .Select(value =>
                    new ReferenceValueModel(value))
                .ToArray();
        }
    }

    public IReadOnlyList<ReferenceValueModel> GetValues(
        string sheetName,
        string propertyName)
    {
        string referenceKey =
            CreateReferenceKey(
                sheetName,
                propertyName);

        if (discoveredValues.TryGetValue(
                referenceKey,
                out IReadOnlyList<ReferenceValueModel>? discovered)
            && discovered.Count > 0)
        {
            return discovered;
        }

        if (fallbackValues.TryGetValue(
                propertyName,
                out IReadOnlyList<ReferenceValueModel>? fallback)
            && fallback.Count > 0)
        {
            return fallback;
        }

        return Array.Empty<ReferenceValueModel>();
    }

    public bool HasValues(
        string sheetName,
        string propertyName)
    {
        return GetValues(
            sheetName,
            propertyName).Count > 0;
    }

    private static string CreateReferenceKey(
        string? sheetName,
        string? propertyName)
    {
        string normalizedSheetName =
            sheetName?.Trim() ?? string.Empty;

        string normalizedPropertyName =
            propertyName?.Trim() ?? string.Empty;

        return
            $"{normalizedSheetName}" +
            $"{ReferenceKeySeparator}" +
            $"{normalizedPropertyName}";
    }

    private static bool IsSupportedReferenceValue(JToken token)
    {
        return token.Type is
            JTokenType.String or
            JTokenType.Integer or
            JTokenType.Float or
            JTokenType.Boolean;
    }

    private static string GetTokenText(JToken token)
    {
        if (token is not JValue value)
            return token.ToString();

        return value.Value switch
        {
            null => string.Empty,

            IFormattable formattable =>
                formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture),

            _ => value.Value.ToString() ?? string.Empty
        };
    }
}