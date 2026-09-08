using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class PathLevelRequirementsService
{
    public const string BaseEntryId = "PathXpBase";
    public const string NextEntryId = "PathXpNext";
    public const string MaxLevelEntryId = "PathMaxLevel";
    public const string PropertyPath = "value";

    public static IReadOnlyList<PathLevelRequirementOption> Options { get; } =
    [
        new(100, "Original"),
        new(80, "80%"),
        new(60, "60%"),
        new(40, "40%"),
        new(20, "20%")
    ];

    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;

    public PathLevelRequirementsService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService)
    {
        this.mutationService = mutationService ??
            throw new ArgumentNullException(nameof(mutationService));
        this.stateService = stateService ??
            throw new ArgumentNullException(nameof(stateService));
    }

    public int DetectPercentage(ProjectModel project)
    {
        _ = ResolveTargets(project);
        GameplayOperationStateModel? state = stateService.FindState(
            project, ProgressionType.PathLevelRequirements);
        if (state == null) return 100;
        RequireCompatibleState(project, state);
        return ReadPercentage(state);
    }

    public bool CanRestorePreviousValues(ProjectModel project) =>
        stateService.CanRestorePreviousValues(
            project, ProgressionType.PathLevelRequirements);

    public PathLevelRequirementsPreview CreatePreview(
        ProjectModel project,
        int percentage)
    {
        ValidatePercentage(percentage);
        PathLevelRequirementTargets targets = ResolveTargets(project);
        JArray current = Capture(targets);
        GameplayOperationStateModel? state = stateService.FindState(
            project, ProgressionType.PathLevelRequirements);
        JArray baseline = state == null
            ? current
            : CompatibleBaseline(project, state);
        JArray expected = BuildExpected(baseline, percentage);
        (int currentBase, int currentNext) = ReadPair(current);
        (int baseValue, int nextValue) = ReadPair(baseline);
        (int proposedBase, int proposedNext) = ReadPair(expected);
        int maxLevel = targets.MaxLevel;
        int currentMaximum = Requirement(currentBase, currentNext, maxLevel - 1);
        int proposedMaximum = Requirement(proposedBase, proposedNext, maxLevel - 1);
        bool rounded =
            baseValue * percentage % 100 != 0 ||
            nextValue * percentage % 100 != 0;
        return new PathLevelRequirementsPreview(
            currentBase,
            currentMaximum,
            proposedBase,
            proposedMaximum,
            currentBase,
            currentNext,
            proposedBase,
            proposedNext,
            rounded);
    }

    public ProjectMutationResult Apply(ProjectModel project, int percentage) =>
        ApplyCore(project, percentage, new ProjectMutationResult());

    internal ProjectMutationResult Apply(
        ProjectModel project,
        int percentage,
        ProjectOperationExecutionContext context) =>
        ApplyCore(project, percentage, context.MutationResult);

    internal ProjectMutationResult Replay(
        ProjectModel project,
        int percentage,
        JArray? exactSourceBaseline,
        ProjectOperationExecutionContext context) =>
        ApplyCore(
            project,
            percentage,
            context.MutationResult,
            replay: true,
            exactSourceBaseline: exactSourceBaseline);

    public ProjectMutationResult RestorePreviousValues(ProjectModel project) =>
        RestoreCore(project, new ProjectMutationResult());

    internal ProjectMutationResult RestorePreviousValues(
        ProjectModel project,
        ProjectOperationExecutionContext context) =>
        RestoreCore(project, context.MutationResult);

    private ProjectMutationResult ApplyCore(
        ProjectModel project,
        int percentage,
        ProjectMutationResult result,
        bool replay = false,
        JArray? exactSourceBaseline = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ValidatePercentage(percentage);
        PathLevelRequirementTargets targets = ResolveTargets(project);
        GameplayOperationStateModel? existing = stateService.FindState(
            project, ProgressionType.PathLevelRequirements);
        JArray baseline = replay
            ? exactSourceBaseline == null
                ? Capture(targets)
                : (JArray)exactSourceBaseline.DeepClone()
            : existing == null
                ? Capture(targets)
                : CompatibleBaseline(project, existing);
        ValidateBaseline(baseline);
        JArray current = Capture(targets);
        if (replay && !SameShape(baseline, current))
            throw CompatibilityFailure("Path level requirement structure changed.");

        JArray expected = BuildExpected(baseline, percentage);
        ValidateFormula(expected, targets.MaxLevel);
        if (existing != null &&
            JToken.DeepEquals(current, expected) &&
            ReadPercentage(existing) == percentage &&
            (!replay || JToken.DeepEquals(existing.BaselineArray, baseline)))
        {
            return result;
        }

        ApplyExpected(targets, current, expected, result);
        GameplayOperationStateModel replacement = CreateState(
            baseline, expected, percentage, targets.MaxLevel);
        result.AddGameplayOperationState(
            project,
            existing?.DeepClone(),
            replacement,
            project.IsGameplayOperationStateModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    private ProjectMutationResult RestoreCore(
        ProjectModel project,
        ProjectMutationResult result)
    {
        GameplayOperationStateModel existing =
            stateService.GetRequiredPreviousValuesState(
                project, ProgressionType.PathLevelRequirements);
        PathLevelRequirementTargets targets = ResolveTargets(project);
        JArray baseline = (JArray)existing.BaselineArray.DeepClone();
        JArray current = Capture(targets);
        if (JToken.DeepEquals(current, baseline) && ReadPercentage(existing) == 100)
            return result;

        ApplyExpected(targets, current, baseline, result);
        GameplayOperationStateModel replacement = CreateState(
            baseline, baseline, 100, targets.MaxLevel);
        result.AddGameplayOperationState(
            project,
            existing.DeepClone(),
            replacement,
            project.IsGameplayOperationStateModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    internal static PathLevelRequirementTargets ResolveTargets(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        List<SheetModel> sheets = project.Sheets.Where(sheet =>
            string.Equals(sheet.Name, "constant", StringComparison.Ordinal)).ToList();
        if (sheets.Count != 1)
            throw CompatibilityFailure(sheets.Count == 0
                ? "Path level requirement data is missing."
                : "Path level requirement data is ambiguous.");

        PathLevelRequirementTarget baseTarget = ResolveTarget(sheets[0], BaseEntryId);
        PathLevelRequirementTarget nextTarget = ResolveTarget(sheets[0], NextEntryId);
        PathLevelRequirementTarget maxTarget = ResolveTarget(sheets[0], MaxLevelEntryId);
        int baseValue = ReadPositiveInt(baseTarget.Property, "base requirement");
        int nextValue = ReadPositiveInt(nextTarget.Property, "requirement increase");
        int maxLevel = ReadPositiveInt(maxTarget.Property, "maximum Path level");
        if (maxLevel < 2)
            throw CompatibilityFailure("The maximum Path level must be at least 2.");
        _ = Requirement(baseValue, nextValue, maxLevel - 1);
        return new(baseTarget, nextTarget, maxTarget, maxLevel);
    }

    private static PathLevelRequirementTarget ResolveTarget(
        SheetModel sheet,
        string entryId)
    {
        List<EntryModel> entries = sheet.Entries.Where(entry =>
            string.Equals(entry.Id, entryId, StringComparison.Ordinal)).ToList();
        if (entries.Count != 1)
            throw CompatibilityFailure(entries.Count == 0
                ? $"Required Path constant '{entryId}' is missing."
                : $"Required Path constant '{entryId}' is duplicated.");
        List<PropertyModel> properties = entries[0].Properties.Where(property =>
            string.Equals(property.EffectivePropertyPath, PropertyPath,
                StringComparison.Ordinal)).ToList();
        if (properties.Count != 1 || properties[0].SourceProperty == null)
            throw CompatibilityFailure(
                $"Path constant '{entryId}' does not contain one connected value.");
        if (properties[0].SourceProperty!.Value.Type != JTokenType.Integer)
            throw CompatibilityFailure($"Path constant '{entryId}' must be an integer.");
        return new(entries[0], properties[0]);
    }

    internal static JArray Capture(PathLevelRequirementTargets targets) =>
    [
        Record(BaseEntryId, targets.Base.Property.GetCurrentValueSnapshot()),
        Record(NextEntryId, targets.Next.Property.GetCurrentValueSnapshot())
    ];

    internal static JArray BuildExpected(JArray baseline, int percentage)
    {
        ValidatePercentage(percentage);
        (int baseValue, int nextValue) = ReadPair(baseline);
        return
        [
            Record(BaseEntryId, new JValue(Scale(baseValue, percentage))),
            Record(NextEntryId, new JValue(Scale(nextValue, percentage)))
        ];
    }

    internal static void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        if (state.OperationType != ProgressionType.PathLevelRequirements)
            throw new InvalidOperationException("The saved Path Level Requirements operation type is invalid.");
        int percentage = ReadPercentage(state);
        ValidatePercentage(percentage);
        if (state.GameplaySettings?.Properties().Count() != 2 ||
            state.GameplaySettings["pathMaxLevel"]?.Type != JTokenType.Integer)
        {
            throw new InvalidOperationException("The saved Path Level Requirements settings are invalid.");
        }

        PathLevelRequirementTargets targets = ResolveTargets(project);
        int savedMax = state.GameplaySettings["pathMaxLevel"]!.Value<int>();
        JArray expected = BuildExpected(state.BaselineArray, percentage);
        JArray current = Capture(targets);
        if (savedMax != targets.MaxLevel ||
            state.AppliedPercentage != percentage ||
            state.ElementCount != 2 || state.BaselineArray.Count != 2 ||
            !string.Equals(state.TargetSheet, "constant,constant", StringComparison.Ordinal) ||
            !string.Equals(state.TargetEntry, $"{BaseEntryId},{NextEntryId}", StringComparison.Ordinal) ||
            !string.Equals(state.TargetPath, "value|value", StringComparison.Ordinal) ||
            !FingerprintEquals(state.BaselineArray, state.BaselineFingerprint, false) ||
            !FingerprintEquals(state.BaselineArray, state.ElementShapeFingerprint, true) ||
            !FingerprintEquals(expected, state.ExpectedCurrentFingerprint, false) ||
            !JToken.DeepEquals(current, expected))
        {
            throw new InvalidOperationException(
                "The saved Path Level Requirements settings no longer match the loaded project.");
        }
        ValidateFormula(expected, savedMax);
    }

    internal static bool TryGetProfilePercentage(ProjectModel project, out int percentage)
    {
        percentage = 100;
        GameplayOperationStateModel? state = project.GameplayOperationStates
            .SingleOrDefault(candidate => candidate.OperationType ==
                ProgressionType.PathLevelRequirements);
        if (state == null) return false;
        ValidateState(project, state);
        percentage = ReadPercentage(state);
        return percentage != 100 &&
            !JToken.DeepEquals(state.BaselineArray,
                BuildExpected(state.BaselineArray, percentage));
    }

    internal static void ValidateProfilePercentage(int percentage) =>
        ValidatePercentage(percentage);

    private void ApplyExpected(
        PathLevelRequirementTargets targets,
        JArray current,
        JArray expected,
        ProjectMutationResult result)
    {
        (_, _) = ReadPair(current);
        (int baseValue, int nextValue) = ReadPair(expected);
        if (!JToken.DeepEquals(current[0]!["value"], expected[0]!["value"]))
            result.Merge(mutationService.EnsurePropertyByPath(
                targets.Base.Entry, PropertyPath, new JValue(baseValue)));
        if (!JToken.DeepEquals(current[1]!["value"], expected[1]!["value"]))
            result.Merge(mutationService.EnsurePropertyByPath(
                targets.Next.Entry, PropertyPath, new JValue(nextValue)));
    }

    private static GameplayOperationStateModel CreateState(
        JArray baseline,
        JArray expected,
        int percentage,
        int maxLevel) => new()
    {
        OperationType = ProgressionType.PathLevelRequirements,
        TargetSheet = "constant,constant",
        TargetEntry = $"{BaseEntryId},{NextEntryId}",
        TargetPath = "value|value",
        BaselineArray = (JArray)baseline.DeepClone(),
        AppliedPercentage = percentage,
        GameplaySettings = new JObject
        {
            ["percentage"] = percentage,
            ["pathMaxLevel"] = maxLevel
        },
        BaselineFingerprint = ContentFingerprint(baseline),
        ExpectedCurrentFingerprint = ContentFingerprint(expected),
        ElementCount = 2,
        ElementShapeFingerprint = ShapeFingerprint(baseline)
    };

    private static JArray CompatibleBaseline(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        RequireCompatibleState(project, state);
        return (JArray)state.BaselineArray.DeepClone();
    }

    private static void RequireCompatibleState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        ValidateState(project, state);
    }

    private static JObject Record(string entry, JToken value) => new()
    {
        ["sheet"] = "constant",
        ["entry"] = entry,
        ["targetPath"] = PropertyPath,
        ["value"] = value.DeepClone()
    };

    private static (int Base, int Next) ReadPair(JArray records)
    {
        ValidateBaseline(records);
        return (
            ReadRecord(records[0]!, BaseEntryId),
            ReadRecord(records[1]!, NextEntryId));
    }

    private static void ValidateBaseline(JArray records)
    {
        if (records.Count != 2)
            throw new InvalidOperationException("The remembered Path requirement baseline is incomplete.");
        _ = ReadRecord(records[0]!, BaseEntryId);
        _ = ReadRecord(records[1]!, NextEntryId);
    }

    private static int ReadRecord(JToken token, string entry)
    {
        if (token is not JObject record ||
            record.Value<string>("sheet") != "constant" ||
            record.Value<string>("entry") != entry ||
            record.Value<string>("targetPath") != PropertyPath ||
            record["value"]?.Type != JTokenType.Integer)
        {
            throw new InvalidOperationException("The remembered Path requirement baseline is invalid.");
        }
        int value = record["value"]!.Value<int>();
        if (value < 1)
            throw new InvalidOperationException("The remembered Path requirement baseline must be positive.");
        return value;
    }

    private static int ReadPositiveInt(PropertyModel property, string description)
    {
        try
        {
            int value = property.SourceProperty!.Value.Value<int>();
            if (value < 1) throw CompatibilityFailure($"The Path {description} must be positive.");
            return value;
        }
        catch (Exception exception) when (exception is OverflowException or FormatException)
        {
            throw CompatibilityFailure($"The Path {description} is outside the supported integer range.");
        }
    }

    private static int Scale(int value, int percentage)
    {
        decimal scaled = checked(value * (decimal)percentage / 100m);
        decimal rounded = Math.Round(scaled, 0, MidpointRounding.AwayFromZero);
        return Math.Max(1, checked((int)rounded));
    }

    private static int Requirement(int baseValue, int nextValue, int level) =>
        checked(baseValue + checked(level - 1) * nextValue);

    private static void ValidateFormula(JArray values, int maxLevel)
    {
        (int baseValue, int nextValue) = ReadPair(values);
        for (int level = 1; level < maxLevel; level++)
            if (Requirement(baseValue, nextValue, level) < 1)
                throw CompatibilityFailure("A Path level requirement is not positive.");
    }

    private static int ReadPercentage(GameplayOperationStateModel state)
    {
        if (state.GameplaySettings?["percentage"]?.Type != JTokenType.Integer)
            throw new InvalidOperationException("The saved Path Level Requirements selection is invalid.");
        return state.GameplaySettings["percentage"]!.Value<int>();
    }

    private static void ValidatePercentage(int percentage)
    {
        if (percentage is not (100 or 80 or 60 or 40 or 20))
            throw new InvalidOperationException("Select Original, 80%, 60%, 40%, or 20%.");
    }

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

internal sealed record PathLevelRequirementTarget(EntryModel Entry, PropertyModel Property);
internal sealed record PathLevelRequirementTargets(
    PathLevelRequirementTarget Base,
    PathLevelRequirementTarget Next,
    PathLevelRequirementTarget Max,
    int MaxLevel);
