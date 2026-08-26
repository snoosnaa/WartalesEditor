using System;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Services.Operations;

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
        ProjectMutationResult result = new();
        return AddCampFacilitiesCore(project, result);
    }

    internal ProjectMutationResult AddCampFacilities(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return AddCampFacilitiesCore(project, context.MutationResult);
    }

    private ProjectMutationResult AddCampFacilitiesCore(
        ProjectModel project,
        ProjectMutationResult result)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        ValidateAddCampFacilitiesCompatibility(project);

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
            FindRequiredUniqueEntry(
                itemSheet,
                AnvilEntryId);

        EntryModel apothecaryEntry =
            FindRequiredUniqueEntry(
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

        JObject anvilProps =
            campFacilityJsonBuilder.BuildAnvilProps(existingAnvilProps);
        JObject anvilTool = campFacilityJsonBuilder.BuildAnvilTool();
        JObject anvilIcon = campFacilityJsonBuilder.BuildAnvilIcon();
        JObject apothecaryProps =
            campFacilityJsonBuilder.BuildApothecaryProps(existingApothecaryProps);
        JObject apothecaryTool = campFacilityJsonBuilder.BuildApothecaryTool();
        JObject apothecaryIcon = campFacilityJsonBuilder.BuildApothecaryIcon();
        JObject anvilCraft = campFacilityJsonBuilder.BuildAnvilCraftEntry();
        JObject apothecaryCraft = campFacilityJsonBuilder.BuildApothecaryCraftEntry();
        EntryModel? existingAnvilCraft = FindUniqueCraftEntry(craftSheet, AnvilEntryId);
        EntryModel? existingApothecaryCraft =
            FindUniqueCraftEntry(craftSheet, ApothecaryTableEntryId);

        result.Merge(
            projectMutationService.EnsureObjectByPath(
                anvilEntry,
                PropsPropertyName,
                anvilProps));

        result.Merge(
            projectMutationService.EnsureObjectByPath(
                anvilEntry,
                ToolPropertyName,
                anvilTool));

        result.Merge(
            projectMutationService.EnsureObjectByPath(
                anvilEntry,
                IconPropertyName,
                anvilIcon));

        result.Merge(
            projectMutationService.EnsureObjectByPath(
                apothecaryEntry,
                PropsPropertyName,
                apothecaryProps));

        result.Merge(
            projectMutationService.EnsureObjectByPath(
                apothecaryEntry,
                ToolPropertyName,
                apothecaryTool));

        result.Merge(
            projectMutationService.EnsureObjectByPath(
                apothecaryEntry,
                IconPropertyName,
                apothecaryIcon));

        result.Merge(
            EnsureCraftEntry(
                craftSheet,
                existingAnvilCraft,
                anvilCraft));

        result.Merge(
            EnsureCraftEntry(
                craftSheet,
                existingApothecaryCraft,
                apothecaryCraft));

        return result;
    }

    public ProjectMutationResult UpgradeAllEquipment(
        ProjectModel project)
    {
        ProjectMutationResult result = new();
        return UpgradeAllEquipmentCore(project, result);
    }

    internal ProjectMutationResult UpgradeAllEquipment(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return UpgradeAllEquipmentCore(project, context.MutationResult);
    }

    private ProjectMutationResult UpgradeAllEquipmentCore(
        ProjectModel project,
        ProjectMutationResult result)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        ValidateUpgradeAllEquipmentCompatibility(project);

        SheetModel itemSheet =
            FindRequiredSheet(
                project,
                ItemSheetName,
                "Upgrade All Equipment");

        List<(EntryModel Entry, int UpdatedFlags)> plannedChanges =
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

            if (updatedFlags != existingFlags)
            {
                plannedChanges.Add((entry, updatedFlags));
            }
        }

        foreach ((EntryModel entry, int updatedFlags) in plannedChanges)
        {
            result.Merge(
                projectMutationService.EnsurePropertyByPath(
                    entry,
                    flagsPropertyPath,
                    new JValue(
                        updatedFlags)));
        }

        return result;
    }

    internal void ValidateAddCampFacilitiesCompatibility(
        ProjectModel project)
    {
        SheetModel itemSheet = FindRequiredSheet(
            project, ItemSheetName, "Add Camp Facilities");
        SheetModel craftSheet = FindRequiredSheet(
            project, CraftSheetName, "Add Camp Facilities");
        EntryModel anvil = FindRequiredUniqueEntry(itemSheet, AnvilEntryId);
        EntryModel apothecary = FindRequiredUniqueEntry(itemSheet, ApothecaryTableEntryId);
        JObject anvilProps = RequireObjectProperty(
            anvil, PropsPropertyName, "Add Camp Facilities");
        JObject apothecaryProps = RequireObjectProperty(
            apothecary, PropsPropertyName, "Add Camp Facilities");

        JObject anvilPropsValue = campFacilityJsonBuilder.BuildAnvilProps(anvilProps);
        JObject anvilTool = campFacilityJsonBuilder.BuildAnvilTool();
        JObject anvilIcon = campFacilityJsonBuilder.BuildAnvilIcon();
        JObject apothecaryPropsValue = campFacilityJsonBuilder.BuildApothecaryProps(apothecaryProps);
        JObject apothecaryTool = campFacilityJsonBuilder.BuildApothecaryTool();
        JObject apothecaryIcon = campFacilityJsonBuilder.BuildApothecaryIcon();
        JObject anvilCraftValue = campFacilityJsonBuilder.BuildAnvilCraftEntry();
        JObject apothecaryCraftValue = campFacilityJsonBuilder.BuildApothecaryCraftEntry();

        ValidateObjectMerge(anvil, PropsPropertyName, anvilPropsValue);
        ValidateObjectMerge(anvil, ToolPropertyName, anvilTool);
        ValidateObjectMerge(anvil, IconPropertyName, anvilIcon);
        ValidateObjectMerge(apothecary, PropsPropertyName, apothecaryPropsValue);
        ValidateObjectMerge(apothecary, ToolPropertyName, apothecaryTool);
        ValidateObjectMerge(apothecary, IconPropertyName, apothecaryIcon);

        EntryModel? anvilCraft = FindUniqueCraftEntry(craftSheet, AnvilEntryId);
        EntryModel? apothecaryCraft = FindUniqueCraftEntry(craftSheet, ApothecaryTableEntryId);
        if (anvilCraft != null)
            ValidateExistingCraftEntry(anvilCraft, anvilCraftValue, AnvilEntryId);
        else
            ValidateCraftEntryCreation(craftSheet, anvilCraftValue);
        if (apothecaryCraft != null)
            ValidateExistingCraftEntry(apothecaryCraft, apothecaryCraftValue, ApothecaryTableEntryId);
        else
            ValidateCraftEntryCreation(craftSheet, apothecaryCraftValue);
    }

    internal void ValidateUpgradeAllEquipmentCompatibility(
        ProjectModel project)
    {
        SheetModel itemSheet = FindRequiredSheet(
            project, ItemSheetName, "Upgrade All Equipment");
        foreach (string targetId in UpgradeAllEquipmentTargetCatalog.EntryIds)
        {
            EntryModel[] matches = itemSheet.Entries
                .Where(entry => string.Equals(entry.Id, targetId, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length == 0)
                throw new GameplayCompatibilityException(
                    GameplayCompatibilityStatus.PartiallyOutdated,
                    "Some supported equipment is no longer available in this game-data version.",
                    $"Target item '{targetId}' is missing from the equipment catalog.");
            if (matches.Length > 1)
                throw new GameplayCompatibilityException(
                    GameplayCompatibilityStatus.AmbiguousTarget,
                    "A supported equipment target is duplicated and cannot be selected safely.",
                    $"Target item '{targetId}' occurs {matches.Length} times.");

            EntryModel entry = matches[0];
            if (entry.SourceEntry?[PropsPropertyName] is not JObject props)
                throw new GameplayCompatibilityException(
                    GameplayCompatibilityStatus.StructureChanged,
                    "A supported equipment target has an incompatible structure.",
                    $"Target item '{entry.Id}' does not contain a valid props object.");
            JToken? flags = props[FlagsPropertyName];
            if (flags != null && flags.Type != JTokenType.Integer)
                throw new GameplayCompatibilityException(
                    GameplayCompatibilityStatus.TypeChanged,
                    "A supported equipment target uses an incompatible value type.",
                    $"Target item '{entry.Id}' contains a changed props.flags type.");
        }
    }

    private void ValidateObjectMerge(
        EntryModel entry,
        string propertyPath,
        JObject value)
    {
        try
        {
            projectMutationService.ValidateObjectByPath(entry, propertyPath, value);
        }
        catch (InvalidOperationException exception)
        {
            throw new GameplayCompatibilityException(
                GameplayCompatibilityStatus.StructureChanged,
                "A camp-facility target has an incompatible structure.",
                exception.Message);
        }
    }

    private void ValidateCraftEntryCreation(
        SheetModel craftSheet,
        JObject craftEntry)
    {
        try
        {
            projectMutationService.ValidateEntryCreation(craftSheet, craftEntry);
        }
        catch (InvalidOperationException exception)
        {
            throw new GameplayCompatibilityException(
                GameplayCompatibilityStatus.StructureChanged,
                "The crafting data cannot accept a camp-facility recipe.",
                exception.Message);
        }
    }

    private static void ValidateExistingCraftEntry(
        EntryModel entry,
        JObject expected,
        string itemId)
    {
        if (entry.SourceEntry == null)
            throw new GameplayCompatibilityException(
                GameplayCompatibilityStatus.StructureChanged,
                "An existing camp-facility recipe has an incompatible structure.");

        foreach (JProperty expectedProperty in expected.Properties())
        {
            JToken? actual = entry.SourceEntry[expectedProperty.Name];
            if (actual == null || actual.Type != expectedProperty.Value.Type)
                throw new GameplayCompatibilityException(
                    GameplayCompatibilityStatus.StructureChanged,
                    "An existing camp-facility recipe has an incompatible structure.",
                    $"Craft entry '{itemId}' has an invalid '{expectedProperty.Name}' value.");
        }

        if (!string.Equals(entry.SourceEntry[CraftItemPropertyName]?.Value<string>(), itemId, StringComparison.Ordinal) ||
            !string.Equals(entry.SourceEntry[ToolPropertyName]?.Value<string>(), "Workshop", StringComparison.Ordinal))
        {
            throw new GameplayCompatibilityException(
                GameplayCompatibilityStatus.StructureChanged,
                "An existing camp-facility recipe has an incompatible identity.");
        }
    }

    private ProjectMutationResult EnsureCraftEntry(
        SheetModel craftSheet,
        EntryModel? existingEntry,
        JObject craftEntry)
    {
        ArgumentNullException.ThrowIfNull(
            craftSheet);

        ArgumentNullException.ThrowIfNull(
            craftEntry);

        if (existingEntry != null)
        {
            return new ProjectMutationResult();
        }

        return projectMutationService.CreateEntry(
            craftSheet,
            craftEntry);
    }

    private EntryModel? FindUniqueCraftEntry(
        SheetModel craftSheet,
        string itemId)
    {
        EntryModel[] matches = craftSheet.Entries
            .Where(entry => string.Equals(
                entry.SourceEntry?[CraftItemPropertyName]?.Value<string>(),
                itemId,
                StringComparison.Ordinal))
            .ToArray();

        if (matches.Length > 1)
            throw new GameplayCompatibilityException(
                GameplayCompatibilityStatus.AmbiguousTarget,
                "A camp-facility crafting recipe is duplicated and cannot be selected safely.",
                $"Craft item '{itemId}' occurs {matches.Length} times.");

        return matches.SingleOrDefault();
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

    private EntryModel FindRequiredUniqueEntry(
        SheetModel sheet,
        string entryId)
    {
        ArgumentNullException.ThrowIfNull(
            sheet);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            entryId);

        EntryModel[] matches = sheet.Entries
            .Where(entry => string.Equals(entry.Id, entryId, StringComparison.Ordinal))
            .ToArray();

        if (matches.Length == 0)
            throw new GameplayCompatibilityException(
                GameplayCompatibilityStatus.MissingTarget,
                "A required camp-facility target is missing.",
                $"Required entry '{entryId}' was not found in sheet '{sheet.Name}'.");
        if (matches.Length > 1)
            throw new GameplayCompatibilityException(
                GameplayCompatibilityStatus.AmbiguousTarget,
                "A required camp-facility target is duplicated and cannot be selected safely.",
                $"Required entry '{entryId}' occurs {matches.Length} times in sheet '{sheet.Name}'.");

        return matches[0];
    }
}
