using System.Reflection;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;

int checks = 0;
ProfileOperationReplayService replay = new();
Console.WriteLine("Replay smoke: progression");

ProjectModel xp = ProgressionProject(
    new long[] { 0, 200, 260, 580 },
    new long[] { 20, 80, 320, 1280 });
ProjectOperationResult character = Replay(
    xp,
    ProfileOperationIds.CharacterXp,
    new JObject { ["percentage"] = 40 });
Check(character.Succeeded, "Character XP replay succeeds");
Check(Values(xp, "LevelXpValues").SequenceEqual(
        new long[] { 0, 80, 104, 232 }),
    "Character XP uses fresh baseline at 40 percent");
CheckFreshState(xp, ProgressionType.Character, character, 4);
Check(replay.GetOwnedSnapshotLeaves(
        xp,
        Request(ProfileOperationIds.CharacterXp,
            new JObject { ["percentage"] = 40 })).Count == 1,
    "Character XP owned array leaf identified");

ProjectOperationResult profession = Replay(
    xp,
    ProfileOperationIds.ProfessionXp,
    new JObject { ["percentage"] = 50 });
Check(profession.Succeeded, "Profession XP replay succeeds");
Check(Values(xp, "JobXpLevels").SequenceEqual(
        new long[] { 10, 40, 160, 640 }),
    "Profession XP uses fresh baseline at 50 percent");
CheckFreshState(xp, ProgressionType.Profession, profession, 4);

GameplayOperationStateModel characterSeed = State(xp, ProgressionType.Character);
new GameplayOperationStateService().RemoveState(
    xp,
    ProgressionType.Character,
    markModified: false);
xp.IsGameplayOperationStateModified = false;
ProjectOperationResult characterStateOnly = Replay(
    xp,
    ProfileOperationIds.CharacterXp,
    new JObject { ["percentage"] = 40 },
    characterSeed);
CheckStateOnly(characterStateOnly, "Character XP already-equal replay");
CheckStateOnlyHistory(
    xp,
    ProgressionType.Character,
    characterStateOnly,
    "Character XP state-only history");

ProjectModel crossSource = ProgressionProject(
    new long[] { 0, 220, 300, 700 },
    new long[] { 20, 80, 320, 1280 },
    identityMarker: 'b');
ProjectOperationResult crossSourceReplay = Replay(
    crossSource,
    ProfileOperationIds.CharacterXp,
    new JObject { ["percentage"] = 40 },
    characterSeed);
Check(crossSourceReplay.Succeeded &&
      Values(crossSource, "LevelXpValues").SequenceEqual(
          new long[] { 0, 88, 120, 280 }),
    "cross-source seed is ignored in favor of fresh target baseline");
Check(State(crossSource, ProgressionType.Character)
          .BaselineArray[1]!["xp"]!.Value<long>() == 220,
    "cross-source replay state owns the fresh target baseline");
CheckFreshState(
    crossSource,
    ProgressionType.Character,
    crossSourceReplay,
    4);

ProjectModel unverifiedTarget = MovementProject();
unverifiedTarget.EstablishPersistedIdentity(
    unverifiedTarget.CurrentCdbContentIdentity,
    null,
    SourceProvenanceStatus.Unknown);
string unverifiedBefore = Json(unverifiedTarget);
ProjectOperationResult unverifiedReplay = Replay(
    unverifiedTarget,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"));
Check(!unverifiedReplay.Succeeded &&
      Json(unverifiedTarget) == unverifiedBefore &&
      unverifiedTarget.GameplayOperationStates.Count == 0,
    "unverified target source cannot establish replay baseline authority");

ProjectModel contentMismatchTarget = MovementProject();
string contentMismatchSource =
    contentMismatchTarget.SourceCdbGenerationIdentity!;
contentMismatchTarget.EstablishPersistedIdentity(
    contentMismatchTarget.CurrentCdbContentIdentity,
    contentMismatchSource,
    SourceProvenanceStatus.ContentMismatch);
string contentMismatchBefore = Json(contentMismatchTarget);
ProjectOperationResult contentMismatchReplay = Replay(
    contentMismatchTarget,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"));
Check(!contentMismatchReplay.Succeeded &&
      Json(contentMismatchTarget) == contentMismatchBefore &&
      contentMismatchTarget.GameplayOperationStates.Count == 0,
    "content-mismatched target cannot establish replay baseline authority");

ProjectModel starting = StartingProject();
Console.WriteLine("Replay smoke: starting resources");
ProjectOperationResult startingResult = Replay(
    starting,
    ProfileOperationIds.StartingResources,
    new JObject
    {
        ["krowns"] = 100,
        ["bread"] = 2,
        ["apples"] = 3,
        ["ironOre"] = 4,
        ["wood"] = 5,
        ["cloth"] = 6
    });
Check(startingResult.Succeeded, "Starting Resources replay succeeds");
Check(Value(starting, "item", "Gold", "props.startQuantity") == 110,
    "Starting Resources applies shared additions");
Check(((JArray)Entry(starting, "startChoice", "Origin")
        .SourceEntry!.SelectToken("props.items")!)
        .OfType<JObject>().Any(item =>
            item.Value<string>("item") == "IronOre" &&
            item.Value<int>("count") == 4),
    "Starting Resources applies origin additions");
CheckFreshState(
    starting,
    ProgressionType.StartingResources,
    startingResult,
    4);
Check(replay.GetOwnedSnapshotLeaves(
        starting,
        Request(ProfileOperationIds.StartingResources, new JObject
        {
            ["krowns"] = 100,
            ["bread"] = 2,
            ["apples"] = 3,
            ["ironOre"] = 4,
            ["wood"] = 5,
            ["cloth"] = 6
        })).Count == 4,
    "Starting Resources owned leaves identified");

ProjectModel volunteer = CreateProject(
    Sheet("trait", new JObject
    {
        ["id"] = "Volunteer",
        ["desc"] = "Volunteer",
        ["props"] = new JObject { ["value"] = 10 }
    }));
ProjectOperationResult volunteerResult = Replay(
    volunteer,
    ProfileOperationIds.VolunteerWages,
    new JObject { ["volunteerPercentage"] = 100 });
Check(volunteerResult.Succeeded &&
      Value(volunteer, "trait", "Volunteer", "props.value") == 100,
    "Volunteer Wages replay succeeds");
CheckFreshState(volunteer, ProgressionType.VolunteerWages, volunteerResult, 1);
Check(replay.GetOwnedSnapshotLeaves(
        volunteer,
        Request(ProfileOperationIds.VolunteerWages,
            new JObject { ["volunteerPercentage"] = 100 })).Count == 1,
    "Volunteer Wages owned leaf identified");
Console.WriteLine("Replay smoke: party economy");

ProjectModel valour = ValourProject();
ProjectOperationResult valourResult = Replay(
    valour,
    ProfileOperationIds.ValourPoints,
    new JObject
    {
        ["maximumValour"] = 20,
        ["restoredValour"] = 8,
        ["tentTier1Valour"] = 2,
        ["tentTier2Valour"] = 6,
        ["tentTier3Valour"] = 10
    });
Check(valourResult.Succeeded &&
      Value(valour, "constant", "ActionPointBaseMax", "value") == 20,
    "Valour replay succeeds");
CheckFreshState(valour, ProgressionType.ValourPoints, valourResult, 5);
Check(replay.GetOwnedSnapshotLeaves(
        valour,
        Request(ProfileOperationIds.ValourPoints, new JObject
        {
            ["maximumValour"] = 20,
            ["restoredValour"] = 8,
            ["tentTier1Valour"] = 2,
            ["tentTier2Valour"] = 6,
            ["tentTier3Valour"] = 10
        })).Count == 5,
    "Valour owned leaves identified");

ProjectModel carrying = CarryingProject();
ProjectOperationResult carryingResult = Replay(
    carrying,
    ProfileOperationIds.CarryingCapacity,
    new JObject
    {
        ["saddlebagCapacity"] = 20,
        ["ponyStartingCapacity"] = 80,
        ["hitchingPostTier1Base"] = 20,
        ["hitchingPostTier2Base"] = 30,
        ["hitchingPostTier3Base"] = 40,
        ["hitchingPostTier1Trait"] = 0,
        ["hitchingPostTier2Trait"] = 10,
        ["hitchingPostTier3Trait"] = 20
    });
Check(carryingResult.Succeeded, "Carrying Capacity replay succeeds");
CheckFreshState(carrying, ProgressionType.CarryingCapacity, carryingResult, 7);
Check(replay.GetOwnedSnapshotLeaves(
        carrying,
        Request(ProfileOperationIds.CarryingCapacity, new JObject
        {
            ["saddlebagCapacity"] = 20,
            ["ponyStartingCapacity"] = 80,
            ["hitchingPostTier1Base"] = 20,
            ["hitchingPostTier2Base"] = 30,
            ["hitchingPostTier3Base"] = 40,
            ["hitchingPostTier1Trait"] = 0,
            ["hitchingPostTier2Trait"] = 10,
            ["hitchingPostTier3Trait"] = 20
        })).Count == 7,
    "Carrying Capacity owned leaves identified");

ProjectModel movement = MovementProject();
Console.WriteLine("Replay smoke: movement and rain");
ProjectOperationResult movementResult = Replay(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"));
Check(movementResult.Succeeded &&
      Value(movement, "constant", "PlayerBaseSpeed", "value") == 8 &&
      Value(movement, "constant", "PlayerRunSpeed", "value") == 14,
    "Run Speed Faster identifier produces 8 and 14");
CheckFreshState(
    movement,
    ProgressionType.OverworldMovementSpeed,
    movementResult,
    2);
Check(replay.GetOwnedSnapshotLeaves(
        movement,
        Request(ProfileOperationIds.OverworldMovementSpeed, Preset("Faster")))
        .Count == 2,
    "Run Speed owned leaves identified");
Check(replay.GetOwnedSnapshotLeaves(
        movement,
        Request(ProfileOperationIds.UpgradeAllEquipment, new JObject()))
        .Count == UpgradeAllEquipmentTargetCatalog.EntryIds.Count,
    "Upgrade All Equipment owned leaves reuse the authoritative catalog");

ProjectModel movementHistoryProject = MovementProject();
ProjectOperationResult movementHistoryResult = Replay(
    movementHistoryProject,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"));
ProjectOperationHistoryAction movementHistory = new(
    "Run Speed replay history",
    movementHistoryResult.MutationResult,
    new ProjectOperationTransactionService());
movementHistory.Undo();
Check(Value(movementHistoryProject, "constant", "PlayerBaseSpeed", "value") == 6 &&
      Value(movementHistoryProject, "constant", "PlayerRunSpeed", "value") == 11 &&
      movementHistoryProject.GameplayOperationStates.Count == 0,
    "single replay Undo restores properties and removes state");
movementHistory.Redo();
Check(Value(movementHistoryProject, "constant", "PlayerBaseSpeed", "value") == 8 &&
      Value(movementHistoryProject, "constant", "PlayerRunSpeed", "value") == 14 &&
      movementHistoryProject.GameplayOperationStates.Count == 1,
    "single replay Redo restores properties and state");

GameplayOperationStateModel movementSeed = State(
    movement,
    ProgressionType.OverworldMovementSpeed);
new GameplayOperationStateService().RemoveState(
    movement,
    ProgressionType.OverworldMovementSpeed,
    markModified: false);
movement.IsGameplayOperationStateModified = false;
ProjectOperationResult movementStateOnly = Replay(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    movementSeed);
CheckStateOnly(movementStateOnly, "Run Speed already-equal replay");

ProjectModel rain = RainProject();
ProjectOperationResult rainResult = Replay(
    rain,
    ProfileOperationIds.RainFrequency,
    Preset("RareRain"));
Check(rainResult.Succeeded &&
      Math.Abs(ValueDouble(
          rain,
          "region",
          RainFrequencyService.Regions[0].EntryId,
          RainFrequencyService.PropertyPath) - 1.0) < 0.000001,
    "Rain Frequency replay succeeds");
CheckFreshState(rain, ProgressionType.RainFrequency, rainResult, 12);
Check(replay.GetOwnedSnapshotLeaves(
        rain,
        Request(ProfileOperationIds.RainFrequency,
            Preset("RareRain"))).Count == 12,
    "Rain Frequency owned leaves identified");

ProgressionType[] genericTypes =
{
    ProgressionType.DeliciousMealChance,
    ProgressionType.ForgingAssistance,
    ProgressionType.MiningWoodcuttingTiming,
    ProgressionType.FishingSpeed,
    ProgressionType.LockpickingTolerance,
    ProgressionType.NinePuzzleAssistance,
    ProgressionType.RunStaminaRecovery,
    ProgressionType.BattleCameraZoom,
    ProgressionType.CampfireExpansion,
    ProgressionType.CookingPotFoodReduction,
    ProgressionType.WorkshopMaterials,
    ProgressionType.VendorRefresh,
    ProgressionType.RubySapphireValue,
    ProgressionType.TimeBetweenRests,
    ProgressionType.ResourceReplenishment,
    ProgressionType.LecternKnowledgeGain,
    ProgressionType.PositiveRandomTraits
};
Dictionary<ProgressionType, string> genericIds = new()
{
    [ProgressionType.DeliciousMealChance] = ProfileOperationIds.DeliciousMealChance,
    [ProgressionType.ForgingAssistance] = ProfileOperationIds.ForgingAssistance,
    [ProgressionType.MiningWoodcuttingTiming] = ProfileOperationIds.MiningWoodcuttingTiming,
    [ProgressionType.FishingSpeed] = ProfileOperationIds.FishingSpeed,
    [ProgressionType.LockpickingTolerance] = ProfileOperationIds.LockpickingTolerance,
    [ProgressionType.NinePuzzleAssistance] = ProfileOperationIds.NinePuzzleAssistance,
    [ProgressionType.RunStaminaRecovery] = ProfileOperationIds.RunStaminaRecovery,
    [ProgressionType.BattleCameraZoom] = ProfileOperationIds.BattleCameraZoom,
    [ProgressionType.CampfireExpansion] = ProfileOperationIds.CampfireExpansion,
    [ProgressionType.CookingPotFoodReduction] = ProfileOperationIds.CookingPotFoodReduction,
    [ProgressionType.WorkshopMaterials] = ProfileOperationIds.WorkshopMaterials,
    [ProgressionType.VendorRefresh] = ProfileOperationIds.VendorRefresh,
    [ProgressionType.RubySapphireValue] = ProfileOperationIds.RubySapphireValue,
    [ProgressionType.TimeBetweenRests] = ProfileOperationIds.TimeBetweenRests,
    [ProgressionType.ResourceReplenishment] = ProfileOperationIds.ResourceReplenishment,
    [ProgressionType.LecternKnowledgeGain] = ProfileOperationIds.LecternKnowledgeGain,
    [ProgressionType.PositiveRandomTraits] = ProfileOperationIds.PositiveRandomTraits
};

foreach (ProgressionType type in genericTypes)
{
    Console.WriteLine($"Replay smoke: {type}");
    ProjectModel project = GenericPresetProject(type);
    GameplayPresetDefinition definition = GameplayPresetCatalog.Get(type);
    string selected = definition.Presets[1].Key;
    JArray fresh = (JArray)typeof(GameplayPresetOption).GetProperty(
        "Values",
        BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(definition.Presets[0])!;
    ProjectOperationResult result = Replay(
        project,
        genericIds[type],
        Preset(selected));
    Check(result.Succeeded, $"{type} generic replay succeeds");
    CheckFreshState(project, type, result, fresh.Count);
    Check(JToken.DeepEquals(
            new JArray(State(project, type).BaselineArray
                .OfType<JObject>()
                .Select(record => record["value"]!.DeepClone())),
            fresh),
        $"{type} retains its fresh source baseline");
    Check(replay.GetOwnedSnapshotLeaves(
            project,
            Request(genericIds[type], Preset(selected))).Count == fresh.Count,
        $"{type} declares every owned snapshot leaf");
}

ProjectModel positive = GenericPresetProject(
    ProgressionType.PositiveRandomTraits);
ProjectOperationResult positiveResult = Replay(
    positive,
    ProfileOperationIds.PositiveRandomTraits,
    Preset("PositiveOnly"));
Check(positiveResult.Succeeded &&
      Math.Abs(ValueDouble(positive, "constant",
          "RandomTrait2Positive", "value") - 1.0) < 0.000001,
    "Positive Random Traits replays PositiveOnly");
GameplayOperationStateModel positiveSeed = State(
    positive,
    ProgressionType.PositiveRandomTraits);
new GameplayOperationStateService().RemoveState(
    positive,
    ProgressionType.PositiveRandomTraits,
    markModified: false);
positive.IsGameplayOperationStateModified = false;
ProjectOperationResult positiveStateOnly = Replay(
    positive,
    ProfileOperationIds.PositiveRandomTraits,
    Preset("PositiveOnly"),
    positiveSeed);
CheckStateOnly(
    positiveStateOnly,
    "Positive Random Traits already-equal replay");

ProjectModel positiveAlreadyEqual = GenericPresetProject(
    ProgressionType.PositiveRandomTraits,
    "PositiveOnly");
ProjectOperationResult positiveAlreadyEqualResult = Replay(
    positiveAlreadyEqual,
    ProfileOperationIds.PositiveRandomTraits,
    Preset("PositiveOnly"));
CheckStateOnly(
    positiveAlreadyEqualResult,
    "Generic preset fresh already-equal replay");

ProjectModel rewards = RequestBoardProject();
Console.WriteLine("Replay smoke: request board");
ProjectOperationResult rewardsResult = Replay(
    rewards,
    ProfileOperationIds.RequestBoardRewards,
    new JObject { ["percentage"] = 150 });
Check(rewardsResult.Succeeded &&
      RewardValue(rewards, RequestBoardRewardsService.MinimumEntryId, 0) == 300,
    "Request Board replay uses generalized contract");
Check(replay.GetOwnedSnapshotLeaves(
        rewards,
        Request(ProfileOperationIds.RequestBoardRewards,
            new JObject { ["percentage"] = 150 })).Count == 2,
    "Request Board owned array leaves identified");
GameplayOperationStateModel rewardsSeed = State(
    rewards,
    ProgressionType.RequestBoardRewards);
new GameplayOperationStateService().RemoveState(
    rewards,
    ProgressionType.RequestBoardRewards,
    markModified: false);
rewards.IsGameplayOperationStateModified = false;
ProjectOperationResult rewardsStateOnly = Replay(
    rewards,
    ProfileOperationIds.RequestBoardRewards,
    new JObject { ["percentage"] = 150 },
    rewardsSeed);
CheckStateOnly(rewardsStateOnly, "Request Board already-equal replay");

ProjectModel traits = TraitProject(includeNew: false);
Console.WriteLine("Replay smoke: random traits");
JArray traitIntent = TraitIntent(
    ("NegativeAbsent", "Negative", "Starting", false),
    ("PositiveTrue", "Positive", "Starting", true),
    ("NegativeDisabled", "Negative", "Recruitment", false),
    ("PositiveAbsent", "Positive", "Recruitment", true));
ProjectOperationResult traitResult = Replay(
    traits,
    ProfileOperationIds.RandomTraitExclusions,
    new JObject { ["traits"] = traitIntent });
Check(traitResult.Succeeded, "Random Trait semantic replay succeeds");
CheckFreshState(
    traits,
    ProgressionType.RandomTraitExclusions,
    traitResult,
    4);
Check(replay.GetOwnedSnapshotLeaves(
        traits,
        Request(ProfileOperationIds.RandomTraitExclusions,
            new JObject { ["traits"] = traitIntent.DeepClone() })).Count == 4,
    "Random Trait Exclusions owned leaves use semantic trait IDs");

ProjectModel traitsWithNew = TraitProject(includeNew: true);
ProjectOperationResult newTraitResult = Replay(
    traitsWithNew,
    ProfileOperationIds.RandomTraitExclusions,
    new JObject { ["traits"] = traitIntent });
Check(newTraitResult.Succeeded &&
      Entry(traitsWithNew, "trait", "NewCandidate")
          .SourceEntry!.Property("done") == null,
    "new Random Trait candidate preserves fresh default");

ProjectModel missingTrait = TraitProject(includeNew: false);
Entry(missingTrait, "trait", "NegativeAbsent").SourceEntry!.Remove();
missingTrait.Sheets.Single(sheet => sheet.Name == "trait")
    .Entries.Remove(Entry(missingTrait, "trait", "NegativeAbsent"));
string missingBefore = Json(missingTrait);
ProjectOperationResult missingTraitResult = Replay(
    missingTrait,
    ProfileOperationIds.RandomTraitExclusions,
    new JObject { ["traits"] = traitIntent });
Check(!missingTraitResult.Succeeded && Json(missingTrait) == missingBefore &&
      missingTrait.GameplayOperationStates.Count == 0,
    "missing semantic trait fails before mutation");

ProjectModel changedTrait = TraitProject(includeNew: false);
Entry(changedTrait, "trait", "PositiveTrue")
    .SourceEntry!.SelectToken("props.personality")!.Replace(1);
string changedBefore = Json(changedTrait);
ProjectOperationResult changedTraitResult = Replay(
    changedTrait,
    ProfileOperationIds.RandomTraitExclusions,
    new JObject { ["traits"] = traitIntent });
Check(!changedTraitResult.Succeeded && Json(changedTrait) == changedBefore &&
      changedTrait.GameplayOperationStates.Count == 0,
    "changed semantic trait identity fails before mutation");

Console.WriteLine("Replay smoke: exact-source seed validation");
CheckInvalidSeed(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    AlterState(movementSeed, value =>
        value[nameof(GameplayOperationStateModel.TargetSheet)] = "item"),
    "wrong exact-source TargetSheet");
CheckInvalidSeed(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    AlterState(movementSeed, value =>
        value[nameof(GameplayOperationStateModel.TargetEntry)] = "Wrong"),
    "wrong exact-source TargetEntry");
CheckInvalidSeed(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    AlterState(movementSeed, value =>
        value[nameof(GameplayOperationStateModel.TargetPath)] = "wrong.path"),
    "wrong exact-source TargetPath");
CheckInvalidSeed(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    AlterState(movementSeed, value =>
    {
        JArray baseline = (JArray)value[
            nameof(GameplayOperationStateModel.BaselineArray)]!;
        baseline[0]!["unexpectedShape"] = true;
        value[nameof(GameplayOperationStateModel.BaselineFingerprint)] =
            GameplayOperationFingerprintService.CreateContentFingerprint(
                baseline);
        value[nameof(GameplayOperationStateModel.ElementShapeFingerprint)] =
            GameplayOperationFingerprintService.CreateShapeFingerprint(
                baseline);
    }),
    "wrong internally fingerprinted exact-source target shape");
CheckInvalidSeed(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    AlterState(movementSeed, value =>
        value[nameof(GameplayOperationStateModel.BaselineFingerprint)] =
            "corrupted"),
    "corrupted exact-source baseline fingerprint");
CheckInvalidSeed(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    AlterState(movementSeed, value =>
        value[nameof(GameplayOperationStateModel.ExpectedCurrentFingerprint)] =
            "corrupted"),
    "corrupted exact-source expected-current fingerprint");

ProjectModel staleSeedTarget = MovementProject();
CheckInvalidSeed(
    staleSeedTarget,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    movementSeed,
    "exact-source seed whose current target no longer matches expected state");
CheckInvalidSeed(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    AlterState(movementSeed, value =>
        value[nameof(GameplayOperationStateModel.OperationType)] =
            (int)ProgressionType.RainFrequency),
    "wrong exact-source operation type");
CheckInvalidSeed(
    xp,
    ProfileOperationIds.CharacterXp,
    new JObject { ["percentage"] = 50 },
    characterSeed,
    "same-source XP percentage mismatch");
CheckInvalidSeed(
    movement,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("VeryFast"),
    movementSeed,
    "same-source movement preset mismatch");
GameplayOperationStateModel volunteerSeed = State(
    volunteer,
    ProgressionType.VolunteerWages);
CheckInvalidSeed(
    volunteer,
    ProfileOperationIds.VolunteerWages,
    new JObject { ["volunteerPercentage"] = 50 },
    volunteerSeed,
    "same-source Party Economy settings mismatch");
CheckInvalidSeed(
    positive,
    ProfileOperationIds.PositiveRandomTraits,
    Preset("Vanilla"),
    positiveSeed,
    "same-source generic preset mismatch");
GameplayOperationStateModel traitSeed = State(
    traits,
    ProgressionType.RandomTraitExclusions);
JArray mismatchedTraitIntent = (JArray)traitIntent.DeepClone();
mismatchedTraitIntent[0]!["allowed"] =
    !mismatchedTraitIntent[0]!.Value<bool>("allowed");
CheckInvalidSeed(
    traits,
    ProfileOperationIds.RandomTraitExclusions,
    new JObject { ["traits"] = mismatchedTraitIntent },
    traitSeed,
    "same-source random-trait semantic selection mismatch");
CheckInvalidSeed(
    rewards,
    ProfileOperationIds.RequestBoardRewards,
    new JObject { ["percentage"] = 200 },
    rewardsSeed,
    "same-source Request Board percentage mismatch");

ProjectModel replayFailure = MovementProject();
GameplayOperationStateModel malformedSeed = movementSeed.DeepClone();
malformedSeed.BaselineArray[0]!["entry"] = "Wrong";
string failureBefore = Json(replayFailure);
ProjectOperationResult failedReplay = Replay(
    replayFailure,
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"),
    malformedSeed);
Check(!failedReplay.Succeeded && Json(replayFailure) == failureBefore &&
      replayFailure.GameplayOperationStates.Count == 0,
    "replay failure leaves no mutations or orphaned state");

ProjectModel workflowTarget = MovementProject();
Console.WriteLine("Replay smoke: Phase 3 workflow integration");
ModProfileModel phaseOneProfile = Profile(
    workflowTarget,
    Request(ProfileOperationIds.OverworldMovementSpeed, Preset("Faster")));
ModificationSnapshotImportResultModel workflowResult =
    new ModProfileWorkflowService().ApplyProfile(
    workflowTarget,
    phaseOneProfile);
Check(Value(workflowTarget, "constant", "PlayerBaseSpeed", "value") == 8 &&
      Value(workflowTarget, "constant", "PlayerRunSpeed", "value") == 14 &&
      workflowTarget.GameplayOperationStates.Count == 1 &&
      workflowResult.MutationResult.WasModified,
    "Profile Apply executes Phase 2 replay through Phase 3 integration");

Console.WriteLine(
    $"ALL PROFILE OPERATION REPLAY CHECKS PASSED ({checks})");
return;

ProjectOperationResult Replay(
    ProjectModel project,
    string operationId,
    JObject settings,
    GameplayOperationStateModel? seed = null)
{
    ProjectOperationResult result =
        replay.Replay(project, Request(operationId, settings), seed);
    if (!result.Succeeded)
        Console.WriteLine($"Replay failure ({operationId}): {result.Message}");
    return result;
}

void CheckFreshState(
    ProjectModel project,
    ProgressionType type,
    ProjectOperationResult result,
    int expectedBaselineCount)
{
    GameplayOperationStateModel state = State(project, type);
    Check(result.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
        $"{type} journals one state mutation");
    Check(state.BaselineArray.Count == expectedBaselineCount,
        $"{type} captures complete baseline");
    Check(state.ProjectCompatibilityIdentity ==
          project.SourceCdbGenerationIdentity &&
          state.LocalRestoreContentIdentity ==
          project.CurrentCdbContentIdentity,
        $"{type} state is bound to target identities");
}

void CheckStateOnly(ProjectOperationResult result, string name)
{
    Check(result.Succeeded && result.MutationResult.WasModified,
        $"{name} succeeds with a journal");
    Check(result.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
        $"{name} records state rollback");
    Check(result.MutationResult.UpdatedProperties.Count == 0 &&
          result.MutationResult.CreatedProperties.Count == 0 &&
          result.MutationResult.RemovedProperties.Count == 0,
        $"{name} creates no fake property mutation");
}

void CheckStateOnlyHistory(
    ProjectModel project,
    ProgressionType type,
    ProjectOperationResult result,
    string name)
{
    ProjectOperationHistoryAction history = new(
        name,
        result.MutationResult,
        new ProjectOperationTransactionService());
    history.Undo();
    Check(project.GameplayOperationStates.All(state =>
            state.OperationType != type),
        $"{name} Undo removes fresh state");
    history.Redo();
    Check(project.GameplayOperationStates.Any(state =>
            state.OperationType == type),
        $"{name} Redo restores fresh state");
}

void CheckInvalidSeed(
    ProjectModel project,
    string operationId,
    JObject settings,
    GameplayOperationStateModel seed,
    string name)
{
    string beforeJson = Json(project);
    JArray beforeStates = JArray.FromObject(
        project.GameplayOperationStates);
    bool beforeProjectModified = project.IsModified;
    bool beforeStateModified = project.IsGameplayOperationStateModified;

    ProjectOperationResult result = Replay(
        project,
        operationId,
        settings,
        seed);

    Check(!result.Succeeded &&
          !result.MutationResult.WasModified &&
          Json(project) == beforeJson &&
          JToken.DeepEquals(
              JArray.FromObject(project.GameplayOperationStates),
              beforeStates) &&
          project.IsModified == beforeProjectModified &&
          project.IsGameplayOperationStateModified == beforeStateModified,
        $"{name} rejects without mutation, state, or history journal");
}

void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"FAILED: {name}");
    checks++;
}

static ProfileOperationRequestModel Request(
    string operationId,
    JObject settings) => new()
{
    OperationId = operationId,
    Settings = settings
};

static JObject Preset(string key) => new() { ["preset"] = key };

static GameplayOperationStateModel State(
    ProjectModel project,
    ProgressionType type) =>
    project.GameplayOperationStates.Single(state =>
        state.OperationType == type).DeepClone();

static GameplayOperationStateModel AlterState(
    GameplayOperationStateModel source,
    Action<JObject> alter)
{
    JObject serialized = JObject.FromObject(source);
    alter(serialized);
    return serialized.ToObject<GameplayOperationStateModel>()
        ?? throw new InvalidOperationException(
            "The altered gameplay state could not be created.");
}

static ProjectModel ProgressionProject(
    IReadOnlyList<long> character,
    IReadOnlyList<long> profession,
    char identityMarker = 'a') =>
    CreateProjectWithIdentity(identityMarker,
        Sheet("constant",
            ArrayEntry("LevelXpValues", "levels", "xp", character),
            ArrayEntry("JobXpLevels", "levels", "xp", profession)));

static long[] Values(ProjectModel project, string id) =>
    ((JArray)Entry(project, "constant", id).SourceEntry!["levels"]!)
        .OfType<JObject>()
        .Select(item => item.Value<long>("xp"))
        .ToArray();

static JObject ArrayEntry(
    string id,
    string path,
    string valueName,
    IEnumerable<long> values) => new()
{
    ["id"] = id,
    [path] = new JArray(values.Select(value =>
        new JObject { [valueName] = value }))
};

static ProjectModel StartingProject() => CreateProjectWithIdentity('c',
    Sheet("item",
        StartItem("Gold", 10),
        StartItem("Bread", 1),
        StartItem("Apple", 1),
        new JObject { ["id"] = "IronOre" },
        new JObject { ["id"] = "Wood" },
        new JObject { ["id"] = "Cloth" }),
    Sheet("startChoice", new JObject
    {
        ["id"] = "Origin",
        ["desc"] = "Origin",
        ["introText"] = "Intro",
        ["props"] = new JObject
        {
            ["pattern"] = "Pattern",
            ["items"] = new JArray()
        }
    }),
    Sheet("unitPattern", new JObject { ["id"] = "Pattern" }));

static JObject StartItem(string id, int quantity) => new()
{
    ["id"] = id,
    ["props"] = new JObject
    {
        ["startQuantity"] = quantity,
        ["startQuantityDifficultyBonus"] = 0
    }
};

static ProjectModel ValourProject() => CreateProjectWithIdentity('d',
    Sheet("constant",
        Scalar("ActionPointBaseMax", 14),
        Scalar("ActionPointGainPerSleep", 2)),
    Sheet("item",
        Bonus("Tent", "props", "bonuses", "bonus", "ActionPoint", 1),
        Bonus("TentT2", "props", "bonuses", "bonus", "ActionPoint", 2),
        Bonus("TentT3", "props", "bonuses", "bonus", "ActionPoint", 3)));

static ProjectModel CarryingProject() => CreateProjectWithIdentity('e',
    Sheet("item",
        ArrayObject("AnimAccCarriage", "baseBonus",
            new JObject { ["attribute"] = "Transport", ["value"] = 10 }),
        PersonalBonus("PonyAuge", 10, null),
        PersonalBonus("PonyAugeT2", 10, 5),
        PersonalBonus("PonyAugeT3", 10, 10)),
    Sheet("unitClass",
        ArrayObject("Pony", "stats",
            new JObject { ["attribute"] = "Transport", ["value"] = 55 })));

static JObject PersonalBonus(string id, int baseValue, int? traitValue)
{
    JArray values = new(new JObject
    {
        ["bonus"] = "PonyAugeTransport",
        ["value"] = baseValue
    });
    if (traitValue.HasValue)
        values.Add(new JObject
        {
            ["bonus"] = "PonyAugeTransportTrait",
            ["value"] = traitValue.Value
        });
    return new JObject
    {
        ["id"] = id,
        ["tool"] = new JObject { ["personalBonuses"] = values }
    };
}

static ProjectModel MovementProject() => CreateProjectWithIdentity('f',
    Sheet("constant", Scalar("PlayerBaseSpeed", 6), Scalar("PlayerRunSpeed", 11)));

static ProjectModel RainProject() => CreateProjectWithIdentity('1',
    Sheet("region", RainFrequencyService.Regions.Select(region =>
        new JObject
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

static ProjectModel RequestBoardProject() => CreateProjectWithIdentity('2',
    Sheet("constant",
        RewardEntry(RequestBoardRewardsService.MinimumEntryId,
            (0, 200L), (1, 100L)),
        RewardEntry(RequestBoardRewardsService.MaximumEntryId,
            (0, 250L), (1, 150L))));

static JObject RewardEntry(string id, params (int Difficulty, long Value)[] values) =>
    new()
    {
        ["id"] = id,
        [RequestBoardRewardsService.PropertyPath] = new JArray(
            values.Select(value => new JObject
            {
                ["difficulty"] = value.Difficulty,
                ["value"] = value.Value
            }))
    };

static long RewardValue(ProjectModel project, string id, int difficulty) =>
    ((JArray)Entry(project, "constant", id).SourceEntry![
        RequestBoardRewardsService.PropertyPath]!)
        .OfType<JObject>().Single(item =>
            item.Value<int>("difficulty") == difficulty)
        .Value<long>("value");

static ProjectModel TraitProject(bool includeNew)
{
    List<JObject> recruitment = new()
    {
        Trait("RecruitmentAnchor", null, "unsupported"),
        Trait("PositiveAbsent", 0, null),
        Trait("NegativeDisabled", 1, false)
    };
    if (includeNew)
        recruitment.Add(Trait("NewCandidate", 0, null));
    return CreateProjectWithIdentity('3', new JObject
    {
        ["name"] = "trait",
        ["lines"] = new JArray(
            new[]
            {
                Trait("PositiveTrue", 0, true),
                Trait("NegativeAbsent", 1, null),
                Trait("HiddenAnchor", null, "unsupported")
            }.Concat(recruitment).Concat(new[]
            {
                Trait("AcquiredAnchor", null, "unsupported")
            })),
        ["separators"] = new JArray
        {
            Separator("Starting", "PositiveTrue"),
            Separator("Hidden", "HiddenAnchor"),
            Separator("Recruitment", "RecruitmentAnchor"),
            Separator("Acquired", "AcquiredAnchor")
        }
    });
}

static JObject Trait(string id, int? personality, object? done)
{
    JObject value = new()
    {
        ["id"] = id,
        ["props"] = personality.HasValue
            ? new JObject { ["personality"] = personality.Value }
            : new JObject()
    };
    if (done != null) value["done"] = JToken.FromObject(done);
    return value;
}

static JObject Separator(string title, string id) => new()
{
    ["title"] = title,
    ["id"] = id
};

static JArray TraitIntent(
    params (string Id, string Personality, string Group, bool Allowed)[] values) =>
    new(values.OrderBy(value => value.Id, StringComparer.Ordinal)
        .Select(value => new JObject
        {
            ["id"] = value.Id,
            ["personality"] = value.Personality,
            ["group"] = value.Group,
            ["allowed"] = value.Allowed
        }));

static ProjectModel GenericPresetProject(
    ProgressionType type,
    string sourcePresetKey = "Vanilla")
{
    GameplayPresetDefinition definition = GameplayPresetCatalog.Get(type);
    object[] targets = ((System.Collections.IEnumerable)
        typeof(GameplayPresetDefinition).GetProperty(
            "Targets",
            BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(definition)!).Cast<object>().ToArray();
    GameplayPresetOption sourcePreset = definition.Presets.Single(option =>
        string.Equals(option.Key, sourcePresetKey, StringComparison.Ordinal));
    JArray sourceValues = (JArray)typeof(GameplayPresetOption).GetProperty(
        "Values",
        BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(sourcePreset)!;
    Dictionary<string, Dictionary<string, JObject>> sheets = new(
        StringComparer.Ordinal);

    for (int index = 0; index < targets.Length; index++)
    {
        object target = targets[index];
        Type targetType = target.GetType();
        string sheet = (string)targetType.GetProperty("Sheet")!.GetValue(target)!;
        string entryId = (string)targetType.GetProperty("Entry")!.GetValue(target)!;
        string path = (string)targetType.GetProperty("Path")!.GetValue(target)!;
        string? discriminator = (string?)targetType.GetProperty("Discriminator")!.GetValue(target);
        string? identity = (string?)targetType.GetProperty("Identity")!.GetValue(target);
        if (!sheets.TryGetValue(sheet, out Dictionary<string, JObject>? entries))
            sheets.Add(sheet, entries = new(StringComparer.Ordinal));
        if (!entries.TryGetValue(entryId, out JObject? entry))
            entries.Add(entryId, entry = new JObject { ["id"] = entryId });

        if (discriminator == null)
        {
            SetPath(entry, path, sourceValues[index]!.DeepClone());
        }
        else
        {
            JArray array = EnsureArray(entry, path);
            array.Add(new JObject
            {
                [discriminator] = identity,
                ["value"] = sourceValues[index]!.DeepClone()
            });
        }
    }

    return CreateProjectWithIdentity('4', sheets.Select(sheet =>
        Sheet(sheet.Key, sheet.Value.Values.ToArray())).ToArray());
}

static void SetPath(JObject entry, string path, JToken value)
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

static JArray EnsureArray(JObject entry, string path)
{
    JToken? existing = entry.SelectToken(path);
    if (existing is JArray array) return array;
    JArray created = new();
    SetPath(entry, path, created);
    return created;
}

static ModProfileModel Profile(
    ProjectModel project,
    ProfileOperationRequestModel request) => new()
{
    FormatVersion = 4,
    SourceCdbGenerationIdentity = project.SourceCdbGenerationIdentity,
    Metadata = new ModProfileMetadataModel
    {
        Name = "Phase 2 boundary",
        ProfileVersion = "1.0",
        CreatedAtUtc = DateTimeOffset.UtcNow,
        ModifiedAtUtc = DateTimeOffset.UtcNow
    },
    Snapshot = new ModificationSnapshotModel
    {
        SourceFileName = "fixture.cdb",
        SourceCdbGenerationIdentity = project.SourceCdbGenerationIdentity
    },
    OperationRequests = new() { request }
};

static JObject Scalar(string id, object value) => new()
{
    ["id"] = id,
    ["value"] = JToken.FromObject(value)
};

static JObject Bonus(
    string id,
    string container,
    string path,
    string discriminator,
    string identity,
    int value) => new()
{
    ["id"] = id,
    [container] = new JObject
    {
        [path] = new JArray(new JObject
        {
            [discriminator] = identity,
            ["value"] = value
        })
    }
};

static JObject ArrayObject(string id, string path, JObject value) => new()
{
    ["id"] = id,
    [path] = new JArray(value)
};

static JObject Sheet(string name, params JObject[] entries) => new()
{
    ["name"] = name,
    ["lines"] = new JArray(entries)
};

static ProjectModel CreateProject(
    params JObject[] sheets) => CreateProjectWithIdentity('9', sheets);

static ProjectModel CreateProjectWithIdentity(
    char identityMarker,
    params JObject[] sheets)
{
    JObject root = new() { ["sheets"] = new JArray(sheets) };
    ProjectModel project = new()
    {
        RootDocument = root,
        OriginalJson = root.ToString(),
        FileName = "profile-replay-fixture.cdb"
    };
    ProjectModelFactory factory = new();
    foreach (JObject sheet in sheets)
        project.Sheets.Add(factory.CreateSheetModel(sheet));
    string identity = "sha256:" + new string(identityMarker, 64);
    project.EstablishPersistedIdentity(
        identity,
        identity,
        SourceProvenanceStatus.Verified);
    return project;
}

static EntryModel Entry(
    ProjectModel project,
    string sheet,
    string id) =>
    project.Sheets.Single(candidate => candidate.Name == sheet)
        .Entries.Single(candidate => candidate.Id == id);

static long Value(
    ProjectModel project,
    string sheet,
    string id,
    string path) =>
    Entry(project, sheet, id).SourceEntry!.SelectToken(path)!.Value<long>();

static double ValueDouble(
    ProjectModel project,
    string sheet,
    string id,
    string path) =>
    Entry(project, sheet, id).SourceEntry!.SelectToken(path)!.Value<double>();

static string Json(ProjectModel project) =>
    project.RootDocument.ToString(Newtonsoft.Json.Formatting.None);
