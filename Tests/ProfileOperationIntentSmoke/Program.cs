using Newtonsoft.Json.Linq;
using System.IO;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;

int checks = 0;
ProfileOperationIntentRegistry registry = new();
ModProfileSerializationService serializer = new();

string[] expectedIds =
{
    ProfileOperationIds.CharacterXp,
    ProfileOperationIds.ProfessionXp,
    ProfileOperationIds.StartingResources,
    ProfileOperationIds.VolunteerWages,
    ProfileOperationIds.ValourPoints,
    ProfileOperationIds.CarryingCapacity,
    ProfileOperationIds.OverworldMovementSpeed,
    ProfileOperationIds.RainFrequency,
    ProfileOperationIds.DeliciousMealChance,
    ProfileOperationIds.ForgingAssistance,
    ProfileOperationIds.MiningWoodcuttingTiming,
    ProfileOperationIds.FishingSpeed,
    ProfileOperationIds.LockpickingTolerance,
    ProfileOperationIds.NinePuzzleAssistance,
    ProfileOperationIds.RunStaminaRecovery,
    ProfileOperationIds.BattleCameraZoom,
    ProfileOperationIds.CampfireExpansion,
    ProfileOperationIds.CookingPotFoodReduction,
    ProfileOperationIds.WorkshopMaterials,
    ProfileOperationIds.VendorRefresh,
    ProfileOperationIds.RubySapphireValue,
    ProfileOperationIds.TimeBetweenRests,
    ProfileOperationIds.ResourceReplenishment,
    ProfileOperationIds.LecternKnowledgeGain,
    ProfileOperationIds.PositiveRandomTraits,
    ProfileOperationIds.RandomTraitExclusions,
    ProfileOperationIds.RequestBoardRewards,
    ProfileOperationIds.PathLevelRequirements,
    ProfileOperationIds.PathXpRewardsMight,
    ProfileOperationIds.PathXpRewardsTrade,
    ProfileOperationIds.PathXpRewardsCrime,
    ProfileOperationIds.PathXpRewardsMystery,
    ProfileOperationIds.AddCampFacilities,
    ProfileOperationIds.UpgradeAllEquipment
};
Check(registry.OperationIds.Count == 34, "34 canonical operation IDs registered");
Check(registry.OperationIds.SequenceEqual(
        expectedIds.OrderBy(id => id, StringComparer.Ordinal)),
    "registered operation IDs are complete and deterministic");

for (int version = 1; version <= ModProfileFormat.CurrentVersion; version++)
{
    ProfileOperationRequestModel[] legacyRequests = version == 1
        ? Array.Empty<ProfileOperationRequestModel>()
        : new[]
        {
            Request(
                ProfileOperationIds.RequestBoardRewards,
                new JObject { ["percentage"] = 300 }),
            Request(ProfileOperationIds.AddCampFacilities)
        };
    ModProfileModel profile = Profile(version, legacyRequests);
    ModProfileModel loaded = serializer.Deserialize(serializer.Serialize(profile));
    Check(loaded.FormatVersion == version, $"format {version} loads");
    Check(loaded.OperationRequests.Count == legacyRequests.Length,
        $"format {version} preserves compatible existing requests");
}

ModProfileModel metadataUpdated = new ModProfileService().UpdateMetadata(
    Profile(3, new[] { Request(ProfileOperationIds.AddCampFacilities) }),
    description: "Metadata only");
Check(metadataUpdated.FormatVersion == 3,
    "metadata-only edits do not migrate an older profile format");

ProfileOperationRequestModel character = Project(
    State(ProgressionType.Character, percentage: 40));
Check(character.OperationId == ProfileOperationIds.CharacterXp &&
      character.Settings!["percentage"]!.Value<int>() == 40,
    "Character XP legacy percentage projected");

ProfileOperationRequestModel profession = Project(
    State(ProgressionType.Profession, percentage: 50));
Check(profession.OperationId == ProfileOperationIds.ProfessionXp &&
      profession.Settings!["percentage"]!.Value<int>() == 50,
    "Profession XP legacy percentage projected");

GameplayOperationStateModel startingState = State(
    ProgressionType.StartingResources,
    startingResources: new StartingResourcesSettings
    {
        Krowns = 100,
        Bread = 2,
        Apples = 3,
        IronOre = 4,
        Wood = 5,
        Cloth = 6
    });
ProfileOperationRequestModel starting = Project(startingState);
Check(starting.Settings!.Properties().Select(p => p.Name).SequenceEqual(
        new[] { "krowns", "bread", "apples", "ironOre", "wood", "cloth" }) &&
      starting.Settings["krowns"]!.Value<int>() == 100,
    "Starting Resources projected with canonical fields");

JObject volunteerSettings = new() { ["volunteerPercentage"] = 75 };
JObject valourSettings = new()
{
    ["maximumValour"] = 10,
    ["restoredValour"] = 4,
    ["tentTier1Valour"] = 1,
    ["tentTier2Valour"] = 3,
    ["tentTier3Valour"] = 5
};
JObject carryingSettings = new()
{
    ["saddlebagCapacity"] = 20,
    ["ponyStartingCapacity"] = 80,
    ["hitchingPostTier1Base"] = 10,
    ["hitchingPostTier2Base"] = 20,
    ["hitchingPostTier3Base"] = 30,
    ["hitchingPostTier1Trait"] = 0,
    ["hitchingPostTier2Trait"] = 5,
    ["hitchingPostTier3Trait"] = 10
};
Check(JToken.DeepEquals(
        Project(State(ProgressionType.VolunteerWages,
            gameplaySettings: volunteerSettings)).Settings,
        volunteerSettings),
    "Volunteer Wages settings projected");
Check(JToken.DeepEquals(
        Project(State(ProgressionType.ValourPoints,
            gameplaySettings: valourSettings)).Settings,
        valourSettings),
    "Valour Points settings projected");
Check(JToken.DeepEquals(
        Project(State(ProgressionType.CarryingCapacity,
            gameplaySettings: carryingSettings)).Settings,
        carryingSettings),
    "Carrying Capacity settings projected");

ProfileOperationRequestModel movement = Project(State(
    ProgressionType.OverworldMovementSpeed,
    gameplaySettings: Preset("Faster")));
Check(movement.OperationId == ProfileOperationIds.OverworldMovementSpeed &&
      movement.Settings!["preset"]!.Value<string>() == "Faster",
    "Run Speed stable preset identifier projected");

ProfileOperationRequestModel rain = Project(State(
    ProgressionType.RainFrequency,
    gameplaySettings: Preset("RareRain")));
Check(rain.Settings!["preset"]!.Value<string>() == "RareRain",
    "Rain Frequency preset projected");

Dictionary<ProgressionType, string> genericPresets = new()
{
    [ProgressionType.DeliciousMealChance] = "Improved",
    [ProgressionType.ForgingAssistance] = "Easier",
    [ProgressionType.MiningWoodcuttingTiming] = "Easy",
    [ProgressionType.FishingSpeed] = "Faster",
    [ProgressionType.LockpickingTolerance] = "Easier",
    [ProgressionType.NinePuzzleAssistance] = "Easy",
    [ProgressionType.RunStaminaRecovery] = "Fast",
    [ProgressionType.BattleCameraZoom] = "Extended",
    [ProgressionType.CampfireExpansion] = "Expanded",
    [ProgressionType.CookingPotFoodReduction] = "Improved",
    [ProgressionType.WorkshopMaterials] = "High",
    [ProgressionType.VendorRefresh] = "VeryFast",
    [ProgressionType.RubySapphireValue] = "Higher",
    [ProgressionType.TimeBetweenRests] = "Longer",
    [ProgressionType.ResourceReplenishment] = "Faster",
    [ProgressionType.LecternKnowledgeGain] = "Increased",
    [ProgressionType.PositiveRandomTraits] = "PositiveOnly"
};
foreach ((ProgressionType type, string preset) in genericPresets)
{
    ProfileOperationRequestModel projected = Project(State(
        type,
        gameplaySettings: Preset(preset)));
    Check(projected.Settings!["preset"]!.Value<string>() == preset,
        $"{type} preset projected");
}

GameplayOperationStateModel requestBoardState = State(
    ProgressionType.RequestBoardRewards,
    percentage: 300,
    gameplaySettings: new JObject { ["percentage"] = 300 });
ProfileOperationRequestModel requestBoard = Project(requestBoardState);
Check(requestBoard.OperationId == ProfileOperationIds.RequestBoardRewards &&
      requestBoard.Settings!["percentage"]!.Value<int>() == 300,
    "Request Board percentage projected");

GameplayOperationStateModel traitsState = State(
    ProgressionType.RandomTraitExclusions,
    gameplaySettings: new JObject
    {
        ["allowedTraitIds"] = new JArray("Brave")
    },
    baseline: new JArray(
        Trait("Cruel", RandomTraitPersonality.Negative, "Starting", "True"),
        Trait("Brave", RandomTraitPersonality.Positive, "Starting", "True")));
ProfileOperationRequestModel traits = Project(traitsState);
JArray semanticTraits = (JArray)traits.Settings!["traits"]!;
Check(semanticTraits.Count == 2 &&
      semanticTraits[0]!["id"]!.Value<string>() == "Brave" &&
      semanticTraits[0]!["personality"]!.Value<string>() == "Positive" &&
      semanticTraits[0]!["allowed"]!.Value<bool>() &&
      semanticTraits[1]!["id"]!.Value<string>() == "Cruel" &&
      !semanticTraits[1]!["allowed"]!.Value<bool>(),
    "Random Trait intent uses ordered semantic identities and selections");

GameplayOperationStateModel restored = State(
    ProgressionType.OverworldMovementSpeed,
    gameplaySettings: Preset("PreviousValues"));
Check(registry.TryProjectLegacyIntent(
        restored,
        out ProfileOperationRequestModel? noIntent,
        out string noIntentError) &&
      noIntent == null && noIntentError.Length == 0,
    "explicit previous-values state projects as no effective intent");

ProfileOperationRequestModel noOpCharacter = Project(State(
    ProgressionType.Character,
    percentage: 100,
    baselineFingerprint: "same",
    expectedFingerprint: "same"));
Check(noOpCharacter.Settings!["percentage"]!.Value<int>() == 100,
    "already-satisfied state still preserves explicit intent");

Check(!registry.TryProjectLegacyIntent(
        State(ProgressionType.PositiveRandomTraits,
            gameplaySettings: new JObject()),
        out _,
        out string projectionError) &&
      projectionError.Contains("cannot be projected", StringComparison.Ordinal),
    "unsafe legacy projection reports a compatibility failure");

ProfileOperationRequestModel addCamp = Request(ProfileOperationIds.AddCampFacilities);
ProfileOperationRequestModel upgrade = Request(ProfileOperationIds.UpgradeAllEquipment);
registry.ValidateRequest(addCamp, 2);
registry.ValidateRequest(upgrade, 2);
registry.ValidateRequest(requestBoard, 2);
Check(true, "existing additive and Request Board requests remain valid");

CheckThrows<InvalidOperationException>(
    () => registry.ValidateRequest(Request("unknown-operation"), 4),
    "unknown operation ID rejected");
CheckThrows<InvalidOperationException>(
    () => registry.ValidateRequest(
        Request(ProfileOperationIds.CharacterXp,
            new JObject { ["percentage"] = "40" }), 4),
    "malformed percentage rejected");
CheckThrows<InvalidOperationException>(
    () => registry.ValidateRequest(
        Request(ProfileOperationIds.PositiveRandomTraits,
            Preset("FriendlyOnly")), 4),
    "invalid preset key rejected");
CheckThrows<InvalidOperationException>(
    () => registry.ValidateRequest(
        Request(ProfileOperationIds.StartingResources,
            new JObject { ["krowns"] = 1 }), 4),
    "incomplete Starting Resources settings rejected");
CheckThrows<InvalidOperationException>(
    () => registry.ValidateRequest(
        new ProfileOperationRequestModel
        {
            FormatVersion = 2,
            OperationId = ProfileOperationIds.CharacterXp,
            Settings = new JObject { ["percentage"] = 40 }
        }, 4),
    "unsupported request format rejected");
CheckThrows<ModProfileSerializationException>(
    () => serializer.Serialize(Profile(
        4,
        new[] { character, character })),
    "duplicate operation intent rejected");
CheckThrows<ModProfileSerializationException>(
    () => serializer.Serialize(Profile(
        3,
        new[] { character })),
    "new stateful intent rejected in pre-intent profile format");

ModProfileModel roundTripProfile = Profile(
    4,
    new[]
    {
        character,
        movement,
        traits,
        requestBoard,
        addCamp,
        upgrade
    });
string first = serializer.Serialize(roundTripProfile);
string second = serializer.Serialize(serializer.Deserialize(first));
Check(first == second, "Format 4 serialization round trip is deterministic");

JArray serializedRequests = (JArray)JObject.Parse(first)["OperationRequests"]!;
Check(serializedRequests.All(token =>
        token["Settings"] is not JObject settings ||
        !ContainsForbiddenIntentMember(settings)),
    "intent payloads contain no baseline, path, fingerprint, or source authority");

ModProfileModel retainedStateProfile = Profile(
    4,
    new[] { character },
    new[] { State(ProgressionType.Character, percentage: 40) });
string retainedJson = serializer.Serialize(retainedStateProfile);
JObject retainedRoot = JObject.Parse(retainedJson);
Check(retainedRoot["Snapshot"]!["GameplayOperationStates"]!.Any(),
    "Format 4 retains exact-source Gameplay Operation State separately");
Check(retainedRoot["OperationRequests"]![0]!["Settings"]!["BaselineArray"] == null,
    "retained state baseline is not copied into intent");

ModProfileModel mismatchedStateProfile = Profile(
    4,
    new[] { Request(ProfileOperationIds.CharacterXp,
        new JObject { ["percentage"] = 50 }) },
    new[] { State(ProgressionType.Character, percentage: 40) });
CheckThrows<ModProfileSerializationException>(
    () => serializer.Serialize(mismatchedStateProfile),
    "Format 4 intent and retained state mismatch rejected");

ProjectModel captureProject = new();
captureProject.GameplayOperationStates.Add(State(
    ProgressionType.PositiveRandomTraits,
    gameplaySettings: Preset("PositiveOnly")));
ModProfileModel captured = new ModProfileService().CreateProfile(
    captureProject,
    "Intent Capture");
Check(captured.FormatVersion == 4 &&
      captured.OperationRequests.Count == 0 &&
      captured.Snapshot.GameplayOperationStates.Count == 0,
    "new profile does not capture intent from state without restore authority");
Check(captured.Snapshot.Categories.Count == 0,
    "Phase 1 ordinary snapshot capture remains otherwise unchanged");

string allModsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "Wartales Editor",
    "Profiles",
    "All Mods.wtprofile");
if (File.Exists(allModsPath))
{
    ModProfileModel allMods = serializer.Load(allModsPath);
    int projectedCount = 0;
    foreach (GameplayOperationStateModel state in
             allMods.Snapshot.GameplayOperationStates)
    {
        bool succeeded = registry.TryProjectLegacyIntent(
            state,
            out ProfileOperationRequestModel? projected,
            out string error);
        Check(succeeded, $"All Mods {state.OperationType} projects: {error}");
        if (projected != null) projectedCount++;
    }

    Check(projectedCount > 0,
        "existing All Mods profile exposes recognizable legacy intent");
    Console.WriteLine(
        $"ALL MODS LEGACY INTENTS PROJECTED ({projectedCount})");
}
else
{
    Console.WriteLine("ALL MODS PROFILE NOT PRESENT; OPTIONAL CHECK SKIPPED");
}

Console.WriteLine($"ALL PROFILE OPERATION INTENT CHECKS PASSED ({checks})");
return;

ProfileOperationRequestModel Project(GameplayOperationStateModel state)
{
    bool succeeded = registry.TryProjectLegacyIntent(
        state,
        out ProfileOperationRequestModel? result,
        out string error);
    if (!succeeded || result == null)
        throw new InvalidOperationException(error.Length == 0
            ? "State produced no intent."
            : error);
    return result;
}

static ModProfileModel Profile(
    int formatVersion,
    IEnumerable<ProfileOperationRequestModel>? requests = null,
    IEnumerable<GameplayOperationStateModel>? states = null)
{
    ModificationSnapshotModel snapshot = new()
    {
        FormatVersion = formatVersion == 1 ? 1 : 2,
        SourceFileName = "data.cdb",
        SourceCdbGenerationIdentity = formatVersion >= 3
            ? new string('a', 64)
            : null,
        GameplayOperationStates = states?.ToList() ?? new()
    };

    return new ModProfileModel
    {
        FormatVersion = formatVersion,
        SourceCdbGenerationIdentity = formatVersion >= 3
            ? new string('a', 64)
            : null,
        Metadata = new ModProfileMetadataModel
        {
            Name = "Intent Test",
            ProfileVersion = "1.0",
            CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ModifiedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        },
        Snapshot = snapshot,
        OperationRequests = requests?.ToList() ?? new()
    };
}

static ProfileOperationRequestModel Request(
    string operationId,
    JObject? settings = null) =>
    new()
    {
        OperationId = operationId,
        Settings = settings
    };

static GameplayOperationStateModel State(
    ProgressionType type,
    int percentage = 100,
    JObject? gameplaySettings = null,
    StartingResourcesSettings? startingResources = null,
    JArray? baseline = null,
    string baselineFingerprint = "baseline",
    string expectedFingerprint = "expected") =>
    new()
    {
        OperationType = type,
        AppliedPercentage = percentage,
        GameplaySettings = gameplaySettings,
        StartingResources = startingResources,
        BaselineArray = baseline ?? new JArray(1),
        BaselineFingerprint = baselineFingerprint,
        ExpectedCurrentFingerprint = expectedFingerprint,
        ElementCount = baseline?.Count ?? 1,
        ElementShapeFingerprint = "shape"
    };

static JObject Preset(string preset) =>
    new() { ["preset"] = preset };

static JObject Trait(
    string id,
    RandomTraitPersonality personality,
    string group,
    string doneState) =>
    new()
    {
        ["id"] = id,
        ["personality"] = (int)personality,
        ["group"] = group,
        ["doneState"] = doneState
    };

static bool ContainsForbiddenIntentMember(JObject value) =>
    value.DescendantsAndSelf()
        .OfType<JProperty>()
        .Any(property => property.Name.Contains("baseline", StringComparison.OrdinalIgnoreCase) ||
                         property.Name.Contains("fingerprint", StringComparison.OrdinalIgnoreCase) ||
                         property.Name.Contains("source", StringComparison.OrdinalIgnoreCase) ||
                         property.Name.Contains("identity", StringComparison.OrdinalIgnoreCase) ||
                         property.Name.Contains("path", StringComparison.OrdinalIgnoreCase));

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
