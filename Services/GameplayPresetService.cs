using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class GameplayPresetService
{
    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;

    public GameplayPresetService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService)
    {
        this.mutationService = mutationService
            ?? throw new ArgumentNullException(nameof(mutationService));
        this.stateService = stateService
            ?? throw new ArgumentNullException(nameof(stateService));
    }

    public GameplayPresetOption? DetectPreset(
        ProjectModel project,
        ProgressionType type)
    {
        ArgumentNullException.ThrowIfNull(project);
        GameplayPresetDefinition definition = GameplayPresetCatalog.Get(type);
        IReadOnlyList<ResolvedGameplayTarget> targets = ResolveTargets(project, definition);
        JArray current = CaptureTargets(targets);
        GameplayOperationStateModel? state = stateService.FindState(project, type);

        if (state != null)
        {
            stateService.ValidateState(project, state);
            if (!state.IsCompatible)
                throw new InvalidOperationException(state.CompatibilityMessage);

            string? key = state.GameplaySettings?.Value<string>("preset");
            return definition.Presets.SingleOrDefault(x =>
                string.Equals(x.Key, key, StringComparison.Ordinal));
        }

        return definition.Presets.FirstOrDefault(preset =>
            JToken.DeepEquals(
                current,
                BuildExpected(current, definition, preset)));
    }

    public bool CanRestorePreviousValues(
        ProjectModel project,
        ProgressionType type) =>
        stateService.CanRestorePreviousValues(project, type);

    public ProjectMutationResult RestorePreviousValues(
        ProjectModel project,
        ProgressionType type)
    {
        _ = stateService.GetRequiredPreviousValuesState(project, type);
        return Apply(project, type, "Vanilla");
    }

    public ProjectMutationResult Apply(
        ProjectModel project,
        ProgressionType type,
        string presetKey)
    {
        ArgumentNullException.ThrowIfNull(project);
        GameplayPresetDefinition definition = GameplayPresetCatalog.Get(type);
        GameplayPresetOption preset = GetPreset(definition, presetKey);
        ValidatePreset(definition, preset);

        IReadOnlyList<ResolvedGameplayTarget> targets = ResolveTargets(project, definition);
        GameplayOperationStateModel? existing = stateService.FindState(project, type);
        JArray baseline;

        if (existing == null)
        {
            baseline = CaptureTargets(targets);
        }
        else
        {
            stateService.ValidateState(project, existing);
            if (!existing.IsCompatible)
                throw new InvalidOperationException(existing.CompatibilityMessage);
            baseline = (JArray)existing.BaselineArray.DeepClone();
        }

        JArray expected = BuildExpected(baseline, definition, preset);
        ValidateBaseline(definition, baseline);
        ValidateExpected(definition, preset, expected);
        JArray current = CaptureTargets(targets);
        if (existing != null &&
            JToken.DeepEquals(current, expected) &&
            string.Equals(
                existing.GameplaySettings?.Value<string>("preset"),
                preset.Key,
                StringComparison.Ordinal))
            return new ProjectMutationResult();

        ProjectMutationResult result = new();
        if (!JToken.DeepEquals(current, expected))
            ApplyExpected(project, expected, result);

        GameplayOperationStateModel replacement =
            CreateState(definition, baseline, expected, preset);
        GameplayOperationStateModel? previous = existing?.DeepClone();
        bool previousModified = project.IsGameplayOperationStateModified;
        stateService.ReplaceState(project, replacement);
        result.AddGameplayOperationState(
            project,
            previous,
            replacement,
            previousModified);
        return result;
    }

    internal static void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        GameplayPresetDefinition definition =
            GameplayPresetCatalog.Get(state.OperationType);
        string key = state.GameplaySettings?.Value<string>("preset")
            ?? throw new InvalidOperationException(
                "The selected gameplay preset is missing.");
        GameplayPresetOption preset = GetPreset(definition, key);
        ValidatePreset(definition, preset);

        IReadOnlyList<ResolvedGameplayTarget> targets =
            ResolveTargets(project, definition);
        JArray current = CaptureTargets(targets);
        JArray expected = BuildExpected(state.BaselineArray, definition, preset);
        ValidateBaseline(definition, state.BaselineArray);
        ValidateExpected(definition, preset, expected);
        string sheets = Join(state.BaselineArray, "sheet", ",");
        string entries = Join(state.BaselineArray, "entry", ",");
        string paths = Join(state.BaselineArray, "targetPath", "|");

        if (state.ElementCount != definition.Targets.Count ||
            state.BaselineArray.Count != definition.Targets.Count ||
            !string.Equals(state.TargetSheet, sheets, StringComparison.Ordinal) ||
            !string.Equals(state.TargetEntry, entries, StringComparison.Ordinal) ||
            !string.Equals(state.TargetPath, paths, StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(state.BaselineArray),
                state.BaselineFingerprint,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService.CreateShapeFingerprint(state.BaselineArray),
                state.ElementShapeFingerprint,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(expected),
                state.ExpectedCurrentFingerprint,
                StringComparison.Ordinal) ||
            !JToken.DeepEquals(current, expected))
            throw new InvalidOperationException(
                "The saved gameplay settings no longer match the loaded project.");
    }

    internal static IReadOnlyList<ResolvedGameplayTarget> ResolveTargets(
        ProjectModel project,
        ProgressionType type) =>
        ResolveTargets(project, GameplayPresetCatalog.Get(type));

    internal static void ValidatePreset(
        GameplayPresetDefinition definition,
        GameplayPresetOption preset)
    {
        if (preset.Values.Count != definition.Targets.Count)
            throw new InvalidOperationException(
                "The selected preset does not contain every required value.");

        double? commonScale = null;
        GameplayPresetOption vanilla = definition.Presets.Single(x =>
            string.Equals(x.Key, "Vanilla", StringComparison.Ordinal));
        for (int index = 0; index < definition.Targets.Count; index++)
        {
            if (definition.Targets[index].ValueSemantics !=
                GameplayPresetValueSemantics.BaselineScaled)
                continue;

            double reference = ReadNumber(vanilla.Values[index]!);
            double selected = ReadNumber(preset.Values[index]!);
            Require(reference != 0 &&
                    double.IsFinite(reference) &&
                    double.IsFinite(selected),
                "The selected proportional preset is invalid.");
            double scale = selected / reference;
            if (commonScale.HasValue)
                Require(
                    Math.Abs(scale - commonScale.Value) < 0.000001,
                    "The selected proportional preset does not use one common multiplier.");
            else
                commonScale = scale;
        }

        ValidateValues(
            definition.OperationType,
            preset.Values.Select(ReadNumber).ToArray(),
            false);
    }

    private static void ValidateBaseline(
        GameplayPresetDefinition definition,
        JArray baseline)
    {
        if (baseline.Count != definition.Targets.Count)
            throw new InvalidOperationException(
                "The remembered gameplay baseline is incomplete.");

        double[] values = baseline
            .OfType<JObject>()
            .Select(record => ReadNumber(
                record["value"] ??
                throw new InvalidOperationException(
                    "A remembered gameplay value is missing.")))
            .ToArray();

        switch (definition.OperationType)
        {
            case ProgressionType.MiningWoodcuttingTiming:
                RequirePositive(values);
                Require(
                    values[0] < values[1],
                    "The captured Mining and Woodcutting timing range is not supported.");
                break;
            case ProgressionType.VendorRefresh:
                RequirePositive(values);
                Require(
                    values[0] < values[1] && values[1] < values[2],
                    "The captured merchant refill categories are not supported.");
                break;
            case ProgressionType.ResourceReplenishment:
                RequireFinitePositiveOrdered(
                    values,
                    "The captured resource replenishment categories are not supported.");
                break;
            case ProgressionType.LecternKnowledgeGain:
                RequireFinitePositive(
                    values,
                    "The captured Lectern Knowledge value is not supported.");
                break;
            case ProgressionType.PositiveRandomTraits:
                ValidateTraitProbabilities(
                    values,
                    "The captured random-trait probabilities are not supported.");
                break;
            case ProgressionType.BattleCameraZoom:
                RequirePositive(values);
                Require(
                    values[0] <= values[1],
                    "The captured battle-camera range is not supported.");
                break;
        }
    }

    private static void ValidateExpected(
        GameplayPresetDefinition definition,
        GameplayPresetOption preset,
        JArray expected)
    {
        if (expected.Count != definition.Targets.Count)
            throw new InvalidOperationException(
                "The selected preset did not resolve every required value.");

        if (string.Equals(preset.Key, "Vanilla", StringComparison.Ordinal))
            return;

        ValidateValues(
            definition.OperationType,
            expected
                .OfType<JObject>()
                .Select(record => ReadNumber(
                    record["value"] ??
                    throw new InvalidOperationException(
                        "A resolved gameplay value is missing.")))
                .ToArray(),
            true);
    }

    private static void ValidateValues(
        ProgressionType operationType,
        double[] values,
        bool resolvedValues)
    {
        switch (operationType)
        {
            case ProgressionType.DeliciousMealChance:
                RequireIntegers(values, 0, 100);
                Require(values[0] <= values[1], "Tier 2 chance cannot exceed Tier 3.");
                break;
            case ProgressionType.ForgingAssistance:
                RequirePositive(values);
                Require(values[0] <= values[1], "The minimum forge window cannot exceed the maximum.");
                break;
            case ProgressionType.MiningWoodcuttingTiming:
                RequirePositive(values);
                Require(values[0] < values[1], "The timing range is invalid.");
                break;
            case ProgressionType.FishingSpeed:
                RequirePositive(values);
                break;
            case ProgressionType.LockpickingTolerance:
                Require(values[0] > 0 && values[0] <= values[1] && values[1] < 1,
                    "The lockpicking tolerance range is invalid.");
                break;
            case ProgressionType.NinePuzzleAssistance:
                RequireIntegers(values, 0, 12);
                break;
            case ProgressionType.RunStaminaRecovery:
                RequirePositive(values);
                Require(values[0] <= values[1], "Normal recovery cannot be slower than exhausted recovery.");
                break;
            case ProgressionType.BattleCameraZoom:
                RequirePositive(values);
                Require(values[0] <= values[1], "The minimum camera distance cannot exceed the maximum.");
                break;
            case ProgressionType.CampfireExpansion:
                RequireIntegers(values, 1, 100);
                ValidateCampfire(values);
                break;
            case ProgressionType.CookingPotFoodReduction:
            case ProgressionType.WorkshopMaterials:
                RequireIntegers(values, 0, 100);
                Require(values[0] <= values[1] && values[1] <= values[2],
                    "The selected tier progression is invalid.");
                break;
            case ProgressionType.VendorRefresh:
                RequirePositive(values);
                Require(values[0] < values[1] && values[1] < values[2],
                    "Merchant refill categories must preserve their relative order.");
                if (!resolvedValues)
                    Require(Math.Abs(values[1] / values[0] - (10.0 / 3.0)) < 0.000001 &&
                            Math.Abs(values[2] / values[1] - 3.0) < 0.000001,
                        "Merchant refill category relationships were not preserved.");
                break;
            case ProgressionType.ResourceReplenishment:
                RequireFinitePositiveOrdered(
                    values,
                    "Resource replenishment categories must preserve their relative order.");
                break;
            case ProgressionType.LecternKnowledgeGain:
                RequireFinitePositive(
                    values,
                    "Lectern Knowledge gain must remain finite and positive.");
                break;
            case ProgressionType.PositiveRandomTraits:
                ValidateTraitProbabilities(
                    values,
                    "Random-trait probabilities must remain between 0 and 1 with a total no greater than 1.");
                break;
            case ProgressionType.RubySapphireValue:
                RequireIntegers(values, 0, 100000);
                Require(values[0] == values[1], "Ruby and Sapphire values must change together.");
                break;
            case ProgressionType.TimeBetweenRests:
                RequireIntegers(values, 1, 1000);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operationType));
        }
    }

    private static IReadOnlyList<ResolvedGameplayTarget> ResolveTargets(
        ProjectModel project,
        GameplayPresetDefinition definition)
    {
        List<ResolvedGameplayTarget> targets = new();
        foreach (GameplayTargetDefinition target in definition.Targets)
            targets.Add(ResolveTarget(project, target));
        return targets;
    }

    private static ResolvedGameplayTarget ResolveTarget(
        ProjectModel project,
        GameplayTargetDefinition target)
    {
        SheetModel sheet = project.Sheets.SingleOrDefault(x =>
            string.Equals(x.Name, target.Sheet, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"The required {target.Sheet} settings are not available in this project.");
        EntryModel entry = sheet.Entries.SingleOrDefault(x =>
            string.Equals(x.Id, target.Entry, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"A required setting for this gameplay tool is not available in this project.");
        PropertyModel property = entry.Properties.SingleOrDefault(x =>
            string.Equals(x.EffectivePropertyPath, target.Path, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"A required value for this gameplay tool is not available in this project.");

        if (target.Discriminator == null)
        {
            JProperty source = property.SourceProperty
                ?? throw new InvalidOperationException("A required source value is missing.");
            RequireNumeric(source.Value);
            return new ResolvedGameplayTarget(target, entry, property, source);
        }

        if (property.SourceProperty?.Value is not JArray array)
            throw new InvalidOperationException(
                "A required gameplay bonus list is not available in this project.");
        JObject[] matches = array.OfType<JObject>().Where(item =>
            string.Equals(
                item.Value<string>(target.Discriminator),
                target.Identity,
                StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1 || matches[0].Property("value") is not JProperty value)
            throw new InvalidOperationException(
                "A unique required gameplay bonus was not found.");
        RequireNumeric(value.Value);
        return new ResolvedGameplayTarget(target, entry, property, value);
    }

    private static JArray CaptureTargets(
        IReadOnlyList<ResolvedGameplayTarget> targets)
    {
        JArray records = new();
        foreach (ResolvedGameplayTarget target in targets)
        {
            JObject context = new();
            string targetPath = target.Definition.Path;
            if (target.Definition.Discriminator != null)
            {
                JArray array = (JArray)target.Property.SourceProperty!.Value;
                JObject owner = (JObject)target.ValueProperty.Parent!;
                int index = array.IndexOf(owner);
                targetPath = $"{target.Definition.Path}.{index}.value";
                JObject structure = (JObject)owner.DeepClone();
                structure.Remove("value");
                context = new JObject
                {
                    ["arrayPath"] = target.Definition.Path,
                    ["discriminator"] = target.Definition.Discriminator,
                    ["identity"] = target.Definition.Identity,
                    ["structure"] = structure
                };
            }

            records.Add(new JObject
            {
                ["sheet"] = target.Definition.Sheet,
                ["entry"] = target.Definition.Entry,
                ["targetPath"] = targetPath,
                ["path"] = target.Definition.Path,
                ["value"] = target.ValueProperty.Value.DeepClone(),
                ["context"] = context
            });
        }
        return records;
    }

    private static JArray BuildExpected(
        JArray baseline,
        GameplayPresetDefinition definition,
        GameplayPresetOption preset)
    {
        if (baseline.Count != definition.Targets.Count)
            throw new InvalidOperationException(
                "The remembered gameplay baseline is incomplete.");
        JArray expected = (JArray)baseline.DeepClone();
        GameplayPresetOption vanilla = definition.Presets.Single(x =>
            string.Equals(x.Key, "Vanilla", StringComparison.Ordinal));
        for (int index = 0; index < expected.Count; index++)
        {
            JToken source = baseline[index]!["value"]!;
            GameplayTargetDefinition target = definition.Targets[index];
            JToken resolvedValue;

            if (string.Equals(preset.Key, "Vanilla", StringComparison.Ordinal) ||
                target.ValueSemantics ==
                GameplayPresetValueSemantics.PreserveBaseline)
            {
                resolvedValue = source.DeepClone();
            }
            else if (target.ValueSemantics ==
                     GameplayPresetValueSemantics.BaselineScaled)
            {
                double reference = ReadNumber(vanilla.Values[index]!);
                if (reference == 0)
                    throw new InvalidOperationException(
                        "The reference gameplay baseline cannot be scaled.");
                double scale = ReadNumber(preset.Values[index]!) / reference;
                resolvedValue = CreateCompatibleNumber(
                    source,
                    new JValue(ReadNumber(source) * scale));
            }
            else
            {
                resolvedValue = CreateCompatibleNumber(
                    source,
                    preset.Values[index]!);
            }

            expected[index]!["value"] = resolvedValue;
        }
        return expected;
    }

    private void ApplyExpected(
        ProjectModel project,
        JArray expected,
        ProjectMutationResult result)
    {
        foreach (JObject record in expected.OfType<JObject>())
        {
            EntryModel entry = FindEntry(
                project,
                record.Value<string>("sheet")!,
                record.Value<string>("entry")!);
            JObject context = (JObject)record["context"]!;
            string? arrayPath = context.Value<string>("arrayPath");
            if (string.IsNullOrWhiteSpace(arrayPath))
            {
                result.Merge(mutationService.EnsurePropertyByPath(
                    entry,
                    record.Value<string>("path")!,
                    record["value"]!.DeepClone()));
                continue;
            }

            PropertyModel arrayProperty = entry.Properties.Single(x =>
                string.Equals(x.EffectivePropertyPath, arrayPath, StringComparison.Ordinal));
            JArray array = (JArray)arrayProperty.SourceProperty!.Value.DeepClone();
            string discriminator = context.Value<string>("discriminator")!;
            string identity = context.Value<string>("identity")!;
            JObject match = array.OfType<JObject>().Single(x =>
                string.Equals(x.Value<string>(discriminator), identity, StringComparison.Ordinal));
            match["value"] = record["value"]!.DeepClone();
            result.Merge(mutationService.EnsurePropertyByPath(entry, arrayPath, array));
        }
    }

    private static GameplayOperationStateModel CreateState(
        GameplayPresetDefinition definition,
        JArray baseline,
        JArray expected,
        GameplayPresetOption preset) => new()
    {
        OperationType = definition.OperationType,
        TargetSheet = Join(baseline, "sheet", ","),
        TargetEntry = Join(baseline, "entry", ","),
        TargetPath = Join(baseline, "targetPath", "|"),
        BaselineArray = (JArray)baseline.DeepClone(),
        GameplaySettings = new JObject
        {
            ["preset"] = preset.Key
        },
        BaselineFingerprint = GameplayOperationFingerprintService
            .CreateContentFingerprint(baseline),
        ExpectedCurrentFingerprint = GameplayOperationFingerprintService
            .CreateContentFingerprint(expected),
        ElementCount = baseline.Count,
        ElementShapeFingerprint = GameplayOperationFingerprintService
            .CreateShapeFingerprint(baseline),
        IsCompatible = true
    };

    private static string Join(JArray records, string property, string separator) =>
        string.Join(separator, records.OfType<JObject>().Select(x => x.Value<string>(property)));

    private static GameplayPresetOption GetPreset(
        GameplayPresetDefinition definition,
        string key) =>
        definition.Presets.SingleOrDefault(x =>
            string.Equals(x.Key, key, StringComparison.Ordinal))
        ?? throw new InvalidOperationException("Select one of the supported presets.");

    private static EntryModel FindEntry(
        ProjectModel project,
        string sheetName,
        string entryId) =>
        project.Sheets.Single(x => x.Name == sheetName).Entries.Single(x => x.Id == entryId);

    private static JValue CreateCompatibleNumber(JToken source, JToken value)
    {
        RequireNumeric(value);
        double requestedValue = Convert.ToDouble(value);
        bool isWholeNumber =
            Math.Abs(requestedValue - Math.Round(requestedValue)) < 0.0000001;
        return source.Type == JTokenType.Integer && isWholeNumber
            ? new JValue(Convert.ToInt64(Math.Round(requestedValue)))
            : new JValue(requestedValue);
    }

    private static double ReadNumber(JToken token)
    {
        RequireNumeric(token);
        return token.Value<double>();
    }

    private static void RequireNumeric(JToken token)
    {
        if (token.Type is not (JTokenType.Integer or JTokenType.Float))
            throw new InvalidOperationException("A required gameplay value is not numeric.");
    }

    private static void RequirePositive(IEnumerable<double> values) =>
        Require(values.All(x => x > 0), "Every selected value must be positive.");

    private static void RequireFinitePositive(
        IEnumerable<double> values,
        string message) =>
        Require(values.All(x => double.IsFinite(x) && x > 0), message);

    private static void ValidateTraitProbabilities(
        IReadOnlyList<double> values,
        string message) =>
        Require(
            values.Count == 3 &&
            values.All(x => double.IsFinite(x) && x >= 0 && x <= 1) &&
            values.Sum() <= 1.0000001,
            message);

    private static void RequireFinitePositiveOrdered(
        IReadOnlyList<double> values,
        string message) =>
        Require(
            values.Count == 3 &&
            values.All(double.IsFinite) &&
            values[0] > 0 &&
            values[0] < values[1] &&
            values[1] < values[2],
            message);

    private static void RequireIntegers(
        IEnumerable<double> values,
        int minimum,
        int maximum) =>
        Require(values.All(x =>
                Math.Abs(x - Math.Round(x)) < 0.000001 &&
                x >= minimum &&
                x <= maximum),
            $"Every selected value must be a whole number from {minimum} to {maximum}.");

    private static void ValidateCampfire(IReadOnlyList<double> values)
    {
        for (int tier = 0; tier < 3; tier++)
        {
            int offset = tier * 6;
            Require(values[offset] == values[offset + 1] &&
                    values[offset + 2] == values[offset + 3] &&
                    values[offset] == values[offset + 2],
                "Campfire dimensions must remain synchronized.");
            Require(values[offset + 4] == values[offset + 5],
                "Campfire capacity values must remain synchronized.");
        }
        Require(values[4] <= values[10] && values[10] <= values[16],
            "Campfire capacity must not decrease at higher tiers.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

internal sealed record ResolvedGameplayTarget(
    GameplayTargetDefinition Definition,
    EntryModel Entry,
    PropertyModel Property,
    JProperty ValueProperty);
