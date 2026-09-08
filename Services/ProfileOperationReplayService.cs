using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ProfileOperationReplayService
{
    private readonly ProfileOperationIntentRegistry registry;
    private readonly ProfileOperationReplayBaselineService baselineService;
    private readonly GameplayOperationStateService stateService;
    private readonly ProgressionScalingService progressionService;
    private readonly StartingResourcesService startingResourcesService;
    private readonly PartyEconomyService partyEconomyService;
    private readonly OverworldMovementSpeedService movementService;
    private readonly RainFrequencyService rainService;
    private readonly GameplayPresetService presetService;
    private readonly RandomTraitExclusionsService traitService;
    private readonly RequestBoardRewardsService requestBoardService;
    private readonly PathLevelRequirementsService pathLevelService;
    private readonly PathXpRewardsService pathRewardsService;
    private readonly ProjectOperationTransactionService transactionService;
    private readonly ProjectOperationService operationService;
    private readonly ProfileOperationResolver additiveResolver;

    public ProfileOperationReplayService()
        : this(new LocalizationService())
    {
    }

    public ProfileOperationReplayService(
        LocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(localizationService);
        ProjectMutationService mutations = new();
        stateService = new GameplayOperationStateService(
            mutations, localizationService);
        registry = new ProfileOperationIntentRegistry();
        baselineService = new ProfileOperationReplayBaselineService(
            stateService,
            registry);
        progressionService = new ProgressionScalingService(mutations, stateService);
        startingResourcesService = new StartingResourcesService(mutations, stateService);
        partyEconomyService = new PartyEconomyService(mutations, stateService);
        movementService = new OverworldMovementSpeedService(mutations, stateService);
        rainService = new RainFrequencyService(mutations, stateService);
        presetService = new GameplayPresetService(mutations, stateService);
        traitService = new RandomTraitExclusionsService(mutations, stateService);
        requestBoardService = new RequestBoardRewardsService(mutations, stateService);
        pathLevelService = new PathLevelRequirementsService(mutations, stateService);
        pathRewardsService = new PathXpRewardsService(
            mutations, stateService, localizationService);
        transactionService = new ProjectOperationTransactionService();

        ContentCreationService contentCreation = new(mutations);
        additiveResolver = new ProfileOperationResolver(
            new AddCampFacilitiesOperation(contentCreation),
            new UpgradeAllEquipmentOperation(contentCreation),
            requestBoardService);
        operationService = new ProjectOperationService();
    }

    internal GameplayOperationStateModel? Preflight(
        ProjectModel targetProject,
        ProfileOperationRequestModel intent,
        GameplayOperationStateModel? exactSourceBaselineSeed = null)
    {
        ArgumentNullException.ThrowIfNull(targetProject);
        ArgumentNullException.ThrowIfNull(intent);

        registry.ValidateRequest(
            intent,
            ModProfileFormat.ProfileOperationIntentVersion);

        ProgressionType? operationType = registry.GetOperationType(
            intent.OperationId);
        if (operationType == null)
        {
            if (exactSourceBaselineSeed != null)
            {
                throw new InvalidOperationException(
                    "Additive profile operations do not accept a " +
                    "gameplay-state baseline.");
            }

            IProjectOperation additive = additiveResolver.Resolve(intent);
            ValidateOperationCanExecute(additive, targetProject);
            return null;
        }

        JArray? baseline;
        GameplayOperationStateModel? acceptedSeed;
        try
        {
            baseline = baselineService.SelectExactSourceBaseline(
                targetProject,
                operationType.Value,
                exactSourceBaselineSeed,
                intent);
            acceptedSeed = baseline == null
                ? null
                : exactSourceBaselineSeed;
        }
        catch (InvalidOperationException) when (
            exactSourceBaselineSeed != null &&
            CanReplayFromFreshMatchingBaseline(
                targetProject,
                intent,
                operationType.Value,
                exactSourceBaselineSeed))
        {
            baseline = null;
            acceptedSeed = null;
        }

        IProjectOperation operation = CreateStatefulOperation(
            intent,
            operationType.Value);
        ValidateOperationCanExecute(operation, targetProject);

        if (operationType == ProgressionType.RandomTraitExclusions)
        {
            traitService.PreflightReplay(
                targetProject,
                (JArray)intent.Settings!["traits"]!,
                baseline);
        }

        return acceptedSeed;
    }

    internal string GetDisplayName(ProfileOperationRequestModel intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        registry.ValidateRequest(
            intent,
            ModProfileFormat.ProfileOperationIntentVersion);

        ProgressionType? operationType = registry.GetOperationType(
            intent.OperationId);
        return operationType == null
            ? additiveResolver.Resolve(intent).Name
            : CreateStatefulOperation(intent, operationType.Value).Name;
    }

    public ProjectOperationResult Replay(
        ProjectModel targetProject,
        ProfileOperationRequestModel intent,
        GameplayOperationStateModel? exactSourceBaselineSeed = null)
    {
        ArgumentNullException.ThrowIfNull(targetProject);
        ArgumentNullException.ThrowIfNull(intent);

        try
        {
            registry.ValidateRequest(
                intent,
                ModProfileFormat.ProfileOperationIntentVersion);

            if (intent.OperationId is
                ProfileOperationIds.AddCampFacilities or
                ProfileOperationIds.UpgradeAllEquipment)
            {
                if (exactSourceBaselineSeed != null)
                {
                    throw new InvalidOperationException(
                        "Additive profile operations do not accept a " +
                        "gameplay-state baseline.");
                }

                return operationService.Execute(
                    additiveResolver.Resolve(intent),
                    targetProject);
            }

            ProgressionType operationType = registry
                .GetOperationType(intent.OperationId)
                ?? throw new InvalidOperationException(
                    $"Profile operation '{intent.OperationId}' is not " +
                    "stateful.");
            JArray? baseline = baselineService.SelectExactSourceBaseline(
                targetProject,
                operationType,
                exactSourceBaselineSeed,
                intent);
            ProjectOperationExecutionContext context = new();

            try
            {
                ReplayStateful(
                    targetProject,
                    intent,
                    operationType,
                    baseline,
                    context);

                GameplayOperationStateModel state = stateService.FindState(
                        targetProject,
                        operationType)
                    ?? throw new InvalidOperationException(
                        "Replay did not create gameplay-operation state.");
                stateService.ValidateState(targetProject, state);
                if (!state.IsCompatible)
                {
                    throw new InvalidOperationException(
                        state.CompatibilityMessage);
                }

                bool stateOnly =
                    context.MutationResult.GameplayOperationStateRollbackRecords
                        .Count > 0 &&
                    context.MutationResult.CreatedEntries.Count == 0 &&
                    context.MutationResult.CreatedProperties.Count == 0 &&
                    context.MutationResult.UpdatedProperties.Count == 0 &&
                    context.MutationResult.RemovedProperties.Count == 0;

                return ProjectOperationResult.Success(
                    context.MutationResult,
                    stateOnly
                        ? "These settings were already configured; " +
                          "restore information was established."
                        : null);
            }
            catch (Exception operationException)
            {
                if (context.MutationResult.WasModified)
                {
                    try
                    {
                        transactionService.Rollback(context.MutationResult);
                    }
                    catch (Exception rollbackException)
                    {
                        throw new ProjectRollbackIntegrityException(
                            "Profile gameplay evaluation failed and its " +
                            "temporary changes could not be fully rolled back.",
                            operationException,
                            rollbackException);
                    }
                }

                throw;
            }
        }
        catch (ProjectRollbackIntegrityException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ProjectOperationResult.Failure(
                "The profile gameplay setting could not be replayed." +
                Environment.NewLine + Environment.NewLine +
                exception.Message);
        }
    }

    public IReadOnlyList<ProfileOwnedSnapshotLeaf>
        GetOwnedSnapshotLeaves(
            ProjectModel project,
            ProfileOperationRequestModel intent)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(intent);
        registry.ValidateRequest(
            intent,
            ModProfileFormat.ProfileOperationIntentVersion);

        ProgressionType? operationType =
            registry.GetOperationType(intent.OperationId);
        if (operationType == null)
        {
            return GetAdditiveOwnedLeaves(intent.OperationId);
        }

        IEnumerable<string>? traitIds = operationType ==
                ProgressionType.RandomTraitExclusions
            ? ((JArray)intent.Settings!["traits"]!)
                .OfType<JObject>()
                .Select(trait => trait.Value<string>("id")!)
            : null;

        return GetStatefulOwnedSnapshotLeaves(
            project,
            operationType.Value,
            traitIds);
    }

    internal IReadOnlyList<ProfileOwnedSnapshotLeaf>
        GetOwnedSnapshotLeaves(
            ProjectModel project,
            GameplayOperationStateModel state)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(state);

        IEnumerable<string>? traitIds = state.OperationType ==
                ProgressionType.RandomTraitExclusions
            ? state.BaselineArray
                .OfType<JObject>()
                .Select(trait => trait.Value<string>("id")!)
            : null;

        return GetStatefulOwnedSnapshotLeaves(
            project,
            state.OperationType,
            traitIds);
    }

    private IReadOnlyList<ProfileOwnedSnapshotLeaf>
        GetStatefulOwnedSnapshotLeaves(
            ProjectModel project,
            ProgressionType operationType,
            IEnumerable<string>? traitIds)
    {
        return operationType switch
        {
            ProgressionType.Character or ProgressionType.Profession =>
                GetProgressionLeaves(project, operationType),
            ProgressionType.StartingResources =>
                GetStartingResourceLeaves(project),
            ProgressionType.VolunteerWages or
            ProgressionType.ValourPoints or
            ProgressionType.CarryingCapacity =>
                GetPartyLeaves(project, operationType),
            ProgressionType.OverworldMovementSpeed => new[]
            {
                Leaf("constant", OverworldMovementSpeedService.WalkEntryId, "value"),
                Leaf("constant", OverworldMovementSpeedService.RunEntryId, "value")
            },
            ProgressionType.RainFrequency => RainFrequencyService.Regions
                .Select(region => Leaf(
                    "region",
                    region.EntryId,
                    RainFrequencyService.PropertyPath))
                .ToArray(),
            ProgressionType.RandomTraitExclusions =>
                (traitIds ?? Array.Empty<string>())
                    .Select(id => Leaf(
                        "trait",
                        id,
                        "done"))
                    .ToArray(),
            ProgressionType.RequestBoardRewards => new[]
            {
                Leaf("constant", RequestBoardRewardsService.MinimumEntryId,
                    RequestBoardRewardsService.PropertyPath),
                Leaf("constant", RequestBoardRewardsService.MaximumEntryId,
                    RequestBoardRewardsService.PropertyPath)
            },
            ProgressionType.PathLevelRequirements => new[]
            {
                Leaf("constant", PathLevelRequirementsService.BaseEntryId,
                    PathLevelRequirementsService.PropertyPath),
                Leaf("constant", PathLevelRequirementsService.NextEntryId,
                    PathLevelRequirementsService.PropertyPath)
            },
            ProgressionType.PathXpRewardsMight or
            ProgressionType.PathXpRewardsTrade or
            ProgressionType.PathXpRewardsCrime or
            ProgressionType.PathXpRewardsMystery =>
                GetPathRewardLeaves(project, operationType),
            _ when GameplayPresetCatalog.IsSupported(operationType) =>
                GameplayPresetCatalog.Get(operationType).Targets
                    .Select(target => Leaf(
                        target.Sheet,
                        target.Entry,
                        target.Path))
                    .ToArray(),
            _ => throw new InvalidOperationException(
                $"Gameplay operation '{operationType}' does not " +
                "declare snapshot ownership.")
        };
    }

    private void ReplayStateful(
        ProjectModel project,
        ProfileOperationRequestModel intent,
        ProgressionType operationType,
        JArray? baseline,
        ProjectOperationExecutionContext context)
    {
        JObject settings = intent.Settings
            ?? throw new InvalidOperationException(
                "The profile gameplay settings are missing.");

        switch (operationType)
        {
            case ProgressionType.Character:
            case ProgressionType.Profession:
                progressionService.Replay(
                    project,
                    operationType,
                    settings.Value<int>("percentage"),
                    baseline,
                    context);
                break;
            case ProgressionType.StartingResources:
                startingResourcesService.Replay(
                    project,
                    ReadStartingResources(settings),
                    baseline,
                    context);
                break;
            case ProgressionType.VolunteerWages:
            case ProgressionType.ValourPoints:
            case ProgressionType.CarryingCapacity:
                partyEconomyService.Replay(
                    project,
                    operationType,
                    ReadPartyEconomy(settings, operationType),
                    baseline,
                    context);
                break;
            case ProgressionType.OverworldMovementSpeed:
                movementService.Replay(
                    project,
                    Enum.Parse<OverworldMovementPreset>(
                        settings.Value<string>("preset")!,
                        ignoreCase: false),
                    baseline,
                    context);
                break;
            case ProgressionType.RainFrequency:
                rainService.Replay(
                    project,
                    Enum.Parse<RainFrequencyPreset>(
                        settings.Value<string>("preset")!,
                        ignoreCase: false),
                    baseline,
                    context);
                break;
            case ProgressionType.RandomTraitExclusions:
                traitService.Replay(
                    project,
                    (JArray)settings["traits"]!,
                    baseline,
                    context);
                break;
            case ProgressionType.RequestBoardRewards:
                requestBoardService.Replay(
                    project,
                    settings.Value<int>("percentage"),
                    baseline,
                    context);
                break;
            case ProgressionType.PathLevelRequirements:
                pathLevelService.Replay(
                    project,
                    settings.Value<int>("percentage"),
                    baseline,
                    context);
                break;
            case ProgressionType.PathXpRewardsMight:
            case ProgressionType.PathXpRewardsTrade:
            case ProgressionType.PathXpRewardsCrime:
            case ProgressionType.PathXpRewardsMystery:
                pathRewardsService.Replay(
                    project,
                    PathXpRewardsService.GetPathId(operationType),
                    settings.Value<int>("multiplier"),
                    baseline,
                    context);
                break;
            default:
                presetService.Replay(
                    project,
                    operationType,
                    settings.Value<string>("preset")!,
                    baseline,
                    context);
                break;
        }
    }

    private bool CanReplayFromFreshMatchingBaseline(
        ProjectModel project,
        ProfileOperationRequestModel intent,
        ProgressionType operationType,
        GameplayOperationStateModel seed)
    {
        try
        {
            if (project.SourceProvenanceStatus !=
                    SourceProvenanceStatus.Verified ||
                seed.FormatVersion !=
                    GameplayOperationStateModel.CurrentFormatVersion ||
                seed.OperationType != operationType ||
                !new CdbGenerationIdentityService().AreEqual(
                    project.SourceCdbGenerationIdentity,
                    seed.ProjectCompatibilityIdentity) ||
                seed.BaselineArray.Count != seed.ElementCount ||
                !string.Equals(
                    GameplayOperationFingerprintService
                        .CreateContentFingerprint(seed.BaselineArray),
                    seed.BaselineFingerprint,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    GameplayOperationFingerprintService
                        .CreateShapeFingerprint(seed.BaselineArray),
                    seed.ElementShapeFingerprint,
                    StringComparison.Ordinal))
            {
                return false;
            }

            registry.ValidateStateMatchesRequest(seed, intent);
            JArray current;
            bool metadataMatches;

            switch (operationType)
            {
                case ProgressionType.Character:
                case ProgressionType.Profession:
                    ProgressionTableBinding binding = progressionService
                        .ResolveProgressionTable(project, operationType);
                    current = (JArray)binding.ArrayProperty
                        .GetCurrentValueSnapshot();
                    metadataMatches =
                        string.Equals(
                            seed.TargetSheet,
                            "constant",
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetEntry,
                            binding.Entry.Id,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetPath,
                            binding.ArrayPropertyPath,
                            StringComparison.Ordinal);
                    break;

                case ProgressionType.StartingResources:
                    current = StartingResourcesService
                        .CaptureCurrentTargets(project);
                    metadataMatches =
                        string.Equals(
                            seed.TargetSheet,
                            "item,startChoice",
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetEntry,
                            "StartingResources",
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetPath,
                            "props.startQuantity|props.items",
                            StringComparison.Ordinal);
                    break;

                case ProgressionType.VolunteerWages:
                case ProgressionType.ValourPoints:
                case ProgressionType.CarryingCapacity:
                    current = PartyEconomyService.CaptureTargets(
                        project,
                        operationType);
                    metadataMatches = MatchesMetadataFromBaseline(
                        seed,
                        "sheet",
                        "entry",
                        "path");
                    break;

                case ProgressionType.OverworldMovementSpeed:
                    (MovementTarget walk, MovementTarget run) =
                        OverworldMovementSpeedService.ResolveTargets(project);
                    current = OverworldMovementSpeedService.CaptureTargets(
                        walk,
                        run);
                    metadataMatches =
                        string.Equals(
                            seed.TargetSheet,
                            "constant,constant",
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetEntry,
                            $"{OverworldMovementSpeedService.WalkEntryId}," +
                            OverworldMovementSpeedService.RunEntryId,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetPath,
                            "value|value",
                            StringComparison.Ordinal);
                    break;

                case ProgressionType.RainFrequency:
                    IReadOnlyList<RainTarget> rainTargets =
                        RainFrequencyService.ResolveTargets(project);
                    current = RainFrequencyService.Capture(rainTargets);
                    metadataMatches =
                        string.Equals(
                            seed.TargetSheet,
                            "region",
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetEntry,
                            string.Join(",", RainFrequencyService.Regions
                                .Select(region => region.EntryId)),
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetPath,
                            RainFrequencyService.PropertyPath,
                            StringComparison.Ordinal);
                    break;

                case ProgressionType.RandomTraitExclusions:
                    return RandomTraitExclusionsService
                        .CurrentMatchesBaseline(project, seed);

                case ProgressionType.RequestBoardRewards:
                    RequestBoardRewardTargets rewardTargets =
                        RequestBoardRewardsService.ResolveTargets(project);
                    current = RequestBoardRewardsService.Capture(
                        rewardTargets);
                    metadataMatches =
                        string.Equals(
                            seed.TargetSheet,
                            "constant,constant",
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetEntry,
                            $"{RequestBoardRewardsService.MinimumEntryId}," +
                            RequestBoardRewardsService.MaximumEntryId,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            seed.TargetPath,
                            $"{RequestBoardRewardsService.PropertyPath}|" +
                            RequestBoardRewardsService.PropertyPath,
                            StringComparison.Ordinal);
                    break;

                case ProgressionType.PathLevelRequirements:
                    PathLevelRequirementTargets pathLevelTargets =
                        PathLevelRequirementsService.ResolveTargets(project);
                    current = PathLevelRequirementsService.Capture(pathLevelTargets);
                    metadataMatches =
                        string.Equals(seed.TargetSheet, "constant,constant", StringComparison.Ordinal) &&
                        string.Equals(seed.TargetEntry,
                            $"{PathLevelRequirementsService.BaseEntryId}," +
                            PathLevelRequirementsService.NextEntryId,
                            StringComparison.Ordinal) &&
                        string.Equals(seed.TargetPath, "value|value", StringComparison.Ordinal) &&
                        seed.GameplaySettings?["pathMaxLevel"]?.Value<int>() ==
                            pathLevelTargets.MaxLevel;
                    break;

                case ProgressionType.PathXpRewardsMight:
                case ProgressionType.PathXpRewardsTrade:
                case ProgressionType.PathXpRewardsCrime:
                case ProgressionType.PathXpRewardsMystery:
                    PathXpRewardTargets pathRewardTargets =
                        PathXpRewardsService.ResolveTargets(
                            project,
                            PathXpRewardsService.GetPathId(operationType));
                    current = PathXpRewardsService.Capture(pathRewardTargets);
                    metadataMatches = MatchesMetadataFromBaseline(
                        seed, "sheet", "entry", "targetPath");
                    break;

                default:
                    IReadOnlyList<ResolvedGameplayTarget> targets =
                        GameplayPresetService.ResolveTargets(
                            project,
                            operationType);
                    current = GameplayPresetService.CaptureTargets(targets);
                    metadataMatches = MatchesMetadataFromBaseline(
                        seed,
                        "sheet",
                        "entry",
                        "targetPath");
                    break;
            }

            return metadataMatches &&
                string.Equals(
                    GameplayOperationFingerprintService
                        .CreateShapeFingerprint(current),
                    seed.ElementShapeFingerprint,
                    StringComparison.Ordinal) &&
                JToken.DeepEquals(current, seed.BaselineArray);
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesMetadataFromBaseline(
        GameplayOperationStateModel seed,
        string sheetProperty,
        string entryProperty,
        string pathProperty) =>
        string.Equals(
            seed.TargetSheet,
            Join(seed.BaselineArray, sheetProperty, ","),
            StringComparison.Ordinal) &&
        string.Equals(
            seed.TargetEntry,
            Join(seed.BaselineArray, entryProperty, ","),
            StringComparison.Ordinal) &&
        string.Equals(
            seed.TargetPath,
            Join(seed.BaselineArray, pathProperty, "|"),
            StringComparison.Ordinal);

    private static string Join(
        JArray values,
        string propertyName,
        string separator) =>
        string.Join(
            separator,
            values.OfType<JObject>()
                .Select(value =>
                    value.Value<string>(propertyName) ?? string.Empty));

    private IProjectOperation CreateStatefulOperation(
        ProfileOperationRequestModel intent,
        ProgressionType operationType)
    {
        JObject settings = intent.Settings
            ?? throw new InvalidOperationException(
                "The profile gameplay settings are missing.");

        return operationType switch
        {
            ProgressionType.Character =>
                new CharacterXpRequirementsOperation(
                    progressionService,
                    settings.Value<int>("percentage")),
            ProgressionType.Profession =>
                new ProfessionXpRequirementsOperation(
                    progressionService,
                    settings.Value<int>("percentage")),
            ProgressionType.StartingResources =>
                new StartingResourcesOperation(
                    startingResourcesService,
                    ReadStartingResources(settings)),
            ProgressionType.VolunteerWages or
            ProgressionType.ValourPoints or
            ProgressionType.CarryingCapacity =>
                new PartyEconomyOperation(
                    partyEconomyService,
                    operationType,
                    ReadPartyEconomy(settings, operationType)),
            ProgressionType.OverworldMovementSpeed =>
                new OverworldMovementSpeedOperation(
                    movementService,
                    Enum.Parse<OverworldMovementPreset>(
                        settings.Value<string>("preset")!,
                        ignoreCase: false)),
            ProgressionType.RainFrequency =>
                new RainFrequencyOperation(
                    rainService,
                    Enum.Parse<RainFrequencyPreset>(
                        settings.Value<string>("preset")!,
                        ignoreCase: false)),
            ProgressionType.RandomTraitExclusions =>
                new RandomTraitExclusionsOperation(
                    traitService,
                    ((JArray)settings["traits"]!)
                        .OfType<JObject>()
                        .Where(selection =>
                            selection.Value<bool>("allowed"))
                        .Select(selection =>
                            selection.Value<string>("id")!)
                        .ToArray()),
            ProgressionType.RequestBoardRewards =>
                new RequestBoardRewardsOperation(
                    requestBoardService,
                    settings.Value<int>("percentage")),
            ProgressionType.PathLevelRequirements =>
                new PathLevelRequirementsOperation(
                    pathLevelService,
                    settings.Value<int>("percentage")),
            ProgressionType.PathXpRewardsMight or
            ProgressionType.PathXpRewardsTrade or
            ProgressionType.PathXpRewardsCrime or
            ProgressionType.PathXpRewardsMystery =>
                new PathXpRewardsOperation(
                    pathRewardsService,
                    PathXpRewardsService.GetPathId(operationType),
                    settings.Value<int>("multiplier")),
            _ when GameplayPresetCatalog.IsSupported(operationType) =>
                new GameplayPresetOperation(
                    presetService,
                    operationType,
                    settings.Value<string>("preset")!),
            _ => throw new InvalidOperationException(
                $"Profile operation '{intent.OperationId}' is not " +
                "replay-capable.")
        };
    }

    private static void ValidateOperationCanExecute(
        IProjectOperation operation,
        ProjectModel project)
    {
        if (!operation.CanExecute(project))
        {
            throw new InvalidOperationException(
                $"The operation '{operation.Name}' cannot be executed " +
                "on the current project.");
        }

        if (operation is IContextualProjectOperation contextual)
        {
            contextual.Preflight(project);
        }
    }

    private IReadOnlyList<ProfileOwnedSnapshotLeaf>
        GetProgressionLeaves(
            ProjectModel project,
            ProgressionType operationType)
    {
        ProgressionTableBinding binding = progressionService
            .ResolveProgressionTable(project, operationType);
        return new[]
        {
            Leaf("constant", binding.Entry.Id, binding.ArrayPropertyPath)
        };
    }

    private static IReadOnlyList<ProfileOwnedSnapshotLeaf>
        GetStartingResourceLeaves(ProjectModel project) =>
        StartingResourcesService.CaptureCurrentTargets(project)
            .OfType<JObject>()
            .Select(record => Leaf(
                string.Equals(
                    record.Value<string>("kind"),
                    "shared",
                    StringComparison.Ordinal)
                    ? "item"
                    : "startChoice",
                record.Value<string>("entry")!,
                record.Value<string>("path")!))
            .ToArray();

    private static IReadOnlyList<ProfileOwnedSnapshotLeaf> GetPartyLeaves(
        ProjectModel project,
        ProgressionType operationType) =>
        PartyEconomyService.CaptureTargets(project, operationType)
            .OfType<JObject>()
            .Select(record => Leaf(
                record.Value<string>("sheet")!,
                record.Value<string>("entry")!,
                record.Value<string>("path")!))
            .ToArray();

    private static IReadOnlyList<ProfileOwnedSnapshotLeaf>
        GetPathRewardLeaves(
            ProjectModel project,
            ProgressionType operationType) =>
        PathXpRewardsService.ResolveTargets(
                project,
                PathXpRewardsService.GetPathId(operationType))
            .Targets
            .Select(target => Leaf(
                "counter",
                target.Entry.Id,
                PathXpRewardsService.RewardPropertyPath))
            .ToArray();

    private static IReadOnlyList<ProfileOwnedSnapshotLeaf>
        GetAdditiveOwnedLeaves(string operationId)
    {
        if (operationId == ProfileOperationIds.UpgradeAllEquipment)
        {
            return UpgradeAllEquipmentTargetCatalog.EntryIds
                .Select(id => Leaf("item", id, "props.flags"))
                .ToArray();
        }

        return Array.Empty<ProfileOwnedSnapshotLeaf>();
    }

    private static StartingResourcesSettings ReadStartingResources(
        JObject settings) => new()
    {
        Krowns = settings.Value<int>("krowns"),
        Bread = settings.Value<int>("bread"),
        Apples = settings.Value<int>("apples"),
        IronOre = settings.Value<int>("ironOre"),
        Wood = settings.Value<int>("wood"),
        Cloth = settings.Value<int>("cloth")
    };

    private static PartyEconomySettings ReadPartyEconomy(
        JObject settings,
        ProgressionType operationType) => operationType switch
    {
        ProgressionType.VolunteerWages => new PartyEconomySettings
        {
            VolunteerPercentage = settings.Value<int>("volunteerPercentage")
        },
        ProgressionType.ValourPoints => new PartyEconomySettings
        {
            MaximumValour = settings.Value<int>("maximumValour"),
            RestoredValour = settings.Value<int>("restoredValour"),
            TentTier1Valour = settings.Value<int>("tentTier1Valour"),
            TentTier2Valour = settings.Value<int>("tentTier2Valour"),
            TentTier3Valour = settings.Value<int>("tentTier3Valour")
        },
        ProgressionType.CarryingCapacity => new PartyEconomySettings
        {
            SaddlebagCapacity = settings.Value<int>("saddlebagCapacity"),
            PonyStartingCapacity = settings.Value<int>("ponyStartingCapacity"),
            HitchingPostTier1Base = settings.Value<int>("hitchingPostTier1Base"),
            HitchingPostTier2Base = settings.Value<int>("hitchingPostTier2Base"),
            HitchingPostTier3Base = settings.Value<int>("hitchingPostTier3Base"),
            HitchingPostTier1Trait = settings.Value<int>("hitchingPostTier1Trait"),
            HitchingPostTier2Trait = settings.Value<int>("hitchingPostTier2Trait"),
            HitchingPostTier3Trait = settings.Value<int>("hitchingPostTier3Trait")
        },
        _ => throw new ArgumentOutOfRangeException(nameof(operationType))
    };

    private static ProfileOwnedSnapshotLeaf Leaf(
        string sheet,
        string entry,
        string path) => new(sheet, entry, path);
}
