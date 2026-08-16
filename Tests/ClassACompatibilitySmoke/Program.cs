using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.ViewModels;

VerifyScalarVanillaRestoration();
VerifyMiningBaselineScaling();
VerifyVendorBaselineScaling();
VerifyResourceReplenishment();
VerifyCampfireExpansion();
VerifyBattleCameraBaselineDrift();
VerifyLegacyValour();
VerifyLegacyCarrying();
VerifySnapshotPathCompatibility();
VerifyApplyFeedbackState();
VerifyCatalogCoverage();
VerifyMalformedTargets();

Console.WriteLine("ALL CLASS A COMPATIBILITY CHECKS PASSED");

static void VerifyScalarVanillaRestoration()
{
    ProjectModel project = CreateProject(
        Sheet("constant", ScalarEntry("FishingDurationControl", 7.5)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ProjectOperationResult fast = executor.Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.FishingSpeed,
            "Fast"),
        project);
    Check(fast.Succeeded, "differing scalar baseline applies");
    CheckNumber(project, "constant", "FishingDurationControl", "value", 2);

    GameplayOperationStateModel state = project.GameplayOperationStates.Single();
    CheckTokenNumber(state.BaselineArray[0]!["value"]!, 7.5,
        "differing scalar baseline captured");

    string fastJson = Json(project);
    ProjectOperationResult vanilla = executor.Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.FishingSpeed,
            "Vanilla"),
        project);
    Check(vanilla.Succeeded, "scalar Vanilla applies");
    CheckNumber(project, "constant", "FishingDurationControl", "value", 7.5);
    CheckTokenNumber(project.GameplayOperationStates.Single().BaselineArray[0]!["value"]!,
        7.5, "scalar baseline remains unchanged");
    CheckPresetState(project, ProgressionType.FishingSpeed, "Vanilla");

    string vanillaJson = Json(project);
    VerifyUndoRedo(
        "scalar Vanilla",
        vanilla,
        project,
        fastJson,
        vanillaJson);
    Console.WriteLine("PASS differing-baseline Vanilla restoration");
}

static void VerifyMiningBaselineScaling()
{
    ProjectModel project = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("MiningSpeedCircleMin", 2.0),
            ScalarEntry("MiningSpeedCircleMax", 3.5)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ApplyPreset(executor, service, project,
        ProgressionType.MiningWoodcuttingTiming, "Easier");
    CheckMining(project, 1.6, 2.8, "80 percent");

    ApplyPreset(executor, service, project,
        ProgressionType.MiningWoodcuttingTiming, "Easy");
    CheckMining(project, 1.2, 2.1, "60 percent");

    ProjectOperationResult veryEasy = ApplyPreset(
        executor,
        service,
        project,
        ProgressionType.MiningWoodcuttingTiming,
        "VeryEasy");
    CheckMining(project, 0.8, 1.4, "40 percent");
    string scaledJson = Json(project);

    ProjectOperationResult vanilla = ApplyPreset(
        executor,
        service,
        project,
        ProgressionType.MiningWoodcuttingTiming,
        "Vanilla");
    CheckMining(project, 2.0, 3.5, "Vanilla");
    CheckTokenNumber(
        project.GameplayOperationStates.Single().BaselineArray[0]!["value"]!,
        2.0,
        "Mining minimum baseline remains unchanged");
    CheckTokenNumber(
        project.GameplayOperationStates.Single().BaselineArray[1]!["value"]!,
        3.5,
        "Mining maximum baseline remains unchanged");
    CheckPresetState(
        project,
        ProgressionType.MiningWoodcuttingTiming,
        "Vanilla");
    VerifyUndoRedo(
        "Mining Vanilla",
        vanilla,
        project,
        scaledJson,
        Json(project));

    ProjectModel unsupported = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("MiningSpeedCircleMin", 4.0),
            ScalarEntry("MiningSpeedCircleMax", 3.0)));
    string before = Json(unsupported);
    ProjectOperationResult rejected = ApplyPreset(
        executor,
        CreatePresetService(),
        unsupported,
        ProgressionType.MiningWoodcuttingTiming,
        "Easy",
        expectSuccess: false);
    Check(!rejected.Succeeded, "unsupported Mining baseline rejected");
    Check(Json(unsupported) == before && unsupported.GameplayOperationStates.Count == 0,
        "unsupported Mining baseline fails before mutation");
    _ = veryEasy;
    Console.WriteLine("PASS Mining baseline-relative scaling");
}

static void VerifyVendorBaselineScaling()
{
    ProjectModel project = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("MerchantRefillPerDaySlow", 0.4),
            ScalarEntry("MerchantRefillPerDayNormal", 1.2),
            ScalarEntry("MerchantRefillPerDayFast", 4.0),
            ScalarEntry("MerchantFullRefillDays", 17)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ApplyPreset(executor, service, project, ProgressionType.VendorRefresh, "Faster");
    CheckNumber(project, "constant", "MerchantRefillPerDaySlow", "value", 0.8);
    CheckNumber(project, "constant", "MerchantRefillPerDayNormal", "value", 2.4);
    CheckNumber(project, "constant", "MerchantRefillPerDayFast", "value", 8.0);
    CheckNumber(project, "constant", "MerchantFullRefillDays", "value", 10);

    ApplyPreset(executor, service, project, ProgressionType.VendorRefresh, "Vanilla");
    CheckNumber(project, "constant", "MerchantRefillPerDaySlow", "value", 0.4);
    CheckNumber(project, "constant", "MerchantRefillPerDayNormal", "value", 1.2);
    CheckNumber(project, "constant", "MerchantRefillPerDayFast", "value", 4.0);
    CheckNumber(project, "constant", "MerchantFullRefillDays", "value", 17);
    Console.WriteLine("PASS Vendor baseline-relative rates");
}

static void VerifyResourceReplenishment()
{
    ProjectModel project = CreateResourceProject(0.2, 0.5, 1.4, 1.75, 19);
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ProjectOperationResult faster = ApplyPreset(
        executor, service, project,
        ProgressionType.ResourceReplenishment, "Faster");
    CheckResourceValues(project, 0.4, 1.0, 2.8);
    CheckNumber(project, "constant", "GatherRefillFactorExtreme", "value", 1.75);
    CheckNumber(project, "constant", "UnrelatedFixture", "value", 19);
    Check(faster.MutationResult.UpdatedProperties.Count == 3,
        "Resource Faster changes only three refill targets");

    string fasterJson = Json(project);
    ProjectOperationResult same = ApplyPreset(
        executor, service, project,
        ProgressionType.ResourceReplenishment, "Faster");
    Check(!same.MutationResult.WasModified && Json(project) == fasterJson,
        "Resource same-preset Apply is idempotent");

    ApplyPreset(executor, service, project,
        ProgressionType.ResourceReplenishment, "Fast");
    CheckResourceValues(project, 0.6, 1.5, 4.2);
    ApplyPreset(executor, service, project,
        ProgressionType.ResourceReplenishment, "VeryFast");
    CheckResourceValues(project, 1.0, 2.5, 7.0);
    string veryFastJson = Json(project);

    var profile = new ModProfileService().CreateProfile(
        project, "Resource Replenishment smoke");
    ProjectModel profileTarget =
        CreateResourceProject(0.2, 0.5, 1.4, 1.75, 19);
    ModificationSnapshotImportResultModel profileResult =
        new ModProfileWorkflowService().ApplyProfile(profileTarget, profile);
    Check(!profileResult.HasFailures,
        "Resource profile replay succeeds");
    CheckResourceValues(profileTarget, 1.0, 2.5, 7.0);
    CheckNumber(profileTarget, "constant", "GatherRefillFactorExtreme", "value", 1.75);
    VerifyGameplayStateFileRoundTrip(
        project,
        profileTarget,
        ProgressionType.ResourceReplenishment);

    ProjectOperationResult vanilla = ApplyPreset(
        executor, service, project,
        ProgressionType.ResourceReplenishment, "Vanilla");
    CheckResourceValues(project, 0.2, 0.5, 1.4);
    CheckNumber(project, "constant", "GatherRefillFactorExtreme", "value", 1.75);
    VerifyUndoRedo(
        "Resource Vanilla", vanilla, project, veryFastJson, Json(project));

    GameplayOperationStateModel state = project.GameplayOperationStates.Single();
    Check(state.BaselineArray.Count == 3,
        "Resource baseline contains all three categories");
    CheckTokenNumber(state.BaselineArray[0]!["value"]!, 0.2,
        "Resource differing Slow baseline preserved");
    VerifySnapshotStateRoundTrip(project, ProgressionType.ResourceReplenishment, 3);
    VerifyProfileStateRoundTrip(project, ProgressionType.ResourceReplenishment, 3);

    foreach (string missing in new[]
             {
                 "GatherRefillSlow", "GatherRefillNormal", "GatherRefillFast"
             })
    {
        ProjectModel malformed = CreateResourceProject(0.2, 0.5, 1.4, 1.75, 19,
            missing);
        string before = Json(malformed);
        Check(!ExecutePreset(
                malformed,
                ProgressionType.ResourceReplenishment,
                "Faster").Succeeded,
            $"Resource missing {missing} fails safely");
        Check(Json(malformed) == before,
            $"Resource missing {missing} rolls back without mutation");
    }

    foreach ((object slow, object normal, object fast, string label) invalid in new[]
             {
                 ((object)"invalid", (object)0.5, (object)1.4, "wrong type"),
                 ((object)0.0, (object)0.5, (object)1.4, "zero"),
                 ((object)(-0.1), (object)0.5, (object)1.4, "negative"),
                 ((object)0.5, (object)0.5, (object)1.4, "equal"),
                 ((object)0.7, (object)0.5, (object)1.4, "unordered")
             })
    {
        ProjectModel malformed = CreateResourceProject(
            invalid.slow, invalid.normal, invalid.fast, 1.75, 19);
        string before = Json(malformed);
        Check(!ExecutePreset(
                malformed,
                ProgressionType.ResourceReplenishment,
                "VeryFast").Succeeded,
            $"Resource {invalid.label} baseline rejected");
        Check(Json(malformed) == before,
            $"Resource {invalid.label} failure is atomic");
    }

    Console.WriteLine("PASS Resource Replenishment baseline scaling and safety");
}

static void VerifyCampfireExpansion()
{
    ProjectModel project = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2"),
            CampfireEntry("FirecampT3")));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ProjectOperationResult expanded = ApplyPreset(
        executor, service, project,
        ProgressionType.CampfireExpansion, "Expanded");
    CheckCampfire(project, "Firecamp", 6, 4);
    CheckCampfire(project, "FirecampT2", 6, 8);
    CheckCampfire(project, "FirecampT3", 6, 12);
    Check(expanded.MutationResult.UpdatedProperties.Count == 16,
        "Campfire Expanded records only values that changed");
    string expandedJson = Json(project);

    ProjectOperationResult same = ApplyPreset(
        executor, service, project,
        ProgressionType.CampfireExpansion, "Expanded");
    Check(!same.MutationResult.WasModified && Json(project) == expandedJson,
        "Campfire reapply is idempotent");

    ProjectOperationResult vanilla = ApplyPreset(
        executor, service, project,
        ProgressionType.CampfireExpansion, "Vanilla");
    CheckCampfire(project, "Firecamp", 4, 4);
    CheckCampfire(project, "FirecampT2", 4, 4);
    CheckCampfire(project, "FirecampT3", 4, 4);
    CheckPresetState(project, ProgressionType.CampfireExpansion, "Vanilla");
    VerifyUndoRedo(
        "Campfire Vanilla", vanilla, project, expandedJson, Json(project));
    VerifyProfileStateRoundTrip(project, ProgressionType.CampfireExpansion, 18);

    ProjectModel missingCapacity = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2", omitCapacity: true),
            CampfireEntry("FirecampT3")));
    Check(!ExecutePreset(
            missingCapacity,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire missing runtime capacity fails safely");

    ProjectModel wrongType = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2", capacity: "invalid"),
            CampfireEntry("FirecampT3")));
    Check(!ExecutePreset(
            wrongType,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire wrong runtime capacity type fails safely");

    ProjectModel missingToolCapacity = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2", omitToolCapacity: true),
            CampfireEntry("FirecampT3")));
    string missingToolCapacityBefore = Json(missingToolCapacity);
    Check(!ExecutePreset(
            missingToolCapacity,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire missing source tool capacity fails safely");
    Check(Json(missingToolCapacity) == missingToolCapacityBefore,
        "Campfire missing source tool capacity fails before accepted mutation");

    ProjectModel wrongToolCapacityType = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2", toolCapacity: "invalid"),
            CampfireEntry("FirecampT3")));
    string wrongToolCapacityBefore = Json(wrongToolCapacityType);
    Check(!ExecutePreset(
            wrongToolCapacityType,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire wrong source tool capacity type fails safely");
    Check(Json(wrongToolCapacityType) == wrongToolCapacityBefore,
        "Campfire wrong source tool capacity type fails before accepted mutation");

    ProjectModel partialTier = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2")));
    Check(!ExecutePreset(
            partialTier,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire partial tier fails safely");

    Console.WriteLine("PASS Campfire coordinated dimensions and assignment capacity");
}

static void VerifyBattleCameraBaselineDrift()
{
    ProjectModel project = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("CameraMinDistance", 35),
            ScalarEntry("CameraMaxDistance", 45)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ProjectOperationResult far = ApplyPreset(
        executor,
        service,
        project,
        ProgressionType.BattleCameraZoom,
        "Far");
    CheckNumber(project, "constant", "CameraMinDistance", "value", 35);
    CheckNumber(project, "constant", "CameraMaxDistance", "value", 48);
    Check(
        far.MutationResult.UpdatedProperties.All(
            property => property.EffectivePropertyPath != "value" ||
                        property.SourceProperty?.Parent is not JObject owner ||
                        owner.Value<string>("id") != "CameraMinDistance"),
        "Battle Camera non-Vanilla does not mutate minimum");

    ApplyPreset(executor, service, project,
        ProgressionType.BattleCameraZoom, "Vanilla");
    CheckNumber(project, "constant", "CameraMinDistance", "value", 35);
    CheckNumber(project, "constant", "CameraMaxDistance", "value", 45);
    Console.WriteLine("PASS Battle Camera baseline drift");
}

static void VerifyLegacyValour()
{
    VerifyLegacyValourSupported();
    VerifyLegacyValourCustom();
    Console.WriteLine("PASS legacy Valour compatibility and upgrade");
}

static void VerifyLegacyValourSupported()
{
    ProjectModel project = CreateValourProject(2, 3, 4);
    AddLegacyPartyState(project, ProgressionType.ValourPoints);
    PartyEconomyService service = CreatePartyService();
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.ValourPoints);

    Check(viewModel.SelectedTentPreset == "Increased",
        "legacy Valour detects supported current Tent preset");
    Check(viewModel.CanApply, "legacy Valour supported preset can upgrade");
    string before = Json(project);
    ProjectOperationResult upgrade = ExecuteParty(
        project,
        service,
        ProgressionType.ValourPoints,
        viewModel.CreateSettings());
    Check(upgrade.Succeeded, "legacy Valour supported upgrade applies");
    GameplayOperationStateModel expanded = project.GameplayOperationStates.Single();
    Check(expanded.ElementCount == 5 && expanded.BaselineArray.Count == 5,
        "legacy Valour expands to five targets");
    CheckPartyBaseline(expanded, 2, 3, 4);

    string after = Json(project);
    VerifyUndoRedo("legacy Valour upgrade", upgrade, project, before, after);
    Check(project.GameplayOperationStates.Single().ElementCount == 5,
        "legacy Valour redo restores expanded state");
    VerifyProfileStateRoundTrip(project, ProgressionType.ValourPoints, 5);
}

static void VerifyLegacyValourCustom()
{
    ProjectModel project = CreateValourProject(3, 5, 8);
    AddLegacyPartyState(project, ProgressionType.ValourPoints);
    PartyEconomyService service = CreatePartyService();
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.ValourPoints);

    Check(viewModel.SelectedTentPreset == null && !viewModel.CanApply,
        "legacy Valour custom current Tent values block implicit Apply");
    Check(viewModel.PreviewText.Contains("3 / 5 / 8", StringComparison.Ordinal),
        "legacy Valour reports actual custom Tent values");
    Check(viewModel.ValidationMessage.Contains("custom", StringComparison.OrdinalIgnoreCase),
        "legacy Valour reports custom state clearly");
    CheckPartyValues(project, ProgressionType.ValourPoints, 3, 5, 8);

    viewModel.SelectedTentPreset = "Increased";
    Check(viewModel.CanApply, "legacy Valour explicit preset enables Apply");
    string customJson = Json(project);
    ProjectOperationResult upgrade = ExecuteParty(
        project,
        service,
        ProgressionType.ValourPoints,
        viewModel.CreateSettings());
    Check(upgrade.Succeeded, "legacy Valour custom upgrade applies explicitly");
    GameplayOperationStateModel state = project.GameplayOperationStates.Single();
    CheckPartyBaseline(state, 3, 5, 8);
    CheckPartyValues(project, ProgressionType.ValourPoints, 2, 3, 4);
    string increasedJson = Json(project);
    VerifyUndoRedo(
        "legacy Valour custom upgrade",
        upgrade,
        project,
        customJson,
        increasedJson);

    viewModel.RefreshFromProject();
    viewModel.ResetToGameDefaults();
    Check(viewModel.CanApply, "legacy Valour baseline reset explicitly confirmed");
    ProjectOperationResult restore = ExecuteParty(
        project,
        service,
        ProgressionType.ValourPoints,
        viewModel.CreateSettings());
    Check(restore.Succeeded, "legacy Valour baseline restoration applies");
    CheckPartyValues(project, ProgressionType.ValourPoints, 3, 5, 8);
}

static void VerifyLegacyCarrying()
{
    VerifyLegacyCarryingSupported();
    VerifyLegacyCarryingCustom();
    Console.WriteLine("PASS legacy Carrying compatibility and upgrade");
}

static void VerifyLegacyCarryingSupported()
{
    ProjectModel project = CreateCarryingProject(20, 40, 60, 20, 30);
    AddLegacyPartyState(project, ProgressionType.CarryingCapacity);
    PartyEconomyService service = CreatePartyService();
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.CarryingCapacity);

    Check(viewModel.SelectedHitchingPostPreset == "Increased",
        "legacy Carrying detects supported current Hitching Post preset");
    Check(viewModel.CanApply, "legacy Carrying supported preset can upgrade");
    ProjectOperationResult upgrade = ExecuteParty(
        project,
        service,
        ProgressionType.CarryingCapacity,
        viewModel.CreateSettings());
    Check(upgrade.Succeeded, "legacy Carrying supported upgrade applies");
    GameplayOperationStateModel expanded = project.GameplayOperationStates.Single();
    Check(expanded.ElementCount == 7 && expanded.BaselineArray.Count == 7,
        "legacy Carrying expands to seven targets");
    CheckCarryingBaseline(expanded, 20, 40, 60, 20, 30);
    VerifyProfileStateRoundTrip(project, ProgressionType.CarryingCapacity, 7);
}

static void VerifyLegacyCarryingCustom()
{
    ProjectModel project = CreateCarryingProject(11, 22, 33, 7, 14);
    AddLegacyPartyState(project, ProgressionType.CarryingCapacity);
    PartyEconomyService service = CreatePartyService();
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.CarryingCapacity);

    Check(viewModel.SelectedHitchingPostPreset == null && !viewModel.CanApply,
        "legacy Carrying custom current values block implicit Apply");
    Check(viewModel.PreviewText.Contains("11 / 22 / 33", StringComparison.Ordinal) &&
          viewModel.PreviewText.Contains("0 / 7 / 14", StringComparison.Ordinal),
        "legacy Carrying reports actual custom values");
    Check(viewModel.ValidationMessage.Contains("custom", StringComparison.OrdinalIgnoreCase),
        "legacy Carrying reports custom state clearly");
    CheckCarryingValues(project, 11, 22, 33, 7, 14);

    viewModel.SelectedHitchingPostPreset = "Increased";
    Check(viewModel.CanApply, "legacy Carrying explicit preset enables Apply");
    string customJson = Json(project);
    ProjectOperationResult upgrade = ExecuteParty(
        project,
        service,
        ProgressionType.CarryingCapacity,
        viewModel.CreateSettings());
    Check(upgrade.Succeeded, "legacy Carrying custom upgrade applies explicitly");
    GameplayOperationStateModel state = project.GameplayOperationStates.Single();
    CheckCarryingBaseline(state, 11, 22, 33, 7, 14);
    CheckCarryingValues(project, 20, 40, 60, 20, 30);
    string increasedJson = Json(project);
    VerifyUndoRedo(
        "legacy Carrying custom upgrade",
        upgrade,
        project,
        customJson,
        increasedJson);

    viewModel.RefreshFromProject();
    viewModel.ResetToGameDefaults();
    Check(viewModel.CanApply, "legacy Carrying baseline reset explicitly confirmed");
    ProjectOperationResult restore = ExecuteParty(
        project,
        service,
        ProgressionType.CarryingCapacity,
        viewModel.CreateSettings());
    Check(restore.Succeeded, "legacy Carrying baseline restoration applies");
    CheckCarryingValues(project, 11, 22, 33, 7, 14);
}

static void VerifySnapshotPathCompatibility()
{
    ProjectModel project = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "PathFixture",
                ["tool"] = new JObject { ["width"] = 4 },
                ["icon"] = new JObject { ["width"] = 32 },
                ["height"] = 9
            }));
    ModificationSnapshotMatcher matcher = new();

    ModificationMatchResultModel pathMatch = matcher.Match(
        project,
        SnapshotProperty("width", "tool.width"));
    Check(pathMatch.MatchedCount == 1 &&
          pathMatch.Items[0].TargetProperty?.EffectivePropertyPath == "tool.width",
        "new snapshot full path selects exact duplicate leaf");

    ModificationMatchResultModel legacyUnique = matcher.Match(
        project,
        SnapshotProperty("height", string.Empty));
    Check(legacyUnique.MatchedCount == 1 &&
          legacyUnique.Items[0].TargetProperty?.EffectivePropertyPath == "height",
        "legacy unique leaf fallback remains compatible");

    ModificationMatchResultModel legacyAmbiguous = matcher.Match(
        project,
        SnapshotProperty("width", string.Empty));
    Check(legacyAmbiguous.Items.Single().Status ==
          ModificationMatchStatus.PropertyAmbiguous,
        "legacy duplicate leaf fails as ambiguous");
    Check(legacyAmbiguous.Items.Single().TargetProperty == null,
        "legacy duplicate leaf never selects an arbitrary property");
    Console.WriteLine("PASS snapshot path compatibility");
}

static void VerifyApplyFeedbackState()
{
    GameplayApplyFeedbackViewModel feedback = new();
    Check(!feedback.IsVisible,
        "Apply feedback begins hidden");
    feedback.ShowApplied("Settings were updated.");
    Check(feedback.IsVisible &&
          feedback.Heading == "Applied successfully" &&
          feedback.Message == "Settings were updated.",
        "Apply feedback exposes success state");
    feedback.ShowAlreadyApplied();
    Check(feedback.IsVisible &&
          feedback.Heading == "Already applied" &&
          feedback.Message.Contains("already matches", StringComparison.Ordinal),
        "Apply feedback exposes no-op state");
    feedback.Clear();
    Check(!feedback.IsVisible &&
          feedback.Heading.Length == 0 &&
          feedback.Message.Length == 0,
        "Apply feedback clears when settings change");

    StartingResourcesDialogViewModel startingResources = new(
        CreateProject(),
        new GameplayOperationStateService(new ProjectMutationService()));
    startingResources.ApplyFeedback.ShowApplied("Starting resources were updated.");
    startingResources.SetInputBindingValid(false);
    Check(!startingResources.ApplyFeedback.IsVisible,
        "Starting Resources clears stale feedback for invalid input");

    PartyEconomyDialogViewModel partyEconomy = new(
        CreateValourProject(1, 2, 3),
        CreatePartyService(),
        ProgressionType.ValourPoints);
    partyEconomy.ApplyFeedback.ShowAlreadyApplied();
    partyEconomy.SetInputBindingValid(false);
    Check(!partyEconomy.ApplyFeedback.IsVisible,
        "Party Economy clears stale feedback for invalid input");
    Console.WriteLine("PASS shared Apply feedback state");
}

static void VerifyCatalogCoverage()
{
    Check((int)ProgressionType.RubySapphireValue == 20 &&
          (int)ProgressionType.TimeBetweenRests == 21 &&
          (int)ProgressionType.ResourceReplenishment == 22,
        "new operation type preserves persisted enum identities");
    ProgressionType[] supported = Enum.GetValues<ProgressionType>()
        .Where(GameplayPresetCatalog.IsSupported)
        .ToArray();
    Check(supported.Length == 15, "catalog contains fifteen shared preset tools");

    foreach (ProgressionType type in supported)
    {
        GameplayPresetDefinition definition = GameplayPresetCatalog.Get(type);
        Check(definition.Targets.Count > 0, $"{type} has targets");
        Check(definition.Presets.Count > 0, $"{type} has presets");
        Check(definition.Presets.Count(preset => preset.Key == "Vanilla") == 1,
            $"{type} has exactly one Vanilla preset");
        Check(definition.Presets.Select(preset => preset.Key).Distinct().Count() ==
              definition.Presets.Count,
            $"{type} preset keys are unique");

        foreach (GameplayTargetDefinition target in definition.Targets)
        {
            Check(!string.IsNullOrWhiteSpace(target.Sheet) &&
                  !string.IsNullOrWhiteSpace(target.Entry) &&
                  !string.IsNullOrWhiteSpace(target.Path),
                $"{type} target identity is complete");
            Check((target.Discriminator == null) == (target.Identity == null),
                $"{type} target type is internally consistent");
        }

        foreach (GameplayPresetOption preset in definition.Presets)
        {
            Check(preset.Values.Count == definition.Targets.Count,
                $"{type}/{preset.Key} resolved value count");
            GameplayPresetService.ValidatePreset(definition, preset);
        }
    }
    Console.WriteLine("PASS complete preset catalog validation coverage");
}

static void VerifyMalformedTargets()
{
    ProjectModel missingTier = CreateProject(
        Sheet(
            "item",
            BonusEntry("CookingPotT2", "props", "bonuses", "bonus", "PerfectRecipe", 15)));
    Check(!ExecutePreset(
            missingTier,
            ProgressionType.DeliciousMealChance,
            "High").Succeeded,
        "missing tier fails safely");

    JObject duplicateT2 = BonusEntry(
        "CookingPotT2", "props", "bonuses", "bonus", "PerfectRecipe", 15);
    ((JArray)duplicateT2.SelectToken("props.bonuses")!).Add(
        new JObject { ["bonus"] = "PerfectRecipe", ["value"] = 16 });
    ProjectModel duplicate = CreateProject(
        Sheet(
            "item",
            duplicateT2,
            BonusEntry("CookingPotT3", "props", "bonuses", "bonus", "PerfectRecipe", 30)));
    Check(!ExecutePreset(
            duplicate,
            ProgressionType.DeliciousMealChance,
            "High").Succeeded,
        "duplicate discriminator fails safely");

    ProjectModel missingDiscriminator = CreateProject(
        Sheet(
            "item",
            BonusEntry("Workshop", "tool", "bonusesIfAssigned", "bonus", "Other", 2),
            BonusEntry("WorkshopT2", "tool", "bonusesIfAssigned", "bonus", "RawMaterialOnRest", 2),
            BonusEntry("WorkshopT3", "tool", "bonusesIfAssigned", "bonus", "RawMaterialOnRest", 2)));
    Check(!ExecutePreset(
            missingDiscriminator,
            ProgressionType.WorkshopMaterials,
            "High").Succeeded,
        "missing discriminator fails safely");

    ProjectModel wrongType = CreateProject(
        Sheet("constant", ScalarEntry("FishingDurationControl", "invalid")));
    Check(!ExecutePreset(
            wrongType,
            ProgressionType.FishingSpeed,
            "Fast").Succeeded,
        "wrong token type fails safely");

    ProjectModel partialState = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("ForgeDurationPerfectHeatMin", 0.25),
            ScalarEntry("ForgeDurationPerfectHeatMax", 0.25)));
    Check(ExecutePreset(
            partialState,
            ProgressionType.ForgingAssistance,
            "Easy").Succeeded,
        "multi-target state fixture applies");
    partialState.GameplayOperationStates.Single().BaselineArray.RemoveAt(1);
    string before = Json(partialState);
    Check(!ExecutePreset(
            partialState,
            ProgressionType.ForgingAssistance,
            "Easier").Succeeded,
        "partial multi-target state fails safely");
    Check(Json(partialState) == before,
        "partial multi-target state failure does not mutate project");
    Console.WriteLine("PASS representative malformed-target coverage");
}

static GameplayPresetService CreatePresetService()
{
    ProjectMutationService mutation = new();
    return new GameplayPresetService(
        mutation,
        new GameplayOperationStateService(mutation));
}

static PartyEconomyService CreatePartyService()
{
    ProjectMutationService mutation = new();
    return new PartyEconomyService(
        mutation,
        new GameplayOperationStateService(mutation));
}

static ProjectOperationResult ApplyPreset(
    ProjectOperationService executor,
    GameplayPresetService service,
    ProjectModel project,
    ProgressionType type,
    string preset,
    bool expectSuccess = true)
{
    ProjectOperationResult result = executor.Execute(
        new GameplayPresetOperation(service, type, preset),
        project);
    Check(result.Succeeded == expectSuccess,
        $"{type}/{preset} expected success={expectSuccess}: {result.Message}");
    return result;
}

static ProjectOperationResult ExecutePreset(
    ProjectModel project,
    ProgressionType type,
    string preset)
{
    GameplayPresetService service = CreatePresetService();
    return new ProjectOperationService().Execute(
        new GameplayPresetOperation(service, type, preset),
        project);
}

static ProjectOperationResult ExecuteParty(
    ProjectModel project,
    PartyEconomyService service,
    ProgressionType type,
    PartyEconomySettings settings) =>
    new ProjectOperationService().Execute(
        new PartyEconomyOperation(service, type, settings),
        project);

static void AddLegacyPartyState(
    ProjectModel project,
    ProgressionType type)
{
    JArray current = PartyEconomyService.CaptureTargets(project, type);
    JArray baseline = new(current.Take(2).Select(record => record!.DeepClone()));
    JObject settings = type == ProgressionType.ValourPoints
        ? new JObject
        {
            ["maximumValour"] = baseline[0]!["value"]!.Value<int>(),
            ["restoredValour"] = baseline[1]!["value"]!.Value<int>()
        }
        : new JObject
        {
            ["saddlebagCapacity"] = baseline[0]!["value"]!.Value<int>(),
            ["ponyStartingCapacity"] = baseline[1]!["value"]!.Value<int>()
        };
    project.GameplayOperationStates.Add(new GameplayOperationStateModel
    {
        OperationType = type,
        TargetSheet = Join(baseline, "sheet", ","),
        TargetEntry = Join(baseline, "entry", ","),
        TargetPath = Join(baseline, "path", "|"),
        BaselineArray = baseline,
        GameplaySettings = settings,
        BaselineFingerprint =
            GameplayOperationFingerprintService.CreateContentFingerprint(baseline),
        ExpectedCurrentFingerprint =
            GameplayOperationFingerprintService.CreateContentFingerprint(baseline),
        ElementCount = 2,
        ElementShapeFingerprint =
            GameplayOperationFingerprintService.CreateShapeFingerprint(baseline),
        IsCompatible = true
    });
}

static string Join(JArray array, string property, string separator) =>
    string.Join(
        separator,
        array.OfType<JObject>().Select(record => record.Value<string>(property)));

static void VerifyUndoRedo(
    string name,
    ProjectOperationResult result,
    ProjectModel project,
    string before,
    string after)
{
    EditHistoryService history = new();
    history.Record(new ProjectOperationHistoryAction(
        name,
        result.MutationResult,
        new ProjectOperationTransactionService()));
    Check(history.Undo(), $"{name} Undo command");
    Check(Json(project) == before, $"{name} exact Undo");
    Check(history.Redo(), $"{name} Redo command");
    Check(Json(project) == after, $"{name} exact Redo");
}

static void CheckPresetState(
    ProjectModel project,
    ProgressionType type,
    string expectedPreset)
{
    GameplayOperationStateModel state = project.GameplayOperationStates.Single(
        candidate => candidate.OperationType == type);
    Check(
        string.Equals(
            state.GameplaySettings?.Value<string>("preset"),
            expectedPreset,
            StringComparison.Ordinal),
        $"{type} reset records the {expectedPreset} operation state");
}

static void VerifyProfileStateRoundTrip(
    ProjectModel project,
    ProgressionType type,
    int expectedCount)
{
    ModProfileSerializationService serializer = new();
    var profile = new ModProfileService().CreateProfile(
        project,
        $"{type} compatibility profile");
    var roundTrip = serializer.Deserialize(serializer.Serialize(profile));
    GameplayOperationStateModel state =
        roundTrip.Snapshot.GameplayOperationStates.Single(candidate =>
            candidate.OperationType == type);
    Check(state.ElementCount == expectedCount &&
          state.BaselineArray.Count == expectedCount,
        $"{type} expanded state profile round trip");
}

static void VerifySnapshotStateRoundTrip(
    ProjectModel project,
    ProgressionType type,
    int expectedCount)
{
    ModificationSnapshotModel snapshot =
        new ModificationSnapshotService().CreateSnapshot(project, "smoke");
    ModificationSnapshotSerializationService serializer = new();
    ModificationSnapshotModel roundTrip =
        serializer.Deserialize(serializer.Serialize(snapshot));
    GameplayOperationStateModel state =
        roundTrip.GameplayOperationStates.Single(candidate =>
            candidate.OperationType == type);
    Check(state.ElementCount == expectedCount &&
          state.BaselineArray.Count == expectedCount,
        $"{type} snapshot state round trip");
}

static void VerifyGameplayStateFileRoundTrip(
    ProjectModel source,
    ProjectModel target,
    ProgressionType type)
{
    string cdbPath = Path.Combine(
        Path.GetTempPath(),
        $"wartales-editor-{Guid.NewGuid():N}.cdb");
    GameplayOperationStatePersistenceService persistence = new();
    try
    {
        persistence.Save(source, cdbPath);
        target.GameplayOperationStates.Clear();
        target.GameplayOperationStateWarnings.Clear();
        target.IsGameplayOperationStateModified = false;
        persistence.LoadIntoProject(target, cdbPath);
        GameplayOperationStateModel state =
            target.GameplayOperationStates.Single(candidate =>
                candidate.OperationType == type);
        Check(state.IsCompatible,
            $"{type} operation state save/reload remains compatible");
    }
    finally
    {
        string sidecar = persistence.GetSidecarPath(cdbPath);
        if (File.Exists(sidecar))
            File.Delete(sidecar);
    }
}

static void CheckMining(
    ProjectModel project,
    double minimum,
    double maximum,
    string label)
{
    CheckNumber(project, "constant", "MiningSpeedCircleMin", "value", minimum);
    CheckNumber(project, "constant", "MiningSpeedCircleMax", "value", maximum);
    Check(minimum < maximum, $"Mining {label} relationship preserved");
}

static void CheckPartyValues(
    ProjectModel project,
    ProgressionType type,
    params int[] expected)
{
    JArray current = PartyEconomyService.CaptureTargets(project, type);
    int[] actual = current.Skip(2)
        .Select(record => record!["value"]!.Value<int>())
        .ToArray();
    Check(actual.SequenceEqual(expected), $"{type} current expanded values");
}

static void CheckPartyBaseline(
    GameplayOperationStateModel state,
    params int[] expected)
{
    int[] actual = state.BaselineArray.Skip(2)
        .Select(record => record!["value"]!.Value<int>())
        .ToArray();
    Check(actual.SequenceEqual(expected), $"{state.OperationType} expanded baseline");
}

static void CheckCarryingValues(
    ProjectModel project,
    int tier1Base,
    int tier2Base,
    int tier3Base,
    int tier2Trait,
    int tier3Trait) =>
    CheckPartyValues(
        project,
        ProgressionType.CarryingCapacity,
        tier1Base,
        tier2Base,
        tier3Base,
        tier2Trait,
        tier3Trait);

static void CheckCarryingBaseline(
    GameplayOperationStateModel state,
    int tier1Base,
    int tier2Base,
    int tier3Base,
    int tier2Trait,
    int tier3Trait) =>
    CheckPartyBaseline(
        state,
        tier1Base,
        tier2Base,
        tier3Base,
        tier2Trait,
        tier3Trait);

static ProjectModel CreateValourProject(
    int tier1,
    int tier2,
    int tier3) =>
    CreateProject(
        Sheet(
            "constant",
            ScalarEntry("ActionPointBaseMax", 14),
            ScalarEntry("ActionPointGainPerSleep", 2)),
        Sheet(
            "item",
            BonusEntry("Tent", "props", "bonuses", "bonus", "ActionPoint", tier1),
            BonusEntry("TentT2", "props", "bonuses", "bonus", "ActionPoint", tier2),
            BonusEntry("TentT3", "props", "bonuses", "bonus", "ActionPoint", tier3)));

static ProjectModel CreateCarryingProject(
    int tier1Base,
    int tier2Base,
    int tier3Base,
    int tier2Trait,
    int tier3Trait) =>
    CreateProject(
        Sheet(
            "item",
            ArrayEntry(
                "AnimAccCarriage",
                "baseBonus",
                new JObject { ["attribute"] = "Transport", ["value"] = 10 }),
            PersonalBonusEntry("PonyAuge", tier1Base, null),
            PersonalBonusEntry("PonyAugeT2", tier2Base, tier2Trait),
            PersonalBonusEntry("PonyAugeT3", tier3Base, tier3Trait)),
        Sheet(
            "unitClass",
            ArrayEntry(
                "Pony",
                "stats",
                new JObject { ["attribute"] = "Transport", ["value"] = 55 })));

static JObject PersonalBonusEntry(
    string id,
    int baseValue,
    int? traitValue)
{
    JArray bonuses = new()
    {
        new JObject
        {
            ["bonus"] = "PonyAugeTransport",
            ["value"] = baseValue
        }
    };
    if (traitValue.HasValue)
        bonuses.Add(new JObject
        {
            ["bonus"] = "PonyAugeTransportTrait",
            ["value"] = traitValue.Value
        });
    return new JObject
    {
        ["id"] = id,
        ["tool"] = new JObject { ["personalBonuses"] = bonuses }
    };
}

static JObject BonusEntry(
    string id,
    string container,
    string arrayName,
    string discriminator,
    string identity,
    int value) =>
    new()
    {
        ["id"] = id,
        [container] = new JObject
        {
            [arrayName] = new JArray
            {
                new JObject
                {
                    [discriminator] = identity,
                    ["value"] = value
                }
            }
        }
    };

static JObject ArrayEntry(
    string id,
    string arrayName,
    params JObject[] values) =>
    new()
    {
        ["id"] = id,
        [arrayName] = new JArray(values)
    };

static JObject ScalarEntry(string id, object value) =>
    new()
    {
        ["id"] = id,
        ["value"] = JToken.FromObject(value)
    };

static ProjectModel CreateResourceProject(
    object slow,
    object normal,
    object fast,
    object extreme,
    object unrelated,
    string? omitted = null)
{
    List<JObject> entries = new();
    foreach ((string id, object value) in new[]
             {
                 ("GatherRefillSlow", slow),
                 ("GatherRefillNormal", normal),
                 ("GatherRefillFast", fast),
                 ("GatherRefillFactorExtreme", extreme),
                 ("UnrelatedFixture", unrelated)
             })
    {
        if (!string.Equals(id, omitted, StringComparison.Ordinal))
            entries.Add(ScalarEntry(id, value));
    }
    return CreateProject(Sheet("constant", entries.ToArray()));
}

static void CheckResourceValues(
    ProjectModel project,
    double slow,
    double normal,
    double fast)
{
    CheckNumber(project, "constant", "GatherRefillSlow", "value", slow);
    CheckNumber(project, "constant", "GatherRefillNormal", "value", normal);
    CheckNumber(project, "constant", "GatherRefillFast", "value", fast);
}

static JObject CampfireEntry(
    string id,
    object? capacity = null,
    bool omitCapacity = false,
    object? toolCapacity = null,
    bool omitToolCapacity = false)
{
    JObject tool = new()
    {
        ["campWidth"] = 4,
        ["width"] = 4,
        ["campHeight"] = 4,
        ["height"] = 4
    };
    if (!omitToolCapacity)
        tool["toolCapacity"] = JToken.FromObject(toolCapacity ?? 4);
    if (!omitCapacity)
        tool["capacity"] = JToken.FromObject(capacity ?? 4);
    return new JObject
    {
        ["id"] = id,
        ["tool"] = tool
    };
}

static void CheckCampfire(
    ProjectModel project,
    string id,
    int dimension,
    int capacity)
{
    foreach (string path in new[]
             {
                 "tool.campWidth", "tool.width",
                 "tool.campHeight", "tool.height"
             })
        CheckNumber(project, "item", id, path, dimension);
    CheckNumber(project, "item", id, "tool.toolCapacity", capacity);
    CheckNumber(project, "item", id, "tool.capacity", capacity);
}

static JObject Sheet(string name, params JObject[] entries) =>
    new()
    {
        ["name"] = name,
        ["lines"] = new JArray(entries)
    };

static ProjectModel CreateProject(params JObject[] sheets)
{
    JObject root = new() { ["sheets"] = new JArray(sheets) };
    ProjectModel project = new()
    {
        FileName = "compatibility-fixture.cdb",
        OriginalJson = root.ToString(),
        RootDocument = root
    };
    ProjectModelFactory factory = new();
    foreach (JObject sheet in sheets)
        project.Sheets.Add(factory.CreateSheetModel(sheet));
    return project;
}

static EntryModel Entry(
    ProjectModel project,
    string sheet,
    string id) =>
    project.Sheets.Single(candidate => candidate.Name == sheet)
        .Entries.Single(candidate => candidate.Id == id);

static void CheckNumber(
    ProjectModel project,
    string sheet,
    string id,
    string path,
    double expected)
{
    JToken value = Entry(project, sheet, id).SourceEntry!.SelectToken(path)!;
    CheckTokenNumber(value, expected, $"{sheet}/{id}/{path}");
}

static void CheckTokenNumber(JToken actual, double expected, string name) =>
    Check(
        Math.Abs(actual.Value<double>() - expected) < 0.000001,
        $"{name}: expected {expected}, actual {actual}");

static ModificationSnapshotModel SnapshotProperty(
    string name,
    string path) =>
    new()
    {
        Categories =
        {
            new ModificationSnapshotCategoryModel
            {
                Name = "item",
                Settings =
                {
                    new ModificationSnapshotSettingModel
                    {
                        Id = "PathFixture",
                        Name = "PathFixture",
                        DisplayName = "PathFixture",
                        Properties =
                        {
                            new ModificationSnapshotPropertyModel
                            {
                                Name = name,
                                PropertyPath = path,
                                OriginalValue = 0,
                                CurrentValue = 1
                            }
                        }
                    }
                }
            }
        }
    };

static string Json(ProjectModel project) =>
    project.RootDocument.ToString(Newtonsoft.Json.Formatting.None);

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException($"FAIL: {message}");
}
