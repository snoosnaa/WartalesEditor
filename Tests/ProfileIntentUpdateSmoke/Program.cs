using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;

int checks = 0;
ModProfileWorkflowService workflow = new();
ModProfileSerializationService serializer = new();
ProjectMutationService mutations = new();

ProjectModel sourceA = CreateProject("A");
ApplyPreset(sourceA, "Fast");
_ = mutations.EnsurePropertyByPath(
    Entry(sourceA, "OrdinaryValue"),
    "value",
    new JValue(2));

ModProfileModel created = workflow.CreateProfile(
    sourceA,
    "Phase 4",
    "capture",
    "test",
    "1.0",
    "phase4");
Check(created.FormatVersion == ModProfileFormat.CurrentVersion,
    "new profile writes the current root format");
Check(created.OperationRequests.Count == 1 &&
      Request(created).Settings!.Value<string>("preset") == "Fast",
    "active compatible gameplay state captures canonical intent");
Check(created.Snapshot.Categories.SelectMany(category => category.Settings)
          .All(setting => setting.Id != "FishingDurationControl"),
    "operation-owned raw leaf is excluded");
Check(created.Snapshot.Categories.Single().Settings.Single().Id ==
          "OrdinaryValue",
    "ordinary non-owned raw leaf is retained");
Check(created.Snapshot.GameplayOperationStates.Count == 1 &&
      created.Snapshot.GameplayOperationStates[0]
          .ProjectCompatibilityIdentity ==
      sourceA.SourceCdbGenerationIdentity &&
      string.IsNullOrEmpty(created.Snapshot.GameplayOperationStates[0]
          .LocalRestoreContentIdentity),
    "retained state is exact-source-only and carries no local authority");

string firstJson = serializer.Serialize(created);
ModProfileModel roundTrip = serializer.Deserialize(firstJson);
Check(serializer.Serialize(roundTrip) == firstJson,
    "current-format serialization is deterministic");

AcceptCurrent(sourceA);
ModProfileModel afterSaveCreate = workflow.CreateProfile(
    sourceA,
    "After Save");
Check(afterSaveCreate.OperationRequests.Count == 1 &&
      !afterSaveCreate.Snapshot.Categories.Any(),
    "saved-clean project still captures active stateful intent");

string beforeUnchangedUpdate = SnapshotProject(sourceA);
ModProfileModel unchanged = workflow.CreateUpdatedProfile(
    sourceA,
    created,
    "unchanged");
Check(JToken.DeepEquals(
        JToken.FromObject(created.OperationRequests),
        JToken.FromObject(unchanged.OperationRequests)),
    "unchanged intent is preserved by stable operation ID");
Check(SnapshotProject(sourceA) == beforeUnchangedUpdate,
    "profile update is read-only for the source project");
workflow.ValidateUpdatedProfileCandidate(sourceA, created, unchanged);
Check(true, "independent candidate validation accepts canonical update");

ApplyPreset(sourceA, "VeryFast");
ModProfileModel replaced = workflow.CreateUpdatedProfile(
    sourceA,
    unchanged,
    "replace");
Check(replaced.OperationRequests.Count == 1 &&
      Request(replaced).Settings!.Value<string>("preset") == "VeryFast",
    "changed canonical intent replaces the prior request");
Check(replaced.Snapshot.Categories.SelectMany(category => category.Settings)
          .All(setting => setting.Id != "FishingDurationControl"),
    "updated profile has no competing operation-owned leaf");

RestorePreset(sourceA);
ModProfileModel restored = workflow.CreateUpdatedProfile(
    sourceA,
    replaced,
    "restore");
Check(restored.OperationRequests.Count == 0 &&
      restored.Snapshot.GameplayOperationStates.Count == 0,
    "authoritative Restore Previous Values removes intent and retained state");
Check(restored.Snapshot.Categories.SelectMany(category => category.Settings)
          .All(setting => setting.Id == "OrdinaryValue"),
    "restoration removes owned raw output without losing ordinary history");

ModProfileModel legacy = new()
{
    FormatVersion = 3,
    Metadata = roundTrip.Metadata,
    Snapshot = roundTrip.Snapshot,
    SourceCdbGenerationIdentity = roundTrip.SourceCdbGenerationIdentity,
    OperationRequests = new List<ProfileOperationRequestModel>
    {
        new() { OperationId = ProfileOperationIds.AddCampFacilities }
    }
};
ProjectModel legacySource = CreateProject("A");
ApplyPreset(legacySource, "Fast");
ModProfileModel migrated = workflow.CreateUpdatedProfile(
    legacySource,
    legacy,
    "migrate");
Check(migrated.FormatVersion == ModProfileFormat.CurrentVersion &&
      migrated.OperationRequests.Any(request =>
          request.OperationId == ProfileOperationIds.FishingSpeed) &&
      migrated.OperationRequests.Any(request =>
          request.OperationId == ProfileOperationIds.AddCampFacilities),
    "legacy update projects stateful intent and preserves additive request");
Check(new ModProfileService().UpdateMetadata(legacy, description: "metadata")
          .FormatVersion == 3,
    "metadata-only editing does not migrate legacy format");

ProjectModel sourceB = CreateProject("B");
var applyB = workflow.ApplyProfile(sourceB, created);
Check(!applyB.HasFailures &&
      Entry(sourceB, "FishingDurationControl").SourceEntry!["value"]!
          .Value<int>() == 2 &&
      Entry(sourceB, "OrdinaryValue").SourceEntry!["value"]!
          .Value<int>() == 2,
    "new format-4 profile applies through Phase 3 on changed source");
Check(sourceB.GameplayOperationStates.Single()
          .ProjectCompatibilityIdentity ==
      sourceB.SourceCdbGenerationIdentity,
    "changed-source apply creates fresh source-B gameplay state");

ModProfileModel updatedFromB = workflow.CreateUpdatedProfile(
    sourceB,
    created,
    "source-b");
Check(updatedFromB.Snapshot.GameplayOperationStates.All(state =>
          state.ProjectCompatibilityIdentity ==
          sourceB.SourceCdbGenerationIdentity) &&
      updatedFromB.Snapshot.GameplayOperationStates.All(state =>
          state.ProjectCompatibilityIdentity !=
          sourceA.SourceCdbGenerationIdentity),
    "changed-source update retains only current-source state");

ProjectModel sourceC = CreateProject("C");
var applyC = workflow.ApplyProfile(sourceC, updatedFromB);
Check(!applyC.HasFailures &&
      Entry(sourceC, "FishingDurationControl").SourceEntry!["value"]!
          .Value<int>() == 2 &&
      sourceC.GameplayOperationStates.Single()
          .ProjectCompatibilityIdentity ==
      sourceC.SourceCdbGenerationIdentity,
    "updated profile remains source-independent on another generation");

ModProfileModel malformed = serializer.Deserialize(
    serializer.Serialize(updatedFromB));
malformed.OperationRequests[0] = new ProfileOperationRequestModel
{
    OperationId = malformed.OperationRequests[0].OperationId,
    Settings = new JObject { ["preset"] = "Faster" }
};
CheckThrows<InvalidOperationException>(
    () => workflow.ValidateUpdatedProfileCandidate(
        sourceB,
        created,
        malformed),
    "candidate validation rejects unexpected intent mutation");

ProfileEffectiveChangeCountService profileCounting = new();
Check(profileCounting.Calculate(created) == 1,
    "profile-only count reports only the intrinsically exact ordinary change");
ProjectModel countTarget = CreateProject("A");
string countTargetBefore = SnapshotProject(countTarget);
int targetCount = profileCounting.Calculate(
    countTarget,
    created,
    out bool targetCountIsExact);
Check(targetCountIsExact && targetCount == 2,
    "target-context count includes one semantic and one ordinary effective change");
Check(SnapshotProject(countTarget) == countTargetBefore,
    "target-context counting rolls back every evaluated mutation");

ProjectModel safeFailureTarget = CreateProject("safe-failure");
PropertyModel safeFailureProperty =
    Entry(safeFailureTarget, "FishingDurationControl")
        .Properties.Single(property =>
            property.EffectivePropertyPath == "value");
string safeFailureBefore = SnapshotProject(safeFailureTarget);
int safeFailureNotifications = 0;
EventHandler<PropertyValueChangedEventArgs> safeFailureObserver =
    (_, _) =>
    {
        safeFailureNotifications++;
        if (safeFailureNotifications == 1)
        {
            throw new InvalidOperationException(
                "Injected evaluation observer failure.");
        }
    };
safeFailureProperty.ValueChanged += safeFailureObserver;
int safeFallbackCount;
bool safeFallbackExact;
try
{
    safeFallbackCount = profileCounting.Calculate(
        safeFailureTarget,
        created,
        out safeFallbackExact);
}
finally
{
    safeFailureProperty.ValueChanged -= safeFailureObserver;
}
Check(!safeFallbackExact &&
      safeFallbackCount == 1 &&
      safeFailureNotifications >= 2 &&
      SnapshotProject(safeFailureTarget) == safeFailureBefore,
    "observer failure with confirmed rollback returns truthful non-exact ordinary count and leaves the project unchanged");

ProjectModel fatalFailureTarget = CreateProject("fatal-failure");
PropertyModel fatalFailureProperty =
    Entry(fatalFailureTarget, "FishingDurationControl")
        .Properties.Single(property =>
            property.EffectivePropertyPath == "value");
int fatalFailureNotifications = 0;
EventHandler<PropertyValueChangedEventArgs> fatalFailureObserver =
    (_, _) =>
    {
        fatalFailureNotifications++;
        throw new InvalidOperationException(
            "Injected mutation and rollback observer failure.");
    };
fatalFailureProperty.ValueChanged += fatalFailureObserver;
bool fatalFailureSurfaced = false;
try
{
    _ = profileCounting.Calculate(
        fatalFailureTarget,
        created,
        out _);
}
catch (ProjectRollbackIntegrityException)
{
    fatalFailureSurfaced = true;
}
finally
{
    fatalFailureProperty.ValueChanged -= fatalFailureObserver;
}
Check(fatalFailureSurfaced && fatalFailureNotifications >= 2,
    "mutation plus rollback observer failure surfaces fatal rollback-integrity failure instead of a normal count");

string fatalLibraryPath = Path.Combine(
    Path.GetTempPath(),
    $"wartales-phase4-fatal-{Guid.NewGuid():N}");
try
{
    ModProfileLibraryService fatalLibrary = new(
        new ModProfileLibraryPathService(fatalLibraryPath),
        serializer,
        profileCounting);
    _ = fatalLibrary.AddProfile(created);
    ProjectModel fatalLibraryTarget = CreateProject("fatal-library");
    PropertyModel fatalLibraryProperty =
        Entry(fatalLibraryTarget, "FishingDurationControl")
            .Properties.Single(property =>
                property.EffectivePropertyPath == "value");
    int libraryObserverNotifications = 0;
    EventHandler<PropertyValueChangedEventArgs> fatalLibraryObserver =
        (_, _) => libraryObserverNotifications++;
    fatalLibraryProperty.ValueChanged += fatalLibraryObserver;
    try
    {
        ModProfileSummaryModel summary =
            fatalLibrary.GetProfiles(fatalLibraryTarget).Single();
        Check(libraryObserverNotifications == 0 &&
              !summary.IsEffectiveChangeCountExact,
            "profile library does not observationally apply profiles for its primary count");
    }
    finally
    {
        fatalLibraryProperty.ValueChanged -= fatalLibraryObserver;
    }
}
finally
{
    if (Directory.Exists(fatalLibraryPath))
        Directory.Delete(fatalLibraryPath, recursive: true);
}

ProjectModel directOwnedCreateSource = CreateProject("direct-create");
_ = mutations.EnsurePropertyByPath(
    Entry(directOwnedCreateSource, "FishingDurationControl"),
    "value",
    new JValue(99));
ModProfileModel directOwnedCreate = workflow.CreateProfile(
    directOwnedCreateSource,
    "Direct owned value");
Check(directOwnedCreate.OperationRequests.Count == 0 &&
      directOwnedCreate.Snapshot.Categories
          .SelectMany(category => category.Settings)
          .Single(setting => setting.Id == "FishingDurationControl")
          .Properties.Single().CurrentValue.Value<int>() == 99,
    "new profile preserves a direct operation-owned edit when no semantic state exists");

ModProfileSummaryModel nonExactSummary = new()
{
    EffectiveChangeCount = 1,
    OperationCount = 2,
    IsEffectiveChangeCountExact = false
};
Check(nonExactSummary.ChangeSummaryText ==
          "Unavailable",
    "missing historical impact authority displays Unavailable");

string summaryLibraryPath = Path.Combine(
    Path.GetTempPath(),
    $"wartales-phase4-summary-{Guid.NewGuid():N}");
try
{
    ModProfileLibraryService summaryLibrary = new(
        new ModProfileLibraryPathService(summaryLibraryPath),
        serializer,
        profileCounting);
    _ = summaryLibrary.AddProfile(created);
    ModProfileSummaryModel withoutTarget =
        summaryLibrary.GetProfiles().Single();
    ModProfileSummaryModel withTarget =
        summaryLibrary.GetProfiles(CreateProject("A")).Single();
    Check(!withoutTarget.IsEffectiveChangeCountExact &&
          withoutTarget.EffectiveChangeCount == 0 &&
          withoutTarget.OperationCount == 1 &&
          withoutTarget.ChangeSummaryText ==
              "Unavailable",
        "profile library reports unavailable without historical authority");
    Check(!withTarget.IsEffectiveChangeCountExact &&
          withTarget.EffectiveChangeCount == 0 &&
          withTarget.ChangeSummaryText == "Unavailable",
        "current target does not become primary Profile Changes authority");
}
finally
{
    if (Directory.Exists(summaryLibraryPath))
        Directory.Delete(summaryLibraryPath, recursive: true);
}

string allModsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "Wartales Editor",
    "Profiles",
    "All Mods.wtprofile");
string goldenPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "Wartales Editor",
    "Golden CDB",
    "data.cdb");
if (File.Exists(allModsPath) && File.Exists(goldenPath))
{
    ModProfileModel allMods = serializer.Load(allModsPath);
    ProjectModel controlledA = new JsonDataService().LoadProject(goldenPath);
    string controlledAIdentity = new CdbGenerationIdentityService().Calculate(
        File.ReadAllBytes(goldenPath));
    controlledA.EstablishPersistedIdentity(
        controlledAIdentity,
        controlledAIdentity,
        SourceProvenanceStatus.Verified);
    var legacyApply = workflow.ApplyProfile(controlledA, allMods);
    Check(!legacyApply.HasFailures,
        "safe in-memory All Mods copy applies to controlled Golden source");

    string controlledBeforeUpdate = SnapshotProject(controlledA);
    ModProfileModel allModsV4 = workflow.CreateUpdatedProfile(
        controlledA,
        allMods,
        "phase4-all-mods");
    Check(allModsV4.FormatVersion == ModProfileFormat.CurrentVersion &&
          allMods.Snapshot.GameplayOperationStates.Count == 26 &&
          allModsV4.OperationRequests.Count(request =>
              new ProfileOperationIntentRegistry().GetOperationType(
                  request.OperationId) != null) == 27,
        "All Mods safe copy projects legacy states and preserves canonical Request Board intent in the current format");
    Check(allMods.OperationRequests.Where(request =>
              request.OperationId is
                  ProfileOperationIds.AddCampFacilities or
                  ProfileOperationIds.UpgradeAllEquipment)
          .All(existing => allModsV4.OperationRequests.Any(candidate =>
              candidate.OperationId == existing.OperationId)),
        "All Mods migration preserves additive requests");
    Check(SnapshotProject(controlledA) == controlledBeforeUpdate,
        "All Mods candidate creation does not mutate controlled source");
    workflow.ValidateUpdatedProfileCandidate(
        controlledA,
        allMods,
        allModsV4);
    Check(true, "All Mods format-4 candidate passes independent validation");

    string[] representativeOperations =
    {
        ProfileOperationIds.CharacterXp,
        ProfileOperationIds.StartingResources,
        ProfileOperationIds.VolunteerWages,
        ProfileOperationIds.ValourPoints,
        ProfileOperationIds.CarryingCapacity,
        ProfileOperationIds.OverworldMovementSpeed,
        ProfileOperationIds.RainFrequency,
        ProfileOperationIds.RandomTraitExclusions,
        ProfileOperationIds.FishingSpeed,
        ProfileOperationIds.BattleCameraZoom,
        ProfileOperationIds.RequestBoardRewards
    };
    foreach (string operationId in representativeOperations)
    {
        ModProfileModel operationProfile = RequestOnly(
            allModsV4,
            operationId);
        ProjectModel countProject = LoadVerifiedProject(goldenPath);
        string countBefore = SnapshotProject(countProject);
        int count = profileCounting.Calculate(
            countProject,
            operationProfile,
            out bool exact);
        ProjectModel applyProject = LoadVerifiedProject(goldenPath);
        ModificationSnapshotImportResultModel actual =
            workflow.ApplyProfile(applyProject, operationProfile);
        Check(exact &&
              count > 0 &&
              count == actual.AppliedEffectiveChangeCount &&
              actual.EffectiveChangeCount ==
                  actual.AppliedEffectiveChangeCount &&
              SnapshotProject(countProject) == countBefore,
            $"{operationId} target-context count equals authoritative Apply output ({count})");
    }

    ModProfileModel twoResources = WithSettings(
        allModsV4,
        ProfileOperationIds.StartingResources,
        new JObject
        {
            ["krowns"] = 1,
            ["bread"] = 1,
            ["apples"] = 0,
            ["ironOre"] = 0,
            ["wood"] = 0,
            ["cloth"] = 0
        });
    VerifyExactCount(
        twoResources,
        LoadVerifiedProject(goldenPath),
        2,
        "Starting Resources changes only two selected shared resources");

    ProjectModel partyBaseline = LoadVerifiedProject(goldenPath);
    JArray volunteerBaseline = PartyEconomyService.CaptureTargets(
        partyBaseline,
        ProgressionType.VolunteerWages);
    ModProfileModel oneVolunteer = WithSettings(
        allModsV4,
        ProfileOperationIds.VolunteerWages,
        new JObject
        {
            ["volunteerPercentage"] =
                volunteerBaseline[0]!.Value<int>("value") + 1
        });
    VerifyExactCount(
        oneVolunteer,
        LoadVerifiedProject(goldenPath),
        1,
        "Volunteer Wages changes one effective field");

    JArray valourBaseline = PartyEconomyService.CaptureTargets(
        partyBaseline,
        ProgressionType.ValourPoints);
    ModProfileModel twoValour = WithSettings(
        allModsV4,
        ProfileOperationIds.ValourPoints,
        new JObject
        {
            ["maximumValour"] = valourBaseline[0]!.Value<int>("value") + 1,
            ["restoredValour"] = valourBaseline[1]!.Value<int>("value") + 1,
            ["tentTier1Valour"] = valourBaseline[2]!.Value<int>("value"),
            ["tentTier2Valour"] = valourBaseline[3]!.Value<int>("value"),
            ["tentTier3Valour"] = valourBaseline[4]!.Value<int>("value")
        });
    VerifyExactCount(
        twoValour,
        LoadVerifiedProject(goldenPath),
        2,
        "Valour Points counts only its two changed fields");

    JArray carryingBaseline = PartyEconomyService.CaptureTargets(
        partyBaseline,
        ProgressionType.CarryingCapacity);
    ModProfileModel twoCarrying = WithSettings(
        allModsV4,
        ProfileOperationIds.CarryingCapacity,
        new JObject
        {
            ["saddlebagCapacity"] = carryingBaseline[0]!.Value<int>("value") + 1,
            ["ponyStartingCapacity"] = carryingBaseline[1]!.Value<int>("value") + 1,
            ["hitchingPostTier1Base"] = carryingBaseline[2]!.Value<int>("value"),
            ["hitchingPostTier2Base"] = carryingBaseline[3]!.Value<int>("value"),
            ["hitchingPostTier3Base"] = carryingBaseline[4]!.Value<int>("value"),
            ["hitchingPostTier1Trait"] = 0,
            ["hitchingPostTier2Trait"] = carryingBaseline[5]!.Value<int>("value"),
            ["hitchingPostTier3Trait"] = carryingBaseline[6]!.Value<int>("value")
        });
    VerifyExactCount(
        twoCarrying,
        LoadVerifiedProject(goldenPath),
        2,
        "Carrying Capacity counts only its two changed fields");

    ModProfileModel movementProfile = RequestOnly(
        allModsV4,
        ProfileOperationIds.OverworldMovementSpeed);
    OverworldMovementPreset movementPreset = Enum.Parse<
        OverworldMovementPreset>(
        movementProfile.OperationRequests.Single().Settings!
            .Value<string>("preset")!);
    OverworldMovementPresetOption movementValues =
        OverworldMovementSpeedService.Presets.Single(option =>
            option.Preset == movementPreset);
    ProjectModel oneMovementTarget = LoadVerifiedProject(goldenPath);
    _ = mutations.EnsurePropertyByPath(
        FindEntry(
            oneMovementTarget,
            "constant",
            OverworldMovementSpeedService.RunEntryId),
        "value",
        new JValue(movementValues.RunSpeed));
    AcceptAsBaseline(oneMovementTarget);
    VerifyExactCount(
        movementProfile,
        oneMovementTarget,
        1,
        "Run Speed counts one changed value when the other already matches");

    ModProfileModel rainProfile = RequestOnly(
        allModsV4,
        ProfileOperationIds.RainFrequency);
    ProjectModel rainExpected = LoadVerifiedProject(goldenPath);
    _ = workflow.ApplyProfile(rainExpected, rainProfile);
    ProjectModel partialRainTarget = LoadVerifiedProject(goldenPath);
    ProfileOwnedSnapshotLeaf[] rainLeaves = new ProfileOperationReplayService()
        .GetOwnedSnapshotLeaves(
            partialRainTarget,
            rainProfile.OperationRequests.Single())
        .ToArray();
    for (int index = 3; index < rainLeaves.Length; index++)
    {
        ProfileOwnedSnapshotLeaf leaf = rainLeaves[index];
        JToken expectedValue = FindProperty(rainExpected, leaf)!
            .SourceProperty!.Value;
        _ = mutations.EnsurePropertyByPath(
            FindEntry(partialRainTarget, leaf.SheetName, leaf.EntryId),
            leaf.PropertyPath,
            expectedValue);
    }
    AcceptAsBaseline(partialRainTarget);
    VerifyExactCount(
        rainProfile,
        partialRainTarget,
        3,
        "Rain Frequency counts only three regions that remain different");

    string[] directConflictOperations =
    {
        ProfileOperationIds.FishingSpeed,
        ProfileOperationIds.OverworldMovementSpeed,
        ProfileOperationIds.CharacterXp,
        ProfileOperationIds.RandomTraitExclusions
    };
    ProfileOperationReplayService replay = new();
    foreach (string operationId in directConflictOperations)
    {
        ModProfileModel historical = RequestOnly(allModsV4, operationId);
        ProjectModel conflictProject = LoadVerifiedProject(goldenPath);
        ProfileOwnedSnapshotLeaf owned = replay
            .GetOwnedSnapshotLeaves(
                conflictProject,
                historical.OperationRequests.Single())
            .First(leaf => FindProperty(conflictProject, leaf) != null);
        PropertyModel ownedProperty = FindProperty(conflictProject, owned)!;
        _ = mutations.EnsurePropertyByPath(
            FindEntry(conflictProject, owned.SheetName, owned.EntryId),
            owned.PropertyPath,
            DifferentValue(ownedProperty.SourceProperty!.Value));
        string historicalBefore = serializer.Serialize(historical);
        string projectBefore = SnapshotProject(conflictProject);
        CheckThrows<InvalidOperationException>(
            () => workflow.CreateUpdatedProfile(
                conflictProject,
                historical,
                $"conflict-{operationId}"),
            $"{operationId} historical intent rejects an unexplained direct owned edit");
        Check(serializer.Serialize(historical) == historicalBefore &&
              SnapshotProject(conflictProject) == projectBefore,
            $"{operationId} ambiguity rejection leaves profile and project unchanged");
    }

    string[] updateFamilyOperations =
    {
        ProfileOperationIds.CharacterXp,
        ProfileOperationIds.OverworldMovementSpeed,
        ProfileOperationIds.StartingResources,
        ProfileOperationIds.VolunteerWages,
        ProfileOperationIds.RainFrequency,
        ProfileOperationIds.RequestBoardRewards,
        ProfileOperationIds.RandomTraitExclusions,
        ProfileOperationIds.FishingSpeed
    };
    ProfileOperationIntentRegistry registry = new();
    foreach (string operationId in updateFamilyOperations)
    {
        ModProfileModel historical = RequestOnly(allModsV4, operationId);

        ProjectModel replacementProject = LoadVerifiedProject(goldenPath);
        JObject replacementSettings = CreateChangedSettings(
            operationId,
            historical.OperationRequests.Single().Settings!,
            replacementProject);
        ModProfileModel replacementProfile = WithSettings(
            allModsV4,
            operationId,
            replacementSettings);
        ModificationSnapshotImportResultModel replacementApply =
            workflow.ApplyProfile(replacementProject, replacementProfile);
        Check(!replacementApply.HasFailures,
            $"{operationId} replacement fixture applies changed authoritative state");
        ProfileOwnedSnapshotLeaf unrelatedReplacement =
            AddUnrelatedOrdinaryEdit(
                replacementProject,
                replay.GetOwnedSnapshotLeaves(
                    replacementProject,
                    replacementProfile.OperationRequests.Single()));
        string replacementBefore = SnapshotProject(replacementProject);
        ModProfileModel replacementCandidate = workflow.CreateUpdatedProfile(
            replacementProject,
            historical,
            $"replace-{operationId}");
        ProfileOperationRequestModel replacedRequest =
            replacementCandidate.OperationRequests.Single(request =>
                request.OperationId == operationId);
        registry.ValidateRequest(
            replacedRequest,
            replacementCandidate.FormatVersion);
        Check(JToken.DeepEquals(
                  replacedRequest.Settings,
                  replacementSettings) &&
              ContainsSnapshotLeaf(
                  replacementCandidate,
                  unrelatedReplacement) &&
              ExcludesOwnedSnapshotLeaves(
                  replacementProject,
                  replacementCandidate,
                  replacedRequest,
                  replay) &&
              SnapshotProject(replacementProject) == replacementBefore,
            $"{operationId} Update replaces canonical intent, excludes owned raw leaves, retains unrelated edits, and is observational");

        ProjectModel removalProject = LoadVerifiedProject(goldenPath);
        JObject baselineSettings = CreateBaselineSettings(
            operationId,
            historical.OperationRequests.Single().Settings!,
            removalProject);
        ModificationSnapshotImportResultModel historicalApply =
            workflow.ApplyProfile(removalProject, historical);
        Check(!historicalApply.HasFailures,
            $"{operationId} removal fixture establishes historical authoritative state");
        ModProfileModel baselineProfile = WithSettings(
            allModsV4,
            operationId,
            baselineSettings);
        Check(RestoreToBaseline(
                operationId,
                removalProject,
                baselineProfile),
            $"{operationId} removal fixture restores its captured baseline");
        ProfileOwnedSnapshotLeaf unrelatedRemoval =
            AddUnrelatedOrdinaryEdit(
                removalProject,
                replay.GetOwnedSnapshotLeaves(
                    removalProject,
                    historical.OperationRequests.Single()));
        string removalBefore = SnapshotProject(removalProject);
        ModProfileModel removalCandidate = workflow.CreateUpdatedProfile(
            removalProject,
            historical,
            $"remove-{operationId}");
        Check(removalCandidate.OperationRequests.All(request =>
                  request.OperationId != operationId),
            $"{operationId} Update removes restored intent");
        Check(ContainsSnapshotLeaf(removalCandidate, unrelatedRemoval),
            $"{operationId} removal retains unrelated ordinary edits");
        Check(SnapshotProject(removalProject) == removalBefore,
            $"{operationId} removal Update is observational");

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    ProjectModel controlledB = new JsonDataService().LoadProject(goldenPath);
    controlledB.RootDocument["phase4Generation"] = "B";
    string controlledBIdentity = new CdbGenerationIdentityService().Calculate(
        Encoding.UTF8.GetBytes(controlledB.RootDocument.ToString()));
    controlledB.EstablishPersistedIdentity(
        controlledBIdentity,
        controlledBIdentity,
        SourceProvenanceStatus.Verified);
    var v4Apply = workflow.ApplyProfile(controlledB, allModsV4);
    Check(!v4Apply.HasFailures,
        "migrated All Mods format-4 profile applies cross-generation");
    Check(controlledB.GameplayOperationStates.All(state =>
          state.ProjectCompatibilityIdentity == controlledBIdentity),
        "All Mods cross-generation replay creates only fresh target state");
}
else
{
    Console.WriteLine(
        "ALL MODS PHASE 4 CONTROLLED MIGRATION SKIPPED: fixture files not present");
}

Console.WriteLine($"ALL PROFILE INTENT UPDATE CHECKS PASSED ({checks})");

void ApplyPreset(ProjectModel project, string preset)
{
    ProjectMutationService serviceMutations = new();
    GameplayPresetService service = new(
        serviceMutations,
        new GameplayOperationStateService(serviceMutations));
    var result = new ProjectOperationService().Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.FishingSpeed,
            preset),
        project);
    Check(result.Succeeded, $"{preset} fixture operation succeeds");
}

void RestorePreset(ProjectModel project)
{
    ProjectMutationService serviceMutations = new();
    GameplayPresetService service = new(
        serviceMutations,
        new GameplayOperationStateService(serviceMutations));
    var result = new ProjectOperationService().Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.FishingSpeed,
            "Vanilla",
            true),
        project);
    Check(result.Succeeded, "restore fixture operation succeeds");
}

void AcceptCurrent(ProjectModel project)
{
    foreach (PropertyModel property in project.Sheets
                 .SelectMany(sheet => sheet.Entries)
                 .SelectMany(entry => entry.Properties))
        property.AcceptCurrentValue();
    new GameplayOperationStateService().AcceptCurrentStates(project);
    project.IsGameplayOperationStateModified = false;
}

ProjectModel CreateProject(string generation)
{
    JObject sheet = new()
    {
        ["name"] = "constant",
        ["columns"] = new JArray(
            new JObject { ["typeStr"] = "0", ["name"] = "id" },
            new JObject { ["typeStr"] = "3", ["name"] = "value" }),
        ["lines"] = new JArray(
            new JObject { ["id"] = "FishingDurationControl", ["value"] = 6 },
            new JObject { ["id"] = "OrdinaryValue", ["value"] = 1 },
            new JObject { ["id"] = $"Generation{generation}", ["value"] = 1 })
    };
    JObject root = new() { ["sheets"] = new JArray(sheet) };
    ProjectModel project = new()
    {
        FileName = $"source-{generation}.cdb",
        OriginalJson = root.ToString(),
        RootDocument = root
    };
    project.Sheets.Add(new ProjectModelFactory().CreateSheetModel(sheet));
    string identity = new CdbGenerationIdentityService().Calculate(
        Encoding.UTF8.GetBytes(project.OriginalJson));
    project.EstablishPersistedIdentity(
        identity,
        identity,
        SourceProvenanceStatus.Verified);
    return project;
}

ProjectModel LoadVerifiedProject(string fileName)
{
    ProjectModel project = new JsonDataService().LoadProject(fileName);
    string identity = new CdbGenerationIdentityService().Calculate(
        File.ReadAllBytes(fileName));
    project.EstablishPersistedIdentity(
        identity,
        identity,
        SourceProvenanceStatus.Verified);
    return project;
}

ModProfileModel RequestOnly(ModProfileModel source, string operationId)
{
    ModProfileModel profile = serializer.Deserialize(
        serializer.Serialize(source));
    profile.ImpactManifest = null;
    ProfileOperationRequestModel request = profile.OperationRequests
        .Single(candidate => candidate.OperationId == operationId);
    profile.OperationRequests.Clear();
    profile.OperationRequests.Add(request);
    profile.Snapshot.Categories.Clear();
    profile.Snapshot.GameplayOperationStates.Clear();
    return profile;
}

ModProfileModel WithSettings(
    ModProfileModel source,
    string operationId,
    JObject settings)
{
    ModProfileModel profile = RequestOnly(source, operationId);
    profile.OperationRequests.Clear();
    profile.OperationRequests.Add(new ProfileOperationRequestModel
    {
        OperationId = operationId,
        Settings = settings
    });
    return profile;
}

JObject CreateChangedSettings(
    string operationId,
    JObject current,
    ProjectModel project)
{
    JObject changed = (JObject)current.DeepClone();
    switch (operationId)
    {
        case ProfileOperationIds.CharacterXp:
            changed["percentage"] =
                current.Value<int>("percentage") == 80 ? 70 : 80;
            break;
        case ProfileOperationIds.OverworldMovementSpeed:
            changed["preset"] = OverworldMovementSpeedService.Presets
                .Select(option => option.Preset.ToString())
                .First(value => value != OverworldMovementPreset.Vanilla.ToString() &&
                                value != current.Value<string>("preset"));
            break;
        case ProfileOperationIds.StartingResources:
            changed["krowns"] = current.Value<int>("krowns") + 1;
            break;
        case ProfileOperationIds.VolunteerWages:
            int volunteer = current.Value<int>("volunteerPercentage");
            changed["volunteerPercentage"] = volunteer == 50 ? 49 : 50;
            break;
        case ProfileOperationIds.RainFrequency:
            changed["preset"] = RainFrequencyService.Presets
                .Select(option => option.Preset.ToString())
                .First(value => value != RainFrequencyPreset.Vanilla.ToString() &&
                                value != current.Value<string>("preset"));
            break;
        case ProfileOperationIds.RequestBoardRewards:
            int rewards = current.Value<int>("percentage");
            changed["percentage"] = rewards == 150 ? 200 : 150;
            break;
        case ProfileOperationIds.RandomTraitExclusions:
            JObject firstTrait = changed["traits"]!.OfType<JObject>().First();
            firstTrait["allowed"] = !firstTrait.Value<bool>("allowed");
            break;
        case ProfileOperationIds.FishingSpeed:
            changed["preset"] = GameplayPresetCatalog
                .Get(ProgressionType.FishingSpeed)
                .Presets.Select(option => option.Key)
                .First(value => value != "Vanilla" &&
                                value != current.Value<string>("preset"));
            break;
        default:
            throw new InvalidOperationException(
                $"No changed-settings fixture exists for '{operationId}'.");
    }

    return changed;
}

JObject CreateBaselineSettings(
    string operationId,
    JObject historical,
    ProjectModel cleanProject)
{
    switch (operationId)
    {
        case ProfileOperationIds.CharacterXp:
            return new JObject { ["percentage"] = 100 };
        case ProfileOperationIds.OverworldMovementSpeed:
            ProjectMutationService movementMutations = new();
            OverworldMovementPreset movement =
                new OverworldMovementSpeedService(
                    movementMutations,
                    new GameplayOperationStateService(movementMutations))
                .DetectPreset(cleanProject);
            return new JObject { ["preset"] = movement.ToString() };
        case ProfileOperationIds.StartingResources:
            return new JObject
            {
                ["krowns"] = 0,
                ["bread"] = 0,
                ["apples"] = 0,
                ["ironOre"] = 0,
                ["wood"] = 0,
                ["cloth"] = 0
            };
        case ProfileOperationIds.VolunteerWages:
            return new JObject { ["volunteerPercentage"] = 0 };
        case ProfileOperationIds.RainFrequency:
            ProjectMutationService rainMutations = new();
            RainFrequencyPreset rain = new RainFrequencyService(
                rainMutations,
                new GameplayOperationStateService(rainMutations))
                .DetectPreset(cleanProject);
            return new JObject { ["preset"] = rain.ToString() };
        case ProfileOperationIds.RequestBoardRewards:
            return new JObject { ["percentage"] = 100 };
        case ProfileOperationIds.RandomTraitExclusions:
            ProjectMutationService traitMutations = new();
            IReadOnlyDictionary<string, bool> allowed =
                new RandomTraitExclusionsService(
                    traitMutations,
                    new GameplayOperationStateService(traitMutations))
                .Discover(cleanProject)
                .ToDictionary(candidate => candidate.Id, candidate => candidate.IsAllowed);
            JObject selections = (JObject)historical.DeepClone();
            foreach (JObject trait in selections["traits"]!.OfType<JObject>())
                trait["allowed"] = allowed[trait.Value<string>("id")!];
            return selections;
        case ProfileOperationIds.FishingSpeed:
            ProjectMutationService presetMutations = new();
            GameplayPresetOption preset = new GameplayPresetService(
                presetMutations,
                new GameplayOperationStateService(presetMutations))
                .DetectPreset(cleanProject, ProgressionType.FishingSpeed)
                ?? throw new InvalidOperationException(
                    "The clean Fishing Speed fixture has no canonical preset.");
            return new JObject { ["preset"] = preset.Key };
        default:
            throw new InvalidOperationException(
                $"No baseline-settings fixture exists for '{operationId}'.");
    }
}

ProfileOwnedSnapshotLeaf AddUnrelatedOrdinaryEdit(
    ProjectModel project,
    IEnumerable<ProfileOwnedSnapshotLeaf> ownedLeaves)
{
    HashSet<ProfileOwnedSnapshotLeaf> owned = ownedLeaves.ToHashSet();
    var candidate = project.Sheets
        .SelectMany(sheet => sheet.Entries.SelectMany(entry =>
            entry.Properties.Select(property => new
            {
                Sheet = sheet,
                Entry = entry,
                Property = property,
                Leaf = new ProfileOwnedSnapshotLeaf(
                    sheet.Name,
                    entry.Id,
                    property.EffectivePropertyPath)
            })))
        .First(item =>
            !owned.Contains(item.Leaf) &&
            !item.Property.IsReadOnly &&
            item.Property.SourceProperty?.Value.Type is
                JTokenType.Integer or JTokenType.Float or JTokenType.Boolean);
    _ = mutations.EnsurePropertyByPath(
        candidate.Entry,
        candidate.Property.EffectivePropertyPath,
        DifferentValue(candidate.Property.SourceProperty!.Value));
    return candidate.Leaf;
}

bool ContainsSnapshotLeaf(
    ModProfileModel profile,
    ProfileOwnedSnapshotLeaf expected) =>
    profile.Snapshot.Categories.Any(category =>
        category.Name == expected.SheetName &&
        category.Settings.Any(setting =>
            setting.Id == expected.EntryId &&
            setting.Properties.Any(property =>
                (string.IsNullOrWhiteSpace(property.PropertyPath)
                    ? property.Name
                    : property.PropertyPath) == expected.PropertyPath)));

bool ExcludesOwnedSnapshotLeaves(
    ProjectModel project,
    ModProfileModel profile,
    ProfileOperationRequestModel request,
    ProfileOperationReplayService replay)
{
    HashSet<ProfileOwnedSnapshotLeaf> owned = replay
        .GetOwnedSnapshotLeaves(project, request)
        .ToHashSet();
    return profile.Snapshot.Categories
        .SelectMany(category => category.Settings.SelectMany(setting =>
            setting.Properties.Select(property =>
                new ProfileOwnedSnapshotLeaf(
                    category.Name,
                    setting.Id,
                    string.IsNullOrWhiteSpace(property.PropertyPath)
                        ? property.Name
                        : property.PropertyPath))))
        .All(leaf => !owned.Contains(leaf));
}

bool RestoreToBaseline(
    string operationId,
    ProjectModel project,
    ModProfileModel baselineProfile)
{
    ProjectMutationService serviceMutations = new();
    GameplayOperationStateService states = new(serviceMutations);
    ProjectOperationService operations = new();

    ProjectOperationResult? result = operationId switch
    {
        ProfileOperationIds.OverworldMovementSpeed =>
            operations.Execute(
                new OverworldMovementSpeedOperation(
                    new OverworldMovementSpeedService(
                        serviceMutations,
                        states),
                    OverworldMovementPreset.Vanilla,
                    restorePreviousValues: true),
                project),
        ProfileOperationIds.RainFrequency =>
            operations.Execute(
                new RainFrequencyOperation(
                    new RainFrequencyService(
                        serviceMutations,
                        states),
                    RainFrequencyPreset.Vanilla,
                    restorePreviousValues: true),
                project),
        ProfileOperationIds.RequestBoardRewards =>
            operations.Execute(
                new RequestBoardRewardsOperation(
                    new RequestBoardRewardsService(
                        serviceMutations,
                        states),
                    100,
                    restorePreviousValues: true),
                project),
        ProfileOperationIds.FishingSpeed =>
            operations.Execute(
                new GameplayPresetOperation(
                    new GameplayPresetService(
                        serviceMutations,
                        states),
                    ProgressionType.FishingSpeed,
                    "Vanilla",
                    restorePreviousValues: true),
                project),
        ProfileOperationIds.RandomTraitExclusions =>
            RestoreRandomTraitExclusions(
                project,
                serviceMutations,
                states,
                operations),
        ProfileOperationIds.VolunteerWages =>
            RestoreVolunteerWages(
                project,
                serviceMutations,
                states,
                operations),
        _ => null
    };

    return result?.Succeeded ??
           !workflow.ApplyProfile(project, baselineProfile).HasFailures;
}

ProjectOperationResult RestoreVolunteerWages(
    ProjectModel project,
    ProjectMutationService mutationService,
    GameplayOperationStateService states,
    ProjectOperationService operations)
{
    PartyEconomyService service = new(mutationService, states);
    PartyEconomySettings baseline = service.GetBaselineSettings(
        project,
        ProgressionType.VolunteerWages);
    return operations.Execute(
        new PartyEconomyOperation(
            service,
            ProgressionType.VolunteerWages,
            baseline),
        project);
}

ProjectOperationResult RestoreRandomTraitExclusions(
    ProjectModel project,
    ProjectMutationService mutationService,
    GameplayOperationStateService states,
    ProjectOperationService operations)
{
    RandomTraitExclusionsService service = new(mutationService, states);
    RandomTraitExclusionRestoreSelectionResult selection =
        service.ResolvePreviousAllowedTraitIds(project);
    if (selection.Status != RandomTraitExclusionRestoreStatus.Succeeded)
    {
        return ProjectOperationResult.Failure(
            "The Random Trait Exclusions restore fixture was unavailable.");
    }

    return operations.Execute(
        new RandomTraitExclusionsOperation(
            service,
            selection.AllowedTraitIds),
        project);
}

void VerifyExactCount(
    ModProfileModel profile,
    ProjectModel target,
    int expected,
    string message)
{
    string before = SnapshotProject(target);
    int actual = profileCounting.Calculate(target, profile, out bool exact);
    Check(exact && actual == expected && SnapshotProject(target) == before,
        $"{message} (count {actual})");
}

void AcceptAsBaseline(ProjectModel project)
{
    foreach (PropertyModel property in project.Sheets
                 .SelectMany(sheet => sheet.Entries)
                 .SelectMany(entry => entry.Properties))
    {
        property.AcceptCurrentValue();
    }

    project.GameplayOperationStates.Clear();
    project.IsGameplayOperationStateModified = false;
    project.OriginalJson = project.RootDocument.ToString();
    string identity = new CdbGenerationIdentityService().Calculate(
        Encoding.UTF8.GetBytes(project.OriginalJson));
    project.EstablishPersistedIdentity(
        identity,
        identity,
        SourceProvenanceStatus.Verified);
}

EntryModel FindEntry(
    ProjectModel project,
    string sheetName,
    string entryId) =>
    project.Sheets.Single(sheet => sheet.Name == sheetName)
        .Entries.Single(entry => entry.Id == entryId);

PropertyModel? FindProperty(
    ProjectModel project,
    ProfileOwnedSnapshotLeaf leaf) =>
    FindEntry(project, leaf.SheetName, leaf.EntryId)
        .Properties.SingleOrDefault(property =>
            property.EffectivePropertyPath == leaf.PropertyPath);

JToken DifferentValue(JToken value)
{
    if (value.Type == JTokenType.Integer)
        return new JValue(value.Value<long>() + 1);
    if (value.Type == JTokenType.Float)
        return new JValue(value.Value<decimal>() + 1m);
    if (value.Type == JTokenType.Boolean)
        return new JValue(!value.Value<bool>());
    if (value is JArray array)
    {
        JArray changed = (JArray)array.DeepClone();
        JValue? number = changed.Descendants()
            .OfType<JValue>()
            .FirstOrDefault(item => item.Type is
                JTokenType.Integer or JTokenType.Float);
        if (number?.Type == JTokenType.Integer)
        {
            number.Replace(new JValue(number.Value<long>() + 1));
            return changed;
        }
        if (number?.Type == JTokenType.Float)
        {
            number.Replace(new JValue(number.Value<decimal>() + 1m));
            return changed;
        }
    }

    throw new InvalidOperationException(
        $"The focused fixture cannot alter token type '{value.Type}'.");
}

EntryModel Entry(ProjectModel project, string id) =>
    project.Sheets.Single().Entries.Single(entry => entry.Id == id);

ProfileOperationRequestModel Request(ModProfileModel profile) =>
    profile.OperationRequests.Single(request =>
        request.OperationId == ProfileOperationIds.FishingSpeed);

string SnapshotProject(ProjectModel project) =>
    project.RootDocument.ToString(Newtonsoft.Json.Formatting.None) + "|" +
    JToken.FromObject(project.GameplayOperationStates)
        .ToString(Newtonsoft.Json.Formatting.None) + "|" +
    string.Join(
        ";",
        project.GameplayOperationStates.Select(state =>
            $"{state.OperationType}:{state.IsCompatible}:" +
            $"{state.CompatibilityMessage}:" +
            $"{state.PersistedStateFingerprint}")) + "|" +
    JToken.FromObject(project.HistoricalGameplayOperationStates)
        .ToString(Newtonsoft.Json.Formatting.None) + "|" +
    project.IsModified + "|" +
    project.IsGameplayOperationStateModified + "|" +
    project.SourceCdbGenerationIdentity + "|" +
    project.CurrentCdbContentIdentity + "|" +
    project.SourceProvenanceStatus + "|" +
    string.Join(
        ";",
        project.Sheets.SelectMany(sheet => sheet.Entries.SelectMany(entry =>
                entry.Properties
                    .Where(property => property.IsModified)
                    .Select(property =>
                        $"{sheet.Name}/{entry.Id}/" +
                        property.EffectivePropertyPath)))
            .OrderBy(value => value, StringComparer.Ordinal));

void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException($"FAIL: {message}");
    checks++;
    Console.WriteLine($"PASS {message}");
}

void CheckThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        Check(true, message);
        return;
    }

    throw new InvalidOperationException($"FAIL: {message}");
}
