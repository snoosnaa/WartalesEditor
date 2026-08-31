using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.ViewModels;
using WartalesEditor.Views;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

int checks = 0;

ProjectModel project = CreateProject(
    MinArray((0, 200), (1, 175), (2, 150), (3, 125)),
    MaxArray((2, 200), (0, 250), (3, 150), (1, 225)));
ProjectMutationService mutation = new();
GameplayOperationStateService states = new(mutation);
RequestBoardRewardsService service = new(mutation, states);
RequestBoardRewardTargets resolved =
    RequestBoardRewardsService.ResolveTargets(project);
Check(resolved.Minimum.Records.Count == 4, "unique minimum target resolved");
Check(resolved.Maximum.Records.Count == 4, "unique maximum target resolved");
Check(resolved.Minimum.Records.Keys.ToHashSet().SetEquals(new long[] { 0, 1, 2, 3 }),
    "all discriminators discovered dynamically");
Check(resolved.Maximum.Array[0]!["difficulty"]!.Value<int>() == 2,
    "different physical ordering accepted");
Check(service.DetectPercentage(project) == 100, "initial preset is 100 percent");

ProjectOperationService executor = new();
ProjectOperationResult initialNoOp = executor.Execute(
    new RequestBoardRewardsOperation(service, 100), project);
Check(initialNoOp.Succeeded && !initialNoOp.MutationResult.WasModified,
    "initial 100 percent is a true no-op");
Check(project.GameplayOperationStates.Count == 0,
    "initial 100 percent creates no operation state");

ProjectOperationResult scaled150 = executor.Execute(
    new RequestBoardRewardsOperation(service, 150), project);
Check(scaled150.Succeeded, "150 percent operation succeeds");
Check(scaled150.MutationResult.UpdatedProperties.Distinct().Count() == 2,
    "both arrays mutate in one result");
Check(scaled150.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
    "one operation-state change recorded");
Check(Value(project, RequestBoardRewardsService.MinimumEntryId, 0) == 300,
    "150 percent minimum scales");
Check(Value(project, RequestBoardRewardsService.MinimumEntryId, 1) == 263,
    "half value rounds away from zero");
Check(Value(project, RequestBoardRewardsService.MaximumEntryId, 1) == 338,
    "maximum half value rounds away from zero");
Check(Array(project, RequestBoardRewardsService.MinimumEntryId)
        .All(record => record!["value"]!.Type == JTokenType.Integer),
    "scaled output remains integer tokens");
Check((string?)Array(project, RequestBoardRewardsService.MinimumEntryId)[0]!["future"] ==
      "preserved-0", "unknown record members preserved");
Check(Array(project, RequestBoardRewardsService.MaximumEntryId)[0]!["difficulty"]!.Value<int>() == 2,
    "original maximum ordering preserved");
Check(project.Sheets.Single(sheet => sheet.Name == "constant")
        .Entries.Single(entry => entry.Id == "UnrelatedConstant")
        .SourceEntry!["value"]!.Value<int>() == 77,
    "unrelated constant unchanged");
Check(project.Sheets.Single(sheet => sheet.Name == "mission")
        .Entries.Single(entry => entry.Id == "Champion")
        .SourceEntry!["goldBonus"]!.Value<int>() == 50,
    "mission goldBonus unchanged");
Check(project.Sheets.Single(sheet => sheet.Name == "mission")
        .Entries.Single(entry => entry.Id == "WeeklyBounty")
        .SourceEntry!["goldBonus"]!.Value<int>() == 100,
    "Weekly Bounty goldBonus unchanged");
Check(ScalarValue(project, "constant", "MissionGoldCoefUnit", "value") == 0.8m &&
      ScalarValue(project, "constant", "MissionGoldIncrPerExtraUnit", "value") == 7m,
    "troop-size constants unchanged");
Check(ScalarValue(project, "constant", "MissionNegociationGoldBonus", "value") == 10m &&
      ScalarValue(project, "constant", "MissionNegociationGoldMalus", "value") == 5m &&
      ScalarValue(project, "constant", "MissionNegociationMaxTries", "value") == 3m,
    "negotiation constants unchanged");
Check(ScalarValue(project, "path", "MissionGold", "bonus") == 10m &&
      ScalarValue(project, "trait", "SonOfTrader", "bonus") == 10m,
    "path and trait bonuses unchanged");
Check(ScalarValue(project, "fief", "FiefMissionReward", "value") == 999m,
    "Fief reward remains outside operation scope");

ProjectModel goldenBaseline = CreateProject(
    MinArray((0, 200), (1, 175), (2, 150), (3, 125)),
    MaxArray((2, 200), (0, 250), (3, 150), (1, 225)));
GoldenCdbComparisonResult goldenResult =
    new GoldenCdbComparisonService().Compare(
        project,
        new GoldenCdbReference(
            goldenBaseline,
            "golden-request-board-test",
            1,
            "golden.cdb"));
Check(goldenResult.Differences.Count(item =>
        item.Sheet == "constant" &&
        item.Property == RequestBoardRewardsService.PropertyPath &&
        item.Entry is RequestBoardRewardsService.MinimumEntryId or
            RequestBoardRewardsService.MaximumEntryId) == 2,
    "Golden observes both complete array property changes");

ProjectOperationResult scaled200 = executor.Execute(
    new RequestBoardRewardsOperation(service, 200), project);
Check(scaled200.Succeeded, "200 percent operation succeeds");
Check(Value(project, RequestBoardRewardsService.MinimumEntryId, 0) == 400,
    "repeated Apply uses baseline instead of compounding");
Check(Value(project, RequestBoardRewardsService.MinimumEntryId, 1) == 350,
    "200 percent integer result correct");
ProjectOperationResult scaled300 = executor.Execute(
    new RequestBoardRewardsOperation(service, 300), project);
Check(scaled300.Succeeded &&
      Value(project, RequestBoardRewardsService.MaximumEntryId, 0) == 750,
    "300 percent scaling succeeds");

ProjectOperationHistoryAction history = new(
    "Request Board Rewards",
    scaled300.MutationResult,
    new ProjectOperationTransactionService());
history.Undo();
Check(Value(project, RequestBoardRewardsService.MinimumEntryId, 0) == 400 &&
      project.GameplayOperationStates.Single().AppliedPercentage == 200,
    "Undo restores both values and previous state");
history.Redo();
Check(Value(project, RequestBoardRewardsService.MinimumEntryId, 0) == 600 &&
      project.GameplayOperationStates.Single().AppliedPercentage == 300,
    "Redo reapplies values and state");

ProjectOperationResult active100 = executor.Execute(
    new RequestBoardRewardsOperation(service, 100), project);
Check(active100.Succeeded && active100.MutationResult.WasModified,
    "active-chain 100 percent is Undoable");
Check(Value(project, RequestBoardRewardsService.MinimumEntryId, 0) == 200 &&
      Value(project, RequestBoardRewardsService.MaximumEntryId, 0) == 250,
    "active-chain 100 percent restores both baselines");
Check(project.GameplayOperationStates.Single().AppliedPercentage == 100,
    "active-chain 100 percent state retained");
Check(states.CanRestorePreviousValues(
        project,
        ProgressionType.RequestBoardRewards),
    "verified provenance authorizes Restore Previous Values");
ProjectOperationResult restoreNoOp = executor.Execute(
    new RequestBoardRewardsOperation(service, 100, true), project);
Check(restoreNoOp.Succeeded && !restoreNoOp.MutationResult.WasModified,
    "already restored operation creates no history mutation");

_ = executor.Execute(new RequestBoardRewardsOperation(service, 150), project);
ProjectOperationResult restored = executor.Execute(
    new RequestBoardRewardsOperation(service, 100, true), project);
Check(restored.Succeeded && restored.MutationResult.WasModified,
    "Restore Previous Values succeeds after a change");
Check(Array(project, RequestBoardRewardsService.MinimumEntryId)[0]!["future"]!.Value<string>() ==
      "preserved-0", "Restore preserves complete baseline records");

RequestBoardRewardsPreview preview = service.CreatePreview(project, 150);
Check(preview.DifficultyCount == 4 && preview.ProposedMinimum == 188 &&
      preview.ProposedMaximum == 375,
    "aggregate preview is derived without discriminator labels");

ProjectModel profileSource = CreateProject(
    MinArray((0, 200), (1, 175)),
    MaxArray((0, 250), (1, 225)));
ProjectMutationService profileMutation = new();
RequestBoardRewardsService profileService = new(
    profileMutation,
    new GameplayOperationStateService(profileMutation));
Check(executor.Execute(
        new RequestBoardRewardsOperation(profileService, 150),
        profileSource).Succeeded,
    "profile source operation succeeds");
ModificationSnapshotModel portableSnapshot =
    new ModificationSnapshotService().CreateSnapshot(
        profileSource,
        "request-board-test");
Check(portableSnapshot.GameplayOperationStates.Any(state =>
        state.OperationType == ProgressionType.RequestBoardRewards),
    "ordinary snapshots retain compatible Request Board state");

string persistenceBase = Path.Combine(
    Path.GetTempPath(),
    "WartalesEditorRequestBoardRewards");
string persistenceRoot = Path.Combine(
    persistenceBase,
    Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(persistenceRoot);
try
{
    string persistedCdb = Path.Combine(persistenceRoot, "data.cdb");
    JsonDataService dataService = new();
    dataService.SaveProject(profileSource, persistedCdb);
    Check(File.Exists(persistedCdb + ".wtstate"),
        "Request Board state persists to an isolated sidecar");
    ProjectModel reloaded = dataService.LoadProject(persistedCdb);
    Check(reloaded.GameplayOperationStates.Single(state =>
            state.OperationType == ProgressionType.RequestBoardRewards)
            .AppliedPercentage == 150,
        "saved Request Board percentage reloads");
    Check(new GameplayOperationStateService().CanRestorePreviousValues(
            reloaded,
            ProgressionType.RequestBoardRewards),
        "reloaded state retains verified Restore authority");
}
finally
{
    Directory.Delete(persistenceRoot, recursive: true);
    if (Directory.Exists(persistenceBase) &&
        !Directory.EnumerateFileSystemEntries(persistenceBase).Any())
    {
        Directory.Delete(persistenceBase);
    }
}
ModProfileModel profile = new ModProfileService().CreateProfile(
    profileSource,
    "Request Board Rewards");
ProfileOperationRequestModel request = profile.OperationRequests.Single(candidate =>
    candidate.OperationId == ProfileOperationIds.RequestBoardRewards);
Check(request.Settings!["percentage"]!.Value<int>() == 150,
    "profile captures percentage intent");
Check(!profile.Snapshot.Categories.SelectMany(category => category.Settings)
        .Where(setting => setting.Id is RequestBoardRewardsService.MinimumEntryId or
            RequestBoardRewardsService.MaximumEntryId)
        .SelectMany(setting => setting.Properties)
        .Any(property => property.PropertyPath == RequestBoardRewardsService.PropertyPath),
    "profile excludes owned absolute array snapshots");
Check(profile.Snapshot.GameplayOperationStates.All(state =>
        state.OperationType != ProgressionType.RequestBoardRewards),
    "profile excludes source-specific Request Board state");
Check(new EffectiveChangeCountService().Calculate(profile) == 2,
    "effective profile accounting reports two owned project changes");
string serialized = new ModProfileSerializationService().Serialize(profile);
Check(serialized.Contains("request-board-rewards", StringComparison.Ordinal) &&
      serialized.Contains("\"percentage\": 150", StringComparison.Ordinal),
    "profile serialization retains request settings");

ProjectModel destination = CreateProject(
    MinArray((0, 300), (1, 250), (4, 100)),
    MaxArray((4, 180), (1, 350), (0, 400)));
ModificationSnapshotImportResultModel appliedProfile =
    new ModProfileWorkflowService().ApplyProfile(destination, profile);
Check(!appliedProfile.HasFailures, "profile replay succeeds on compatible newer data");
Check(Value(destination, RequestBoardRewardsService.MinimumEntryId, 0) == 450 &&
      Value(destination, RequestBoardRewardsService.MinimumEntryId, 4) == 150,
    "profile replay derives from destination baseline");
GameplayOperationStateModel destinationState = destination.GameplayOperationStates.Single(state =>
    state.OperationType == ProgressionType.RequestBoardRewards);
Check(destinationState.BaselineArray[0]!["value"]![0]!["value"]!.Value<int>() == 300,
    "destination-specific baseline captured");
Check(destinationState.BaselineArray[0]!["value"]!.Count() == 3,
    "new matching discriminator participates in replay");

Check(executor.Execute(
        new RequestBoardRewardsOperation(
            new RequestBoardRewardsService(
                new ProjectMutationService(),
                new GameplayOperationStateService()),
            300),
        profileSource).Succeeded,
    "profile update source advances to 300 percent");
ModProfileModel updatedProfile = new ModProfileService().CreateUpdatedProfile(
    profileSource,
    profile);
Check(updatedProfile.OperationRequests.Single(candidate =>
        candidate.OperationId == ProfileOperationIds.RequestBoardRewards)
        .Settings!["percentage"]!.Value<int>() == 300,
    "Update Existing Profile recaptures percentage intent");

ModProfileModel invalidProfile = CloneProfileWithRequest(
    profile,
    new JObject { ["percentage"] = 125 });
CheckThrows<ModProfileSerializationException>(
    () => new ModProfileSerializationService().Serialize(invalidProfile),
    "invalid profile percentage rejected");

ProjectModel invalidDestination = CreateProject(
    MinArray((0, 200)),
    MaxArray((1, 250)));
string invalidBefore = invalidDestination.RootDocument.ToString();
CheckThrows<InvalidOperationException>(
    () => new ModProfileWorkflowService().ApplyProfile(invalidDestination, profile),
    "invalid profile destination fails atomically");
Check(invalidDestination.RootDocument.ToString() == invalidBefore &&
      invalidDestination.GameplayOperationStates.Count == 0,
    "failed profile replay leaves no partial values or state");

VerifyInvalid(
    CreateProject(null, MaxArray((0, 250))),
    "missing minimum target rejected");
VerifyInvalid(
    CreateProject(MinArray((0, 200)), null),
    "missing maximum target rejected");
ProjectModel duplicateTarget = CreateProject(
    MinArray((0, 200)),
    MaxArray((0, 250)));
duplicateTarget.Sheets.Single(sheet => sheet.Name == "constant").Entries.Add(
    new ProjectModelFactory().CreateEntryModel(
        "constant",
        Entry(RequestBoardRewardsService.MinimumEntryId, MinArray((0, 200))),
        99));
VerifyInvalid(duplicateTarget, "duplicate target rejected");
VerifyInvalid(
    CreateProject(new JArray(new JValue(1)), MaxArray((0, 250))),
    "non-object record rejected");
VerifyInvalid(
    CreateProject(new JArray(new JObject { ["value"] = 200 }), MaxArray((0, 250))),
    "missing discriminator rejected");
VerifyInvalid(
    CreateProject(new JArray(new JObject { ["difficulty"] = 0 }), MaxArray((0, 250))),
    "missing reward value rejected");
VerifyInvalid(
    CreateProject(MinArray((0, 200), (0, 175)), MaxArray((0, 250))),
    "duplicate discriminator rejected");
VerifyInvalid(
    CreateProject(MinArray((0, 200)), MaxArray((1, 250))),
    "mismatched discriminator sets rejected");
JArray floatMinimum = MinArray((0, 200));
floatMinimum[0]!["value"] = 200.5m;
VerifyInvalid(
    CreateProject(floatMinimum, MaxArray((0, 250))),
    "noninteger reward rejected");
VerifyInvalid(
    CreateProject(MinArray((0, 300)), MaxArray((0, 250))),
    "invalid Min Max relation rejected");
ProjectModel overflow = CreateProject(
    MinArray((0, long.MaxValue / 2)),
    MaxArray((0, long.MaxValue / 2)));
RequestBoardRewardsService overflowService = new(
    new ProjectMutationService(),
    new GameplayOperationStateService());
ProjectOperationResult overflowResult = executor.Execute(
    new RequestBoardRewardsOperation(overflowService, 300), overflow);
Check(!overflowResult.Succeeded && !overflow.IsModified &&
      overflow.GameplayOperationStates.Count == 0,
    "arithmetic overflow fails before mutation");

ProjectModel rollbackProject = CreateProject(
    MinArray((0, 200)),
    MaxArray((0, 250)));
ProjectMutationService rollbackMutation = new();
RequestBoardRewardsService rollbackService = new(
    rollbackMutation,
    new GameplayOperationStateService(rollbackMutation));
ProjectOperationResult rejected = new ProjectOperationService(
    new RejectAllValidatorProvider(),
    new ProjectOperationTransactionService()).Execute(
        new RequestBoardRewardsOperation(rollbackService, 150),
        rollbackProject);
Check(!rejected.Succeeded &&
      Value(rollbackProject, RequestBoardRewardsService.MinimumEntryId, 0) == 200 &&
      Value(rollbackProject, RequestBoardRewardsService.MaximumEntryId, 0) == 250,
    "validator failure rolls back both arrays");
Check(rollbackProject.GameplayOperationStates.Count == 0 &&
      !rollbackProject.IsGameplayOperationStateModified,
    "validator failure rolls back operation state");

ProjectModel unrelatedProject = CreateProject(
    MinArray((0, 200)),
    MaxArray((0, 250)));
EntryModel unrelated = unrelatedProject.Sheets.Single(sheet => sheet.Name == "constant")
    .Entries.Single(entry => entry.Id == "UnrelatedConstant");
ProjectMutationResult unrelatedMutation = new ProjectMutationService()
    .EnsurePropertyByPath(unrelated, "value", 88);
OperationValidationResult unrelatedValidation =
    new RequestBoardRewardsOperationValidator().Validate(
        new RequestBoardRewardsOperation(
            new RequestBoardRewardsService(
                new ProjectMutationService(),
                new GameplayOperationStateService()),
            100),
        unrelatedProject,
        unrelatedMutation);
Check(!unrelatedValidation.IsValid,
    "validator rejects unrelated mutation connection");

IReadOnlyList<GameplayCompatibilityAssessment> compatibility =
    new GameplayCompatibilityAssessmentService().Assess(project);
Check(compatibility.Any(result =>
        result.ToolName == "Request Board Rewards" &&
        result.Status == GameplayCompatibilityStatus.Compatible),
    "Update Survival reports compatible feature");
IReadOnlyList<GameplayCompatibilityAssessment> incompatibleCompatibility =
    new GameplayCompatibilityAssessmentService().Assess(invalidDestination);
Check(incompatibleCompatibility.Any(result =>
        result.ToolName == "Request Board Rewards" &&
        result.Status == GameplayCompatibilityStatus.StructureChanged),
    "Update Survival classifies incompatible structure");

string mainWindow = File.ReadAllText(Path.Combine(
    FindRepositoryRoot(),
    "MainWindow.xaml"));
Check(mainWindow.Contains("Request Board Rewards", StringComparison.Ordinal) &&
      mainWindow.Contains("RequestBoardRewardsCommand", StringComparison.Ordinal),
    "Gameplay Tools World command is wired");
Check(!mainWindow.Contains("MissionGoldMinDifficulty", StringComparison.Ordinal) &&
      !mainWindow.Contains("valueDifficulty", StringComparison.Ordinal),
    "player UI exposes no internal target names");

VerifyUnknownSourceLocalRestoreAuthority();

Console.WriteLine($"Request Board Rewards smoke checks passed: {checks}");

void VerifyUnknownSourceLocalRestoreAuthority()
{
    JArray baselineMinimum = MinArray(
        (0, 200), (1, 175), (2, 150), (3, 125));
    JArray baselineMaximum = MaxArray(
        (0, 250), (1, 225), (2, 200), (3, 150));
    ProjectModel unknown = CreateUnknownProject(
        (JArray)baselineMinimum.DeepClone(),
        (JArray)baselineMaximum.DeepClone());
    ProjectMutationService localMutation = new();
    GameplayOperationStateService localStates = new(localMutation);
    RequestBoardRewardsService localService = new(
        localMutation,
        localStates);
    ProjectOperationService localExecutor = new();

    ProjectOperationResult localApply = localExecutor.Execute(
        new RequestBoardRewardsOperation(localService, 300),
        unknown);
    GameplayOperationStateModel localState =
        unknown.GameplayOperationStates.Single();
    Check(localApply.Succeeded &&
          unknown.SourceProvenanceStatus == SourceProvenanceStatus.Unknown &&
          string.IsNullOrEmpty(localState.ProjectCompatibilityIdentity) &&
          !string.IsNullOrEmpty(localState.LocalRestoreContentIdentity),
        "ordinary-open Apply captures bounded local Restore authority");
    Check(localStates.CanRestorePreviousValues(
            unknown,
            ProgressionType.RequestBoardRewards),
        "ordinary-open Request Board Restore is immediately available");

    ProjectOperationResult localRestore = localExecutor.Execute(
        new RequestBoardRewardsOperation(localService, 100, true),
        unknown);
    Check(localRestore.Succeeded &&
          JToken.DeepEquals(
              Array(unknown, RequestBoardRewardsService.MinimumEntryId),
              baselineMinimum) &&
          JToken.DeepEquals(
              Array(unknown, RequestBoardRewardsService.MaximumEntryId),
              baselineMaximum),
        "ordinary-open Request Board Restore returns both exact baselines");
    ProjectOperationHistoryAction localHistory = new(
        "Request Board Rewards",
        localRestore.MutationResult,
        new ProjectOperationTransactionService());
    localHistory.Undo();
    Check(Value(unknown, RequestBoardRewardsService.MinimumEntryId, 0) == 600,
        "ordinary-open Restore Undo returns to 300 percent");
    localHistory.Redo();
    Check(Value(unknown, RequestBoardRewardsService.MinimumEntryId, 0) == 200,
        "ordinary-open Restore Redo returns to captured baseline");
    ProjectOperationResult localRestoreNoOp = localExecutor.Execute(
        new RequestBoardRewardsOperation(localService, 100, true),
        unknown);
    Check(localRestoreNoOp.Succeeded &&
          !localRestoreNoOp.MutationResult.WasModified,
        "repeated Restore is a successful no-op with no empty history action");

    ProjectModel mismatch = CreateUnknownProject(
        MinArray((0, 200), (1, 175), (2, 150), (3, 125)),
        MaxArray((0, 250), (1, 225), (2, 200), (3, 150)));
    ProjectMutationService mismatchMutation = new();
    GameplayOperationStateService mismatchStates = new(mismatchMutation);
    RequestBoardRewardsService mismatchService = new(
        mismatchMutation,
        mismatchStates);
    _ = localExecutor.Execute(
        new RequestBoardRewardsOperation(mismatchService, 300),
        mismatch);
    Array(mismatch, RequestBoardRewardsService.MinimumEntryId)[0]!["value"] = 601;
    Check(!mismatchStates.CanRestorePreviousValues(
            mismatch,
            ProgressionType.RequestBoardRewards),
        "local authority still rejects expected-current fingerprint mismatch");

    string localRoot = Path.Combine(
        Path.GetTempPath(),
        "WartalesEditorLocalRestoreAuthority",
        Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(localRoot);
    try
    {
        string path = Path.Combine(localRoot, "ordinary-open.cdb");
        ProjectModel persisted = CreateUnknownProject(
            MinArray((0, 200), (1, 175), (2, 150), (3, 125)),
            MaxArray((0, 250), (1, 225), (2, 200), (3, 150)));
        ProjectMutationService persistedMutation = new();
        GameplayOperationStateService persistedStates = new(persistedMutation);
        RequestBoardRewardsService persistedService = new(
            persistedMutation,
            persistedStates);
        _ = localExecutor.Execute(
            new RequestBoardRewardsOperation(persistedService, 300),
            persisted);
        JArray capturedBaseline = (JArray)persisted.GameplayOperationStates
            .Single().BaselineArray.DeepClone();
        JsonDataService data = new();
        data.SaveProject(persisted, path);
        Check(persistedStates.CanRestorePreviousValues(
                persisted,
                ProgressionType.RequestBoardRewards),
            "ordinary-open local Restore authority survives Save");

        ProjectModel reopened = data.LoadProject(path);
        GameplayOperationStateService reopenedStates = new();
        Check(reopened.SourceProvenanceStatus == SourceProvenanceStatus.Unknown &&
              reopened.GameplayOperationStates.Count == 1 &&
              reopened.HistoricalGameplayOperationStates.Count == 0 &&
              reopenedStates.CanRestorePreviousValues(
                  reopened,
                  ProgressionType.RequestBoardRewards),
            "exact-bound local active state survives Save and reopen");
        Check(JToken.DeepEquals(
                reopened.GameplayOperationStates.Single().BaselineArray,
                capturedBaseline),
            "Save and reopen does not recapture 300 percent arrays as baseline");
        RequestBoardRewardsService reopenedService = new(
            new ProjectMutationService(),
            reopenedStates);
        ProjectOperationResult reopenedRestore = localExecutor.Execute(
            new RequestBoardRewardsOperation(reopenedService, 100, true),
            reopened);
        Check(reopenedRestore.Succeeded &&
              Value(reopened, RequestBoardRewardsService.MinimumEntryId, 0) == 200 &&
              Value(reopened, RequestBoardRewardsService.MaximumEntryId, 0) == 250,
            "reopened local Request Board state restores exact Min and Max baselines");

        GameplayStateManifestSnapshot prior =
            data.CaptureGameplayStateForReplacement(path);
        ProjectModel replacement = CreateProject(
            MinArray((0, 200), (1, 175), (2, 150), (3, 125)),
            MaxArray((0, 250), (1, 225), (2, 200), (3, 150)));
        string newSource = "sha256:" + new string('b', 64);
        data.ApplyAuthoritativeImportIdentity(
            replacement,
            newSource,
            prior);
        Check(replacement.GameplayOperationStates.Count == 0 &&
              replacement.HistoricalGameplayOperationStates.All(state =>
                  string.IsNullOrEmpty(state.LocalRestoreContentIdentity)) &&
              !new GameplayOperationStateService().CanRestorePreviousValues(
                  replacement,
                  ProgressionType.RequestBoardRewards),
            "authoritative source replacement rejects stale local-only authority");
    }
    finally
    {
        Directory.Delete(localRoot, recursive: true);
        string parent = Path.GetDirectoryName(localRoot)!;
        if (Directory.Exists(parent) &&
            !Directory.EnumerateFileSystemEntries(parent).Any())
        {
            Directory.Delete(parent);
        }
    }

    Exception? uiFailure = null;
    Thread uiThread = new(() =>
    {
        try
        {
            RunRequestBoardRestoreButtonRegression();
        }
        catch (Exception exception)
        {
            uiFailure = exception;
        }
    });
    uiThread.SetApartmentState(ApartmentState.STA);
    uiThread.Start();
    uiThread.Join();
    if (uiFailure != null)
        throw new InvalidOperationException(
            "FAILED: real Request Board Restore button path",
            uiFailure);
}

void RunRequestBoardRestoreButtonRegression()
{
    WartalesEditor.App application = new();
    application.InitializeComponent();
    Window owner = new()
    {
        Width = 400,
        Height = 300,
        ShowInTaskbar = false,
        WindowStyle = WindowStyle.None
    };
    application.MainWindow = owner;
    owner.Show();

    ProjectModel uiProject = CreateUnknownProject(
        MinArray((0, 200), (1, 175), (2, 150), (3, 125)),
        MaxArray((0, 250), (1, 225), (2, 200), (3, 150)));
    ProjectMutationService uiMutation = new();
    GameplayOperationStateService uiStates = new(uiMutation);
    RequestBoardRewardsService uiService = new(uiMutation, uiStates);
    RequestBoardRewardsDialogViewModel uiViewModel = new(
        uiProject,
        uiService);
    RequestBoardRewardsDialog dialog = new()
    {
        Owner = owner,
        DataContext = uiViewModel,
        ShowInTaskbar = false
    };
    ProjectOperationService uiExecutor = new();
    EditHistoryService history = new();
    ProjectOperationTransactionService transaction = new();
    dialog.ApplyRequested += (_, eventArgs) =>
    {
        ProjectOperationResult result = uiExecutor.Execute(
            new RequestBoardRewardsOperation(
                uiService,
                eventArgs.Percentage,
                eventArgs.RestorePreviousValues),
            uiProject);
        if (!result.Succeeded)
            throw new InvalidOperationException(result.Message);
        if (result.MutationResult.WasModified)
        {
            history.Record(new ProjectOperationHistoryAction(
                "Request Board Rewards",
                result.MutationResult,
                transaction));
        }
        uiViewModel.RefreshFromProject();
    };
    dialog.Show();
    uiViewModel.SelectedPreset = uiViewModel.Presets.Single(option =>
        option.Percentage == 300);
    FindButton(dialog, "Apply").RaiseEvent(
        new RoutedEventArgs(Button.ClickEvent));
    Button restoreButton = FindButton(dialog, "Restore Previous Values");
    Check(restoreButton.IsEnabled,
        "real Request Board Restore button enables after ordinary-open Apply");
    restoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Check(Value(uiProject, RequestBoardRewardsService.MinimumEntryId, 0) == 200 &&
          Value(uiProject, RequestBoardRewardsService.MaximumEntryId, 0) == 250 &&
          history.CanUndo,
        "real Request Board Restore button restores both arrays and records history");
    history.Undo();
    Check(Value(uiProject, RequestBoardRewardsService.MinimumEntryId, 0) == 600,
        "real Restore button history Undo returns to 300 percent");
    history.Redo();
    Check(Value(uiProject, RequestBoardRewardsService.MinimumEntryId, 0) == 200,
        "real Restore button history Redo returns to baseline");
    dialog.Close();
    owner.Close();
    application.Shutdown();
}

Button FindButton(DependencyObject parent, string content)
{
    if (parent is Button button && Equals(button.Content, content))
        return button;
    for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
    {
        try
        {
            return FindButton(
                VisualTreeHelper.GetChild(parent, index),
                content);
        }
        catch (InvalidOperationException)
        {
        }
    }
    throw new InvalidOperationException($"Button '{content}' was not found.");
}

void VerifyInvalid(ProjectModel candidate, string name)
{
    string before = candidate.RootDocument.ToString();
    CheckThrows<Exception>(
        () => RequestBoardRewardsService.ResolveTargets(candidate),
        name);
    Check(candidate.RootDocument.ToString() == before,
        name + " without mutation");
}

void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"FAILED: {name}");
    checks++;
}

void CheckThrows<TException>(Action action, string name)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        checks++;
        return;
    }

    throw new InvalidOperationException($"FAILED: {name}");
}

ProjectModel CreateProject(JArray? minimum, JArray? maximum)
{
    JArray constants = new();
    if (minimum != null)
        constants.Add(Entry(RequestBoardRewardsService.MinimumEntryId, minimum));
    if (maximum != null)
        constants.Add(Entry(RequestBoardRewardsService.MaximumEntryId, maximum));
    constants.Add(new JObject
    {
        ["id"] = "UnrelatedConstant",
        ["value"] = 77
    });
    constants.Add(new JObject
    {
        ["id"] = "MissionGoldCoefUnit",
        ["value"] = 0.8m
    });
    constants.Add(new JObject
    {
        ["id"] = "MissionGoldIncrPerExtraUnit",
        ["value"] = 7
    });
    constants.Add(new JObject
    {
        ["id"] = "MissionNegociationGoldBonus",
        ["value"] = 10
    });
    constants.Add(new JObject
    {
        ["id"] = "MissionNegociationGoldMalus",
        ["value"] = 5
    });
    constants.Add(new JObject
    {
        ["id"] = "MissionNegociationMaxTries",
        ["value"] = 3
    });

    JObject root = new()
    {
        ["sheets"] = new JArray
        {
            new JObject
            {
                ["name"] = "constant",
                ["lines"] = constants
            },
            new JObject
            {
                ["name"] = "mission",
                ["lines"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "Champion",
                        ["goldBonus"] = 50
                    },
                    new JObject
                    {
                        ["id"] = "WeeklyBounty",
                        ["goldBonus"] = 100
                    }
                }
            },
            new JObject
            {
                ["name"] = "path",
                ["lines"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "MissionGold",
                        ["bonus"] = 10
                    }
                }
            },
            new JObject
            {
                ["name"] = "trait",
                ["lines"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "SonOfTrader",
                        ["bonus"] = 10
                    }
                }
            },
            new JObject
            {
                ["name"] = "fief",
                ["lines"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "FiefMissionReward",
                        ["value"] = 999
                    }
                }
            }
        }
    };
    ProjectModel result = new()
    {
        RootDocument = root,
        OriginalJson = root.ToString(),
        FileName = "request-board-test.cdb"
    };
    ProjectModelFactory factory = new();
    foreach (JObject sheet in root["sheets"]!.OfType<JObject>())
        result.Sheets.Add(factory.CreateSheetModel(sheet));
    string identity = "sha256:" + new string('a', 64);
    result.EstablishPersistedIdentity(
        identity,
        identity,
        SourceProvenanceStatus.Verified);
    return result;
}

ProjectModel CreateUnknownProject(JArray? minimum, JArray? maximum)
{
    ProjectModel result = CreateProject(minimum, maximum);
    result.EstablishPersistedIdentity(
        result.CurrentCdbContentIdentity,
        null,
        SourceProvenanceStatus.Unknown);
    return result;
}

JObject Entry(string id, JArray values) => new()
{
    ["id"] = id,
    [RequestBoardRewardsService.PropertyPath] = values
};

JArray MinArray(params (long Difficulty, long Value)[] values) =>
    RewardArray(values);

JArray MaxArray(params (long Difficulty, long Value)[] values) =>
    RewardArray(values);

JArray RewardArray(params (long Difficulty, long Value)[] values) =>
    new(values.Select(value => new JObject
    {
        ["difficulty"] = value.Difficulty,
        ["value"] = value.Value,
        ["future"] = $"preserved-{value.Difficulty}"
    }));

JArray Array(ProjectModel source, string entryId) =>
    (JArray)source.Sheets.Single(sheet => sheet.Name == "constant")
        .Entries.Single(entry => entry.Id == entryId)
        .SourceEntry![RequestBoardRewardsService.PropertyPath]!;

long Value(ProjectModel source, string entryId, long difficulty) =>
    Array(source, entryId)
        .OfType<JObject>()
        .Single(record => record["difficulty"]!.Value<long>() == difficulty)
        ["value"]!.Value<long>();

decimal ScalarValue(
    ProjectModel source,
    string sheetName,
    string entryId,
    string propertyName) =>
    source.Sheets.Single(sheet => sheet.Name == sheetName)
        .Entries.Single(entry => entry.Id == entryId)
        .SourceEntry![propertyName]!.Value<decimal>();

ModProfileModel CloneProfileWithRequest(
    ModProfileModel source,
    JObject settings) => new()
{
    FormatVersion = source.FormatVersion,
    SourceCdbGenerationIdentity = source.SourceCdbGenerationIdentity,
    Metadata = source.Metadata,
    Snapshot = source.Snapshot,
    OperationRequests = new List<ProfileOperationRequestModel>
    {
        new()
        {
            OperationId = ProfileOperationIds.RequestBoardRewards,
            Settings = settings
        }
    }
};

string FindRepositoryRoot()
{
    DirectoryInfo? current = new(AppContext.BaseDirectory);
    while (current != null &&
           !File.Exists(Path.Combine(current.FullName, "WartalesEditor.csproj")))
    {
        current = current.Parent;
    }

    return current?.FullName ?? throw new InvalidOperationException(
        "Repository root was not found.");
}

sealed class RejectAllValidatorProvider : IOperationValidatorProvider
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult) =>
        OperationValidationResult.Failure("Injected validator failure.");
}
