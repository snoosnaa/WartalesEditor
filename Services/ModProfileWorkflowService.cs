using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Models.Operations;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ModProfileWorkflowService
{
    private readonly ModProfileService
        profileService;

    private readonly ModProfileSerializationService
        serializationService;

    private readonly ModificationSnapshotWorkflowService
        snapshotWorkflowService;

    private readonly ProfileOperationResolver
        operationResolver;

    private readonly ProjectOperationService
        projectOperationService;

    private readonly ProjectOperationTransactionService
        transactionService;

    private readonly ProfileOperationReplayService
        replayService;

    private readonly ProfileOperationIntentRegistry
        intentRegistry = new();

    private readonly CdbGenerationIdentityService
        identityService = new();

    private readonly ProfileEffectiveChangeCountService
        effectiveChangeCountService =
            new();

    private readonly UpdatedProfileCandidateValidationService
        updatedProfileCandidateValidationService;

    private readonly ProfileImpactEvaluationService
        impactEvaluationService;

    private readonly ProfileImpactManifestValidationService
        impactManifestValidationService = new();

    private readonly ProfileGameplayContentIdentityService
        gameplayContentIdentityService = new();

    public ModProfileWorkflowService()
        : this(
            new ModProfileService(),
            new ModProfileSerializationService(),
            new ModificationSnapshotWorkflowService(),
            CreateDefaultResolver(),
            new ProjectOperationService(),
            new ProjectOperationTransactionService())
    {
    }

    public ModProfileWorkflowService(
        ModProfileService profileService,
        ModProfileSerializationService
            serializationService,
        ModificationSnapshotWorkflowService
            snapshotWorkflowService)
        : this(
            profileService,
            serializationService,
            snapshotWorkflowService,
            CreateDefaultResolver(),
            new ProjectOperationService(),
            new ProjectOperationTransactionService())
    {
    }

    public ModProfileWorkflowService(
        ModProfileService profileService,
        ModProfileSerializationService
            serializationService,
        ModificationSnapshotWorkflowService
            snapshotWorkflowService,
        ProfileOperationResolver operationResolver,
        ProjectOperationService projectOperationService,
        ProjectOperationTransactionService transactionService)
        : this(
            profileService,
            serializationService,
            snapshotWorkflowService,
            operationResolver,
            projectOperationService,
            transactionService,
            new LocalizationService())
    {
    }

    public ModProfileWorkflowService(
        ModProfileService profileService,
        ModProfileSerializationService serializationService,
        ModificationSnapshotWorkflowService snapshotWorkflowService,
        ProfileOperationResolver operationResolver,
        ProjectOperationService projectOperationService,
        ProjectOperationTransactionService transactionService,
        LocalizationService localizationService)
        : this(
            profileService,
            serializationService,
            snapshotWorkflowService,
            operationResolver,
            projectOperationService,
            transactionService,
            localizationService,
            ProfileImpactBaselineResolver.CreateDefault())
    {
    }

    public ModProfileWorkflowService(
        ModProfileService profileService,
        ModProfileSerializationService serializationService,
        ModificationSnapshotWorkflowService snapshotWorkflowService,
        ProfileOperationResolver operationResolver,
        ProjectOperationService projectOperationService,
        ProjectOperationTransactionService transactionService,
        LocalizationService localizationService,
        ProfileImpactBaselineResolver impactBaselineResolver)
    {
        this.profileService =
            profileService
            ?? throw new ArgumentNullException(
                nameof(profileService));

        this.serializationService =
            serializationService
            ?? throw new ArgumentNullException(
                nameof(serializationService));

        this.snapshotWorkflowService =
            snapshotWorkflowService
            ?? throw new ArgumentNullException(
                nameof(snapshotWorkflowService));

        replayService = new ProfileOperationReplayService(
            localizationService ?? throw new ArgumentNullException(
                nameof(localizationService)));
        updatedProfileCandidateValidationService =
            new UpdatedProfileCandidateValidationService(localizationService);

        this.operationResolver =
            operationResolver
            ?? throw new ArgumentNullException(
                nameof(operationResolver));

        this.projectOperationService =
            projectOperationService
            ?? throw new ArgumentNullException(
                nameof(projectOperationService));

        this.transactionService =
            transactionService
            ?? throw new ArgumentNullException(
                nameof(transactionService));

        impactEvaluationService = new ProfileImpactEvaluationService(
            impactBaselineResolver ?? throw new ArgumentNullException(
                nameof(impactBaselineResolver)),
            new JsonDataService(),
            gameplayContentIdentityService,
            impactManifestValidationService,
            ApplyProfile);
    }

    public ModProfileModel CreateProfile(
        ProjectModel project,
        string profileName,
        string description = "",
        string author = "",
        string profileVersion = "1.0",
        string editorVersion = "")
    {
        ModProfileModel profile = profileService.CreateProfile(
            project,
            profileName,
            description,
            author,
            profileVersion,
            editorVersion);

        profile.ImpactManifest = impactEvaluationService.TryEstablish(
            project,
            profile,
            editorVersion,
            out _);
        return profile;
    }

    public ModProfileModel CreateUpdatedProfile(
        ProjectModel project,
        ModProfileModel existingProfile,
        string editorVersion = "")
    {
        ModProfileModel candidate = profileService.CreateUpdatedProfile(
            project,
            existingProfile,
            editorVersion);

        if (CanPreserveImpactManifest(existingProfile, candidate))
        {
            candidate.ImpactManifest =
                existingProfile.ImpactManifest!.DeepClone();
        }
        else
        {
            candidate.ImpactManifest = impactEvaluationService.TryEstablish(
                project,
                candidate,
                editorVersion,
                out _);
        }

        return candidate;
    }

    private bool CanPreserveImpactManifest(
        ModProfileModel existingProfile,
        ModProfileModel candidate)
    {
        if (!impactManifestValidationService.TryValidate(
                existingProfile,
                existingProfile.ImpactManifest,
                out _))
        {
            return false;
        }

        ProfileImpactManifestModel manifest = existingProfile.ImpactManifest!;
        return identityService.AreEqual(
                   manifest.SourceCdbGenerationIdentity,
                   candidate.SourceCdbGenerationIdentity) &&
               identityService.AreEqual(
                   manifest.ProfileGameplayContentIdentity,
                   gameplayContentIdentityService.Calculate(candidate));
    }

    public void ValidateUpdatedProfileCandidate(
        ProjectModel intendedProject,
        ModProfileModel candidate)
    {
        ArgumentNullException.ThrowIfNull(intendedProject);
        ArgumentNullException.ThrowIfNull(candidate);

        throw new InvalidOperationException(
            "Update Profile validation requires the selected existing " +
            "profile as reconciliation input.");
    }

    public void ValidateUpdatedProfileCandidate(
        ProjectModel intendedProject,
        ModProfileModel existingProfile,
        ModProfileModel candidate)
    {
        ArgumentNullException.ThrowIfNull(intendedProject);
        ArgumentNullException.ThrowIfNull(existingProfile);
        ArgumentNullException.ThrowIfNull(candidate);

        updatedProfileCandidateValidationService.Validate(
            intendedProject,
            existingProfile,
            candidate);
    }

    public void Save(
        ModProfileModel profile,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(profile);

        serializationService.Save(
            profile,
            fileName);
    }

    public ModProfileModel Load(
        string fileName)
    {
        return serializationService.Load(
            fileName);
    }

    public ModificationSnapshotImportResultModel
        ApplyProfile(
            ProjectModel targetProject,
            ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            profile);

        _ = serializationService.Serialize(
            profile);

        ModificationSnapshotModel snapshot =
            profileService.GetSnapshot(
                profile);

        ProfileApplyPlan plan = BuildApplyPlan(
            targetProject,
            profile,
            snapshot);

        List<ProfileOperationApplyItemResultModel>
            operationResults =
                new();

        ProjectMutationResult mutationResult =
            new();

        try
        {
            foreach (ProfileOperationRequestModel request in
                     plan.StructuralRequests)
            {
                ExecuteAdditiveRequest(
                    targetProject,
                    request,
                    operationResults,
                    mutationResult);
            }

            foreach (PlannedStatefulRequest planned in
                     plan.StatefulRequests)
            {
                ProfileOperationReplayResult replay =
                    replayService.ReplayForProfile(
                    targetProject,
                    planned.Request,
                    planned.ExactSourceSeed,
                    planned.SourceContext);
                ProjectOperationResult result = replay.OperationResult;
                string displayName = replayService.GetDisplayName(
                    planned.Request);

                if (!result.Succeeded)
                {
                    if (result.RollbackIntegrityFailed)
                    {
                        throw new ProjectRollbackIntegrityException(
                            result.Message ??
                            "Profile evaluation rollback integrity failed.");
                    }

                    operationResults.Add(
                        new ProfileOperationApplyItemResultModel(
                            planned.Request.OperationId,
                            displayName,
                            ProfileOperationApplyStatus.Failed,
                            result.Message ??
                                "The gameplay setting could not be applied."));
                    throw new InvalidOperationException(
                        $"{displayName} could not be restored." +
                        Environment.NewLine + Environment.NewLine +
                        result.Message);
                }

                mutationResult.Merge(result.MutationResult);
                ProfileOperationApplyStatus status =
                    replay.AllRequestedTraitsUnavailable
                        ? ProfileOperationApplyStatus.Unavailable
                        : IsStateOnly(result.MutationResult) ||
                    !result.MutationResult.WasModified
                        ? ProfileOperationApplyStatus.AlreadyConfigured
                        : ProfileOperationApplyStatus.Applied;
                operationResults.Add(
                    new ProfileOperationApplyItemResultModel(
                        planned.Request.OperationId,
                        displayName,
                        status,
                        result.Message ?? string.Empty,
                        BuildAlreadyConfiguredSummary(
                            planned.Request,
                            displayName,
                            status,
                            result.MutationResult),
                        replay.HasUnavailableTraits));
            }

            foreach (ProfileOperationRequestModel request in
                     plan.RemainingAdditiveRequests)
            {
                ExecuteAdditiveRequest(
                    targetProject,
                    request,
                    operationResults,
                    mutationResult);
            }

            ModificationSnapshotImportResultModel
                snapshotResult =
                    snapshotWorkflowService.ApplySafely(
                        targetProject,
                        plan.Snapshot,
                        profile.Metadata.Name,
                        profile.SourceCdbGenerationIdentity,
                        profile.FormatVersion >=
                            ModProfileFormat.ProvenanceVersion);

            mutationResult.Merge(
                snapshotResult.MutationResult);

            if (snapshotResult.HasFailures)
            {
                throw new InvalidOperationException(
                    "The profile's property changes could not all " +
                    "be applied. No profile changes were kept.");
            }

            return new ModificationSnapshotImportResultModel(
                snapshotResult.Snapshot,
                snapshotResult.MatchResult,
                snapshotResult.PreviewResult,
                snapshotResult.ApplyResult,
                snapshotResult.FileName,
                operationResults,
                mutationResult,
                effectiveChangeCountService.Calculate(
                    mutationResult));
        }
        catch (Exception operationException)
        {
            if (mutationResult.WasModified)
            {
                try
                {
                    transactionService.Rollback(
                        mutationResult);
                }
                catch (Exception rollbackException)
                {
                    throw new ProjectRollbackIntegrityException(
                        "Profile evaluation failed and its temporary changes " +
                        "could not be fully rolled back.",
                        operationException,
                        rollbackException);
                }
            }

            throw;
        }
    }

    public ModificationSnapshotImportResultModel
        LoadAndApplyProfile(
            ProjectModel targetProject,
            string fileName)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ModProfileModel profile =
            Load(fileName);

        return ApplyProfile(
            targetProject,
            profile);
    }

    private static IReadOnlyList<
        ProfileOperationRequestModel> OrderRequests(
            IEnumerable<ProfileOperationRequestModel> requests)
    {
        return requests
            .OrderBy(request =>
                request.OperationId ==
                    ProfileOperationIds.AddCampFacilities
                    ? 0
                    : 1)
            .ToList();
    }

    private ProfileApplyPlan BuildApplyPlan(
        ProjectModel targetProject,
        ModProfileModel profile,
        ModificationSnapshotModel snapshot)
    {
        IReadOnlyList<ProfileOperationRequestModel> intents =
            ResolveApplyIntents(profile, snapshot);
        Dictionary<ProgressionType, GameplayOperationStateModel> statesByType =
            snapshot.GameplayOperationStates.ToDictionary(
                state => state.OperationType);
        bool sameSource =
            targetProject.SourceProvenanceStatus ==
                SourceProvenanceStatus.Verified &&
            identityService.IsValid(profile.SourceCdbGenerationIdentity) &&
            identityService.AreEqual(
                targetProject.SourceCdbGenerationIdentity,
                profile.SourceCdbGenerationIdentity);

        List<ProfileOperationRequestModel> structural = new();
        List<ProfileOperationRequestModel> remainingAdditive = new();
        List<PlannedStatefulRequest> stateful = new();
        Dictionary<ProfileOwnedSnapshotLeaf, string> ownedLeaves = new();
        HashSet<ProgressionType> replayedTypes = new();

        foreach (ProfileOperationRequestModel request in OrderRequests(intents))
        {
            ProgressionType? operationType =
                intentRegistry.GetOperationType(request.OperationId);
            if (operationType == null)
            {
                PreflightAdditive(targetProject, request);
                if (request.OperationId ==
                    ProfileOperationIds.AddCampFacilities)
                {
                    structural.Add(request);
                }
                else
                {
                    remainingAdditive.Add(request);
                }

                AddOwnedLeaves(targetProject, request, ownedLeaves);
                continue;
            }

            statesByType.TryGetValue(
                operationType.Value,
                out GameplayOperationStateModel? seed);
            if (seed == null)
            {
                GameplayOperationStateModel? targetState =
                    targetProject.GameplayOperationStates
                        .SingleOrDefault(state =>
                            state.OperationType == operationType.Value);
                if (targetState != null)
                {
                    try
                    {
                        intentRegistry.ValidateStateMatchesRequest(
                            targetState,
                            request);
                        seed = targetState.DeepClone();
                    }
                    catch (InvalidOperationException)
                    {
                        // A target state for different settings is not a
                        // replay baseline candidate for this intent.
                    }
                }
            }
            if (sameSource && seed != null &&
                !identityService.AreEqual(
                    seed.ProjectCompatibilityIdentity,
                    profile.SourceCdbGenerationIdentity))
            {
                string displayName = replayService.GetDisplayName(request);
                throw new InvalidOperationException(
                    $"The retained gameplay state for " +
                    $"'{displayName}' does not belong to the " +
                    "profile's source generation.");
            }

            GameplayOperationStateModel? acceptedSeed;
            try
            {
                acceptedSeed = replayService.Preflight(
                    targetProject,
                    request,
                    seed,
                    sameSource
                        ? ProfileReplaySourceContext.ExactSource
                        : ProfileReplaySourceContext.ChangedSource);
            }
            catch (Exception exception)
            {
                string displayName = replayService.GetDisplayName(request);
                throw new InvalidOperationException(
                    $"{displayName} " +
                    "is not compatible with the current project.",
                    exception);
            }
            AddOwnedLeaves(targetProject, request, ownedLeaves);
            replayedTypes.Add(operationType.Value);
            stateful.Add(new PlannedStatefulRequest(
                request,
                acceptedSeed,
                sameSource
                    ? ProfileReplaySourceContext.ExactSource
                    : ProfileReplaySourceContext.ChangedSource));
        }

        ModificationSnapshotModel applySnapshot = CreateApplySnapshot(
            snapshot,
            ownedLeaves,
            replayedTypes);

        _ = snapshotWorkflowService.Match(targetProject, applySnapshot);
        _ = snapshotWorkflowService.Preview(targetProject, applySnapshot);

        return new ProfileApplyPlan(
            structural,
            stateful,
            remainingAdditive,
            applySnapshot);
    }

    private IReadOnlyList<ProfileOperationRequestModel> ResolveApplyIntents(
        ModProfileModel profile,
        ModificationSnapshotModel snapshot)
    {
        Dictionary<string, ProfileOperationRequestModel> byId =
            profile.OperationRequests.ToDictionary(
                request => request.OperationId,
                StringComparer.Ordinal);

        if (profile.FormatVersion >=
            ModProfileFormat.ProfileOperationIntentVersion)
        {
            return byId.Values.ToArray();
        }

        foreach (GameplayOperationStateModel state in
                 snapshot.GameplayOperationStates)
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

            if (byId.TryGetValue(
                    projected.OperationId,
                    out ProfileOperationRequestModel? existing))
            {
                if (!JToken.DeepEquals(
                        existing.Settings,
                        projected.Settings))
                {
                    throw new InvalidOperationException(
                        $"Legacy profile intent " +
                        $"'{projected.OperationId}' is ambiguous.");
                }

                continue;
            }

            byId.Add(projected.OperationId, projected);
        }

        return byId.Values.ToArray();
    }

    private void AddOwnedLeaves(
        ProjectModel targetProject,
        ProfileOperationRequestModel request,
        IDictionary<ProfileOwnedSnapshotLeaf, string> ownedLeaves)
    {
        foreach (ProfileOwnedSnapshotLeaf leaf in
                 replayService.GetOwnedSnapshotLeaves(
                     targetProject,
                     request))
        {
            if (ownedLeaves.TryGetValue(
                    leaf,
                    out string? existingOperation) &&
                !string.Equals(
                    existingOperation,
                    request.OperationId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Profile operations '{existingOperation}' and " +
                    $"'{request.OperationId}' both own " +
                    $"'{leaf.SheetName}/{leaf.EntryId}/" +
                    $"{leaf.PropertyPath}'.");
            }

            ownedLeaves[leaf] = request.OperationId;
        }
    }

    private void PreflightAdditive(
        ProjectModel targetProject,
        ProfileOperationRequestModel request)
    {
        IProjectOperation operation = operationResolver.Resolve(request);
        if (!operation.CanExecute(targetProject))
        {
            throw new InvalidOperationException(
                $"The operation '{operation.Name}' cannot be executed " +
                "on the current project.");
        }

        if (operation is IContextualProjectOperation contextual)
        {
            contextual.Preflight(targetProject);
        }
    }

    private void ExecuteAdditiveRequest(
        ProjectModel targetProject,
        ProfileOperationRequestModel request,
        ICollection<ProfileOperationApplyItemResultModel> operationResults,
        ProjectMutationResult mutationResult)
    {
        IProjectOperation operation = operationResolver.Resolve(request);
        ProjectOperationResult result = projectOperationService.Execute(
            operation,
            targetProject);

        if (!result.Succeeded)
        {
            if (result.RollbackIntegrityFailed)
            {
                throw new ProjectRollbackIntegrityException(
                    result.Message ??
                    "Profile evaluation rollback integrity failed.");
            }

            operationResults.Add(
                new ProfileOperationApplyItemResultModel(
                    request.OperationId,
                    operation.Name,
                    ProfileOperationApplyStatus.Failed,
                    result.Message ??
                        "The gameplay tool could not be applied."));
            throw new InvalidOperationException(
                $"{operation.Name} could not be restored." +
                Environment.NewLine + Environment.NewLine +
                result.Message);
        }

        mutationResult.Merge(result.MutationResult);
        ProfileOperationApplyStatus status =
            result.MutationResult.WasModified
                ? ProfileOperationApplyStatus.Applied
                : ProfileOperationApplyStatus.AlreadyConfigured;
        operationResults.Add(
            new ProfileOperationApplyItemResultModel(
                request.OperationId,
                operation.Name,
                status,
                result.Message ?? string.Empty,
                BuildAlreadyConfiguredSummary(
                    request,
                    operation.Name,
                    status,
                    result.MutationResult)));
    }

    private string? BuildAlreadyConfiguredSummary(
        ProfileOperationRequestModel request,
        string displayName,
        ProfileOperationApplyStatus status,
        ProjectMutationResult mutationResult)
    {
        if (request.OperationId ==
            ProfileOperationIds.UpgradeAllEquipment)
        {
            int changedItems =
                new EffectiveChangeCountService()
                    .Calculate(mutationResult);
            int alreadyUpgradeable = Math.Max(
                0,
                UpgradeAllEquipmentTargetCatalog.Count - changedItems);
            return alreadyUpgradeable == 0
                ? null
                : $"Upgrade All Equipment: {alreadyUpgradeable:N0} eligible " +
                  $"{(alreadyUpgradeable == 1 ? "item was" : "items were")} " +
                  "already upgradeable.";
        }

        if (status != ProfileOperationApplyStatus.AlreadyConfigured)
        {
            return null;
        }

        if (request.OperationId ==
            ProfileOperationIds.AddCampFacilities)
        {
            return "Add Camp Facilities: the Anvil and Apothecary Table " +
                   "were already available.";
        }

        JObject settings = request.Settings ?? new JObject();
        if (request.OperationId is
            ProfileOperationIds.CharacterXp or
            ProfileOperationIds.ProfessionXp or
            ProfileOperationIds.RequestBoardRewards or
            ProfileOperationIds.PathLevelRequirements)
        {
            int percentage = settings.Value<int>("percentage");
            return $"{displayName}: already configured at {percentage}%.";
        }

        if (request.OperationId is
            ProfileOperationIds.PathXpRewardsMight or
            ProfileOperationIds.PathXpRewardsTrade or
            ProfileOperationIds.PathXpRewardsCrime or
            ProfileOperationIds.PathXpRewardsMystery)
        {
            int multiplier = settings.Value<int>("multiplier");
            string label = multiplier == 1 ? "Original" : $"{multiplier}×";
            return $"{displayName}: already configured at {label}.";
        }

        if (request.OperationId ==
            ProfileOperationIds.OverworldMovementSpeed)
        {
            string key = settings.Value<string>("preset") ?? string.Empty;
            string label = OverworldMovementSpeedService.Presets
                .FirstOrDefault(option => string.Equals(
                    option.Preset.ToString(),
                    key,
                    StringComparison.Ordinal))
                ?.Name ?? key;
            return $"Run Speed: already configured to {label}.";
        }

        if (request.OperationId == ProfileOperationIds.RainFrequency)
        {
            string key = settings.Value<string>("preset") ?? string.Empty;
            string label = RainFrequencyService.Presets
                .FirstOrDefault(option => string.Equals(
                    option.Preset.ToString(),
                    key,
                    StringComparison.Ordinal))
                ?.Name ?? key;
            return $"{displayName}: already configured to {label}.";
        }

        ProgressionType? operationType =
            intentRegistry.GetOperationType(request.OperationId);
        if (operationType.HasValue &&
            GameplayPresetCatalog.IsSupported(operationType.Value))
        {
            string key = settings.Value<string>("preset") ?? string.Empty;
            string label = GameplayPresetCatalog.Get(operationType.Value)
                .Presets
                .FirstOrDefault(option => string.Equals(
                    option.Key,
                    key,
                    StringComparison.Ordinal))
                ?.Name ?? key;
            return $"{displayName}: already configured to {label}.";
        }

        return request.OperationId switch
        {
            ProfileOperationIds.StartingResources =>
                "Starting Resources: already configured with the selected supplies.",
            ProfileOperationIds.RandomTraitExclusions =>
                "Random Trait Exclusions: already configured with the selected traits.",
            _ => $"{displayName}: already configured."
        };
    }

    private static ModificationSnapshotModel CreateApplySnapshot(
        ModificationSnapshotModel source,
        IReadOnlyDictionary<ProfileOwnedSnapshotLeaf, string> ownedLeaves,
        IReadOnlySet<ProgressionType> replayedTypes)
    {
        List<ModificationSnapshotCategoryModel> categories = new();
        foreach (ModificationSnapshotCategoryModel category in
                 source.Categories)
        {
            List<ModificationSnapshotSettingModel> settings = new();
            foreach (ModificationSnapshotSettingModel setting in
                     category.Settings)
            {
                List<ModificationSnapshotPropertyModel> properties = new();
                foreach (ModificationSnapshotPropertyModel property in
                         setting.Properties)
                {
                    string path = string.IsNullOrWhiteSpace(
                        property.PropertyPath)
                            ? property.Name
                            : property.PropertyPath;
                    ProfileOwnedSnapshotLeaf leaf = new(
                        category.Name,
                        setting.Id,
                        path);

                    JToken originalValue = property.OriginalValue;
                    if (ownedLeaves.TryGetValue(leaf, out string? owner))
                    {
                        if (owner != ProfileOperationIds.UpgradeAllEquipment)
                        {
                            continue;
                        }

                        if (!ProfileOperationCaptureService
                                .TryCreatePostUpgradeOriginalValue(
                                    property,
                                    out JToken adjustedOriginal))
                        {
                            throw new InvalidOperationException(
                                $"Upgrade All Equipment owns an invalid " +
                                $"snapshot value at '{category.Name}/" +
                                $"{setting.Id}/{path}'.");
                        }

                        if (JToken.DeepEquals(
                                adjustedOriginal,
                                property.CurrentValue))
                        {
                            continue;
                        }

                        originalValue = adjustedOriginal;
                    }

                    properties.Add(new ModificationSnapshotPropertyModel
                    {
                        Name = property.Name,
                        PropertyPath = property.PropertyPath,
                        OriginalPropertyExisted =
                            property.OriginalPropertyExisted,
                        OriginalValue = originalValue.DeepClone(),
                        CurrentValue = property.CurrentValue.DeepClone()
                    });
                }

                if (properties.Count > 0)
                {
                    settings.Add(new ModificationSnapshotSettingModel
                    {
                        Id = setting.Id,
                        Name = setting.Name,
                        DisplayName = setting.DisplayName,
                        Properties = properties
                    });
                }
            }

            if (settings.Count > 0)
            {
                categories.Add(new ModificationSnapshotCategoryModel
                {
                    Name = category.Name,
                    Settings = settings
                });
            }
        }

        return new ModificationSnapshotModel
        {
            FormatVersion = source.FormatVersion,
            CreatedAtUtc = source.CreatedAtUtc,
            EditorVersion = source.EditorVersion,
            SourceFileName = source.SourceFileName,
            GameVersion = source.GameVersion,
            SourceCdbGenerationIdentity =
                source.SourceCdbGenerationIdentity,
            Categories = categories,
            GameplayOperationStates = source.GameplayOperationStates
                .Where(state => !replayedTypes.Contains(
                    state.OperationType))
                .Select(state => state.DeepClone())
                .ToList()
        };
    }

    private static bool IsStateOnly(ProjectMutationResult result) =>
        result.GameplayOperationStateRollbackRecords.Count > 0 &&
        result.CreatedEntries.Count == 0 &&
        result.CreatedProperties.Count == 0 &&
        result.UpdatedProperties.Count == 0 &&
        result.RemovedProperties.Count == 0 &&
        result.CreatedJsonPropertyRollbackRecords.Count == 0;

    private sealed record PlannedStatefulRequest(
        ProfileOperationRequestModel Request,
        GameplayOperationStateModel? ExactSourceSeed,
        ProfileReplaySourceContext SourceContext);

    private sealed record ProfileApplyPlan(
        IReadOnlyList<ProfileOperationRequestModel> StructuralRequests,
        IReadOnlyList<PlannedStatefulRequest> StatefulRequests,
        IReadOnlyList<ProfileOperationRequestModel>
            RemainingAdditiveRequests,
        ModificationSnapshotModel Snapshot);

    private static ProfileOperationResolver
        CreateDefaultResolver()
    {
        ProjectMutationService mutationService =
            new();

        ContentCreationService contentCreationService =
            new(mutationService);

        GameplayOperationStateService stateService =
            new(mutationService);

        return new ProfileOperationResolver(
            new AddCampFacilitiesOperation(
                contentCreationService),
            new UpgradeAllEquipmentOperation(
                contentCreationService),
            new RequestBoardRewardsService(
                mutationService,
                stateService));
    }
}
