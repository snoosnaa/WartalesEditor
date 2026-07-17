using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public class JsonDataService
{
    public string Load(string fileName)
    {
        return File.ReadAllText(fileName);
    }

    public string SerializeProject(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (project.RootDocument == null)
        {
            throw new InvalidOperationException(
                "The project does not contain a root JSON document.");
        }

        return project.RootDocument.ToString(
            Formatting.Indented);
    }

    public bool TrySerializeProject(
        ProjectModel project,
        out string serializedProject,
        out string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(project);

        try
        {
            serializedProject =
                SerializeProject(project);

            errorMessage =
                string.Empty;

            return true;
        }
        catch (Exception exception)
        {
            serializedProject =
                string.Empty;

            errorMessage =
                exception.Message;

            return false;
        }
    }

    public void SaveProject(
        ProjectModel project,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(project);

        string serializedProject =
            SerializeProject(project);

        File.WriteAllText(
            fileName,
            serializedProject);

        foreach (SheetModel sheet in project.Sheets)
        {
            foreach (EntryModel entry in sheet.Entries)
            {
                foreach (PropertyModel property
                         in entry.Properties)
                {
                    property.AcceptCurrentValue();
                }
            }
        }

        project.IsModified = false;
    }

    public bool SetFirstStartingPartyMemberToRanger(
        ProjectModel project)
    {
        JArray? sheets =
            (JArray?)project.RootDocument["sheets"];

        if (sheets == null)
            return false;

        JObject? unitPatternSheet = sheets
            .OfType<JObject>()
            .FirstOrDefault(sheet =>
                string.Equals(
                    (string?)sheet["name"],
                    "unitPattern",
                    StringComparison.Ordinal));

        JArray? lines =
            (JArray?)unitPatternSheet?["lines"];

        if (lines == null)
            return false;

        JObject? startingParty = lines
            .OfType<JObject>()
            .FirstOrDefault(line =>
                string.Equals(
                    (string?)line["id"],
                    "PlayerStartAdventurer",
                    StringComparison.Ordinal));

        JArray? types =
            (JArray?)startingParty?["types"];

        if (types == null)
            return false;

        JObject? firstPartyMember = types
            .OfType<JObject>()
            .FirstOrDefault(partyMember =>
                (int?)partyMember["slot"] == 0);

        if (firstPartyMember == null)
            return false;

        firstPartyMember["unitClass"] =
            "Rogue";

        project.IsModified =
            true;

        return true;
    }

    public int GetSheetCount(
        string json)
    {
        JObject root =
            JObject.Parse(json);

        JArray? sheets =
            (JArray?)root["sheets"];

        return sheets?.Count
            ?? 0;
    }

    public List<string> GetSheetNames(
        string json)
    {
        List<string> names =
            new();

        JObject root =
            JObject.Parse(json);

        JArray? sheets =
            (JArray?)root["sheets"];

        if (sheets == null)
            return names;

        foreach (JObject sheet in sheets)
        {
            string? name =
                (string?)sheet["name"];

            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    public ProjectModel LoadProject(
        string fileName)
    {
        string json =
            Load(fileName);

        JObject root =
            JObject.Parse(json);

        ProjectModel project =
            new()
            {
                FileName =
                    fileName,

                OriginalJson =
                    json,

                RootDocument =
                    root
            };

        JArray? sheets =
            (JArray?)root["sheets"];

        if (sheets == null)
            return project;

        foreach (JObject sheet in sheets)
        {
            string? name =
                (string?)sheet["name"];

            if (string.IsNullOrWhiteSpace(name))
                continue;

            SheetModel sheetModel =
                new()
                {
                    Name =
                        name
                };

            JArray? entries =
                (JArray?)sheet["lines"];

            if (entries != null)
            {
                int entryNumber =
                    1;

                foreach (JObject entry in entries)
                {
                    string displayName =
                        entry["id"]?.ToString()
                        ?? entryNumber.ToString();

                    EntryModel entryModel =
                        new()
                        {
                            Id =
                                entryNumber.ToString(),

                            DisplayName =
                                displayName
                        };

                    foreach (JProperty property
                             in entry.Properties())
                    {
                        PropertyModel propertyModel =
                            new()
                            {
                                SheetName =
                                    name,

                                Name =
                                    property.Name,

                                Value =
                                    property.Value.ToString(),

                                SourceProperty =
                                    property
                            };

                        propertyModel
                            .CaptureOriginalValue();

                        entryModel.Properties.Add(
                            propertyModel);
                    }

                    sheetModel.Entries.Add(
                        entryModel);

                    entryNumber++;
                }
            }

            project.Sheets.Add(
                sheetModel);
        }

        project.IsModified =
            false;

        return project;
    }
}