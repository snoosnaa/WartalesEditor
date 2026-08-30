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

    private readonly CdbGenerationIdentityService
        cdbGenerationIdentityService;

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

        cdbGenerationIdentityService =
            new CdbGenerationIdentityService();
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

        byte[] persistedBytes =
            new UTF8Encoding(false).GetBytes(
                serializedProject);

        string targetContentIdentity =
            cdbGenerationIdentityService.Calculate(
                persistedBytes);

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
            File.WriteAllBytes(
                cdbTemporaryFile,
                persistedBytes);

            sidecarTemporaryFile =
                gameplayOperationStatePersistenceService
                    .WriteTemporary(
                        project,
                        fullFileName,
                        targetContentIdentity);

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

            gameplayOperationStatePersistenceService
                .AcceptCurrentStates(project);

            project.AdvanceCurrentContentIdentity(
                targetContentIdentity);
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
        ReferenceProjectLoadResult loaded =
            LoadReferenceProjectCore(fileName);

        ProjectModel project = loaded.Project;

        gameplayOperationStatePersistenceService
            .LoadIntoProject(
                project,
                fileName);

        if (project.SourceProvenanceStatus ==
            SourceProvenanceStatus.ContentMismatch)
        {
            project.SetUpdateCompatibilityReport(
                new UpdateCompatibilityReportService().Create(
                    project,
                    SourceGenerationTransition.ExternalContentMismatch));
        }
        else if (project.RequiresGameplayStateManifestMigration)
        {
            project.SetUpdateCompatibilityReport(
                new UpdateCompatibilityReportService().Create(
                    project,
                    SourceGenerationTransition.PreviousSourceGenerationUnknown));
        }
        else if (project.RequiresUnverifiedGameplayStateNotice)
        {
            project.SetUpdateCompatibilityReport(
                new UpdateCompatibilityReportService().Create(
                    project,
                    SourceGenerationTransition.PreviousSourceGenerationUnknown));
        }

        return project;
    }

    public ProjectModel LoadReferenceProject(
        string fileName)
    {
        return LoadReferenceProjectCore(fileName).Project;
    }

    internal ReferenceProjectLoadResult
        LoadReferenceProjectWithBytes(
            string fileName)
    {
        return LoadReferenceProjectCore(fileName);
    }

    private ReferenceProjectLoadResult
        LoadReferenceProjectCore(
            string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string fullFileName =
            Path.GetFullPath(fileName);

        byte[] exactBytes =
            File.ReadAllBytes(fullFileName);

        if (exactBytes.Length == 0)
        {
            throw new InvalidDataException(
                "The data file is empty.");
        }

        string currentContentIdentity =
            cdbGenerationIdentityService.Calculate(exactBytes);

        string json;
        using (MemoryStream stream = new(exactBytes, writable: false))
        using (StreamReader reader = new(
                   stream,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: true))
        {
            json = reader.ReadToEnd();
        }

        ProjectModel project =
            CreateProjectFromJson(json, fullFileName);

        if (project.Sheets.Count == 0)
        {
            throw new InvalidDataException(
                "The data file does not contain any usable project sheets.");
        }

        project.EstablishPersistedIdentity(
            currentContentIdentity,
            null,
            SourceProvenanceStatus.Unknown);

        return new ReferenceProjectLoadResult(
            project,
            exactBytes,
            currentContentIdentity);
    }

    internal GameplayStateManifestSnapshot CaptureGameplayStateForReplacement(
        string cdbFileName)
    {
        return gameplayOperationStatePersistenceService
            .CaptureForReplacement(cdbFileName);
    }

    internal void ApplyAuthoritativeImportIdentity(
        ProjectModel project,
        string sourceIdentity,
        GameplayStateManifestSnapshot previousState)
    {
        gameplayOperationStatePersistenceService.ApplyAuthoritativeImport(
            project,
            sourceIdentity,
            previousState);
    }

    internal void PersistImportedGameplayState(ProjectModel project)
    {
        gameplayOperationStatePersistenceService.Save(
            project,
            project.FileName);
    }

    public void CompletePostPublicationMigration(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!project.RequiresGameplayStateManifestMigration ||
            string.IsNullOrWhiteSpace(project.FileName))
        {
            return;
        }

        gameplayOperationStatePersistenceService.Save(
            project,
            project.FileName);
    }

    internal ProjectModel CreateProjectFromJson(
        string json,
        string fileName = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

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

        foreach (JToken sourceSheetToken in sourceSheets)
        {
            if (sourceSheetToken is not JObject sourceSheet)
            {
                project.ProjectLoadWarnings.Add(
                    "A raw sheet record uses an unsupported structure and remains preserved without editor modeling.");
                continue;
            }

            string? sheetName =
                sourceSheet["name"]?.ToString();

            if (string.IsNullOrWhiteSpace(
                    sheetName))
            {
                project.ProjectLoadWarnings.Add(
                    "A raw sheet without a usable name remains preserved without editor modeling.");
                continue;
            }

            try
            {
                SheetModel sheetModel =
                    projectModelFactory.CreateSheetModel(sourceSheet);
                project.Sheets.Add(sheetModel);
            }
            catch (Exception exception)
            {
                project.ProjectLoadWarnings.Add(
                    $"Sheet '{sheetName}' could not be modeled and remains preserved in raw project data: {exception.Message}");
            }
        }

        project.IsModified =
            false;

        return project;
    }
}

internal sealed record ReferenceProjectLoadResult(
    ProjectModel Project,
    byte[] ExactBytes,
    string ContentIdentity);
