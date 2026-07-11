using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace WartalesEditor.Services;

public class LocalizationService
{
    private readonly Dictionary<string, string> localizedNames =
        new(StringComparer.OrdinalIgnoreCase);

    public int EntryCount => localizedNames.Count;

    public void Load(string fileName)
    {
        localizedNames.Clear();

        XDocument document = XDocument.Load(fileName);

        XElement? root = document.Root;

        if (root == null)
            return;

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

                localizedNames[id] = localizedText;
            }
        }
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
}