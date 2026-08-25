using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace WartalesEditor.Services;

public class LocalizationService
{
    private Dictionary<string, string> localizedNames =
        new(StringComparer.OrdinalIgnoreCase);

    public int EntryCount => localizedNames.Count;

    public void Load(string fileName)
    {
        Apply(
            Prepare(fileName));
    }

    internal LocalizationPreparation Prepare(
        string fileName)
    {
        return Prepare(
            XDocument.Load(fileName));
    }

    internal LocalizationPreparation Prepare(
        XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Dictionary<string, string> preparedNames =
            new(StringComparer.OrdinalIgnoreCase);

        XElement? root = document.Root;

        if (root == null)
            return new LocalizationPreparation(
                preparedNames);

        foreach (XElement sheet in root.Elements("sheet"))
        {
            foreach (XElement entry in sheet.Elements())
            {
                string id = entry.Name.LocalName;

                string? localizedText = null;

                // Prefer the most common display fields.
                string[] preferredFields =
                {
                    "name",
                    "text",
                    "title"
                };

                foreach (string field in preferredFields)
                {
                    XElement? element = entry.Element(field);

                    if (element != null &&
                        !string.IsNullOrWhiteSpace(element.Value))
                    {
                        localizedText = element.Value.Trim();
                        break;
                    }
                }

                // If none of the preferred fields exist,
                // use the first non-empty child element.
                if (localizedText == null)
                {
                    foreach (XElement child in entry.Elements())
                    {
                        if (!string.IsNullOrWhiteSpace(child.Value))
                        {
                            localizedText = child.Value.Trim();
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(id) ||
                    string.IsNullOrWhiteSpace(localizedText))
                {
                    continue;
                }

                preparedNames[id] = localizedText;
            }
        }

        return new LocalizationPreparation(
            preparedNames);
    }

    internal LocalizationPreparation Capture()
    {
        return new LocalizationPreparation(
            new Dictionary<string, string>(
                localizedNames,
                StringComparer.OrdinalIgnoreCase));
    }

    internal void Apply(
        LocalizationPreparation preparation)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        localizedNames = preparation.Names;
    }

    public string? GetLocalizedName(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return localizedNames.TryGetValue(id, out string? value)
            ? value
            : null;
    }

    public bool Contains(string id)
    {
        return !string.IsNullOrWhiteSpace(id) &&
               localizedNames.ContainsKey(id);
    }

    public void Clear()
    {
        localizedNames =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed class LocalizationPreparation
{
    internal LocalizationPreparation(
        Dictionary<string, string> names)
    {
        Names = names;
    }

    internal Dictionary<string, string> Names { get; }
}
