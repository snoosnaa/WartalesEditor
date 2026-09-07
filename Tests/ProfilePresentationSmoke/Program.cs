using System.Reflection;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.Services.Validation;
using WartalesEditor.ViewModels;

int checks = 0;
ProfileOperationReplayService replay = new();

ProjectModel xp = ProgressionProject();
ProgressionScalingDialogViewModel xpViewModel = new(
    xp,
    new ProgressionScalingService(new ProjectMutationService()),
    new GameplayOperationStateService());
xpViewModel.CharacterPercentage = 70;
Check(replay.Replay(xp, Request(ProfileOperationIds.CharacterXp,
    new JObject { ["percentage"] = 40 })).Succeeded,
    "Character XP profile replay succeeds");
Check(replay.Replay(xp, Request(ProfileOperationIds.ProfessionXp,
    new JObject { ["percentage"] = 50 })).Succeeded,
    "Profession XP profile replay succeeds");
xpViewModel.RefreshAfterProjectOperation();
Check(xpViewModel.CharacterPercentage == 70,
    "pending Character XP input is preserved");
Check(xpViewModel.ProfessionPercentage == 50,
    "non-pending Profession XP refreshes to authoritative 50 percent");
Check(xpViewModel.HasTrustedCharacterBaseline &&
      xpViewModel.HasTrustedProfessionBaseline,
    "XP refresh exposes authoritative baselines");
Check(xpViewModel.CharacterPreviewText.Contains("At 70%", StringComparison.Ordinal),
    "pending XP preview is recalculated against refreshed state");
xpViewModel.CharacterPercentage = 0;
xpViewModel.RefreshAfterProjectOperation();
Check(xpViewModel.CharacterPercentage == 0 &&
      !string.IsNullOrWhiteSpace(xpViewModel.CharacterValidationMessage),
    "invalid pending XP remains visibly invalid");

ProjectModel movement = MovementProject();
ProjectMutationService movementMutations = new();
GameplayOperationStateService movementStates = new(movementMutations);
OverworldMovementSpeedService movementService = new(
    movementMutations,
    movementStates);
OverworldMovementSpeedDialogViewModel movementViewModel = new(
    movement,
    movementService);
movementService.Apply(movement, OverworldMovementPreset.Faster);
movementViewModel.RefreshAfterProjectOperation();
Check(movementViewModel.CurrentStateText == "Fast" &&
      movementViewModel.SelectedPreset?.Preset == OverworldMovementPreset.Faster,
    "Run Speed refresh maps Faster identifier to Fast player label");
Check(movementViewModel.CanRestorePreviousValues,
    "Run Speed refresh enables Restore Previous Values");
movementViewModel.SelectedPreset = movementViewModel.Presets.Single(option =>
    option.Preset == OverworldMovementPreset.VeryFast);
movementService.Apply(movement, OverworldMovementPreset.Fast);
movementViewModel.RefreshAfterProjectOperation();
Check(movementViewModel.CurrentStateText == "Faster" &&
      movementViewModel.SelectedPreset?.Preset == OverworldMovementPreset.VeryFast,
    "valid pending Run Speed selection survives authoritative refresh");

ProjectModel traits = TraitProject();
ProjectMutationService traitMutations = new();
GameplayOperationStateService traitStates = new(traitMutations);
GameplayPresetService traitService = new(traitMutations, traitStates);
GameplayPresetDialogViewModel traitViewModel = new(
    traits,
    traitService,
    ProgressionType.PositiveRandomTraits);
traitService.Apply(traits, ProgressionType.PositiveRandomTraits, "PositiveOnly");
traitViewModel.RefreshAfterProjectOperation();
Check(traitViewModel.CurrentStateText == "Positive Only" &&
      traitViewModel.SelectedPreset?.Key == "PositiveOnly",
    "Positive Random Traits refreshes to Positive Only");
Check(traitViewModel.CanRestorePreviousValues,
    "Positive Random Traits refresh enables Restore Previous Values");

ProjectModel rain = RainProject();
ProjectMutationService rainMutations = new();
GameplayOperationStateService rainStates = new(rainMutations);
RainFrequencyService rainService = new(rainMutations, rainStates);
RainFrequencyDialogViewModel rainViewModel = new(rain, rainService);
rainService.Apply(rain, RainFrequencyPreset.RareRain);
rainViewModel.RefreshAfterProjectOperation();
Check(rainViewModel.CurrentStateText == "Rare Rain" &&
      rainViewModel.CanRestorePreviousValues,
    "Rain refreshes current preset and restore authority");

ProjectModel fishing = ScalarProject("FishingDurationControl", 6);
ProjectMutationService fishingMutations = new();
GameplayOperationStateService fishingStates = new(fishingMutations);
GameplayPresetService fishingService = new(fishingMutations, fishingStates);
GameplayPresetDialogViewModel fishingViewModel = new(
    fishing,
    fishingService,
    ProgressionType.FishingSpeed);
fishingViewModel.SelectedPreset = fishingViewModel.Presets.Single(option =>
    option.Key == "VeryFast");
fishingService.Apply(fishing, ProgressionType.FishingSpeed, "Faster");
fishingViewModel.RefreshAfterProjectOperation();
Check(fishingViewModel.CurrentStateText == "Faster" &&
      fishingViewModel.SelectedPreset?.Key == "VeryFast",
    "generic preset refresh preserves a valid pending selection");
Check(fishingViewModel.CanRestorePreviousValues,
    "generic preset refresh exposes restore authority");

ProjectModel countSource = ScalarProject("FishingDurationControl", 6);
ProjectMutationService countSourceMutations = new();
GameplayOperationStateService countSourceStates = new(countSourceMutations);
new GameplayPresetService(countSourceMutations, countSourceStates)
    .Apply(countSource, ProgressionType.FishingSpeed, "Fast");
ModProfileModel countProfile = new ModProfileWorkflowService()
    .CreateProfile(countSource, "Phase 5 count isolation");
ProjectModel countTarget = ScalarProject("FishingDurationControl", 6);
ProjectMutationService countTargetMutations = new();
GameplayPresetDialogViewModel countTargetViewModel = new(
    countTarget,
    new GameplayPresetService(
        countTargetMutations,
        new GameplayOperationStateService(countTargetMutations)),
    ProgressionType.FishingSpeed);
countTargetViewModel.SelectedPreset = countTargetViewModel.Presets.Single(option =>
    option.Key == "VeryFast");
string fishingJsonBeforeCount = countTarget.RootDocument.ToString();
int evaluatedCount = new EffectiveChangeCountService().Calculate(
    countTarget,
    countProfile,
    out bool countIsExact);
Check(countIsExact && evaluatedCount == 1 &&
      countTarget.RootDocument.ToString() == fishingJsonBeforeCount,
    "target-context counting remains exact and rolls back observationally");
Check(countTargetViewModel.CurrentStateText == "Vanilla" &&
      countTargetViewModel.SelectedPreset?.Key == "VeryFast",
    "Profile Manager counting does not refresh dialogs or alter pending input");

ProjectModel rewards = RequestBoardProject();
ProjectMutationService rewardMutations = new();
GameplayOperationStateService rewardStates = new(rewardMutations);
RequestBoardRewardsService rewardService = new(rewardMutations, rewardStates);
RequestBoardRewardsDialogViewModel rewardViewModel = new(rewards, rewardService);
rewardService.Apply(rewards, 200);
rewardViewModel.RefreshAfterProjectOperation();
Check(rewardViewModel.CurrentStateText == "200%" &&
      rewardViewModel.SelectedPreset?.Percentage == 200,
    "Request Board refreshes semantic percentage");
Check(rewardViewModel.CanRestorePreviousValues,
    "Request Board refresh enables Restore Previous Values");

StartingResourcesDialogViewModel startingViewModel = new(
    CreateProject(),
    new GameplayOperationStateService(new ProjectMutationService()));
startingViewModel.Krowns = 250;
startingViewModel.Bread = 25;
startingViewModel.RefreshAfterProjectOperation();
Check(startingViewModel.Krowns == 250 && startingViewModel.Bread == 25,
    "Starting Resources pending values survive refresh");
Check(startingViewModel.PreviewText.Contains("250 Krowns", StringComparison.Ordinal),
    "Starting Resources pending preview is recalculated");

ProjectModel valour = ValourProject();
PartyEconomyDialogViewModel partyViewModel = new(
    valour,
    new PartyEconomyService(
        new ProjectMutationService(),
        new GameplayOperationStateService()),
    ProgressionType.ValourPoints);
partyViewModel.MaximumValour = 25;
partyViewModel.RestoredValour = 6;
partyViewModel.RefreshAfterProjectOperation();
Check(partyViewModel.MaximumValour == 25 &&
      partyViewModel.RestoredValour == 6,
    "Party Economy pending values survive refresh");
Check(partyViewModel.PreviewText.Contains("25 Valour", StringComparison.Ordinal),
    "Party Economy pending preview is recalculated");

CountingRefreshable first = new();
CountingRefreshable last = new();
IReadOnlyList<Exception> refreshFailures =
    MainViewModel.RefreshGameplayViewModels(new IGameplayProjectRefreshable[]
    {
        first,
        new ThrowingRefreshable(),
        last
    });
Check(first.Count == 1 && last.Count == 1,
    "one refresh failure does not prevent other open dialogs refreshing");
Check(refreshFailures.Count == 1,
    "post-commit refresh failures are surfaced separately");

ModProfileWorkflowService workflow = new();
Check(SemanticSummary(workflow,
        Request(ProfileOperationIds.CharacterXp,
            new JObject { ["percentage"] = 40 }),
        "Character XP") ==
      "Character XP: already configured at 40%.",
    "already-configured Character XP result is semantic");
Check(SemanticSummary(workflow,
        Request(ProfileOperationIds.OverworldMovementSpeed,
            new JObject { ["preset"] = "Faster" }),
        "Run Speed") ==
      "Run Speed: already configured to Fast.",
    "already-configured Run Speed uses accepted player label");
Check(SemanticSummary(workflow,
        Request(ProfileOperationIds.PositiveRandomTraits,
            new JObject { ["preset"] = "PositiveOnly" }),
        "Positive Random Traits") ==
      "Positive Random Traits: already configured to Positive Only.",
    "already-configured Positive Traits result is semantic");
Check(SemanticSummary(workflow,
        Request(ProfileOperationIds.RequestBoardRewards,
            new JObject { ["percentage"] = 200 }),
        "Request Board Rewards") ==
      "Request Board Rewards: already configured at 200%.",
    "Request Board already-configured result identifies its percentage");
Check(SemanticSummary(workflow,
        Request(ProfileOperationIds.AddCampFacilities, new JObject()),
        "Add Camp Facilities").Contains("Anvil and Apothecary Table",
            StringComparison.Ordinal),
    "Add Camp already-present result names its facilities");

ProjectMutationResult almostCompleteUpgrade = new();
for (int index = 0;
     index < UpgradeAllEquipmentTargetCatalog.Count - 3;
     index++)
{
    almostCompleteUpgrade.AddUpdatedProperty(
        new PropertyModel { Name = $"flags-{index}" },
        new JValue(0));
}
string upgradeSummary = SemanticSummary(
    workflow,
    Request(ProfileOperationIds.UpgradeAllEquipment, new JObject()),
    "Upgrade All Equipment",
    ProfileOperationApplyStatus.Applied,
    almostCompleteUpgrade);
Check(upgradeSummary ==
      "Upgrade All Equipment: 3 eligible items were already upgradeable.",
    "Upgrade All Equipment identifies three already-upgradeable items semantically");

string combined = AppendSemanticResults(new[]
{
    new ProfileOperationApplyItemResultModel(
        ProfileOperationIds.UpgradeAllEquipment,
        "Upgrade All Equipment",
        ProfileOperationApplyStatus.Applied,
        string.Empty,
        upgradeSummary),
    new ProfileOperationApplyItemResultModel(
        ProfileOperationIds.RequestBoardRewards,
        "Request Board Rewards",
        ProfileOperationApplyStatus.AlreadyConfigured,
        string.Empty,
        "Request Board Rewards: already configured at 200%."),
    new ProfileOperationApplyItemResultModel(
        ProfileOperationIds.RequestBoardRewards,
        "Request Board Rewards",
        ProfileOperationApplyStatus.AlreadyConfigured,
        string.Empty,
        "Request Board Rewards: already configured at 200%.")
});
Check(combined.Contains("Already configured:", StringComparison.Ordinal) &&
      combined.Contains("Upgrade All Equipment", StringComparison.Ordinal) &&
      combined.Contains("Request Board Rewards", StringComparison.Ordinal),
    "mixed semantic completion summary remains coherent");
Check(!combined.Contains("CDB", StringComparison.OrdinalIgnoreCase) &&
      !combined.Contains("fingerprint", StringComparison.OrdinalIgnoreCase) &&
      !combined.Contains("property path", StringComparison.OrdinalIgnoreCase),
    "profile completion reporting contains no internal data jargon");
Check(combined.Split(
          "Request Board Rewards: already configured at 200%.",
          StringSplitOptions.None).Length - 1 == 1,
    "Request Board semantic result is not duplicated");

ProjectModel savedMovement = MovementProject();
ProjectMutationService savedMutations = new();
GameplayOperationStateService savedStates = new(savedMutations);
OverworldMovementSpeedService savedService = new(savedMutations, savedStates);
savedService.Apply(savedMovement, OverworldMovementPreset.Faster);
savedStates.AcceptCurrentStates(savedMovement);
OverworldMovementSpeedDialogViewModel reopened = new(
    savedMovement,
    savedService);
Check(reopened.CurrentStateText == "Fast" &&
      reopened.CanRestorePreviousValues,
    "fresh dialog reads accepted persisted gameplay state");

ProfileOperationApplyItemResultModel[] stableIdentityResults =
{
    new("same-operation", "First", ProfileOperationApplyStatus.AlreadyConfigured,
        string.Empty, "Earlier wording."),
    new("same-operation", "First", ProfileOperationApplyStatus.AlreadyConfigured,
        string.Empty, "Authoritative later wording."),
    new("different-operation", "Second", ProfileOperationApplyStatus.AlreadyConfigured,
        string.Empty, "Authoritative later wording.")
};
string[] stableSummaries =
    MainViewModel.GetSemanticAlreadyConfiguredSummaries(stableIdentityResults);
Check(stableSummaries.SequenceEqual(new[]
      {
          "Authoritative later wording.",
          "Authoritative later wording."
      }),
    "semantic summaries de-duplicate by stable operation ID and retain unrelated identical text");

rain = RainProject();
rainMutations = new ProjectMutationService();
rainStates = new GameplayOperationStateService(rainMutations);
rainService = new RainFrequencyService(rainMutations, rainStates);
rainViewModel = new RainFrequencyDialogViewModel(rain, rainService);
rainViewModel.SelectedPreset = rainViewModel.Presets.Single(option =>
    option.Preset == RainFrequencyPreset.NoRain);
rainService.Apply(rain, RainFrequencyPreset.RareRain);
rainViewModel.RefreshAfterProjectOperation();
Check(rainViewModel.CurrentStateText == "Rare Rain" &&
      rainViewModel.SelectedPreset?.Preset == RainFrequencyPreset.NoRain &&
      rainViewModel.CanRestorePreviousValues,
    "Rain preserves a valid pending selection while refreshing authoritative state");

rewards = RequestBoardProject();
rewardMutations = new ProjectMutationService();
rewardStates = new GameplayOperationStateService(rewardMutations);
rewardService = new RequestBoardRewardsService(rewardMutations, rewardStates);
rewardViewModel = new RequestBoardRewardsDialogViewModel(rewards, rewardService);
rewardViewModel.SelectedPreset = rewardViewModel.Presets.Single(option =>
    option.Percentage == 300);
rewardService.Apply(rewards, 200);
rewardViewModel.RefreshAfterProjectOperation();
Check(rewardViewModel.CurrentStateText == "200%" &&
      rewardViewModel.SelectedPreset?.Percentage == 300 &&
      rewardViewModel.CanRestorePreviousValues,
    "Request Board preserves a valid pending percentage while refreshing authoritative state");

ProgressionType[] genericTypes = Enum.GetValues<ProgressionType>()
    .Where(GameplayPresetCatalog.IsSupported)
    .ToArray();
foreach (ProgressionType type in genericTypes)
{
    ProjectModel genericProject = GenericPresetProject(type);
    ProjectMutationService genericMutations = new();
    GameplayOperationStateService genericStates = new(genericMutations);
    GameplayPresetService genericService = new(genericMutations, genericStates);
    GameplayPresetDialogViewModel genericViewModel = new(
        genericProject,
        genericService,
        type);
    GameplayPresetOption appliedPreset = genericViewModel.Presets[1];
    GameplayPresetOption pendingPreset = genericViewModel.Presets[^1];
    genericViewModel.SelectedPreset = pendingPreset;
    genericService.Apply(genericProject, type, appliedPreset.Key);
    genericViewModel.RefreshAfterProjectOperation();
    Check(genericViewModel.CurrentStateText == appliedPreset.Name &&
          genericViewModel.SelectedPreset?.Key == pendingPreset.Key &&
          genericViewModel.CanRestorePreviousValues,
        $"{type} production ViewModel refresh preserves pending preset");
}

VerifyRandomTraitCandidateReconciliation();
VerifyMainViewModelProfileApplyIntegration();
VerifyMixedProductionSummary();
VerifyPersistedFreshDialogs();

if (checks != 75)
    throw new InvalidOperationException(
        $"FAILED: focused Phase 5 check count is 75, actual {checks}");
Console.WriteLine($"ALL PROFILE PRESENTATION CHECKS PASSED ({checks})");

void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"FAILED: {name}");
    checks++;
    Console.WriteLine($"PASS {name}");
}

string SemanticSummary(
    ModProfileWorkflowService service,
    ProfileOperationRequestModel request,
    string displayName,
    ProfileOperationApplyStatus status =
        ProfileOperationApplyStatus.AlreadyConfigured,
    ProjectMutationResult? mutationResult = null)
{
    MethodInfo method = typeof(ModProfileWorkflowService).GetMethod(
        "BuildAlreadyConfiguredSummary",
        BindingFlags.Instance | BindingFlags.NonPublic)!;
    return (string?)method.Invoke(service, new object[]
    {
        request,
        displayName,
        status,
        mutationResult ?? new ProjectMutationResult()
    }) ?? string.Empty;
}

string AppendSemanticResults(
    IReadOnlyList<ProfileOperationApplyItemResultModel> results)
{
    StringBuilder message = new("5 changes applied.");
    string[] summaries =
        MainViewModel.GetSemanticAlreadyConfiguredSummaries(results);
    MethodInfo method = typeof(MainViewModel).GetMethod(
        "AppendSemanticAlreadyConfigured",
        BindingFlags.Static | BindingFlags.NonPublic)!;
    method.Invoke(null, new object[] { message, summaries });
    return message.ToString();
}

void VerifyRandomTraitCandidateReconciliation()
{
    ProjectModel project = RandomTraitProject();
    ProjectMutationService mutations = new();
    GameplayOperationStateService states = new(mutations);
    RandomTraitExclusionsService service = new(mutations, states);
    RandomTraitExclusionsDialogViewModel viewModel = new(
        project,
        service,
        new LocalizationService());

    RandomTraitExclusionItemViewModel retained = AllTraits(viewModel)
        .Single(item => item.Id == "PositiveTrue");
    retained.IsAllowed = false;
    AddRandomTrait(project, "NewTrait", 0, null, "Recruitment");
    viewModel.RefreshAfterProjectOperation();
    Check(AllTraits(viewModel).Any(item =>
              item.Id == "NewTrait" && item.IsAllowed) &&
          !AllTraits(viewModel).Single(item =>
              item.Id == "PositiveTrue").IsAllowed,
        "Random Trait refresh adds a candidate with its authoritative default and preserves valid pending IDs");

    RemoveRandomTrait(project, "NegativeAbsent");
    viewModel.RefreshAfterProjectOperation();
    Check(AllTraits(viewModel).All(item => item.Id != "NegativeAbsent") &&
          !viewModel.GetAllowedTraitIds().Contains("NegativeAbsent"),
        "Random Trait refresh removes vanished candidates from display and submission");

    MoveRandomTrait(project, "PositiveTrue", "Recruitment");
    viewModel.RefreshAfterProjectOperation();
    Check(AllTraits(viewModel).Single(item =>
              item.Id == "PositiveTrue").IsAllowed,
        "Random Trait refresh rejects stale pending selection after semantic group change");

    ProjectModel stateOnly = RandomTraitProject();
    ProjectMutationService stateMutations = new();
    GameplayOperationStateService stateService = new(stateMutations);
    RandomTraitExclusionsService stateTraitService = new(
        stateMutations,
        stateService);
    IReadOnlyCollection<string> allowed = stateTraitService.Discover(stateOnly)
        .Where(candidate => candidate.IsAllowed)
        .Select(candidate => candidate.Id)
        .ToArray();
    stateTraitService.Apply(stateOnly, allowed);
    RandomTraitExclusionsDialogViewModel stateViewModel = new(
        stateOnly,
        stateTraitService,
        new LocalizationService());
    AddRandomTrait(stateOnly, "StateOnlyNew", 1, null, "Recruitment");
    stateViewModel.RefreshAfterProjectOperation();
    Check(AllTraits(stateViewModel).Any(item => item.Id == "StateOnlyNew") &&
          stateViewModel.CanRestorePreviousValues,
        "state-backed Random Trait refresh reconciles new membership and restore authority");

    ProjectModel countSource = RandomTraitProject();
    ProjectMutationService countMutations = new();
    RandomTraitExclusionsService countService = new(
        countMutations,
        new GameplayOperationStateService(countMutations));
    countService.Apply(
        countSource,
        countService.Discover(countSource)
            .Where(candidate => candidate.Id != "PositiveTrue")
            .Select(candidate => candidate.Id)
            .ToArray());
    ModProfileModel profile = new ModProfileWorkflowService()
        .CreateProfile(countSource, "Random trait count isolation");
    ProjectModel countTarget = RandomTraitProject();
    RandomTraitExclusionsDialogViewModel countViewModel = new(
        countTarget,
        new RandomTraitExclusionsService(
            new ProjectMutationService(),
            new GameplayOperationStateService()),
        new LocalizationService());
    AllTraits(countViewModel).Single(item => item.Id == "NegativeAbsent")
        .IsAllowed = false;
    string[] beforeIds = AllTraits(countViewModel).Select(item => item.Id).ToArray();
    string[] beforeAllowed = countViewModel.GetAllowedTraitIds()
        .OrderBy(id => id, StringComparer.Ordinal).ToArray();
    _ = new EffectiveChangeCountService().Calculate(
        countTarget,
        profile,
        out bool exact);
    Check(exact &&
          beforeIds.SequenceEqual(AllTraits(countViewModel).Select(item => item.Id)) &&
          beforeAllowed.SequenceEqual(countViewModel.GetAllowedTraitIds()
              .OrderBy(id => id, StringComparer.Ordinal)),
        "observational counting leaves Random Trait membership and pending selections untouched");

    string[] failedIds = AllTraits(countViewModel).Select(item => item.Id).ToArray();
    string[] failedAllowed = countViewModel.GetAllowedTraitIds()
        .OrderBy(id => id, StringComparer.Ordinal).ToArray();
    TestMessageDialogService failedMessages = new();
    MainViewModel failedMain = CreateMainViewModel(
        new ModProfileWorkflowService(),
        failedMessages);
    failedMain.Project = countTarget;
    failedMain.ApplyProfileForTesting(
        ProfileSummary(Path.Combine(
            Path.GetTempPath(),
            $"missing-{Guid.NewGuid():N}.wtprofile")),
        new IGameplayProjectRefreshable[] { countViewModel });
    Check(failedMessages.LastError != null &&
          failedIds.SequenceEqual(AllTraits(countViewModel).Select(item => item.Id)) &&
          failedAllowed.SequenceEqual(countViewModel.GetAllowedTraitIds()
              .OrderBy(id => id, StringComparer.Ordinal)),
        "failed production Profile Apply leaves Random Trait membership and pending selections untouched");
}

void VerifyMainViewModelProfileApplyIntegration()
{
    string temp = Path.Combine(
        Path.GetTempPath(),
        "WartalesEditorPhase5Presentation",
        Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(temp);
    try
    {
        ModProfileWorkflowService workflow = new();
        ProjectModel source = ProgressionProject();
        Check(replay.Replay(source, Request(ProfileOperationIds.CharacterXp,
            new JObject { ["percentage"] = 40 })).Succeeded,
            "state-only production fixture captures Character XP intent");
        ModProfileModel profile = workflow.CreateProfile(source, "State only");
        string profilePath = Path.Combine(temp, "state-only.wtprofile");
        workflow.Save(profile, profilePath);

        ProjectModel target = ProjectFromRoot((JObject)source.RootDocument.DeepClone());
        target.EstablishPersistedIdentity(
            source.SourceCdbGenerationIdentity!,
            source.SourceCdbGenerationIdentity,
            SourceProvenanceStatus.Verified);
        ProgressionScalingDialogViewModel dialog = new(
            target,
            new ProgressionScalingService(new ProjectMutationService()),
            new GameplayOperationStateService());
        TestMessageDialogService messages = new();
        MainViewModel main = CreateMainViewModel(workflow, messages);
        main.Project = target;
        main.ApplyProfileForTesting(ProfileSummary(profilePath), new[] { dialog });
        Check(target.GameplayOperationStates.Any(state =>
                  state.OperationType == ProgressionType.Character),
            $"actual state-only MainViewModel Profile Apply creates state ({messages.LastError})");
        Check(!target.Sheets.SelectMany(sheet => sheet.Entries)
                  .SelectMany(entry => entry.Properties)
                  .Any(property => property.IsModified),
            "actual state-only MainViewModel Profile Apply makes no property mutation");
        Check(dialog.CharacterPercentage == 40 &&
              dialog.HasTrustedCharacterBaseline,
            "actual state-only MainViewModel Profile Apply refreshes the open dialog");
        Check(messages.LastInformation?.Contains(
                  "already configured at 40%.",
                  StringComparison.Ordinal) == true,
            $"actual state-only MainViewModel Profile Apply reports semantic completion ({messages.LastInformation})");

        GameplayOperationStateModel existingState = target.GameplayOperationStates
            .Single(state => state.OperationType == ProgressionType.Character);
        messages.Clear();
        main.ApplyProfileForTesting(ProfileSummary(profilePath), new[] { dialog });
        Check(ReferenceEquals(existingState, target.GameplayOperationStates.Single()) &&
              messages.LastInformation?.Contains(
                  "already configured at 40%.",
                  StringComparison.Ordinal) == true,
            "actual true no-op Profile Apply preserves state and reports one semantic result");

        CountingRefreshable failedRefresh = new();
        string beforeFailure = target.RootDocument.ToString();
        main.ApplyProfileForTesting(
            ProfileSummary(Path.Combine(temp, "missing.wtprofile")),
            new[] { failedRefresh });
        Check(failedRefresh.Count == 0 &&
              target.RootDocument.ToString() == beforeFailure &&
              messages.LastError != null,
            "failed production Profile Apply does not refresh dialogs or mutate project");

        ModProfileWorkflowService rollbackWorkflow = new();
        ProjectModel rollbackSource = CombinedProgressionMovementProject();
        Check(replay.Replay(rollbackSource,
                  Request(ProfileOperationIds.CharacterXp,
                      new JObject { ["percentage"] = 40 })).Succeeded &&
              replay.Replay(rollbackSource,
                  Request(ProfileOperationIds.OverworldMovementSpeed,
                      new JObject { ["preset"] = "Faster" })).Succeeded,
            "rollback production fixture captures two semantic operations");
        string rollbackProfilePath = Path.Combine(temp, "rollback.wtprofile");
        rollbackWorkflow.Save(
            rollbackWorkflow.CreateProfile(rollbackSource, "Rollback"),
            rollbackProfilePath);
        ProjectModel rollbackTarget = CombinedProgressionMovementProject();
        string rollbackBefore = rollbackTarget.RootDocument.ToString();
        PropertyModel failing = rollbackTarget.Sheets.Single()
            .Entries.Single(entry => entry.Id ==
                OverworldMovementSpeedService.WalkEntryId)
            .Properties.Single(property => property.EffectivePropertyPath == "value");
        EventHandler<PropertyValueChangedEventArgs>? throwOnce = null;
        throwOnce = (_, _) =>
        {
            failing.ValueChanged -= throwOnce;
            throw new InvalidOperationException("Injected presentation integration failure.");
        };
        failing.ValueChanged += throwOnce;
        CountingRefreshable rollbackRefresh = new();
        TestMessageDialogService rollbackMessages = new();
        MainViewModel rollbackMain = CreateMainViewModel(
            rollbackWorkflow,
            rollbackMessages);
        rollbackMain.Project = rollbackTarget;
        rollbackMain.ApplyProfileForTesting(
            ProfileSummary(rollbackProfilePath),
            new[] { rollbackRefresh });
        Check(rollbackRefresh.Count == 0 &&
              rollbackTarget.RootDocument.ToString() == rollbackBefore &&
              rollbackTarget.GameplayOperationStates.Count == 0 &&
              rollbackMessages.LastError != null,
            "mutate-then-rollback production Profile Apply does not refresh dialogs");

        ProjectModel refreshFailureTarget = ProgressionProject();
        TestMessageDialogService refreshFailureMessages = new();
        MainViewModel refreshFailureMain = CreateMainViewModel(
            workflow,
            refreshFailureMessages);
        refreshFailureMain.Project = refreshFailureTarget;
        CountingRefreshable successfulRefresh = new();
        refreshFailureMain.ApplyProfileForTesting(
            ProfileSummary(profilePath),
            new IGameplayProjectRefreshable[]
            {
                new ThrowingRefreshable(),
                successfulRefresh
            });
        Check(successfulRefresh.Count == 1 &&
              refreshFailureMessages.LastError == null &&
              refreshFailureMessages.LastWarning?.Contains(
                  "applied successfully",
                  StringComparison.OrdinalIgnoreCase) == true &&
              refreshFailureTarget.GameplayOperationStates.Count == 1,
            "production Profile Apply isolates refresh failure after committing and refreshes remaining dialogs");
    }
    finally
    {
        Directory.Delete(temp, true);
    }
}

void VerifyPersistedFreshDialogs()
{
    string temp = Path.Combine(
        Path.GetTempPath(),
        "WartalesEditorPhase5Persistence",
        Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(temp);
    try
    {
        ProjectModel source = CombinedPresentationProject();
        Check(replay.Replay(source, Request(ProfileOperationIds.CharacterXp,
                  new JObject { ["percentage"] = 40 })).Succeeded &&
              replay.Replay(source, Request(ProfileOperationIds.OverworldMovementSpeed,
                  new JObject { ["preset"] = "Faster" })).Succeeded &&
              replay.Replay(source, Request(ProfileOperationIds.PositiveRandomTraits,
                  new JObject { ["preset"] = "PositiveOnly" })).Succeeded,
            "save-reopen production fixture captures representative semantic settings");
        ModProfileWorkflowService workflow = new();
        ModProfileModel profile = workflow.CreateProfile(source, "Persistence");
        ProjectModel target = CombinedPresentationProject();
        _ = workflow.ApplyProfile(target, profile);

        string cdbPath = Path.Combine(temp, "presentation.cdb");
        JsonDataService json = new();
        json.SaveProject(target, cdbPath);
        ProjectModel reopenedProject = json.LoadProject(cdbPath);

        ProjectMutationService mutations = new();
        GameplayOperationStateService states = new(mutations);
        ProgressionScalingDialogViewModel xpDialog = new(
            reopenedProject,
            new ProgressionScalingService(mutations, states),
            states);
        OverworldMovementSpeedDialogViewModel movementDialog = new(
            reopenedProject,
            new OverworldMovementSpeedService(mutations, states));
        GameplayPresetDialogViewModel traitsDialog = new(
            reopenedProject,
            new GameplayPresetService(mutations, states),
            ProgressionType.PositiveRandomTraits);
        Check(xpDialog.CharacterPercentage == 40 &&
              xpDialog.HasTrustedCharacterBaseline &&
              movementDialog.CurrentStateText == "Fast" &&
              movementDialog.CanRestorePreviousValues &&
              traitsDialog.CurrentStateText == "Positive Only" &&
              traitsDialog.CanRestorePreviousValues,
            "disk save/reopen constructs fresh dialogs from persisted gameplay state");
    }
    finally
    {
        Directory.Delete(temp, true);
    }
}

void VerifyMixedProductionSummary()
{
    ProjectModel source = AdditivePresentationProject();
    ProjectMutationService mutations = new();
    ContentCreationService content = new(mutations);
    ProjectOperationService operations = new();
    Check(operations.Execute(
              new AddCampFacilitiesOperation(content),
              source).Succeeded &&
          operations.Execute(
              new UpgradeAllEquipmentOperation(content),
              source).Succeeded,
        "mixed summary fixture applies authoritative additive operations");
    source.Sheets.Single(sheet => sheet.Name == "constant")
        .Entries.Single(entry => entry.Id == "Ordinary")
        .Properties.Single(property => property.Name == "value").Value = 2L;
    source.Sheets.Single(sheet => sheet.Name == "constant")
        .Entries.Single(entry => entry.Id == "Unavailable")
        .Properties.Single(property => property.Name == "value").Value = 2L;

    ModProfileWorkflowService workflow = new();
    ModProfileModel profile = workflow.CreateProfile(source, "Mixed summary");
    JObject targetRoot = (JObject)source.RootDocument.DeepClone();
    JObject constant = ((JArray)targetRoot["sheets"]!).OfType<JObject>()
        .Single(sheet => sheet.Value<string>("name") == "constant");
    JArray lines = (JArray)constant["lines"]!;
    lines.OfType<JObject>().Single(entry =>
        entry.Value<string>("id") == "Ordinary")["value"] = 1;
    lines.OfType<JObject>().Single(entry =>
        entry.Value<string>("id") == "Unavailable").Remove();
    ProjectModel target = ProjectFromRoot(targetRoot);

    WartalesEditor.Models.Snapshots.ModificationSnapshotImportResultModel result =
        workflow.ApplyProfile(target, profile);
    string summary = MainViewModel.BuildProfileApplySummary(result);
    Check(result.AppliedEffectiveChangeCount == 1 &&
          result.UnappliedEffectiveChangeCount == 1 &&
          summary.Contains("1 of", StringComparison.Ordinal) &&
          summary.Contains("not available", StringComparison.Ordinal) &&
          summary.Contains("Upgrade All Equipment", StringComparison.Ordinal) &&
          summary.Contains("Add Camp Facilities", StringComparison.Ordinal) &&
          summary.Contains("Anvil and Apothecary Table", StringComparison.Ordinal) &&
          !summary.Contains("fingerprint", StringComparison.OrdinalIgnoreCase) &&
          !summary.Contains("property path", StringComparison.OrdinalIgnoreCase) &&
          !summary.Contains("operation intent", StringComparison.OrdinalIgnoreCase),
        "actual mixed workflow summary reports applied, unavailable, Upgrade, and Add Camp outcomes without jargon");
}

IEnumerable<RandomTraitExclusionItemViewModel> AllTraits(
    RandomTraitExclusionsDialogViewModel viewModel) =>
    viewModel.PositiveTraits.Concat(viewModel.NegativeTraits);

ProfileOperationRequestModel Request(string id, JObject settings) => new()
{
    OperationId = id,
    Settings = settings
};

ProjectModel ProgressionProject() => CreateProject(Sheet("constant",
    ArrayEntry("LevelXpValues", 0, 200, 260, 580),
    ArrayEntry("JobXpLevels", 20, 80, 320, 1280)));

ProjectModel MovementProject() => CreateProject(Sheet("constant",
    ScalarEntry(OverworldMovementSpeedService.WalkEntryId, 6),
    ScalarEntry(OverworldMovementSpeedService.RunEntryId, 11)));

ProjectModel TraitProject() => CreateProject(Sheet("constant",
    ScalarEntry("RandomTrait1Positive1Negative", 0.25),
    ScalarEntry("RandomTrait2Positive", 0.25),
    ScalarEntry("RandomTrait1Positive", 0.25)));

ProjectModel RainProject() => CreateProject(Sheet("region",
    RainFrequencyService.Regions.Select(region => new JObject
    {
        ["id"] = region.EntryId,
        ["props"] = new JObject
        {
            ["meteo"] = new JObject
            {
                ["rainDaysPerMonth"] = region.VanillaValue
            }
        }
    }).ToArray()));

ProjectModel ScalarProject(string id, object value) =>
    CreateProject(Sheet("constant", ScalarEntry(id, value)));

ProjectModel RequestBoardProject() => CreateProject(Sheet("constant",
    RewardEntry(RequestBoardRewardsService.MinimumEntryId, 200, 175, 150, 125),
    RewardEntry(RequestBoardRewardsService.MaximumEntryId, 250, 225, 200, 150)));

ProjectModel ValourProject() => CreateProject(
    Sheet("constant",
        ScalarEntry("ActionPointBaseMax", 14),
        ScalarEntry("ActionPointGainPerSleep", 2)),
    Sheet("item",
        BonusEntry("Tent", 1),
        BonusEntry("TentT2", 2),
        BonusEntry("TentT3", 3)));

JObject Sheet(string name, params JObject[] entries) => new()
{
    ["name"] = name,
    ["lines"] = new JArray(entries)
};

JObject ScalarEntry(string id, object value) => new()
{
    ["id"] = id,
    ["value"] = JToken.FromObject(value)
};

JObject ArrayEntry(string id, params long[] values) => new()
{
    ["id"] = id,
    ["levels"] = new JArray(values.Select(value =>
        new JObject { ["xp"] = value }))
};

JObject RewardEntry(string id, params long[] values) => new()
{
    ["id"] = id,
    [RequestBoardRewardsService.PropertyPath] =
        new JArray(values.Select((value, difficulty) => new JObject
        {
            ["difficulty"] = difficulty,
            ["value"] = value
        }))
};

JObject BonusEntry(string id, int value) => new()
{
    ["id"] = id,
    ["props"] = new JObject
    {
        ["bonuses"] = new JArray
        {
            new JObject
            {
                ["bonus"] = "ActionPoint",
                ["value"] = value
            }
        }
    }
};

ProjectModel CreateProject(params JObject[] sheets)
{
    JObject root = new() { ["sheets"] = new JArray(sheets) };
    ProjectModel project = new()
    {
        FileName = "phase5-presentation.cdb",
        OriginalJson = root.ToString(),
        RootDocument = root
    };
    ProjectModelFactory factory = new();
    foreach (JObject sheet in sheets)
        project.Sheets.Add(factory.CreateSheetModel(sheet));
    string identity = new CdbGenerationIdentityService().Calculate(
        Encoding.UTF8.GetBytes(project.OriginalJson));
    project.EstablishPersistedIdentity(
        identity,
        identity,
        SourceProvenanceStatus.Verified);
    return project;
}

ProjectModel ProjectFromRoot(JObject root)
{
    return CreateProject(((JArray)root["sheets"]!).OfType<JObject>()
        .Select(sheet => (JObject)sheet.DeepClone())
        .ToArray());
}

ProjectModel CombinedProgressionMovementProject() => CreateProject(
    Sheet("constant",
        ArrayEntry("LevelXpValues", 0, 200, 260, 580),
        ArrayEntry("JobXpLevels", 20, 80, 320, 1280),
        ScalarEntry(OverworldMovementSpeedService.WalkEntryId, 6),
        ScalarEntry(OverworldMovementSpeedService.RunEntryId, 11)));

ProjectModel CombinedPresentationProject() => CreateProject(
    Sheet("constant",
        ArrayEntry("LevelXpValues", 0, 200, 260, 580),
        ArrayEntry("JobXpLevels", 20, 80, 320, 1280),
        ScalarEntry(OverworldMovementSpeedService.WalkEntryId, 6),
        ScalarEntry(OverworldMovementSpeedService.RunEntryId, 11),
        ScalarEntry("RandomTrait1Positive1Negative", 0.25),
        ScalarEntry("RandomTrait2Positive", 0.25),
        ScalarEntry("RandomTrait1Positive", 0.25)));

ProjectModel AdditivePresentationProject()
{
    Dictionary<string, JObject> items =
        UpgradeAllEquipmentTargetCatalog.EntryIds.ToDictionary(
            id => id,
            id => new JObject
            {
                ["id"] = id,
                ["props"] = new JObject { ["flags"] = 0 }
            },
            StringComparer.Ordinal);
    items["Anvil"] = new JObject
    {
        ["id"] = "Anvil",
        ["props"] = new JObject()
    };
    items["ApothecaryTable"] = new JObject
    {
        ["id"] = "ApothecaryTable",
        ["props"] = new JObject()
    };
    return CreateProject(
        Sheet("item", items.Values.ToArray()),
        Sheet("craft"),
        Sheet("constant",
            ScalarEntry("Ordinary", 1),
            ScalarEntry("Unavailable", 1)));
}

ProjectModel GenericPresetProject(ProgressionType type)
{
    GameplayPresetDefinition definition = GameplayPresetCatalog.Get(type);
    object[] targets = ((System.Collections.IEnumerable)
        typeof(GameplayPresetDefinition).GetProperty(
            "Targets",
            BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(definition)!).Cast<object>().ToArray();
    JArray sourceValues = (JArray)typeof(GameplayPresetOption).GetProperty(
        "Values",
        BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(definition.Presets[0])!;
    Dictionary<string, Dictionary<string, JObject>> sheets = new(
        StringComparer.Ordinal);

    for (int index = 0; index < targets.Length; index++)
    {
        object target = targets[index];
        Type targetType = target.GetType();
        string sheet = (string)targetType.GetProperty("Sheet")!
            .GetValue(target)!;
        string entryId = (string)targetType.GetProperty("Entry")!
            .GetValue(target)!;
        string path = (string)targetType.GetProperty("Path")!
            .GetValue(target)!;
        string? discriminator = (string?)targetType
            .GetProperty("Discriminator")!.GetValue(target);
        string? identity = (string?)targetType.GetProperty("Identity")!
            .GetValue(target);
        if (!sheets.TryGetValue(sheet, out Dictionary<string, JObject>? entries))
        {
            entries = new Dictionary<string, JObject>(StringComparer.Ordinal);
            sheets.Add(sheet, entries);
        }

        if (!entries.TryGetValue(entryId, out JObject? entry))
        {
            entry = new JObject { ["id"] = entryId };
            entries.Add(entryId, entry);
        }

        if (discriminator == null)
        {
            SetPath(entry, path, sourceValues[index]!.DeepClone());
        }
        else
        {
            EnsureArray(entry, path).Add(new JObject
            {
                [discriminator] = identity,
                ["value"] = sourceValues[index]!.DeepClone()
            });
        }
    }

    return CreateProject(sheets.Select(sheet =>
        Sheet(sheet.Key, sheet.Value.Values.ToArray())).ToArray());
}

void SetPath(JObject entry, string path, JToken value)
{
    string[] parts = path.Split('.');
    JObject parent = entry;
    for (int index = 0; index < parts.Length - 1; index++)
    {
        if (parent[parts[index]] is not JObject child)
        {
            child = new JObject();
            parent[parts[index]] = child;
        }

        parent = child;
    }

    parent[parts[^1]] = value;
}

JArray EnsureArray(JObject entry, string path)
{
    if (entry.SelectToken(path) is JArray existing)
    {
        return existing;
    }

    JArray created = new();
    SetPath(entry, path, created);
    return created;
}

ProjectModel RandomTraitProject() => CreateProject(RandomTraitSheet(
    new[]
    {
        RandomTraitEntry("StartingAnchor", null, "unsupported"),
        RandomTraitEntry("PositiveTrue", 0, true),
        RandomTraitEntry("NegativeAbsent", 1, null)
    },
    new[] { RandomTraitEntry("HiddenAnchor", null, "unsupported") },
    new[]
    {
        RandomTraitEntry("RecruitmentAnchor", null, "unsupported"),
        RandomTraitEntry("PositiveAbsent", 0, null, 2),
        RandomTraitEntry("NegativeDisabled", 1, false)
    },
    new[] { RandomTraitEntry("AcquiredAnchor", null, "unsupported") }));

JObject RandomTraitEntry(
    string id,
    int? personality,
    object? done,
    int? generationEligibility = null)
{
    JObject entry = new()
    {
        ["id"] = id,
        ["props"] = personality.HasValue
            ? new JObject { ["personality"] = personality.Value }
            : new JObject()
    };
    if (generationEligibility.HasValue)
    {
        entry["gen"] = generationEligibility.Value;
    }

    if (done != null)
    {
        entry["done"] = JToken.FromObject(done);
    }

    return entry;
}

JObject RandomTraitSheet(
    IReadOnlyList<JObject> starting,
    IReadOnlyList<JObject> hidden,
    IReadOnlyList<JObject> recruitment,
    IReadOnlyList<JObject> acquired) => new()
{
    ["name"] = "trait",
    ["columns"] = new JArray
    {
        new JObject { ["typeStr"] = "0", ["name"] = "id" },
        new JObject { ["typeStr"] = "10:Animal,NotAnimal", ["name"] = "gen", ["opt"] = true },
        new JObject { ["typeStr"] = "17", ["name"] = "props" },
        new JObject { ["typeStr"] = "2", ["name"] = "done", ["opt"] = true }
    },
    ["lines"] = new JArray(starting.Concat(hidden).Concat(recruitment).Concat(acquired)),
    ["separators"] = new JArray
    {
        new JObject { ["title"] = "Starting", ["id"] = starting[0]["id"]!.DeepClone() },
        new JObject { ["title"] = "Hidden", ["id"] = hidden[0]["id"]!.DeepClone() },
        new JObject { ["title"] = "Recruitment", ["id"] = recruitment[0]["id"]!.DeepClone() },
        new JObject { ["title"] = "Acquired", ["id"] = acquired[0]["id"]!.DeepClone() }
    }
};

void AddRandomTrait(
    ProjectModel project,
    string id,
    int personality,
    object? done,
    string group)
{
    SheetModel sheet = project.Sheets.Single(candidate => candidate.Name == "trait");
    JObject source = RandomTraitEntry(id, personality, done, 2);
    JArray lines = (JArray)sheet.SourceSheet!["lines"]!;
    string boundaryId = group == "Starting" ? "HiddenAnchor" : "AcquiredAnchor";
    int boundary = lines.OfType<JObject>().ToList().FindIndex(candidate =>
        candidate.Value<string>("id") == boundaryId);
    lines.Insert(boundary, source);
    sheet.Entries.Add(new ProjectModelFactory().CreateEntryModel(
        "trait",
        source,
        sheet.Entries.Count + 1));
}

void RemoveRandomTrait(ProjectModel project, string id)
{
    SheetModel sheet = project.Sheets.Single(candidate => candidate.Name == "trait");
    EntryModel entry = sheet.Entries.Single(candidate => candidate.Id == id);
    sheet.Entries.Remove(entry);
    entry.SourceEntry!.Remove();
}

void MoveRandomTrait(ProjectModel project, string id, string targetGroup)
{
    SheetModel sheet = project.Sheets.Single(candidate => candidate.Name == "trait");
    JObject source = sheet.Entries.Single(candidate => candidate.Id == id).SourceEntry!;
    JArray lines = (JArray)sheet.SourceSheet!["lines"]!;
    source.Remove();
    string boundaryId = targetGroup == "Starting" ? "HiddenAnchor" : "AcquiredAnchor";
    int boundary = lines.OfType<JObject>().ToList().FindIndex(candidate =>
        candidate.Value<string>("id") == boundaryId);
    lines.Insert(boundary, source);
}

ModProfileSummaryModel ProfileSummary(string path) => new()
{
    FilePath = path,
    FileName = Path.GetFileName(path),
    Name = Path.GetFileNameWithoutExtension(path)
};

MainViewModel CreateMainViewModel(
    ModProfileWorkflowService workflow,
    TestMessageDialogService messages)
{
    JsonDataService json = new();
    ModificationSnapshotWorkflowService snapshotWorkflow = new();
    ProjectMutationService mutations = new();
    ContentCreationService content = new(mutations);
    AddCampFacilitiesOperation addCamp = new(content);
    UpgradeAllEquipmentOperation upgrade = new(content);
    ProjectOperationTransactionService transactions = new();
    ProjectOperationService operations = new(
        new OperationValidatorProvider(),
        transactions);
    LocalizationService localization = new();
    return new MainViewModel(
        json,
        new SearchService(),
        localization,
        new EditHistoryService(),
        new ModificationSnapshotService(),
        snapshotWorkflow,
        new ChangeSummaryService(),
        new ModProfileLibraryService(),
        workflow,
        ReferenceDataService.Instance,
        new ValidationWorkflowService(new ValidationService(json)),
        new ValidationPresentationService(),
        operations,
        transactions,
        addCamp,
        upgrade,
        new TestFileDialogService(),
        messages,
        new LanguageDataService(
            localization,
            Path.Combine(Path.GetTempPath(), "Phase5LanguageData", Guid.NewGuid().ToString("N"))));
}

sealed class CountingRefreshable : IGameplayProjectRefreshable
{
    public int Count { get; private set; }
    public void RefreshAfterProjectOperation() => Count++;
}

sealed class ThrowingRefreshable : IGameplayProjectRefreshable
{
    public void RefreshAfterProjectOperation() =>
        throw new InvalidOperationException("simulated presentation failure");
}

sealed class TestFileDialogService : IFileDialogService
{
    public string? ShowOpenFileDialog(
        string filter,
        string? initialFileName = null) => null;

    public string? ShowSaveFileDialog(
        string filter,
        string? initialFileName = null) => null;
}

sealed class TestMessageDialogService : IMessageDialogService
{
    public string? LastInformation { get; private set; }
    public string? LastWarning { get; private set; }
    public string? LastError { get; private set; }

    public void Clear()
    {
        LastInformation = null;
        LastWarning = null;
        LastError = null;
    }

    public void ShowInformation(string message, string title) =>
        LastInformation = message;

    public void ShowWarning(string message, string title) =>
        LastWarning = message;

    public void ShowError(string message, string title) =>
        LastError = message;

    public bool ShowConfirmation(string message, string title) => true;

    public UnsavedChangesResult ShowUnsavedChanges(
        string message,
        string title) => UnsavedChangesResult.Discard;
}
