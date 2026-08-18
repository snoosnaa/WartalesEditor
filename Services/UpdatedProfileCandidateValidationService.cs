using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class UpdatedProfileCandidateValidationService
{
    private readonly ModificationSnapshotService snapshotService;
    private readonly SnapshotPropertyResolutionService resolutionService;
    private readonly ProfileOperationCaptureService operationCaptureService;
    private readonly GameplayOperationStateService stateService;

    public UpdatedProfileCandidateValidationService()
        : this(
            new ModificationSnapshotService(),
            new SnapshotPropertyResolutionService(),
            ProfileOperationCaptureService.CreateDefault(),
            new GameplayOperationStateService())
    {
    }

    public UpdatedProfileCandidateValidationService(
        ModificationSnapshotService snapshotService,
        SnapshotPropertyResolutionService resolutionService,
        ProfileOperationCaptureService operationCaptureService,
        GameplayOperationStateService stateService)
    {
        this.snapshotService = snapshotService
            ?? throw new ArgumentNullException(nameof(snapshotService));
        this.resolutionService = resolutionService
            ?? throw new ArgumentNullException(nameof(resolutionService));
        this.operationCaptureService = operationCaptureService
            ?? throw new ArgumentNullException(nameof(operationCaptureService));
        this.stateService = stateService
            ?? throw new ArgumentNullException(nameof(stateService));
    }

    public void Validate(
        ProjectModel intendedProject,
        ModProfileModel existingProfile,
        ModProfileModel candidate)
    {
        ArgumentNullException.ThrowIfNull(intendedProject);
        ArgumentNullException.ThrowIfNull(existingProfile);
        ArgumentNullException.ThrowIfNull(candidate);

        ValidateMetadata(existingProfile, candidate);
        stateService.ValidateProjectStates(intendedProject);

        ModificationSnapshotModel currentDelta =
            snapshotService.CreateSnapshot(
                intendedProject,
                candidate.Snapshot.EditorVersion);
        IReadOnlyList<ProfileOperationRequestModel> currentRequests =
            operationCaptureService.Capture(intendedProject, currentDelta);

        ValidateSnapshotMetadata(intendedProject, currentDelta, candidate.Snapshot);
        ValidateOperationStates(intendedProject, currentDelta, candidate.Snapshot);
        ValidateOperationRequests(currentRequests, candidate.OperationRequests);
        ValidateProperties(
            intendedProject,
            existingProfile.Snapshot,
            currentDelta,
            candidate.Snapshot);
    }

    private static void ValidateMetadata(
        ModProfileModel existingProfile,
        ModProfileModel candidate)
    {
        ModProfileMetadataModel previous = existingProfile.Metadata
            ?? throw new InvalidOperationException(
                "The selected profile does not contain metadata.");
        ModProfileMetadataModel current = candidate.Metadata
            ?? throw new InvalidOperationException(
                "The updated profile does not contain metadata.");

        if (candidate.FormatVersion != ModProfileFormat.CurrentVersion ||
            !string.Equals(current.Name, previous.Name, StringComparison.Ordinal) ||
            !string.Equals(current.Description, previous.Description, StringComparison.Ordinal) ||
            !string.Equals(current.Author, previous.Author, StringComparison.Ordinal) ||
            !string.Equals(current.ProfileVersion, previous.ProfileVersion, StringComparison.Ordinal) ||
            current.CreatedAtUtc != previous.CreatedAtUtc ||
            !current.Tags.SequenceEqual(previous.Tags, StringComparer.Ordinal) ||
            current.ModifiedAtUtc < current.CreatedAtUtc)
        {
            throw new InvalidOperationException(
                "The updated profile does not preserve the selected profile identity metadata.");
        }
    }

    private static void ValidateSnapshotMetadata(
        ProjectModel intendedProject,
        ModificationSnapshotModel currentDelta,
        ModificationSnapshotModel candidate)
    {
        string expectedSourceFile = string.IsNullOrWhiteSpace(intendedProject.FileName)
            ? string.Empty
            : Path.GetFileName(intendedProject.FileName);

        if (candidate.FormatVersion != currentDelta.FormatVersion ||
            !string.Equals(candidate.SourceFileName, expectedSourceFile, StringComparison.Ordinal) ||
            !string.Equals(candidate.EditorVersion, currentDelta.EditorVersion, StringComparison.Ordinal) ||
            !string.Equals(candidate.GameVersion, currentDelta.GameVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The updated profile snapshot metadata does not match the current project.");
        }
    }

    private void ValidateOperationStates(
        ProjectModel intendedProject,
        ModificationSnapshotModel currentDelta,
        ModificationSnapshotModel candidate)
    {
        Dictionary<ProgressionType, GameplayOperationStateModel> expected =
            ToStateMap(currentDelta.GameplayOperationStates, "current project");
        Dictionary<ProgressionType, GameplayOperationStateModel> actual =
            ToStateMap(candidate.GameplayOperationStates, "updated profile");

        foreach (GameplayOperationStateModel state in actual.Values)
        {
            GameplayOperationStateModel validated = state.DeepClone();
            stateService.ValidateState(intendedProject, validated);
            if (!validated.IsCompatible)
            {
                throw new InvalidOperationException(
                    $"The updated profile contains incompatible gameplay state " +
                    $"for '{state.OperationType}': {validated.CompatibilityMessage}");
            }
        }

        if (expected.Count != actual.Count ||
            expected.Any(pair =>
                !actual.TryGetValue(pair.Key, out GameplayOperationStateModel? state) ||
                !JToken.DeepEquals(
                    JToken.FromObject(pair.Value),
                    JToken.FromObject(state))))
        {
            throw new InvalidOperationException(
                "The updated profile gameplay state does not match the current project.");
        }
    }

    private static Dictionary<ProgressionType, GameplayOperationStateModel> ToStateMap(
        IEnumerable<GameplayOperationStateModel> states,
        string sourceName)
    {
        Dictionary<ProgressionType, GameplayOperationStateModel> result = new();
        foreach (GameplayOperationStateModel state in states)
        {
            if (!result.TryAdd(state.OperationType, state))
            {
                throw new InvalidOperationException(
                    $"The {sourceName} contains duplicate gameplay state for " +
                    $"'{state.OperationType}'.");
            }
        }

        return result;
    }

    private static void ValidateOperationRequests(
        IReadOnlyList<ProfileOperationRequestModel> expected,
        IReadOnlyList<ProfileOperationRequestModel> actual)
    {
        JArray expectedToken = new(expected
            .OrderBy(request => request.OperationId, StringComparer.Ordinal)
            .Select(JToken.FromObject));
        JArray actualToken = new(actual
            .OrderBy(request => request.OperationId, StringComparer.Ordinal)
            .Select(JToken.FromObject));

        if (!JToken.DeepEquals(expectedToken, actualToken))
        {
            throw new InvalidOperationException(
                "The updated profile gameplay-tool requests do not match the current project.");
        }
    }

    private void ValidateProperties(
        ProjectModel project,
        ModificationSnapshotModel previousSnapshot,
        ModificationSnapshotModel currentDelta,
        ModificationSnapshotModel candidateSnapshot)
    {
        Dictionary<string, ModificationSnapshotPropertyModel> candidate =
            CreateCandidatePropertyMap(candidateSnapshot);
        Dictionary<string, ModificationSnapshotPropertyModel> delta =
            CreateCanonicalPropertyMap(currentDelta, "current project delta");
        HashSet<string> expectedIdentities = new(StringComparer.Ordinal);
        HashSet<string> previousIdentities = new(StringComparer.Ordinal);

        foreach ((ModificationSnapshotCategoryModel category,
                  ModificationSnapshotSettingModel setting,
                  ModificationSnapshotPropertyModel property) in
                 EnumerateProperties(previousSnapshot))
        {
            EntryModel? liveEntry = FindEntry(project, category.Name, setting.Id);
            SnapshotPropertyResolutionResult resolution = liveEntry == null
                ? new SnapshotPropertyResolutionResult(
                    SnapshotPropertyResolutionStatus.NotFound,
                    Array.Empty<PropertyModel>())
                : resolutionService.Resolve(liveEntry, property);

            if (resolution.Status == SnapshotPropertyResolutionStatus.Ambiguous)
            {
                throw new InvalidOperationException(
                    $"Legacy profile property '{GetIdentity(property)}' in " +
                    $"'{category.Name}/{setting.Id}' is ambiguous.");
            }

            PropertyModel? liveProperty = resolution.Property;
            if (liveProperty == null)
            {
                bool? existed =
                    SnapshotPropertyHistoryService
                        .GetOriginalPropertyExistence(property);
                if (existed != false)
                {
                    throw new InvalidOperationException(
                        $"Profile property '{GetIdentity(property)}' in " +
                        $"'{category.Name}/{setting.Id}' is missing without " +
                        "authoritative historical-absence evidence.");
                }

                continue;
            }

            string identity = CreateIdentity(
                category.Name,
                setting.Id,
                liveProperty.EffectivePropertyPath);
            if (!previousIdentities.Add(identity))
            {
                throw new InvalidOperationException(
                    $"The selected profile contains duplicate property identity '{identity}'.");
            }

            JToken liveValue = liveProperty.GetCurrentValueSnapshot();
            if (JToken.DeepEquals(liveValue, property.OriginalValue))
                continue;

            expectedIdentities.Add(identity);
            ModificationSnapshotPropertyModel candidateProperty =
                RequireCandidate(candidate, identity);
            if (!JToken.DeepEquals(candidateProperty.OriginalValue, property.OriginalValue) ||
                !JToken.DeepEquals(candidateProperty.CurrentValue, liveValue) ||
                candidateProperty.OriginalPropertyExisted !=
                    SnapshotPropertyHistoryService
                        .GetOriginalPropertyExistence(property))
            {
                throw new InvalidOperationException(
                    $"Updated profile property '{identity}' does not preserve its " +
                    "historical original and newest live value.");
            }
        }

        foreach ((ModificationSnapshotCategoryModel category,
                  ModificationSnapshotSettingModel setting,
                  ModificationSnapshotPropertyModel property) in
                 EnumerateProperties(currentDelta))
        {
            string identity = CreateIdentity(
                category.Name,
                setting.Id,
                property.PropertyPath);
            if (previousIdentities.Contains(identity))
                continue;

            expectedIdentities.Add(identity);
            ModificationSnapshotPropertyModel candidateProperty =
                RequireCandidate(candidate, identity);
            if (!JToken.DeepEquals(candidateProperty.OriginalValue, property.OriginalValue) ||
                !JToken.DeepEquals(candidateProperty.CurrentValue, property.CurrentValue) ||
                candidateProperty.OriginalPropertyExisted != property.OriginalPropertyExisted)
            {
                throw new InvalidOperationException(
                    $"Updated profile property '{identity}' does not match the " +
                    "current project delta.");
            }
        }

        if (!candidate.Keys.ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedIdentities))
        {
            throw new InvalidOperationException(
                "The updated profile contains missing or unexpected property records.");
        }
    }

    private static Dictionary<string, ModificationSnapshotPropertyModel>
        CreateCandidatePropertyMap(ModificationSnapshotModel snapshot)
    {
        return CreateCanonicalPropertyMap(snapshot, "updated profile");
    }

    private static Dictionary<string, ModificationSnapshotPropertyModel>
        CreateCanonicalPropertyMap(
            ModificationSnapshotModel snapshot,
            string sourceName)
    {
        Dictionary<string, ModificationSnapshotPropertyModel> result =
            new(StringComparer.Ordinal);
        HashSet<string> categories = new(StringComparer.Ordinal);

        foreach (ModificationSnapshotCategoryModel category in snapshot.Categories)
        {
            if (string.IsNullOrWhiteSpace(category.Name) ||
                !categories.Add(category.Name))
            {
                throw new InvalidOperationException(
                    $"The {sourceName} contains a missing or duplicate category identity.");
            }

            HashSet<string> settings = new(StringComparer.Ordinal);
            foreach (ModificationSnapshotSettingModel setting in category.Settings)
            {
                if (string.IsNullOrWhiteSpace(setting.Id) ||
                    !settings.Add(setting.Id))
                {
                    throw new InvalidOperationException(
                        $"The {sourceName} contains a missing or duplicate entry identity.");
                }

                foreach (ModificationSnapshotPropertyModel property in setting.Properties)
                {
                    if (string.IsNullOrWhiteSpace(property.PropertyPath))
                    {
                        throw new InvalidOperationException(
                            $"The {sourceName} contains a non-canonical property path.");
                    }

                    string identity = CreateIdentity(
                        category.Name,
                        setting.Id,
                        property.PropertyPath);
                    if (!result.TryAdd(identity, property))
                    {
                        throw new InvalidOperationException(
                            $"The {sourceName} contains duplicate property identity '{identity}'.");
                    }
                }
            }
        }

        return result;
    }

    private static ModificationSnapshotPropertyModel RequireCandidate(
        IReadOnlyDictionary<string, ModificationSnapshotPropertyModel> candidate,
        string identity)
    {
        if (!candidate.TryGetValue(
                identity,
                out ModificationSnapshotPropertyModel? property))
        {
            throw new InvalidOperationException(
                $"The updated profile is missing required property '{identity}'.");
        }

        return property;
    }

    private static IEnumerable<(
        ModificationSnapshotCategoryModel Category,
        ModificationSnapshotSettingModel Setting,
        ModificationSnapshotPropertyModel Property)> EnumerateProperties(
        ModificationSnapshotModel snapshot)
    {
        return snapshot.Categories.SelectMany(category =>
            category.Settings.SelectMany(setting =>
                setting.Properties.Select(property =>
                    (category, setting, property))));
    }

    private static EntryModel? FindEntry(
        ProjectModel project,
        string categoryName,
        string entryId)
    {
        return project.Sheets
            .SingleOrDefault(sheet =>
                string.Equals(sheet.Name, categoryName, StringComparison.Ordinal))
            ?.Entries.SingleOrDefault(entry =>
                string.Equals(entry.Id, entryId, StringComparison.Ordinal));
    }

    private static string GetIdentity(
        ModificationSnapshotPropertyModel property) =>
        string.IsNullOrWhiteSpace(property.PropertyPath)
            ? property.Name
            : property.PropertyPath;

    private static string CreateIdentity(
        string categoryName,
        string settingId,
        string propertyPath) =>
        $"{categoryName}\u001f{settingId}\u001f{propertyPath}";
}
