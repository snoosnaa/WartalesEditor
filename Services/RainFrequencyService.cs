using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class RainFrequencyService
{
    private const string PreviousValuesSetting = "PreviousValues";
    public const string PropertyPath =
        "props.meteo.rainDaysPerMonth";

    public static IReadOnlyList<RainFrequencyPresetOption>
        Presets { get; } =
        new[]
        {
            new RainFrequencyPresetOption(
                RainFrequencyPreset.Vanilla,
                "Vanilla",
                1m,
                "Restores each supported region's original rain frequency."),
            new RainFrequencyPresetOption(
                RainFrequencyPreset.LessRain,
                "Less Rain",
                0.5m,
                "Rain occurs about half as often while regional differences remain."),
            new RainFrequencyPresetOption(
                RainFrequencyPreset.RareRain,
                "Rare Rain",
                0.25m,
                "Rain becomes uncommon while regional differences remain."),
            new RainFrequencyPresetOption(
                RainFrequencyPreset.NoRain,
                "No Rain",
                0m,
                "Disables ordinary regional rain. Other weather systems remain unchanged.")
        };

    public static IReadOnlyList<RainRegionDefinition>
        Regions { get; } =
        new[]
        {
            new RainRegionDefinition("Alazar_Aneding", "Aneding", 4),
            new RainRegionDefinition("Edoran_1", "Edoran", 6),
            new RainRegionDefinition("Gosenberg_1", "Gosenberg", 6),
            new RainRegionDefinition("Harag_1", "Harag", 6),
            new RainRegionDefinition("InterRegion_1", "Border Region", 6),
            new RainRegionDefinition("Gosenberg_2", "Gosenberg", 6),
            new RainRegionDefinition("Alazar_1", "Alazar", 4),
            new RainRegionDefinition("Belerion_1", "Belerion", 4),
            new RainRegionDefinition("Edoran_2", "Edoran", 6),
            new RainRegionDefinition("InterRegion_2", "Border Region", 6),
            new RainRegionDefinition("Alazar_2", "Alazar", 4),
            new RainRegionDefinition("Edoran_3", "Edoran", 6)
        };

    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;

    public RainFrequencyService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService)
    {
        this.mutationService = mutationService
            ?? throw new ArgumentNullException(nameof(mutationService));
        this.stateService = stateService
            ?? throw new ArgumentNullException(nameof(stateService));
    }

    public RainFrequencyPreset DetectPreset(ProjectModel project)
    {
        try
        {
            IReadOnlyList<RainTarget> targets =
                ResolveTargets(project);
            foreach (RainFrequencyPresetOption preset in Presets)
            {
                if (Matches(targets, preset))
                    return preset.Preset;
            }
            return RainFrequencyPreset.Custom;
        }
        catch
        {
            return RainFrequencyPreset.Unavailable;
        }
    }

    public bool CanRestorePreviousValues(ProjectModel project) =>
        stateService.CanRestorePreviousValues(
            project,
            ProgressionType.RainFrequency);

    public ProjectMutationResult RestorePreviousValues(ProjectModel project)
    {
        GameplayOperationStateModel existing =
            stateService.GetRequiredPreviousValuesState(
                project,
                ProgressionType.RainFrequency);
        IReadOnlyList<RainTarget> targets = ResolveTargets(project);
        JArray baseline = (JArray)existing.BaselineArray.DeepClone();
        JArray current = Capture(targets);

        if (JToken.DeepEquals(current, baseline) &&
            string.Equals(
                existing.GameplaySettings?.Value<string>("preset"),
                PreviousValuesSetting,
                StringComparison.Ordinal))
        {
            return new ProjectMutationResult();
        }

        ProjectMutationResult result = new();
        for (int index = 0; index < targets.Count; index++)
        {
            result.Merge(
                mutationService.EnsurePropertyByPath(
                    targets[index].Entry,
                    PropertyPath,
                    baseline[index]!["value"]!));
        }

        GameplayOperationStateModel replacement =
            CreateState(baseline, baseline, PreviousValuesSetting);
        GameplayOperationStateModel previous = existing.DeepClone();
        bool previousModified = project.IsGameplayOperationStateModified;
        stateService.ReplaceState(project, replacement);
        result.AddGameplayOperationState(
            project,
            previous,
            replacement,
            previousModified);
        return result;
    }

    public ProjectMutationResult Apply(
        ProjectModel project,
        RainFrequencyPreset preset)
    {
        RainFrequencyPresetOption selection =
            GetRequiredPreset(preset);
        IReadOnlyList<RainTarget> targets =
            ResolveTargets(project);
        GameplayOperationStateModel? existing =
            stateService.FindState(
                project,
                ProgressionType.RainFrequency);

        if (existing != null)
            stateService.ValidateState(project, existing);

        JArray baseline = existing == null || !existing.IsCompatible
            ? Capture(targets)
            : (JArray)existing.BaselineArray.DeepClone();
        JArray expected = BuildExpected(CreateBaseline(), selection);

        ProjectMutationResult result = new();
        for (int index = 0; index < targets.Count; index++)
        {
            result.Merge(
                mutationService.EnsurePropertyByPath(
                    targets[index].Entry,
                    PropertyPath,
                    expected[index]!["value"]!));
        }

        if (!result.WasModified &&
            existing != null &&
            existing.IsCompatible &&
            string.Equals(
                existing.GameplaySettings?.Value<string>("preset"),
                preset.ToString(),
                StringComparison.Ordinal))
        {
            return result;
        }

        GameplayOperationStateModel replacement =
            CreateState(
                baseline,
                expected,
                selection.Preset.ToString());
        GameplayOperationStateModel? previous =
            existing?.DeepClone();
        bool previousModified =
            project.IsGameplayOperationStateModified;
        stateService.ReplaceState(project, replacement);
        result.AddGameplayOperationState(
            project,
            previous,
            replacement,
            previousModified);
        return result;
    }

    internal static IReadOnlyList<RainTarget>
        ResolveTargets(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        SheetModel sheet =
            project.Sheets.SingleOrDefault(candidate =>
                string.Equals(
                    candidate.Name,
                    "region",
                    StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "Regional rain settings are not available in this project.");

        List<RainTarget> targets = new();
        foreach (RainRegionDefinition definition in Regions)
        {
            EntryModel entry =
                sheet.Entries.SingleOrDefault(candidate =>
                    string.Equals(
                        candidate.Id,
                        definition.EntryId,
                        StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"{definition.DisplayName} rain settings are not available.");
            PropertyModel property =
                entry.Properties.SingleOrDefault(candidate =>
                    string.Equals(
                        candidate.EffectivePropertyPath,
                        PropertyPath,
                        StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"{definition.DisplayName} rain frequency is not available.");
            if (property.SourceProperty?.Value.Type is not
                (JTokenType.Integer or JTokenType.Float))
                throw new InvalidOperationException(
                    $"{definition.DisplayName} rain frequency is not numeric.");
            if (property.SourceProperty.Value.Value<decimal>() < 0)
                throw new InvalidOperationException(
                    $"{definition.DisplayName} rain frequency is invalid.");
            targets.Add(new RainTarget(definition, entry, property));
        }
        return targets;
    }

    internal static void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        if (state.GameplaySettings == null)
            throw new InvalidOperationException(
                "The saved Rain Frequency preset is invalid.");
        string setting =
            state.GameplaySettings.Value<string>("preset") ?? string.Empty;
        ValidateBaseline(state.BaselineArray);
        IReadOnlyList<RainTarget> targets =
            ResolveTargets(project);
        JArray expected;
        if (string.Equals(
                setting,
                PreviousValuesSetting,
                StringComparison.Ordinal))
        {
            expected = (JArray)state.BaselineArray.DeepClone();
        }
        else
        {
            if (!Enum.TryParse(setting, out RainFrequencyPreset preset))
                throw new InvalidOperationException(
                    "The saved Rain Frequency preset is invalid.");
            RainFrequencyPresetOption selection = GetRequiredPreset(preset);
            expected = BuildExpected(CreateBaseline(), selection);
        }
        JArray current = Capture(targets);

        if (state.ElementCount != Regions.Count ||
            state.BaselineArray.Count != Regions.Count ||
            !string.Equals(
                state.TargetSheet,
                "region",
                StringComparison.Ordinal) ||
            !string.Equals(
                state.TargetEntry,
                string.Join(",", Regions.Select(x => x.EntryId)),
                StringComparison.Ordinal) ||
            !string.Equals(
                state.TargetPath,
                PropertyPath,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService
                    .CreateContentFingerprint(state.BaselineArray),
                state.BaselineFingerprint,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService
                    .CreateShapeFingerprint(state.BaselineArray),
                state.ElementShapeFingerprint,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService
                    .CreateContentFingerprint(expected),
                state.ExpectedCurrentFingerprint,
                StringComparison.Ordinal) ||
            !JToken.DeepEquals(current, expected))
            throw new InvalidOperationException(
                "The saved Rain Frequency settings no longer match the loaded project.");
    }

    private static void ValidateBaseline(JArray baseline)
    {
        if (baseline.Count != Regions.Count)
            throw new InvalidOperationException(
                "The remembered regional rain baseline is incomplete.");

        for (int index = 0; index < Regions.Count; index++)
        {
            RainRegionDefinition region = Regions[index];
            if (baseline[index] is not JObject record ||
                !string.Equals(
                    record.Value<string>("sheet"),
                    "region",
                    StringComparison.Ordinal) ||
                !string.Equals(
                    record.Value<string>("entry"),
                    region.EntryId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    record.Value<string>("path"),
                    PropertyPath,
                    StringComparison.Ordinal) ||
                record["value"]?.Type is not
                    (JTokenType.Integer or JTokenType.Float) ||
                record["value"]!.Value<decimal>() < 0)
            {
                throw new InvalidOperationException(
                    "The remembered regional rain baseline is invalid.");
            }
        }
    }

    internal static JArray BuildExpected(
        JArray baseline,
        RainFrequencyPresetOption preset)
    {
        if (baseline.Count != Regions.Count)
            throw new InvalidOperationException(
                "The remembered regional rain baseline is incomplete.");

        JArray expected = (JArray)baseline.DeepClone();
        for (int index = 0; index < expected.Count; index++)
        {
            decimal baselineValue =
                expected[index]!["value"]!.Value<decimal>();
            decimal value =
                preset.Preset == RainFrequencyPreset.NoRain
                    ? 0m
                    : baselineValue * preset.Multiplier;
            if (value < 0)
                throw new InvalidOperationException(
                    "The selected Rain Frequency preset is invalid.");
            expected[index]!["value"] =
                decimal.Truncate(value) == value
                    ? new JValue(decimal.ToInt32(value))
                    : new JValue(value);
        }
        return expected;
    }

    internal static JArray CreateBaseline()
    {
        return new JArray(
            Regions.Select(region =>
                new JObject
                {
                    ["sheet"] = "region",
                    ["entry"] = region.EntryId,
                    ["path"] = PropertyPath,
                    ["value"] = region.VanillaValue
                }));
    }

    private static JArray Capture(
        IReadOnlyList<RainTarget> targets)
    {
        return new JArray(
            targets.Select(target =>
                new JObject
                {
                    ["sheet"] = "region",
                    ["entry"] = target.Definition.EntryId,
                    ["path"] = PropertyPath,
                    ["value"] =
                        target.Property.SourceProperty!.Value.DeepClone()
                }));
    }

    private static bool Matches(
        IReadOnlyList<RainTarget> targets,
        RainFrequencyPresetOption preset)
    {
        return JToken.DeepEquals(
            Capture(targets),
            BuildExpected(CreateBaseline(), preset));
    }

    private static GameplayOperationStateModel CreateState(
        JArray baseline,
        JArray expected,
        string setting)
    {
        return new GameplayOperationStateModel
        {
            OperationType = ProgressionType.RainFrequency,
            TargetSheet = "region",
            TargetEntry =
                string.Join(",", Regions.Select(x => x.EntryId)),
            TargetPath = PropertyPath,
            BaselineArray = (JArray)baseline.DeepClone(),
            GameplaySettings = new JObject
            {
                ["preset"] = setting
            },
            BaselineFingerprint =
                GameplayOperationFingerprintService
                    .CreateContentFingerprint(baseline),
            ExpectedCurrentFingerprint =
                GameplayOperationFingerprintService
                    .CreateContentFingerprint(expected),
            ElementCount = Regions.Count,
            ElementShapeFingerprint =
                GameplayOperationFingerprintService
                    .CreateShapeFingerprint(baseline),
            IsCompatible = true
        };
    }

    private static RainFrequencyPresetOption GetRequiredPreset(
        RainFrequencyPreset preset)
    {
        return Presets.SingleOrDefault(option =>
            option.Preset == preset)
            ?? throw new InvalidOperationException(
                "Select one of the supported Rain Frequency presets.");
    }
}

public sealed record RainRegionDefinition(
    string EntryId,
    string DisplayName,
    int VanillaValue);

internal sealed record RainTarget(
    RainRegionDefinition Definition,
    EntryModel Entry,
    PropertyModel Property);
