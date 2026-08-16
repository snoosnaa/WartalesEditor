using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class PartyEconomyService
{
    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;

    public PartyEconomyService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService)
    {
        this.mutationService = mutationService ?? throw new ArgumentNullException(nameof(mutationService));
        this.stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
    }

    public PartyEconomySettings GetSettings(ProjectModel project, ProgressionType type)
    {
        GameplayOperationStateModel? state = stateService.FindState(project, type);
        if (state != null)
        {
            stateService.ValidateState(project, state);
            if (!state.IsCompatible) throw new InvalidOperationException(state.CompatibilityMessage);
            if (IsLegacyExpandedState(state))
                return ReadLegacySettings(
                    state.GameplaySettings,
                    type,
                    CaptureTargets(project, type));
            return ReadSettings(state.GameplaySettings, type);
        }

        JArray current = CaptureTargets(project, type);
        return SettingsFromTargets(current, type);
    }

    public PartyEconomySettings GetBaselineSettings(ProjectModel project, ProgressionType type)
    {
        GameplayOperationStateModel? state = stateService.FindState(project, type);
        if (state == null)
            return SettingsFromTargets(CaptureTargets(project, type), type);
        JArray baseline = ExpandLegacyBaseline(
            project,
            type,
            state.BaselineArray);
        return SettingsFromTargets(baseline, type);
    }

    public ProjectMutationResult Apply(
        ProjectModel project,
        ProgressionType type,
        PartyEconomySettings settings)
    {
        settings.Validate(type);
        GameplayOperationStateModel? existing = stateService.FindState(project, type);
        JArray baseline;
        GameplayOperationStateModel? previousState = existing?.DeepClone();
        if (existing == null)
        {
            baseline = CaptureTargets(project, type);
        }
        else
        {
            stateService.ValidateState(project, existing);
            if (!existing.IsCompatible) throw new InvalidOperationException(existing.CompatibilityMessage);
            baseline = ExpandLegacyBaseline(
                project,
                type,
                existing.BaselineArray);
        }

        JArray expected = BuildExpected(baseline, settings, type);
        JArray current = CaptureTargets(project, type);
        JObject selectedSettings = WriteSettings(settings, type);
        if (existing != null &&
            JToken.DeepEquals(current, expected) &&
            JToken.DeepEquals(existing.GameplaySettings, selectedSettings))
            return new ProjectMutationResult();

        ProjectMutationResult result = new();
        if (!JToken.DeepEquals(current, expected))
            ApplyExpected(project, expected, result);

        GameplayOperationStateModel replacement = CreateState(type, baseline, expected, settings);
        bool previousModified = project.IsGameplayOperationStateModified;
        stateService.ReplaceState(project, replacement);
        result.AddGameplayOperationState(project, previousState, replacement, previousModified);
        return result;
    }

    internal static void ValidateState(ProjectModel project, GameplayOperationStateModel state)
    {
        JArray current = CaptureTargets(project, state.OperationType);
        if (IsLegacyExpandedState(state))
        {
            ValidateLegacyState(state, current);
            return;
        }
        PartyEconomySettings settings = ReadSettings(state.GameplaySettings, state.OperationType);
        settings.Validate(state.OperationType);
        JArray expected = BuildExpected(state.BaselineArray, settings, state.OperationType);
        string targetSheets = string.Join(",", current.OfType<JObject>().Select(x => x.Value<string>("sheet")));
        string targetEntries = string.Join(",", current.OfType<JObject>().Select(x => x.Value<string>("entry")));
        string targetPaths = string.Join("|", current.OfType<JObject>().Select(x => x.Value<string>("path")));
        if (state.ElementCount != state.BaselineArray.Count ||
            !string.Equals(state.TargetSheet, targetSheets, StringComparison.Ordinal) ||
            !string.Equals(state.TargetEntry, targetEntries, StringComparison.Ordinal) ||
            !string.Equals(state.TargetPath, targetPaths, StringComparison.Ordinal) ||
            !string.Equals(GameplayOperationFingerprintService.CreateContentFingerprint(state.BaselineArray), state.BaselineFingerprint, StringComparison.Ordinal) ||
            !string.Equals(GameplayOperationFingerprintService.CreateShapeFingerprint(state.BaselineArray), state.ElementShapeFingerprint, StringComparison.Ordinal) ||
            !string.Equals(GameplayOperationFingerprintService.CreateContentFingerprint(expected), state.ExpectedCurrentFingerprint, StringComparison.Ordinal) ||
            !JToken.DeepEquals(current, expected))
            throw new InvalidOperationException("The remembered targets no longer match the loaded project.");
    }

    private static void ValidateLegacyState(
        GameplayOperationStateModel state,
        JArray current)
    {
        JArray currentLegacy = new(current.Take(2).Select(x => x!.DeepClone()));
        JArray expectedLegacy = (JArray)state.BaselineArray.DeepClone();
        int[] values = state.OperationType == ProgressionType.ValourPoints
            ? new[]
            {
                RequiredInt(state.GameplaySettings, "maximumValour"),
                RequiredInt(state.GameplaySettings, "restoredValour")
            }
            : new[]
            {
                RequiredInt(state.GameplaySettings, "saddlebagCapacity"),
                RequiredInt(state.GameplaySettings, "ponyStartingCapacity")
            };
        for (int index = 0; index < 2; index++)
            expectedLegacy[index]!["value"] = values[index];

        string targetSheets = string.Join(",", currentLegacy.OfType<JObject>().Select(x => x.Value<string>("sheet")));
        string targetEntries = string.Join(",", currentLegacy.OfType<JObject>().Select(x => x.Value<string>("entry")));
        string targetPaths = string.Join("|", currentLegacy.OfType<JObject>().Select(x => x.Value<string>("path")));
        if (state.ElementCount != 2 ||
            !string.Equals(state.TargetSheet, targetSheets, StringComparison.Ordinal) ||
            !string.Equals(state.TargetEntry, targetEntries, StringComparison.Ordinal) ||
            !string.Equals(state.TargetPath, targetPaths, StringComparison.Ordinal) ||
            !string.Equals(GameplayOperationFingerprintService.CreateContentFingerprint(state.BaselineArray), state.BaselineFingerprint, StringComparison.Ordinal) ||
            !string.Equals(GameplayOperationFingerprintService.CreateShapeFingerprint(state.BaselineArray), state.ElementShapeFingerprint, StringComparison.Ordinal) ||
            !string.Equals(GameplayOperationFingerprintService.CreateContentFingerprint(expectedLegacy), state.ExpectedCurrentFingerprint, StringComparison.Ordinal) ||
            !JToken.DeepEquals(currentLegacy, expectedLegacy))
            throw new InvalidOperationException(
                "The saved legacy Party Economy settings no longer match the loaded project.");
    }

    internal static JArray CaptureTargets(ProjectModel project, ProgressionType type)
    {
        JArray result = new();
        foreach (Target target in ResolveTargets(project, type))
            result.Add(new JObject
            {
                ["sheet"] = target.Sheet,
                ["entry"] = target.Entry.Id,
                ["path"] = target.Path,
                ["value"] = target.ValueProperty.Value.DeepClone(),
                ["context"] = target.Context.DeepClone()
            });
        return result;
    }

    internal static IReadOnlyList<Target> ResolveTargets(ProjectModel project, ProgressionType type)
    {
        return type switch
        {
            ProgressionType.VolunteerWages => new[]
            {
                Resolve(project, "trait", "Volunteer", "props.value",
                    new JObject { ["description"] = FindEntry(project, "trait", "Volunteer").SourceEntry?["desc"]?.DeepClone() })
            },
            ProgressionType.ValourPoints => new[]
            {
                Resolve(project, "constant", "ActionPointBaseMax", "value", new JObject()),
                Resolve(project, "constant", "ActionPointGainPerSleep", "value", new JObject()),
                ResolveArrayValue(project, "item", "Tent", "props.bonuses", "bonus", "ActionPoint"),
                ResolveArrayValue(project, "item", "TentT2", "props.bonuses", "bonus", "ActionPoint"),
                ResolveArrayValue(project, "item", "TentT3", "props.bonuses", "bonus", "ActionPoint")
            },
            ProgressionType.CarryingCapacity => new[]
            {
                ResolveArrayValue(project, "item", "AnimAccCarriage", "baseBonus", "attribute", "Transport"),
                ResolveArrayValue(project, "unitClass", "Pony", "stats", "attribute", "Transport"),
                ResolveArrayValue(project, "item", "PonyAuge", "tool.personalBonuses", "bonus", "PonyAugeTransport"),
                ResolveArrayValue(project, "item", "PonyAugeT2", "tool.personalBonuses", "bonus", "PonyAugeTransport"),
                ResolveArrayValue(project, "item", "PonyAugeT3", "tool.personalBonuses", "bonus", "PonyAugeTransport"),
                ResolveArrayValue(project, "item", "PonyAugeT2", "tool.personalBonuses", "bonus", "PonyAugeTransportTrait"),
                ResolveArrayValue(project, "item", "PonyAugeT3", "tool.personalBonuses", "bonus", "PonyAugeTransportTrait")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    internal static JArray BuildExpected(JArray baseline, PartyEconomySettings settings, ProgressionType type)
    {
        settings.Validate(type);
        JArray expected = (JArray)baseline.DeepClone();
        int[] values = type switch
        {
            ProgressionType.VolunteerWages => new[] { settings.VolunteerPercentage },
            ProgressionType.ValourPoints => new[]
            {
                settings.MaximumValour, settings.RestoredValour,
                settings.TentTier1Valour, settings.TentTier2Valour,
                settings.TentTier3Valour
            },
            ProgressionType.CarryingCapacity => new[]
            {
                settings.SaddlebagCapacity, settings.PonyStartingCapacity,
                settings.HitchingPostTier1Base, settings.HitchingPostTier2Base,
                settings.HitchingPostTier3Base, settings.HitchingPostTier2Trait,
                settings.HitchingPostTier3Trait
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        for (int index = 0; index < expected.Count; index++)
            expected[index]!["value"] = values[index];
        return expected;
    }

    private void ApplyExpected(ProjectModel project, JArray expected, ProjectMutationResult result)
    {
        foreach (JObject record in expected.OfType<JObject>())
        {
            EntryModel entry = FindEntry(project, record.Value<string>("sheet")!, record.Value<string>("entry")!);
            JObject context = (JObject)record["context"]!;
            string? arrayPath = context.Value<string>("arrayPath");
            if (string.IsNullOrWhiteSpace(arrayPath))
            {
                result.Merge(mutationService.EnsurePropertyByPath(
                    entry, record.Value<string>("path")!, record["value"]!.DeepClone()));
                continue;
            }

            PropertyModel arrayProperty = entry.Properties.Single(p => p.EffectivePropertyPath == arrayPath);
            JArray array = (JArray)arrayProperty.SourceProperty!.Value.DeepClone();
            string discriminator = context.Value<string>("discriminator")!;
            string identity = context.Value<string>("identity")!;
            JObject match = array.OfType<JObject>().Single(x => x.Value<string>(discriminator) == identity);
            match["value"] = record["value"]!.DeepClone();
            result.Merge(mutationService.EnsurePropertyByPath(entry, arrayPath, array));
        }
    }

    private static GameplayOperationStateModel CreateState(
        ProgressionType type, JArray baseline, JArray expected, PartyEconomySettings settings) => new()
    {
        OperationType = type,
        TargetSheet = string.Join(",", baseline.OfType<JObject>().Select(x => x.Value<string>("sheet"))),
        TargetEntry = string.Join(",", baseline.OfType<JObject>().Select(x => x.Value<string>("entry"))),
        TargetPath = string.Join("|", baseline.OfType<JObject>().Select(x => x.Value<string>("path"))),
        BaselineArray = (JArray)baseline.DeepClone(),
        GameplaySettings = WriteSettings(settings, type),
        BaselineFingerprint = GameplayOperationFingerprintService.CreateContentFingerprint(baseline),
        ExpectedCurrentFingerprint = GameplayOperationFingerprintService.CreateContentFingerprint(expected),
        ElementCount = baseline.Count,
        ElementShapeFingerprint = GameplayOperationFingerprintService.CreateShapeFingerprint(baseline),
        IsCompatible = true
    };

    private static PartyEconomySettings SettingsFromTargets(JArray targets, ProgressionType type) =>
        type switch
        {
            ProgressionType.VolunteerWages => new PartyEconomySettings { VolunteerPercentage = targets[0]!["value"]!.Value<int>() },
            ProgressionType.ValourPoints => new PartyEconomySettings
            {
                MaximumValour = targets[0]!["value"]!.Value<int>(),
                RestoredValour = targets[1]!["value"]!.Value<int>(),
                TentTier1Valour = targets[2]!["value"]!.Value<int>(),
                TentTier2Valour = targets[3]!["value"]!.Value<int>(),
                TentTier3Valour = targets[4]!["value"]!.Value<int>()
            },
            ProgressionType.CarryingCapacity => new PartyEconomySettings
            {
                SaddlebagCapacity = targets[0]!["value"]!.Value<int>(),
                PonyStartingCapacity = targets[1]!["value"]!.Value<int>(),
                HitchingPostTier1Base = targets[2]!["value"]!.Value<int>(),
                HitchingPostTier2Base = targets[3]!["value"]!.Value<int>(),
                HitchingPostTier3Base = targets[4]!["value"]!.Value<int>(),
                HitchingPostTier2Trait = targets[5]!["value"]!.Value<int>(),
                HitchingPostTier3Trait = targets[6]!["value"]!.Value<int>()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private static JObject WriteSettings(PartyEconomySettings settings, ProgressionType type) =>
        type switch
        {
            ProgressionType.VolunteerWages => new JObject { ["volunteerPercentage"] = settings.VolunteerPercentage },
            ProgressionType.ValourPoints => new JObject
            {
                ["maximumValour"] = settings.MaximumValour,
                ["restoredValour"] = settings.RestoredValour,
                ["tentTier1Valour"] = settings.TentTier1Valour,
                ["tentTier2Valour"] = settings.TentTier2Valour,
                ["tentTier3Valour"] = settings.TentTier3Valour
            },
            ProgressionType.CarryingCapacity => new JObject
            {
                ["saddlebagCapacity"] = settings.SaddlebagCapacity,
                ["ponyStartingCapacity"] = settings.PonyStartingCapacity,
                ["hitchingPostTier1Base"] = settings.HitchingPostTier1Base,
                ["hitchingPostTier2Base"] = settings.HitchingPostTier2Base,
                ["hitchingPostTier3Base"] = settings.HitchingPostTier3Base,
                ["hitchingPostTier1Trait"] = settings.HitchingPostTier1Trait,
                ["hitchingPostTier2Trait"] = settings.HitchingPostTier2Trait,
                ["hitchingPostTier3Trait"] = settings.HitchingPostTier3Trait
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private static PartyEconomySettings ReadSettings(JObject? value, ProgressionType type)
    {
        if (value == null) throw new InvalidOperationException("The saved gameplay settings are missing.");
        return type switch
        {
            ProgressionType.VolunteerWages => new PartyEconomySettings { VolunteerPercentage = RequiredInt(value, "volunteerPercentage") },
            ProgressionType.ValourPoints => new PartyEconomySettings
            {
                MaximumValour = RequiredInt(value, "maximumValour"),
                RestoredValour = RequiredInt(value, "restoredValour"),
                TentTier1Valour = RequiredInt(value, "tentTier1Valour"),
                TentTier2Valour = RequiredInt(value, "tentTier2Valour"),
                TentTier3Valour = RequiredInt(value, "tentTier3Valour")
            },
            ProgressionType.CarryingCapacity => new PartyEconomySettings
            {
                SaddlebagCapacity = RequiredInt(value, "saddlebagCapacity"),
                PonyStartingCapacity = RequiredInt(value, "ponyStartingCapacity"),
                HitchingPostTier1Base = RequiredInt(value, "hitchingPostTier1Base"),
                HitchingPostTier2Base = RequiredInt(value, "hitchingPostTier2Base"),
                HitchingPostTier3Base = RequiredInt(value, "hitchingPostTier3Base"),
                HitchingPostTier1Trait = RequiredInt(value, "hitchingPostTier1Trait"),
                HitchingPostTier2Trait = RequiredInt(value, "hitchingPostTier2Trait"),
                HitchingPostTier3Trait = RequiredInt(value, "hitchingPostTier3Trait")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static int RequiredInt(JObject? value, string name) =>
        value?[name]?.Type == JTokenType.Integer ? value!.Value<int>(name) :
        throw new InvalidOperationException($"Saved setting '{name}' is missing or invalid.");

    private static PartyEconomySettings ReadLegacySettings(
        JObject? value,
        ProgressionType type,
        JArray current)
    {
        if (current.Count != (type == ProgressionType.ValourPoints ? 5 : 7))
            throw new InvalidOperationException(
                "The current Party Economy targets are incomplete.");

        return type switch
        {
            ProgressionType.ValourPoints => new PartyEconomySettings
            {
                MaximumValour = RequiredInt(value, "maximumValour"),
                RestoredValour = RequiredInt(value, "restoredValour"),
                TentTier1Valour = current[2]!["value"]!.Value<int>(),
                TentTier2Valour = current[3]!["value"]!.Value<int>(),
                TentTier3Valour = current[4]!["value"]!.Value<int>()
            },
            ProgressionType.CarryingCapacity => new PartyEconomySettings
            {
                SaddlebagCapacity = RequiredInt(value, "saddlebagCapacity"),
                PonyStartingCapacity = RequiredInt(value, "ponyStartingCapacity"),
                HitchingPostTier1Base = current[2]!["value"]!.Value<int>(),
                HitchingPostTier2Base = current[3]!["value"]!.Value<int>(),
                HitchingPostTier3Base = current[4]!["value"]!.Value<int>(),
                HitchingPostTier1Trait = 0,
                HitchingPostTier2Trait = current[5]!["value"]!.Value<int>(),
                HitchingPostTier3Trait = current[6]!["value"]!.Value<int>()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static bool IsLegacyExpandedState(
        GameplayOperationStateModel state) =>
        state.BaselineArray.Count == 2 &&
        state.OperationType is
            (ProgressionType.ValourPoints or ProgressionType.CarryingCapacity);

    private static JArray ExpandLegacyBaseline(
        ProjectModel project,
        ProgressionType type,
        JArray baseline)
    {
        int expectedCount = type switch
        {
            ProgressionType.ValourPoints => 5,
            ProgressionType.CarryingCapacity => 7,
            _ => baseline.Count
        };
        if (baseline.Count == expectedCount)
            return (JArray)baseline.DeepClone();
        if (baseline.Count != 2 ||
            type is not (ProgressionType.ValourPoints or ProgressionType.CarryingCapacity))
            throw new InvalidOperationException(
                "The remembered Party Economy baseline is incomplete.");

        JArray expanded = (JArray)baseline.DeepClone();
        JArray current = CaptureTargets(project, type);
        for (int index = 2; index < current.Count; index++)
            expanded.Add(current[index]!.DeepClone());
        return expanded;
    }

    private static Target Resolve(ProjectModel project, string sheet, string entryId, string path, JObject context)
    {
        EntryModel entry = FindEntry(project, sheet, entryId);
        PropertyModel property = entry.Properties.SingleOrDefault(p => p.EffectivePropertyPath == path)
            ?? throw new InvalidOperationException($"Required property '{path}' was not found on '{entryId}'.");
        if (property.SourceProperty?.Value.Type != JTokenType.Integer)
            throw new InvalidOperationException($"Required property '{entryId}/{path}' must be an integer.");
        return new Target(
            sheet,
            entry,
            property,
            property.SourceProperty!,
            path,
            context);
    }

    private static Target ResolveArrayValue(ProjectModel project, string sheet, string entryId, string arrayPath, string discriminator, string identity)
    {
        EntryModel entry = FindEntry(project, sheet, entryId);
        PropertyModel arrayProperty = entry.Properties.SingleOrDefault(p => p.EffectivePropertyPath == arrayPath)
            ?? throw new InvalidOperationException($"Required array '{arrayPath}' was not found on '{entryId}'.");
        if (arrayProperty.SourceProperty?.Value is not JArray array)
            throw new InvalidOperationException($"Required array '{entryId}/{arrayPath}' is invalid.");
        JObject[] matches = array.OfType<JObject>().Where(x => x.Value<string>(discriminator) == identity).ToArray();
        if (matches.Length != 1 || matches[0]["value"]?.Type != JTokenType.Integer)
            throw new InvalidOperationException($"A unique integer '{identity}' target was not found on '{entryId}'.");
        int index = array.IndexOf(matches[0]);
        string path = $"{arrayPath}.{index}.value";
        JObject structure = (JObject)matches[0].DeepClone();
        structure.Remove("value");
        return new Target(sheet, entry, arrayProperty, matches[0].Property("value")!,
            path, new JObject
            {
                ["arrayPath"] = arrayPath,
                ["discriminator"] = discriminator,
                ["identity"] = identity,
                ["structure"] = structure
            });
    }

    private static EntryModel FindEntry(ProjectModel project, string sheetName, string entryId)
    {
        SheetModel sheet = project.Sheets.SingleOrDefault(x => x.Name == sheetName)
            ?? throw new InvalidOperationException($"Required sheet '{sheetName}' was not found.");
        return sheet.Entries.SingleOrDefault(x => x.Id == entryId)
            ?? throw new InvalidOperationException($"Required entry '{entryId}' was not found in '{sheetName}'.");
    }
}

internal sealed record Target(
    string Sheet,
    EntryModel Entry,
    PropertyModel Property,
    JProperty ValueProperty,
    string Path,
    JObject Context);
