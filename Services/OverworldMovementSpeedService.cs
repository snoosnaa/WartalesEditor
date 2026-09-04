using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class OverworldMovementSpeedService
{
    private const string PreviousValuesSetting = "PreviousValues";
    public const string WalkEntryId = "PlayerBaseSpeed";
    public const string RunEntryId = "PlayerRunSpeed";

    public static IReadOnlyList<OverworldMovementPresetOption> Presets { get; } =
        new[]
        {
            new OverworldMovementPresetOption(OverworldMovementPreset.Vanilla, "Vanilla", 6, 11),
            new OverworldMovementPresetOption(OverworldMovementPreset.Faster, "Fast", 8, 14),
            new OverworldMovementPresetOption(OverworldMovementPreset.Fast, "Faster", 9, 17),
            new OverworldMovementPresetOption(OverworldMovementPreset.VeryFast, "Very Fast", 12, 22)
        };

    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;

    public OverworldMovementSpeedService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService)
    {
        this.mutationService = mutationService ?? throw new ArgumentNullException(nameof(mutationService));
        this.stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
    }

    public OverworldMovementPreset DetectPreset(ProjectModel project)
    {
        try
        {
            (MovementTarget walk, MovementTarget run) = ResolveTargets(project);
            double walkValue = walk.Property.SourceProperty!.Value.Value<double>();
            double runValue = run.Property.SourceProperty!.Value.Value<double>();
            return Presets.FirstOrDefault(x =>
                       x.WalkSpeed == walkValue && x.RunSpeed == runValue)?.Preset
                   ?? OverworldMovementPreset.Custom;
        }
        catch
        {
            return OverworldMovementPreset.Unavailable;
        }
    }

    public bool CanRestorePreviousValues(ProjectModel project) =>
        stateService.CanRestorePreviousValues(
            project,
            ProgressionType.OverworldMovementSpeed);

    public ProjectMutationResult RestorePreviousValues(ProjectModel project)
    {
        return RestorePreviousValuesCore(project, new ProjectMutationResult());
    }

    internal ProjectMutationResult RestorePreviousValues(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RestorePreviousValuesCore(project, context.MutationResult);
    }

    private ProjectMutationResult RestorePreviousValuesCore(
        ProjectModel project,
        ProjectMutationResult result)
    {
        GameplayOperationStateModel existing =
            stateService.GetRequiredPreviousValuesState(
                project,
                ProgressionType.OverworldMovementSpeed);
        (MovementTarget walk, MovementTarget run) = ResolveTargets(project);
        JArray baseline = (JArray)existing.BaselineArray.DeepClone();
        JArray current = CaptureTargets(walk, run);

        if (JToken.DeepEquals(current, baseline) &&
            string.Equals(
                existing.GameplaySettings?.Value<string>("preset"),
                PreviousValuesSetting,
                StringComparison.Ordinal))
        {
            return result;
        }

        if (!JToken.DeepEquals(current, baseline))
        {
            result.Merge(mutationService.EnsurePropertyByPath(
                walk.Entry,
                "value",
                baseline[0]!["value"]!));
            result.Merge(mutationService.EnsurePropertyByPath(
                run.Entry,
                "value",
                baseline[1]!["value"]!));
        }

        GameplayOperationStateModel replacement =
            CreateState(baseline, baseline, PreviousValuesSetting, null);
        GameplayOperationStateModel previous = existing.DeepClone();
        bool previousModified = project.IsGameplayOperationStateModified;
        result.AddGameplayOperationState(
            project, previous, replacement, previousModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    public ProjectMutationResult Apply(
        ProjectModel project,
        OverworldMovementPreset preset)
    {
        return ApplyCore(project, preset, new ProjectMutationResult());
    }

    internal ProjectMutationResult Apply(
        ProjectModel project,
        OverworldMovementPreset preset,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ApplyCore(project, preset, context.MutationResult);
    }

    private ProjectMutationResult ApplyCore(
        ProjectModel project,
        OverworldMovementPreset preset,
        ProjectMutationResult result)
    {
        OverworldMovementPresetOption selection = GetRequiredPreset(preset);
        ValidatePair(selection.WalkSpeed, selection.RunSpeed);
        (MovementTarget walk, MovementTarget run) = ResolveTargets(project);
        GameplayOperationStateModel? existing =
            stateService.FindState(project, ProgressionType.OverworldMovementSpeed);

        if (existing != null)
        {
            stateService.ValidateState(project, existing);
        }

        JArray baseline = existing == null || !existing.IsCompatible
            ? CaptureTargets(walk, run)
            : (JArray)existing.BaselineArray.DeepClone();
        JArray expected = BuildExpected(baseline, selection);
        JArray current = CaptureTargets(walk, run);

        if (existing != null &&
            JToken.DeepEquals(current, expected) &&
            string.Equals(
                existing.GameplaySettings?.Value<string>("preset"),
                preset.ToString(),
                StringComparison.Ordinal))
            return result;

        if (!JToken.DeepEquals(current, expected))
        {
            result.Merge(mutationService.EnsurePropertyByPath(
                walk.Entry,
                "value",
                CreateCompatibleNumber(
                    walk.Property.SourceProperty!.Value,
                    selection.WalkSpeed)));
            result.Merge(mutationService.EnsurePropertyByPath(
                run.Entry,
                "value",
                CreateCompatibleNumber(
                    run.Property.SourceProperty!.Value,
                    selection.RunSpeed)));
        }

        GameplayOperationStateModel replacement =
            CreateState(
                baseline,
                expected,
                selection.Preset.ToString(),
                selection);
        GameplayOperationStateModel? previous = existing?.DeepClone();
        bool previousModified = project.IsGameplayOperationStateModified;
        result.AddGameplayOperationState(
            project, previous, replacement, previousModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    internal static void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        if (state.GameplaySettings == null)
            throw new InvalidOperationException("The selected movement preset is missing.");
        string presetName = state.GameplaySettings.Value<string>("preset") ?? string.Empty;
        (MovementTarget walk, MovementTarget run) = ResolveTargets(project);
        JArray current = CaptureTargets(walk, run);
        JArray expected;
        if (string.Equals(
                presetName,
                PreviousValuesSetting,
                StringComparison.Ordinal))
        {
            expected = (JArray)state.BaselineArray.DeepClone();
        }
        else
        {
            if (!Enum.TryParse(presetName, out OverworldMovementPreset preset))
                throw new InvalidOperationException("The selected movement preset is invalid.");
            OverworldMovementPresetOption selection = GetRequiredPreset(preset);
            expected = BuildExpected(state.BaselineArray, selection);
        }

        if (state.ElementCount != 2 ||
            state.BaselineArray.Count != 2 ||
            !string.Equals(state.TargetSheet, "constant,constant", StringComparison.Ordinal) ||
            !string.Equals(state.TargetEntry, $"{WalkEntryId},{RunEntryId}", StringComparison.Ordinal) ||
            !string.Equals(state.TargetPath, "value|value", StringComparison.Ordinal) ||
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
                "The saved movement settings no longer match the loaded project.");
    }

    internal static (MovementTarget Walk, MovementTarget Run)
        ResolveTargets(ProjectModel project)
    {
        SheetModel sheet = project.Sheets.SingleOrDefault(x =>
            string.Equals(x.Name, "constant", StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "The required movement settings are not available in this project.");
        return (
            ResolveTarget(sheet, WalkEntryId),
            ResolveTarget(sheet, RunEntryId));
    }

    private static MovementTarget ResolveTarget(SheetModel sheet, string id)
    {
        EntryModel entry = sheet.Entries.SingleOrDefault(x =>
            string.Equals(x.Id, id, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "A required movement setting is not available in this project.");
        PropertyModel property = entry.Properties.SingleOrDefault(x =>
            string.Equals(x.EffectivePropertyPath, "value", StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "A required movement value is not available in this project.");
        if (property.SourceProperty?.Value.Type is not
            (JTokenType.Integer or JTokenType.Float))
            throw new InvalidOperationException(
                "A required movement value is not a supported whole number.");
        return new MovementTarget(entry, property);
    }

    internal static JArray CaptureTargets(
        MovementTarget walk,
        MovementTarget run) => new()
    {
        CreateRecord(WalkEntryId, walk.Property.SourceProperty!.Value),
        CreateRecord(RunEntryId, run.Property.SourceProperty!.Value)
    };

    private static JObject CreateRecord(string entry, JToken value) => new()
    {
        ["sheet"] = "constant",
        ["entry"] = entry,
        ["path"] = "value",
        ["value"] = value.DeepClone()
    };

    private static JArray BuildExpected(
        JArray baseline,
        OverworldMovementPresetOption preset)
    {
        if (baseline.Count != 2)
            throw new InvalidOperationException(
                "The remembered movement baseline is incomplete.");
        JArray expected = (JArray)baseline.DeepClone();
        expected[0]!["value"] = CreateCompatibleNumber(
            baseline[0]!["value"]!,
            preset.WalkSpeed);
        expected[1]!["value"] = CreateCompatibleNumber(
            baseline[1]!["value"]!,
            preset.RunSpeed);
        return expected;
    }

    private static JValue CreateCompatibleNumber(
        JToken source,
        int value) =>
        source.Type == JTokenType.Float
            ? new JValue(Convert.ToDouble(value))
            : new JValue(value);

    private static GameplayOperationStateModel CreateState(
        JArray baseline,
        JArray expected,
        string setting,
        OverworldMovementPresetOption? selection)
    {
        JObject gameplaySettings = new()
        {
            ["preset"] = setting
        };
        if (selection != null)
        {
            gameplaySettings["walkSpeed"] = selection.WalkSpeed;
            gameplaySettings["runSpeed"] = selection.RunSpeed;
        }

        return new GameplayOperationStateModel
        {
            OperationType = ProgressionType.OverworldMovementSpeed,
            TargetSheet = "constant,constant",
            TargetEntry = $"{WalkEntryId},{RunEntryId}",
            TargetPath = "value|value",
            BaselineArray = (JArray)baseline.DeepClone(),
            GameplaySettings = gameplaySettings,
            BaselineFingerprint =
                GameplayOperationFingerprintService.CreateContentFingerprint(baseline),
            ExpectedCurrentFingerprint =
                GameplayOperationFingerprintService.CreateContentFingerprint(expected),
            ElementCount = 2,
            ElementShapeFingerprint =
                GameplayOperationFingerprintService.CreateShapeFingerprint(baseline),
            IsCompatible = true
        };
    }

    private static OverworldMovementPresetOption GetRequiredPreset(
        OverworldMovementPreset preset) =>
        Presets.SingleOrDefault(x => x.Preset == preset)
        ?? throw new InvalidOperationException(
            "Select one of the supported movement presets.");

    private static void ValidatePair(int walk, int run)
    {
        if (walk <= 0 || run <= 0 || walk >= run)
            throw new InvalidOperationException(
                "The selected movement preset is invalid.");
    }
}

internal sealed record MovementTarget(
    EntryModel Entry,
    PropertyModel Property);
