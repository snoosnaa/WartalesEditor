using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class PathXpRewardsService
{
    public const string MightPathId = "PathMight";
    public const string TradePathId = "PathTrade";
    public const string CrimePathId = "PathCrime";
    public const string MysteryPathId = "PathMystery";
    public const string PathPropertyPath = "path";
    public const string RewardPropertyPath = "pathXP";

    public static IReadOnlyList<string> PathIds { get; } =
        [MightPathId, TradePathId, CrimePathId, MysteryPathId];

    public static IReadOnlyList<PathXpRewardOption> Options { get; } =
    [
        new(1, "Original"), new(2, "2×"), new(3, "3×"),
        new(4, "4×"), new(5, "5×")
    ];

    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;
    private readonly LocalizationService localizationService;

    public PathXpRewardsService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService,
        LocalizationService? localizationService = null)
    {
        this.mutationService = mutationService ??
            throw new ArgumentNullException(nameof(mutationService));
        this.stateService = stateService ??
            throw new ArgumentNullException(nameof(stateService));
        this.localizationService = localizationService ?? new LocalizationService();
    }

    public int DetectMultiplier(ProjectModel project, string pathId)
    {
        _ = ResolveTargets(project, pathId);
        ProgressionType type = GetOperationType(pathId);
        GameplayOperationStateModel? state = stateService.FindState(project, type);
        if (state == null) return 1;
        ValidateState(project, state);
        return ReadMultiplier(state);
    }

    public bool CanRestorePreviousValues(ProjectModel project, string pathId) =>
        stateService.CanRestorePreviousValues(project, GetOperationType(pathId));

    public string GetDisplayName(string pathId) =>
        localizationService.GetLocalizedName(pathId) ?? pathId;

    public PathXpRewardsPreview CreatePreview(
        ProjectModel project,
        string pathId,
        int multiplier)
    {
        ValidateMultiplier(multiplier);
        PathXpRewardTargets targets = ResolveTargets(project, pathId);
        JArray current = Capture(targets);
        GameplayOperationStateModel? state = stateService.FindState(
            project, GetOperationType(pathId));
        JArray baseline;
        if (state == null)
        {
            baseline = current;
        }
        else
        {
            ValidateState(project, state);
            baseline = (JArray)state.BaselineArray.DeepClone();
        }
        JArray expected = BuildExpected(baseline, multiplier);
        int[] currentValues = ReadValues(current);
        int[] proposedValues = ReadValues(expected);
        return new PathXpRewardsPreview(
            pathId,
            GetDisplayName(pathId),
            currentValues.Length,
            currentValues.Min(),
            currentValues.Max(),
            proposedValues.Min(),
            proposedValues.Max(),
            currentValues.Sum(value => (long)value),
            proposedValues.Sum(value => (long)value),
            CountExpectedChanges(current, expected));
    }

    public ProjectMutationResult Apply(
        ProjectModel project,
        string pathId,
        int multiplier) =>
        ApplyCore(project, pathId, multiplier, new ProjectMutationResult());

    internal ProjectMutationResult Apply(
        ProjectModel project,
        string pathId,
        int multiplier,
        ProjectOperationExecutionContext context) =>
        ApplyCore(project, pathId, multiplier, context.MutationResult);

    internal ProjectMutationResult Replay(
        ProjectModel project,
        string pathId,
        int multiplier,
        JArray? exactSourceBaseline,
        ProjectOperationExecutionContext context) =>
        ApplyCore(project, pathId, multiplier, context.MutationResult,
            replay: true, exactSourceBaseline: exactSourceBaseline);

    public ProjectMutationResult RestorePreviousValues(
        ProjectModel project,
        string pathId) =>
        RestoreCore(project, pathId, new ProjectMutationResult());

    internal ProjectMutationResult RestorePreviousValues(
        ProjectModel project,
        string pathId,
        ProjectOperationExecutionContext context) =>
        RestoreCore(project, pathId, context.MutationResult);

    private ProjectMutationResult ApplyCore(
        ProjectModel project,
        string pathId,
        int multiplier,
        ProjectMutationResult result,
        bool replay = false,
        JArray? exactSourceBaseline = null)
    {
        ValidatePathId(pathId);
        ValidateMultiplier(multiplier);
        ProgressionType type = GetOperationType(pathId);
        PathXpRewardTargets targets = ResolveTargets(project, pathId);
        GameplayOperationStateModel? existing = stateService.FindState(project, type);
        JArray baseline = replay
            ? exactSourceBaseline == null
                ? Capture(targets)
                : (JArray)exactSourceBaseline.DeepClone()
            : existing == null
                ? Capture(targets)
                : CompatibleBaseline(project, existing);
        ValidateBaseline(baseline, pathId);
        JArray current = Capture(targets);
        if (replay && !SameShape(baseline, current))
            throw CompatibilityFailure("Path challenge reward membership changed.");
        JArray expected = BuildExpected(baseline, multiplier);

        if (existing != null &&
            JToken.DeepEquals(current, expected) &&
            ReadMultiplier(existing) == multiplier &&
            (!replay || JToken.DeepEquals(existing.BaselineArray, baseline)))
        {
            return result;
        }

        ApplyExpected(targets, current, expected, result);
        GameplayOperationStateModel replacement = CreateState(
            type, pathId, baseline, expected, multiplier);
        result.AddGameplayOperationState(
            project, existing?.DeepClone(), replacement,
            project.IsGameplayOperationStateModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    private ProjectMutationResult RestoreCore(
        ProjectModel project,
        string pathId,
        ProjectMutationResult result)
    {
        ValidatePathId(pathId);
        ProgressionType type = GetOperationType(pathId);
        GameplayOperationStateModel existing =
            stateService.GetRequiredPreviousValuesState(project, type);
        PathXpRewardTargets targets = ResolveTargets(project, pathId);
        JArray baseline = (JArray)existing.BaselineArray.DeepClone();
        ValidateBaseline(baseline, pathId);
        JArray current = Capture(targets);
        if (!SameShape(baseline, current))
            throw CompatibilityFailure("Path challenge reward membership changed.");
        if (JToken.DeepEquals(current, baseline) && ReadMultiplier(existing) == 1)
            return result;

        ApplyExpected(targets, current, baseline, result);
        GameplayOperationStateModel replacement = CreateState(
            type, pathId, baseline, baseline, 1);
        result.AddGameplayOperationState(
            project, existing.DeepClone(), replacement,
            project.IsGameplayOperationStateModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    internal static PathXpRewardTargets ResolveTargets(
        ProjectModel project,
        string pathId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ValidatePathId(pathId);
        List<SheetModel> sheets = project.Sheets.Where(sheet =>
            string.Equals(sheet.Name, "counter", StringComparison.Ordinal)).ToList();
        if (sheets.Count != 1)
            throw CompatibilityFailure(sheets.Count == 0
                ? "Path challenge reward data is missing."
                : "Path challenge reward data is ambiguous.");

        List<PathXpRewardTarget> targets = new();
        foreach (EntryModel entry in sheets[0].Entries)
        {
            if (IsOutdated(entry)) continue;
            PropertyModel? pathProperty = FindUniqueProperty(entry, PathPropertyPath, required: false);
            if (pathProperty == null) continue;
            if (pathProperty.SourceProperty?.Value.Type != JTokenType.String)
                throw CompatibilityFailure($"Challenge '{entry.Id}' has a malformed Path discriminator.");
            string candidatePath = pathProperty.SourceProperty.Value.Value<string>() ?? string.Empty;
            if (!PathIds.Contains(candidatePath, StringComparer.Ordinal))
                continue;
            if (!string.Equals(candidatePath, pathId, StringComparison.Ordinal))
                continue;
            PropertyModel reward = FindUniqueProperty(entry, RewardPropertyPath, required: true)!;
            int value = ReadPositiveInt(reward, entry.Id);
            targets.Add(new(entry, reward, value));
        }

        if (targets.Count == 0)
            throw CompatibilityFailure(
                "No supported Path XP reward targets were found.");
        if (targets.GroupBy(target => target.Entry.Id, StringComparer.Ordinal)
            .Any(group => group.Count() != 1))
        {
            throw CompatibilityFailure("Path challenge reward IDs are duplicated.");
        }
        return new(pathId, targets.OrderBy(target => target.Entry.Id,
            StringComparer.Ordinal).ToArray());
    }

    internal static JArray Capture(PathXpRewardTargets targets) =>
        new(targets.Targets.Select(target => Record(
            targets.PathId,
            target.Entry.Id,
            target.Property.GetCurrentValueSnapshot())));

    internal static JArray BuildExpected(JArray baseline, int multiplier)
    {
        ValidateMultiplier(multiplier);
        JArray expected = (JArray)baseline.DeepClone();
        foreach (JObject record in expected.OfType<JObject>())
        {
            int original = ReadRecordValue(record);
            record["value"] = checked(original * multiplier);
        }
        return expected;
    }

    internal static int CountExpectedChanges(JArray current, JArray expected)
    {
        if (!SameShape(current, expected))
            throw new InvalidOperationException(
                "Path challenge reward preview membership changed.");
        return current.Zip(expected, (left, right) =>
            JToken.DeepEquals(left!["value"], right!["value"]) ? 0 : 1).Sum();
    }

    internal static void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        string pathId = GetPathId(state.OperationType);
        int multiplier = ReadMultiplier(state);
        ValidateMultiplier(multiplier);
        if (state.GameplaySettings?.Properties().Count() != 1)
            throw new InvalidOperationException("The saved Path XP Rewards settings are invalid.");
        PathXpRewardTargets targets = ResolveTargets(project, pathId);
        JArray current = Capture(targets);
        JArray expected = BuildExpected(state.BaselineArray, multiplier);
        string entries = JoinEntries(state.BaselineArray);
        string sheets = string.Join(",", Enumerable.Repeat("counter", state.BaselineArray.Count));
        string paths = string.Join("|", Enumerable.Repeat(RewardPropertyPath, state.BaselineArray.Count));
        if (state.ElementCount != state.BaselineArray.Count ||
            state.ElementCount != targets.Targets.Count ||
            !string.Equals(state.TargetSheet, sheets, StringComparison.Ordinal) ||
            !string.Equals(state.TargetEntry, entries, StringComparison.Ordinal) ||
            !string.Equals(state.TargetPath, paths, StringComparison.Ordinal) ||
            !FingerprintEquals(state.BaselineArray, state.BaselineFingerprint, false) ||
            !FingerprintEquals(state.BaselineArray, state.ElementShapeFingerprint, true) ||
            !FingerprintEquals(expected, state.ExpectedCurrentFingerprint, false) ||
            !JToken.DeepEquals(current, expected))
        {
            throw new InvalidOperationException(
                "The saved Path XP Rewards settings no longer match the loaded project.");
        }
        ValidateBaseline(state.BaselineArray, pathId);
    }

    internal static bool TryGetProfileMultiplier(
        ProjectModel project,
        ProgressionType type,
        out int multiplier)
    {
        multiplier = 1;
        _ = GetPathId(type);
        GameplayOperationStateModel? state = project.GameplayOperationStates
            .SingleOrDefault(candidate => candidate.OperationType == type);
        if (state == null) return false;
        ValidateState(project, state);
        multiplier = ReadMultiplier(state);
        return multiplier != 1 &&
            !JToken.DeepEquals(state.BaselineArray,
                BuildExpected(state.BaselineArray, multiplier));
    }

    internal static void ValidateProfileMultiplier(int multiplier) =>
        ValidateMultiplier(multiplier);

    internal static ProgressionType GetOperationType(string pathId) => pathId switch
    {
        MightPathId => ProgressionType.PathXpRewardsMight,
        TradePathId => ProgressionType.PathXpRewardsTrade,
        CrimePathId => ProgressionType.PathXpRewardsCrime,
        MysteryPathId => ProgressionType.PathXpRewardsMystery,
        _ => throw new ArgumentOutOfRangeException(nameof(pathId))
    };

    internal static string GetPathId(ProgressionType type) => type switch
    {
        ProgressionType.PathXpRewardsMight => MightPathId,
        ProgressionType.PathXpRewardsTrade => TradePathId,
        ProgressionType.PathXpRewardsCrime => CrimePathId,
        ProgressionType.PathXpRewardsMystery => MysteryPathId,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private void ApplyExpected(
        PathXpRewardTargets targets,
        JArray current,
        JArray expected,
        ProjectMutationResult result)
    {
        Dictionary<string, JObject> expectedById = expected.OfType<JObject>()
            .ToDictionary(record => record.Value<string>("entry")!, StringComparer.Ordinal);
        foreach ((PathXpRewardTarget target, int index) in
                 targets.Targets.Select((target, index) => (target, index)))
        {
            JObject record = expectedById[target.Entry.Id];
            if (JToken.DeepEquals(current[index]!["value"], record["value"])) continue;
            result.Merge(mutationService.EnsurePropertyByPath(
                target.Entry, RewardPropertyPath,
                new JValue(ReadRecordValue(record))));
        }
    }

    private static GameplayOperationStateModel CreateState(
        ProgressionType type,
        string pathId,
        JArray baseline,
        JArray expected,
        int multiplier) => new()
    {
        OperationType = type,
        TargetSheet = string.Join(",", Enumerable.Repeat("counter", baseline.Count)),
        TargetEntry = JoinEntries(baseline),
        TargetPath = string.Join("|", Enumerable.Repeat(RewardPropertyPath, baseline.Count)),
        BaselineArray = (JArray)baseline.DeepClone(),
        AppliedPercentage = 100,
        GameplaySettings = new JObject { ["multiplier"] = multiplier },
        BaselineFingerprint = ContentFingerprint(baseline),
        ExpectedCurrentFingerprint = ContentFingerprint(expected),
        ElementCount = baseline.Count,
        ElementShapeFingerprint = ShapeFingerprint(baseline)
    };

    private static JArray CompatibleBaseline(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        ValidateState(project, state);
        return (JArray)state.BaselineArray.DeepClone();
    }

    private static PropertyModel? FindUniqueProperty(
        EntryModel entry,
        string path,
        bool required)
    {
        List<PropertyModel> properties = entry.Properties.Where(property =>
            string.Equals(property.EffectivePropertyPath, path,
                StringComparison.Ordinal)).ToList();
        if (properties.Count > 1 || (required && properties.Count == 0))
            throw CompatibilityFailure($"Challenge '{entry.Id}' has invalid '{path}' data.");
        return properties.SingleOrDefault();
    }

    private static bool IsOutdated(EntryModel entry)
    {
        if (entry.Id.StartsWith("Outdated", StringComparison.OrdinalIgnoreCase)) return true;
        JToken? marker = entry.SourceEntry?["outdated"] ?? entry.SourceEntry?["isOutdated"];
        return marker?.Type == JTokenType.Boolean && marker.Value<bool>();
    }

    private static int ReadPositiveInt(PropertyModel property, string entryId)
    {
        if (property.SourceProperty?.Value.Type != JTokenType.Integer)
            throw CompatibilityFailure($"Challenge '{entryId}' has a non-integer Path reward.");
        try
        {
            int value = property.SourceProperty.Value.Value<int>();
            if (value < 1)
                throw CompatibilityFailure($"Challenge '{entryId}' has a non-positive Path reward.");
            return value;
        }
        catch (Exception exception) when (exception is OverflowException or FormatException)
        {
            throw CompatibilityFailure($"Challenge '{entryId}' has a Path reward outside the supported range.");
        }
    }

    private static JObject Record(string pathId, string entry, JToken value) => new()
    {
        ["sheet"] = "counter", ["entry"] = entry,
        ["targetPath"] = RewardPropertyPath, ["path"] = pathId,
        ["value"] = value.DeepClone()
    };

    private static void ValidateBaseline(JArray records, string pathId)
    {
        if (records.Count == 0)
            throw new InvalidOperationException("The remembered Path reward baseline is empty.");
        string? prior = null;
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (JToken token in records)
        {
            if (token is not JObject record ||
                record.Value<string>("sheet") != "counter" ||
                record.Value<string>("targetPath") != RewardPropertyPath ||
                record.Value<string>("path") != pathId ||
                string.IsNullOrWhiteSpace(record.Value<string>("entry")))
                throw new InvalidOperationException("The remembered Path reward baseline is invalid.");
            string id = record.Value<string>("entry")!;
            if (!ids.Add(id) || (prior != null && StringComparer.Ordinal.Compare(prior, id) >= 0))
                throw new InvalidOperationException("The remembered Path reward membership is invalid.");
            _ = ReadRecordValue(record);
            prior = id;
        }
    }

    private static int[] ReadValues(JArray records) =>
        records.OfType<JObject>().Select(ReadRecordValue).ToArray();

    private static int ReadRecordValue(JObject record)
    {
        if (record["value"]?.Type != JTokenType.Integer)
            throw new InvalidOperationException("A remembered Path reward is not an integer.");
        int value = record["value"]!.Value<int>();
        if (value < 1)
            throw new InvalidOperationException("A remembered Path reward is not positive.");
        return value;
    }

    private static int ReadMultiplier(GameplayOperationStateModel state)
    {
        if (state.GameplaySettings?["multiplier"]?.Type != JTokenType.Integer)
            throw new InvalidOperationException("The saved Path XP Rewards selection is invalid.");
        return state.GameplaySettings["multiplier"]!.Value<int>();
    }

    private static void ValidatePathId(string pathId)
    {
        if (!PathIds.Contains(pathId, StringComparer.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(pathId));
    }

    private static void ValidateMultiplier(int multiplier)
    {
        if (multiplier is < 1 or > 5)
            throw new InvalidOperationException("Select Original, 2×, 3×, 4×, or 5×.");
    }

    private static string JoinEntries(JArray baseline) => string.Join(",",
        baseline.OfType<JObject>().Select(record => record.Value<string>("entry")));
    private static bool SameShape(JArray left, JArray right) =>
        string.Equals(ShapeFingerprint(left), ShapeFingerprint(right), StringComparison.Ordinal);
    private static bool FingerprintEquals(JArray value, string fingerprint, bool shape) =>
        string.Equals(shape ? ShapeFingerprint(value) : ContentFingerprint(value),
            fingerprint, StringComparison.Ordinal);
    private static string ContentFingerprint(JArray value) =>
        GameplayOperationFingerprintService.CreateContentFingerprint(value);
    private static string ShapeFingerprint(JArray value) =>
        GameplayOperationFingerprintService.CreateShapeFingerprint(value);
    private static InvalidOperationException CompatibilityFailure(string message) => new(message);
}

internal sealed record PathXpRewardTarget(
    EntryModel Entry,
    PropertyModel Property,
    int Value);

internal sealed record PathXpRewardTargets(
    string PathId,
    IReadOnlyList<PathXpRewardTarget> Targets);
