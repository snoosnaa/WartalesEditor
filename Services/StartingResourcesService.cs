using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class StartingResourcesService
{
    public const string GoldItemId = "Gold";
    public const string BreadItemId = "Bread";
    public const string AppleItemId = "Apple";
    public const string IronOreItemId = "IronOre";
    public const string WoodItemId = "Wood";
    public const string ClothItemId = "Cloth";

    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;

    public StartingResourcesService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService)
    {
        ArgumentNullException.ThrowIfNull(mutationService);
        ArgumentNullException.ThrowIfNull(stateService);
        this.mutationService = mutationService;
        this.stateService = stateService;
    }

    public GameplayOperationStateModel Initialize(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        JArray baseline = CaptureCurrentTargets(project);
        StartingResourcesSettings settings = new();
        JArray expected = BuildExpectedTargets(project, baseline, settings);
        GameplayOperationStateModel state = CreateState(
            baseline,
            settings,
            expected);
        stateService.ReplaceState(project, state);
        return state.DeepClone();
    }

    public ProjectMutationResult Apply(
        ProjectModel project,
        StartingResourcesSettings settings)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        GameplayOperationStateModel previousState =
            stateService.GetRequiredCompatibleState(
                project,
                ProgressionType.StartingResources)
                .DeepClone();

        bool previousStateWasModified =
            project.IsGameplayOperationStateModified;

        JArray expected = BuildExpectedTargets(
            project,
            previousState.BaselineArray,
            settings);

        if (JToken.DeepEquals(CaptureCurrentTargets(project), expected) &&
            SettingsEqual(previousState.StartingResources, settings))
        {
            return new ProjectMutationResult();
        }

        ProjectMutationResult result = new();
        ApplyExpectedTargets(project, expected, result);

        GameplayOperationStateModel replacement =
            CreateState(previousState.BaselineArray, settings, expected);

        stateService.ReplaceState(project, replacement);
        result.AddGameplayOperationState(
            project,
            previousState,
            replacement,
            previousStateWasModified);

        return result;
    }

    public StartingResourcesSettings GetAppliedSettings(ProjectModel project)
    {
        GameplayOperationStateModel state =
            stateService.GetRequiredCompatibleState(
                project,
                ProgressionType.StartingResources);
        return state.StartingResources?.DeepClone()
            ?? throw new InvalidOperationException(
                "Starting Resources settings are missing.");
    }

    internal static JArray CaptureCurrentTargets(ProjectModel project)
    {
        StartingResourcesTargets targets = ResolveTargets(project);
        JArray result = new();

        foreach (SharedTarget target in targets.Shared)
        {
            result.Add(new JObject
            {
                ["kind"] = "shared",
                ["entry"] = target.Entry.Id,
                ["path"] = "props.startQuantity",
                ["value"] = target.Property.SourceProperty!.Value.DeepClone(),
                ["difficulty"] = target.DifficultyToken?.DeepClone()
            });
        }

        foreach (OriginTarget target in targets.Origins)
        {
            result.Add(new JObject
            {
                ["kind"] = "origin",
                ["entry"] = target.Entry.Id,
                ["pattern"] = target.Pattern,
                ["path"] = "props.items",
                ["value"] = target.Items.DeepClone()
            });
        }

        return result;
    }

    internal static JArray BuildExpectedTargets(
        ProjectModel project,
        JArray baseline,
        StartingResourcesSettings settings)
    {
        settings.Validate();
        StartingResourcesTargets currentTargets = ResolveTargets(project);
        ValidateBaselineTargets(baseline, currentTargets);
        JArray result = (JArray)baseline.DeepClone();

        Dictionary<string, int> sharedExtras = new(StringComparer.Ordinal)
        {
            [GoldItemId] = settings.Krowns,
            [BreadItemId] = settings.Bread,
            [AppleItemId] = settings.Apples
        };

        foreach (JObject record in result.OfType<JObject>())
        {
            string kind = record.Value<string>("kind") ?? string.Empty;
            if (kind == "shared")
            {
                string entry = record.Value<string>("entry") ?? string.Empty;
                long baselineValue = record.Value<long>("value");
                record["value"] = checked(baselineValue + sharedExtras[entry]);
                continue;
            }

            JArray items = (JArray)record["value"]!;
            MergeExtra(items, IronOreItemId, settings.IronOre);
            MergeExtra(items, WoodItemId, settings.Wood);
            MergeExtra(items, ClothItemId, settings.Cloth);
        }

        return result;
    }

    internal static void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        StartingResourcesSettings settings = state.StartingResources
            ?? throw new InvalidOperationException(
                "The selected resource amounts are missing.");
        settings.Validate();

        if (!string.Equals(state.TargetSheet, "item,startChoice", StringComparison.Ordinal) ||
            !string.Equals(state.TargetEntry, "StartingResources", StringComparison.Ordinal) ||
            !string.Equals(state.TargetPath, "props.startQuantity|props.items", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The Starting Resources target identity does not match.");
        }

        if (state.BaselineArray.Count != state.ElementCount)
        {
            throw new InvalidOperationException(
                "The remembered target count is invalid.");
        }

        if (!string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(state.BaselineArray),
                state.BaselineFingerprint,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService.CreateShapeFingerprint(state.BaselineArray),
                state.ElementShapeFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The remembered Starting Resources data is invalid.");
        }

        JArray expected = BuildExpectedTargets(project, state.BaselineArray, settings);
        if (!string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(expected),
                state.ExpectedCurrentFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The selected extras do not reproduce the expected result.");
        }

        JArray current = CaptureCurrentTargets(project);
        if (!string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(current),
                state.ExpectedCurrentFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The loaded starting supplies do not match the saved settings.");
        }
    }

    internal static StartingResourcesTargets ResolveTargets(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        SheetModel itemSheet = FindSheet(project, "item");
        SheetModel startChoiceSheet = FindSheet(project, "startChoice");
        SheetModel unitPatternSheet = FindSheet(project, "unitPattern");

        string[] allItemIds =
        {
            GoldItemId, BreadItemId, AppleItemId,
            IronOreItemId, WoodItemId, ClothItemId
        };
        foreach (string itemId in allItemIds)
        {
            _ = FindEntry(itemSheet, itemId);
        }

        List<SharedTarget> shared = new();
        foreach (string itemId in allItemIds.Take(3))
        {
            EntryModel entry = FindEntry(itemSheet, itemId);
            PropertyModel property = entry.Properties.SingleOrDefault(p =>
                string.Equals(p.EffectivePropertyPath, "props.startQuantity", StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"Item '{itemId}' has no props.startQuantity property.");
            if (property.SourceProperty?.Value.Type != JTokenType.Integer)
            {
                throw new InvalidOperationException(
                    $"Item '{itemId}' props.startQuantity must be an integer.");
            }
            JToken? difficulty = entry.SourceEntry?["props"]?["startQuantityDifficultyBonus"];
            shared.Add(new SharedTarget(entry, property, difficulty));
        }

        List<OriginTarget> origins = new();
        foreach (EntryModel entry in startChoiceSheet.Entries)
        {
            JObject? source = entry.SourceEntry;
            JObject? props = source?["props"] as JObject;
            string pattern = props?["pattern"]?.Value<string>() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pattern) ||
                string.Equals(entry.Id, "TroopDefault", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(source?["desc"]?.Value<string>()) ||
                string.IsNullOrWhiteSpace(source?["introText"]?.Value<string>()))
            {
                continue;
            }
            if (!unitPatternSheet.Entries.Any(candidate =>
                    string.Equals(candidate.Id, pattern, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"Starting origin '{entry.Id}' references missing unit pattern '{pattern}'.");
            }
            if (props?["items"] is not JArray items)
            {
                throw new InvalidOperationException(
                    $"Starting origin '{entry.Id}' has no props.items array.");
            }
            origins.Add(new OriginTarget(entry, pattern, items));
        }

        if (origins.Count == 0)
        {
            throw new InvalidOperationException("No eligible starting origins were found.");
        }

        return new StartingResourcesTargets(shared, origins);
    }

    private void ApplyExpectedTargets(
        ProjectModel project,
        JArray expected,
        ProjectMutationResult aggregate)
    {
        StartingResourcesTargets targets = ResolveTargets(project);
        foreach (JObject record in expected.OfType<JObject>())
        {
            string entryId = record.Value<string>("entry")!;
            string kind = record.Value<string>("kind")!;
            EntryModel entry = kind == "shared"
                ? targets.Shared.Single(target => target.Entry.Id == entryId).Entry
                : targets.Origins.Single(target => target.Entry.Id == entryId).Entry;
            aggregate.Merge(mutationService.EnsurePropertyByPath(
                entry,
                record.Value<string>("path")!,
                record["value"]!.DeepClone()));
        }
    }

    private static void MergeExtra(JArray items, string itemId, int extra)
    {
        JObject[] matches = items.OfType<JObject>()
            .Where(item => string.Equals(item.Value<string>("item"), itemId, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Duplicate starting item '{itemId}' was found.");
        }
        if (matches.Length == 1)
        {
            JToken? countToken = matches[0]["count"];
            if (countToken?.Type != JTokenType.Integer)
            {
                throw new InvalidOperationException($"Starting item '{itemId}' count must be an integer.");
            }
            matches[0]["count"] = checked(countToken.Value<long>() + extra);
        }
        else if (extra > 0)
        {
            items.Add(new JObject
            {
                ["item"] = itemId,
                ["count"] = extra,
                ["stolen"] = false,
                ["hidden"] = false
            });
        }
    }

    private static void ValidateBaselineTargets(
        JArray baseline,
        StartingResourcesTargets targets)
    {
        string[] expectedShared = targets.Shared.Select(t => t.Entry.Id).ToArray();
        string[] baselineShared = baseline.OfType<JObject>()
            .Where(r => r.Value<string>("kind") == "shared")
            .Select(r => r.Value<string>("entry") ?? string.Empty).ToArray();
        string[] expectedOrigins = targets.Origins.Select(t => t.Entry.Id).ToArray();
        string[] baselineOrigins = baseline.OfType<JObject>()
            .Where(r => r.Value<string>("kind") == "origin")
            .Select(r => r.Value<string>("entry") ?? string.Empty).ToArray();
        if (!expectedShared.SequenceEqual(baselineShared) ||
            !expectedOrigins.SequenceEqual(baselineOrigins))
        {
            throw new InvalidOperationException(
                "The eligible Starting Resources targets have changed.");
        }
        foreach (JObject record in baseline.OfType<JObject>()
                     .Where(r => r.Value<string>("kind") == "shared"))
        {
            SharedTarget target = targets.Shared.Single(t => t.Entry.Id == record.Value<string>("entry"));
            if (!JToken.DeepEquals(record["difficulty"], target.DifficultyToken))
            {
                throw new InvalidOperationException(
                    $"Item '{target.Entry.Id}' difficulty additions have changed.");
            }
        }

        foreach (JObject record in baseline.OfType<JObject>()
                     .Where(r => r.Value<string>("kind") == "origin"))
        {
            OriginTarget target = targets.Origins.Single(t => t.Entry.Id == record.Value<string>("entry"));
            if (!string.Equals(
                    record.Value<string>("pattern"),
                    target.Pattern,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Starting origin '{target.Entry.Id}' unit pattern has changed.");
            }
        }
    }

    private static GameplayOperationStateModel CreateState(
        JArray baseline,
        StartingResourcesSettings settings,
        JArray expected)
    {
        return new GameplayOperationStateModel
        {
            OperationType = ProgressionType.StartingResources,
            TargetSheet = "item,startChoice",
            TargetEntry = "StartingResources",
            TargetPath = "props.startQuantity|props.items",
            BaselineArray = (JArray)baseline.DeepClone(),
            AppliedPercentage = 100,
            StartingResources = settings.DeepClone(),
            BaselineFingerprint = GameplayOperationFingerprintService.CreateContentFingerprint(baseline),
            ExpectedCurrentFingerprint = GameplayOperationFingerprintService.CreateContentFingerprint(expected),
            ElementCount = baseline.Count,
            ElementShapeFingerprint = GameplayOperationFingerprintService.CreateShapeFingerprint(baseline),
            IsCompatible = true
        };
    }

    private static bool SettingsEqual(
        StartingResourcesSettings? left,
        StartingResourcesSettings right) =>
        left != null &&
        left.Krowns == right.Krowns && left.Bread == right.Bread &&
        left.Apples == right.Apples && left.IronOre == right.IronOre &&
        left.Wood == right.Wood && left.Cloth == right.Cloth;

    private static SheetModel FindSheet(ProjectModel project, string name) =>
        project.Sheets.SingleOrDefault(sheet => string.Equals(sheet.Name, name, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Required sheet '{name}' was not found.");

    private static EntryModel FindEntry(SheetModel sheet, string id) =>
        sheet.Entries.SingleOrDefault(entry => string.Equals(entry.Id, id, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Required entry '{id}' was not found in '{sheet.Name}'.");
}

internal sealed record SharedTarget(
    EntryModel Entry,
    PropertyModel Property,
    JToken? DifficultyToken);

internal sealed record OriginTarget(
    EntryModel Entry,
    string Pattern,
    JArray Items);

internal sealed record StartingResourcesTargets(
    IReadOnlyList<SharedTarget> Shared,
    IReadOnlyList<OriginTarget> Origins);
