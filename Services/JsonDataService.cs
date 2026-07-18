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
    private readonly ProjectModelFactory
        projectModelFactory;

    public JsonDataService()
        : this(
            new ProjectModelFactory())
    {
    }

    public JsonDataService(
        ProjectModelFactory projectModelFactory)
    {
        this.projectModelFactory =
            projectModelFactory
            ?? throw new ArgumentNullException(
                nameof(projectModelFactory));
    }

    public string Load(
        string fileName)
    {
        return File.ReadAllText(
            fileName);
    }

    public string SerializeProject(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(
            project);

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
        ArgumentNullException.ThrowIfNull(
            project);

        try
        {
            serializedProject =
                SerializeProject(
                    project);

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
        ArgumentNullException.ThrowIfNull(
            project);

        string serializedProject =
            SerializeProject(
                project);

        File.WriteAllText(
            fileName,
            serializedProject);

        foreach (SheetModel sheet in
                 project.Sheets)
        {
            foreach (EntryModel entry in
                     sheet.Entries)
            {
                foreach (PropertyModel property in
                         entry.Properties)
                {
                    property.AcceptCurrentValue();
                }
            }
        }

        project.IsModified =
            false;
    }

    public bool SetFirstStartingPartyMemberToRanger(
        ProjectModel project)
    {
        JArray? sheets =
            project.RootDocument["sheets"]
                as JArray;

        if (sheets == null)
            return false;

        JObject? unitPatternSheet =
            sheets
                .OfType<JObject>()
                .FirstOrDefault(sheet =>
                    string.Equals(
                        sheet["name"]?.ToString(),
                        "unitPattern",
                        StringComparison.Ordinal));

        JArray? lines =
            unitPatternSheet?["lines"]
                as JArray;

        if (lines == null)
            return false;

        JObject? startingParty =
            lines
                .OfType<JObject>()
                .FirstOrDefault(line =>
                    string.Equals(
                        line["id"]?.ToString(),
                        "PlayerStartAdventurer",
                        StringComparison.Ordinal));

        JArray? types =
            startingParty?["types"]
                as JArray;

        if (types == null)
            return false;

        JObject? firstPartyMember =
            types
                .OfType<JObject>()
                .FirstOrDefault(partyMember =>
                    partyMember["slot"]?.Value<int>()
                    == 0);

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
            JObject.Parse(
                json);

        JArray? sheets =
            root["sheets"]
                as JArray;

        return sheets?.Count
            ?? 0;
    }

    public List<string> GetSheetNames(
        string json)
    {
        List<string> names =
            new();

        JObject root =
            JObject.Parse(
                json);

        JArray? sheets =
            root["sheets"]
                as JArray;

        if (sheets == null)
            return names;

        foreach (JObject sheet in
                 sheets.OfType<JObject>())
        {
            string? name =
                sheet["name"]?.ToString();

            if (!string.IsNullOrWhiteSpace(
                    name))
            {
                names.Add(
                    name);
            }
        }

        return names;
    }

    public ProjectModel LoadProject(
        string fileName)
    {
        string json =
            Load(
                fileName);

        JObject root =
            JObject.Parse(
                json);

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

        JArray? sourceSheets =
            root["sheets"]
                as JArray;

        if (sourceSheets == null)
        {
            return project;
        }

        foreach (JObject sourceSheet in
                 sourceSheets.OfType<JObject>())
        {
            string? sheetName =
                sourceSheet["name"]?.ToString();

            if (string.IsNullOrWhiteSpace(
                    sheetName))
            {
                continue;
            }

            SheetModel sheetModel =
                projectModelFactory
                    .CreateSheetModel(
                        sourceSheet);

            project.Sheets.Add(
                sheetModel);
        }

        project.IsModified =
            false;

        return project;
    }
}