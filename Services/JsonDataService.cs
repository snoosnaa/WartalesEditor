using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using WartalesEditor.Models;
using System;
using System.Linq;

namespace WartalesEditor.Services;

public class JsonDataService
{
    public string Load(string fileName)
    {
        return File.ReadAllText(fileName);
    }

    public void SaveProject(ProjectModel project, string fileName)
    {
        File.WriteAllText(
            fileName,
            project.RootDocument.ToString(Newtonsoft.Json.Formatting.Indented));
    }

    public bool SetFirstStartingPartyMemberToRanger(ProjectModel project)
    {
        JArray? sheets = (JArray?)project.RootDocument["sheets"];

        if (sheets == null)
            return false;

        JObject? unitPatternSheet = sheets
            .OfType<JObject>()
            .FirstOrDefault(sheet =>
                string.Equals(
                    (string?)sheet["name"],
                    "unitPattern",
                    StringComparison.Ordinal));

        JArray? lines = (JArray?)unitPatternSheet?["lines"];

        if (lines == null)
            return false;

        JObject? startingParty = lines
            .OfType<JObject>()
            .FirstOrDefault(line =>
                string.Equals(
                    (string?)line["id"],
                    "PlayerStartAdventurer",
                    StringComparison.Ordinal));

        JArray? types = (JArray?)startingParty?["types"];

        if (types == null)
            return false;

        JObject? firstPartyMember = types
            .OfType<JObject>()
            .FirstOrDefault(partyMember =>
                (int?)partyMember["slot"] == 0);

        if (firstPartyMember == null)
            return false;

        firstPartyMember["unitClass"] = "Rogue";

        project.IsModified = true;

        return true;
    }

    public int GetSheetCount(string json)
    {
        JObject root = JObject.Parse(json);

        JArray? sheets = (JArray?)root["sheets"];

        return sheets?.Count ?? 0;
    }
    public List<string> GetSheetNames(string json)
    {
        List<string> names = new();

        JObject root = JObject.Parse(json);

        JArray? sheets = (JArray?)root["sheets"];

        if (sheets == null)
            return names;

        foreach (JObject sheet in sheets)
        {
            string? name = (string?)sheet["name"];

            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names;
    }
    public ProjectModel LoadProject(string fileName)
    {
        string json = Load(fileName);

        JObject root = JObject.Parse(json);

        ProjectModel project = new()
        {
            FileName = fileName,
            OriginalJson = json,
            RootDocument = root
        };

        JArray? sheets = (JArray?)root["sheets"];

        if (sheets == null)
            return project;

        foreach (JObject sheet in sheets)
        {
            string? name = (string?)sheet["name"];

            if (string.IsNullOrWhiteSpace(name))
                continue;

            SheetModel sheetModel = new()
            {
                Name = name
            };

            JArray? entries = (JArray?)sheet["lines"];

            if (entries != null)
            {
                int entryNumber = 1;

                foreach (JObject entry in entries)
                {
                    string displayName = entry["id"]?.ToString() ?? entryNumber.ToString();

                    EntryModel entryModel = new()
                    {
                        Id = entryNumber.ToString(),
                        DisplayName = displayName
                    };

                    foreach (JProperty property in entry.Properties())
                    {
                        entryModel.Properties.Add(new PropertyModel
                        {
                            Name = property.Name,
                            Value = property.Value.ToString(),
                            SourceProperty = property
                        });
                    }

                    sheetModel.Entries.Add(entryModel);

                    entryNumber++;
                }
            }

            project.Sheets.Add(sheetModel);
        }

        return project;
    }
}