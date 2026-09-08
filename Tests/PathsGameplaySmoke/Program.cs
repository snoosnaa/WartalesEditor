using Newtonsoft.Json.Linq;
using System.IO;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.ViewModels;

int checks = 0;
ProjectModel project = CreateProject(5, 5, 12);
ProjectMutationService mutations = new();
GameplayOperationStateService states = new(mutations);
PathLevelRequirementsService levels = new(mutations, states);
PathXpRewardsService rewards = new(mutations, states, Localizer());
ProjectOperationService operations = new();

PathLevelRequirementsPreview originalPreview = levels.CreatePreview(project, 100);
Check(originalPreview.CurrentMinimum == 5 && originalPreview.CurrentMaximum == 55,
    "vanilla Path requirement formula");
Check(levels.CreatePreview(project, 60).ProposedMaximum == 33,
    "concise requirement preview");
Check(PathLevelRequirementsService.Options.Select(x => x.Percentage)
    .SequenceEqual(new[] { 100, 80, 60, 40, 20 }), "approved requirement options only");
Check(levels.CreatePreview(CreateProject(6, 4, 12), 80) is
      { ProposedBase: 5, ProposedNext: 3, WasRounded: true },
    "6/4 changed-source rounding is deterministic");
Check(levels.CreatePreview(CreateProject(7, 3, 12), 80) is
      { ProposedBase: 6, ProposedNext: 2, WasRounded: true },
    "7/3 changed-source rounding is deterministic");
ProjectModel levelOriginal = CreateProject(5, 5, 12);
ProjectMutationService originalMutation = new();
GameplayOperationStateService originalStates = new(originalMutation);
ProjectOperationResult originalStateOnly = operations.Execute(
    new PathLevelRequirementsOperation(
        new PathLevelRequirementsService(originalMutation, originalStates), 100),
    levelOriginal);
Check(originalStateOnly.Succeeded && originalStateOnly.MutationResult.UpdatedProperties.Count == 0 &&
      originalStateOnly.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "Original can establish restore state without fake property mutation");

ProjectOperationResult level60 = operations.Execute(
    new PathLevelRequirementsOperation(levels, 60), project);
Check(level60.Succeeded && Scalar(project, "constant", "PathXpBase", "value") == 3,
    "60 percent Base applied");
Check(Scalar(project, "constant", "PathXpNext", "value") == 3,
    "60 percent Next applied");
Check(Scalar(project, "constant", "PathMaxLevel", "value") == 12,
    "PathMaxLevel untouched");
Check(level60.MutationResult.UpdatedProperties.Count == 2 &&
      level60.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "level operation atomic result");

ProjectOperationResult level20 = operations.Execute(
    new PathLevelRequirementsOperation(levels, 20), project);
Check(Scalar(project, "constant", "PathXpBase", "value") == 1 &&
      Scalar(project, "constant", "PathXpNext", "value") == 1,
    "requirement reapply is noncompounding");
ProjectOperationHistoryAction levelHistory = new(
    "Path Level Requirements", level20.MutationResult,
    new ProjectOperationTransactionService());
levelHistory.Undo();
Check(Scalar(project, "constant", "PathXpBase", "value") == 3,
    "level Undo restores prior setting");
levelHistory.Redo();
Check(Scalar(project, "constant", "PathXpBase", "value") == 1,
    "level Redo restores applied setting");
ProjectOperationResult levelRestore = operations.Execute(
    new PathLevelRequirementsOperation(levels, 100, true), project);
Check(levelRestore.Succeeded && Scalar(project, "constant", "PathXpBase", "value") == 5 &&
      Scalar(project, "constant", "PathXpNext", "value") == 5,
    "level Restore exact baseline");
ProjectOperationHistoryAction levelRestoreHistory = new(
    "Restore Path Level Requirements", levelRestore.MutationResult,
    new ProjectOperationTransactionService());
levelRestoreHistory.Undo();
Check(Scalar(project, "constant", "PathXpBase", "value") == 1,
    "level Restore is one reversible history action");
levelRestoreHistory.Redo();
Check(Scalar(project, "constant", "PathXpBase", "value") == 5,
    "level Restore Redo is atomic");
ProjectOperationResult levelNoOp = operations.Execute(
    new PathLevelRequirementsOperation(levels, 100), project);
Check(levelNoOp.Succeeded && levelNoOp.MutationResult.UpdatedProperties.Count == 0 &&
      levelNoOp.MutationResult.GameplayOperationStateRollbackRecords.Count == 0,
    "matching Path requirement state is a true no-op");

Check(PathXpRewardsService.Options.Select(x => x.Multiplier)
    .SequenceEqual(new[] { 1, 2, 3, 4, 5 }), "approved reward options only");
foreach ((string pathId, int count) in new[]
{
    (PathXpRewardsService.MightPathId, 2),
    (PathXpRewardsService.TradePathId, 2),
    (PathXpRewardsService.CrimePathId, 17),
    (PathXpRewardsService.MysteryPathId, 2)
})
    Check(PathXpRewardsService.ResolveTargets(project, pathId).Targets.Count == count,
        $"{pathId} discovery");
Check(PathXpRewardsService.ResolveTargets(project, PathXpRewardsService.CrimePathId)
        .Targets.Select(target => target.Entry.Id)
        .SequenceEqual(PathXpRewardsService.ResolveTargets(
            project, PathXpRewardsService.CrimePathId).Targets
            .Select(target => target.Entry.Id).OrderBy(id => id, StringComparer.Ordinal)),
    "Path reward target ordering is deterministic");
Check(!PathXpRewardsService.ResolveTargets(project, PathXpRewardsService.CrimePathId)
        .Targets.Any(target => target.Entry.Id == "OutdatedCrime"),
    "Outdated counters are excluded");

ProfileOperationIntentRegistry registry = new();
foreach (string pathId in PathXpRewardsService.PathIds)
{
    ProjectModel isolated = CreateProject(5, 5, 12);
    Dictionary<string, int> originalCounters = CounterValues(isolated);
    JToken specialBefore = GoalSheet(isolated);
    ProjectMutationService isolatedMutations = new();
    GameplayOperationStateService isolatedStates = new(isolatedMutations);
    PathXpRewardsService isolatedRewards = new(isolatedMutations, isolatedStates, Localizer());
    ProjectOperationResult apply = operations.Execute(new PathXpRewardsOperation(
        isolatedRewards, pathId, 2), isolated);
    HashSet<string> ownedIds = PathXpRewardsService.ResolveTargets(isolated, pathId)
        .Targets.Select(target => target.Entry.Id).ToHashSet(StringComparer.Ordinal);
    Check(apply.Succeeded && ownedIds.All(id => CounterValues(isolated)[id] ==
          checked(originalCounters[id] * 2)), $"{pathId} Apply 2x changes owned rewards");
    Check(originalCounters.Where(pair => !ownedIds.Contains(pair.Key))
          .All(pair => CounterValues(isolated)[pair.Key] == pair.Value),
        $"{pathId} Apply leaves other Paths unchanged");
    Check(isolated.GameplayOperationStates.Count == 1 &&
          isolated.GameplayOperationStates[0].OperationType ==
              PathXpRewardsService.GetOperationType(pathId),
        $"{pathId} Apply creates only selected Path state");
    Check(JToken.DeepEquals(specialBefore, GoalSheet(isolated)),
        $"{pathId} Apply leaves every special reward unchanged");
    ProjectOperationHistoryAction applyHistory = new(
        $"{pathId} Apply", apply.MutationResult, new ProjectOperationTransactionService());
    applyHistory.Undo();
    Check(originalCounters.All(pair => CounterValues(isolated)[pair.Key] == pair.Value),
        $"{pathId} Apply is one Undo");
    applyHistory.Redo();
    Check(ownedIds.All(id => CounterValues(isolated)[id] == checked(originalCounters[id] * 2)),
        $"{pathId} Redo restores owned rewards");
    ProjectOperationResult restore = operations.Execute(new PathXpRewardsOperation(
        isolatedRewards, pathId, 1, restorePreviousValues: true), isolated);
    Check(restore.Succeeded && originalCounters.All(pair =>
          CounterValues(isolated)[pair.Key] == pair.Value),
        $"{pathId} Restore returns exact original rewards");
    Check(JToken.DeepEquals(specialBefore, GoalSheet(isolated)),
        $"{pathId} Restore leaves every special reward unchanged");
}

ProjectModel crimeBoundary = CreateProject(5, 5, 12);
JToken crimeBoundarySpecial = GoalSheet(crimeBoundary);
ProjectMutationService crimeBoundaryMutations = new();
GameplayOperationStateService crimeBoundaryStates = new(crimeBoundaryMutations);
PathXpRewardsService crimeBoundaryRewards = new(crimeBoundaryMutations, crimeBoundaryStates);
ProjectOperationResult crime2Boundary = operations.Execute(new PathXpRewardsOperation(
    crimeBoundaryRewards, PathXpRewardsService.CrimePathId, 2), crimeBoundary);
Check(crime2Boundary.Succeeded && JToken.DeepEquals(
      crimeBoundarySpecial, GoalSheet(crimeBoundary)),
    "MerchAttack and all special rewards unchanged after Crime 2x");
Check(crime2Boundary.MutationResult.UpdatedProperties.Distinct().Count() == 17,
    "MerchAttack is excluded from Crime effective change count");
GameplayOperationStateModel crimeBoundaryState = crimeBoundary.GameplayOperationStates.Single();
Check(crimeBoundaryState.BaselineArray.OfType<JObject>().All(record =>
      record.Value<string>("sheet") == "counter" &&
      record.Value<string>("targetPath") == "pathXP" &&
      record.Value<string>("entry") != "MerchAttack"),
    "MerchAttack is excluded from Crime baseline and state membership");
ProjectOperationResult crime5Boundary = operations.Execute(new PathXpRewardsOperation(
    crimeBoundaryRewards, PathXpRewardsService.CrimePathId, 5), crimeBoundary);
Check(crime5Boundary.Succeeded && JToken.DeepEquals(
      crimeBoundarySpecial, GoalSheet(crimeBoundary)),
    "MerchAttack and all special rewards unchanged after Crime 5x");
ProjectOperationResult crimeRestoreBoundary = operations.Execute(new PathXpRewardsOperation(
    crimeBoundaryRewards, PathXpRewardsService.CrimePathId, 1, true), crimeBoundary);
Check(crimeRestoreBoundary.Succeeded && JToken.DeepEquals(
      crimeBoundarySpecial, GoalSheet(crimeBoundary)),
    "MerchAttack and all special rewards unchanged after Crime Restore");

JToken merchBefore = project.RootDocument.SelectToken(
    "$.sheets[?(@.name == 'goal')].lines[?(@.id == 'MerchAttack')].reward.pathXp")!.DeepClone();
Dictionary<string, int> otherBefore = CounterValues(project)
    .Where(pair => !pair.Key.StartsWith("Crime", StringComparison.Ordinal))
    .ToDictionary();
PathXpRewardsPreview crimePreview = rewards.CreatePreview(
    project, PathXpRewardsService.CrimePathId, 3);
Check(crimePreview.TargetCount == 17 && crimePreview.CurrentTotal == 32 &&
      crimePreview.ProposedTotal == 96 &&
      crimePreview.ExpectedChangedPropertyCount == 17,
    "Crime concise preview totals and exact change count");
Check(crimePreview.DisplayName == "Crime and Chaos", "Path name localized");
PathXpRewardsDialogViewModel englishHeadings = new(
    CreateProject(5, 5, 12),
    new PathXpRewardsService(new ProjectMutationService(),
        new GameplayOperationStateService(), Localizer()));
Check(englishHeadings.Sections.Select(section => section.DisplayName).SequenceEqual(
      new[] { "Power and Glory", "Trade and Craftsmanship", "Crime and Chaos", "Mysteries and Wisdom" }),
    "dialog headings use only localized main Path names");
Check(englishHeadings.Sections.All(section =>
      !section.DisplayName.Contains("Drifters", StringComparison.Ordinal) &&
      !section.DisplayName.Contains("Retailers", StringComparison.Ordinal) &&
      !section.DisplayName.Contains("Scoundrels", StringComparison.Ordinal) &&
      !section.DisplayName.Contains("Seekers", StringComparison.Ordinal)),
    "dialog headings exclude concatenated progression ranks");
PathXpRewardsDialogViewModel alternateHeadings = new(
    CreateProject(5, 5, 12),
    new PathXpRewardsService(new ProjectMutationService(),
        new GameplayOperationStateService(), Localizer("Alternate Crime")));
Check(alternateHeadings.Sections.Single(section =>
      section.PathId == PathXpRewardsService.CrimePathId).DisplayName == "Alternate Crime",
    "dialog heading uses alternate loaded localization");
PathXpRewardsDialogViewModel fallbackHeadings = new(
    CreateProject(5, 5, 12),
    new PathXpRewardsService(new ProjectMutationService(),
        new GameplayOperationStateService(), new LocalizationService()));
Check(fallbackHeadings.Sections.Select(section => section.DisplayName)
      .SequenceEqual(PathXpRewardsService.PathIds),
    "dialog headings retain canonical-ID missing-localization fallback");
Check(rewards.CreatePreview(CreateProject(5, 5, 12),
      PathXpRewardsService.CrimePathId, 1).ExpectedChangedPropertyCount == 0,
    "Original preview reports zero expected changes");
Check(rewards.CreatePreview(CreateProject(5, 5, 12),
      PathXpRewardsService.MightPathId, 2).ExpectedChangedPropertyCount == 2,
    "other Path preview reports exact discovered changes");
PathXpRewardTargets mixedTargets = PathXpRewardsService.ResolveTargets(
    CreateProject(5, 5, 12), PathXpRewardsService.MightPathId);
JArray mixedCurrent = PathXpRewardsService.Capture(mixedTargets);
JArray mixedExpected = PathXpRewardsService.BuildExpected(mixedCurrent, 2);
mixedCurrent[1]!["value"] = mixedExpected[1]!["value"]!.DeepClone();
Check(PathXpRewardsService.CountExpectedChanges(mixedCurrent, mixedExpected) == 1,
    "mixed preview comparison counts only the differing subset");

ProjectOperationResult crime3 = operations.Execute(
    new PathXpRewardsOperation(rewards, PathXpRewardsService.CrimePathId, 3), project);
Check(crime3.Succeeded && crime3.MutationResult.UpdatedProperties.Count == 17,
    "Crime-only operation changes discovered scalars");
Check(Reward(project, "CrimeA") == 3 && Reward(project, "CrimeB") == 6 &&
      Reward(project, "CrimeC") == 9, "Crime 3x values");
Check(otherBefore.All(pair => CounterValues(project)[pair.Key] == pair.Value),
    "other Paths unchanged");
Check(JToken.DeepEquals(merchBefore, project.RootDocument.SelectToken(
    "$.sheets[?(@.name == 'goal')].lines[?(@.id == 'MerchAttack')].reward.pathXp")),
    "MerchAttack and reward.pathXp untouched");
Check(project.GameplayOperationStates.Count(state => state.OperationType is
    ProgressionType.PathXpRewardsMight or ProgressionType.PathXpRewardsTrade or
    ProgressionType.PathXpRewardsMystery) == 0, "untouched Paths receive no state");

ProjectOperationResult crime5 = operations.Execute(
    new PathXpRewardsOperation(rewards, PathXpRewardsService.CrimePathId, 5), project);
Check(Reward(project, "CrimeA") == 5 && Reward(project, "CrimeC") == 15,
    "reward reapply uses original baseline");
ProjectOperationResult crimeRestore = operations.Execute(
    new PathXpRewardsOperation(rewards, PathXpRewardsService.CrimePathId, 1, true), project);
Check(crimeRestore.Succeeded && Reward(project, "CrimeA") == 1 && Reward(project, "CrimeC") == 3,
    "independent reward Restore");
ProjectOperationResult rewardNoOp = operations.Execute(
    new PathXpRewardsOperation(rewards, PathXpRewardsService.CrimePathId, 1), project);
Check(rewardNoOp.Succeeded && rewardNoOp.MutationResult.UpdatedProperties.Count == 0 &&
      rewardNoOp.MutationResult.GameplayOperationStateRollbackRecords.Count == 0,
    "matching Path reward state is a true no-op");
ProjectModel rewardOriginalProject = CreateProject(5, 5, 12);
ProjectMutationService rewardOriginalMutations = new();
ProjectOperationResult rewardOriginal = operations.Execute(
    new PathXpRewardsOperation(
        new PathXpRewardsService(rewardOriginalMutations,
            new GameplayOperationStateService(rewardOriginalMutations)),
        PathXpRewardsService.CrimePathId, 1), rewardOriginalProject);
Check(rewardOriginal.Succeeded && rewardOriginal.MutationResult.UpdatedProperties.Count == 0 &&
      rewardOriginal.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "Original Path rewards can establish state without fake property mutation");

ProjectModel changed = CreateProject(7, 3, 12, addCrimeTarget: true);
ProfileOperationReplayService replay = new();
ProjectOperationResult replayLevels = replay.Replay(changed, new ProfileOperationRequestModel
{
    OperationId = ProfileOperationIds.PathLevelRequirements,
    Settings = new JObject { ["percentage"] = 80 }
});
Check(replayLevels.Succeeded && Scalar(changed, "constant", "PathXpBase", "value") == 6 &&
      Scalar(changed, "constant", "PathXpNext", "value") == 2,
    "changed-source requirement replay rounds from fresh baseline");
ProjectOperationResult replayCrime = replay.Replay(changed, new ProfileOperationRequestModel
{
    OperationId = ProfileOperationIds.PathXpRewardsCrime,
    Settings = new JObject { ["multiplier"] = 2 }
});
Check(replayCrime.Succeeded && Reward(changed, "CrimeNew") == 8,
    "changed-source new Crime target included");
Check(JToken.DeepEquals(changed.RootDocument.SelectToken(
    "$.sheets[?(@.name == 'goal')].lines[?(@.id == 'MerchAttack')].reward.pathXp"), merchBefore),
    "profile replay leaves MerchAttack untouched");
Check(replay.GetOwnedSnapshotLeaves(changed, new ProfileOperationRequestModel
    {
        OperationId = ProfileOperationIds.PathXpRewardsCrime,
        Settings = new JObject { ["multiplier"] = 2 }
    }).All(leaf => leaf.EntryId != "MerchAttack" && leaf.SheetName == "counter"),
    "MerchAttack is excluded from profile owned-leaf metadata");

ProjectModel removedTarget = CreateProject(5, 5, 12);
RemoveEntry(removedTarget, "counter", "CrimeQ");
ProjectOperationResult removedReplay = replay.Replay(removedTarget,
    new ProfileOperationRequestModel
    {
        OperationId = ProfileOperationIds.PathXpRewardsCrime,
        Settings = new JObject { ["multiplier"] = 2 }
    });
Check(removedReplay.Succeeded && !CounterValues(removedTarget).ContainsKey("CrimeQ") &&
      Reward(removedTarget, "CrimeA") == 2 &&
      removedTarget.GameplayOperationStates.Single().ElementCount == 16,
    "changed-source removed target is accepted and not resurrected");
ProjectModel renamedTarget = CreateProject(5, 5, 12);
RemoveEntry(renamedTarget, "counter", "CrimeQ");
AddEntry(renamedTarget, "counter", Counter(
    "CrimeRenamed", PathXpRewardsService.CrimePathId, 4));
ProjectOperationResult renamedReplay = replay.Replay(renamedTarget,
    new ProfileOperationRequestModel
    {
        OperationId = ProfileOperationIds.PathXpRewardsCrime,
        Settings = new JObject { ["multiplier"] = 3 }
    });
Check(renamedReplay.Succeeded && Reward(renamedTarget, "CrimeRenamed") == 12 &&
      renamedTarget.GameplayOperationStates.Single().TargetEntry.Contains(
          "CrimeRenamed", StringComparison.Ordinal) &&
      !renamedTarget.GameplayOperationStates.Single().TargetEntry.Contains(
          "CrimeQ", StringComparison.Ordinal),
    "changed-source renamed canonical target uses current membership");
foreach (string pathId in PathXpRewardsService.PathIds)
{
    ProjectModel freshMembership = CreateProject(5, 5, 12);
    string newId = pathId + "NewTarget";
    AddEntry(freshMembership, "counter", Counter(newId, pathId, 4));
    string operationId = registry.GetOperationId(
        PathXpRewardsService.GetOperationType(pathId));
    ProjectOperationResult freshReplay = replay.Replay(freshMembership,
        new ProfileOperationRequestModel
        {
            OperationId = operationId,
            Settings = new JObject { ["multiplier"] = 2 }
        });
    Check(freshReplay.Succeeded && Reward(freshMembership, newId) == 8 &&
          freshMembership.GameplayOperationStates.Single().TargetEntry.Contains(
              newId, StringComparison.Ordinal),
        $"{pathId} changed-source profile replay discovers fresh membership");
}

Check(registry.GetOperationType(ProfileOperationIds.PathXpRewardsCrime) ==
      ProgressionType.PathXpRewardsCrime, "Crime profile operation registered");
Check(registry.GetOperationType(ProfileOperationIds.PathLevelRequirements) ==
      ProgressionType.PathLevelRequirements, "level profile operation registered");
registry.ValidateRequest(new ProfileOperationRequestModel
{
    OperationId = ProfileOperationIds.PathXpRewardsCrime,
    Settings = new JObject { ["multiplier"] = 5 }
}, 4);
CheckThrows<InvalidOperationException>(() => registry.ValidateRequest(
    new ProfileOperationRequestModel
    {
        OperationId = ProfileOperationIds.PathXpRewardsCrime,
        Settings = new JObject { ["multiplier"] = 6 }
    }, 4), "unsupported multiplier rejected");

LocalizationService alternateLocalization = Localizer("Alternate Crime Path");
ProfileOperationRequestModel localizedCrimeRequest = new()
{
    OperationId = ProfileOperationIds.PathXpRewardsCrime,
    Settings = new JObject { ["multiplier"] = 2 }
};
ProfileOperationReplayService localizedReplayService = new(alternateLocalization);
ProjectOperationResult localizedReplay = localizedReplayService.Replay(
    CreateProject(5, 5, 12), localizedCrimeRequest);
Check(localizedReplay.Succeeded &&
      localizedReplayService.GetDisplayName(localizedCrimeRequest) ==
      "Alternate Crime Path XP Rewards",
    "alternate Language Data reaches profile replay presentation");
Check(new ProfileOperationReplayService().GetDisplayName(localizedCrimeRequest) ==
      "PathCrime XP Rewards",
    "missing localization follows established canonical-ID fallback");
InvalidOperationException statePresentation = CaptureException<InvalidOperationException>(() =>
    new GameplayOperationStateService(new ProjectMutationService(), alternateLocalization)
        .GetRequiredCompatibleState(
            CreateProject(5, 5, 12), ProgressionType.PathXpRewardsCrime));
Check(statePresentation.Message.Contains(
      "Alternate Crime Path XP Rewards", StringComparison.Ordinal),
    "state and Restore-facing presentation uses loaded Language Data");
ModProfileWorkflowService localizedWorkflow = CreateWorkflow(alternateLocalization);
ProjectModel localizedSummaryTarget = CreateProject(5, 5, 12);
ModProfileModel localizedSummaryProfile = ProfileWithRequest(
    localizedSummaryTarget, localizedCrimeRequest);
Check(localizedWorkflow.ApplyProfile(localizedSummaryTarget,
      localizedSummaryProfile).OperationResults.Single().DisplayName.Contains(
          "Alternate Crime Path", StringComparison.Ordinal),
    "localized Path name reaches profile Apply result");
ModificationSnapshotImportResultModel alreadyLocalized = localizedWorkflow.ApplyProfile(
    localizedSummaryTarget, localizedSummaryProfile);
Check(MainViewModel.BuildProfileApplySummary(alreadyLocalized).Contains(
      "Alternate Crime Path XP Rewards: already configured at 2×.",
      StringComparison.Ordinal),
    "already-configured summary uses loaded Language Data");

foreach ((LocalizationService localization, string expectedName, bool resolved) in
         new[]
         {
             (Localizer(), "Crime and Chaos XP Rewards", true),
             (alternateLocalization, "Alternate Crime Path XP Rewards", true),
             (new LocalizationService(), "PathCrime XP Rewards", false)
         })
{
    ModProfileWorkflowService candidateWorkflow = CreateWorkflow(localization);
    ProjectModel candidateProject = CreateProject(5, 5, 12);
    ProjectMutationService candidateMutations = new();
    GameplayOperationStateService candidateStates = new(
        candidateMutations, localization);
    Check(operations.Execute(new PathXpRewardsOperation(
          new PathXpRewardsService(candidateMutations, candidateStates, localization),
          PathXpRewardsService.CrimePathId, 2), candidateProject).Succeeded,
        $"{expectedName} candidate-validation fixture applies");
    ModProfileModel existingCandidateProfile = candidateWorkflow.CreateProfile(
        candidateProject, "Candidate validation localization");
    ModProfileModel invalidCandidate = candidateWorkflow.CreateUpdatedProfile(
        candidateProject, existingCandidateProfile);
    invalidCandidate.Snapshot.GameplayOperationStates.Single()
        .BaselineArray[0]!["value"] = 999;
    string candidateProjectBefore = ProjectSnapshot(candidateProject);
    string existingProfileBefore = new ModProfileSerializationService().Serialize(
        existingCandidateProfile);
    InvalidOperationException candidateFailure =
        CaptureException<InvalidOperationException>(() =>
            candidateWorkflow.ValidateUpdatedProfileCandidate(
                candidateProject, existingCandidateProfile, invalidCandidate));
    Check(candidateFailure.Message.Contains(expectedName, StringComparison.Ordinal) &&
          (!resolved ||
           (!candidateFailure.Message.Contains("PathCrime", StringComparison.Ordinal) &&
            !candidateFailure.Message.Contains("PathXpRewardsCrime", StringComparison.Ordinal))) &&
          ProjectSnapshot(candidateProject) == candidateProjectBefore &&
          new ModProfileSerializationService().Serialize(existingCandidateProfile) ==
              existingProfileBefore,
        $"{expectedName} candidate-validation failure is localized and observational");

    ProjectModel missingPathTarget = CreateProject(5, 5, 12);
    foreach (string id in CounterValues(missingPathTarget).Keys
                 .Where(id => id.StartsWith("Crime", StringComparison.Ordinal))
                 .ToArray())
    {
        RemoveEntry(missingPathTarget, "counter", id);
    }
    string missingTargetBefore = ProjectSnapshot(missingPathTarget);
    ModProfileModel missingTargetProfile = ProfileWithRequest(
        missingPathTarget, localizedCrimeRequest);
    InvalidOperationException missingTargetFailure =
        CaptureException<InvalidOperationException>(() =>
            candidateWorkflow.ApplyProfile(missingPathTarget, missingTargetProfile));
    Check(missingTargetFailure.Message.Contains(expectedName, StringComparison.Ordinal) &&
          (!resolved ||
           (!missingTargetFailure.Message.Contains("PathCrime", StringComparison.Ordinal) &&
            !missingTargetFailure.Message.Contains(
                "PathXpRewardsCrime", StringComparison.Ordinal) &&
            !missingTargetFailure.Message.Contains(
                ProfileOperationIds.PathXpRewardsCrime,
                StringComparison.OrdinalIgnoreCase))) &&
          ProjectSnapshot(missingPathTarget) == missingTargetBefore,
        $"{expectedName} missing-target profile replay fails safely without ID leakage");
}
CheckThrows<InvalidOperationException>(() => levels.Apply(CreateProject(5, 5, 12), 50),
    "unsupported percentage rejected");
CheckThrows<InvalidOperationException>(() =>
    PathLevelRequirementsService.ResolveTargets(CreateProject(5, 5, 1)),
    "invalid maximum Path level rejected");
ProjectModel missingBase = CreateProject(5, 5, 12);
RemoveEntry(missingBase, "constant", "PathXpBase");
CheckThrows<InvalidOperationException>(() =>
    PathLevelRequirementsService.ResolveTargets(missingBase),
    "missing PathXpBase rejected");
ProjectModel duplicateBase = CreateProject(5, 5, 12);
AddEntry(duplicateBase, "constant", new JObject { ["id"] = "PathXpBase", ["value"] = 5 });
CheckThrows<InvalidOperationException>(() =>
    PathLevelRequirementsService.ResolveTargets(duplicateBase),
    "duplicate PathXpBase rejected");
ProjectModel missingNext = CreateProject(5, 5, 12);
RemoveEntry(missingNext, "constant", "PathXpNext");
CheckThrows<InvalidOperationException>(() =>
    PathLevelRequirementsService.ResolveTargets(missingNext),
    "missing PathXpNext rejected");
ProjectModel duplicateNext = CreateProject(5, 5, 12);
AddEntry(duplicateNext, "constant", new JObject { ["id"] = "PathXpNext", ["value"] = 5 });
CheckThrows<InvalidOperationException>(() =>
    PathLevelRequirementsService.ResolveTargets(duplicateNext),
    "duplicate PathXpNext rejected");
ProjectModel missingMax = CreateProject(5, 5, 12);
RemoveEntry(missingMax, "constant", "PathMaxLevel");
CheckThrows<InvalidOperationException>(() =>
    PathLevelRequirementsService.ResolveTargets(missingMax),
    "missing PathMaxLevel rejected");
ProjectModel wrongLevelType = CreateProject(5, 5, 12);
SetValue(wrongLevelType, "constant", "PathXpBase", "value", new JValue("5"));
CheckThrows<InvalidOperationException>(() =>
    PathLevelRequirementsService.ResolveTargets(wrongLevelType),
    "non-integer Path requirement rejected");
ProjectModel levelOverflow = CreateProject(int.MaxValue, int.MaxValue, 12);
CheckThrows<OverflowException>(() =>
    PathLevelRequirementsService.ResolveTargets(levelOverflow),
    "Path requirement formula overflow rejected");
CheckThrows<InvalidOperationException>(() => PathXpRewardsService.ResolveTargets(
    DuplicateCrimeProject(), PathXpRewardsService.CrimePathId),
    "duplicate counter ID rejected");
ProjectModel malformedPath = CreateProject(5, 5, 12);
SetValue(malformedPath, "counter", "CrimeA", "path", new JValue(3));
CheckThrows<InvalidOperationException>(() => PathXpRewardsService.ResolveTargets(
    malformedPath, PathXpRewardsService.CrimePathId),
    "malformed Path discriminator rejected");
ProjectModel missingReward = CreateProject(5, 5, 12);
RemoveProperty(missingReward, "counter", "CrimeA", "pathXP");
CheckThrows<InvalidOperationException>(() => PathXpRewardsService.ResolveTargets(
    missingReward, PathXpRewardsService.CrimePathId),
    "missing owned pathXP rejected");
ProjectModel wrongRewardType = CreateProject(5, 5, 12);
SetValue(wrongRewardType, "counter", "CrimeA", "pathXP", new JValue("1"));
CheckThrows<InvalidOperationException>(() => PathXpRewardsService.ResolveTargets(
    wrongRewardType, PathXpRewardsService.CrimePathId),
    "non-integer owned pathXP rejected");
ProjectModel rewardOverflow = CreateProject(5, 5, 12);
SetValue(rewardOverflow, "counter", "CrimeA", "pathXP", new JValue(int.MaxValue));
CheckThrows<OverflowException>(() => new PathXpRewardsService(
        new ProjectMutationService(), new GameplayOperationStateService())
    .CreatePreview(rewardOverflow, PathXpRewardsService.CrimePathId, 2),
    "Path reward overflow rejected before mutation");
ProjectModel drift = CreateProject(5, 5, 12);
ProjectMutationService driftMutations = new();
GameplayOperationStateService driftStates = new(driftMutations);
PathXpRewardsService driftRewards = new(driftMutations, driftStates);
Check(operations.Execute(new PathXpRewardsOperation(driftRewards,
    PathXpRewardsService.CrimePathId, 2), drift).Succeeded,
    "same-source drift fixture applies");
AddEntry(drift, "counter", Counter("CrimeAddedLater", PathXpRewardsService.CrimePathId, 2));
ProjectOperationResult driftRestore = operations.Execute(new PathXpRewardsOperation(
    driftRewards, PathXpRewardsService.CrimePathId, 1, true), drift);
Check(!driftRestore.Succeeded && Reward(drift, "CrimeA") == 2,
    "same-source membership drift fails without partial Restore");

ProjectModel profileSource = CreateProject(5, 5, 12);
ProjectMutationService profileMutations = new();
GameplayOperationStateService profileStates = new(profileMutations);
PathLevelRequirementsService profileLevels = new(profileMutations, profileStates);
PathXpRewardsService profileRewards = new(profileMutations, profileStates);
Check(operations.Execute(new PathLevelRequirementsOperation(profileLevels, 60), profileSource).Succeeded,
    "profile source global operation succeeds");
Check(operations.Execute(new PathXpRewardsOperation(profileRewards,
    PathXpRewardsService.CrimePathId, 3), profileSource).Succeeded,
    "profile source Crime operation succeeds");
ModificationSnapshotModel profileSnapshot = new ModificationSnapshotService()
    .CreateSnapshot(profileSource);
IReadOnlyList<ProfileOperationRequestModel> captured =
    ProfileOperationCaptureService.CreateDefault().Capture(profileSource, profileSnapshot);
Check(captured.Any(request => request.OperationId == ProfileOperationIds.PathLevelRequirements &&
      request.Settings!["percentage"]!.Value<int>() == 60),
    "Profile Create captures global percentage intent");
Check(captured.Any(request => request.OperationId == ProfileOperationIds.PathXpRewardsCrime &&
      request.Settings!["multiplier"]!.Value<int>() == 3),
    "Profile Create captures Crime multiplier intent");
Check(!profileSnapshot.Categories.SelectMany(category => category.Settings)
    .SelectMany(setting => setting.Properties)
    .Any(property => property.PropertyPath is "value" or "pathXP"),
    "Profile Create excludes owned raw leaves");

ProfileOperationCaptureService captureService = ProfileOperationCaptureService.CreateDefault();
ModProfileModel historicalCrime = new()
{
    FormatVersion = 4,
    OperationRequests =
    [
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.PathXpRewardsCrime,
            Settings = new JObject { ["multiplier"] = 3 }
        }
    ]
};
IReadOnlyList<ProfileOperationRequestModel> preserved = captureService.ReconcileForUpdate(
    CreateProject(5, 5, 12), historicalCrime, Array.Empty<ProfileOperationRequestModel>());
Check(preserved.Single().Settings!["multiplier"]!.Value<int>() == 3,
    "Profile Update preserves historical intent without new authority");
IReadOnlyList<ProfileOperationRequestModel> replaced = captureService.ReconcileForUpdate(
    CreateProject(5, 5, 12), historicalCrime,
    [new ProfileOperationRequestModel
    {
        OperationId = ProfileOperationIds.PathXpRewardsCrime,
        Settings = new JObject { ["multiplier"] = 5 }
    }]);
Check(replaced.Single().Settings!["multiplier"]!.Value<int>() == 5,
    "Profile Update replaces changed Path intent");
IReadOnlyList<ProfileOperationRequestModel> removed = captureService.ReconcileForUpdate(
    project, historicalCrime, Array.Empty<ProfileOperationRequestModel>());
Check(removed.Count == 0,
    "Profile Update removes authoritatively restored Path intent");

ProjectModel ambiguity = CreateProject(5, 5, 12);
PropertyModel directCrime = ambiguity.Sheets.Single(sheet => sheet.Name == "counter")
    .Entries.Single(entry => entry.Id == "CrimeA").Properties
    .Single(property => property.EffectivePropertyPath == "pathXP");
directCrime.ApplySnapshotValue(new JValue(9));
ModificationSnapshotModel directDelta = new ModificationSnapshotService().CreateSnapshot(ambiguity);
CheckThrows<InvalidOperationException>(() =>
    captureService.ValidateNoUnresolvedOwnedLeafConflicts(
        ambiguity,
        directDelta,
        new[]
        {
            new ProfileOperationRequestModel
            {
                OperationId = ProfileOperationIds.PathXpRewardsCrime,
                Settings = new JObject { ["multiplier"] = 3 }
            }
        }), "historical Crime intent plus direct owned edit is ambiguous");

ModProfileWorkflowService alternateWorkflow = CreateWorkflow(alternateLocalization);
ModProfileModel alternateHistoricalCrime = ProfileWithRequest(
    ambiguity, localizedCrimeRequest);
string ambiguityBefore = ProjectSnapshot(ambiguity);
InvalidOperationException localizedAmbiguity = CaptureException<InvalidOperationException>(() =>
    alternateWorkflow.CreateUpdatedProfile(ambiguity, alternateHistoricalCrime));
Check(localizedAmbiguity.Message.Contains("Alternate Crime Path XP Rewards",
      StringComparison.Ordinal) &&
      !localizedAmbiguity.Message.Contains("PathCrime", StringComparison.Ordinal) &&
      ProjectSnapshot(ambiguity) == ambiguityBefore,
    "actual Profile Update ambiguity is localized and observational");

ProjectModel requirementAmbiguityProject = CreateProject(5, 5, 12);
PropertyModel directBase = requirementAmbiguityProject.Sheets
    .Single(sheet => sheet.Name == "constant").Entries
    .Single(entry => entry.Id == PathLevelRequirementsService.BaseEntryId)
    .Properties.Single(property => property.EffectivePropertyPath == "value");
directBase.ApplySnapshotValue(new JValue(6));
ModProfileModel historicalRequirements = ProfileWithRequest(
    requirementAmbiguityProject,
    new ProfileOperationRequestModel
    {
        OperationId = ProfileOperationIds.PathLevelRequirements,
        Settings = new JObject { ["percentage"] = 60 }
    });
string requirementAmbiguityBefore = ProjectSnapshot(requirementAmbiguityProject);
InvalidOperationException requirementAmbiguity = CaptureException<InvalidOperationException>(() =>
    alternateWorkflow.CreateUpdatedProfile(
        requirementAmbiguityProject, historicalRequirements));
Check(requirementAmbiguity.Message.Contains("Path Level Requirements",
      StringComparison.Ordinal) &&
      !requirementAmbiguity.Message.Contains("fingerprint", StringComparison.OrdinalIgnoreCase) &&
      ProjectSnapshot(requirementAmbiguityProject) == requirementAmbiguityBefore,
    "actual Requirements Base ambiguity fails player-safely before writing");
ProjectModel nextAmbiguityProject = CreateProject(5, 5, 12);
nextAmbiguityProject.Sheets.Single(sheet => sheet.Name == "constant").Entries
    .Single(entry => entry.Id == PathLevelRequirementsService.NextEntryId)
    .Properties.Single(property => property.EffectivePropertyPath == "value")
    .ApplySnapshotValue(new JValue(6));
InvalidOperationException nextAmbiguity = CaptureException<InvalidOperationException>(() =>
    alternateWorkflow.CreateUpdatedProfile(
        nextAmbiguityProject,
        ProfileWithRequest(nextAmbiguityProject,
            new ProfileOperationRequestModel
            {
                OperationId = ProfileOperationIds.PathLevelRequirements,
                Settings = new JObject { ["percentage"] = 60 }
            })));
Check(nextAmbiguity.Message.Contains("Path Level Requirements", StringComparison.Ordinal),
    "actual Requirements Next ambiguity fails before writing");

foreach (string pathId in PathXpRewardsService.PathIds)
{
    ProjectModel updateProject = CreateProject(5, 5, 12);
    ProjectMutationService updateMutations = new();
    GameplayOperationStateService updateStates = new(updateMutations, alternateLocalization);
    PathXpRewardsService updateRewards = new(
        updateMutations, updateStates, alternateLocalization);
    Check(operations.Execute(new PathXpRewardsOperation(
          updateRewards, pathId, 2), updateProject).Succeeded,
        $"{pathId} profile fixture applies");
    ModProfileModel createdPathProfile = alternateWorkflow.CreateProfile(
        updateProject, pathId);
    string operationId = registry.GetOperationId(
        PathXpRewardsService.GetOperationType(pathId));
    Check(createdPathProfile.OperationRequests.Single().OperationId == operationId,
        $"{pathId} Profile Create captures independent intent");
    Check(!createdPathProfile.Snapshot.Categories.SelectMany(category => category.Settings)
          .SelectMany(setting => setting.Properties)
          .Any(property => property.PropertyPath == "pathXP"),
        $"{pathId} Profile Create excludes owned raw leaves");
    string beforePreserve = ProjectSnapshot(updateProject);
    ModProfileModel preservedPathProfile = alternateWorkflow.CreateUpdatedProfile(
        updateProject, createdPathProfile);
    alternateWorkflow.ValidateUpdatedProfileCandidate(
        updateProject, createdPathProfile, preservedPathProfile);
    Check(preservedPathProfile.OperationRequests.Single().Settings!["multiplier"]!
          .Value<int>() == 2 && ProjectSnapshot(updateProject) == beforePreserve,
        $"{pathId} actual Profile Update preserves intent observationally");
    Check(operations.Execute(new PathXpRewardsOperation(
          updateRewards, pathId, 3), updateProject).Succeeded,
        $"{pathId} replacement fixture applies");
    ModProfileModel replacedPathProfile = alternateWorkflow.CreateUpdatedProfile(
        updateProject, preservedPathProfile);
    alternateWorkflow.ValidateUpdatedProfileCandidate(
        updateProject, preservedPathProfile, replacedPathProfile);
    Check(replacedPathProfile.OperationRequests.Single().Settings!["multiplier"]!
          .Value<int>() == 3,
        $"{pathId} actual Profile Update replaces intent");
    Check(operations.Execute(new PathXpRewardsOperation(
          updateRewards, pathId, 1, true), updateProject).Succeeded,
        $"{pathId} removal fixture restores");
    ModProfileModel removedPathProfile = alternateWorkflow.CreateUpdatedProfile(
        updateProject, replacedPathProfile);
    alternateWorkflow.ValidateUpdatedProfileCandidate(
        updateProject, replacedPathProfile, removedPathProfile);
    Check(!removedPathProfile.OperationRequests.Any(request =>
          request.OperationId == operationId),
        $"{pathId} actual Profile Update removes restored intent");
}

ProjectModel requirementUpdateProject = CreateProject(5, 5, 12);
ProjectMutationService requirementUpdateMutations = new();
GameplayOperationStateService requirementUpdateStates = new(
    requirementUpdateMutations, alternateLocalization);
PathLevelRequirementsService requirementUpdateService = new(
    requirementUpdateMutations, requirementUpdateStates);
Check(operations.Execute(new PathLevelRequirementsOperation(
      requirementUpdateService, 60), requirementUpdateProject).Succeeded,
    "Requirements Profile Update fixture applies");
ModProfileModel requirementProfile = alternateWorkflow.CreateProfile(
    requirementUpdateProject, "Requirements");
string requirementUpdateBefore = ProjectSnapshot(requirementUpdateProject);
ModProfileModel preservedRequirementProfile = alternateWorkflow.CreateUpdatedProfile(
    requirementUpdateProject, requirementProfile);
alternateWorkflow.ValidateUpdatedProfileCandidate(
    requirementUpdateProject, requirementProfile, preservedRequirementProfile);
Check(preservedRequirementProfile.OperationRequests.Single().Settings!["percentage"]!
      .Value<int>() == 60 &&
      ProjectSnapshot(requirementUpdateProject) == requirementUpdateBefore,
    "actual Requirements Profile Update preserves intent observationally");
Check(operations.Execute(new PathLevelRequirementsOperation(
      requirementUpdateService, 40), requirementUpdateProject).Succeeded,
    "Requirements replacement fixture applies");
ModProfileModel replacedRequirementProfile = alternateWorkflow.CreateUpdatedProfile(
    requirementUpdateProject, preservedRequirementProfile);
Check(replacedRequirementProfile.OperationRequests.Single().Settings!["percentage"]!
      .Value<int>() == 40,
    "actual Requirements Profile Update replaces intent");
Check(operations.Execute(new PathLevelRequirementsOperation(
      requirementUpdateService, 100, true), requirementUpdateProject).Succeeded,
    "Requirements removal fixture restores");
ModProfileModel removedRequirementProfile = alternateWorkflow.CreateUpdatedProfile(
    requirementUpdateProject, replacedRequirementProfile);
Check(!removedRequirementProfile.OperationRequests.Any(request =>
      request.OperationId == ProfileOperationIds.PathLevelRequirements),
    "actual Requirements Profile Update removes restored intent");

foreach (int multiplier in new[] { 1, 2, 3, 4, 5 })
    Check(rewards.CreatePreview(CreateProject(5, 5, 12),
              PathXpRewardsService.CrimePathId, multiplier).ProposedTotal == 32L * multiplier,
        $"Crime {multiplier}x configured base total");

ProjectModel combinedDirect = CreateProject(5, 5, 12);
JToken combinedDirectSpecial = GoalSheet(combinedDirect);
ProjectMutationService combinedDirectMutations = new();
GameplayOperationStateService combinedDirectStates = new(combinedDirectMutations);
Check(operations.Execute(new PathLevelRequirementsOperation(
      new PathLevelRequirementsService(combinedDirectMutations, combinedDirectStates),
      60), combinedDirect).Succeeded &&
      operations.Execute(new PathXpRewardsOperation(
          new PathXpRewardsService(combinedDirectMutations, combinedDirectStates),
          PathXpRewardsService.CrimePathId, 3), combinedDirect).Succeeded &&
      JToken.DeepEquals(combinedDirectSpecial, GoalSheet(combinedDirect)),
    "combined direct Requirements and Crime Apply leaves special rewards unchanged");

ProjectModel combinedTarget = CreateProject(5, 5, 12);
JToken combinedSpecial = GoalSheet(combinedTarget);
JToken combinedMerch = combinedTarget.RootDocument.SelectToken(
    "$.sheets[?(@.name == 'goal')].lines[?(@.id == 'MerchAttack')].reward.pathXp")!.DeepClone();
ModProfileModel combinedProfile = new()
{
    FormatVersion = 4,
    SourceCdbGenerationIdentity = combinedTarget.SourceCdbGenerationIdentity,
    Metadata = new ModProfileMetadataModel
    {
        Name = "Paths combined",
        ProfileVersion = "1.0",
        CreatedAtUtc = DateTimeOffset.UtcNow,
        ModifiedAtUtc = DateTimeOffset.UtcNow
    },
    Snapshot = new ModificationSnapshotModel
    {
        SourceFileName = "paths.cdb",
        SourceCdbGenerationIdentity = combinedTarget.SourceCdbGenerationIdentity
    },
    OperationRequests =
    [
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.PathLevelRequirements,
            Settings = new JObject { ["percentage"] = 60 }
        },
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.PathXpRewardsCrime,
            Settings = new JObject { ["multiplier"] = 3 }
        }
    ]
};
ModificationSnapshotImportResultModel combinedResult =
    new ModProfileWorkflowService().ApplyProfile(combinedTarget, combinedProfile);
Check(combinedResult.MutationResult.UpdatedProperties.Distinct().Count() == 19,
    "combined profile counts two requirements plus 17 Crime rewards");
Check(combinedResult.OperationResults.Count == 2 &&
      combinedTarget.GameplayOperationStates.Count == 2,
    "combined profile retains two independent semantic operations");
Check(JToken.DeepEquals(combinedMerch, combinedTarget.RootDocument.SelectToken(
    "$.sheets[?(@.name == 'goal')].lines[?(@.id == 'MerchAttack')].reward.pathXp")),
    "combined profile leaves MerchAttack unchanged");
Check(JToken.DeepEquals(combinedSpecial, GoalSheet(combinedTarget)),
    "combined profile leaves every reward.pathXp array unchanged");
ProjectOperationHistoryAction combinedHistory = new(
    "Apply Paths profile",
    combinedResult.MutationResult,
    new ProjectOperationTransactionService());
combinedHistory.Undo();
Check(Scalar(combinedTarget, "constant", "PathXpBase", "value") == 5 &&
      Reward(combinedTarget, "CrimeA") == 1 &&
      combinedTarget.GameplayOperationStates.Count == 0,
    "combined Profile Apply is one reversible history action");
combinedHistory.Redo();
Check(Scalar(combinedTarget, "constant", "PathXpBase", "value") == 3 &&
      Reward(combinedTarget, "CrimeA") == 3 &&
      combinedTarget.GameplayOperationStates.Count == 2,
    "combined profile history Redo is atomic");

string persistenceDirectory = Path.Combine(Path.GetTempPath(),
    "WartalesEditor-PathsGameplay-" + Guid.NewGuid().ToString("N"));
string persistedPath = Path.Combine(persistenceDirectory, "paths.cdb");
try
{
    JsonDataService json = new();
    json.SaveProject(combinedTarget, persistedPath);
    ProjectModel reopened = json.LoadProject(persistedPath);
    Check(reopened.GameplayOperationStates.Count == 2 &&
          new PathLevelRequirementsService(new ProjectMutationService(),
              new GameplayOperationStateService()).DetectPercentage(reopened) == 60,
        "combined gameplay state survives save and reopen");
    Check(new PathXpRewardsService(new ProjectMutationService(),
              new GameplayOperationStateService()).DetectMultiplier(
              reopened, PathXpRewardsService.CrimePathId) == 3,
        "Crime gameplay state survives save and reopen");
    GameplayOperationStateService reopenedStates = new();
    Check(reopenedStates.CanRestorePreviousValues(
              reopened, ProgressionType.PathLevelRequirements) &&
          reopenedStates.CanRestorePreviousValues(
              reopened, ProgressionType.PathXpRewardsCrime),
        "save and reopen preserves Restore authority for both Paths tools");
    GameplayOperationStateModel reopenedCrime = reopened.GameplayOperationStates.Single(
        state => state.OperationType == ProgressionType.PathXpRewardsCrime);
    Check(reopenedCrime.ElementCount == 17 &&
          reopenedCrime.BaselineArray.OfType<JObject>().All(record =>
              record.Value<string>("sheet") == "counter" &&
              record.Value<string>("entry") != "MerchAttack"),
        "save and reopen preserves exact Crime ordinary-counter membership");
    Check(JToken.DeepEquals(combinedSpecial, GoalSheet(reopened)),
        "save and reopen preserves MerchAttack and every special reward");
}
finally
{
    if (Directory.Exists(persistenceDirectory))
        Directory.Delete(persistenceDirectory, recursive: true);
}

ProjectModel refreshProject = CreateProject(5, 5, 12);
ProjectMutationService refreshMutations = new();
GameplayOperationStateService refreshStates = new(refreshMutations);
PathLevelRequirementsService refreshLevels = new(refreshMutations, refreshStates);
PathXpRewardsService refreshRewards = new(refreshMutations, refreshStates, Localizer());
PathLevelRequirementsDialogViewModel levelViewModel = new(refreshProject, refreshLevels);
levelViewModel.SelectedOption = levelViewModel.Options.Single(option => option.Percentage == 40);
Check(operations.Execute(new PathLevelRequirementsOperation(refreshLevels, 60), refreshProject).Succeeded,
    "dialog refresh level fixture applies");
levelViewModel.RefreshAfterProjectOperation();
Check(levelViewModel.CurrentStateText == "60%" &&
      levelViewModel.SelectedOption?.Percentage == 40,
    "level dialog refresh preserves valid pending input");
PathXpRewardsDialogViewModel rewardViewModel = new(refreshProject, refreshRewards);
PathXpRewardsSectionViewModel crimeSection = rewardViewModel.Sections.Single(
    section => section.PathId == PathXpRewardsService.CrimePathId);
crimeSection.SelectedOption = crimeSection.Options.Single(option => option.Multiplier == 5);
Check(operations.Execute(new PathXpRewardsOperation(refreshRewards,
        PathXpRewardsService.CrimePathId, 3), refreshProject).Succeeded,
    "dialog refresh reward fixture applies");
rewardViewModel.RefreshAfterProjectOperation();
Check(crimeSection.CurrentStateText == "3×" &&
      crimeSection.SelectedOption?.Multiplier == 5 &&
      rewardViewModel.Sections.Count == 4,
    "reward dialog refresh preserves pending input across four sections");
ProjectModel previewProject = CreateProject(5, 5, 12);
ProjectMutationService previewMutations = new();
GameplayOperationStateService previewStates = new(previewMutations);
PathXpRewardsService previewRewards = new(previewMutations, previewStates, Localizer());
PathXpRewardsSectionViewModel previewCrime = new PathXpRewardsDialogViewModel(
    previewProject, previewRewards).Sections.Single(section =>
        section.PathId == PathXpRewardsService.CrimePathId);
Check(previewCrime.PreviewText == "Current reward range: 1–3 XP",
    "Original no-change preview contains only current reward range");
Check(operations.Execute(new PathXpRewardsOperation(previewRewards,
        PathXpRewardsService.CrimePathId, 5), previewProject).Succeeded,
    "preview fixture applies Crime 5x");
previewCrime.RefreshFromProject();
Check(previewCrime.PreviewText == "Current reward range: 5–15 XP",
    "5x no-change preview contains only current discovered reward range");
previewCrime.SelectedOption = previewCrime.Options.Single(option => option.Multiplier == 3);
Check(previewCrime.PreviewText == "5–15 XP → 3–9 XP",
    "changed preview contains only current and proposed discovered ranges");
Check(!previewCrime.PreviewText.Contains("challenge", StringComparison.OrdinalIgnoreCase) &&
      !previewCrime.PreviewText.Contains("total", StringComparison.OrdinalIgnoreCase) &&
      !previewCrime.PreviewText.Contains("values will change", StringComparison.OrdinalIgnoreCase),
    "player preview excludes engineering counts and totals");
previewCrime.ApplyFeedback.ShowApplied("XP rewards were configured at 3×.");
Check(previewCrime.ApplyFeedback.IsVisible &&
      previewCrime.ApplyFeedback.Message == "XP rewards were configured at 3×." &&
      !previewCrime.PreviewText.Contains("configured", StringComparison.OrdinalIgnoreCase),
    "operation status remains separate from reward-range preview");

string rewardsDialogXaml = File.ReadAllText(Path.Combine(
    RepositoryRoot(), "Views", "PathXpRewardsDialog.xaml"));
Check(rewardsDialogXaml.Contains("ItemsSource=\"{Binding Sections}\"", StringComparison.Ordinal) &&
      rewardsDialogXaml.Contains("Header=\"{Binding DisplayName}\"", StringComparison.Ordinal),
    "dialog renders all four ViewModel sections with localized headings");
Check(rewardsDialogXaml.Contains("DisplayMemberPath=\"DisplayText\" Width=\"96\"", StringComparison.Ordinal) &&
      !rewardsDialogXaml.Contains("MinWidth=\"120\"", StringComparison.Ordinal),
    "multiplier selector uses compact fixed width");
Check(rewardsDialogXaml.Contains("Height=\"700\"", StringComparison.Ordinal) &&
      rewardsDialogXaml.Contains("VerticalScrollBarVisibility=\"Auto\"", StringComparison.Ordinal) &&
      rewardsDialogXaml.Contains("Margin=\"0,0,0,6\"", StringComparison.Ordinal),
    "normal dialog height and compact sections avoid routine scrolling while retaining defensive scrolling");
Check(rewardsDialogXaml.Contains("Click=\"RestoreButton_Click\"", StringComparison.Ordinal) &&
      rewardsDialogXaml.Contains("Click=\"ApplyButton_Click\"", StringComparison.Ordinal),
    "independent Restore and Apply bindings are preserved");
DialogLayoutEvidence layoutEvidence = MeasureRewardsDialog();
Check(layoutEvidence.SectionCount == 4 && layoutEvidence.ComboBoxCount == 4 &&
      layoutEvidence.ActionButtonCount == 9,
    "measured dialog retains four sections and all independent actions");
Check(layoutEvidence.ComboBoxesAreCompact,
    "measured multiplier selectors remain compact");
Check(layoutEvidence.ScrollableHeight <= 0.5,
    "all four sections and Close fit without normal-size vertical scrolling");

Console.WriteLine($"Paths gameplay smoke checks passed: {checks}");

void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"FAILED: {name}");
    checks++;
}

void CheckThrows<T>(Action action, string name) where T : Exception
{
    try { action(); }
    catch (T) { checks++; return; }
    throw new InvalidOperationException($"FAILED: {name}");
}

ProjectModel CreateProject(int baseValue, int nextValue, int maxLevel, bool addCrimeTarget = false)
{
    JArray counters =
    [
        Counter("MightA", PathXpRewardsService.MightPathId, 1),
        Counter("MightB", PathXpRewardsService.MightPathId, 3),
        Counter("TradeA", PathXpRewardsService.TradePathId, 2),
        Counter("TradeB", PathXpRewardsService.TradePathId, 4),
        Counter("CrimeA", PathXpRewardsService.CrimePathId, 1),
        Counter("CrimeB", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeC", PathXpRewardsService.CrimePathId, 3),
        Counter("CrimeD", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeE", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeF", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeG", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeH", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeI", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeJ", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeK", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeL", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeM", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeN", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeO", PathXpRewardsService.CrimePathId, 2),
        Counter("CrimeP", PathXpRewardsService.CrimePathId, 1),
        Counter("CrimeQ", PathXpRewardsService.CrimePathId, 1),
        Counter("MysteryA", PathXpRewardsService.MysteryPathId, 2),
        Counter("MysteryB", PathXpRewardsService.MysteryPathId, 4),
        new JObject { ["id"] = "OutdatedCrime", ["path"] = PathXpRewardsService.CrimePathId,
            ["pathXP"] = 99, ["outdated"] = true }
    ];
    if (addCrimeTarget)
        counters.Add(Counter("CrimeNew", PathXpRewardsService.CrimePathId, 4));
    JObject root = new()
    {
        ["sheets"] = new JArray
        {
            Sheet("constant",
                new JObject { ["id"] = "PathXpBase", ["value"] = baseValue },
                new JObject { ["id"] = "PathXpNext", ["value"] = nextValue },
                new JObject { ["id"] = "PathMaxLevel", ["value"] = maxLevel }),
            new JObject { ["name"] = "counter", ["lines"] = counters },
            Sheet("goal",
                new JObject
                {
                    ["id"] = "MerchAttack",
                    ["reward"] = new JObject
                    {
                        ["pathXp"] = new JArray(new JObject
                        {
                            ["path"] = PathXpRewardsService.CrimePathId,
                            ["v"] = 10,
                            ["percent"] = true,
                            ["future"] = "preserved"
                        })
                    }
                },
                new JObject
                {
                    ["id"] = "OtherSpecialReward",
                    ["reward"] = new JObject
                    {
                        ["pathXp"] = new JArray(new JObject
                        {
                            ["path"] = PathXpRewardsService.MightPathId,
                            ["v"] = 7,
                            ["future"] = "also-preserved"
                        })
                    }
                })
        }
    };
    ProjectModel model = new() { RootDocument = root, OriginalJson = root.ToString(), FileName = "paths.cdb" };
    ProjectModelFactory factory = new();
    foreach (JObject sheet in root["sheets"]!.OfType<JObject>())
        model.Sheets.Add(factory.CreateSheetModel(sheet));
    string identity = new CdbGenerationIdentityService().Calculate(
        System.Text.Encoding.UTF8.GetBytes(model.OriginalJson));
    model.EstablishPersistedIdentity(identity, identity, SourceProvenanceStatus.Verified);
    return model;
}

ProjectModel DuplicateCrimeProject()
{
    ProjectModel model = CreateProject(5, 5, 12);
    JObject counterSheet = (JObject)model.RootDocument["sheets"]!
        .Single(token => token!["name"]!.Value<string>() == "counter");
    JObject duplicate = Counter("CrimeA", PathXpRewardsService.CrimePathId, 2);
    ((JArray)counterSheet["lines"]!).Add(duplicate);
    model.Sheets.Single(sheet => sheet.Name == "counter").Entries.Add(
        new ProjectModelFactory().CreateEntryModel("counter", duplicate, 99));
    return model;
}

LocalizationService Localizer(string crimeName = "Crime and Chaos")
{
    LocalizationService service = new();
    service.Apply(service.Prepare(System.Xml.Linq.XDocument.Parse(
        "<root><sheet><PathMight><text>Power and Glory</text></PathMight>" +
        "<PathTrade><name>Trade and Craftsmanship</name></PathTrade>" +
        $"<PathCrime><name>{crimeName}</name></PathCrime>" +
        "<PathMystery><name>Mysteries and Wisdom</name></PathMystery>" +
        "<PathMight><props.bonuses><line8><title>Brave Drifters</title></line8>" +
        "<line9><title>Respected Trailblazers</title></line9></props.bonuses></PathMight>" +
        "<PathTrade><props.bonuses><line8><title>Honest Retailers</title></line8></props.bonuses></PathTrade>" +
        "<PathCrime><props.bonuses><line8><title>Menacing Scoundrels</title></line8></props.bonuses></PathCrime>" +
        "<PathMystery><props.bonuses><line8><title>Seasoned Seekers</title></line8></props.bonuses></PathMystery>" +
        "</sheet></root>")));
    return service;
}

string RepositoryRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "WartalesEditor.csproj")))
            return directory.FullName;
        directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("Repository root was not found.");
}

DialogLayoutEvidence MeasureRewardsDialog()
{
    DialogLayoutEvidence? evidence = null;
    Exception? failure = null;
    Thread thread = new(() =>
    {
        try
        {
            System.Windows.Application application = new()
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
            using FileStream resourcesStream = File.OpenRead(Path.Combine(
                RepositoryRoot(), "Resources", "SharedUiResources.xaml"));
            application.Resources.MergedDictionaries.Add(
                (System.Windows.ResourceDictionary)System.Windows.Markup.XamlReader.Load(
                    resourcesStream));

            ProjectModel layoutProject = CreateProject(5, 5, 12);
            ProjectMutationService layoutMutations = new();
            PathXpRewardsDialogViewModel layoutViewModel = new(
                layoutProject,
                new PathXpRewardsService(layoutMutations,
                    new GameplayOperationStateService(layoutMutations), Localizer()));
            WartalesEditor.Views.PathXpRewardsDialog dialog = new()
            {
                DataContext = layoutViewModel
            };
            System.Windows.FrameworkElement root =
                (System.Windows.FrameworkElement)dialog.Content;
            root.Measure(new System.Windows.Size(640, 660));
            root.Arrange(new System.Windows.Rect(0, 0, 640, 660));
            root.UpdateLayout();

            List<System.Windows.DependencyObject> descendants = Descendants(root).ToList();
            System.Windows.Controls.ScrollViewer scrollViewer = descendants
                .OfType<System.Windows.Controls.ScrollViewer>().Single(control =>
                    control.VerticalScrollBarVisibility ==
                    System.Windows.Controls.ScrollBarVisibility.Auto);
            List<System.Windows.Controls.ComboBox> comboBoxes = descendants
                .OfType<System.Windows.Controls.ComboBox>().ToList();
            evidence = new DialogLayoutEvidence(
                descendants.OfType<System.Windows.Controls.GroupBox>().Count(),
                comboBoxes.Count,
                descendants.OfType<System.Windows.Controls.Button>().Count(),
                comboBoxes.All(comboBox => Math.Abs(comboBox.ActualWidth - 96) <= 0.5),
                scrollViewer.ScrollableHeight);
            dialog.Close();
        }
        catch (Exception exception)
        {
            failure = exception;
        }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (failure != null)
        throw new InvalidOperationException("Path XP Rewards layout measurement failed.", failure);
    return evidence ?? throw new InvalidOperationException(
        "Path XP Rewards layout measurement returned no evidence.");
}

IEnumerable<System.Windows.DependencyObject> Descendants(
    System.Windows.DependencyObject parent)
{
    int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
    for (int index = 0; index < count; index++)
    {
        System.Windows.DependencyObject child =
            System.Windows.Media.VisualTreeHelper.GetChild(parent, index);
        yield return child;
        foreach (System.Windows.DependencyObject descendant in Descendants(child))
            yield return descendant;
    }
}

ModProfileWorkflowService CreateWorkflow(LocalizationService localization)
{
    ProjectMutationService mutation = new();
    ContentCreationService creation = new(mutation);
    AddCampFacilitiesOperation addCamp = new(creation);
    UpgradeAllEquipmentOperation upgrade = new(creation);
    ProjectOperationTransactionService transaction = new();
    ProjectOperationService operation = new(
        new OperationValidatorProvider(), transaction);
    ProfileOperationCaptureService capture = new(
        new OperationValidatorProvider(), addCamp, upgrade, localization);
    return new ModProfileWorkflowService(
        new ModProfileService(
            new ModificationSnapshotService(),
            capture,
            new ProfileSnapshotReconciliationService(),
            new GameplayOperationStateService(mutation, localization)),
        new ModProfileSerializationService(),
        new ModificationSnapshotWorkflowService(),
        new ProfileOperationResolver(addCamp, upgrade),
        operation,
        transaction,
        localization);
}

ModProfileModel ProfileWithRequest(
    ProjectModel source,
    ProfileOperationRequestModel request) => new()
{
    FormatVersion = 4,
    SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity,
    Metadata = new ModProfileMetadataModel
    {
        Name = "Paths test profile",
        ProfileVersion = "1.0",
        CreatedAtUtc = DateTimeOffset.UtcNow,
        ModifiedAtUtc = DateTimeOffset.UtcNow
    },
    Snapshot = new ModificationSnapshotModel
    {
        SourceFileName = "paths.cdb",
        SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity
    },
    OperationRequests = [request]
};

string ProjectSnapshot(ProjectModel model) =>
    model.RootDocument.ToString(Newtonsoft.Json.Formatting.None) + "|" +
    JToken.FromObject(model.GameplayOperationStates)
        .ToString(Newtonsoft.Json.Formatting.None) + "|" +
    model.IsModified + "|" + model.IsGameplayOperationStateModified;

T CaptureException<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T exception) { return exception; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

JObject Sheet(string name, params JObject[] entries) =>
    new() { ["name"] = name, ["lines"] = new JArray(entries) };
JObject Counter(string id, string path, int xp) =>
    new() { ["id"] = id, ["path"] = path, ["pathXP"] = xp, ["thresholdXp"] = 10 };
int Scalar(ProjectModel model, string sheet, string entry, string path) =>
    model.Sheets.Single(x => x.Name == sheet).Entries.Single(x => x.Id == entry)
        .Properties.Single(x => x.EffectivePropertyPath == path).SourceProperty!.Value.Value<int>();
int Reward(ProjectModel model, string id) => Scalar(model, "counter", id, "pathXP");
Dictionary<string, int> CounterValues(ProjectModel model) =>
    model.Sheets.Single(x => x.Name == "counter").Entries
        .Where(x => !x.Id.StartsWith("Outdated", StringComparison.Ordinal))
        .ToDictionary(x => x.Id, x => x.SourceEntry!["pathXP"]!.Value<int>());
JToken GoalSheet(ProjectModel model) => model.RootDocument["sheets"]!
    .Single(token => token!["name"]!.Value<string>() == "goal").DeepClone();

void RemoveEntry(ProjectModel model, string sheetName, string entryId)
{
    SheetModel sheet = model.Sheets.Single(value => value.Name == sheetName);
    EntryModel entry = sheet.Entries.Single(value => value.Id == entryId);
    ((JArray)sheet.SourceSheet!["lines"]!).Remove(entry.SourceEntry!);
    sheet.Entries.Remove(entry);
}

void AddEntry(ProjectModel model, string sheetName, JObject source)
{
    SheetModel sheet = model.Sheets.Single(value => value.Name == sheetName);
    ((JArray)sheet.SourceSheet!["lines"]!).Add(source);
    sheet.Entries.Add(new ProjectModelFactory().CreateEntryModel(
        sheetName, source, sheet.Entries.Count + 1));
}

void SetValue(
    ProjectModel model,
    string sheetName,
    string entryId,
    string propertyPath,
    JToken value)
{
    EntryModel entry = model.Sheets.Single(sheet => sheet.Name == sheetName)
        .Entries.Single(candidate => candidate.Id == entryId);
    entry.SourceEntry![propertyPath] = value;
}

void RemoveProperty(
    ProjectModel model,
    string sheetName,
    string entryId,
    string propertyPath)
{
    EntryModel entry = model.Sheets.Single(sheet => sheet.Name == sheetName)
        .Entries.Single(candidate => candidate.Id == entryId);
    entry.SourceEntry!.Property(propertyPath)!.Remove();
    PropertyModel property = entry.Properties.Single(candidate =>
        candidate.EffectivePropertyPath == propertyPath);
    entry.Properties.Remove(property);
}

record DialogLayoutEvidence(
    int SectionCount,
    int ComboBoxCount,
    int ActionButtonCount,
    bool ComboBoxesAreCompact,
    double ScrollableHeight);
