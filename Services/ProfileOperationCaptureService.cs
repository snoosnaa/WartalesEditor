using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ProfileOperationCaptureService
{
    private const int UpgradeableEquipmentFlag = 128;

    private readonly IOperationValidatorProvider validatorProvider;
    private readonly AddCampFacilitiesOperation addCampOperation;
    private readonly UpgradeAllEquipmentOperation upgradeOperation;
    private readonly CampFacilityJsonBuilder campBuilder;
    private readonly ProfileOperationIntentRegistry intentRegistry;
    private readonly ProfileOperationReplayService replayService;
    private readonly GameplayOperationStateService stateService;
    private readonly CdbGenerationIdentityService identityService = new();

    public static ProfileOperationCaptureService CreateDefault()
    {
        ProjectMutationService mutationService = new();
        ContentCreationService contentCreationService = new(mutationService);

        return new ProfileOperationCaptureService(
            new OperationValidatorProvider(),
            new AddCampFacilitiesOperation(contentCreationService),
            new UpgradeAllEquipmentOperation(contentCreationService));
    }

    public ProfileOperationCaptureService(
        IOperationValidatorProvider validatorProvider,
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation)
        : this(
            validatorProvider,
            addCampOperation,
            upgradeOperation,
            new CampFacilityJsonBuilder(),
            new ProfileOperationIntentRegistry())
    {
    }

    public ProfileOperationCaptureService(
        IOperationValidatorProvider validatorProvider,
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation,
        LocalizationService localizationService)
        : this(
            validatorProvider,
            addCampOperation,
            upgradeOperation,
            new CampFacilityJsonBuilder(),
            new ProfileOperationIntentRegistry(),
            localizationService)
    {
    }

    public ProfileOperationCaptureService(
        IOperationValidatorProvider validatorProvider,
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation,
        CampFacilityJsonBuilder campBuilder)
        : this(
            validatorProvider,
            addCampOperation,
            upgradeOperation,
            campBuilder,
            new ProfileOperationIntentRegistry(),
            new LocalizationService())
    {
    }

    public ProfileOperationCaptureService(
        IOperationValidatorProvider validatorProvider,
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation,
        CampFacilityJsonBuilder campBuilder,
        ProfileOperationIntentRegistry intentRegistry)
        : this(
            validatorProvider,
            addCampOperation,
            upgradeOperation,
            campBuilder,
            intentRegistry,
            new LocalizationService())
    {
    }

    public ProfileOperationCaptureService(
        IOperationValidatorProvider validatorProvider,
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation,
        CampFacilityJsonBuilder campBuilder,
        ProfileOperationIntentRegistry intentRegistry,
        LocalizationService localizationService)
    {
        this.validatorProvider = validatorProvider
            ?? throw new ArgumentNullException(
                nameof(validatorProvider));
        this.addCampOperation = addCampOperation
            ?? throw new ArgumentNullException(
                nameof(addCampOperation));
        this.upgradeOperation = upgradeOperation
            ?? throw new ArgumentNullException(
                nameof(upgradeOperation));
        this.campBuilder = campBuilder
            ?? throw new ArgumentNullException(
                nameof(campBuilder));
        this.intentRegistry = intentRegistry
            ?? throw new ArgumentNullException(
                nameof(intentRegistry));
        ArgumentNullException.ThrowIfNull(localizationService);
        replayService = new ProfileOperationReplayService(localizationService);
        stateService = new GameplayOperationStateService(
            new ProjectMutationService(), localizationService);
    }

    public IReadOnlyList<ProfileOperationRequestModel> Capture(
        ProjectModel project,
        ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(snapshot);

        List<ProfileOperationRequestModel> requests = new();

        if (IsApplied(addCampOperation, project))
        {
            requests.Add(
                CreateRequest(
                    ProfileOperationIds.AddCampFacilities));
            FilterAddCampProperties(project, snapshot);
        }

        if (IsApplied(upgradeOperation, project))
        {
            requests.Add(
                CreateRequest(
                    ProfileOperationIds.UpgradeAllEquipment));
            FilterUpgradeProperties(snapshot);
        }

        CaptureStatefulIntents(
            project,
            snapshot,
            requests);

        RemoveEmptySnapshotContainers(snapshot);
        return requests
            .OrderBy(
                request => request.OperationId,
                StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<ProfileOperationRequestModel>
        ReconcileForUpdate(
            ProjectModel project,
            ModProfileModel existingProfile,
            IReadOnlyList<ProfileOperationRequestModel> currentRequests)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(existingProfile);
        ArgumentNullException.ThrowIfNull(currentRequests);

        Dictionary<string, ProfileOperationRequestModel> reconciled =
            GetExistingRequests(existingProfile);
        Dictionary<string, ProfileOperationRequestModel> current =
            currentRequests.ToDictionary(
                request => request.OperationId,
                CloneRequest,
                StringComparer.Ordinal);
        HashSet<string> observedStateful =
            GetAuthoritativeStateOperationIds(project);

        foreach (string operationId in observedStateful)
        {
            if (!current.ContainsKey(operationId))
            {
                reconciled.Remove(operationId);
            }
        }

        foreach ((string operationId, ProfileOperationRequestModel request)
                 in current)
        {
            reconciled[operationId] = request;
        }

        return reconciled.Values
            .OrderBy(request => request.OperationId, StringComparer.Ordinal)
            .ToArray();
    }

    internal void FilterOwnedLeavesForUpdate(
        ProjectModel project,
        ModificationSnapshotModel snapshot,
        IReadOnlyList<ProfileOperationRequestModel> requests)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(requests);

        foreach (GameplayOperationStateModel state in
                 GetAuthoritativeStates(project))
        {
            RemoveOwnedLeaves(
                snapshot,
                replayService.GetOwnedSnapshotLeaves(project, state));
        }

        foreach (ProfileOperationRequestModel request in requests)
        {
            ProgressionType? type = intentRegistry.GetOperationType(
                request.OperationId);
            if (type != null)
            {
                RemoveOwnedLeaves(
                    snapshot,
                    replayService.GetOwnedSnapshotLeaves(project, request));
            }
        }

        if (requests.Any(request => request.OperationId ==
                ProfileOperationIds.AddCampFacilities))
        {
            FilterAddCampProperties(project, snapshot);
        }

        if (requests.Any(request => request.OperationId ==
                ProfileOperationIds.UpgradeAllEquipment))
        {
            FilterUpgradeProperties(snapshot);
        }

        RemoveEmptySnapshotContainers(snapshot);
    }

    internal void ValidateNoUnresolvedOwnedLeafConflicts(
        ProjectModel project,
        ModificationSnapshotModel currentDelta,
        IReadOnlyList<ProfileOperationRequestModel> reconciledRequests)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(currentDelta);
        ArgumentNullException.ThrowIfNull(reconciledRequests);

        HashSet<string> authoritativeOperationIds =
            GetAuthoritativeStateOperationIds(project);

        foreach (ProfileOperationRequestModel request in
                 reconciledRequests)
        {
            ProgressionType? type = intentRegistry.GetOperationType(
                request.OperationId);
            if (type == null ||
                authoritativeOperationIds.Contains(request.OperationId))
            {
                continue;
            }

            HashSet<ProfileOwnedSnapshotLeaf> owned = replayService
                .GetOwnedSnapshotLeaves(project, request)
                .ToHashSet();
            ProfileOwnedSnapshotLeaf[] conflicts = currentDelta.Categories
                .SelectMany(category => category.Settings.SelectMany(setting =>
                    setting.Properties.Select(property =>
                        new ProfileOwnedSnapshotLeaf(
                            category.Name,
                            setting.Id,
                            GetPropertyIdentity(property)))))
                .Where(owned.Contains)
                .OrderBy(leaf => leaf.SheetName, StringComparer.Ordinal)
                .ThenBy(leaf => leaf.EntryId, StringComparer.Ordinal)
                .ThenBy(leaf => leaf.PropertyPath, StringComparer.Ordinal)
                .ToArray();

            if (conflicts.Length == 0)
            {
                continue;
            }

            string featureName = replayService.GetDisplayName(request);
            throw new InvalidOperationException(
                $"{featureName} cannot be reconciled because current " +
                "direct edits overlap values controlled by that " +
                $"gameplay setting. Open {featureName} and apply the intended " +
                "setting, restore its previous values, or undo the " +
                "direct edits before updating this profile.");
        }
    }

    internal bool IsOwnedLeafForUpdate(
        ProjectModel project,
        string sheetName,
        string entryId,
        ModificationSnapshotPropertyModel property,
        IReadOnlyList<ProfileOperationRequestModel> requests)
    {
        ArgumentNullException.ThrowIfNull(property);

        ModificationSnapshotModel probe = new()
        {
            Categories = new()
            {
                new ModificationSnapshotCategoryModel
                {
                    Name = sheetName,
                    Settings = new()
                    {
                        new ModificationSnapshotSettingModel
                        {
                            Id = entryId,
                            Properties = new()
                            {
                                new ModificationSnapshotPropertyModel
                                {
                                    Name = property.Name,
                                    PropertyPath = property.PropertyPath,
                                    OriginalPropertyExisted =
                                        property.OriginalPropertyExisted,
                                    OriginalValue =
                                        property.OriginalValue.DeepClone(),
                                    CurrentValue =
                                        property.CurrentValue.DeepClone()
                                }
                            }
                        }
                    }
                }
            }
        };

        FilterOwnedLeavesForUpdate(project, probe, requests);
        return probe.Categories.Count == 0;
    }

    private void CaptureStatefulIntents(
        ProjectModel project,
        ModificationSnapshotModel snapshot,
        ICollection<ProfileOperationRequestModel> requests)
    {
        HashSet<string> operationIds = requests
            .Select(request => request.OperationId)
            .ToHashSet(StringComparer.Ordinal);

        snapshot.GameplayOperationStates.Clear();

        foreach (GameplayOperationStateModel state in
                 GetAuthoritativeStates(project)
                     .OrderBy(state => state.OperationType))
        {
            RemoveOwnedLeaves(
                snapshot,
                replayService.GetOwnedSnapshotLeaves(project, state));

            if (!IsEffectiveState(state))
            {
                continue;
            }

            if (!intentRegistry.TryProjectLegacyIntent(
                    state,
                    out ProfileOperationRequestModel? intent,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }

            if (intent == null)
            {
                throw new InvalidOperationException(
                    $"Gameplay state '{stateService.GetDisplayName(state.OperationType)}' represents " +
                    "an effective outcome but has no canonical profile intent.");
            }

            if (!operationIds.Add(intent.OperationId))
            {
                throw new InvalidOperationException(
                    $"The current project contains more than one " +
                    $"profile intent for '{replayService.GetDisplayName(intent)}'.");
            }

            requests.Add(intent);

            if (CanRetainExactSourceState(project, state))
            {
                GameplayOperationStateModel retained = state.DeepClone();
                retained.LocalRestoreContentIdentity = string.Empty;
                snapshot.GameplayOperationStates.Add(retained);
            }
        }
    }

    private Dictionary<string, ProfileOperationRequestModel>
        GetExistingRequests(ModProfileModel profile)
    {
        Dictionary<string, ProfileOperationRequestModel> requests =
            new(StringComparer.Ordinal);

        foreach (ProfileOperationRequestModel request in
                 profile.OperationRequests)
        {
            intentRegistry.ValidateRequest(request, profile.FormatVersion);
            if (!requests.TryAdd(request.OperationId, CloneRequest(request)))
            {
                throw new InvalidOperationException(
                    $"The selected profile contains more than one request " +
                    $"for '{replayService.GetDisplayName(request)}'.");
            }
        }

        if (profile.FormatVersion >=
            ModProfileFormat.ProfileOperationIntentVersion)
        {
            return requests;
        }

        foreach (GameplayOperationStateModel state in
                 profile.Snapshot.GameplayOperationStates)
        {
            if (!intentRegistry.TryProjectLegacyIntent(
                    state,
                    out ProfileOperationRequestModel? projected,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }

            if (projected == null)
            {
                continue;
            }

            if (requests.TryGetValue(
                    projected.OperationId,
                    out ProfileOperationRequestModel? existing))
            {
                if (!JToken.DeepEquals(existing.Settings, projected.Settings))
                {
                    throw new InvalidOperationException(
                        $"Legacy profile intent '{projected.OperationId}' " +
                        "is ambiguous.");
                }

                continue;
            }

            requests.Add(projected.OperationId, CloneRequest(projected));
        }

        return requests;
    }

    private HashSet<string> GetAuthoritativeStateOperationIds(
        ProjectModel project) => GetAuthoritativeStates(project)
        .Select(state => intentRegistry.GetOperationId(state.OperationType))
        .ToHashSet(StringComparer.Ordinal);

    private IReadOnlyList<GameplayOperationStateModel>
        GetAuthoritativeStates(ProjectModel project)
    {
        List<GameplayOperationStateModel> states = new();

        foreach (GameplayOperationStateModel state in
                 project.GameplayOperationStates)
        {
            GameplayOperationStateModel validated = state.DeepClone();
            stateService.ValidateState(project, validated);
            if (!validated.IsCompatible ||
                !stateService.HasRestoreAuthority(project, state))
            {
                continue;
            }

            states.Add(validated);
        }

        return states;
    }

    private bool CanRetainExactSourceState(
        ProjectModel project,
        GameplayOperationStateModel state) =>
        project.SourceProvenanceStatus == SourceProvenanceStatus.Verified &&
        identityService.IsValid(project.SourceCdbGenerationIdentity) &&
        identityService.AreEqual(
            project.SourceCdbGenerationIdentity,
            state.ProjectCompatibilityIdentity);

    private static bool IsEffectiveState(
        GameplayOperationStateModel state) =>
        !string.Equals(
            state.BaselineFingerprint,
            state.ExpectedCurrentFingerprint,
            StringComparison.Ordinal);

    private static ProfileOperationRequestModel CloneRequest(
        ProfileOperationRequestModel request) => new()
    {
        FormatVersion = request.FormatVersion,
        OperationId = request.OperationId,
        Settings = (JObject?)request.Settings?.DeepClone()
    };

    private static void RemoveOwnedLeaves(
        ModificationSnapshotModel snapshot,
        IEnumerable<ProfileOwnedSnapshotLeaf> leaves)
    {
        HashSet<ProfileOwnedSnapshotLeaf> owned = leaves.ToHashSet();
        foreach (ModificationSnapshotCategoryModel category in
                 snapshot.Categories)
        {
            foreach (ModificationSnapshotSettingModel setting in
                     category.Settings)
            {
                setting.Properties.RemoveAll(property => owned.Contains(
                    new ProfileOwnedSnapshotLeaf(
                        category.Name,
                        setting.Id,
                        GetPropertyIdentity(property))));
            }
        }
    }

    private bool IsApplied(
        IProjectOperation operation,
        ProjectModel project)
    {
        OperationValidationResult result =
            validatorProvider.Validate(
                operation,
                project,
                new ProjectMutationResult());

        return result.IsValid;
    }

    private static ProfileOperationRequestModel CreateRequest(
        string operationId,
        JObject? settings = null)
    {
        return new ProfileOperationRequestModel
        {
            OperationId = operationId,
            Settings = settings
        };
    }

    private static void FilterRequestBoardRewards(
        ModificationSnapshotModel snapshot)
    {
        ModificationSnapshotCategoryModel? category =
            snapshot.Categories.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Name,
                    "constant",
                    StringComparison.Ordinal));
        if (category != null)
        {
            foreach (string entryId in new[]
                     {
                         RequestBoardRewardsService.MinimumEntryId,
                         RequestBoardRewardsService.MaximumEntryId
                     })
            {
                ModificationSnapshotSettingModel? setting =
                    category.Settings.FirstOrDefault(candidate =>
                        string.Equals(
                            candidate.Id,
                            entryId,
                            StringComparison.Ordinal));
                setting?.Properties.RemoveAll(property =>
                    string.Equals(
                        GetPropertyIdentity(property),
                        RequestBoardRewardsService.PropertyPath,
                        StringComparison.Ordinal));
            }
        }

        snapshot.GameplayOperationStates.RemoveAll(state =>
            state.OperationType ==
            ProgressionType.RequestBoardRewards);
    }

    private void FilterAddCampProperties(
        ProjectModel project,
        ModificationSnapshotModel snapshot)
    {
        SheetModel? itemSheet =
            project.Sheets.FirstOrDefault(sheet =>
                string.Equals(
                    sheet.Name,
                    "item",
                    StringComparison.Ordinal));

        if (itemSheet == null)
        {
            return;
        }

        FilterFacility(
            snapshot,
            itemSheet,
            "Anvil",
            campBuilder.BuildAnvilProps,
            campBuilder.BuildAnvilTool(),
            campBuilder.BuildAnvilIcon());

        FilterFacility(
            snapshot,
            itemSheet,
            "ApothecaryTable",
            campBuilder.BuildApothecaryProps,
            campBuilder.BuildApothecaryTool(),
            campBuilder.BuildApothecaryIcon());
    }

    private static void FilterFacility(
        ModificationSnapshotModel snapshot,
        SheetModel itemSheet,
        string entryId,
        Func<JObject, JObject> buildProps,
        JObject tool,
        JObject icon)
    {
        EntryModel? entry =
            itemSheet.Entries.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    entryId,
                    StringComparison.Ordinal));

        if (entry?.SourceEntry?["props"] is not JObject props)
        {
            return;
        }

        Dictionary<string, JToken> ownedValues =
            new(StringComparer.Ordinal);

        JObject builtProps =
            buildProps(props);

        AddNamedValues(
            builtProps,
            ownedValues,
            "props",
            "model",
            "activity",
            "hideInCheatMenu",
            "bonuses");
        AddLeafValues("tool", tool, ownedValues);
        AddLeafValues("icon", icon, ownedValues);

        ModificationSnapshotSettingModel? setting =
            FindSetting(snapshot, "item", entryId);

        setting?.Properties.RemoveAll(property =>
            ownedValues.TryGetValue(
                GetPropertyIdentity(property),
                out JToken? expectedValue)
            &&
            JToken.DeepEquals(
                property.CurrentValue,
                expectedValue));
    }

    private static void AddLeafValues(
        string parentPath,
        JObject source,
        IDictionary<string, JToken> values)
    {
        foreach (JProperty property in source.Properties())
        {
            if (property.Value is JObject nested)
            {
                AddLeafValues(
                    $"{parentPath}.{property.Name}",
                    nested,
                    values);
                continue;
            }

            values[$"{parentPath}.{property.Name}"] =
                property.Value.DeepClone();
        }
    }

    private static void AddNamedValues(
        JObject source,
        IDictionary<string, JToken> values,
        string parentPath,
        params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            JToken? value = source[propertyName];

            if (value != null)
            {
                values[$"{parentPath}.{propertyName}"] =
                    value.DeepClone();
            }
        }
    }

    private static void FilterUpgradeProperties(
        ModificationSnapshotModel snapshot)
    {
        ModificationSnapshotCategoryModel? itemCategory =
            snapshot.Categories.FirstOrDefault(category =>
                string.Equals(
                    category.Name,
                    "item",
                    StringComparison.Ordinal));

        if (itemCategory == null)
        {
            return;
        }

        foreach (ModificationSnapshotSettingModel setting in
                 itemCategory.Settings)
        {
            if (!UpgradeAllEquipmentTargetCatalog.Contains(
                    setting.Id))
            {
                continue;
            }

            setting.Properties.RemoveAll(property =>
                string.Equals(
                    GetPropertyIdentity(property),
                    "props.flags",
                    StringComparison.Ordinal)
                &&
                IsUpgradeOwnedFlagChange(property));
        }
    }

    private static bool IsUpgradeOwnedFlagChange(
        ModificationSnapshotPropertyModel property)
    {
        if (property.CurrentValue.Type != JTokenType.Integer)
        {
            return false;
        }

        int originalFlags =
            property.OriginalValue.Type == JTokenType.Integer
                ? property.OriginalValue.Value<int>()
                : 0;

        if (property.OriginalValue.Type is not
            (JTokenType.Integer or JTokenType.Null))
        {
            return false;
        }

        return property.CurrentValue.Value<int>() ==
               (originalFlags | UpgradeableEquipmentFlag);
    }

    internal static bool TryCreatePostUpgradeOriginalValue(
        ModificationSnapshotPropertyModel property,
        out JToken adjustedOriginal)
    {
        ArgumentNullException.ThrowIfNull(property);
        adjustedOriginal = property.OriginalValue.DeepClone();

        if (property.CurrentValue.Type != JTokenType.Integer ||
            property.OriginalValue.Type is not
                (JTokenType.Integer or JTokenType.Null))
        {
            return false;
        }

        int currentFlags = property.CurrentValue.Value<int>();
        if ((currentFlags & UpgradeableEquipmentFlag) == 0)
        {
            return false;
        }

        int originalFlags =
            property.OriginalValue.Type == JTokenType.Integer
                ? property.OriginalValue.Value<int>()
                : 0;
        adjustedOriginal = new JValue(
            originalFlags | UpgradeableEquipmentFlag);
        return true;
    }

    private static string GetPropertyIdentity(
        ModificationSnapshotPropertyModel property) =>
        string.IsNullOrWhiteSpace(property.PropertyPath)
            ? property.Name
            : property.PropertyPath;

    private static ModificationSnapshotSettingModel? FindSetting(
        ModificationSnapshotModel snapshot,
        string sheetName,
        string entryId)
    {
        return snapshot.Categories
            .FirstOrDefault(category =>
                string.Equals(
                    category.Name,
                    sheetName,
                    StringComparison.Ordinal))
            ?.Settings
            .FirstOrDefault(setting =>
                string.Equals(
                    setting.Id,
                    entryId,
                    StringComparison.Ordinal));
    }

    private static void RemoveEmptySnapshotContainers(
        ModificationSnapshotModel snapshot)
    {
        foreach (ModificationSnapshotCategoryModel category in
                 snapshot.Categories)
        {
            category.Settings.RemoveAll(setting =>
                setting.Properties.Count == 0);
        }

        snapshot.Categories.RemoveAll(category =>
            category.Settings.Count == 0);
    }
}
