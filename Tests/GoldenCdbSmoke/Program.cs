using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.Services.Validation;
using WartalesEditor.ViewModels;
using WartalesEditor.Views;

int checks = 0;
string root = Path.Combine(
    Path.GetTempPath(),
    "WartalesEditorGoldenCdbTests",
    Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);

try
{
    JsonDataService json = new();
    CdbGenerationIdentityService identities = new();
    string storage = Path.Combine(root, "Golden CDB");
    GoldenCdbService service = new(json, storage);
    GoldenCdbComparisonService comparer = new();

    Check(service.GetCanonicalPath() == Path.Combine(storage, "data.cdb"),
        "test path override");
    string defaultPath = new GoldenCdbService(json).GetCanonicalPath();
    Check(defaultPath.EndsWith(
        Path.Combine("Wartales Editor", "Golden CDB", "data.cdb"),
        StringComparison.OrdinalIgnoreCase), "default Documents path");
    Check(service.GetState().Availability == GoldenCdbAvailability.NotSet,
        "initial NotSet state");
    Check(service.Remove().IsNotSet, "missing remove is idempotent");
    Check(service.IsCanonicalPath(Path.Combine(storage, ".", "data.cdb")) &&
          !service.IsCanonicalPath(Path.Combine(storage, "other.cdb")),
        "canonical path comparison normalizes paths");

    string sourceA = WriteCdb("source-a.cdb", BaseJson(1));
    byte[] sourceABytes = File.ReadAllBytes(sourceA);
    File.WriteAllText(sourceA + ".wtstate", "not valid state");
    service.ValidateSourceFile(sourceA);
    GoldenCdbState first = service.SetFromFile(sourceA);
    Check(first.IsAvailable, "initial set available");
    Check(File.ReadAllBytes(service.GetCanonicalPath()).SequenceEqual(sourceABytes),
        "exact source bytes preserved");
    Check(first.Identity == identities.Calculate(sourceABytes),
        "canonical deterministic identity");
    Check(Directory.GetFiles(storage).Select(Path.GetFileName)
            .SequenceEqual(new[] { "data.cdb" }),
        "no metadata archive or sidecar copied");
    GoldenCdbReference firstReference = service.LoadReference();
    Check(firstReference.Project.GameplayOperationStates.Count == 0 &&
          firstReference.Project.HistoricalGameplayOperationStates.Count == 0,
        "reference load ignores adjacent source sidecar");
    Check(firstReference.Project.SourceCdbGenerationIdentity == null &&
          firstReference.Project.SourceProvenanceStatus == SourceProvenanceStatus.Unknown &&
          firstReference.Project.CurrentCdbContentIdentity == first.Identity,
        "reference identity boundaries");
    Check(ReferenceEquals(firstReference, service.LoadReference()),
        "unchanged hash reuses parsed cache");

    File.Delete(sourceA);
    Check(service.LoadReference().Identity == first.Identity,
        "source may be deleted after designation");
    GoldenCdbService coldSourceIndependentService = new(json, storage);
    GoldenCdbReference coldSourceIndependentReference =
        coldSourceIndependentService.LoadReference();
    GoldenCdbComparisonResult coldSourceIndependentComparison =
        new GoldenCdbComparisonService().Compare(
            firstReference.Project,
            coldSourceIndependentReference);
    Check(coldSourceIndependentReference.Identity == first.Identity &&
          coldSourceIndependentReference.Project.FileName ==
              coldSourceIndependentService.GetCanonicalPath() &&
          coldSourceIndependentComparison.IsExactMatch,
        "fresh service reloads and compares canonical Golden after source deletion");

    string invalid = Path.Combine(root, "invalid.cdb");
    File.WriteAllText(invalid, "not-json");
    CheckThrows(() => service.ValidateSourceFile(invalid),
        "invalid JSON rejected");
    string empty = Path.Combine(root, "empty.cdb");
    File.WriteAllBytes(empty, Array.Empty<byte>());
    CheckThrows(() => service.ValidateSourceFile(empty),
        "empty CDB rejected");
    string noSheets = WriteCdb("no-sheets.cdb", "{\"sheets\":[]}");
    CheckThrows(() => service.ValidateSourceFile(noSheets),
        "zero usable sheets rejected");
    Check(service.LoadReference().Identity == first.Identity,
        "invalid candidates preserve Golden");

    string sourceB = WriteCdb("source-b.cdb", BaseJson(2));
    GoldenCdbState second = service.SetFromFile(sourceB);
    Check(second.IsAvailable && second.Identity != first.Identity,
        "successful replacement");
    Check(File.ReadAllBytes(service.GetCanonicalPath())
            .SequenceEqual(File.ReadAllBytes(sourceB)),
        "replacement exact bytes");
    Check(NoTransientArtifacts(), "replacement temporary cleanup");
    string secondIdentity = second.Identity;
    Check(service.SetFromFile(service.GetCanonicalPath()).Identity == secondIdentity &&
          NoTransientArtifacts(), "canonical self replacement no-op");
    Check(service.LoadReference().Project.FileName == service.GetCanonicalPath(),
        "self replacement cache remains canonical-source independent");

    string failingInitialStorage = Path.Combine(root, "Failing Initial");
    GoldenCdbService failingInitial = new(
        json,
        failingInitialStorage,
        new FaultHooks { ThrowAfterPromotion = true });
    CheckThrows(
        () => failingInitial.SetFromFile(sourceB),
        "failed initial publication surfaced");
    Check(!File.Exists(failingInitial.GetCanonicalPath()) &&
          !File.Exists(failingInitial.GetCanonicalPath() + ".candidate.tmp") &&
          !File.Exists(failingInitial.GetCanonicalPath() + ".rollback.tmp"),
        "failed initial publication removes incomplete canonical and transients");

    string sourceC = WriteCdb("source-c.cdb", BaseJson(3));
    FaultHooks prePromotion = new() { ThrowAfterCandidate = true };
    GoldenCdbService preFailure = new(json, storage, prePromotion);
    CheckThrows(() => preFailure.SetFromFile(sourceC),
        "pre-promotion failure surfaced");
    Check(identities.Calculate(service.GetCanonicalPath()) == secondIdentity,
        "pre-promotion failure preserves previous Golden");
    Check(NoTransientArtifacts(), "pre-promotion cleanup");

    FaultHooks postPromotion = new() { ThrowAfterPromotion = true };
    GoldenCdbService postFailure = new(json, storage, postPromotion);
    CheckThrows(() => postFailure.SetFromFile(sourceC),
        "post-promotion failure surfaced");
    Check(identities.Calculate(service.GetCanonicalPath()) == secondIdentity,
        "post-promotion failure restores exact previous Golden");
    Check(postFailure.GetState().IsAvailable,
        "restored Golden remains usable");
    Check(NoTransientArtifacts(), "post-promotion cleanup");

    FaultHooks recoveryFailure = new()
    {
        ThrowAfterPromotion = true,
        CorruptRollback = true
    };
    GoldenCdbService brokenRecovery = new(json, storage, recoveryFailure);
    CheckThrowsExact<GoldenCdbPublicationException>(
        () => brokenRecovery.SetFromFile(sourceC),
        "rollback verification failure surfaced");
    Check(brokenRecovery.GetState().Availability == GoldenCdbAvailability.Invalid,
        "unrecoverable replacement marks Golden invalid");
    Check(NoTransientArtifacts(), "failed recovery cleanup");

    string candidateCleanupStorage = Path.Combine(root, "Candidate Cleanup");
    FaultHooks candidateCleanupHooks = new()
    {
        FailCandidateCleanupOnce = true
    };
    GoldenCdbService candidateCleanupService = new(
        json,
        candidateCleanupStorage,
        candidateCleanupHooks);
    GoldenCdbState candidateCleanupState =
        candidateCleanupService.SetFromFile(sourceB);
    Check(candidateCleanupState.IsAvailable &&
          candidateCleanupState.HasCleanupWarning &&
          File.Exists(candidateCleanupService.GetCanonicalPath()) &&
          File.Exists(candidateCleanupService.GetCanonicalPath() + ".candidate.tmp") &&
          candidateCleanupService.LoadReference().Identity ==
              identities.Calculate(sourceB),
        "candidate cleanup failure preserves coherent Golden and reports warning");
    GoldenCdbState candidateCleanupRetry =
        candidateCleanupService.SetFromFile(sourceC);
    Check(candidateCleanupRetry.IsAvailable &&
          !candidateCleanupRetry.HasCleanupWarning &&
          OnlyCanonicalFile(candidateCleanupService),
        "next transaction removes stale candidate and completes cleanly");

    string rollbackCleanupStorage = Path.Combine(root, "Rollback Cleanup");
    GoldenCdbService rollbackSeed = new(json, rollbackCleanupStorage);
    rollbackSeed.SetFromFile(sourceB);
    FaultHooks rollbackCleanupHooks = new()
    {
        FailRollbackCleanupOnce = true
    };
    GoldenCdbService rollbackCleanupService = new(
        json,
        rollbackCleanupStorage,
        rollbackCleanupHooks);
    GoldenCdbState rollbackCleanupState =
        rollbackCleanupService.SetFromFile(sourceC);
    Check(rollbackCleanupState.IsAvailable &&
          rollbackCleanupState.HasCleanupWarning &&
          File.Exists(rollbackCleanupService.GetCanonicalPath() + ".rollback.tmp") &&
          rollbackCleanupService.LoadReference().Identity ==
              identities.Calculate(sourceC),
        "rollback cleanup failure preserves promoted Golden and reports warning");
    Check(rollbackCleanupService.GetState().HasCleanupWarning,
        "cleanup warning persists while recognized residue remains");
    GoldenCdbState rollbackCleanupRetry =
        rollbackCleanupService.SetFromFile(sourceB);
    Check(rollbackCleanupRetry.IsAvailable &&
          !rollbackCleanupRetry.HasCleanupWarning &&
          OnlyCanonicalFile(rollbackCleanupService),
        "next replacement owns no stale rollback and leaves only canonical data.cdb");

    service.SetFromFile(sourceB);
    GoldenCdbReference beforeExternal = service.LoadReference();
    File.Copy(sourceC, service.GetCanonicalPath(), overwrite: true);
    GoldenCdbReference afterExternal = service.LoadReference();
    Check(afterExternal.Identity != beforeExternal.Identity &&
          afterExternal.Identity == identities.Calculate(sourceC),
        "external canonical change detected on next use");

    service.SetFromFile(sourceB);
    ProjectModel persisted = json.LoadReferenceProject(sourceB);
    service.ValidateProjectSource(persisted);
    Check(service.SetFromProject(persisted).IsAvailable,
        "set valid current project");
    persisted.IsGameplayOperationStateModified = true;
    persisted.IsModified = true;
    service.ValidateProjectSource(persisted);
    Check(true, "gameplay-state-only modification does not block exact persisted CDB designation");
    persisted.IsGameplayOperationStateModified = false;
    persisted.IsModified = true;
    service.ValidateProjectSource(persisted);
    Check(true, "project-level modified flag without a CDB difference does not block designation");
    PropertyModel persistedValue = persisted.Sheets[0].Entries[0].Properties
        .Single(property => property.Name == "value");
    persistedValue.Value = "10";
    CheckThrows(() => service.ValidateProjectSource(persisted),
        "real unsaved current CDB rejected");
    persistedValue.Value = "2";
    persisted.IsModified = false;
    File.WriteAllText(sourceB, BaseJson(9));
    CheckThrows(() => service.ValidateProjectSource(persisted),
        "current disk identity mismatch rejected");
    ProjectModel noPath = json.LoadReferenceProject(sourceC);
    noPath.FileName = string.Empty;
    CheckThrows(() => service.ValidateProjectSource(noPath),
        "missing durable current path rejected");

    string structuralFile = WriteCdb(
        "structural-removal.cdb",
        RandomTraitJson());
    ProjectModel structuralProject = json.LoadReferenceProject(structuralFile);
    ProjectMutationService structuralMutation = new();
    GameplayOperationStateService structuralState = new(structuralMutation);
    RandomTraitExclusionsService structuralExclusions = new(
        structuralMutation,
        structuralState);
    ProjectOperationService structuralOperations = new();
    Check(structuralOperations.Execute(
            new RandomTraitExclusionsOperation(
                structuralExclusions,
                new[] { "PositiveTrue", "NegativeAbsent" }),
            structuralProject).Succeeded,
        "production exclusions setup adds persisted structural target");
    json.SaveProject(structuralProject, structuralFile);
    Check(structuralOperations.Execute(
            new RandomTraitExclusionsOperation(
                structuralExclusions,
                new[] { "PositiveTrue", "NegativeAbsent", "PositiveAbsent" }),
            structuralProject).Succeeded,
        "production exclusions operation removes property structurally");
    Check(structuralProject.IsGameplayOperationStateModified &&
          structuralProject.Sheets.Single().Entries
              .Single(entry => entry.Id == "PositiveAbsent")
              .SourceEntry!.Property("done") == null &&
          !JToken.DeepEquals(
              structuralProject.RootDocument,
              json.LoadReferenceProject(structuralFile).RootDocument),
        "structural removal is an effective unsaved CDB change with gameplay state");
    string structuralStorage = Path.Combine(root, "Structural Golden");
    GoldenCdbService structuralGolden = new(json, structuralStorage);
    CheckThrowsMessage(
        () => structuralGolden.SetFromProject(structuralProject),
        "Save the current project",
        "Set Current rejects production structural removal plus gameplay state");
    Check(!File.Exists(structuralGolden.GetCanonicalPath()),
        "rejected structural removal does not designate persisted disk bytes");

    string stateOnlyFile = WriteCdb(
        "state-only.cdb",
        RandomTraitJson());
    ProjectModel stateOnlyProject = json.LoadReferenceProject(stateOnlyFile);
    ProjectMutationService stateOnlyMutation = new();
    GameplayOperationStateService stateOnlyState = new(stateOnlyMutation);
    RandomTraitExclusionsService stateOnlyExclusions = new(
        stateOnlyMutation,
        stateOnlyState);
    string[] currentAllowed = stateOnlyExclusions.Discover(stateOnlyProject)
        .Where(candidate => candidate.IsAllowed)
        .Select(candidate => candidate.Id)
        .ToArray();
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                stateOnlyExclusions,
                currentAllowed),
            stateOnlyProject).Succeeded &&
          stateOnlyProject.IsGameplayOperationStateModified &&
          JToken.DeepEquals(
              stateOnlyProject.RootDocument,
              json.LoadReferenceProject(stateOnlyFile).RootDocument),
        "production gameplay-state-only operation leaves CDB content unchanged");
    GoldenCdbService stateOnlyGolden = new(
        json,
        Path.Combine(root, "State Only Golden"));
    GoldenCdbState stateOnlyDesignation =
        stateOnlyGolden.SetFromProject(stateOnlyProject);
    Check(stateOnlyDesignation.IsAvailable &&
          File.ReadAllBytes(stateOnlyGolden.GetCanonicalPath())
              .SequenceEqual(File.ReadAllBytes(stateOnlyFile)),
        "gameplay-state-only designation remains allowed and exact-byte based");

    service.SetFromFile(sourceC);
    ProjectModel detached = service.LoadDetachedProject();
    File.WriteAllText(service.GetCanonicalPath() + ".wtstate", "invalid sidecar");
    ProjectModel detachedWithSidecar = service.LoadDetachedProject();
    Check(detachedWithSidecar.GameplayOperationStates.Count == 0 &&
          detachedWithSidecar.CurrentCdbContentIdentity == service.GetState().Identity &&
          detachedWithSidecar.SourceProvenanceStatus == SourceProvenanceStatus.Unknown,
        "detached Golden load is sidecar-free and identity-correct");
    Check(!ReferenceEquals(service.LoadReference().Project, detached),
        "active-load candidate is detached from cached Golden model");

    string exactCurrentFile = WriteCdb("exact-current.cdb", BaseJson(3));
    ProjectModel exactCurrent = json.LoadReferenceProject(exactCurrentFile);
    GoldenCdbComparisonResult exact = comparer.Compare(
        exactCurrent,
        service.LoadReference());
    Check(exact.IsExactMatch && exact.DifferenceCount == 0,
        "exact bytes exact all-clear");
    exactCurrent.IsModified = true;
    GoldenCdbComparisonResult noShortcut = comparer.Compare(
        exactCurrent,
        service.LoadReference());
    Check(!noShortcut.IsExactMatch && noShortcut.DifferenceCount == 0,
        "unsaved state prevents exact hash shortcut");

    string formatted = WriteCdb(
        "formatted.cdb",
        JObject.Parse(BaseJson(3)).ToString());
    ProjectModel formattedProject = json.LoadReferenceProject(formatted);
    GoldenCdbComparisonResult modeled = comparer.Compare(
        formattedProject,
        service.LoadReference());
    Check(!modeled.IsExactMatch && modeled.DifferenceCount == 0,
        "byte-different modeled-identical all-clear");

    CheckComparison(
        BaseJson(4),
        item => item.Category == GoldenCdbComparisonCategory.ChangedValue &&
                item.Property == "value",
        "scalar change");
    service.SetFromFile(WriteCdb("extra-golden.cdb", JsonWithProperty(true)));
    CheckComparison(
        JsonWithProperty(includeExtra: false),
        item => item.Category == GoldenCdbComparisonCategory.MissingFromCurrent &&
                item.Property == "extra",
        "missing property");
    service.SetFromFile(WriteCdb("no-extra-golden.cdb", JsonWithProperty(false)));
    CheckComparison(
        JsonWithProperty(includeExtra: true),
        item => item.Category == GoldenCdbComparisonCategory.NewInCurrent &&
                item.Property == "extra",
        "new property");
    service.SetFromFile(sourceC);
    CheckComparison(
        JsonWithEntries(includeA: false, includeB: false),
        item => item.Category == GoldenCdbComparisonCategory.MissingFromCurrent &&
                item.Scope == GoldenCdbComparisonScope.Entry,
        "missing entry aggregation");
    service.SetFromFile(WriteCdb("only-a-golden.cdb", JsonWithEntries(true, false)));
    CheckComparison(
        JsonWithEntries(includeA: true, includeB: true),
        item => item.Category == GoldenCdbComparisonCategory.NewInCurrent &&
                item.Scope == GoldenCdbComparisonScope.Entry,
        "new entry aggregation");
    service.SetFromFile(sourceC);
    CheckComparison(
        "{\"sheets\":[{\"name\":\"other\",\"lines\":[{\"id\":\"A\",\"value\":3}]}]}",
        item => item.Scope == GoldenCdbComparisonScope.Sheet &&
                item.Category == GoldenCdbComparisonCategory.MissingFromCurrent,
        "missing sheet aggregation");
    CheckComparison(
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"A\",\"value\":3}]},{\"name\":\"newSheet\",\"lines\":[{\"id\":\"N\",\"value\":1}]}]}",
        item => item.Scope == GoldenCdbComparisonScope.Sheet &&
                item.Category == GoldenCdbComparisonCategory.NewInCurrent,
        "new sheet aggregation");
    CheckComparison(
        BaseJson("3"),
        item => item.Category == GoldenCdbComparisonCategory.TypeChanged,
        "type change");
    CheckComparison(
        JsonWithArray("[{\"x\":2}]") ,
        item => item.Category == GoldenCdbComparisonCategory.ChangedValue &&
                item.Property == "arr",
        "array content change");
    CheckComparison(
        JsonWithArray("[{\"x\":2,\"y\":1}]") ,
        item => item.Category == GoldenCdbComparisonCategory.StructureChanged,
        "array shape change");

    GoldenCdbComparisonResult duplicateCurrentSheet = CheckComparison(
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"A\",\"value\":3}]},{\"name\":\"constant\",\"lines\":[{\"id\":\"B\",\"value\":1}]}]}",
        item => item.Category == GoldenCdbComparisonCategory.AmbiguousIdentity &&
                item.Scope == GoldenCdbComparisonScope.CoverageSummary,
        "duplicate sheet ambiguity");
    Check(duplicateCurrentSheet.DifferenceCount == 0 &&
          duplicateCurrentSheet.CoverageIssueCount == 1,
        "current duplicate sheet produces coverage only");

    string duplicateGoldenSheetFile = WriteCdb(
        "duplicate-golden-sheet.cdb",
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"A\",\"value\":3}]},{\"name\":\"constant\",\"lines\":[{\"id\":\"B\",\"value\":1}]}]}");
    service.SetFromFile(duplicateGoldenSheetFile);
    GoldenCdbComparisonResult duplicateGoldenSheet = CompareJson(BaseJson(3));
    Check(duplicateGoldenSheet.DifferenceCount == 0 &&
          duplicateGoldenSheet.CoverageIssues.Count(item =>
              item.Category == GoldenCdbComparisonCategory.AmbiguousIdentity &&
              item.Scope == GoldenCdbComparisonScope.CoverageSummary) == 1,
        "Golden duplicate sheet produces one coverage result and no differences");
    service.SetFromFile(sourceC);

    GoldenCdbComparisonResult duplicateCurrentEntry = CheckComparison(
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"A\",\"value\":3},{\"id\":\"A\",\"value\":4}]}]}",
        item => item.Category == GoldenCdbComparisonCategory.AmbiguousIdentity &&
                item.Scope == GoldenCdbComparisonScope.Entry,
        "duplicate explicit entry ID");
    Check(duplicateCurrentEntry.DifferenceCount == 0 &&
          duplicateCurrentEntry.CoverageIssueCount == 1,
        "current duplicate entry produces coverage only");

    string duplicateGoldenEntryFile = WriteCdb(
        "duplicate-golden-entry.cdb",
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"A\",\"value\":3},{\"id\":\"A\",\"value\":4}]}]}");
    service.SetFromFile(duplicateGoldenEntryFile);
    GoldenCdbComparisonResult duplicateGoldenEntry = CompareJson(BaseJson(3));
    Check(duplicateGoldenEntry.DifferenceCount == 0 &&
          duplicateGoldenEntry.CoverageIssues.Count(item =>
              item.Category == GoldenCdbComparisonCategory.AmbiguousIdentity &&
              item.Scope == GoldenCdbComparisonScope.Entry) == 1,
        "Golden duplicate entry produces coverage only and no descendants");
    service.SetFromFile(sourceC);

    GoldenCdbComparisonResult idlessCoverage = CheckComparison(
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"value\":3},{\"value\":4},{\"id\":\"A\",\"arr\":[{\"x\":1}],\"props\":{\"nested\":5},\"value\":3}]}]}",
        item => item.Category == GoldenCdbComparisonCategory.UnsupportedIdentity &&
                item.Details.Contains("2 record", StringComparison.Ordinal),
        "ID-less coverage aggregated");
    Check(idlessCoverage.DifferenceCount == 0 &&
          idlessCoverage.CoverageIssueCount == 1,
        "ID-less records produce unsupported coverage only");
    GoldenCdbComparisonResult idlessOnly = CompareJson(
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"value\":3}]}]}");
    Check(idlessOnly.DifferenceCount == 0 &&
          idlessOnly.CoverageIssues.Any(item =>
              item.Category == GoldenCdbComparisonCategory.UnsupportedIdentity),
        "ID-less replacement suppresses false missing stable entry");
    CheckComparison(
        "{\"sheets\":[7,{\"name\":\"constant\",\"lines\":[{\"id\":\"A\",\"arr\":[{\"x\":1}],\"props\":{\"nested\":5},\"value\":3}]}]}",
        item => item.Category == GoldenCdbComparisonCategory.UnsupportedStructure,
        "unsupported raw structure coverage");

    ProjectModel duplicateProperty = json.LoadReferenceProject(sourceC);
    EntryModel entry = duplicateProperty.Sheets.Single().Entries.Single();
    PropertyModel original = entry.Properties.Single(p => p.EffectivePropertyPath == "value");
    entry.Properties.Add(new PropertyModel
    {
        Name = original.Name,
        PropertyPath = original.PropertyPath,
        SourceProperty = original.SourceProperty
    });
    duplicateProperty.IsModified = true;
    GoldenCdbComparisonResult duplicatePropertyResult = comparer.Compare(
        duplicateProperty,
        service.LoadReference());
    Check(duplicatePropertyResult.CoverageIssues.Any(item =>
            item.Category == GoldenCdbComparisonCategory.AmbiguousIdentity &&
            item.Scope == GoldenCdbComparisonScope.Property),
        "duplicate property path ambiguity");
    Check(duplicatePropertyResult.DifferenceCount == 0 &&
          duplicatePropertyResult.CoverageIssueCount == 1,
        "duplicate property path produces coverage only and no false missing property");

    ProjectModel unchanged = json.LoadReferenceProject(sourceC);
    string beforeJson = unchanged.RootDocument.ToString();
    bool beforeModified = unchanged.IsModified;
    int beforeStates = unchanged.GameplayOperationStates.Count;
    GoldenCdbComparisonResult unchangedResult = comparer.Compare(
        unchanged,
        service.LoadReference());
    Check(unchanged.RootDocument.ToString() == beforeJson &&
          unchanged.IsModified == beforeModified &&
          unchanged.GameplayOperationStates.Count == beforeStates,
        "comparison is mutation and gameplay-state free");
    Check(unchangedResult.DifferenceCount == 0 &&
          unchangedResult.CoverageIssueCount == 0,
        "identical modeled values omitted");
    Check(comparer.HasCachedGoldenIndex, "Golden comparison index cached");
    comparer.Invalidate();
    Check(!comparer.HasCachedGoldenIndex, "comparison cache invalidation");

    ProjectModel active = json.LoadReferenceProject(sourceC);
    string activeJson = active.RootDocument.ToString();
    service.Remove();
    Check(service.GetState().IsNotSet &&
          active.RootDocument.ToString() == activeJson &&
          File.Exists(service.GetCanonicalPath() + ".wtstate"),
        "remove leaves active project and adjacent state unchanged");
    Check(!File.Exists(service.GetCanonicalPath()) && NoTransientArtifacts(),
        "remove clears only canonical Golden artifacts");
    File.WriteAllText(service.GetCanonicalPath(), "corrupt");
    Check(service.GetState().Availability == GoldenCdbAvailability.Invalid,
        "corrupt Golden is nonfatal invalid state");
    Check(service.Remove().IsNotSet &&
          !File.Exists(service.GetCanonicalPath()),
        "corrupt Golden remains removable");
    CheckThrows(() => service.LoadReference(),
        "missing Golden reference load fails without application state mutation");

    string windowXaml = File.ReadAllText(
        Path.Combine(RepositoryRoot(), "Views", "GoldenCdbWindow.xaml"));
    string windowCode = File.ReadAllText(
        Path.Combine(RepositoryRoot(), "Views", "GoldenCdbWindow.xaml.cs"));
    string mainXaml = File.ReadAllText(
        Path.Combine(RepositoryRoot(), "MainWindow.xaml"));
    string mainVm = File.ReadAllText(
        Path.Combine(RepositoryRoot(), "ViewModels", "MainViewModel.cs"));
    Check(mainXaml.Contains("_Golden CDB...", StringComparison.Ordinal) &&
          windowXaml.Contains("Compare Current to Golden", StringComparison.Ordinal),
        "Tools command and management actions present");
    Check(windowXaml.Contains("WindowStartupLocation=\"CenterOwner\"", StringComparison.Ordinal) &&
          windowXaml.Contains("ShowInTaskbar=\"True\"", StringComparison.Ordinal),
        "modeless utility window placement configuration");
    Check(mainVm.Contains("goldenCdbWindow != null", StringComparison.Ordinal) &&
          mainVm.Contains("OnGoldenCdbWindowClosed", StringComparison.Ordinal),
        "single-window tracking and close lifecycle");
    Check(mainVm.Contains("PromptGoldenSaveChoice", StringComparison.Ordinal) &&
          mainVm.Contains("ReconcileAfterCanonicalWrite", StringComparison.Ordinal),
        "save protection and guaranteed reconciliation wired");
    Check(windowXaml.Contains(
              "Import Current Wartales CDB as Golden",
              StringComparison.Ordinal) &&
          windowCode.Contains(
              "ImportCurrentWartalesRequested",
              StringComparison.Ordinal) &&
          mainVm.Contains(
              "ImportCurrentWartalesAsGoldenAsync",
              StringComparison.Ordinal),
        "Golden window exposes shared Import From Wartales designation action");
    Check(!windowXaml.Contains(">Identity<", StringComparison.Ordinal) &&
          !windowXaml.Contains("ShortIdentity", StringComparison.Ordinal) &&
          !windowXaml.Contains("FullIdentity", StringComparison.Ordinal) &&
          windowXaml.Contains("StatusMessage", StringComparison.Ordinal),
        "Golden window hides identity while retaining player status and cleanup warnings");
    Check(windowXaml.Contains("Load Golden CDB", StringComparison.Ordinal) &&
          windowXaml.Contains("SetCurrentButton_Click", StringComparison.Ordinal) &&
          windowXaml.Contains("SelectButton_Click", StringComparison.Ordinal) &&
          windowXaml.Contains("CompareButton_Click", StringComparison.Ordinal) &&
          windowXaml.Contains("RemoveButton_Click", StringComparison.Ordinal),
        "existing Golden actions remain available with import convenience action");
    Check(mainVm.Contains(
              "goldenCdbWindow.ImportCurrentWartalesRequested -=",
              StringComparison.Ordinal),
        "Golden import event follows single-window subscription cleanup");

    GoldenQuickBmsFixture importSuccessFixture = new(
        json,
        Path.Combine(root, "Golden Import Success"),
        BaseJson(73));
    GoldenCdbService importSuccessGolden = new(
        json,
        Path.Combine(root, "Golden Import Success Storage"));
    TestMessages importSuccessMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true);
    MainViewModel importSuccessMain = CreateMain(
        json,
        new TestFileDialogs(),
        importSuccessMessages,
        importSuccessGolden,
        new GoldenCdbComparisonService(),
        quickBmsImportOptions: importSuccessFixture.Options);
    importSuccessMain.UseQuickBmsImportServiceForTesting(
        importSuccessFixture.Service);
    Check(await importSuccessMain.ImportCurrentWartalesAsGoldenAsync() &&
          importSuccessFixture.Runner.Requests.Count == 1 &&
          importSuccessMain.CurrentFile == importSuccessFixture.PromotedCdbPath &&
          importSuccessMain.Project!.SourceProvenanceStatus ==
              SourceProvenanceStatus.Verified &&
          importSuccessMain.Project.SourceCdbGenerationIdentity ==
              importSuccessMain.Project.CurrentCdbContentIdentity &&
          importSuccessMessages.ConfirmationCount == 1,
        "Golden import reuses production QuickBMS acquisition and publication with provenance");
    Check(File.ReadAllBytes(importSuccessGolden.GetCanonicalPath())
              .SequenceEqual(File.ReadAllBytes(importSuccessFixture.PromotedCdbPath)) &&
          importSuccessGolden.GetState().Identity ==
              identities.Calculate(importSuccessFixture.PromotedCdbPath),
        "successful current Wartales import designates exact durable bytes as Golden");
    Check(importSuccessFixture.Runner.Requests.Single().Arguments.Count == 3 &&
          importSuccessFixture.Runner.Requests.Single().Arguments.All(argument =>
              !string.Equals(argument, "-w", StringComparison.OrdinalIgnoreCase) &&
              !string.Equals(argument, "-r", StringComparison.OrdinalIgnoreCase)),
        "Golden convenience action invokes no QuickBMS write-back or deploy flags");

    GoldenQuickBmsFixture declineFixture = new(
        json,
        Path.Combine(root, "Golden Import Decline"),
        BaseJson(74));
    string declineStorage = Path.Combine(root, "Golden Import Decline Storage");
    GoldenCdbService declineGolden = new(json, declineStorage);
    declineGolden.SetFromFile(sourceB);
    string declineIdentity = declineGolden.GetState().Identity;
    TestMessages declineMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: false);
    MainViewModel declineMain = CreateMain(
        json,
        new TestFileDialogs(),
        declineMessages,
        declineGolden,
        new GoldenCdbComparisonService(),
        quickBmsImportOptions: declineFixture.Options);
    declineMain.UseQuickBmsImportServiceForTesting(declineFixture.Service);
    Check(!await declineMain.ImportCurrentWartalesAsGoldenAsync() &&
          declineFixture.Runner.Requests.Count == 1 &&
          declineMain.Project?.FileName == declineFixture.PromotedCdbPath &&
          declineGolden.GetState().Identity == declineIdentity &&
          declineMessages.ConfirmationCount == 1 &&
          declineMessages.LastInformation?.Contains(
              "not replaced",
              StringComparison.OrdinalIgnoreCase) == true,
        "declining Golden replacement preserves old Golden and successful imported project");

    GoldenQuickBmsFixture cancelFixture = new(
        json,
        Path.Combine(root, "Golden Import Cancel"),
        BaseJson(75));
    GoldenCdbService cancelImportGolden = new(
        json,
        Path.Combine(root, "Golden Import Cancel Storage"));
    cancelImportGolden.SetFromFile(sourceB);
    string cancelImportIdentity = cancelImportGolden.GetState().Identity;
    TestMessages cancelImportMessages = new(
        UnsavedChangesResult.Cancel,
        confirmation: true);
    MainViewModel cancelImportMain = CreateMain(
        json,
        new TestFileDialogs(),
        cancelImportMessages,
        cancelImportGolden,
        new GoldenCdbComparisonService(),
        quickBmsImportOptions: cancelFixture.Options);
    cancelImportMain.UseQuickBmsImportServiceForTesting(cancelFixture.Service);
    ProjectModel cancelImportProject = json.LoadReferenceProject(sourceC);
    cancelImportMain.PromoteLoadedProject(cancelImportProject, sourceC);
    cancelImportProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 751L;
    Check(!await cancelImportMain.ImportCurrentWartalesAsGoldenAsync() &&
          cancelFixture.Runner.Requests.Count == 0 &&
          ReferenceEquals(cancelImportMain.Project, cancelImportProject) &&
          cancelImportGolden.GetState().Identity == cancelImportIdentity &&
          cancelImportMessages.UnsavedPromptCount == 1 &&
          cancelImportMessages.ConfirmationCount == 0,
        "cancelled shared import leaves current project and Golden unchanged");

    GoldenQuickBmsFixture failureFixture = new(
        json,
        Path.Combine(root, "Golden Import Failure"),
        BaseJson(76),
        failImport: true);
    GoldenCdbService importFailureGolden = new(
        json,
        Path.Combine(root, "Golden Import Failure Storage"));
    importFailureGolden.SetFromFile(sourceB);
    string importFailureIdentity = importFailureGolden.GetState().Identity;
    TestMessages importFailureMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true);
    MainViewModel importFailureMain = CreateMain(
        json,
        new TestFileDialogs(),
        importFailureMessages,
        importFailureGolden,
        new GoldenCdbComparisonService(),
        quickBmsImportOptions: failureFixture.Options);
    importFailureMain.UseQuickBmsImportServiceForTesting(failureFixture.Service);
    Check(!await importFailureMain.ImportCurrentWartalesAsGoldenAsync() &&
          failureFixture.Runner.Requests.Count == 1 &&
          importFailureMain.Project == null &&
          importFailureGolden.GetState().Identity == importFailureIdentity &&
          importFailureMessages.LastError != null &&
          importFailureMessages.ConfirmationCount == 0,
        "failed shared QuickBMS import creates no project or Golden replacement");

    GoldenQuickBmsFixture designationFailureFixture = new(
        json,
        Path.Combine(root, "Golden Designation Failure"),
        BaseJson(77));
    string designationFailureStorage =
        Path.Combine(root, "Golden Designation Failure Storage");
    GoldenCdbService designationSeed = new(json, designationFailureStorage);
    designationSeed.SetFromFile(sourceB);
    string designationOldIdentity = designationSeed.GetState().Identity;
    GoldenCdbService designationFailureGolden = new(
        json,
        designationFailureStorage,
        new FaultHooks { ThrowAfterPromotion = true });
    TestMessages designationFailureMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true);
    MainViewModel designationFailureMain = CreateMain(
        json,
        new TestFileDialogs(),
        designationFailureMessages,
        designationFailureGolden,
        new GoldenCdbComparisonService(),
        quickBmsImportOptions: designationFailureFixture.Options);
    designationFailureMain.UseQuickBmsImportServiceForTesting(
        designationFailureFixture.Service);
    Check(!await designationFailureMain.ImportCurrentWartalesAsGoldenAsync() &&
          designationFailureMain.Project?.FileName ==
              designationFailureFixture.PromotedCdbPath &&
          File.Exists(designationFailureFixture.PromotedCdbPath) &&
          designationFailureGolden.GetState().Identity ==
              designationOldIdentity &&
          designationFailureMessages.LastError?.Contains(
              "imported successfully",
              StringComparison.OrdinalIgnoreCase) == true &&
          designationFailureMessages.ConfirmationCount == 1 &&
          designationFailureMain.Status.Contains(
              "import succeeded",
              StringComparison.OrdinalIgnoreCase),
        "Golden designation failure preserves truthful successful import and previous Golden");

    GoldenQuickBmsFixture originalCommandFixture = new(
        json,
        Path.Combine(root, "Original Import Command"),
        BaseJson(78));
    string originalCommandStorage =
        Path.Combine(root, "Original Import Command Storage");
    GoldenCdbService originalCommandGolden = new(
        json,
        originalCommandStorage);
    originalCommandGolden.SetFromFile(sourceB);
    byte[] originalCommandGoldenBytes =
        File.ReadAllBytes(originalCommandGolden.GetCanonicalPath());
    string originalCommandGoldenIdentity =
        originalCommandGolden.GetState().Identity;
    TestMessages originalCommandMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true);
    MainViewModel originalCommandMain = CreateMain(
        json,
        new TestFileDialogs(),
        originalCommandMessages,
        originalCommandGolden,
        new GoldenCdbComparisonService(),
        quickBmsImportOptions: originalCommandFixture.Options);
    originalCommandMain.UseQuickBmsImportServiceForTesting(
        originalCommandFixture.Service);
    originalCommandMain.ImportFromWartalesCommand.Execute(null);
    Check(originalCommandFixture.Runner.Requests.Count == 1 &&
          originalCommandMain.Project?.FileName ==
              originalCommandFixture.PromotedCdbPath &&
          originalCommandMain.CurrentFile ==
              originalCommandFixture.PromotedCdbPath &&
          originalCommandMain.Project.SourceProvenanceStatus ==
              SourceProvenanceStatus.Verified &&
          originalCommandMain.Project.SourceCdbGenerationIdentity ==
              originalCommandMain.Project.CurrentCdbContentIdentity,
        "original Import From Wartales command runs one normal production import and publishes provenance");
    Check(originalCommandMessages.ConfirmationCount == 0 &&
          originalCommandGolden.GetState().Identity ==
              originalCommandGoldenIdentity &&
          File.ReadAllBytes(originalCommandGolden.GetCanonicalPath())
              .SequenceEqual(originalCommandGoldenBytes),
        "original Import From Wartales command has no Golden confirmation designation or byte side effect");
    Check(originalCommandMain.Status.Contains(
              "Imported Wartales data",
              StringComparison.Ordinal) &&
          originalCommandMessages.InformationCount == 1 &&
          originalCommandMessages.LastInformation?.Contains(
              "imported successfully",
              StringComparison.OrdinalIgnoreCase) == true,
        "original Import From Wartales command retains normal success status and messaging");

    Exception? lifecycleException = null;
    Thread lifecycleThread = new(() =>
    {
        try
        {
            RunGoldenWindowLifecycleRegression();
        }
        catch (Exception exception)
        {
            lifecycleException = exception;
        }
    });
    lifecycleThread.SetApartmentState(ApartmentState.STA);
    lifecycleThread.Start();
    lifecycleThread.Join();
    if (lifecycleException != null)
    {
        throw new InvalidOperationException(
            "Golden window lifecycle regression failed.",
            lifecycleException);
    }

    service.SetFromFile(sourceC);
    string preGoldenFile = WriteCdb("pre-golden-current.cdb", BaseJson(71));
    ProjectModel preGoldenProject = json.LoadReferenceProject(preGoldenFile);
    preGoldenProject.IsModified = true;
    TestMessages loadCancelMessages = new(
        UnsavedChangesResult.Cancel,
        confirmation: true);
    MainViewModel loadCancelMain = CreateMain(
        json,
        new TestFileDialogs(),
        loadCancelMessages,
        service,
        new GoldenCdbComparisonService());
    loadCancelMain.PromoteLoadedProject(preGoldenProject, preGoldenFile);
    preGoldenProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 72L;
    Check(!loadCancelMain.LoadGoldenCdb() &&
          ReferenceEquals(loadCancelMain.Project, preGoldenProject) &&
          loadCancelMain.CurrentFile == preGoldenFile &&
          loadCancelMessages.UnsavedPromptCount == 1,
        "Load Golden cancel preserves current project and invokes unsaved protection");

    EditHistoryService loadHistory = new();
    TestMessages loadMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true);
    MainViewModel loadMain = CreateMain(
        json,
        new TestFileDialogs(),
        loadMessages,
        service,
        new GoldenCdbComparisonService(),
        loadHistory);
    ProjectModel beforeLoad = json.LoadReferenceProject(preGoldenFile);
    loadMain.PromoteLoadedProject(beforeLoad, preGoldenFile);
    PropertyModel historyProperty = beforeLoad.Sheets.Single().Entries.Single()
        .Properties.Single(property => property.EffectivePropertyPath == "value");
    loadHistory.Record(historyProperty, new JValue(71), new JValue(72));
    Check(loadHistory.CanUndo, "load setup has edit history");
    Check(loadMain.LoadGoldenCdb() &&
          loadMain.Project != null &&
          !ReferenceEquals(loadMain.Project, beforeLoad) &&
          loadMain.CurrentFile == service.GetCanonicalPath() &&
          loadMain.Project.FileName == service.GetCanonicalPath() &&
          loadMain.Project.CurrentCdbContentIdentity == service.GetState().Identity &&
          loadMain.Project.SourceCdbGenerationIdentity == null &&
          loadMain.Project.SourceProvenanceStatus == SourceProvenanceStatus.Unknown &&
          loadMain.Project.GameplayOperationStates.Count == 0,
        "Load Golden publishes detached sidecar-free project with Golden identity boundaries");
    Check(!loadHistory.CanUndo && !loadHistory.CanRedo,
        "Load Golden clears edit history through normal project promotion");

    string publicationFailureFile = WriteCdb(
        "publication-failure-current.cdb",
        BaseJson(88));
    EditHistoryService publicationFailureHistory = new();
    TestMessages publicationFailureMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true);
    MainViewModel publicationFailureMain = CreateMain(
        json,
        new TestFileDialogs(),
        publicationFailureMessages,
        service,
        new GoldenCdbComparisonService(),
        publicationFailureHistory);
    ProjectModel publicationFailureProject =
        json.LoadReferenceProject(publicationFailureFile);
    publicationFailureMain.PromoteLoadedProject(
        publicationFailureProject,
        publicationFailureFile);
    PropertyModel publicationHistoryProperty =
        publicationFailureProject.Sheets.Single().Entries.Single().Properties
            .Single(property => property.EffectivePropertyPath == "value");
    publicationFailureHistory.Record(
        publicationHistoryProperty,
        new JValue(88),
        new JValue(89));
    string[] referencesBeforeFailure = ReferenceDataService.Instance
        .GetValues("constant", "value")
        .Select(value => value.Value)
        .ToArray();
    string goldenBeforePublicationFailure = service.GetState().Identity;
    publicationFailureMain.UseProjectPublicationFailureForTesting(
        () => throw new InvalidOperationException(
            "Injected project publication failure."));
    Check(!publicationFailureMain.LoadGoldenCdb() &&
          ReferenceEquals(
              publicationFailureMain.Project,
              publicationFailureProject) &&
          publicationFailureMain.CurrentFile == publicationFailureFile &&
          publicationFailureProject.FileName == publicationFailureFile &&
          publicationFailureHistory.CanUndo &&
          ReferenceDataService.Instance.GetValues("constant", "value")
              .Select(value => value.Value)
              .SequenceEqual(referencesBeforeFailure) &&
          service.GetState().Identity == goldenBeforePublicationFailure &&
          publicationFailureMessages.LastError?.Contains(
              "current project was preserved",
              StringComparison.OrdinalIgnoreCase) == true,
        "Load Golden publication failure restores project file references history and Golden state");
    publicationFailureMain.UseProjectPublicationFailureForTesting(null);

    File.WriteAllText(service.GetCanonicalPath(), "invalid Golden");
    ProjectModel beforeInvalidLoad = loadMain.Project!;
    string beforeInvalidFile = loadMain.CurrentFile;
    Check(!loadMain.LoadGoldenCdb() &&
          ReferenceEquals(loadMain.Project, beforeInvalidLoad) &&
          loadMain.CurrentFile == beforeInvalidFile &&
          loadMessages.LastError?.Contains("current project was preserved", StringComparison.OrdinalIgnoreCase) == true,
        "invalid Golden load preserves active project and reports nonfatal error");

    service.SetFromFile(sourceC);
    string goldenBeforeSave = service.GetState().Identity;
    GoldenCdbComparisonService saveComparer = new();
    _ = saveComparer.Compare(
        json.LoadReferenceProject(sourceC),
        service.LoadReference());
    TestFileDialogs directDialogs = new();
    List<string> directSaveOrder = new();
    TestMessages directMessages = new(
        UnsavedChangesResult.Save,
        confirmation: true)
    {
        EventLog = directSaveOrder
    };
    MainViewModel directMain = CreateMain(
        json,
        directDialogs,
        directMessages,
        service,
        saveComparer);
    directMain.UseSaveValidationStartedForTesting(
        () => directSaveOrder.Add("validation"));
    ProjectModel loadedGolden = service.LoadDetachedProject();
    directMain.PromoteLoadedProject(
        loadedGolden,
        service.GetCanonicalPath());
    loadedGolden.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 44L;
    directMain.SaveCommand.Execute(null);
    GoldenCdbState savedGolden = service.GetState();
    Check(savedGolden.Identity != goldenBeforeSave &&
          json.LoadReferenceProject(service.GetCanonicalPath())
              .RootDocument["sheets"]![0]!["lines"]![0]!["value"]!.Value<long>() == 44,
        "Save Golden Anyway writes canonical and refreshes identity");
    Check(directDialogs.SaveCount == 0 &&
          !saveComparer.HasCachedGoldenIndex &&
          directSaveOrder.SequenceEqual(
              new[] { "golden-choice", "validation" }),
        "Save Golden Anyway skips picker and invalidates comparison cache");
    Check(loadedGolden.SourceCdbGenerationIdentity == null,
        "save-over preserves unknown source provenance");

    service.SetFromFile(sourceC);
    string alternate = Path.Combine(root, "golden-copy.cdb");
    List<string> alternateSaveOrder = new();
    TestFileDialogs alternateDialogs = new()
    {
        SaveFileName = alternate,
        EventLog = alternateSaveOrder
    };
    TestMessages alternateMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true)
    {
        EventLog = alternateSaveOrder
    };
    MainViewModel alternateMain = CreateMain(
        json,
        alternateDialogs,
        alternateMessages,
        service,
        new GoldenCdbComparisonService());
    alternateMain.UseSaveValidationStartedForTesting(
        () => alternateSaveOrder.Add("validation"));
    ProjectModel alternateProject = service.LoadDetachedProject();
    alternateMain.PromoteLoadedProject(
        alternateProject,
        service.GetCanonicalPath());
    alternateProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 45L;
    string canonicalBeforeAlternate = service.GetState().Identity;
    alternateMain.SaveCommand.Execute(null);
    Check(File.Exists(alternate) &&
          service.GetState().Identity == canonicalBeforeAlternate &&
          alternateProject.FileName == Path.GetFullPath(alternate),
        "Choose Another Location preserves Golden and follows Save As semantics");
    Check(alternateDialogs.SaveCount == 1,
        "Choose Another Location opens existing save dialog");
    Check(alternateSaveOrder.SequenceEqual(
            new[] { "golden-choice", "picker", "validation" }),
        "Choose Another Location resolves intent and destination before validation");

    service.SetFromFile(sourceC);
    string cancelledTarget = Path.Combine(root, "cancelled.cdb");
    List<string> cancelSaveOrder = new();
    TestFileDialogs cancelDialogs = new() { SaveFileName = cancelledTarget };
    TestMessages cancelMessages = new(
        UnsavedChangesResult.Cancel,
        confirmation: true)
    {
        EventLog = cancelSaveOrder
    };
    MainViewModel cancelMain = CreateMain(
        json,
        cancelDialogs,
        cancelMessages,
        service,
        new GoldenCdbComparisonService());
    cancelMain.UseSaveValidationStartedForTesting(
        () => cancelSaveOrder.Add("validation"));
    ProjectModel cancelProject = service.LoadDetachedProject();
    cancelMain.PromoteLoadedProject(
        cancelProject,
        service.GetCanonicalPath());
    cancelProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 46L;
    string canonicalBeforeCancel = service.GetState().Identity;
    cancelMain.SaveCommand.Execute(null);
    Check(!File.Exists(cancelledTarget) &&
          service.GetState().Identity == canonicalBeforeCancel &&
          cancelDialogs.SaveCount == 0 &&
          cancelSaveOrder.SequenceEqual(new[] { "golden-choice" }),
        "Cancel save-over performs no validation picker or write");

    string otherFile = WriteCdb("other-project.cdb", BaseJson(6));
    service.SetFromFile(sourceC);
    List<string> rejectSaveOrder = new();
    TestFileDialogs rejectDialogs = new()
    {
        SaveFileName = service.GetCanonicalPath(),
        EventLog = rejectSaveOrder
    };
    TestMessages rejectMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: false)
    {
        EventLog = rejectSaveOrder
    };
    MainViewModel rejectMain = CreateMain(
        json,
        rejectDialogs,
        rejectMessages,
        service,
        new GoldenCdbComparisonService());
    rejectMain.UseSaveValidationStartedForTesting(
        () => rejectSaveOrder.Add("validation"));
    ProjectModel otherProject = json.LoadReferenceProject(otherFile);
    rejectMain.PromoteLoadedProject(otherProject, otherFile);
    otherProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 47L;
    string beforeRejectedDestination = service.GetState().Identity;
    rejectMain.SaveCommand.Execute(null);
    Check(service.GetState().Identity == beforeRejectedDestination &&
          rejectMessages.ConfirmationCount == 1 &&
          rejectSaveOrder.SequenceEqual(
              new[] { "picker", "confirmation" }),
        "another project rejection occurs before validation and preserves Golden");

    List<string> approveSaveOrder = new();
    TestFileDialogs approveDialogs = new()
    {
        SaveFileName = service.GetCanonicalPath(),
        EventLog = approveSaveOrder
    };
    TestMessages approveMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true)
    {
        EventLog = approveSaveOrder
    };
    MainViewModel approveMain = CreateMain(
        json,
        approveDialogs,
        approveMessages,
        service,
        new GoldenCdbComparisonService());
    approveMain.UseSaveValidationStartedForTesting(
        () => approveSaveOrder.Add("validation"));
    ProjectModel approvedProject = json.LoadReferenceProject(otherFile);
    approveMain.PromoteLoadedProject(approvedProject, otherFile);
    approvedProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 48L;
    approveMain.SaveCommand.Execute(null);
    Check(json.LoadReferenceProject(service.GetCanonicalPath())
              .RootDocument["sheets"]![0]!["lines"]![0]!["value"]!.Value<long>() == 48 &&
          service.GetState().IsAvailable,
        "confirmed destination overwrite changes and reconciles Golden");
    Check(approveSaveOrder.SequenceEqual(
            new[] { "picker", "confirmation", "validation" }),
        "another-project Golden confirmation precedes validation");

    service.SetFromFile(sourceC);
    List<string> chooseGoldenAgainOrder = new();
    TestFileDialogs chooseGoldenAgainDialogs = new()
    {
        SaveFileName = service.GetCanonicalPath(),
        EventLog = chooseGoldenAgainOrder
    };
    TestMessages chooseGoldenAgainMessages = new(
        UnsavedChangesResult.Discard,
        confirmation: true)
    {
        EventLog = chooseGoldenAgainOrder
    };
    MainViewModel chooseGoldenAgainMain = CreateMain(
        json,
        chooseGoldenAgainDialogs,
        chooseGoldenAgainMessages,
        service,
        new GoldenCdbComparisonService());
    chooseGoldenAgainMain.UseSaveValidationStartedForTesting(
        () => chooseGoldenAgainOrder.Add("validation"));
    ProjectModel chooseGoldenAgainProject = service.LoadDetachedProject();
    chooseGoldenAgainMain.PromoteLoadedProject(
        chooseGoldenAgainProject,
        service.GetCanonicalPath());
    chooseGoldenAgainProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 481L;
    chooseGoldenAgainMain.SaveCommand.Execute(null);
    Check(chooseGoldenAgainOrder.SequenceEqual(new[]
        {
            "golden-choice",
            "picker",
            "confirmation",
            "validation"
        }) && chooseGoldenAgainMessages.ConfirmationCount == 1,
        "Choose Another Location selecting Golden again requires confirmation before validation");

    service.SetFromFile(sourceC);
    List<string> invalidGoldenSaveOrder = new();
    TestMessages invalidGoldenSaveMessages = new(
        UnsavedChangesResult.Save,
        confirmation: true)
    {
        EventLog = invalidGoldenSaveOrder
    };
    MainViewModel invalidGoldenSaveMain = CreateMain(
        json,
        new TestFileDialogs(),
        invalidGoldenSaveMessages,
        service,
        new GoldenCdbComparisonService());
    invalidGoldenSaveMain.UseSaveValidationStartedForTesting(
        () => invalidGoldenSaveOrder.Add("validation"));
    ProjectModel invalidGoldenSaveProject = service.LoadDetachedProject();
    invalidGoldenSaveMain.PromoteLoadedProject(
        invalidGoldenSaveProject,
        service.GetCanonicalPath());
    string invalidGoldenOriginalIdentity = service.GetState().Identity;
    invalidGoldenSaveProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .SourceProperty = null;
    invalidGoldenSaveProject.IsModified = true;
    invalidGoldenSaveMain.SaveCommand.Execute(null);
    Check(invalidGoldenSaveOrder.SequenceEqual(
              new[] { "golden-choice", "validation" }) &&
          service.GetState().Identity == invalidGoldenOriginalIdentity,
        "Save Golden Anyway resolves intent first and validation blocks invalid write");

    string ordinarySource = WriteCdb("ordinary-source.cdb", BaseJson(61));
    string ordinaryTarget = Path.Combine(root, "ordinary-target.cdb");
    List<string> ordinarySaveOrder = new();
    TestFileDialogs ordinaryDialogs = new()
    {
        SaveFileName = ordinaryTarget,
        EventLog = ordinarySaveOrder
    };
    MainViewModel ordinaryMain = CreateMain(
        json,
        ordinaryDialogs,
        new TestMessages(
            UnsavedChangesResult.Discard,
            confirmation: true)
        {
            EventLog = ordinarySaveOrder
        },
        service,
        new GoldenCdbComparisonService());
    ordinaryMain.UseSaveValidationStartedForTesting(
        () => ordinarySaveOrder.Add("validation"));
    ProjectModel ordinaryProject = json.LoadReferenceProject(ordinarySource);
    ordinaryMain.PromoteLoadedProject(ordinaryProject, ordinarySource);
    ordinaryProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 62L;
    ordinaryMain.SaveCommand.Execute(null);
    Check(File.Exists(ordinaryTarget) &&
          ordinarySaveOrder.SequenceEqual(new[] { "picker", "validation" }) &&
          json.LoadReferenceProject(ordinaryTarget)
              .RootDocument["sheets"]![0]!["lines"]![0]!["value"]!.Value<long>() == 62,
        "ordinary non-Golden save selects destination then validates and saves normally");

    service.SetFromFile(sourceC);
    GoldenCdbComparisonService partialComparer = new();
    TestMessages partialMessages = new(
        UnsavedChangesResult.Save,
        confirmation: true);
    MainViewModel partialMain = CreateMain(
        json,
        new TestFileDialogs(),
        partialMessages,
        service,
        partialComparer);
    ProjectModel partialProject = service.LoadDetachedProject();
    partialMain.PromoteLoadedProject(
        partialProject,
        service.GetCanonicalPath());
    partialProject.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 49L;
    string sidecarPath = service.GetCanonicalPath() + ".wtstate";
    if (File.Exists(sidecarPath))
        File.Delete(sidecarPath);
    Directory.CreateDirectory(sidecarPath);
    partialMain.SaveCommand.Execute(null);
    Directory.Delete(sidecarPath);
    Check(json.LoadReferenceProject(service.GetCanonicalPath())
              .RootDocument["sheets"]![0]!["lines"]![0]!["value"]!.Value<long>() == 49 &&
          service.GetState().Identity == identities.Calculate(service.GetCanonicalPath()) &&
          partialMessages.LastError?.Contains("could not be saved", StringComparison.OrdinalIgnoreCase) == true,
        "partial sidecar failure truthfully reports failure and reconciles actual Golden bytes");

    service.Remove();
    TestMessages recreateMessages = new(
        UnsavedChangesResult.Save,
        confirmation: true);
    MainViewModel recreateMain = CreateMain(
        json,
        new TestFileDialogs(),
        recreateMessages,
        service,
        new GoldenCdbComparisonService());
    ProjectModel removedWhileLoaded = json.LoadReferenceProject(sourceC);
    removedWhileLoaded.FileName = service.GetCanonicalPath();
    recreateMain.PromoteLoadedProject(
        removedWhileLoaded,
        service.GetCanonicalPath());
    removedWhileLoaded.Sheets.Single().Entries.Single().Properties
        .Single(property => property.EffectivePropertyPath == "value")
        .Value = 50L;
    recreateMain.SaveCommand.Execute(null);
    Check(service.GetState().IsAvailable &&
          recreateMessages.UnsavedPromptCount == 1,
        "reserved canonical save protection remains after Remove");

    Console.WriteLine($"PASS {checks} Golden CDB checks");

    GoldenCdbComparisonResult CheckComparison(
        string currentJson,
        Func<GoldenCdbComparisonItem, bool> predicate,
        string name)
    {
        string file = WriteCdb(
            $"comparison-{Guid.NewGuid():N}.cdb",
            currentJson);
        ProjectModel current = json.LoadReferenceProject(file);
        GoldenCdbComparisonResult result = comparer.Compare(
            current,
            service.LoadReference());
        Check(result.Items.Any(predicate), name);
        Check(result.DifferenceCount == result.Differences.Count &&
              result.CoverageIssueCount == result.CoverageIssues.Count &&
              result.Differences.All(item => !item.IsCoverageIssue),
            name + " count classification");
        return result;
    }

    GoldenCdbComparisonResult CompareJson(string currentJson)
    {
        string file = WriteCdb(
            $"comparison-{Guid.NewGuid():N}.cdb",
            currentJson);
        return comparer.Compare(
            json.LoadReferenceProject(file),
            service.LoadReference());
    }

    bool NoTransientArtifacts() =>
        !File.Exists(service.GetCanonicalPath() + ".candidate.tmp") &&
        !File.Exists(service.GetCanonicalPath() + ".rollback.tmp");

    bool OnlyCanonicalFile(GoldenCdbService goldenService) =>
        Directory.GetFiles(goldenService.GetCanonicalDirectory())
            .Select(Path.GetFileName)
            .SequenceEqual(new[] { "data.cdb" });

    void RunGoldenWindowLifecycleRegression()
    {
        Application application = new()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        application.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(
                    "pack://application:,,,/WartalesEditor;component/Resources/SharedUiResources.xaml",
                    UriKind.Absolute)
            });
        Window owner = new()
        {
            Width = 1,
            Height = 1,
            Left = -20000,
            Top = -20000,
            Opacity = 0,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None
        };
        application.MainWindow = owner;
        owner.Show();

        try
        {
            GoldenQuickBmsFixture fixture = new(
                json,
                Path.Combine(root, "Golden Window Lifecycle"),
                BaseJson(79));
            GoldenCdbService lifecycleGolden = new(
                json,
                Path.Combine(root, "Golden Window Lifecycle Storage"));
            lifecycleGolden.SetFromFile(sourceB);
            string initialIdentity = lifecycleGolden.GetState().Identity;
            byte[] initialBytes =
                File.ReadAllBytes(lifecycleGolden.GetCanonicalPath());
            TestMessages lifecycleMessages = new(
                UnsavedChangesResult.Discard,
                confirmation: true);
            MainViewModel lifecycleMain = CreateMain(
                json,
                new TestFileDialogs(),
                lifecycleMessages,
                lifecycleGolden,
                new GoldenCdbComparisonService(),
                quickBmsImportOptions: fixture.Options);
            lifecycleMain.UseQuickBmsImportServiceForTesting(
                fixture.Service);

            GoldenCdbWindow? previousWindow = null;
            Button? previousButton = null;
            string previousIdentity = initialIdentity;

            for (int cycle = 1; cycle <= 3; cycle++)
            {
                if (cycle > 1)
                {
                    File.Delete(fixture.PromotedCdbPath);
                    fixture.ExtractedCdbJson = BaseJson(78 + cycle);
                }

                lifecycleMain.ShowGoldenCdbCommand.Execute(null);
                GoldenCdbWindow window =
                    application.Windows
                        .OfType<GoldenCdbWindow>()
                        .Single(candidate => candidate.IsVisible);
                Button importButton = FindButton(
                    window,
                    "Import Current Wartales CDB as Golden");

                Check(lifecycleMain.IsGoldenCdbWindowOpen &&
                      !ReferenceEquals(previousWindow, window),
                    $"Golden lifecycle cycle {cycle} opens one fresh tracked window");

                int requestsBefore = fixture.Runner.Requests.Count;
                int confirmationsBefore = lifecycleMessages.ConfirmationCount;
                int informationBefore = lifecycleMessages.InformationCount;
                importButton.RaiseEvent(
                    new RoutedEventArgs(Button.ClickEvent));

                string currentIdentity = lifecycleGolden.GetState().Identity;
                Check(fixture.Runner.Requests.Count == requestsBefore + 1 &&
                      lifecycleMessages.ConfirmationCount == confirmationsBefore + 1 &&
                      lifecycleMessages.ConfirmationTitles.Last() ==
                          "Replace Golden CDB?",
                    $"Golden lifecycle cycle {cycle} produces exactly one import and replacement confirmation");
                Check(lifecycleMessages.InformationCount == informationBefore + 2 &&
                      lifecycleMain.Project?.FileName == fixture.PromotedCdbPath &&
                      lifecycleMain.Project.SourceProvenanceStatus ==
                          SourceProvenanceStatus.Verified &&
                      lifecycleMain.Project.SourceCdbGenerationIdentity ==
                          lifecycleMain.Project.CurrentCdbContentIdentity,
                    $"Golden lifecycle cycle {cycle} produces one import result one designation result and normal provenance");
                Check(currentIdentity != previousIdentity &&
                      currentIdentity == identities.Calculate(fixture.PromotedCdbPath) &&
                      File.ReadAllBytes(lifecycleGolden.GetCanonicalPath())
                          .SequenceEqual(File.ReadAllBytes(fixture.PromotedCdbPath)) &&
                      OnlyCanonicalFile(lifecycleGolden),
                    $"Golden lifecycle cycle {cycle} atomically designates exact imported bytes without residue");

                if (cycle == 1)
                {
                    Check(!File.ReadAllBytes(lifecycleGolden.GetCanonicalPath())
                              .SequenceEqual(initialBytes) &&
                          currentIdentity != initialIdentity,
                        "accepted existing-Golden replacement replaces A with exact imported B bytes and identity");
                }

                window.Close();
                Check(!lifecycleMain.IsGoldenCdbWindowOpen,
                    $"Golden lifecycle cycle {cycle} close clears tracked window state");

                int closedRequests = fixture.Runner.Requests.Count;
                int closedConfirmations = lifecycleMessages.ConfirmationCount;
                importButton.RaiseEvent(
                    new RoutedEventArgs(Button.ClickEvent));
                Check(fixture.Runner.Requests.Count == closedRequests &&
                      lifecycleMessages.ConfirmationCount == closedConfirmations,
                    $"Golden lifecycle cycle {cycle} closed window cannot trigger stale import or designation");

                previousWindow = window;
                previousButton = importButton;
                previousIdentity = currentIdentity;
            }

            previousButton!.RaiseEvent(
                new RoutedEventArgs(Button.ClickEvent));
            Check(fixture.Runner.Requests.Count == 3 &&
                  lifecycleMessages.ConfirmationCount == 3 &&
                  lifecycleMessages.InformationCount == 6,
                "three Golden window cycles retain one event path without duplicate callbacks or messages");
        }
        finally
        {
            foreach (Window window in application.Windows.Cast<Window>().ToArray())
                window.Close();
            application.Shutdown();
        }
    }

    Button FindButton(
        DependencyObject parent,
        string content)
    {
        if (parent is Button button &&
            string.Equals(
                button.Content as string,
                content,
                StringComparison.Ordinal))
        {
            return button;
        }

        foreach (object child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is DependencyObject dependencyObject)
            {
                try
                {
                    return FindButton(dependencyObject, content);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        throw new InvalidOperationException(
            $"Button '{content}' was not found.");
    }

    string WriteCdb(string name, string content)
    {
        string path = Path.Combine(root, name);
        File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(content));
        return path;
    }

    MainViewModel CreateMain(
        JsonDataService dataService,
        IFileDialogService dialogs,
        IMessageDialogService messages,
        GoldenCdbService goldenService,
        GoldenCdbComparisonService comparisonService,
        EditHistoryService? editHistory = null,
        QuickBmsImportOptions? quickBmsImportOptions = null)
    {
        LocalizationService localization = new();
        ModificationSnapshotWorkflowService snapshotWorkflow = new();
        ValidationWorkflowService validationWorkflow = new(
            new ValidationService(dataService));
        ProjectMutationService mutationService = new();
        ContentCreationService contentService = new(mutationService);
        AddCampFacilitiesOperation addCamp = new(contentService);
        UpgradeAllEquipmentOperation upgrade = new(contentService);
        ProjectOperationTransactionService transaction = new();
        ProjectOperationService operation = new(
            new OperationValidatorProvider(),
            transaction);
        ProfileOperationCaptureService capture = new(
            new OperationValidatorProvider(),
            addCamp,
            upgrade);
        ModProfileService profiles = new(
            new ModificationSnapshotService(),
            capture);
        MainViewModel main = new(
            dataService,
            new SearchService(),
            localization,
            editHistory ?? new EditHistoryService(),
            new ModificationSnapshotService(),
            snapshotWorkflow,
            new ChangeSummaryService(),
            new ModProfileLibraryService(),
            new ModProfileWorkflowService(
                profiles,
                new ModProfileSerializationService(),
                snapshotWorkflow,
                new ProfileOperationResolver(addCamp, upgrade),
                operation,
                transaction),
            ReferenceDataService.Instance,
            validationWorkflow,
            new ValidationPresentationService(),
            operation,
            transaction,
            addCamp,
            upgrade,
            dialogs,
            messages,
            new LanguageDataService(
                localization,
                Path.Combine(root, "Language Data")),
            new WartalesInstallationService(),
            quickBmsImportOptions ??
                QuickBmsImportOptions.CreateDefault());
        main.UseGoldenCdbServicesForTesting(
            goldenService,
            comparisonService);
        return main;
    }
}
finally
{
    if (Directory.Exists(root))
        Directory.Delete(root, recursive: true);
}

Environment.Exit(0);
return;

void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException("FAIL " + name);
    checks++;
}

void CheckThrows(Action action, string name)
{
    try
    {
        action();
    }
    catch
    {
        Check(true, name);
        return;
    }

    Check(false, name);
}

void CheckThrowsExact<TException>(Action action, string name)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        Check(true, name);
        return;
    }

    Check(false, name);
}

void CheckThrowsMessage(
    Action action,
    string expectedMessage,
    string name)
{
    try
    {
        action();
    }
    catch (Exception exception)
    {
        Check(
            exception.Message.Contains(
                expectedMessage,
                StringComparison.OrdinalIgnoreCase),
            name);
        return;
    }

    Check(false, name);
}

string BaseJson(object value) =>
    "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{" +
    "\"id\":\"A\",\"arr\":[{\"x\":1}],\"props\":{\"nested\":5}," +
    $"\"value\":{JToken.FromObject(value).ToString(Newtonsoft.Json.Formatting.None)}" +
    "}]}]}";

string JsonWithProperty(bool includeExtra) =>
    "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{" +
    "\"id\":\"A\",\"arr\":[{\"x\":1}],\"props\":{\"nested\":5},\"value\":3" +
    (includeExtra ? ",\"extra\":8" : string.Empty) +
    "}]}]}";

string JsonWithEntries(bool includeA, bool includeB)
{
    List<string> entries = new();
    if (includeA)
        entries.Add("{\"id\":\"A\",\"value\":3}");
    if (includeB)
        entries.Add("{\"id\":\"B\",\"value\":2}");
    return "{\"sheets\":[{\"name\":\"constant\",\"lines\":[" +
           string.Join(',', entries) + "]}]}";
}

string JsonWithArray(string array) =>
    "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{" +
    $"\"id\":\"A\",\"arr\":{array},\"props\":{{\"nested\":5}},\"value\":3" +
    "}]}]}";

string RandomTraitJson()
{
    JObject sheet = new()
    {
        ["name"] = "trait",
        ["columns"] = new JArray
        {
            new JObject { ["typeStr"] = "0", ["name"] = "id" },
            new JObject
            {
                ["typeStr"] =
                    "10:Animal,NotAnimal,TwoHand,OneHand,Shield,Bow,FistWeapon",
                ["name"] = "gen",
                ["opt"] = true
            },
            new JObject { ["typeStr"] = "17", ["name"] = "props" },
            new JObject { ["typeStr"] = "2", ["name"] = "done", ["opt"] = true }
        }
    };
    JObject[] starting =
    {
        RandomTraitEntry("PositiveTrue", 0, true),
        RandomTraitEntry("NegativeAbsent", 1, null)
    };
    JObject[] hidden =
    {
        RandomTraitEntry("HiddenTrait", 0, "unsupported")
    };
    JObject[] recruitment =
    {
        RandomTraitEntry("RecruitmentWithoutPersonality", null, "unsupported"),
        RandomTraitEntry("PositiveAbsent", 0, null, 2),
        RandomTraitEntry("NegativeDisabled", 1, false)
    };
    JObject[] acquired =
    {
        RandomTraitEntry("AcquiredTrait", 0, "unsupported")
    };
    sheet["lines"] = new JArray(
        starting.Concat(hidden).Concat(recruitment).Concat(acquired));
    sheet["separators"] = new JArray
    {
        new JObject { ["title"] = "Starting", ["id"] = "PositiveTrue" },
        new JObject { ["title"] = "Hidden", ["id"] = "HiddenTrait" },
        new JObject
        {
            ["title"] = "Recruitment",
            ["id"] = "RecruitmentWithoutPersonality"
        },
        new JObject { ["title"] = "Acquired", ["id"] = "AcquiredTrait" }
    };
    return new JObject { ["sheets"] = new JArray(sheet) }
        .ToString(Newtonsoft.Json.Formatting.None);
}

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
        entry["gen"] = generationEligibility.Value;
    if (done != null)
        entry["done"] = JToken.FromObject(done);
    return entry;
}

string RepositoryRoot()
{
    string currentDirectory = Directory.GetCurrentDirectory();
    if (File.Exists(Path.Combine(currentDirectory, "WartalesEditor.csproj")))
        return currentDirectory;

    return Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}

sealed class FaultHooks : IGoldenCdbOperationHooks
{
    public bool ThrowAfterCandidate { get; init; }
    public bool ThrowAfterPromotion { get; init; }
    public bool CorruptRollback { get; init; }
    public bool FailCandidateCleanupOnce { get; init; }
    public bool FailRollbackCleanupOnce { get; init; }

    private bool candidateCleanupFailed;
    private bool rollbackCleanupFailed;

    public void AfterCandidateValidated(string candidatePath)
    {
        if (ThrowAfterCandidate)
            throw new IOException("Injected candidate failure.");
    }

    public void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath)
    {
        if (ThrowAfterPromotion)
            throw new IOException("Injected publication failure.");
    }

    public void BeforeRollbackRestore(
        string canonicalPath,
        string rollbackPath)
    {
        if (CorruptRollback)
            File.WriteAllText(rollbackPath, "corrupt rollback");
    }

    public void BeforeTemporaryCleanup(string temporaryPath)
    {
        if (FailCandidateCleanupOnce &&
            !candidateCleanupFailed &&
            temporaryPath.EndsWith(
                ".candidate.tmp",
                StringComparison.Ordinal))
        {
            candidateCleanupFailed = true;
            if (!File.Exists(temporaryPath))
                File.WriteAllText(temporaryPath, "injected candidate residue");
            throw new IOException("Injected candidate cleanup failure.");
        }

        if (FailRollbackCleanupOnce &&
            !rollbackCleanupFailed &&
            temporaryPath.EndsWith(
                ".rollback.tmp",
                StringComparison.Ordinal))
        {
            rollbackCleanupFailed = true;
            throw new IOException("Injected rollback cleanup failure.");
        }
    }
}

sealed class GoldenQuickBmsFixture
{
    public GoldenQuickBmsFixture(
        JsonDataService jsonDataService,
        string root,
        string extractedCdbJson,
        bool failImport = false)
    {
        ExtractedCdbJson = extractedCdbJson;
        string installation = Path.Combine(root, "Wartales");
        string tools = Path.Combine(root, "quickbms");
        string executable = Path.Combine(tools, "quickbms.exe");
        string script = Path.Combine(tools, "Shiro_Games_PAK_script.bms");
        Directory.CreateDirectory(installation);
        Directory.CreateDirectory(tools);
        File.WriteAllBytes(
            Path.Combine(installation, "res.pak"),
            new byte[]
            {
                (byte)'P', (byte)'A', (byte)'K', 0,
                1, 2, 3, 4
            });
        File.WriteAllText(executable, "fixture executable");
        File.WriteAllText(script, "fixture script");

        Options = new QuickBmsImportOptions
        {
            WartalesInstallationDirectory = installation,
            QuickBmsExecutablePath = executable,
            ShiroScriptPath = script,
            StagingRootDirectory = Path.Combine(root, "staging"),
            ProcessTimeout = TimeSpan.FromSeconds(10)
        };
        PromotedCdbPath = Path.Combine(
            installation,
            "Extracted",
            "data.cdb");
        Runner = new GoldenFakeExternalProcessRunner(request =>
        {
            if (failImport)
            {
                return new ExternalProcessResult
                {
                    Started = true,
                    ExitCode = 7,
                    StandardError = "injected import failure"
                };
            }

            string extracted = Path.Combine(
                request.Arguments[2],
                "content",
                "data.cdb");
            Directory.CreateDirectory(Path.GetDirectoryName(extracted)!);
            File.WriteAllText(extracted, ExtractedCdbJson);
            return new ExternalProcessResult
            {
                Started = true,
                ExitCode = 0
            };
        });
        Service = new QuickBmsImportService(
            jsonDataService,
            new WartalesInstallationService(),
            new QuickBmsToolchainService(),
            Runner,
            new ExtractionWorkspaceService(),
            new FileFingerprintService());
    }

    public QuickBmsImportOptions Options { get; }

    public string PromotedCdbPath { get; }

    public string ExtractedCdbJson { get; set; }

    public GoldenFakeExternalProcessRunner Runner { get; }

    public QuickBmsImportService Service { get; }
}

sealed class GoldenFakeExternalProcessRunner : IExternalProcessRunner
{
    private readonly Func<ExternalProcessRequest, ExternalProcessResult> run;

    public GoldenFakeExternalProcessRunner(
        Func<ExternalProcessRequest, ExternalProcessResult> run)
    {
        this.run = run;
    }

    public List<ExternalProcessRequest> Requests { get; } = new();

    public Task<ExternalProcessResult> RunAsync(
        ExternalProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(run(request));
    }
}

sealed class TestFileDialogs : IFileDialogService
{
    public string? OpenFileName { get; init; }
    public string? SaveFileName { get; init; }
    public List<string>? EventLog { get; init; }
    public int SaveCount { get; private set; }

    public string? ShowOpenFileDialog(
        string filter,
        string? initialFileName = null) => OpenFileName;

    public string? ShowSaveFileDialog(
        string filter,
        string? initialFileName = null)
    {
        SaveCount++;
        EventLog?.Add("picker");
        return SaveFileName;
    }
}

sealed class TestMessages : IMessageDialogService
{
    private readonly UnsavedChangesResult unsavedResult;
    private readonly bool confirmation;

    public TestMessages(
        UnsavedChangesResult unsavedResult,
        bool confirmation)
    {
        this.unsavedResult = unsavedResult;
        this.confirmation = confirmation;
    }

    public int ConfirmationCount { get; private set; }
    public int InformationCount { get; private set; }
    public int UnsavedPromptCount { get; private set; }
    public string? LastWarning { get; private set; }
    public string? LastError { get; private set; }
    public string? LastInformation { get; private set; }
    public List<string>? EventLog { get; init; }
    public List<string> ConfirmationTitles { get; } = new();

    public void ShowInformation(string message, string title)
    {
        InformationCount++;
        LastInformation = message;
    }

    public void ShowWarning(string message, string title) =>
        LastWarning = message;

    public void ShowError(string message, string title) =>
        LastError = message;

    public bool ShowConfirmation(string message, string title)
    {
        ConfirmationCount++;
        ConfirmationTitles.Add(title);
        EventLog?.Add("confirmation");
        return confirmation;
    }

    public UnsavedChangesResult ShowUnsavedChanges(
        string message,
        string title)
    {
        UnsavedPromptCount++;
        EventLog?.Add("golden-choice");
        return unsavedResult;
    }
}
