using System.IO;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.ViewModels;

int checks = 0;
Console.WriteLine("Phase 3 atomic Profile Apply: changed source");
Check((int)ProfileOperationApplyStatus.Applied == 0 &&
      (int)ProfileOperationApplyStatus.AlreadyConfigured == 1 &&
      (int)ProfileOperationApplyStatus.Failed == 2 &&
      (int)ProfileOperationApplyStatus.Unsupported == 3 &&
      (int)ProfileOperationApplyStatus.Unavailable == 4,
    "profile operation result statuses preserve established numeric values");

ProjectModel changedTarget = CombinedProject('b');
ModProfileModel changedProfile = CombinedProfile('a');
string changedBefore = Json(changedTarget);
ModificationSnapshotImportResultModel changedResult =
    new ModProfileWorkflowService().ApplyProfile(
        changedTarget,
        changedProfile);

Check(Values(changedTarget, "LevelXpValues").SequenceEqual(
    new long[] { 0, 120, 160 }), "changed-source Character XP uses target baseline");
Check(Values(changedTarget, "JobXpLevels").SequenceEqual(
    new long[] { 10, 50, 250 }), "changed-source Profession XP uses target baseline");
Check(Value(changedTarget, "PlayerBaseSpeed") == 8,
    "changed-source Run Speed walk is 8");
Check(Value(changedTarget, "PlayerRunSpeed") == 14,
    "changed-source Run Speed run is 14");
Check(ValueDouble(changedTarget, "RandomTrait1Positive1Negative") == 0,
    "Positive Traits mixed branch disabled");
Check(ValueDouble(changedTarget, "RandomTrait2Positive") == 1,
    "Positive Traits positive branch enabled");
Check(ValueDouble(changedTarget, "RandomTrait1Positive") == 0,
    "Positive Traits single-positive branch disabled");
Check(ValueDouble(changedTarget, "GainOnLecternRest") == 150,
    "Lectern replay scales fresh target baseline");
CheckClose(ValueDouble(changedTarget, "GatherRefillSlow"), 0.6,
    "Resource slow refill scales fresh target baseline");
CheckClose(ValueDouble(changedTarget, "GatherRefillNormal"), 1.5,
    "Resource normal refill scales fresh target baseline");
CheckClose(ValueDouble(changedTarget, "GatherRefillFast"), 4.2,
    "Resource fast refill scales fresh target baseline");
Check(Value(changedTarget, "UnrelatedSetting") == 9,
    "ordinary unowned snapshot leaf still applies");
Check(changedTarget.GameplayOperationStates.Count == 6,
    "six stateful intents create six fresh states");
Check(changedTarget.GameplayOperationStates.All(state =>
        state.ProjectCompatibilityIdentity == Identity('b')),
    "all fresh states bind to target source");
Check(changedTarget.GameplayOperationStates.All(state =>
        state.ProjectCompatibilityIdentity != Identity('a')),
    "old source identity is never rebound");
Check(State(changedTarget, ProgressionType.Character)
        .BaselineArray[1]!["xp"]!.Value<long>() == 300,
    "Character state owns fresh target baseline");
Check(State(changedTarget, ProgressionType.LecternKnowledgeGain)
        .BaselineArray[0]!["value"]!.Value<double>() == 30,
    "Lectern state owns fresh target baseline");
Check(State(changedTarget, ProgressionType.ResourceReplenishment)
        .BaselineArray[0]!["value"]!.Value<double>() == 0.2,
    "Resource state owns fresh target baseline");
Check(changedResult.OperationResults.Count == 6,
    "each stateful request executes exactly once");
Check(changedResult.OperationResults.Select(result => result.OperationId)
        .Distinct(StringComparer.Ordinal).Count() == 6,
    "operation results contain no duplicate replay");
Check(changedResult.ApplyResult.AppliedCount == 1,
    "only the ordinary snapshot leaf reaches raw snapshot apply");
Check(changedResult.ApplyResult.TotalCount == 1,
    "owned raw snapshot leaves are excluded from snapshot accounting");
Check(changedResult.MutationResult.GameplayOperationStateRollbackRecords.Count == 6,
    "all replay-created states join the outer journal");
Check(changedResult.MutationResult.WasModified,
    "mixed profile reports a modification");

ProjectOperationHistoryAction history = new(
    "Phase 3 mixed profile",
    changedResult.MutationResult,
    new ProjectOperationTransactionService());
history.Undo();
Check(Json(changedTarget) == changedBefore,
    "one Undo restores all raw properties");
Check(changedTarget.GameplayOperationStates.Count == 0,
    "one Undo removes all fresh states");
Check(Value(changedTarget, "UnrelatedSetting") == 1,
    "Undo restores ordinary snapshot property");
history.Redo();
Check(Value(changedTarget, "PlayerRunSpeed") == 14 &&
      Value(changedTarget, "UnrelatedSetting") == 9,
    "one Redo restores replay and snapshot results");
Check(changedTarget.GameplayOperationStates.Count == 6,
    "one Redo restores all gameplay state");

ModificationSnapshotImportResultModel repeated =
    new ModProfileWorkflowService().ApplyProfile(
        changedTarget,
        changedProfile);
Check(!repeated.MutationResult.WasModified,
    "matching state and target produce a true no-op");
Check(repeated.OperationResults.All(result =>
        result.Status == ProfileOperationApplyStatus.AlreadyConfigured),
    "true no-op operations are classified already configured");

Console.WriteLine("Phase 3 atomic Profile Apply: state-only authority");
ProjectModel sameSource = MovementProject('c', 7, 12);
ProfileOperationRequestModel faster = Request(
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"));
ProjectOperationResult sourceApplied =
    new ProfileOperationReplayService().Replay(sameSource, faster);
Check(sourceApplied.Succeeded, "same-source seed fixture applies");
GameplayOperationStateModel retained = State(
    sameSource,
    ProgressionType.OverworldMovementSpeed).DeepClone();
ProjectModel sameTarget = MovementProject('c', 8, 14);
ModProfileModel exactProfile = Profile(
    sameSource,
    new[] { faster },
    new[] { retained });
ModificationSnapshotImportResultModel exactResult =
    new ModProfileWorkflowService().ApplyProfile(
        sameTarget,
        exactProfile);
Check(exactResult.MutationResult.UpdatedProperties.Count == 0,
    "valid exact-source matching target creates no fake property mutation");
Check(exactResult.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "valid exact-source matching target creates one state record");
Check(State(sameTarget, ProgressionType.OverworldMovementSpeed)
        .BaselineArray[0]!["value"]!.Value<int>() == 7,
    "exact-source baseline fidelity is preserved");
Check(exactResult.OperationResults.Single().Status ==
      ProfileOperationApplyStatus.AlreadyConfigured,
    "state-only replay is classified already configured");
int stateOnlyImpact = new ProfileEffectiveChangeCountService().Calculate(
    MovementProject('c', 8, 14), exactProfile, out bool stateOnlyExact);
Check(stateOnlyExact && stateOnlyImpact == 0,
    "complete state-only target-context evaluation is exact with zero property changes");

ProjectOperationHistoryAction stateOnlyHistory = new(
    "State-only profile",
    exactResult.MutationResult,
    new ProjectOperationTransactionService());
stateOnlyHistory.Undo();
Check(sameTarget.GameplayOperationStates.Count == 0 &&
      Value(sameTarget, "PlayerBaseSpeed") == 8,
    "state-only Undo removes authority without changing raw values");
stateOnlyHistory.Redo();
Check(sameTarget.GameplayOperationStates.Count == 1 &&
      Value(sameTarget, "PlayerRunSpeed") == 14,
    "state-only Redo restores authority without fake raw changes");

ProjectModel equalTarget = MovementProject('d', 8, 14);
ModProfileModel equalProfile = Profile(
    MovementProject('e', 6, 11),
    new[] { faster });
ModificationSnapshotImportResultModel equalResult =
    new ModProfileWorkflowService().ApplyProfile(equalTarget, equalProfile);
Check(equalResult.MutationResult.UpdatedProperties.Count == 0,
    "already-equal changed-source target has no property mutation");
Check(equalResult.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "already-equal changed-source target establishes fresh state");
Check(State(equalTarget, ProgressionType.OverworldMovementSpeed)
        .BaselineArray[0]!["value"]!.Value<int>() == 8,
    "already-equal state captures the target value as fresh baseline");

Console.WriteLine("Phase 3 atomic Profile Apply: isolated structural impact");
ProfileOperationRequestModel addCampRequest = new()
{
    OperationId = ProfileOperationIds.AddCampFacilities
};
ProjectModel addCampTarget = AdditiveProject('1');
ProfileImpactManifestModel? addCampManifest = EstablishImpact(
    addCampTarget,
    new[] { addCampRequest });
ModificationSnapshotImportResultModel addCampResult =
    new ModProfileWorkflowService().ApplyProfile(
        addCampTarget,
        Profile(AdditiveProject('2'), new[] { addCampRequest }));
int addCampMutationCount = new EffectiveChangeCountService().Calculate(
    addCampResult.MutationResult);
Check(addCampManifest != null &&
      addCampManifest.TotalCount == addCampMutationCount,
    "isolated Add Camp manifest count equals its actual distinct mutation count");
Check(addCampManifest!.Leaves.Any(leaf =>
          leaf.EntryId == "Anvil" &&
          leaf.PropertyPath.StartsWith("tool.", StringComparison.Ordinal)) &&
      addCampManifest.Leaves.Any(leaf =>
          leaf.EntryId == "ApothecaryTable" &&
          leaf.PropertyPath.StartsWith("icon.", StringComparison.Ordinal)),
    "isolated Add Camp manifest contains representative facility leaves");
Check(!addCampManifest.Leaves.Any(leaf =>
          UpgradeAllEquipmentTargetCatalog.EntryIds.Contains(leaf.EntryId) &&
          leaf.PropertyPath == "props.flags"),
    "isolated Add Camp manifest contains no Upgrade All leaves");

ProfileOperationRequestModel upgradeAllRequest = new()
{
    OperationId = ProfileOperationIds.UpgradeAllEquipment
};
ProjectModel upgradeAllTarget = AdditiveProject('3');
ProfileImpactManifestModel? upgradeAllManifest = EstablishImpact(
    upgradeAllTarget,
    new[] { upgradeAllRequest });
ModificationSnapshotImportResultModel upgradeAllResult =
    new ModProfileWorkflowService().ApplyProfile(
        upgradeAllTarget,
        Profile(AdditiveProject('4'), new[] { upgradeAllRequest }));
int upgradeAllMutationCount = new EffectiveChangeCountService().Calculate(
    upgradeAllResult.MutationResult);
Check(upgradeAllManifest != null &&
      upgradeAllManifest.TotalCount == upgradeAllMutationCount,
    "isolated Upgrade All manifest count equals its actual distinct mutation count");
Check(upgradeAllManifest!.Leaves.Any(leaf =>
          UpgradeAllEquipmentTargetCatalog.EntryIds.Contains(leaf.EntryId) &&
          leaf.PropertyPath == "props.flags") &&
      upgradeAllManifest.Leaves.All(leaf =>
          leaf.PropertyPath == "props.flags"),
    "isolated Upgrade All manifest contains representative equipment leaves only");
Check(!upgradeAllManifest.Leaves.Any(leaf =>
          (leaf.EntryId == "Anvil" ||
           leaf.EntryId == "ApothecaryTable") &&
          (leaf.PropertyPath.StartsWith("tool.", StringComparison.Ordinal) ||
           leaf.PropertyPath.StartsWith("icon.", StringComparison.Ordinal))),
    "isolated Upgrade All manifest contains no Add Camp leaves");
Console.WriteLine(
    $"Isolated structural counts: Add Camp {addCampMutationCount}, " +
    $"Upgrade All {upgradeAllMutationCount}");

Console.WriteLine("Phase 3 atomic Profile Apply: additive ordering");
ProjectModel additiveTarget = AdditiveProject('f');
ProjectModel additiveSource = AdditiveProject('e');
ModProfileModel additiveProfile = Profile(
    additiveSource,
    new[]
    {
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.UpgradeAllEquipment
        },
        Request(ProfileOperationIds.OverworldMovementSpeed,
            Preset("Faster")),
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.AddCampFacilities
        }
    });
ProfileImpactManifestModel? additiveManifest = EstablishImpact(
    additiveTarget,
    additiveProfile.OperationRequests);
ModificationSnapshotImportResultModel additiveResult =
    new ModProfileWorkflowService().ApplyProfile(
        additiveTarget,
        additiveProfile);
Check(additiveResult.OperationResults.Select(result => result.OperationId)
        .SequenceEqual(new[]
        {
            ProfileOperationIds.AddCampFacilities,
            ProfileOperationIds.OverworldMovementSpeed,
            ProfileOperationIds.UpgradeAllEquipment
        }),
    "structural, stateful, then remaining additive ordering is deterministic");
Check(EntryIn(additiveTarget, "Anvil", "item").SourceEntry!["tool"] is JObject &&
      EntryIn(additiveTarget, "ApothecaryTable", "item").SourceEntry!["icon"] is JObject,
    "Add Camp Facilities structural output is applied");
Check(additiveTarget.Sheets.Single(sheet => sheet.Name == "craft")
        .Entries.Count == 2,
    "Add Camp Facilities creates both craft entries");
Check(UpgradeAllEquipmentTargetCatalog.EntryIds.All(id =>
        (EntryIn(additiveTarget, id, "item").SourceEntry!["props"]!["flags"]!
            .Value<int>() & 128) == 128),
    "Upgrade All Equipment applies to the complete current catalog");
Check(additiveResult.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "mixed additive profile journals stateful authority in the same result");
Check(additiveManifest != null &&
      additiveManifest.TotalCount ==
          new EffectiveChangeCountService().Calculate(
              additiveResult.MutationResult),
    "Add Camp and Upgrade All manifest counts canonical property leaves without CreatedEntry inflation");
Check(additiveManifest!.Leaves.Any(leaf =>
          leaf.EntryId == "Anvil" &&
          leaf.PropertyPath.StartsWith("tool.", StringComparison.Ordinal)) &&
      additiveManifest.Leaves.Any(leaf =>
          UpgradeAllEquipmentTargetCatalog.EntryIds.Contains(leaf.EntryId) &&
          leaf.PropertyPath == "props.flags"),
    "structural manifest includes real Add Camp and Upgrade All output leaves");

Console.WriteLine("Phase 3 atomic Profile Apply: save and reopen");
string persistenceDirectory = Path.Combine(
    Path.GetTempPath(),
    $"wartales-phase3-persistence-{Guid.NewGuid():N}");
try
{
    Directory.CreateDirectory(persistenceDirectory);
    string cdbPath = Path.Combine(persistenceDirectory, "profile-applied.cdb");
    JsonDataService dataService = new();
    dataService.SaveProject(changedTarget, cdbPath);
    string sidecarPath = cdbPath +
        GameplayOperationStatePersistenceService.SidecarExtension;
    string sidecar = File.ReadAllText(sidecarPath);
    Check(File.Exists(sidecarPath) && sidecar.Contains(
            Identity('b'), StringComparison.Ordinal),
        "save writes target-source authority to the .wtstate sidecar");
    Check(!sidecar.Contains(Identity('a'), StringComparison.Ordinal),
        "old profile-source authority is absent from the persisted sidecar");
    ProjectModel reopened = dataService.LoadProject(cdbPath);
    Check(reopened.GameplayOperationStates.Count == 6,
        "save and reopen retains every profile-created state");
    Check(reopened.GameplayOperationStates.All(state =>
            state.ProjectCompatibilityIdentity == Identity('b')),
        "save and reopen retains target-source restore authority");
    Check(Value(reopened, "PlayerRunSpeed") == 14 &&
          Value(reopened, "UnrelatedSetting") == 9,
        "save and reopen retains deterministic replay and snapshot output");
    Check(!reopened.IsModified && !reopened.IsGameplayOperationStateModified,
        "save and reopen accepts persisted profile output as clean baseline");
}
finally
{
    if (Directory.Exists(persistenceDirectory))
        Directory.Delete(persistenceDirectory, recursive: true);
}

Console.WriteLine("Phase 3 atomic Profile Apply: feature families");
ModificationSnapshotImportResultModel startingResult = ApplyIntent(
    StartingProject('6'),
    ProfileOperationIds.StartingResources,
    new JObject
    {
        ["krowns"] = 100,
        ["bread"] = 3,
        ["apples"] = 2,
        ["ironOre"] = 4,
        ["wood"] = 5,
        ["cloth"] = 6
    });
Check(startingResult.MutationResult.WasModified,
    "Starting Resources replays through Profile Apply");
Check(startingResult.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "Starting Resources creates target-bound state");

ModificationSnapshotImportResultModel volunteerResult = ApplyIntent(
    VolunteerProject('7'),
    ProfileOperationIds.VolunteerWages,
    new JObject { ["volunteerPercentage"] = 100 });
Check(volunteerResult.MutationResult.UpdatedProperties.Count == 1,
    "Volunteer Wages replays one target");

ModificationSnapshotImportResultModel valourResult = ApplyIntent(
    ValourProject('8'),
    ProfileOperationIds.ValourPoints,
    new JObject
    {
        ["maximumValour"] = 20,
        ["restoredValour"] = 5,
        ["tentTier1Valour"] = 2,
        ["tentTier2Valour"] = 4,
        ["tentTier3Valour"] = 6
    });
Check(valourResult.MutationResult.UpdatedProperties.Count == 5,
    "Valour Points compound settings replay atomically");

ModificationSnapshotImportResultModel carryingResult = ApplyIntent(
    CarryingProject('9'),
    ProfileOperationIds.CarryingCapacity,
    new JObject
    {
        ["saddlebagCapacity"] = 20,
        ["ponyStartingCapacity"] = 80,
        ["hitchingPostTier1Base"] = 15,
        ["hitchingPostTier2Base"] = 20,
        ["hitchingPostTier3Base"] = 25,
        ["hitchingPostTier1Trait"] = 0,
        ["hitchingPostTier2Trait"] = 8,
        ["hitchingPostTier3Trait"] = 12
    });
Check(carryingResult.MutationResult.UpdatedProperties.Count == 7,
    "Carrying Capacity compound targets replay atomically");

ProjectModel rainTarget = RainProject('a');
ModificationSnapshotImportResultModel rainResult = ApplyIntent(
    rainTarget,
    ProfileOperationIds.RainFrequency,
    Preset("RareRain"));
Check(rainResult.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "Rain Frequency creates fresh state");
Check(rainTarget.GameplayOperationStates.Single().GameplaySettings!["preset"]!
        .Value<string>() == "RareRain",
    "Rain Frequency preserves stable preset intent");

ProjectModel rewardTarget = RequestBoardProject('b');
ModificationSnapshotImportResultModel rewardResult = ApplyIntent(
    rewardTarget,
    ProfileOperationIds.RequestBoardRewards,
    new JObject { ["percentage"] = 150 });
Check(rewardResult.OperationResults.Count == 1,
    "Request Board explicit request executes once");
Check(RewardValue(rewardTarget,
        RequestBoardRewardsService.MinimumEntryId, 0) == 300,
    "Request Board uses target baseline percentage");
Check(rewardResult.ApplyResult.TotalCount == 0,
    "Request Board owned arrays do not replay as raw leaves");

JArray traitSelections = TraitIntent(
    ("PositiveTrue", "Positive", "Starting", false),
    ("NegativeAbsent", "Negative", "Starting", true),
    ("PositiveAbsent", "Positive", "Recruitment", false),
    ("NegativeDisabled", "Negative", "Recruitment", true));
ProjectModel traitTarget = TraitProject('c', includeNew: true);
ModificationSnapshotImportResultModel traitResult = ApplyIntent(
    traitTarget,
    ProfileOperationIds.RandomTraitExclusions,
    new JObject { ["traits"] = traitSelections });
Check(traitResult.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "Random Trait Exclusions creates fresh state");
Check(EntryIn(traitTarget, "PositiveAbsent", "trait")
        .SourceEntry!["done"]!.Value<bool>() == false,
    "Random Trait semantic exclusion applies by stable ID");
Check(EntryIn(traitTarget, "NewCandidate", "trait")
        .SourceEntry!.Property("done") == null,
    "new Random Trait candidate preserves fresh default");

ProjectModel missingTraitTarget = TraitProject('d', includeNew: false);
JArray missingSelection = (JArray)traitSelections.DeepClone();
missingSelection.Add(new JObject
{
    ["id"] = "MissingCandidate",
    ["personality"] = "Positive",
    ["group"] = "Recruitment",
    ["allowed"] = false
});
missingSelection = new JArray(missingSelection
    .OfType<JObject>()
    .OrderBy(selection => selection.Value<string>("id"),
        StringComparer.Ordinal));
string missingTraitBefore = Json(missingTraitTarget);
ModificationSnapshotImportResultModel partialTraitResult = ApplyChangedTraitIntent(
    missingTraitTarget,
    missingSelection);
ProfileOperationApplyItemResultModel partialTraitOperation =
    partialTraitResult.OperationResults.Single();
Check(partialTraitOperation.Status == ProfileOperationApplyStatus.Applied &&
      partialTraitOperation.HasWarning &&
      partialTraitOperation.Message.Contains("MissingCandidate", StringComparison.Ordinal) &&
      !partialTraitOperation.Message.Contains("baseline", StringComparison.OrdinalIgnoreCase),
    "changed-source replay skips genuinely absent traits with a player-safe warning");
Check(Json(missingTraitTarget) != missingTraitBefore &&
      missingTraitTarget.GameplayOperationStates.Count == 1 &&
      !State(missingTraitTarget, ProgressionType.RandomTraitExclusions)
          .TargetEntry.Contains("MissingCandidate", StringComparison.Ordinal) &&
      missingSelection.OfType<JObject>().Any(selection =>
          selection.Value<string>("id") == "MissingCandidate"),
    "partial availability applies compatible traits and owns only current candidates");
Check(MainViewModel.BuildProfileApplySummary(partialTraitResult).Contains(
        "MissingCandidate", StringComparison.Ordinal),
    "partial availability reaches the final Profile Apply summary");
string partialTraitAfter = Json(missingTraitTarget);
ProjectOperationHistoryAction partialTraitHistory = new(
    "Partial Random Trait profile",
    partialTraitResult.MutationResult,
    new ProjectOperationTransactionService());
partialTraitHistory.Undo();
Check(Json(missingTraitTarget) == missingTraitBefore &&
      missingTraitTarget.GameplayOperationStates.Count == 0,
    "partial availability Undo restores properties and state atomically");
partialTraitHistory.Redo();
Check(Json(missingTraitTarget) == partialTraitAfter &&
      missingTraitTarget.GameplayOperationStates.Count == 1,
    "partial availability Redo restores properties and state atomically");

ProjectModel allMissingTarget = TraitProject('e', includeNew: false);
string allMissingBefore = Json(allMissingTarget);
ModificationSnapshotImportResultModel allMissingResult = ApplyChangedTraitIntent(
    allMissingTarget,
    TraitIntent(("RemovedByUpdate", "Positive", "Hidden", false)));
ProfileOperationApplyItemResultModel allMissingOperation =
    allMissingResult.OperationResults.Single();
Check(allMissingOperation.Status == ProfileOperationApplyStatus.Unavailable &&
      allMissingOperation.HasWarning &&
      !allMissingResult.MutationResult.WasModified &&
      allMissingResult.AppliedEffectiveChangeCount == 0 &&
      allMissingResult.OperationsUnavailableCount == 1 &&
      allMissingResult.UnappliedEffectiveChangeCount == 0 &&
      Json(allMissingTarget) == allMissingBefore &&
      allMissingTarget.GameplayOperationStates.Count == 0,
    "all-unavailable changed-source replay records no mutation, state, or undo work");
Check(MainViewModel.BuildProfileApplySummary(allMissingResult).Contains(
        "No changes were applied.", StringComparison.Ordinal) &&
      !MainViewModel.BuildProfileApplySummary(allMissingResult).Contains(
          "already applied", StringComparison.OrdinalIgnoreCase),
    "all-unavailable Profile Apply summary is explicit and does not claim success");
LocalizationService missingTraitLocalization = new();
missingTraitLocalization.Apply(new LocalizationPreparation(
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["RemovedByUpdate"] = "Retired Recruit Trait"
    }));
ProfileOperationReplayResult localizedUnavailable =
    new ProfileOperationReplayService(missingTraitLocalization)
        .ReplayForProfile(
            TraitProject('b', includeNew: false),
            Request(
                ProfileOperationIds.RandomTraitExclusions,
                new JObject
                {
                    ["traits"] = TraitIntent(
                        ("RemovedByUpdate", "Positive", "Hidden", false))
                }),
            null,
            ProfileReplaySourceContext.ChangedSource);
Check(localizedUnavailable.OperationResult.Succeeded &&
      localizedUnavailable.OperationResult.Message!.Contains(
          "Retired Recruit Trait", StringComparison.Ordinal) &&
      !localizedUnavailable.OperationResult.Message.Contains(
          "RemovedByUpdate", StringComparison.Ordinal),
    "unavailable reporting resolves a localized name when available");

ProjectModel mixedUnavailableTarget = TraitAndMovementProject('b', 7, 12);
string mixedUnavailableBefore = Json(mixedUnavailableTarget);
ProfileOperationRequestModel unavailableTraitRequest = Request(
    ProfileOperationIds.RandomTraitExclusions,
    new JObject
    {
        ["traits"] = TraitIntent(
            ("RemovedByUpdate", "Positive", "Hidden", false))
    });
ProfileOperationRequestModel movementRequest = Request(
    ProfileOperationIds.OverworldMovementSpeed,
    Preset("Faster"));
ModificationSnapshotImportResultModel mixedUnavailableResult =
    new ModProfileWorkflowService().ApplyProfile(
        mixedUnavailableTarget,
        Profile(
            TraitAndMovementProject('a', 6, 11),
            new[] { unavailableTraitRequest, movementRequest }));
Check(Value(mixedUnavailableTarget, "PlayerBaseSpeed") == 8 &&
      Value(mixedUnavailableTarget, "PlayerRunSpeed") == 14 &&
      mixedUnavailableTarget.GameplayOperationStates.Count == 1 &&
      mixedUnavailableTarget.GameplayOperationStates.Single().OperationType ==
          ProgressionType.OverworldMovementSpeed &&
      mixedUnavailableResult.OperationResults.Any(result =>
          result.Status == ProfileOperationApplyStatus.Unavailable) &&
      mixedUnavailableResult.OperationResults.Any(result =>
          result.Status == ProfileOperationApplyStatus.Applied),
    "all-unavailable Random Traits does not block another compatible profile operation");
ProjectOperationHistoryAction mixedUnavailableHistory = new(
    "Mixed unavailable profile",
    mixedUnavailableResult.MutationResult,
    new ProjectOperationTransactionService());
mixedUnavailableHistory.Undo();
Check(Json(mixedUnavailableTarget) == mixedUnavailableBefore &&
      mixedUnavailableTarget.GameplayOperationStates.Count == 0,
    "mixed unavailable profile retains one atomic Undo for actual mutations");

ProjectModel driftedTraitTarget = TraitProject('f', includeNew: false);
EntryIn(driftedTraitTarget, "PositiveAbsent", "trait")
    .SourceEntry!.SelectToken("props.personality")!.Replace(1);
string driftedTraitBefore = Json(driftedTraitTarget);
CheckThrows<InvalidOperationException>(
    () => ApplyChangedTraitIntent(
        driftedTraitTarget,
        traitSelections),
    "present but semantically drifted Random Trait fails changed-source preflight");
Check(Json(driftedTraitTarget) == driftedTraitBefore &&
      driftedTraitTarget.GameplayOperationStates.Count == 0,
    "semantic drift failure remains mutation-neutral");

ProjectModel groupDriftTarget = TraitProject('b', includeNew: false);
SheetModel groupDriftSheet = groupDriftTarget.Sheets.Single(sheet =>
    sheet.Name == "trait");
JObject groupDriftSource = EntryIn(
    groupDriftTarget, "PositiveAbsent", "trait").SourceEntry!;
((JObject)groupDriftSource["props"]!)["recruitWeight"] = 0.5;
groupDriftSource.Remove();
JArray groupDriftLines = (JArray)groupDriftSheet.SourceSheet!["lines"]!;
int groupRecruitmentAnchor = groupDriftLines.OfType<JObject>().ToList()
    .FindIndex(entry => entry.Value<string>("id") == "RecruitmentAnchor");
groupDriftLines.Insert(groupRecruitmentAnchor, groupDriftSource);
string groupDriftBefore = Json(groupDriftTarget);
CheckThrows<InvalidOperationException>(
    () => ApplyChangedTraitIntent(groupDriftTarget, traitSelections),
    "same canonical Random Trait ID with semantic-group drift fails preflight");
Check(Json(groupDriftTarget) == groupDriftBefore,
    "semantic-group drift failure is mutation-neutral");

ProjectModel ineligibleTraitTarget = TraitProject('c', includeNew: false);
SheetModel ineligibleTraitSheet = ineligibleTraitTarget.Sheets.Single(sheet =>
    sheet.Name == "trait");
JObject ineligibleSource = EntryIn(
    ineligibleTraitTarget, "PositiveAbsent", "trait").SourceEntry!;
ineligibleSource.Remove();
JArray ineligibleLines = (JArray)ineligibleTraitSheet.SourceSheet!["lines"]!;
int ineligibleRecruitmentAnchor = ineligibleLines.OfType<JObject>().ToList()
    .FindIndex(entry => entry.Value<string>("id") == "RecruitmentAnchor");
ineligibleLines.Insert(ineligibleRecruitmentAnchor, ineligibleSource);
string ineligibleBefore = Json(ineligibleTraitTarget);
CheckThrows<InvalidOperationException>(
    () => ApplyChangedTraitIntent(ineligibleTraitTarget, traitSelections),
    "present but no-longer-eligible Random Trait fails changed-source preflight");
Check(Json(ineligibleTraitTarget) == ineligibleBefore,
    "present-but-ineligible failure is mutation-neutral");

ProjectModel duplicateTraitTarget = TraitProject('d', includeNew: false);
SheetModel duplicateTraitSheet = duplicateTraitTarget.Sheets.Single(sheet =>
    sheet.Name == "trait");
JObject duplicateTraitSource = (JObject)EntryIn(
    duplicateTraitTarget, "PositiveAbsent", "trait").SourceEntry!.DeepClone();
JArray duplicateTraitLines = (JArray)duplicateTraitSheet.SourceSheet!["lines"]!;
int duplicateAcquiredAnchor = duplicateTraitLines.OfType<JObject>().ToList()
    .FindIndex(entry => entry.Value<string>("id") == "AcquiredAnchor");
duplicateTraitLines.Insert(duplicateAcquiredAnchor, duplicateTraitSource);
duplicateTraitSheet.Entries.Add(new ProjectModelFactory().CreateEntryModel(
    "trait", duplicateTraitSource, duplicateTraitSheet.Entries.Count + 1));
string duplicateTraitBefore = Json(duplicateTraitTarget);
CheckThrows<InvalidOperationException>(
    () => ApplyChangedTraitIntent(duplicateTraitTarget, traitSelections),
    "ambiguous duplicate destination Random Trait identity fails preflight");
Check(Json(duplicateTraitTarget) == duplicateTraitBefore,
    "duplicate destination identity failure is mutation-neutral");

ProjectModel mixedDuplicateTarget = TraitAndMovementProject(
    'g', 7, 12);
SheetModel mixedDuplicateSheet = mixedDuplicateTarget.Sheets.Single(sheet =>
    sheet.Name == "trait");
JObject mixedDuplicateSource = (JObject)EntryIn(
    mixedDuplicateTarget, "PositiveAbsent", "trait").SourceEntry!.DeepClone();
JArray mixedDuplicateLines =
    (JArray)mixedDuplicateSheet.SourceSheet!["lines"]!;
int mixedDuplicateAcquiredAnchor = mixedDuplicateLines
    .OfType<JObject>()
    .ToList()
    .FindIndex(entry => entry.Value<string>("id") == "AcquiredAnchor");
mixedDuplicateLines.Insert(
    mixedDuplicateAcquiredAnchor + 1,
    mixedDuplicateSource);
mixedDuplicateSheet.Entries.Add(new ProjectModelFactory().CreateEntryModel(
    "trait", mixedDuplicateSource, mixedDuplicateSheet.Entries.Count + 1));
ProfileOperationRequestModel mixedDuplicateRequest = Request(
    ProfileOperationIds.RandomTraitExclusions,
    new JObject { ["traits"] = traitSelections.DeepClone() });
ModProfileModel mixedDuplicateProfile = Profile(
    MovementProject('0', 6, 11),
    new[] { mixedDuplicateRequest });
mixedDuplicateProfile.Snapshot.Categories.Add(
    new ModificationSnapshotCategoryModel
    {
        Name = "constant",
        Settings = new()
        {
            SnapshotSetting("PlayerBaseSpeed", "value", 7, 99)
        }
    });
string mixedDuplicateBefore = Json(mixedDuplicateTarget);
EditHistoryService mixedDuplicateHistory = new();
InvalidOperationException mixedDuplicateFailure =
    CheckThrows<InvalidOperationException>(
        () =>
        {
            ModificationSnapshotImportResultModel result =
                new ModProfileWorkflowService().ApplyProfile(
                    mixedDuplicateTarget,
                    mixedDuplicateProfile);
            if (result.MutationResult.WasModified)
            {
                mixedDuplicateHistory.Record(new ProjectOperationHistoryAction(
                    "Mixed duplicate profile",
                    result.MutationResult,
                    new ProjectOperationTransactionService()));
            }
        },
        "mixed eligible/noneligible duplicate destination identity fails preflight");
Check(mixedDuplicateFailure.ToString().Contains(
          "ambiguous identities", StringComparison.Ordinal) &&
      !mixedDuplicateFailure.ToString().Contains(
          "not available", StringComparison.OrdinalIgnoreCase),
    "mixed duplicate failure is strict ambiguity rather than unavailability");
Check(Json(mixedDuplicateTarget) == mixedDuplicateBefore &&
      Value(mixedDuplicateTarget, "PlayerBaseSpeed") == 7 &&
      mixedDuplicateTarget.GameplayOperationStates.Count == 0 &&
      !mixedDuplicateHistory.CanUndo &&
      !mixedDuplicateHistory.CanRedo,
    "mixed duplicate atomic Apply leaves traits, state, snapshots, and history unchanged");

ProjectModel exactMissingTarget = TraitProject('a', includeNew: false);
string exactMissingBefore = Json(exactMissingTarget);
ProfileOperationRequestModel exactMissingRequest = Request(
    ProfileOperationIds.RandomTraitExclusions,
    new JObject
    {
        ["traits"] = TraitIntent(
            ("RemovedByUpdate", "Positive", "Hidden", false))
    });
CheckThrows<InvalidOperationException>(
    () => new ModProfileWorkflowService().ApplyProfile(
        exactMissingTarget,
        Profile(TraitProject('a', includeNew: false),
            new[] { exactMissingRequest })),
    "exact-source profile replay remains strict for missing traits");
Check(Json(exactMissingTarget) == exactMissingBefore,
    "exact-source missing-trait failure is mutation-neutral");

Console.WriteLine("Phase 3 atomic Profile Apply: legacy and failure paths");
ProjectModel replayFailureTarget = CombinedProject('7');
ModProfileModel replayFailureProfile = Profile(
    CombinedProject('6'),
    new[]
    {
        Request(ProfileOperationIds.CharacterXp,
            new JObject { ["percentage"] = 40 }),
        Request(ProfileOperationIds.OverworldMovementSpeed,
            Preset("Faster"))
    });
string replayFailureBefore = Json(replayFailureTarget);
PropertyModel failingReplayProperty = Entry(
    replayFailureTarget,
    "PlayerBaseSpeed").Properties.Single(property =>
        property.EffectivePropertyPath == "value");
EventHandler<PropertyValueChangedEventArgs>? throwReplayOnce = null;
throwReplayOnce = (_, _) =>
{
    failingReplayProperty.ValueChanged -= throwReplayOnce;
    throw new InvalidOperationException("Injected replay publication failure.");
};
failingReplayProperty.ValueChanged += throwReplayOnce;
CheckThrows<InvalidOperationException>(
    () => new ModProfileWorkflowService().ApplyProfile(
        replayFailureTarget,
        replayFailureProfile),
    "failure during replay is surfaced");
Check(Json(replayFailureTarget) == replayFailureBefore &&
      replayFailureTarget.GameplayOperationStates.Count == 0,
    "failure during replay rolls back earlier replay properties and state");

ProjectModel additiveFailureTarget = AdditiveProject('9');
ModProfileModel additiveFailureProfile = Profile(
    AdditiveProject('8'),
    new[]
    {
        Request(ProfileOperationIds.OverworldMovementSpeed,
            Preset("Faster")),
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.UpgradeAllEquipment
        }
    });
string additiveFailureBefore = Json(additiveFailureTarget);
EntryModel failingEquipment = UpgradeAllEquipmentTargetCatalog.EntryIds
    .Select(id => EntryIn(additiveFailureTarget, id, "item"))
    .First();
PropertyModel failingFlag = failingEquipment.Properties.Single(property =>
    property.EffectivePropertyPath == "props.flags");
EventHandler<PropertyValueChangedEventArgs>? throwAdditiveOnce = null;
throwAdditiveOnce = (_, _) =>
{
    failingFlag.ValueChanged -= throwAdditiveOnce;
    throw new InvalidOperationException("Injected additive publication failure.");
};
failingFlag.ValueChanged += throwAdditiveOnce;
CheckThrows<InvalidOperationException>(
    () => new ModProfileWorkflowService().ApplyProfile(
        additiveFailureTarget,
        additiveFailureProfile),
    "failure during additive execution is surfaced");
Check(Json(additiveFailureTarget) == additiveFailureBefore &&
      additiveFailureTarget.GameplayOperationStates.Count == 0,
    "failure during additive execution rolls back all earlier changes");

ProjectModel legacySource = MovementProject('f', 6, 11);
Check(new ProfileOperationReplayService().Replay(
        legacySource,
        faster).Succeeded,
    "legacy state fixture applies");
GameplayOperationStateModel legacyState = State(
    legacySource,
    ProgressionType.OverworldMovementSpeed).DeepClone();
ModProfileModel legacyProfile = Profile(
    legacySource,
    Array.Empty<ProfileOperationRequestModel>(),
    new[] { legacyState },
    formatVersion: 3);
ProjectModel legacyTarget = MovementProject('1', 6, 11);
ModificationSnapshotImportResultModel legacyResult =
    new ModProfileWorkflowService().ApplyProfile(
        legacyTarget,
        legacyProfile);
Check(Value(legacyTarget, "PlayerBaseSpeed") == 8 &&
      Value(legacyTarget, "PlayerRunSpeed") == 14,
    "Format 3 safely projects and replays legacy intent");
Check(legacyTarget.GameplayOperationStates.Count == 1,
    "Format 3 replay creates fresh target state");
Check(State(legacyTarget, ProgressionType.OverworldMovementSpeed)
        .ProjectCompatibilityIdentity == Identity('1'),
    "Format 3 state binds only to changed target source");

GameplayOperationStateModel malformed = AlterState(
    legacyState,
    value => ((JObject)value[
        nameof(GameplayOperationStateModel.GameplaySettings)]!)["preset"] =
        "Unsupported");
ModProfileModel malformedProfile = Profile(
    legacySource,
    Array.Empty<ProfileOperationRequestModel>(),
    new[] { malformed },
    formatVersion: 3);
ProjectModel malformedTarget = MovementProject('2', 6, 11);
string malformedBefore = Json(malformedTarget);
CheckThrows<InvalidOperationException>(
    () => new ModProfileWorkflowService().ApplyProfile(
        malformedTarget,
        malformedProfile),
    "malformed legacy stateful intent fails preflight");
Check(Json(malformedTarget) == malformedBefore &&
      malformedTarget.GameplayOperationStates.Count == 0,
    "malformed legacy preflight failure is mutation-neutral");

ModProfileModel snapshotFailureProfile = SnapshotFailureProfile('3');
ProjectModel snapshotFailureTarget = MovementProjectWithOrdinary('4', 6, 11, 1);
string snapshotFailureBefore = Json(snapshotFailureTarget);
PropertyModel failingProperty = Entry(
    snapshotFailureTarget,
    "UnrelatedSetting").Properties.Single(property =>
        property.EffectivePropertyPath == "value");
EventHandler<PropertyValueChangedEventArgs>? throwOnce = null;
throwOnce = (_, _) =>
{
    failingProperty.ValueChanged -= throwOnce;
    throw new InvalidOperationException("Injected snapshot publication failure.");
};
failingProperty.ValueChanged += throwOnce;
CheckThrows<InvalidOperationException>(
    () => new ModProfileWorkflowService().ApplyProfile(
        snapshotFailureTarget,
        snapshotFailureProfile),
    "snapshot apply failure is surfaced");
Check(Json(snapshotFailureTarget) == snapshotFailureBefore,
    "snapshot failure rolls back earlier replay properties");
Check(snapshotFailureTarget.GameplayOperationStates.Count == 0,
    "snapshot failure rolls back earlier replay state");
Check(!snapshotFailureTarget.IsGameplayOperationStateModified,
    "snapshot failure restores gameplay-state modified flag");

ProjectModel missingTarget = MovementProject('5', 6, 11);
missingTarget.Sheets.Single().Entries.RemoveAt(1);
string missingBefore = Json(missingTarget);
CheckThrows<InvalidOperationException>(
    () => new ModProfileWorkflowService().ApplyProfile(
        missingTarget,
        equalProfile),
    "incompatible stateful target fails complete preflight");
Check(Json(missingTarget) == missingBefore &&
      missingTarget.GameplayOperationStates.Count == 0,
    "incompatible target preflight leaves no partial mutation");

Console.WriteLine($"ALL PHASE 3 ATOMIC PROFILE APPLY CHECKS PASSED ({checks})");
return;

void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"FAILED: {name}");
    checks++;
}

void CheckClose(double actual, double expected, string name) =>
    Check(Math.Abs(actual - expected) < 0.000001, name);

TException CheckThrows<TException>(Action action, string name)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException exception)
    {
        checks++;
        return exception;
    }

    throw new InvalidOperationException($"FAILED: {name}");
}

static ModProfileModel CombinedProfile(char identityMarker)
{
    ProjectModel source = CombinedProject(identityMarker);
    return new ModProfileModel
    {
        FormatVersion = 4,
        SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity,
        Metadata = Metadata("Phase 3 changed source"),
        Snapshot = new ModificationSnapshotModel
        {
            SourceFileName = "phase3-source.cdb",
            SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity,
            Categories = new()
            {
                new ModificationSnapshotCategoryModel
                {
                    Name = "constant",
                    Settings = new()
                    {
                        SnapshotSetting("LevelXpValues", "levels",
                            Levels(0, 200, 260), Levels(0, 80, 104)),
                        SnapshotSetting("JobXpLevels", "levels",
                            Levels(20, 80, 320), Levels(10, 40, 160)),
                        SnapshotSetting("PlayerBaseSpeed", "value", 6, 99),
                        SnapshotSetting("PlayerRunSpeed", "value", 11, 99),
                        SnapshotSetting("RandomTrait1Positive1Negative", "value", 0.25, 99),
                        SnapshotSetting("RandomTrait2Positive", "value", 0.25, 99),
                        SnapshotSetting("RandomTrait1Positive", "value", 0.25, 99),
                        SnapshotSetting("GainOnLecternRest", "value", 25, 999),
                        SnapshotSetting("GatherRefillSlow", "value", 0.15, 999),
                        SnapshotSetting("GatherRefillNormal", "value", 0.3, 999),
                        SnapshotSetting("GatherRefillFast", "value", 1.0, 999),
                        SnapshotSetting("UnrelatedSetting", "value", 1, 9)
                    }
                }
            }
        },
        OperationRequests = new()
        {
            Request(ProfileOperationIds.CharacterXp,
                new JObject { ["percentage"] = 40 }),
            Request(ProfileOperationIds.ProfessionXp,
                new JObject { ["percentage"] = 50 }),
            Request(ProfileOperationIds.OverworldMovementSpeed,
                Preset("Faster")),
            Request(ProfileOperationIds.PositiveRandomTraits,
                Preset("PositiveOnly")),
            Request(ProfileOperationIds.LecternKnowledgeGain,
                Preset("VeryHigh")),
            Request(ProfileOperationIds.ResourceReplenishment,
                Preset("Fast"))
        }
    };
}

static ModProfileModel SnapshotFailureProfile(char identityMarker)
{
    ProjectModel source = MovementProjectWithOrdinary(
        identityMarker,
        6,
        11,
        1);
    return new ModProfileModel
    {
        FormatVersion = 4,
        SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity,
        Metadata = Metadata("Phase 3 rollback"),
        Snapshot = new ModificationSnapshotModel
        {
            SourceFileName = "phase3-failure.cdb",
            SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity,
            Categories = new()
            {
                new ModificationSnapshotCategoryModel
                {
                    Name = "constant",
                    Settings = new()
                    {
                        SnapshotSetting(
                            "UnrelatedSetting",
                            "value",
                            1,
                            "incompatible")
                    }
                }
            }
        },
        OperationRequests = new() { Request(
            ProfileOperationIds.OverworldMovementSpeed,
            Preset("Faster")) }
    };
}

static ModProfileModel Profile(
    ProjectModel source,
    IEnumerable<ProfileOperationRequestModel> requests,
    IEnumerable<GameplayOperationStateModel>? states = null,
    int formatVersion = 4) => new()
{
    FormatVersion = formatVersion,
    SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity,
    Metadata = Metadata("Phase 3 fixture"),
    Snapshot = new ModificationSnapshotModel
    {
        SourceFileName = "phase3-fixture.cdb",
        SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity,
        GameplayOperationStates = states?.Select(state =>
            state.DeepClone()).ToList() ?? new()
    },
    OperationRequests = requests.ToList()
};

static ModProfileMetadataModel Metadata(string name) => new()
{
    Name = name,
    ProfileVersion = "1.0",
    CreatedAtUtc = DateTimeOffset.UtcNow,
    ModifiedAtUtc = DateTimeOffset.UtcNow
};

static ModificationSnapshotSettingModel SnapshotSetting(
    string id,
    string path,
    object original,
    object current) => new()
{
    Id = id,
    Name = id,
    DisplayName = id,
    Properties = new()
    {
        new ModificationSnapshotPropertyModel
        {
            Name = path.Split('.').Last(),
            PropertyPath = path,
            OriginalPropertyExisted = true,
            OriginalValue = Token(original),
            CurrentValue = Token(current)
        }
    }
};

static JToken Token(object value) => value is JToken token
    ? token.DeepClone()
    : JToken.FromObject(value);

static ProfileOperationRequestModel Request(
    string id,
    JObject settings) => new()
{
    OperationId = id,
    Settings = settings
};

ModificationSnapshotImportResultModel ApplyIntent(
    ProjectModel target,
    string operationId,
    JObject settings)
{
    ProfileOperationRequestModel request = Request(operationId, settings);
    ProfileImpactManifestModel? manifest = EstablishImpact(
        target,
        new[] { request });
    ModProfileModel profile = Profile(
        MovementProject('0', 6, 11),
        new[] { request });
    ModificationSnapshotImportResultModel result =
        new ModProfileWorkflowService().ApplyProfile(target, profile);
    Check(manifest != null && manifest.TotalCount ==
          new EffectiveChangeCountService().Calculate(result.MutationResult),
        $"{operationId} manifest uses real replay and exact canonical leaf count");
    return result;
}

ModificationSnapshotImportResultModel ApplyChangedTraitIntent(
    ProjectModel target,
    JArray traits)
{
    ProfileOperationRequestModel request = Request(
        ProfileOperationIds.RandomTraitExclusions,
        new JObject { ["traits"] = traits });
    return new ModProfileWorkflowService().ApplyProfile(
        target,
        Profile(MovementProject('0', 6, 11), new[] { request }));
}

ProfileImpactManifestModel? EstablishImpact(
    ProjectModel source,
    IEnumerable<ProfileOperationRequestModel> requests)
{
    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Json(source));
    string identity = new CdbGenerationIdentityService().Calculate(bytes);
    ProjectModel exactSource = new JsonDataService().CreateProjectFromJson(
        System.Text.Encoding.UTF8.GetString(bytes));
    exactSource.EstablishPersistedIdentity(
        identity, identity, SourceProvenanceStatus.Verified);
    ModProfileModel profile = Profile(
        exactSource,
        requests.Select(request => new ProfileOperationRequestModel
        {
            FormatVersion = request.FormatVersion,
            OperationId = request.OperationId,
            Settings = request.Settings == null
                ? null
                : (JObject)request.Settings.DeepClone()
        }),
        formatVersion: ModProfileFormat.CurrentVersion);
    ProfileImpactEvaluationService evaluator = new(
        new ProfileImpactBaselineResolver(
            new IProfileImpactBaselineProvider[]
            {
                new ExactBytesProvider(bytes)
            }),
        new JsonDataService(),
        new ModProfileWorkflowService().ApplyProfile);
    return evaluator.TryEstablish(
        exactSource, profile, "test", out _);
}

static JObject Preset(string key) => new() { ["preset"] = key };

static ProjectModel CombinedProject(char marker) =>
    CreateProject(marker, Sheet("constant",
        ArrayEntry("LevelXpValues", Levels(0, 300, 400)),
        ArrayEntry("JobXpLevels", Levels(20, 100, 500)),
        Scalar("PlayerBaseSpeed", 6),
        Scalar("PlayerRunSpeed", 11),
        Scalar("RandomTrait1Positive1Negative", 0.25),
        Scalar("RandomTrait2Positive", 0.25),
        Scalar("RandomTrait1Positive", 0.25),
        Scalar("GainOnLecternRest", 30),
        Scalar("GatherRefillSlow", 0.2),
        Scalar("GatherRefillNormal", 0.5),
        Scalar("GatherRefillFast", 1.4),
        Scalar("UnrelatedSetting", 1)));

static ProjectModel MovementProject(
    char marker,
    int walk,
    int run) => CreateProject(marker, Sheet("constant",
        Scalar("PlayerBaseSpeed", walk),
        Scalar("PlayerRunSpeed", run)));

static ProjectModel MovementProjectWithOrdinary(
    char marker,
    int walk,
    int run,
    int ordinary) => CreateProject(marker, Sheet("constant",
        Scalar("PlayerBaseSpeed", walk),
        Scalar("PlayerRunSpeed", run),
        Scalar("UnrelatedSetting", ordinary)));

static ProjectModel AdditiveProject(char marker)
{
    List<JObject> items = new()
    {
        new JObject
        {
            ["id"] = "Anvil",
            ["props"] = new JObject
            {
                ["activity"] = "Forge",
                ["hideInCheatMenu"] = true,
                ["unknown"] = "preserved"
            }
        },
        new JObject
        {
            ["id"] = "ApothecaryTable",
            ["props"] = new JObject
            {
                ["activity"] = "Alchemy",
                ["hideInCheatMenu"] = true
            }
        }
    };
    items.AddRange(UpgradeAllEquipmentTargetCatalog.EntryIds.Select(id =>
        new JObject
        {
            ["id"] = id,
            ["props"] = new JObject { ["flags"] = 0 }
        }));
    return CreateProject(marker,
        Sheet("constant",
            Scalar("PlayerBaseSpeed", 6),
            Scalar("PlayerRunSpeed", 11)),
        Sheet("item", items.ToArray()),
        Sheet("craft"));
}

static ProjectModel StartingProject(char marker) => CreateProject(marker,
    Sheet("item",
        StartItem("Gold", 10),
        StartItem("Bread", 1),
        StartItem("Apple", 1),
        new JObject { ["id"] = "IronOre" },
        new JObject { ["id"] = "Wood" },
        new JObject { ["id"] = "Cloth" },
        new JObject { ["id"] = "Hemp" }),
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

static ProjectModel VolunteerProject(char marker) => CreateProject(marker,
    Sheet("trait", new JObject
    {
        ["id"] = "Volunteer",
        ["desc"] = "Volunteer",
        ["props"] = new JObject { ["value"] = 0 }
    }));

static ProjectModel ValourProject(char marker) => CreateProject(marker,
    Sheet("constant",
        Scalar("ActionPointBaseMax", 14),
        Scalar("ActionPointGainPerSleep", 2)),
    Sheet("item",
        Bonus("Tent", "props", "bonuses", "bonus", "ActionPoint", 1),
        Bonus("TentT2", "props", "bonuses", "bonus", "ActionPoint", 2),
        Bonus("TentT3", "props", "bonuses", "bonus", "ActionPoint", 3)));

static ProjectModel CarryingProject(char marker) => CreateProject(marker,
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

static ProjectModel RainProject(char marker) => CreateProject(marker,
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

static ProjectModel RequestBoardProject(char marker) => CreateProject(marker,
    Sheet("constant",
        RewardEntry(RequestBoardRewardsService.MinimumEntryId,
            (0, 200L), (1, 100L)),
        RewardEntry(RequestBoardRewardsService.MaximumEntryId,
            (0, 250L), (1, 150L))));

static JObject RewardEntry(
    string id,
    params (int Difficulty, long Value)[] values) => new()
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
    ((JArray)Entry(project, id).SourceEntry![
        RequestBoardRewardsService.PropertyPath]!)
        .OfType<JObject>()
        .Single(value => value.Value<int>("difficulty") == difficulty)
        .Value<long>("value");

static ProjectModel TraitProject(char marker, bool includeNew)
{
    List<JObject> recruitment = new()
    {
        Trait("RecruitmentAnchor", null, "unsupported"),
        Trait("PositiveAbsent", 0, null),
        Trait("NegativeDisabled", 1, false)
    };
    if (includeNew)
        recruitment.Add(Trait("NewCandidate", 0, null));
    return CreateProject(marker, new JObject
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

static ProjectModel TraitAndMovementProject(
    char marker,
    int walk,
    int run)
{
    ProjectModel traits = TraitProject(marker, includeNew: false);
    return CreateProject(
        marker,
        (JObject)traits.Sheets.Single(sheet => sheet.Name == "trait")
            .SourceSheet!.DeepClone(),
        Sheet("constant",
            Scalar("PlayerBaseSpeed", walk),
            Scalar("PlayerRunSpeed", run)));
}

static JObject Trait(string id, int? personality, object? done)
{
    JObject result = new()
    {
        ["id"] = id,
        ["props"] = personality.HasValue
            ? new JObject { ["personality"] = personality.Value }
            : new JObject()
    };
    if (done != null)
        result["done"] = JToken.FromObject(done);
    return result;
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

static ProjectModel CreateProject(char marker, params JObject[] sheets)
{
    JObject root = new() { ["sheets"] = new JArray(sheets) };
    ProjectModel project = new()
    {
        RootDocument = root,
        OriginalJson = root.ToString(),
        FileName = "phase3-fixture.cdb"
    };
    ProjectModelFactory factory = new();
    foreach (JObject sheet in sheets)
        project.Sheets.Add(factory.CreateSheetModel(sheet));
    project.EstablishPersistedIdentity(
        Identity(marker),
        Identity(marker),
        SourceProvenanceStatus.Verified);
    return project;
}

static string Identity(char marker) =>
    "sha256:" + new string(marker, 64);

static JObject Sheet(string name, params JObject[] entries) => new()
{
    ["name"] = name,
    ["lines"] = new JArray(entries)
};

static JObject Scalar(string id, object value) => new()
{
    ["id"] = id,
    ["value"] = JToken.FromObject(value)
};

static JObject ArrayEntry(string id, JArray values) => new()
{
    ["id"] = id,
    ["levels"] = values
};

static JArray Levels(params long[] values) => new(
    values.Select(value => new JObject { ["xp"] = value }));

static GameplayOperationStateModel State(
    ProjectModel project,
    ProgressionType type) => project.GameplayOperationStates.Single(
        state => state.OperationType == type);

static GameplayOperationStateModel AlterState(
    GameplayOperationStateModel source,
    Action<JObject> alter)
{
    JObject value = JObject.FromObject(source);
    alter(value);
    return value.ToObject<GameplayOperationStateModel>()!;
}

static long[] Values(ProjectModel project, string id) =>
    ((JArray)Entry(project, id).SourceEntry!["levels"]!)
        .OfType<JObject>()
        .Select(value => value.Value<long>("xp"))
        .ToArray();

static long Value(ProjectModel project, string id) =>
    Entry(project, id).SourceEntry!["value"]!.Value<long>();

static double ValueDouble(ProjectModel project, string id) =>
    Entry(project, id).SourceEntry!["value"]!.Value<double>();

static EntryModel Entry(ProjectModel project, string id) =>
    project.Sheets.Single(sheet => sheet.Name == "constant")
        .Entries.Single(entry => entry.Id == id);

static EntryModel EntryIn(ProjectModel project, string id, string sheetName) =>
    project.Sheets.Single(sheet => sheet.Name == sheetName)
        .Entries.Single(entry => entry.Id == id);

static string Json(ProjectModel project) =>
    project.RootDocument.ToString();

sealed class ExactBytesProvider : IProfileImpactBaselineProvider
{
    private readonly byte[] bytes;

    public ExactBytesProvider(byte[] bytes) => this.bytes = bytes;

    public bool TryGetBaseline(
        ProjectModel sourceProject,
        string requiredSourceIdentity,
        out ProfileImpactBaseline? baseline)
    {
        baseline = new ProfileImpactBaseline(
            bytes,
            new CdbGenerationIdentityService().Calculate(bytes),
            "test exact bytes");
        return true;
    }
}
