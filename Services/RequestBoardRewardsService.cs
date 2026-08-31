using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class RequestBoardRewardsService
{
    public const string PropertyPath = "valueDifficulty";
    public const string MinimumEntryId = "MissionGoldMinDifficulty";
    public const string MaximumEntryId = "MissionGoldMaxDifficulty";

    private const string SheetName = "constant";

    internal static IReadOnlyList<RequestBoardRewardTargetDefinition>
        TargetDefinitions { get; } =
        new[]
        {
            new RequestBoardRewardTargetDefinition(MinimumEntryId, "minimum"),
            new RequestBoardRewardTargetDefinition(MaximumEntryId, "maximum")
        };

    public static IReadOnlyList<RequestBoardRewardsPresetOption>
        Presets { get; } =
        new[]
        {
            new RequestBoardRewardsPresetOption(
                100,
                "100%",
                "Current base reward range"),
            new RequestBoardRewardsPresetOption(
                150,
                "150%",
                "1.5× base rewards"),
            new RequestBoardRewardsPresetOption(
                200,
                "200%",
                "2× base rewards"),
            new RequestBoardRewardsPresetOption(
                300,
                "300%",
                "3× base rewards")
        };

    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;

    public RequestBoardRewardsService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService)
    {
        this.mutationService = mutationService
            ?? throw new ArgumentNullException(nameof(mutationService));
        this.stateService = stateService
            ?? throw new ArgumentNullException(nameof(stateService));
    }

    public int DetectPercentage(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        _ = ResolveTargets(project);
        GameplayOperationStateModel? state = stateService.FindState(
            project,
            ProgressionType.RequestBoardRewards);
        if (state == null)
            return 100;

        stateService.ValidateState(project, state);
        if (!state.IsCompatible)
            throw new InvalidOperationException(state.CompatibilityMessage);

        return ReadPercentage(state);
    }

    public RequestBoardRewardsPreview CreatePreview(
        ProjectModel project,
        int percentage)
    {
        ArgumentNullException.ThrowIfNull(project);
        ValidatePercentage(percentage, allowProfilePercentage: false);
        RequestBoardRewardTargets targets = ResolveTargets(project);
        JArray current = Capture(targets);
        GameplayOperationStateModel? state = stateService.FindState(
            project,
            ProgressionType.RequestBoardRewards);
        JArray baseline;
        if (state == null)
        {
            baseline = current;
        }
        else
        {
            stateService.ValidateState(project, state);
            if (!state.IsCompatible)
                throw new InvalidOperationException(state.CompatibilityMessage);
            baseline = (JArray)state.BaselineArray.DeepClone();
        }

        JArray expected = BuildExpected(baseline, percentage);
        RewardArrayPair currentPair = ReadEnvelope(current);
        RewardArrayPair expectedPair = ReadEnvelope(expected);
        return new RequestBoardRewardsPreview(
            currentPair.Minimum.Records.Count,
            currentPair.Minimum.Records.Values.Min(record =>
                ReadInteger(record["value"]!, "reward value")),
            currentPair.Maximum.Records.Values.Max(record =>
                ReadInteger(record["value"]!, "reward value")),
            expectedPair.Minimum.Records.Values.Min(record =>
                ReadInteger(record["value"]!, "reward value")),
            expectedPair.Maximum.Records.Values.Max(record =>
                ReadInteger(record["value"]!, "reward value")));
    }

    public bool CanRestorePreviousValues(ProjectModel project) =>
        stateService.CanRestorePreviousValues(
            project,
            ProgressionType.RequestBoardRewards);

    public ProjectMutationResult Apply(
        ProjectModel project,
        int percentage) =>
        ApplyCore(
            project,
            percentage,
            new ProjectMutationResult());

    internal ProjectMutationResult Apply(
        ProjectModel project,
        int percentage,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ApplyCore(project, percentage, context.MutationResult);
    }

    public ProjectMutationResult RestorePreviousValues(ProjectModel project) =>
        RestorePreviousValuesCore(project, new ProjectMutationResult());

    internal ProjectMutationResult RestorePreviousValues(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RestorePreviousValuesCore(project, context.MutationResult);
    }

    private ProjectMutationResult ApplyCore(
        ProjectModel project,
        int percentage,
        ProjectMutationResult result)
    {
        ArgumentNullException.ThrowIfNull(project);
        ValidatePercentage(percentage, allowProfilePercentage: false);
        RequestBoardRewardTargets targets = ResolveTargets(project);
        GameplayOperationStateModel? existing = stateService.FindState(
            project,
            ProgressionType.RequestBoardRewards);
        JArray baseline;
        if (existing == null)
        {
            baseline = Capture(targets);
        }
        else
        {
            stateService.ValidateState(project, existing);
            if (!existing.IsCompatible)
                throw new InvalidOperationException(existing.CompatibilityMessage);
            baseline = (JArray)existing.BaselineArray.DeepClone();
        }

        JArray expected = BuildExpected(baseline, percentage);
        JArray current = Capture(targets);

        if (existing == null && JToken.DeepEquals(current, expected))
            return result;

        if (existing != null &&
            JToken.DeepEquals(current, expected) &&
            ReadPercentage(existing) == percentage)
        {
            return result;
        }

        if (!JToken.DeepEquals(current, expected))
            ApplyExpected(targets, expected, result);

        GameplayOperationStateModel replacement =
            CreateState(baseline, expected, percentage);
        GameplayOperationStateModel? previous = existing?.DeepClone();
        bool previousModified = project.IsGameplayOperationStateModified;
        result.AddGameplayOperationState(
            project,
            previous,
            replacement,
            previousModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    private ProjectMutationResult RestorePreviousValuesCore(
        ProjectModel project,
        ProjectMutationResult result)
    {
        GameplayOperationStateModel existing =
            stateService.GetRequiredPreviousValuesState(
                project,
                ProgressionType.RequestBoardRewards);
        RequestBoardRewardTargets targets = ResolveTargets(project);
        JArray baseline = (JArray)existing.BaselineArray.DeepClone();
        _ = ReadEnvelope(baseline);
        JArray current = Capture(targets);
        if (JToken.DeepEquals(current, baseline) &&
            ReadPercentage(existing) == 100)
        {
            return result;
        }

        ApplyExpected(targets, baseline, result);
        GameplayOperationStateModel replacement =
            CreateState(baseline, baseline, 100);
        bool previousModified = project.IsGameplayOperationStateModified;
        result.AddGameplayOperationState(
            project,
            existing.DeepClone(),
            replacement,
            previousModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    internal static RequestBoardRewardTargets ResolveTargets(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        List<SheetModel> sheets = project.Sheets
            .Where(sheet => string.Equals(
                sheet.Name,
                SheetName,
                StringComparison.Ordinal))
            .ToList();
        if (sheets.Count == 0)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.MissingTarget);
        if (sheets.Count != 1)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.AmbiguousTarget);

        RequestBoardRewardTarget minimum = ResolveTarget(
            sheets[0],
            TargetDefinitions[0]);
        RequestBoardRewardTarget maximum = ResolveTarget(
            sheets[0],
            TargetDefinitions[1]);
        ValidatePair(minimum.Array, maximum.Array);
        return new RequestBoardRewardTargets(minimum, maximum);
    }

    internal static JArray Capture(RequestBoardRewardTargets targets) =>
        new(
            CreateEnvelopeRecord(targets.Minimum),
            CreateEnvelopeRecord(targets.Maximum));

    internal static JArray BuildExpected(
        JArray baseline,
        int percentage)
    {
        ValidatePercentage(percentage, allowProfilePercentage: false);
        JArray expected = (JArray)baseline.DeepClone();
        RewardArrayPair pair = ReadEnvelope(expected);
        ScaleArray(pair.Minimum, percentage);
        ScaleArray(pair.Maximum, percentage);
        ValidatePair(pair.Minimum.Array, pair.Maximum.Array);
        return expected;
    }

    internal static void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        if (state.OperationType != ProgressionType.RequestBoardRewards)
            throw new InvalidOperationException(
                "The saved Request Board Rewards operation type is invalid.");
        int percentage = ReadPercentage(state);
        ValidatePercentage(percentage, allowProfilePercentage: false);
        if (state.AppliedPercentage != percentage ||
            state.GameplaySettings?.Properties().Count() != 1)
        {
            throw new InvalidOperationException(
                "The saved Request Board Rewards preset is invalid.");
        }

        RequestBoardRewardTargets targets = ResolveTargets(project);
        JArray expected = BuildExpected(state.BaselineArray, percentage);
        JArray current = Capture(targets);
        if (state.ElementCount != TargetDefinitions.Count ||
            state.BaselineArray.Count != TargetDefinitions.Count ||
            !string.Equals(
                state.TargetSheet,
                "constant,constant",
                StringComparison.Ordinal) ||
            !string.Equals(
                state.TargetEntry,
                $"{MinimumEntryId},{MaximumEntryId}",
                StringComparison.Ordinal) ||
            !string.Equals(
                state.TargetPath,
                $"{PropertyPath}|{PropertyPath}",
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(
                    state.BaselineArray),
                state.BaselineFingerprint,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService.CreateShapeFingerprint(
                    state.BaselineArray),
                state.ElementShapeFingerprint,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(
                    expected),
                state.ExpectedCurrentFingerprint,
                StringComparison.Ordinal) ||
            !JToken.DeepEquals(current, expected))
        {
            throw new InvalidOperationException(
                "The saved Request Board Rewards settings no longer match the loaded project.");
        }
    }

    internal static bool TryGetProfilePercentage(
        ProjectModel project,
        out int percentage)
    {
        percentage = 100;
        GameplayOperationStateModel? state = project.GameplayOperationStates
            .SingleOrDefault(candidate =>
                candidate.OperationType ==
                ProgressionType.RequestBoardRewards);
        if (state == null)
            return false;

        ValidateState(project, state);
        percentage = ReadPercentage(state);
        return percentage is 150 or 200 or 300 &&
            !JToken.DeepEquals(
                state.BaselineArray,
                BuildExpected(state.BaselineArray, percentage));
    }

    internal static void ValidateProfilePercentage(int percentage) =>
        ValidatePercentage(percentage, allowProfilePercentage: true);

    private void ApplyExpected(
        RequestBoardRewardTargets targets,
        JArray expected,
        ProjectMutationResult result)
    {
        RewardArrayPair pair = ReadEnvelope(expected);
        result.Merge(mutationService.EnsurePropertyByPath(
            targets.Minimum.Entry,
            PropertyPath,
            pair.Minimum.Array));
        result.Merge(mutationService.EnsurePropertyByPath(
            targets.Maximum.Entry,
            PropertyPath,
            pair.Maximum.Array));
    }

    private static RequestBoardRewardTarget ResolveTarget(
        SheetModel sheet,
        RequestBoardRewardTargetDefinition definition)
    {
        List<EntryModel> entries = sheet.Entries
            .Where(entry => string.Equals(
                entry.Id,
                definition.EntryId,
                StringComparison.Ordinal))
            .ToList();
        if (entries.Count == 0)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.MissingTarget);
        if (entries.Count != 1)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.AmbiguousTarget);

        List<PropertyModel> properties = entries[0].Properties
            .Where(property => string.Equals(
                property.EffectivePropertyPath,
                PropertyPath,
                StringComparison.Ordinal))
            .ToList();
        if (properties.Count == 0)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.MissingTarget);
        if (properties.Count != 1)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.AmbiguousTarget);
        if (properties[0].SourceProperty?.Value is not JArray array)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.TypeChanged);

        ParsedRewardArray parsed = ParseArray(array);
        return new RequestBoardRewardTarget(
            definition,
            entries[0],
            properties[0],
            array,
            parsed.Records);
    }

    private static ParsedRewardArray ParseArray(JArray array)
    {
        if (array.Count == 0)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.StructureChanged);

        Dictionary<long, JObject> records = new();
        foreach (JToken token in array)
        {
            if (token is not JObject record)
                throw CompatibilityFailure(
                    GameplayCompatibilityStatus.StructureChanged);
            if (record["difficulty"]?.Type != JTokenType.Integer ||
                record["value"]?.Type != JTokenType.Integer)
            {
                throw CompatibilityFailure(
                    GameplayCompatibilityStatus.TypeChanged);
            }

            long discriminator;
            _ = ReadInteger(record["value"]!, "reward value");
            try
            {
                discriminator = record["difficulty"]!.Value<long>();
            }
            catch (Exception exception) when (
                exception is OverflowException or FormatException)
            {
                throw CompatibilityFailure(
                    GameplayCompatibilityStatus.TypeChanged);
            }

            if (!records.TryAdd(discriminator, record))
                throw CompatibilityFailure(
                    GameplayCompatibilityStatus.StructureChanged);
        }

        return new ParsedRewardArray(array, records);
    }

    private static RewardArrayPair ReadEnvelope(JArray envelope)
    {
        if (envelope.Count != TargetDefinitions.Count)
            throw new InvalidOperationException(
                "The remembered Request Board reward baseline is incomplete.");

        ParsedRewardArray minimum = ReadEnvelopeRecord(
            envelope[0],
            TargetDefinitions[0]);
        ParsedRewardArray maximum = ReadEnvelopeRecord(
            envelope[1],
            TargetDefinitions[1]);
        ValidatePair(minimum.Array, maximum.Array);
        return new RewardArrayPair(minimum, maximum);
    }

    private static ParsedRewardArray ReadEnvelopeRecord(
        JToken? token,
        RequestBoardRewardTargetDefinition definition)
    {
        if (token is not JObject record ||
            !string.Equals(
                record.Value<string>("sheet"),
                SheetName,
                StringComparison.Ordinal) ||
            !string.Equals(
                record.Value<string>("entry"),
                definition.EntryId,
                StringComparison.Ordinal) ||
            !string.Equals(
                record.Value<string>("targetPath"),
                PropertyPath,
                StringComparison.Ordinal) ||
            record["value"] is not JArray array)
        {
            throw new InvalidOperationException(
                "The remembered Request Board reward baseline is invalid.");
        }

        return ParseArray(array);
    }

    private static JObject CreateEnvelopeRecord(
        RequestBoardRewardTarget target) =>
        new()
        {
            ["sheet"] = SheetName,
            ["entry"] = target.Definition.EntryId,
            ["targetPath"] = PropertyPath,
            ["value"] = target.Property.GetCurrentValueSnapshot()
        };

    private static void ScaleArray(
        ParsedRewardArray parsed,
        int percentage)
    {
        foreach (JObject record in parsed.Records.Values)
        {
            long baseline = ReadInteger(
                record["value"]!,
                "reward value");
            long scaled = checked((long)Math.Round(
                baseline * (decimal)percentage / 100m,
                0,
                MidpointRounding.AwayFromZero));
            record["value"] = new JValue(scaled);
        }
    }

    private static void ValidatePair(
        JArray minimum,
        JArray maximum)
    {
        ParsedRewardArray min = ParseArray(minimum);
        ParsedRewardArray max = ParseArray(maximum);
        if (!min.Records.Keys.ToHashSet().SetEquals(max.Records.Keys))
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.StructureChanged);

        foreach (long discriminator in min.Records.Keys)
        {
            long minimumValue = ReadInteger(
                min.Records[discriminator]["value"]!,
                "minimum reward value");
            long maximumValue = ReadInteger(
                max.Records[discriminator]["value"]!,
                "maximum reward value");
            if (minimumValue > maximumValue)
                throw new InvalidOperationException(
                    "Request Board reward ranges are incomplete or incompatible.");
        }
    }

    private static long ReadInteger(JToken token, string description)
    {
        if (token.Type != JTokenType.Integer)
            throw CompatibilityFailure(
                GameplayCompatibilityStatus.TypeChanged);
        try
        {
            return token.Value<long>();
        }
        catch (Exception exception) when (
            exception is OverflowException or FormatException)
        {
            throw new InvalidOperationException(
                $"The Request Board {description} is outside the supported integer range.");
        }
    }

    private static int ReadPercentage(GameplayOperationStateModel state)
    {
        if (state.GameplaySettings?["percentage"]?.Type !=
            JTokenType.Integer)
        {
            throw new InvalidOperationException(
                "The saved Request Board Rewards preset is invalid.");
        }

        return state.GameplaySettings["percentage"]!.Value<int>();
    }

    private static void ValidatePercentage(
        int percentage,
        bool allowProfilePercentage)
    {
        bool valid = allowProfilePercentage
            ? percentage is 150 or 200 or 300
            : percentage is 100 or 150 or 200 or 300;
        if (!valid)
            throw new InvalidOperationException(
                "Select one of the supported Request Board Rewards presets.");
    }

    private static GameplayOperationStateModel CreateState(
        JArray baseline,
        JArray expected,
        int percentage) =>
        new()
        {
            OperationType = ProgressionType.RequestBoardRewards,
            TargetSheet = "constant,constant",
            TargetEntry = $"{MinimumEntryId},{MaximumEntryId}",
            TargetPath = $"{PropertyPath}|{PropertyPath}",
            BaselineArray = (JArray)baseline.DeepClone(),
            AppliedPercentage = percentage,
            GameplaySettings = new JObject
            {
                ["percentage"] = percentage
            },
            BaselineFingerprint =
                GameplayOperationFingerprintService.CreateContentFingerprint(
                    baseline),
            ExpectedCurrentFingerprint =
                GameplayOperationFingerprintService.CreateContentFingerprint(
                    expected),
            ElementCount = TargetDefinitions.Count,
            ElementShapeFingerprint =
                GameplayOperationFingerprintService.CreateShapeFingerprint(
                    baseline),
            IsCompatible = true
        };

    private static GameplayCompatibilityException CompatibilityFailure(
        GameplayCompatibilityStatus status) =>
        new(
            status,
            status is GameplayCompatibilityStatus.MissingTarget or
                GameplayCompatibilityStatus.AmbiguousTarget
                ? "Request Board reward data could not be recognized in this game version."
                : "Request Board reward ranges are incomplete or incompatible.");
}

internal sealed record RequestBoardRewardTargetDefinition(
    string EntryId,
    string RangeName);

internal sealed record RequestBoardRewardTarget(
    RequestBoardRewardTargetDefinition Definition,
    EntryModel Entry,
    PropertyModel Property,
    JArray Array,
    IReadOnlyDictionary<long, JObject> Records);

internal sealed record RequestBoardRewardTargets(
    RequestBoardRewardTarget Minimum,
    RequestBoardRewardTarget Maximum);

internal sealed record ParsedRewardArray(
    JArray Array,
    IReadOnlyDictionary<long, JObject> Records);

internal sealed record RewardArrayPair(
    ParsedRewardArray Minimum,
    ParsedRewardArray Maximum);
