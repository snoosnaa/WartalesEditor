using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public enum RandomTraitExclusionRestoreStatus
{
    Succeeded,
    Unavailable,
    Failed
}

public sealed class RandomTraitExclusionRestoreSelectionResult
{
    public RandomTraitExclusionRestoreSelectionResult(
        RandomTraitExclusionRestoreStatus status,
        IReadOnlyCollection<string>? allowedTraitIds = null)
    {
        Status = status;
        AllowedTraitIds = allowedTraitIds ?? Array.Empty<string>();
    }

    public RandomTraitExclusionRestoreStatus Status { get; }
    public IReadOnlyCollection<string> AllowedTraitIds { get; }
}

internal sealed record RandomTraitExclusionsReplayAvailability(
    IReadOnlyList<string> UnavailableTraitIds,
    bool AllRequestedTraitsUnavailable);

internal sealed record RandomTraitExclusionsReplayResult(
    ProjectMutationResult MutationResult,
    IReadOnlyList<string> UnavailableTraitIds,
    bool AllRequestedTraitsUnavailable);

public sealed class RandomTraitExclusionsService
{
    private const string TraitSheetName = "trait";
    private const string DonePath = "done";
    private const string StartingGroup = "Starting";
    private const string RecruitmentGroup = "Recruitment";
    private const string GroupField = "group";

    private readonly ProjectMutationService mutationService;
    private readonly GameplayOperationStateService stateService;

    public RandomTraitExclusionsService(
        ProjectMutationService mutationService,
        GameplayOperationStateService stateService)
    {
        this.mutationService = mutationService
            ?? throw new ArgumentNullException(nameof(mutationService));
        this.stateService = stateService
            ?? throw new ArgumentNullException(nameof(stateService));
    }

    public IReadOnlyList<RandomTraitExclusionCandidate> Discover(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        GameplayOperationStateModel? state = stateService.FindState(
            project,
            ProgressionType.RandomTraitExclusions);
        Dictionary<string, RandomTraitDoneBaseline> baselines = new(
            StringComparer.Ordinal);

        if (state != null)
        {
            ValidateState(project, state);
            foreach (JObject record in state.BaselineArray.OfType<JObject>())
                baselines.Add(ReadRequiredString(record, "id"), ReadBaseline(record));
        }

        IReadOnlyList<ResolvedTrait> resolved = ResolveCandidates(project);
        return resolved.Select(candidate =>
        {
            RandomTraitDoneBaseline baseline = baselines.TryGetValue(
                candidate.Entry.Id,
                out RandomTraitDoneBaseline ownedBaseline)
                    ? ownedBaseline
                    : candidate.CurrentDone;
            return new RandomTraitExclusionCandidate
            {
                Id = candidate.Entry.Id,
                DisplayNameKey = candidate.Entry.DisplayName,
                Personality = candidate.Personality,
                SemanticGroup = candidate.Group,
                BaselineDone = baseline,
                IsAllowed = candidate.CurrentDone != RandomTraitDoneBaseline.False
            };
        }).ToArray();
    }

    public bool CanRestorePreviousValues(ProjectModel project) =>
        stateService.CanRestorePreviousValues(
            project,
            ProgressionType.RandomTraitExclusions);

    public IReadOnlyCollection<string> GetPreviousAllowedTraitIds(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        GameplayOperationStateModel state =
            stateService.GetRequiredPreviousValuesState(
                project,
                ProgressionType.RandomTraitExclusions);
        IReadOnlyList<ResolvedTrait> candidates =
            ResolveCandidates(project);
        Dictionary<string, RandomTraitDoneBaseline> baselines =
            state.BaselineArray
                .OfType<JObject>()
                .ToDictionary(
                    record => ReadRequiredString(record, "id"),
                    ReadBaseline,
                    StringComparer.Ordinal);

        return candidates
            .Where(candidate =>
                (baselines.TryGetValue(
                    candidate.Entry.Id,
                    out RandomTraitDoneBaseline baseline)
                        ? baseline
                        : candidate.CurrentDone) != RandomTraitDoneBaseline.False)
            .Select(candidate => candidate.Entry.Id)
            .ToArray();
    }

    public RandomTraitExclusionRestoreSelectionResult
        ResolvePreviousAllowedTraitIds(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (!CanRestorePreviousValues(project))
        {
            return new RandomTraitExclusionRestoreSelectionResult(
                RandomTraitExclusionRestoreStatus.Unavailable);
        }

        try
        {
            return new RandomTraitExclusionRestoreSelectionResult(
                RandomTraitExclusionRestoreStatus.Succeeded,
                GetPreviousAllowedTraitIds(project));
        }
        catch (InvalidOperationException)
        {
            return new RandomTraitExclusionRestoreSelectionResult(
                RandomTraitExclusionRestoreStatus.Unavailable);
        }
        catch (Exception)
        {
            return new RandomTraitExclusionRestoreSelectionResult(
                RandomTraitExclusionRestoreStatus.Failed);
        }
    }

    public ProjectMutationResult Apply(
        ProjectModel project,
        IReadOnlyCollection<string> allowedTraitIds)
    {
        return ApplyCore(project, allowedTraitIds, new ProjectMutationResult());
    }

    internal ProjectMutationResult Apply(
        ProjectModel project,
        IReadOnlyCollection<string> allowedTraitIds,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ApplyCore(project, allowedTraitIds, context.MutationResult);
    }

    internal RandomTraitExclusionsReplayResult Replay(
        ProjectModel project,
        JArray semanticSelections,
        JArray? exactSourceBaseline,
        bool allowUnavailableTraits,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ReplayPreparation preparation = PrepareReplay(
            project,
            semanticSelections,
            exactSourceBaseline,
            allowUnavailableTraits);

        if (preparation.AllRequestedTraitsUnavailable)
        {
            return new RandomTraitExclusionsReplayResult(
                context.MutationResult,
                preparation.UnavailableTraitIds,
                AllRequestedTraitsUnavailable: true);
        }

        GameplayOperationStateModel? existing = stateService.FindState(
            project,
            ProgressionType.RandomTraitExclusions);
        GameplayOperationStateModel replacement = CreateState(
            preparation.Baseline,
            preparation.Expected,
            preparation.Allowed);

        if (existing != null &&
            JToken.DeepEquals(
                existing.BaselineArray,
                preparation.Baseline) &&
            JToken.DeepEquals(
                existing.GameplaySettings,
                replacement.GameplaySettings) &&
            JToken.DeepEquals(
                CaptureCurrent(preparation.Candidates),
                preparation.Expected))
        {
            return new RandomTraitExclusionsReplayResult(
                context.MutationResult,
                preparation.UnavailableTraitIds,
                AllRequestedTraitsUnavailable: false);
        }

        ProjectMutationResult result = ApplyResolved(
            project,
            preparation.Candidates,
            preparation.Expected,
            existing,
            replacement,
            context.MutationResult);
        return new RandomTraitExclusionsReplayResult(
            result,
            preparation.UnavailableTraitIds,
            AllRequestedTraitsUnavailable: false);
    }

    internal RandomTraitExclusionsReplayAvailability PreflightReplay(
        ProjectModel project,
        JArray semanticSelections,
        JArray? exactSourceBaseline,
        bool allowUnavailableTraits)
    {
        ReplayPreparation preparation = PrepareReplay(
            project,
            semanticSelections,
            exactSourceBaseline,
            allowUnavailableTraits);
        return new RandomTraitExclusionsReplayAvailability(
            preparation.UnavailableTraitIds,
            preparation.AllRequestedTraitsUnavailable);
    }

    internal static bool CurrentMatchesBaseline(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        IReadOnlyList<ResolvedTrait> candidates = ResolveCandidates(project);
        return state.ElementCount == state.BaselineArray.Count &&
            string.Equals(
                state.TargetSheet,
                TraitSheetName,
                StringComparison.Ordinal) &&
            string.Equals(
                state.TargetEntry,
                JoinIds(state.BaselineArray),
                StringComparison.Ordinal) &&
            string.Equals(
                state.TargetPath,
                string.Join("|", state.BaselineArray
                    .OfType<JObject>()
                    .Select(record =>
                        $"{ReadRequiredString(record, "id")}.{DonePath}")),
                StringComparison.Ordinal) &&
            JToken.DeepEquals(
                CaptureCurrent(candidates),
                state.BaselineArray);
    }

    private static ReplayPreparation PrepareReplay(
        ProjectModel project,
        JArray semanticSelections,
        JArray? exactSourceBaseline,
        bool allowUnavailableTraits)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(semanticSelections);

        Dictionary<string, JObject> requested = semanticSelections
            .OfType<JObject>()
            .ToDictionary(
                selection => ReadRequiredString(selection, "id"),
                StringComparer.Ordinal);
        Dictionary<string, int> sourceOccurrences = GetTraitSourceEntries(project)
            .Where(entry => entry["id"]?.Type == JTokenType.String)
            .GroupBy(entry => entry.Value<string>("id")!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        string[] ambiguous = requested.Keys
            .Where(id => sourceOccurrences.TryGetValue(id, out int count) && count > 1)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        if (ambiguous.Length > 0)
        {
            throw new InvalidOperationException(
                "These profile traits have ambiguous identities in the current " +
                $"game data: {string.Join(", ", ambiguous)}.");
        }

        IReadOnlyList<ResolvedTrait> candidates = ResolveCandidates(project);
        Dictionary<string, ResolvedTrait> byId = candidates.ToDictionary(
            candidate => candidate.Entry.Id,
            StringComparer.Ordinal);

        string[] unavailable = requested.Keys
            .Where(id => !sourceOccurrences.ContainsKey(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] presentButIncompatible = requested.Keys
            .Where(id => sourceOccurrences.ContainsKey(id) && !byId.ContainsKey(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        if (presentButIncompatible.Length > 0)
        {
            throw new InvalidOperationException(
                "These profile traits still exist but are no longer valid " +
                "random recruit traits: " +
                $"{string.Join(", ", presentButIncompatible)}.");
        }

        if (unavailable.Length > 0)
        {
            if (!allowUnavailableTraits)
            {
                throw new InvalidOperationException(
                    "These profile traits are not available in the current " +
                    $"game data: {string.Join(", ", unavailable)}.");
            }
        }

        foreach ((string id, JObject selection) in requested)
        {
            if (!byId.TryGetValue(id, out ResolvedTrait? candidate))
                continue;
            string personality = ReadRequiredString(
                selection,
                "personality");
            string group = ReadRequiredString(selection, GroupField);
            if (!string.Equals(
                    personality,
                    candidate.Personality.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    group,
                    candidate.Group,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Profile trait '{id}' has changed semantic identity.");
            }
        }

        Dictionary<string, RandomTraitDoneBaseline> baselines = new(
            StringComparer.Ordinal);
        if (exactSourceBaseline != null)
        {
            foreach (JObject record in exactSourceBaseline.OfType<JObject>())
            {
                string id = ReadRequiredString(record, "id");
                if (!byId.TryGetValue(id, out ResolvedTrait? candidate) ||
                    record.Value<int?>("personality") !=
                        (int)candidate.Personality ||
                    !string.Equals(
                        ReadRequiredString(record, GroupField),
                        candidate.Group,
                        StringComparison.Ordinal) ||
                    !baselines.TryAdd(id, ReadBaseline(record)))
                {
                    throw new InvalidOperationException(
                        $"Exact-source trait baseline '{id}' is not " +
                        "compatible with the current candidate structure.");
                }
            }

            if (requested.Keys.Any(id => !baselines.ContainsKey(id)))
            {
                throw new InvalidOperationException(
                    "The exact-source trait baseline does not contain " +
                    "every previously targeted candidate.");
            }
        }

        foreach (ResolvedTrait candidate in candidates)
        {
            baselines.TryAdd(candidate.Entry.Id, candidate.CurrentDone);
        }

        HashSet<string> allowed = new(StringComparer.Ordinal);
        foreach (ResolvedTrait candidate in candidates)
        {
            if (requested.TryGetValue(
                    candidate.Entry.Id,
                    out JObject? selection))
            {
                if (selection["allowed"]?.Type != JTokenType.Boolean)
                {
                    throw new InvalidOperationException(
                        $"Profile trait '{candidate.Entry.Id}' has no " +
                        "valid allowed state.");
                }

                if (selection.Value<bool>("allowed"))
                {
                    allowed.Add(candidate.Entry.Id);
                }
            }
            else if (candidate.CurrentDone !=
                     RandomTraitDoneBaseline.False)
            {
                allowed.Add(candidate.Entry.Id);
            }
        }

        JArray baseline = CreateBaseline(candidates, baselines);
        JArray expected = CreateExpected(baseline, allowed);
        return new ReplayPreparation(
            candidates,
            baseline,
            expected,
            allowed,
            unavailable,
            requested.Count > 0 && unavailable.Length == requested.Count);
    }

    private sealed record ReplayPreparation(
        IReadOnlyList<ResolvedTrait> Candidates,
        JArray Baseline,
        JArray Expected,
        HashSet<string> Allowed,
        IReadOnlyList<string> UnavailableTraitIds,
        bool AllRequestedTraitsUnavailable);

    private ProjectMutationResult ApplyCore(
        ProjectModel project,
        IReadOnlyCollection<string> allowedTraitIds,
        ProjectMutationResult result)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(allowedTraitIds);

        IReadOnlyList<ResolvedTrait> candidates = ResolveCandidates(project);
        Dictionary<string, ResolvedTrait> byId = candidates.ToDictionary(
            candidate => candidate.Entry.Id,
            StringComparer.Ordinal);
        HashSet<string> allowed = ValidateSelection(allowedTraitIds, byId.Keys);
        GameplayOperationStateModel? existing = stateService.FindState(
            project,
            ProgressionType.RandomTraitExclusions);
        Dictionary<string, RandomTraitDoneBaseline> baselines = new(
            StringComparer.Ordinal);

        if (existing != null)
        {
            ValidateState(project, existing);
            foreach (JObject record in existing.BaselineArray.OfType<JObject>())
                baselines.Add(ReadRequiredString(record, "id"), ReadBaseline(record));
        }

        foreach (ResolvedTrait candidate in candidates)
            baselines.TryAdd(candidate.Entry.Id, candidate.CurrentDone);

        JArray baseline = CreateBaseline(candidates, baselines);
        JArray expected = CreateExpected(baseline, allowed);
        GameplayOperationStateModel replacement = CreateState(baseline, expected, allowed);

        if (existing != null &&
            string.Equals(existing.ExpectedCurrentFingerprint,
                replacement.ExpectedCurrentFingerprint,
                StringComparison.Ordinal) &&
            JToken.DeepEquals(existing.BaselineArray, replacement.BaselineArray) &&
            JToken.DeepEquals(existing.GameplaySettings, replacement.GameplaySettings))
            return result;

        return ApplyResolved(project, candidates, expected, existing, replacement, result);
    }

    public ProjectMutationResult RestoreState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(state);
        ValidateStateCompatibility(project, state);

        IReadOnlyList<ResolvedTrait> candidates = ResolveOwnedTraits(project, state);
        HashSet<string> allowed = ReadAllowedIds(state);
        JArray expected = CreateExpected(state.BaselineArray, allowed);
        ValidateReplayCurrent(candidates, state.BaselineArray, expected);
        GameplayOperationStateModel? existing = stateService.FindState(
            project,
            ProgressionType.RandomTraitExclusions);

        if (existing != null &&
            JToken.DeepEquals(existing.BaselineArray, state.BaselineArray) &&
            JToken.DeepEquals(existing.GameplaySettings, state.GameplaySettings) &&
            JToken.DeepEquals(CaptureCurrent(candidates), expected))
            return new ProjectMutationResult();

        return ApplyResolved(
            project, candidates, expected, existing, state,
            new ProjectMutationResult());
    }

    internal static void ValidateState(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        ValidateStateCompatibility(project, state);
        IReadOnlyList<ResolvedTrait> candidates = ResolveOwnedTraits(project, state);
        JArray current = CaptureCurrent(candidates);
        if (!string.Equals(
                GameplayOperationFingerprintService.CreateContentFingerprint(current),
                state.ExpectedCurrentFingerprint,
                StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The saved random trait exclusions no longer match the loaded project.");
    }

    internal static void ValidateStateCompatibility(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(state);
        if (state.OperationType != ProgressionType.RandomTraitExclusions ||
            state.FormatVersion != GameplayOperationStateModel.CurrentFormatVersion ||
            !string.Equals(state.TargetSheet, TraitSheetName, StringComparison.Ordinal) ||
            state.ElementCount <= 0 ||
            state.BaselineArray.Count != state.ElementCount)
            throw new InvalidOperationException("The saved random trait exclusion state is invalid.");

        HashSet<string> allowed = ReadAllowedIds(state);
        JArray expected = CreateExpected(state.BaselineArray, allowed);
        string entries = JoinIds(state.BaselineArray);
        string paths = string.Join("|", state.BaselineArray.OfType<JObject>()
            .Select(record => $"{ReadRequiredString(record, "id")}.{DonePath}"));

        if (!string.Equals(state.TargetEntry, entries, StringComparison.Ordinal) ||
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
                StringComparison.Ordinal))
            throw new InvalidOperationException("The saved random trait exclusion fingerprints are invalid.");

        _ = ResolveOwnedTraits(project, state);
    }

    internal static HashSet<string> ReadAllowedIds(GameplayOperationStateModel state)
    {
        if (state.GameplaySettings?["allowedTraitIds"] is not JArray array)
            throw new InvalidOperationException("The saved allowed-trait selection is missing.");
        string[] ids = array.Select(token => token.Type == JTokenType.String
                ? token.Value<string>() ?? string.Empty
                : string.Empty).ToArray();
        if (ids.Any(string.IsNullOrWhiteSpace) ||
            ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new InvalidOperationException("The saved allowed-trait selection is invalid.");
        return ids.ToHashSet(StringComparer.Ordinal);
    }

    internal static HashSet<string> ResolveCandidateIds(ProjectModel project) =>
        ResolveCandidates(project)
            .Select(candidate => candidate.Entry.Id)
            .ToHashSet(StringComparer.Ordinal);

    internal static HashSet<string> GetChangedTraitIds(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        ValidateStateCompatibility(project, state);
        Dictionary<string, RandomTraitDoneBaseline> baselines =
            state.BaselineArray
                .OfType<JObject>()
                .ToDictionary(
                    record => ReadRequiredString(record, "id"),
                    ReadBaseline,
                    StringComparer.Ordinal);

        return ResolveOwnedTraits(project, state)
            .Where(candidate =>
                candidate.CurrentDone != baselines[candidate.Entry.Id])
            .Select(candidate => candidate.Entry.Id)
            .ToHashSet(StringComparer.Ordinal);
    }

    private ProjectMutationResult ApplyResolved(
        ProjectModel project,
        IReadOnlyList<ResolvedTrait> candidates,
        JArray expected,
        GameplayOperationStateModel? existing,
        GameplayOperationStateModel replacement,
        ProjectMutationResult result)
    {
        Dictionary<string, JObject> expectedById = expected.OfType<JObject>()
            .ToDictionary(record => ReadRequiredString(record, "id"), StringComparer.Ordinal);
        ValidateMutationTargets(candidates, expectedById);
        foreach (ResolvedTrait candidate in candidates)
        {
            RandomTraitDoneBaseline target = ReadBaseline(expectedById[candidate.Entry.Id]);
            if (target == candidate.CurrentDone)
                continue;
            result.Merge(target == RandomTraitDoneBaseline.Absent
                ? mutationService.RemovePropertyByPath(candidate.Entry, DonePath)
                : mutationService.EnsurePropertyByPath(
                    candidate.Entry,
                    DonePath,
                    target == RandomTraitDoneBaseline.True));
        }

        GameplayOperationStateModel? previous = existing?.DeepClone();
        bool previousModified = project.IsGameplayOperationStateModified;
        result.AddGameplayOperationState(project, previous, replacement, previousModified);
        stateService.ReplaceState(project, replacement);
        return result;
    }

    private static void ValidateMutationTargets(
        IReadOnlyList<ResolvedTrait> candidates,
        IReadOnlyDictionary<string, JObject> expectedById)
    {
        foreach (ResolvedTrait candidate in candidates)
        {
            RandomTraitDoneBaseline target = ReadBaseline(expectedById[candidate.Entry.Id]);
            PropertyModel[] models = candidate.Entry.Properties.Where(property =>
                string.Equals(property.EffectivePropertyPath, DonePath, StringComparison.Ordinal))
                .ToArray();
            JProperty? source = candidate.Entry.SourceEntry!.Property(DonePath);

            ValidateDoneConnection(candidate.Entry, source, models);

            if (target == candidate.CurrentDone)
                continue;

            if (target == RandomTraitDoneBaseline.Absent)
            {
                if (source == null)
                    throw new InvalidOperationException(
                        $"Trait '{candidate.Entry.Id}' does not have one connected 'done' value to restore.");
            }
        }
    }

    private static void ValidateReplayCurrent(
        IReadOnlyList<ResolvedTrait> candidates,
        JArray baseline,
        JArray expected)
    {
        Dictionary<string, RandomTraitDoneBaseline> baselineById = baseline
            .OfType<JObject>().ToDictionary(
                record => ReadRequiredString(record, "id"),
                ReadBaseline,
                StringComparer.Ordinal);
        Dictionary<string, RandomTraitDoneBaseline> expectedById = expected
            .OfType<JObject>().ToDictionary(
                record => ReadRequiredString(record, "id"),
                ReadBaseline,
                StringComparer.Ordinal);
        foreach (ResolvedTrait candidate in candidates)
            if (candidate.CurrentDone != baselineById[candidate.Entry.Id] &&
                candidate.CurrentDone != expectedById[candidate.Entry.Id])
                throw new InvalidOperationException(
                    $"Trait '{candidate.Entry.Id}' has changed from the profile baseline.");
    }

    private static IReadOnlyList<ResolvedTrait> ResolveCandidates(ProjectModel project)
    {
        SheetModel sheet = project.Sheets.SingleOrDefault(candidate =>
            string.Equals(candidate.Name, TraitSheetName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "Random trait data is not available in this project.");
        List<ResolvedTrait> candidates = new();
        foreach (ResolvedSource resolvedSource in ResolveSupportedSources(sheet))
        {
            JObject source = resolvedSource.Source;
            if (source.SelectToken("props.personality") is not JValue personalityValue ||
                personalityValue.Type != JTokenType.Integer)
                continue;
            long personalityNumber = personalityValue.Value<long>();
            if (personalityNumber is not (0 or 1))
                continue;

            EntryModel[] connectedEntries = sheet.Entries.Where(entry =>
                    ReferenceEquals(entry.SourceEntry, source))
                .ToArray();
            if (connectedEntries.Length != 1)
                throw new InvalidOperationException(
                    "A supported random trait is not connected to exactly one model entry.");
            EntryModel entry = connectedEntries.Single();
            JProperty? sourceIdProperty = source.Property("id");
            if (sourceIdProperty?.Value is not JValue sourceIdValue ||
                sourceIdValue.Type != JTokenType.String ||
                string.IsNullOrWhiteSpace(sourceIdValue.Value<string>()))
                throw new InvalidOperationException("A random trait does not have a stable identifier.");
            string sourceId = sourceIdValue.Value<string>()!;
            if (!string.Equals(sourceId, entry.Id, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Random trait '{sourceId}' is not connected to its stable model identifier.");
            ValidateDoneConnection(
                entry,
                source.Property(DonePath),
                entry.Properties.Where(property =>
                        string.Equals(
                            property.EffectivePropertyPath,
                            DonePath,
                            StringComparison.Ordinal))
                    .ToArray());
            candidates.Add(new ResolvedTrait(
                entry,
                resolvedSource.Group,
                personalityNumber == 0
                    ? RandomTraitPersonality.Positive
                    : RandomTraitPersonality.Negative,
                ReadCurrentDone(source)));
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException(
                "No compatible standard random traits were found in this project.");
        if (candidates.Select(candidate => candidate.Entry.Id)
            .Distinct(StringComparer.Ordinal).Count() != candidates.Count)
            throw new InvalidOperationException("Random trait identifiers are not unique.");
        return candidates.OrderBy(candidate => candidate.Entry.Id, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<JObject> GetTraitSourceEntries(ProjectModel project)
    {
        SheetModel sheet = project.Sheets.SingleOrDefault(candidate =>
            string.Equals(candidate.Name, TraitSheetName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "Random trait data is not available in this project.");
        if (sheet.SourceSheet?["lines"] is not JArray lines ||
            lines.Any(token => token is not JObject))
        {
            throw CreateSeparatorCompatibilityException();
        }

        return lines.OfType<JObject>().ToArray();
    }

    private static IReadOnlyList<ResolvedSource> ResolveSupportedSources(SheetModel sheet)
    {
        if (sheet.SourceSheet?["lines"] is not JArray lines ||
            sheet.SourceSheet["separators"] is not JArray separators ||
            lines.Any(token => token is not JObject) ||
            separators.Any(token => token is not JObject))
            throw CreateSeparatorCompatibilityException();

        JObject[] sourceEntries = lines.OfType<JObject>().ToArray();
        IReadOnlyList<ResolvedGroup> groups = ResolveTopLevelGroups(
            sheet,
            sourceEntries,
            separators);
        foreach (string requiredGroup in new[] { StartingGroup, RecruitmentGroup })
        {
            if (groups.Count(group => string.Equals(
                    group.Title,
                    requiredGroup,
                    StringComparison.Ordinal)) != 1)
            {
                throw CreateSeparatorCompatibilityException();
            }
        }

        HashSet<JObject> supported = new(
            ReferenceEqualityComparer.Instance);
        foreach (ResolvedGroup group in groups.Where(group =>
                     string.Equals(group.Title, StartingGroup, StringComparison.Ordinal) ||
                     string.Equals(group.Title, RecruitmentGroup, StringComparison.Ordinal)))
        {
            for (int index = group.StartIndex; index < group.EndIndex; index++)
                supported.Add(sourceEntries[index]);
        }

        foreach (JObject source in sourceEntries)
        {
            if (TryReadPersonality(source, out _) &&
                HasPositiveFiniteRecruitWeight(source))
            {
                supported.Add(source);
            }
        }

        List<ResolvedSource> resolved = new();
        for (int index = 0; index < sourceEntries.Length; index++)
        {
            JObject source = sourceEntries[index];
            if (!supported.Contains(source))
                continue;

            ResolvedGroup? group = groups.LastOrDefault(candidate =>
                candidate.StartIndex <= index && index < candidate.EndIndex);
            if (group == null)
                throw CreateSeparatorCompatibilityException();
            resolved.Add(new ResolvedSource(source, group.Title));
        }

        return resolved;
    }

    private static IReadOnlyList<ResolvedGroup> ResolveTopLevelGroups(
        SheetModel sheet,
        IReadOnlyList<JObject> sourceEntries,
        JArray separators)
    {
        List<ResolvedGroup> anchors = new();
        foreach (JObject separator in separators.OfType<JObject>())
        {
            JToken? level = separator["level"];
            if (level != null)
                continue;

            if (separator["title"]?.Type != JTokenType.String ||
                string.IsNullOrWhiteSpace(separator.Value<string>("title")) ||
                separator["id"]?.Type != JTokenType.String ||
                string.IsNullOrWhiteSpace(separator.Value<string>("id")))
            {
                throw CreateSeparatorCompatibilityException();
            }

            string anchorId = separator.Value<string>("id")!;
            int[] sourceMatches = sourceEntries.Select((entry, index) => new
                {
                    Entry = entry,
                    Index = index
                })
                .Where(item =>
                    item.Entry["id"]?.Type == JTokenType.String &&
                    string.Equals(
                        item.Entry.Value<string>("id"),
                        anchorId,
                        StringComparison.Ordinal))
                .Select(item => item.Index)
                .ToArray();
            if (sourceMatches.Length != 1 ||
                sheet.Entries.Count(entry => ReferenceEquals(
                    entry.SourceEntry,
                    sourceEntries[sourceMatches.Single()])) != 1)
            {
                throw CreateSeparatorCompatibilityException();
            }

            anchors.Add(new ResolvedGroup(
                separator.Value<string>("title")!,
                sourceMatches.Single(),
                sourceEntries.Count));
        }

        ResolvedGroup[] ordered = anchors.OrderBy(group => group.StartIndex).ToArray();
        if (ordered.Length == 0 ||
            ordered.Select(group => group.StartIndex).Distinct().Count() != ordered.Length)
        {
            throw CreateSeparatorCompatibilityException();
        }

        for (int index = 0; index < ordered.Length - 1; index++)
            ordered[index] = ordered[index] with { EndIndex = ordered[index + 1].StartIndex };
        return ordered;
    }

    private static bool TryReadPersonality(
        JObject source,
        out long personality)
    {
        personality = default;
        if (source.SelectToken("props.personality") is not JValue value ||
            value.Type != JTokenType.Integer)
        {
            return false;
        }

        personality = value.Value<long>();
        return personality is 0 or 1;
    }

    private static bool HasPositiveFiniteRecruitWeight(JObject source)
    {
        if (source["props"] is not JObject props)
            return false;
        JProperty[] properties = props.Properties().Where(property =>
                string.Equals(
                    property.Name,
                    "recruitWeight",
                    StringComparison.Ordinal))
            .ToArray();
        if (properties.Length != 1 ||
            properties.Single().Value is not JValue value ||
            value.Type is not (JTokenType.Integer or JTokenType.Float))
        {
            return false;
        }

        double weight;
        try
        {
            weight = value.Value<double>();
        }
        catch (Exception exception) when (
            exception is InvalidCastException or FormatException or OverflowException)
        {
            return false;
        }

        return double.IsFinite(weight) && weight > 0;
    }

    private static InvalidOperationException CreateSeparatorCompatibilityException() =>
        new(
            "Random Trait Exclusions is not compatible with this project's trait-group separators.");

    private static void ValidateDoneConnection(
        EntryModel entry,
        JProperty? source,
        IReadOnlyCollection<PropertyModel> models)
    {
        bool isConnected = models.Count == 1 &&
                           source != null &&
                           models.Single().SourceProperty != null &&
                           ReferenceEquals(models.Single().SourceProperty, source);

        if ((source == null && models.Count != 0) ||
            (source != null && !isConnected))
        {
            throw new InvalidOperationException(
                $"Trait '{entry.Id}' has a disconnected or ambiguous 'done' value.");
        }
    }

    private static IReadOnlyList<ResolvedTrait> ResolveOwnedTraits(
        ProjectModel project,
        GameplayOperationStateModel state)
    {
        IReadOnlyList<ResolvedTrait> all = ResolveCandidates(project);
        Dictionary<string, ResolvedTrait> byId = all.ToDictionary(
            candidate => candidate.Entry.Id,
            StringComparer.Ordinal);
        List<ResolvedTrait> owned = new();
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (JObject record in state.BaselineArray.OfType<JObject>())
        {
            string id = ReadRequiredString(record, "id");
            if (!seen.Add(id) || !byId.TryGetValue(id, out ResolvedTrait? trait))
                throw new InvalidOperationException(
                    $"Owned random trait '{id}' is missing or no longer compatible.");
            int personality = record.Value<int?>("personality")
                ?? throw new InvalidOperationException("A remembered trait personality is missing.");
            if (personality != (int)trait.Personality ||
                !string.Equals(
                    ReadRequiredString(record, GroupField),
                    trait.Group,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Owned random trait '{id}' has changed generation structure.");
            _ = ReadBaseline(record);
            owned.Add(trait);
        }
        if (owned.Count != state.BaselineArray.Count)
            throw new InvalidOperationException("The remembered trait baseline is malformed.");
        HashSet<string> allowed = ReadAllowedIds(state);
        if (!allowed.IsSubsetOf(seen))
            throw new InvalidOperationException("The saved selection contains an unowned trait.");
        return owned;
    }

    private static RandomTraitDoneBaseline ReadCurrentDone(JObject source)
    {
        JProperty? done = source.Property(DonePath);
        if (done == null) return RandomTraitDoneBaseline.Absent;
        if (done.Value.Type != JTokenType.Boolean)
            throw new InvalidOperationException(
                $"Random trait '{source.Value<string>("id")}' has an unsupported 'done' value.");
        return done.Value.Value<bool>()
            ? RandomTraitDoneBaseline.True
            : RandomTraitDoneBaseline.False;
    }

    private static JArray CreateBaseline(
        IReadOnlyList<ResolvedTrait> candidates,
        IReadOnlyDictionary<string, RandomTraitDoneBaseline> baselines) =>
        new(candidates.Select(candidate => new JObject
        {
            ["id"] = candidate.Entry.Id,
            ["personality"] = (int)candidate.Personality,
            [GroupField] = candidate.Group,
            ["doneState"] = baselines[candidate.Entry.Id].ToString()
        }));

    private static JArray CreateExpected(JArray baseline, HashSet<string> allowed)
    {
        JArray expected = new();
        foreach (JObject baselineRecord in baseline.OfType<JObject>())
        {
            string id = ReadRequiredString(baselineRecord, "id");
            RandomTraitDoneBaseline baselineDone = ReadBaseline(baselineRecord);
            RandomTraitDoneBaseline expectedDone = allowed.Contains(id)
                ? baselineDone == RandomTraitDoneBaseline.Absent
                    ? RandomTraitDoneBaseline.Absent
                    : RandomTraitDoneBaseline.True
                : RandomTraitDoneBaseline.False;
            JObject record = (JObject)baselineRecord.DeepClone();
            record["doneState"] = expectedDone.ToString();
            expected.Add(record);
        }
        return expected;
    }

    private static JArray CaptureCurrent(IReadOnlyList<ResolvedTrait> candidates) =>
        new(candidates.Select(candidate => new JObject
        {
            ["id"] = candidate.Entry.Id,
            ["personality"] = (int)candidate.Personality,
            [GroupField] = candidate.Group,
            ["doneState"] = candidate.CurrentDone.ToString()
        }));

    private static GameplayOperationStateModel CreateState(
        JArray baseline,
        JArray expected,
        HashSet<string> allowed) => new()
    {
        OperationType = ProgressionType.RandomTraitExclusions,
        TargetSheet = TraitSheetName,
        TargetEntry = JoinIds(baseline),
        TargetPath = string.Join("|", baseline.OfType<JObject>()
            .Select(record => $"{ReadRequiredString(record, "id")}.{DonePath}")),
        BaselineArray = (JArray)baseline.DeepClone(),
        BaselineFingerprint = GameplayOperationFingerprintService.CreateContentFingerprint(baseline),
        ExpectedCurrentFingerprint = GameplayOperationFingerprintService.CreateContentFingerprint(expected),
        ElementCount = baseline.Count,
        ElementShapeFingerprint = GameplayOperationFingerprintService.CreateShapeFingerprint(baseline),
        GameplaySettings = new JObject
        {
            ["allowedTraitIds"] = new JArray(allowed.OrderBy(id => id, StringComparer.Ordinal))
        }
    };

    private static HashSet<string> ValidateSelection(
        IReadOnlyCollection<string> selection,
        IEnumerable<string> candidateIds)
    {
        if (selection.Any(string.IsNullOrWhiteSpace) ||
            selection.Distinct(StringComparer.Ordinal).Count() != selection.Count)
            throw new InvalidOperationException("The trait selection contains invalid identifiers.");
        HashSet<string> candidates = candidateIds.ToHashSet(StringComparer.Ordinal);
        HashSet<string> selected = selection.ToHashSet(StringComparer.Ordinal);
        if (!selected.IsSubsetOf(candidates))
            throw new InvalidOperationException("The trait selection contains an unsupported trait.");
        return selected;
    }

    private static RandomTraitDoneBaseline ReadBaseline(JObject record)
    {
        string value = ReadRequiredString(record, "doneState");
        return Enum.TryParse(value, false, out RandomTraitDoneBaseline baseline)
            ? baseline
            : throw new InvalidOperationException("A remembered trait baseline is invalid.");
    }

    private static string ReadRequiredString(JObject record, string name) =>
        record[name]?.Type == JTokenType.String &&
        !string.IsNullOrWhiteSpace(record.Value<string>(name))
            ? record.Value<string>(name)!
            : throw new InvalidOperationException($"A remembered trait {name} is missing.");

    private static string JoinIds(JArray records) => string.Join(",",
        records.OfType<JObject>().Select(record => ReadRequiredString(record, "id")));

    private sealed record ResolvedTrait(
        EntryModel Entry,
        string Group,
        RandomTraitPersonality Personality,
        RandomTraitDoneBaseline CurrentDone);

    private sealed record ResolvedSource(
        JObject Source,
        string Group);

    private sealed record ResolvedGroup(
        string Title,
        int StartIndex,
        int EndIndex);
}
