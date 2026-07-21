using System;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class ContentCreationService
{
    private const string ItemSheetName =
        "item";

    private const string CraftSheetName =
        "craft";

    private const string AnvilEntryId =
        "Anvil";

    private const string ApothecaryTableEntryId =
        "ApothecaryTable";

    private const string PropsPropertyName =
        "props";

    private const string FlagsPropertyName =
        "flags";

    private const int UpgradeableEquipmentFlag =
        128;

    private const string ToolPropertyName =
        "tool";

    private const string IconPropertyName =
        "icon";

    private const string CraftItemPropertyName =
        "item";

    private readonly ProjectMutationService
        projectMutationService;

    private readonly CampFacilityJsonBuilder
        campFacilityJsonBuilder;

    public ContentCreationService(
        ProjectMutationService projectMutationService)
        : this(
            projectMutationService,
            new CampFacilityJsonBuilder())
    {
    }

    public ContentCreationService(
        ProjectMutationService projectMutationService,
        CampFacilityJsonBuilder campFacilityJsonBuilder)
    {
        ArgumentNullException.ThrowIfNull(
            projectMutationService);

        ArgumentNullException.ThrowIfNull(
            campFacilityJsonBuilder);

        this.projectMutationService =
            projectMutationService;

        this.campFacilityJsonBuilder =
            campFacilityJsonBuilder;
    }

    public ProjectMutationResult AddCampFacilities(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        SheetModel itemSheet =
            FindRequiredSheet(
                project,
                ItemSheetName,
                "Add Camp Facilities");

        SheetModel craftSheet =
            FindRequiredSheet(
                project,
                CraftSheetName,
                "Add Camp Facilities");

        EntryModel anvilEntry =
            FindRequiredEntry(
                itemSheet,
                AnvilEntryId);

        EntryModel apothecaryEntry =
            FindRequiredEntry(
                itemSheet,
                ApothecaryTableEntryId);

        JObject existingAnvilProps =
            RequireObjectProperty(
                anvilEntry,
                PropsPropertyName,
                "Add Camp Facilities");

        JObject existingApothecaryProps =
            RequireObjectProperty(
                apothecaryEntry,
                PropsPropertyName,
                "Add Camp Facilities");

        ProjectMutationResult result =
            new();

        result.Merge(
            EnsureObjectProperty(
                itemSheet,
                anvilEntry,
                PropsPropertyName,
                campFacilityJsonBuilder.BuildAnvilProps(
                    existingAnvilProps)));

        result.Merge(
            EnsureObjectProperty(
                itemSheet,
                anvilEntry,
                ToolPropertyName,
                campFacilityJsonBuilder.BuildAnvilTool()));

        result.Merge(
            EnsureObjectProperty(
                itemSheet,
                anvilEntry,
                IconPropertyName,
                campFacilityJsonBuilder.BuildAnvilIcon()));

        result.Merge(
            EnsureObjectProperty(
                itemSheet,
                apothecaryEntry,
                PropsPropertyName,
                campFacilityJsonBuilder.BuildApothecaryProps(
                    existingApothecaryProps)));

        result.Merge(
            EnsureObjectProperty(
                itemSheet,
                apothecaryEntry,
                ToolPropertyName,
                campFacilityJsonBuilder.BuildApothecaryTool()));

        result.Merge(
            EnsureObjectProperty(
                itemSheet,
                apothecaryEntry,
                IconPropertyName,
                campFacilityJsonBuilder.BuildApothecaryIcon()));

        result.Merge(
            EnsureCraftEntry(
                craftSheet,
                AnvilEntryId,
                campFacilityJsonBuilder.BuildAnvilCraftEntry()));

        result.Merge(
            EnsureCraftEntry(
                craftSheet,
                ApothecaryTableEntryId,
                campFacilityJsonBuilder.BuildApothecaryCraftEntry()));

        return result;
    }

    public ProjectMutationResult UpgradeAllEquipment(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        SheetModel itemSheet =
            FindRequiredSheet(
                project,
                ItemSheetName,
                "Upgrade All Equipment");

        ProjectMutationResult result =
            new();

        const string flagsPropertyPath =
            "props.flags";

        foreach (EntryModel entry in itemSheet.Entries)
        {
            if (!UpgradeAllEquipmentTargetCatalog.Contains(
                    entry.Id))
            {
                continue;
            }

            JObject? props =
                entry.SourceEntry?[PropsPropertyName]
                    as JObject;

            if (props == null)
            {
                throw new InvalidOperationException(
                    $"Upgrade All Equipment cannot continue because " +
                    $"item '{entry.Id}' does not contain a valid " +
                    $"'{PropsPropertyName}' object.");
            }

            JToken? flagsToken =
                props[FlagsPropertyName];

            if (flagsToken != null &&
                flagsToken.Type != JTokenType.Integer)
            {
                throw new InvalidOperationException(
                    $"Upgrade All Equipment cannot continue because " +
                    $"'{flagsPropertyPath}' on item '{entry.Id}' is not " +
                    "an integer. The loaded CDB may be incompatible " +
                    "with this tool.");
            }

            int existingFlags =
                flagsToken?.Value<int>()
                ?? 0;

            int updatedFlags =
                existingFlags |
                UpgradeableEquipmentFlag;

            if (updatedFlags == existingFlags)
            {
                continue;
            }

            result.Merge(
                projectMutationService.EnsurePropertyByPath(
                    entry,
                    flagsPropertyPath,
                    new JValue(
                        updatedFlags)));
        }

        return result;
    }

    private ProjectMutationResult EnsureObjectProperty(
        SheetModel sheet,
        EntryModel entry,
        string propertyName,
        JObject propertyValue)
    {
        ArgumentNullException.ThrowIfNull(
            sheet);

        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyName);

        ArgumentNullException.ThrowIfNull(
            propertyValue);

        if (entry.SourceEntry == null)
        {
            throw new InvalidOperationException(
                $"Entry '{entry.Id}' is not connected " +
                "to a source JSON object.");
        }

        JToken? existingToken =
            entry.SourceEntry[propertyName];

        if (existingToken != null &&
            existingToken.Type != JTokenType.Object)
        {
            throw new InvalidOperationException(
                $"Add Camp Facilities cannot continue because " +
                $"'{propertyName}' on entry '{entry.Id}' is not " +
                "a JSON object.");
        }

        return projectMutationService.EnsureProperty(
            sheet.Name,
            entry,
            propertyName,
            propertyValue);
    }

    private ProjectMutationResult EnsureCraftEntry(
        SheetModel craftSheet,
        string itemId,
        JObject craftEntry)
    {
        ArgumentNullException.ThrowIfNull(
            craftSheet);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            itemId);

        ArgumentNullException.ThrowIfNull(
            craftEntry);

        EntryModel? existingEntry =
            projectMutationService.FindEntryByProperty(
                craftSheet,
                CraftItemPropertyName,
                new JValue(
                    itemId));

        if (existingEntry != null)
        {
            return new ProjectMutationResult();
        }

        return projectMutationService.CreateEntry(
            craftSheet,
            craftEntry);
    }

    private static JObject RequireObjectProperty(
        EntryModel entry,
        string propertyName,
        string operationName)
    {
        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            propertyName);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            operationName);

        if (entry.SourceEntry == null)
        {
            throw new InvalidOperationException(
                $"{operationName} cannot continue because entry " +
                $"'{entry.Id}' is not connected to source JSON.");
        }

        if (entry.SourceEntry[propertyName] is not JObject objectProperty)
        {
            throw new InvalidOperationException(
                $"{operationName} cannot continue because required " +
                $"object '{propertyName}' was not found on entry " +
                $"'{entry.Id}'.");
        }

        return objectProperty;
    }

    private SheetModel FindRequiredSheet(
        ProjectModel project,
        string sheetName,
        string operationName)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            sheetName);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            operationName);

        try
        {
            return projectMutationService.FindSheet(
                project,
                sheetName);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                $"{operationName} cannot continue because " +
                $"the required '{sheetName}' sheet was not found. " +
                "The loaded CDB may be incompatible with this tool.",
                exception);
        }
    }

    private EntryModel FindRequiredEntry(
        SheetModel sheet,
        string entryId)
    {
        ArgumentNullException.ThrowIfNull(
            sheet);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            entryId);

        try
        {
            return projectMutationService.FindEntry(
                sheet,
                entryId);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                "Add Camp Facilities cannot continue because " +
                $"the required entry '{entryId}' was not found " +
                $"in the '{sheet.Name}' sheet. " +
                "The loaded CDB may be incompatible with this tool.",
                exception);
        }
    }
}