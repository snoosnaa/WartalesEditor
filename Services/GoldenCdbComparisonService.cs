using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class GoldenCdbComparisonService
{
    private string cachedIdentity = string.Empty;
    private ProjectComparisonIndex? cachedGoldenIndex;

    public GoldenCdbComparisonResult Compare(
        ProjectModel currentProject,
        GoldenCdbReference golden)
    {
        ArgumentNullException.ThrowIfNull(currentProject);
        ArgumentNullException.ThrowIfNull(golden);

        if (!currentProject.IsModified &&
            string.Equals(
                currentProject.CurrentCdbContentIdentity,
                golden.Identity,
                StringComparison.Ordinal))
        {
            return new GoldenCdbComparisonResult(
                isExactMatch: true,
                Array.Empty<GoldenCdbComparisonItem>());
        }

        ProjectComparisonIndex goldenIndex = GetGoldenIndex(golden);
        ProjectComparisonIndex currentIndex = BuildIndex(
            currentProject,
            isGolden: false);
        List<GoldenCdbComparisonItem> items = new();
        items.AddRange(goldenIndex.CoverageIssues);
        items.AddRange(currentIndex.CoverageIssues);

        foreach (string sheetName in goldenIndex.AllSheetNames
                     .Union(currentIndex.AllSheetNames, StringComparer.Ordinal)
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            if (goldenIndex.UnresolvedSheetNames.Contains(sheetName) ||
                currentIndex.UnresolvedSheetNames.Contains(sheetName))
            {
                continue;
            }

            bool hasGolden = goldenIndex.Sheets.TryGetValue(
                sheetName,
                out SheetComparisonIndex? goldenSheet);
            bool hasCurrent = currentIndex.Sheets.TryGetValue(
                sheetName,
                out SheetComparisonIndex? currentSheet);

            if (!hasCurrent)
            {
                if (currentIndex.HasUnsupportedSheetIdentity)
                    continue;

                items.Add(CreateMissingItem(
                    GoldenCdbComparisonCategory.MissingFromCurrent,
                    GoldenCdbComparisonScope.Sheet,
                    sheetName));
                continue;
            }

            if (!hasGolden)
            {
                if (goldenIndex.HasUnsupportedSheetIdentity)
                    continue;

                items.Add(CreateMissingItem(
                    GoldenCdbComparisonCategory.NewInCurrent,
                    GoldenCdbComparisonScope.Sheet,
                    sheetName));
                continue;
            }

            CompareEntries(
                sheetName,
                goldenSheet!,
                currentSheet!,
                items);
        }

        return new GoldenCdbComparisonResult(
            isExactMatch: false,
            items);
    }

    public void Invalidate()
    {
        cachedIdentity = string.Empty;
        cachedGoldenIndex = null;
    }

    internal bool HasCachedGoldenIndex => cachedGoldenIndex != null;

    private ProjectComparisonIndex GetGoldenIndex(
        GoldenCdbReference golden)
    {
        if (cachedGoldenIndex != null &&
            string.Equals(
                cachedIdentity,
                golden.Identity,
                StringComparison.Ordinal))
        {
            return cachedGoldenIndex;
        }

        cachedGoldenIndex = BuildIndex(
            golden.Project,
            isGolden: true);
        cachedIdentity = golden.Identity;
        return cachedGoldenIndex;
    }

    private static ProjectComparisonIndex BuildIndex(
        ProjectModel project,
        bool isGolden)
    {
        Dictionary<string, SheetComparisonIndex> sheets =
            new(StringComparer.Ordinal);
        HashSet<string> unresolvedSheetNames =
            new(StringComparer.Ordinal);
        List<GoldenCdbComparisonItem> coverage = new();
        bool hasUnsupportedSheetIdentity = false;
        string side = isGolden ? "Golden" : "Current";

        foreach (IGrouping<string, SheetModel> sheetGroup in
                 project.Sheets.GroupBy(
                     sheet => sheet.Name,
                     StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(sheetGroup.Key))
            {
                hasUnsupportedSheetIdentity = true;
                coverage.Add(CreateCoverageItem(
                    GoldenCdbComparisonCategory.UnsupportedIdentity,
                    string.Empty,
                    $"{side} project contains a sheet without a stable name."));
                continue;
            }

            SheetModel[] matches = sheetGroup.ToArray();
            if (matches.Length != 1)
            {
                unresolvedSheetNames.Add(sheetGroup.Key);
                coverage.Add(CreateCoverageItem(
                    GoldenCdbComparisonCategory.AmbiguousIdentity,
                    sheetGroup.Key,
                    $"{side} project contains {matches.Length} sheets named '{sheetGroup.Key}'."));
                continue;
            }

            sheets.Add(
                sheetGroup.Key,
                BuildSheetIndex(
                    matches[0],
                    side,
                    coverage));
        }

        if (project.ProjectLoadWarnings.Count > 0)
        {
            coverage.Add(CreateCoverageItem(
                GoldenCdbComparisonCategory.UnsupportedStructure,
                string.Empty,
                $"{side} project contains {project.ProjectLoadWarnings.Count:N0} preserved structure warning(s)."));
        }

        return new ProjectComparisonIndex(
            sheets,
            unresolvedSheetNames,
            hasUnsupportedSheetIdentity,
            coverage);
    }

    private static SheetComparisonIndex BuildSheetIndex(
        SheetModel sheet,
        string side,
        ICollection<GoldenCdbComparisonItem> coverage)
    {
        Dictionary<string, EntryComparisonIndex> entries =
            new(StringComparer.Ordinal);
        HashSet<string> unresolvedEntryIds =
            new(StringComparer.Ordinal);
        List<EntryModel> stableEntries = new();
        int unsupportedCount = 0;

        foreach (EntryModel entry in sheet.Entries)
        {
            JToken? idToken = entry.SourceEntry?.Property("id")?.Value;
            string id = idToken?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                unsupportedCount++;
                continue;
            }

            stableEntries.Add(entry);
        }

        if (unsupportedCount > 0)
        {
            coverage.Add(new GoldenCdbComparisonItem(
                GoldenCdbComparisonCategory.UnsupportedIdentity,
                GoldenCdbComparisonScope.CoverageSummary,
                sheet.Name,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{side} sheet '{sheet.Name}' contains {unsupportedCount:N0} record(s) without an explicit ID."));
        }

        foreach (IGrouping<string, EntryModel> entryGroup in stableEntries
                     .GroupBy(
                         entry => entry.SourceEntry!["id"]!.ToString(),
                         StringComparer.Ordinal))
        {
            EntryModel[] matches = entryGroup.ToArray();
            if (matches.Length != 1)
            {
                unresolvedEntryIds.Add(entryGroup.Key);
                coverage.Add(new GoldenCdbComparisonItem(
                    GoldenCdbComparisonCategory.AmbiguousIdentity,
                    GoldenCdbComparisonScope.Entry,
                    sheet.Name,
                    entryGroup.Key,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    $"{side} sheet '{sheet.Name}' contains {matches.Length} records with ID '{entryGroup.Key}'."));
                continue;
            }

            entries.Add(
                entryGroup.Key,
                BuildEntryIndex(
                    sheet.Name,
                    matches[0],
                    side,
                    coverage));
        }

        return new SheetComparisonIndex(
            entries,
            unresolvedEntryIds,
            unsupportedCount > 0);
    }

    private static EntryComparisonIndex BuildEntryIndex(
        string sheetName,
        EntryModel entry,
        string side,
        ICollection<GoldenCdbComparisonItem> coverage)
    {
        Dictionary<string, JToken> properties =
            new(StringComparer.Ordinal);
        HashSet<string> unresolvedPropertyPaths =
            new(StringComparer.Ordinal);
        bool hasUnsupportedPropertyIdentity = false;

        foreach (IGrouping<string, PropertyModel> propertyGroup in
                 entry.Properties.GroupBy(
                     property => property.EffectivePropertyPath,
                     StringComparer.Ordinal))
        {
            PropertyModel[] matches = propertyGroup.ToArray();
            if (string.IsNullOrWhiteSpace(propertyGroup.Key) ||
                matches.Length != 1 ||
                matches[0].SourceProperty == null)
            {
                if (string.IsNullOrWhiteSpace(propertyGroup.Key))
                    hasUnsupportedPropertyIdentity = true;
                else
                    unresolvedPropertyPaths.Add(propertyGroup.Key);

                coverage.Add(new GoldenCdbComparisonItem(
                    matches.Length > 1
                        ? GoldenCdbComparisonCategory.AmbiguousIdentity
                        : GoldenCdbComparisonCategory.UnsupportedStructure,
                    GoldenCdbComparisonScope.Property,
                    sheetName,
                    entry.Id,
                    propertyGroup.Key,
                    string.Empty,
                    string.Empty,
                    $"{side} property '{propertyGroup.Key}' could not be compared safely."));
                continue;
            }

            properties.Add(
                propertyGroup.Key,
                matches[0].SourceProperty!.Value);
        }

        return new EntryComparisonIndex(
            properties,
            unresolvedPropertyPaths,
            hasUnsupportedPropertyIdentity);
    }

    private static void CompareEntries(
        string sheetName,
        SheetComparisonIndex golden,
        SheetComparisonIndex current,
        ICollection<GoldenCdbComparisonItem> items)
    {
        foreach (string entryId in golden.AllEntryIds
                     .Union(current.AllEntryIds, StringComparer.Ordinal)
                     .OrderBy(id => id, StringComparer.Ordinal))
        {
            if (golden.UnresolvedEntryIds.Contains(entryId) ||
                current.UnresolvedEntryIds.Contains(entryId))
            {
                continue;
            }

            bool hasGolden = golden.Entries.TryGetValue(
                entryId,
                out EntryComparisonIndex? goldenEntry);
            bool hasCurrent = current.Entries.TryGetValue(
                entryId,
                out EntryComparisonIndex? currentEntry);

            if (!hasCurrent)
            {
                if (current.HasUnsupportedEntryIdentity)
                    continue;

                items.Add(CreateMissingItem(
                    GoldenCdbComparisonCategory.MissingFromCurrent,
                    GoldenCdbComparisonScope.Entry,
                    sheetName,
                    entryId));
                continue;
            }

            if (!hasGolden)
            {
                if (golden.HasUnsupportedEntryIdentity)
                    continue;

                items.Add(CreateMissingItem(
                    GoldenCdbComparisonCategory.NewInCurrent,
                    GoldenCdbComparisonScope.Entry,
                    sheetName,
                    entryId));
                continue;
            }

            CompareProperties(
                sheetName,
                entryId,
                goldenEntry!,
                currentEntry!,
                items);
        }
    }

    private static void CompareProperties(
        string sheetName,
        string entryId,
        EntryComparisonIndex golden,
        EntryComparisonIndex current,
        ICollection<GoldenCdbComparisonItem> items)
    {
        foreach (string propertyPath in golden.AllPropertyPaths
                     .Union(current.AllPropertyPaths, StringComparer.Ordinal)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            if (golden.UnresolvedPropertyPaths.Contains(propertyPath) ||
                current.UnresolvedPropertyPaths.Contains(propertyPath))
            {
                continue;
            }

            bool hasGolden = golden.Properties.TryGetValue(
                propertyPath,
                out JToken? goldenValue);
            bool hasCurrent = current.Properties.TryGetValue(
                propertyPath,
                out JToken? currentValue);

            if (!hasCurrent)
            {
                if (current.HasUnsupportedPropertyIdentity)
                    continue;

                items.Add(CreateMissingItem(
                    GoldenCdbComparisonCategory.MissingFromCurrent,
                    GoldenCdbComparisonScope.Property,
                    sheetName,
                    entryId,
                    propertyPath,
                    Summarize(goldenValue),
                    string.Empty));
                continue;
            }

            if (!hasGolden)
            {
                if (golden.HasUnsupportedPropertyIdentity)
                    continue;

                items.Add(CreateMissingItem(
                    GoldenCdbComparisonCategory.NewInCurrent,
                    GoldenCdbComparisonScope.Property,
                    sheetName,
                    entryId,
                    propertyPath,
                    string.Empty,
                    Summarize(currentValue)));
                continue;
            }

            if (goldenValue!.Type != currentValue!.Type)
            {
                items.Add(CreateValueItem(
                    GoldenCdbComparisonCategory.TypeChanged,
                    sheetName,
                    entryId,
                    propertyPath,
                    goldenValue,
                    currentValue));
                continue;
            }

            if (goldenValue is JArray goldenArray &&
                currentValue is JArray currentArray)
            {
                if (!string.Equals(
                        GameplayOperationFingerprintService.CreateShapeFingerprint(goldenArray),
                        GameplayOperationFingerprintService.CreateShapeFingerprint(currentArray),
                        StringComparison.Ordinal))
                {
                    items.Add(CreateValueItem(
                        GoldenCdbComparisonCategory.StructureChanged,
                        sheetName,
                        entryId,
                        propertyPath,
                        goldenValue,
                        currentValue));
                }
                else if (!JToken.DeepEquals(goldenValue, currentValue))
                {
                    items.Add(CreateValueItem(
                        GoldenCdbComparisonCategory.ChangedValue,
                        sheetName,
                        entryId,
                        propertyPath,
                        goldenValue,
                        currentValue));
                }

                continue;
            }

            if (!JToken.DeepEquals(goldenValue, currentValue))
            {
                items.Add(CreateValueItem(
                    GoldenCdbComparisonCategory.ChangedValue,
                    sheetName,
                    entryId,
                    propertyPath,
                    goldenValue,
                    currentValue));
            }
        }
    }

    private static GoldenCdbComparisonItem CreateValueItem(
        GoldenCdbComparisonCategory category,
        string sheet,
        string entry,
        string property,
        JToken golden,
        JToken current) =>
        new(
            category,
            GoldenCdbComparisonScope.Property,
            sheet,
            entry,
            property,
            Summarize(golden),
            Summarize(current),
            string.Empty);

    private static GoldenCdbComparisonItem CreateMissingItem(
        GoldenCdbComparisonCategory category,
        GoldenCdbComparisonScope scope,
        string sheet,
        string entry = "",
        string property = "",
        string goldenValue = "",
        string currentValue = "") =>
        new(
            category,
            scope,
            sheet,
            entry,
            property,
            goldenValue,
            currentValue,
            string.Empty);

    private static GoldenCdbComparisonItem CreateCoverageItem(
        GoldenCdbComparisonCategory category,
        string sheet,
        string details) =>
        new(
            category,
            GoldenCdbComparisonScope.CoverageSummary,
            sheet,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            details);

    private static string Summarize(JToken? token)
    {
        if (token == null)
            return "Missing";

        return token switch
        {
            JArray array => $"Array ({array.Count:N0} items)",
            JObject value => $"Object ({value.Count:N0} members)",
            JValue value when value.Type == JTokenType.Null => "null",
            JValue value => value.ToString(),
            _ => token.Type.ToString()
        };
    }

    private sealed record ProjectComparisonIndex(
        Dictionary<string, SheetComparisonIndex> Sheets,
        HashSet<string> UnresolvedSheetNames,
        bool HasUnsupportedSheetIdentity,
        IReadOnlyList<GoldenCdbComparisonItem> CoverageIssues)
    {
        public IEnumerable<string> AllSheetNames =>
            Sheets.Keys.Concat(UnresolvedSheetNames);
    }

    private sealed record SheetComparisonIndex(
        Dictionary<string, EntryComparisonIndex> Entries,
        HashSet<string> UnresolvedEntryIds,
        bool HasUnsupportedEntryIdentity)
    {
        public IEnumerable<string> AllEntryIds =>
            Entries.Keys.Concat(UnresolvedEntryIds);
    }

    private sealed record EntryComparisonIndex(
        Dictionary<string, JToken> Properties,
        HashSet<string> UnresolvedPropertyPaths,
        bool HasUnsupportedPropertyIdentity)
    {
        public IEnumerable<string> AllPropertyPaths =>
            Properties.Keys.Concat(UnresolvedPropertyPaths);
    }
}
