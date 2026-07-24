using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public class JsonDataService
{
    private readonly ProjectModelFactory
        projectModelFactory;

    private readonly GameplayOperationStatePersistenceService
        gameplayOperationStatePersistenceService;

    public JsonDataService()
        : this(
            new ProjectModelFactory(),
            new GameplayOperationStatePersistenceService())
    {
    }

    public JsonDataService(
        ProjectModelFactory projectModelFactory)
        : this(
            projectModelFactory,
            new GameplayOperationStatePersistenceService())
    {
    }

    public JsonDataService(
        ProjectModelFactory projectModelFactory,
        GameplayOperationStatePersistenceService
            gameplayOperationStatePersistenceService)
    {
        this.projectModelFactory =
            projectModelFactory
            ?? throw new ArgumentNullException(
                nameof(projectModelFactory));

        this.gameplayOperationStatePersistenceService =
            gameplayOperationStatePersistenceService
            ?? throw new ArgumentNullException(
                nameof(gameplayOperationStatePersistenceService));
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

        string fullFileName =
            Path.GetFullPath(fileName);

        string? directory =
            Path.GetDirectoryName(fullFileName);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string cdbTemporaryFile =
            fullFileName + ".tmp";

        string sidecarTemporaryFile =
            string.Empty;

        bool cdbCommitted = false;

        try
        {
            File.WriteAllText(
                cdbTemporaryFile,
                serializedProject,
                new UTF8Encoding(false));

            sidecarTemporaryFile =
                gameplayOperationStatePersistenceService
                    .WriteTemporary(
                        project,
                        fullFileName);

            File.Move(
                cdbTemporaryFile,
                fullFileName,
                overwrite: true);

            cdbCommitted = true;

            GameplayOperationStatePersistenceService
                .CommitTemporary(
                    sidecarTemporaryFile,
                    gameplayOperationStatePersistenceService
                        .GetSidecarPath(fullFileName));
        }
        catch (Exception exception)
        {
            GameplayOperationStatePersistenceService
                .TryDeleteTemporary(cdbTemporaryFile);

            if (!string.IsNullOrWhiteSpace(
                    sidecarTemporaryFile))
            {
                GameplayOperationStatePersistenceService
                    .TryDeleteTemporary(sidecarTemporaryFile);
            }

            if (cdbCommitted)
            {
                throw new ProjectPartialSaveException(
                    "The CDB was saved, but its required gameplay-" +
                    "operation state sidecar could not be saved. " +
                    "The project remains modified in memory so the " +
                    "save can be retried safely.",
                    exception);
            }

            throw;
        }

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

        project.IsGameplayOperationStateModified =
            false;

        project.FileName =
            fullFileName;
    }

    public void SaveGameplayOperationState(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.IsNullOrWhiteSpace(project.FileName))
        {
            throw new InvalidOperationException(
                "The project must have a file path before gameplay-" +
                "operation state can be saved.");
        }

        gameplayOperationStatePersistenceService.Save(
            project,
            project.FileName);

        project.IsGameplayOperationStateModified =
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

        gameplayOperationStatePersistenceService
            .LoadIntoProject(
                project,
                fileName);

        return project;
    }
}
