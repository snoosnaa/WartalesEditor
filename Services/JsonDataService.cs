using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public class JsonDataService
{
    public string Load(string fileName)
    {
        return File.ReadAllText(fileName);
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
            FileName = fileName
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
                            Value = property.Value.ToString()
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