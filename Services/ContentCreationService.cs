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
                ItemSheetName);

        SheetModel craftSheet =
            FindRequiredSheet(
                project,
                CraftSheetName);

        EntryModel anvilEntry =
            FindRequiredEntry(
                itemSheet,
                AnvilEntryId);

        EntryModel apothecaryEntry =
            FindRequiredEntry(
                itemSheet,
                ApothecaryTableEntryId);

        JObject existingAnvilProps =
            GetExistingObjectPropertyClone(
                anvilEntry,
                PropsPropertyName);

        JObject existingApothecaryProps =
            GetExistingObjectPropertyClone(
                apothecaryEntry,
                PropsPropertyName);

        ProjectMutationResult result =
            new();

        result.Merge(
            projectMutationService.EnsureProperty(
                ItemSheetName,
                anvilEntry,
                PropsPropertyName,
                campFacilityJsonBuilder.BuildAnvilProps(
                    existingAnvilProps)));

        result.Merge(
            projectMutationService.EnsureProperty(
                ItemSheetName,
                anvilEntry,
                ToolPropertyName,
                campFacilityJsonBuilder.BuildAnvilTool()));

        result.Merge(
            projectMutationService.EnsureProperty(
                ItemSheetName,
                anvilEntry,
                IconPropertyName,
                campFacilityJsonBuilder.BuildAnvilIcon()));

        result.Merge(
            projectMutationService.EnsureProperty(
                ItemSheetName,
                apothecaryEntry,
                PropsPropertyName,
                campFacilityJsonBuilder.BuildApothecaryProps(
                    existingApothecaryProps)));

        result.Merge(
            projectMutationService.EnsureProperty(
                ItemSheetName,
                apothecaryEntry,
                ToolPropertyName,
                campFacilityJsonBuilder.BuildApothecaryTool()));

        result.Merge(
            projectMutationService.EnsureProperty(
                ItemSheetName,
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
                campFacilityJsonBuilder
                    .BuildApothecaryCraftEntry()));

        return result;
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

    private JObject GetExistingObjectPropertyClone(
        EntryModel entry,
        string propertyName)
    {
        PropertyModel? property =
            projectMutationService.FindProperty(
                entry,
                propertyName);

        if (property == null)
        {
            return new JObject();
        }

        if (property.SourceProperty == null)
        {
            throw new InvalidOperationException(
                $"Add Camp Facilities cannot continue because " +
                $"property '{propertyName}' on entry " +
                $"'{entry.Id}' is not connected to its " +
                "source JSON property.");
        }

        if (property.SourceProperty.Value is not JObject
            sourceObject)
        {
            throw new InvalidOperationException(
                $"Add Camp Facilities cannot continue because " +
                $"property '{propertyName}' on entry " +
                $"'{entry.Id}' is not a JSON object. " +
                "The loaded CDB may be incompatible with this tool.");
        }

        return (JObject)sourceObject.DeepClone();
    }

    private SheetModel FindRequiredSheet(
        ProjectModel project,
        string sheetName)
    {
        try
        {
            return projectMutationService.FindSheet(
                project,
                sheetName);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                $"Add Camp Facilities cannot continue because " +
                $"the required '{sheetName}' sheet was not found. " +
                "The loaded CDB may be incompatible with this tool.",
                exception);
        }
    }

    private EntryModel FindRequiredEntry(
        SheetModel sheet,
        string entryId)
    {
        try
        {
            return projectMutationService.FindEntry(
                sheet,
                entryId);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                $"Add Camp Facilities cannot continue because " +
                $"the required entry '{entryId}' was not found " +
                $"in the '{sheet.Name}' sheet. " +
                "The loaded CDB may be incompatible with this tool.",
                exception);
        }
    }
}