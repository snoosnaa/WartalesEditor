using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;

int checks = 0;
string root = Path.Combine(Path.GetTempPath(), "WartalesEditorUpdateSurvival", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);

try
{
    CdbGenerationIdentityService identities = new();
    byte[] exactA = System.Text.Encoding.UTF8.GetBytes("{\"sheets\":[]}");
    byte[] exactA2 = System.Text.Encoding.UTF8.GetBytes("{\"sheets\":[]}");
    byte[] semanticA = System.Text.Encoding.UTF8.GetBytes("{ \"sheets\" : [] }");
    Check(identities.Calculate(exactA) == identities.Calculate(exactA2), "1 same bytes identity");
    Check(identities.Calculate(exactA) != identities.Calculate(semanticA), "2 exact-byte identity");
    Check(identities.IsValid(identities.Calculate(exactA)), "3 canonical identity valid");
    Check(identities.Calculate(exactA).StartsWith("sha256:", StringComparison.Ordinal), "4 canonical prefix");
    Check(identities.Calculate(exactA)[7..] == identities.Calculate(exactA)[7..].ToLowerInvariant(), "5 canonical lowercase");

    string cdb = Path.Combine(root, "data.cdb");
    WriteCdb(cdb, 10, "unknown-preserved");
    JsonDataService data = new();
    ProjectModel ordinary = data.LoadProject(cdb);
    string openedCurrent = ordinary.CurrentCdbContentIdentity;
    Check(ordinary.SourceProvenanceStatus == SourceProvenanceStatus.Unknown, "6 missing manifest unknown source");
    Check(ordinary.SourceCdbGenerationIdentity == null, "7 missing manifest no source fabrication");
    Check(identities.IsValid(openedCurrent), "8 ordinary open current identity");
    Check(openedCurrent == identities.Calculate(File.ReadAllBytes(cdb)), "9 open hashes parsed bytes");
    Check((string?)ordinary.RootDocument["future"] == "unknown-preserved", "10 unknown root data loaded");

    EntryModel entry = ordinary.Sheets.Single().Entries.Single();
    new ProjectMutationService().EnsurePropertyByPath(entry, "value", 20);
    Check(ordinary.CurrentCdbContentIdentity == openedCurrent, "11 unsaved edit preserves current identity");
    Check(ordinary.SourceCdbGenerationIdentity == null, "12 unsaved edit preserves unknown source");

    string saveUnknown = Path.Combine(root, "unknown-save.cdb");
    data.SaveProject(ordinary, saveUnknown);
    Check(ordinary.SourceCdbGenerationIdentity == null, "13 save does not invent source");
    Check(ordinary.CurrentCdbContentIdentity == identities.Calculate(File.ReadAllBytes(saveUnknown)), "14 save advances exact current identity");
    Check(File.Exists(saveUnknown + ".wtstate"), "15 save writes one adjacent manifest");
    Check(!Directory.EnumerateFiles(root).Any(path =>
        Path.GetFileName(path).Contains(".wtstate.", StringComparison.OrdinalIgnoreCase)),
        "16 no numbered state history");

    string verifiedFile = Path.Combine(root, "verified.cdb");
    WriteCdb(verifiedFile, 12, "future-value");
    string sourceA = identities.Calculate(File.ReadAllBytes(verifiedFile));
    WriteManifest(verifiedFile, sourceA, sourceA, 2);
    ProjectModel verified = data.LoadProject(verifiedFile);
    Check(verified.SourceProvenanceStatus == SourceProvenanceStatus.Verified, "17 v2 binding verifies source");
    Check(verified.SourceCdbGenerationIdentity == sourceA, "18 v2 recovers source");
    Check(verified.CurrentCdbContentIdentity == sourceA, "19 pristine source equals current");

    string beforeSaveSource = verified.SourceCdbGenerationIdentity!;
    new ProjectMutationService().EnsurePropertyByPath(verified.Sheets.Single().Entries.Single(), "value", 20);
    string verifiedSaveAs = Path.Combine(root, "verified-save-as.cdb");
    data.SaveProject(verified, verifiedSaveAs);
    Check(verified.SourceCdbGenerationIdentity == beforeSaveSource, "20 save as preserves source");
    Check(verified.CurrentCdbContentIdentity != beforeSaveSource, "21 save advances current not source");
    Check(verified.CurrentCdbContentIdentity == identities.Calculate(File.ReadAllBytes(verifiedSaveAs)), "22 saved current coherent");
    JObject savedManifest = JObject.Parse(File.ReadAllText(verifiedSaveAs + ".wtstate"));
    Check(savedManifest.Value<int>("FormatVersion") == 2, "23 wtstate writer v2");
    Check(savedManifest.Value<string>("SourceCdbGenerationIdentity") == sourceA, "24 manifest source stable");
    Check(savedManifest.Value<string>("CurrentCdbContentIdentity") == verified.CurrentCdbContentIdentity, "25 manifest current binding");
    Check(savedManifest["HistoricalOperations"] is JArray, "26 manifest history collection");

    File.AppendAllText(verifiedSaveAs, " ");
    ProjectModel mismatch = data.LoadProject(verifiedSaveAs);
    Check(mismatch.SourceProvenanceStatus == SourceProvenanceStatus.ContentMismatch, "27 external bytes invalidate binding");
    Check(mismatch.SourceCdbGenerationIdentity == null, "28 mismatch removes restore authority");
    Check(mismatch.UpdateCompatibilityReport?.Transition == SourceGenerationTransition.ExternalContentMismatch, "29 mismatch report classification");
    Check(mismatch.UpdateCompatibilityReport?.PlayerSummary.Contains("differs", StringComparison.OrdinalIgnoreCase) == true, "30 mismatch player wording");

    string legacyFile = Path.Combine(root, "legacy.cdb");
    WriteCdb(legacyFile, 8, "legacy");
    WriteManifest(legacyFile, null, string.Empty, 1);
    string legacyBefore = File.ReadAllText(legacyFile + ".wtstate");
    ProjectModel legacy = data.LoadProject(legacyFile);
    Check(legacy.SourceProvenanceStatus == SourceProvenanceStatus.Unknown, "31 v1 provenance unknown");
    Check(legacy.RequiresGameplayStateManifestMigration, "32 v1 migration deferred");
    Check(File.ReadAllText(legacyFile + ".wtstate") == legacyBefore, "33 v1 unchanged before publication");
    data.CompletePostPublicationMigration(legacy);
    Check(JObject.Parse(File.ReadAllText(legacyFile + ".wtstate")).Value<int>("FormatVersion") == 2, "34 v1 migrates after publication");
    Check(!legacy.RequiresGameplayStateManifestMigration, "35 migration completion recorded");

    string malformedFile = Path.Combine(root, "malformed.cdb");
    WriteCdb(malformedFile, 9, "malformed");
    File.WriteAllText(malformedFile + ".wtstate", "{not-json");
    ProjectModel malformed = data.LoadProject(malformedFile);
    Check(malformed.SourceProvenanceStatus == SourceProvenanceStatus.Unknown, "36 malformed provenance unknown");
    Check(File.ReadAllText(malformedFile + ".wtstate") == "{not-json", "37 malformed sidecar preserved");
    Check(malformed.GameplayOperationStateWarnings.Count == 1, "38 malformed warning retained");

    ProjectModel trusted = CreateTrustedProject(10, "trusted");
    ProjectMutationService trustedMutation = new();
    GameplayOperationStateService trustedState = new(trustedMutation);
    GameplayPresetService preset = new(trustedMutation, trustedState);
    ProjectMutationResult presetResult = preset.Apply(trusted, ProgressionType.FishingSpeed, "Fast");
    GameplayOperationStateModel active = trusted.GameplayOperationStates.Single();
    Check(presetResult.WasModified, "39 gameplay operation mutates trusted project");
    Check(active.ProjectCompatibilityIdentity == trusted.SourceCdbGenerationIdentity, "40 active state bound to source");
    Check(trustedState.CanRestorePreviousValues(trusted, ProgressionType.FishingSpeed), "41 same-source restore eligible");
    string identityBeforeUndo = trusted.SourceCdbGenerationIdentity!;
    ProjectOperationTransactionService transaction = new();
    transaction.Rollback(presetResult);
    Check(trusted.SourceCdbGenerationIdentity == identityBeforeUndo, "42 rollback preserves identity");
    transaction.Replay(
        presetResult.PropertyRollbackRecords,
        presetResult.UpdatedProperties.Select(x => x.GetCurrentValueSnapshot()).ToArray(),
        presetResult.RemovedPropertyRollbackRecords,
        presetResult.CreatedPropertyRollbackRecords,
        presetResult.CreatedJsonPropertyRollbackRecords,
        presetResult.CreatedEntryRollbackRecords,
        presetResult.GameplayOperationStateRollbackRecords);
    Check(trusted.SourceCdbGenerationIdentity == identityBeforeUndo, "43 replay preserves identity");

    ProjectModel otherGeneration = CreateTrustedProject(20, "other");
    otherGeneration.GameplayOperationStates.Add(active.DeepClone());
    GameplayOperationStateService otherState = new();
    Check(!otherState.CanRestorePreviousValues(otherGeneration, ProgressionType.FishingSpeed), "44 cross-generation restore blocked");
    Check(active.BaselineArray[0]!["value"]!.Value<double>() == 10, "45 stale baseline retained without mutation");
    Check(otherGeneration.Sheets.Single().Entries.Single().SourceEntry!["value"]!.Value<int>() == 20, "46 coincidental/default target not overwritten");

    ModificationSnapshotModel snapshot = new ModificationSnapshotService().CreateSnapshot(trusted, "test");
    Check(snapshot.FormatVersion == 2, "47 snapshot writer v2");
    Check(snapshot.SourceCdbGenerationIdentity == trusted.SourceCdbGenerationIdentity, "48 snapshot diagnostic source");
    string snapshotJson = new ModificationSnapshotSerializationService().Serialize(snapshot);
    Check(!snapshotJson.Contains("CurrentCdbContentIdentity", StringComparison.Ordinal), "49 snapshot excludes current identity");
    ModProfileModel profile = new ModProfileService().CreateProfile(trusted, "Update Survival");
    Check(profile.FormatVersion == 3, "50 profile writer v3");
    Check(profile.SourceCdbGenerationIdentity == trusted.SourceCdbGenerationIdentity, "51 profile diagnostic source");
    string profileJson = new ModProfileSerializationService().Serialize(profile);
    Check(!profileJson.Contains("CurrentCdbContentIdentity", StringComparison.Ordinal), "52 profile excludes current identity");
    ModificationSnapshotModel legacySnapshot = new ModificationSnapshotSerializationService().Deserialize(
        "{\"FormatVersion\":1,\"CreatedAtUtc\":\"2026-01-01T00:00:00Z\",\"EditorVersion\":\"\",\"SourceFileName\":\"\",\"GameVersion\":\"\",\"Categories\":[],\"GameplayOperationStates\":[]}");
    Check(legacySnapshot.FormatVersion == 1, "53 legacy snapshot readable");

    string beforeProbe = trusted.RootDocument.ToString(Formatting.None);
    IReadOnlyList<GameplayCompatibilityAssessment> assessments =
        new GameplayCompatibilityAssessmentService().Assess(trusted);
    Check(assessments.Count >= Enum.GetValues<ProgressionType>().Length, "54 all gameplay tool areas assessed");
    Check(trusted.RootDocument.ToString(Formatting.None) == beforeProbe, "55 probes are mutation free");
    Check(assessments.Any(x => x.Status == GameplayCompatibilityStatus.MissingTarget), "56 missing target classified");
    Check(assessments.All(x => Enum.IsDefined(x.Status)), "57 typed probe outcomes");
    UpdateCompatibilityReport report = new UpdateCompatibilityReportService().Create(
        trusted, SourceGenerationTransition.ChangedSourceGeneration);
    Check(report.Transition == SourceGenerationTransition.ChangedSourceGeneration, "58 changed generation report");
    Check(report.PlayerSummary.StartsWith("Game data changed", StringComparison.Ordinal), "59 changed generation player notice");
    Check(trusted.RootDocument.ToString(Formatting.None) == beforeProbe, "60 report owns no mutation");

    string unknownSave = Path.Combine(root, "unknown-preservation.cdb");
    data.SaveProject(trusted, unknownSave);
    Check((string?)JObject.Parse(File.ReadAllText(unknownSave))["future"] == "trusted", "61 unknown JSON survives save");
    string unusable = Path.Combine(root, "unusable.cdb");
    File.WriteAllText(unusable, "{\"sheets\":[]}");
    CheckThrows<InvalidDataException>(() => data.LoadProject(unusable), "62 zero usable sheets rejected");

    ProjectModel atomic = CreateTrustedProject(1, "atomic");
    EntryModel atomicEntry = atomic.Sheets.Single().Entries.Single();
    ProjectOperationResult failed = new ProjectOperationService().Execute(
        new ThrowingContextOperation(atomicEntry), atomic);
    Check(!failed.Succeeded, "63 execution exception reports failure");
    Check(atomicEntry.SourceEntry!["value"]!.Value<int>() == 1, "64 execution exception rolls back mutation");
    Check(!atomic.IsGameplayOperationStateModified, "65 failed execution leaves state flag");
    Check(failed.MutationResult.WasModified == false, "66 failed execution returns no undo mutation");

    ProjectOperationResult validatorFailure = new ProjectOperationService(
        new RejectAllValidatorProvider(), new ProjectOperationTransactionService()).Execute(
            new SuccessfulContextOperation(atomicEntry), atomic);
    Check(!validatorFailure.Succeeded, "67 validator failure reports failure");
    Check(atomicEntry.SourceEntry!["value"]!.Value<int>() == 1, "68 validator failure rolls back");
    ProjectOperationResult success = new ProjectOperationService(
        new AcceptAllValidatorProvider(), new ProjectOperationTransactionService()).Execute(
            new SuccessfulContextOperation(atomicEntry), atomic);
    Check(success.Succeeded && success.MutationResult.PropertyRollbackRecords.Count == 1, "69 successful operation one journal");
    Check(atomicEntry.SourceEntry!["value"]!.Value<int>() == 3, "70 successful operation applied");

    string partialSave = Path.Combine(root, "partial-save.cdb");
    ProjectModel partialSaveProject = CreateTrustedProject(4, "partial-save");
    Directory.CreateDirectory(partialSave + ".wtstate");
    CheckThrows<ProjectPartialSaveException>(
        () => data.SaveProject(partialSaveProject, partialSave),
        "71 sidecar failure surfaced after CDB commit");
    Check(File.Exists(partialSave), "72 partial save leaves truthful persisted CDB");
    ProjectModel partialReopen = data.LoadProject(partialSave);
    Check(partialReopen.SourceProvenanceStatus == SourceProvenanceStatus.Unknown,
        "73 partial save cannot certify stale or missing manifest");

    GameplayOperationStateModel historyOne = active.DeepClone();
    GameplayOperationStateModel historyTwo = active.DeepClone();
    trusted.HistoricalGameplayOperationStates.Add(historyOne);
    trusted.HistoricalGameplayOperationStates.Add(historyTwo);
    string historyFile = Path.Combine(root, "history.cdb");
    data.SaveProject(trusted, historyFile);
    JArray historyArray = (JArray)JObject.Parse(
        File.ReadAllText(historyFile + ".wtstate"))["HistoricalOperations"]!;
    Check(historyArray.Count(record =>
            record!["OperationType"]!.Value<int>() ==
                (int)ProgressionType.FishingSpeed) == 1,
        "74 historical state bounded by operation type");

    ProjectModel profileSource = CreateTrustedProject(10, "profile-source");
    ProjectMutationService profileMutation = new();
    GameplayPresetService profilePreset = new(
        profileMutation,
        new GameplayOperationStateService(profileMutation));
    _ = profilePreset.Apply(profileSource, ProgressionType.FishingSpeed, "Fast");
    ModificationSnapshotModel portable =
        new ModificationSnapshotService().CreateSnapshot(profileSource, "test");
    ProjectModel sameSourceTarget = CreateTrustedProject(10, "profile-source");
    ModificationSnapshotImportResultModel sameSourceApply =
        new ModificationSnapshotWorkflowService().ApplySafely(
            sameSourceTarget,
            portable);
    Check(!sameSourceApply.HasFailures &&
          sameSourceTarget.GameplayOperationStates.Count == 1,
        "75 same-source portable gameplay state transports");

    ProjectModel crossSourceTarget = CreateTrustedProject(10, "different-source");
    ModificationSnapshotImportResultModel crossSourceApply =
        new ModificationSnapshotWorkflowService().ApplySafely(
            crossSourceTarget,
            portable);
    Check(!crossSourceApply.HasFailures, "76 cross-source ordinary profile changes continue");
    Check(crossSourceTarget.GameplayOperationStates.Count == 0,
        "77 cross-source gameplay state is not installed active");
    Check(crossSourceTarget.Sheets.Single().Entries.Single().SourceEntry!["value"]!.Value<double>() != 10,
        "78 cross-source compatible ordinary property applied");

    string partialModelFile = Path.Combine(root, "partial-model.cdb");
    File.WriteAllText(partialModelFile,
        "{\"sheets\":[42,{\"name\":\"constant\",\"lines\":[{\"id\":\"FishingDurationControl\",\"value\":10}]}]}");
    ProjectModel partialModel = data.LoadProject(partialModelFile);
    Check(partialModel.Sheets.Count == 1 && partialModel.ProjectLoadWarnings.Count == 1,
        "79 partial unsupported structures open with diagnostics");
    Check(((JArray)partialModel.RootDocument["sheets"]!)[0]!.Value<int>() == 42,
        "80 unsupported raw structures preserved");

    ProjectModel stateAtomic = CreateTrustedProject(5, "state-atomic");
    ProjectOperationResult stateFailure = new ProjectOperationService().Execute(
        new ThrowingStateContextOperation(stateAtomic), stateAtomic);
    Check(!stateFailure.Succeeded && stateAtomic.GameplayOperationStates.Count == 0,
        "81 gameplay-state mutation rolls back with project mutation");
    RollbackFailureContextOperation executionRollbackFailure =
        new(stateAtomic.Sheets.Single().Entries.Single());
    ProjectOperationResult rollbackFailure = new ProjectOperationService().Execute(
        executionRollbackFailure, stateAtomic);
    EditHistoryService rollbackFailureHistory = new();
    if (rollbackFailure.Succeeded && rollbackFailure.MutationResult.WasModified)
        rollbackFailureHistory.Record(new ProjectOperationHistoryAction(
            "invalid", rollbackFailure.MutationResult,
            new ProjectOperationTransactionService()));
    Check(!rollbackFailure.Succeeded &&
          rollbackFailure.Message?.Contains("could not be fully rolled back", StringComparison.Ordinal) == true,
        "82 rollback failure is surfaced as fatal");
    Check(executionRollbackFailure.RollbackAttemptCount == 1,
        "execution rollback failure is attempted exactly once");
    Check(!rollbackFailureHistory.CanUndo,
        "execution rollback failure leaves actual Undo history empty");

    ProjectModel untrustedSource = CreateTrustedProject(10, "untrusted-v2");
    GameplayPresetService untrustedPreset = CreatePresetService();
    _ = untrustedPreset.Apply(untrustedSource, ProgressionType.FishingSpeed, "Fast");
    GameplayOperationStateModel claimedState =
        untrustedSource.GameplayOperationStates.Single().DeepClone();
    string claimedIdentity = claimedState.ProjectCompatibilityIdentity;
    string unknownV2File = Path.Combine(root, "unknown-source-v2.cdb");
    File.WriteAllText(unknownV2File, untrustedSource.RootDocument.ToString(Formatting.None));
    string unknownV2Current = identities.Calculate(File.ReadAllBytes(unknownV2File));
    WriteManifestWithState(
        unknownV2File, null, unknownV2Current, 2, claimedState);
    ProjectModel unknownV2 = data.LoadProject(unknownV2File);
    Check(unknownV2.SourceProvenanceStatus == SourceProvenanceStatus.Unknown &&
          unknownV2.GameplayOperationStates.Count == 0,
        "83 bound v2 manifest with null source remains unknown and inactive");
    Check(unknownV2.HistoricalGameplayOperationStates.Single()
              .ProjectCompatibilityIdentity == string.Empty,
        "84 unknown-source v2 history has no actionable provenance");
    GameplayStateManifestSnapshot unknownV2Snapshot =
        data.CaptureGameplayStateForReplacement(unknownV2File);
    Check(unknownV2Snapshot.Status == PriorGameplayStateStatus.ValidManifestUnknownSource &&
          !unknownV2Snapshot.HasVerifiedSourceProvenance,
        "85 bound null-source v2 capture is explicitly unknown");
    ProjectModel staleIdentityReturn = CreateTrustedProject(
        unknownV2.Sheets.Single().Entries.Single().SourceEntry!["value"]!.Value<int>(),
        "stale-return");
    data.ApplyAuthoritativeImportIdentity(
        staleIdentityReturn, claimedIdentity, unknownV2Snapshot);
    Check(staleIdentityReturn.GameplayOperationStates.Count == 0 &&
          staleIdentityReturn.HistoricalGameplayOperationStates.All(state =>
              string.IsNullOrWhiteSpace(state.ProjectCompatibilityIdentity)),
        "86 untrusted history cannot reactivate when its stale claimed source returns");

    string manifestSourceA = identities.Calculate(
        System.Text.Encoding.UTF8.GetBytes("verified-manifest-source-a"));
    string inconsistentFile = Path.Combine(root, "inconsistent-active-source.cdb");
    File.WriteAllText(
        inconsistentFile,
        untrustedSource.RootDocument.ToString(Formatting.None));
    string inconsistentCurrent = identities.Calculate(
        File.ReadAllBytes(inconsistentFile));
    WriteManifestWithState(
        inconsistentFile,
        manifestSourceA,
        inconsistentCurrent,
        2,
        claimedState);
    ProjectModel inconsistentLoaded = data.LoadProject(inconsistentFile);
    Check(inconsistentLoaded.GameplayOperationStates.Count == 0 &&
          inconsistentLoaded.HistoricalGameplayOperationStates.Single()
              .ProjectCompatibilityIdentity == string.Empty &&
          inconsistentLoaded.GameplayOperationStateWarnings.Any(message =>
              message.Contains("inconsistent", StringComparison.OrdinalIgnoreCase)),
        "verified manifest A with active record B downgrades to warned unknown history");
    Check(!new GameplayOperationStateService().CanRestorePreviousValues(
            inconsistentLoaded, ProgressionType.FishingSpeed),
        "inconsistent active record has no Restore authority");
    GameplayStateManifestSnapshot inconsistentCapture =
        data.CaptureGameplayStateForReplacement(inconsistentFile);
    ProjectModel inconsistentLaterB = CreateTrustedProject(
        untrustedSource.Sheets.Single().Entries.Single()
            .SourceEntry!["value"]!.Value<int>(),
        "inconsistent-later-b");
    data.ApplyAuthoritativeImportIdentity(
        inconsistentLaterB,
        claimedIdentity,
        inconsistentCapture);
    Check(inconsistentLaterB.GameplayOperationStates.Count == 0 &&
          inconsistentLaterB.HistoricalGameplayOperationStates.All(state =>
              string.IsNullOrWhiteSpace(state.ProjectCompatibilityIdentity)) &&
          !new GameplayOperationStateService().CanRestorePreviousValues(
              inconsistentLaterB, ProgressionType.FishingSpeed),
        "later source B cannot reactivate inconsistent active record B");

    string populatedMismatchFile = Path.Combine(root, "populated-content-mismatch.cdb");
    File.WriteAllText(
        populatedMismatchFile,
        untrustedSource.RootDocument.ToString(Formatting.None));
    string staleCurrentIdentity = identities.Calculate(
        System.Text.Encoding.UTF8.GetBytes("stale-current-revision"));
    WriteManifestWithState(
        populatedMismatchFile,
        claimedIdentity,
        staleCurrentIdentity,
        2,
        claimedState);
    ProjectModel populatedMismatch = data.LoadProject(populatedMismatchFile);
    Check(populatedMismatch.SourceProvenanceStatus == SourceProvenanceStatus.ContentMismatch &&
          populatedMismatch.GameplayOperationStates.Count == 0 &&
          populatedMismatch.HistoricalGameplayOperationStates.Single()
              .ProjectCompatibilityIdentity == string.Empty,
        "populated content mismatch downgrades active state to unknown history");
    GameplayStateManifestSnapshot populatedMismatchCapture =
        data.CaptureGameplayStateForReplacement(populatedMismatchFile);
    ProjectModel populatedMismatchReturn = CreateTrustedProject(
        untrustedSource.Sheets.Single().Entries.Single()
            .SourceEntry!["value"]!.Value<int>(),
        "populated-mismatch-return");
    data.ApplyAuthoritativeImportIdentity(
        populatedMismatchReturn,
        claimedIdentity,
        populatedMismatchCapture);
    Check(populatedMismatchReturn.GameplayOperationStates.Count == 0 &&
          populatedMismatchReturn.HistoricalGameplayOperationStates.All(state =>
              string.IsNullOrWhiteSpace(state.ProjectCompatibilityIdentity)) &&
          !new GameplayOperationStateService().CanRestorePreviousValues(
              populatedMismatchReturn, ProgressionType.FishingSpeed),
        "later source A cannot reactivate populated content-mismatch state");

    string fabricatedLegacyFile = Path.Combine(root, "fabricated-legacy.cdb");
    File.WriteAllText(fabricatedLegacyFile, untrustedSource.RootDocument.ToString(Formatting.None));
    WriteManifestWithState(
        fabricatedLegacyFile, null, string.Empty, 1, claimedState);
    ProjectModel fabricatedLegacy = data.LoadProject(fabricatedLegacyFile);
    Check(fabricatedLegacy.HistoricalGameplayOperationStates.Single()
              .ProjectCompatibilityIdentity == string.Empty,
        "87 v1 fabricated record identity is scrubbed to unknown history");
    GameplayStateManifestSnapshot fabricatedLegacyCapture =
        data.CaptureGameplayStateForReplacement(fabricatedLegacyFile);
    ProjectModel fabricatedLegacyReturn = CreateTrustedProject(
        untrustedSource.Sheets.Single().Entries.Single()
            .SourceEntry!["value"]!.Value<int>(),
        "fabricated-legacy-return");
    data.ApplyAuthoritativeImportIdentity(
        fabricatedLegacyReturn,
        claimedIdentity,
        fabricatedLegacyCapture);
    Check(fabricatedLegacyReturn.GameplayOperationStates.Count == 0 &&
          fabricatedLegacyReturn.HistoricalGameplayOperationStates.All(state =>
              string.IsNullOrWhiteSpace(state.ProjectCompatibilityIdentity)) &&
          !new GameplayOperationStateService().CanRestorePreviousValues(
              fabricatedLegacyReturn, ProgressionType.FishingSpeed),
        "v1 fabricated identity cannot reactivate on later exact source match");

    string malformedPrior = Path.Combine(root, "malformed-prior.cdb");
    WriteCdb(malformedPrior, 10, "malformed-prior");
    File.WriteAllText(malformedPrior + ".wtstate", "{bad-json");
    GameplayStateManifestSnapshot malformedCapture =
        data.CaptureGameplayStateForReplacement(malformedPrior);
    Check(malformedCapture.Status == PriorGameplayStateStatus.MalformedManifest &&
          malformedCapture.HadPriorCanonical,
        "88 malformed prior sidecar remains distinct from no prior canonical");

    string unreadablePrior = Path.Combine(root, "unreadable-prior.cdb");
    WriteCdb(unreadablePrior, 10, "unreadable-prior");
    string unreadableSidecar = unreadablePrior + ".wtstate";
    File.WriteAllText(unreadableSidecar, "{}");
    using (FileStream locked = new(
               unreadableSidecar, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        GameplayStateManifestSnapshot unreadableCapture =
            data.CaptureGameplayStateForReplacement(unreadablePrior);
        Check(unreadableCapture.Status == PriorGameplayStateStatus.UnreadableManifest &&
              unreadableCapture.HadPriorCanonical,
            "89 unreadable prior sidecar remains distinct from no prior canonical");
    }

    ProjectModel verifiedHistorySource = CreateTrustedProject(10, "verified-history");
    GameplayPresetService verifiedHistoryPreset = CreatePresetService();
    _ = verifiedHistoryPreset.Apply(
        verifiedHistorySource, ProgressionType.FishingSpeed, "Fast");
    GameplayOperationStateModel verifiedHistoricalState =
        verifiedHistorySource.GameplayOperationStates.Single().DeepClone();
    int verifiedCurrentValue = verifiedHistorySource.Sheets.Single()
        .Entries.Single().SourceEntry!["value"]!.Value<int>();
    ProjectModel verifiedReturn = CreateTrustedProject(
        verifiedCurrentValue, "verified-history-return");
    verifiedReturn.EstablishPersistedIdentity(
        verifiedReturn.CurrentCdbContentIdentity,
        verifiedHistorySource.SourceCdbGenerationIdentity,
        SourceProvenanceStatus.Verified);
    GameplayStateManifestSnapshot verifiedHistoricalSnapshot = new(
        PriorGameplayStateStatus.ValidVerifiedManifest,
        verifiedHistorySource.SourceCdbGenerationIdentity,
        true,
        Array.Empty<GameplayOperationStateModel>(),
        new[] { verifiedHistoricalState });
    data.ApplyAuthoritativeImportIdentity(
        verifiedReturn,
        verifiedHistorySource.SourceCdbGenerationIdentity!,
        verifiedHistoricalSnapshot);
    Check(verifiedReturn.GameplayOperationStates.Count == 1 &&
          verifiedReturn.HistoricalGameplayOperationStates.Count == 0,
        "90 verified history reactivates on exact source return after full validation");

    ProjectModel portableSource = CreateTrustedProject(10, "portable-trust");
    GameplayPresetService portablePreset = CreatePresetService();
    _ = portablePreset.Apply(portableSource, ProgressionType.FishingSpeed, "Fast");
    ModificationSnapshotModel currentPortable =
        new ModificationSnapshotService().CreateSnapshot(portableSource, "test");
    ModificationSnapshotModel legacyPortable = CloneSnapshot(
        currentPortable,
        ModificationSnapshotFormat.LegacyVersion,
        currentPortable.SourceCdbGenerationIdentity);
    ProjectModel legacyPortableTarget = CreateTrustedProject(10, "portable-trust");
    ModificationSnapshotImportResultModel legacyPortableResult =
        new ModificationSnapshotWorkflowService().ApplySafely(
            legacyPortableTarget, legacyPortable);
    Check(!legacyPortableResult.HasFailures &&
          legacyPortableTarget.GameplayOperationStates.Count == 0 &&
          legacyPortableTarget.Sheets.Single().Entries.Single()
              .SourceEntry!["value"]!.Value<int>() != 10,
        "91 legacy snapshot applies ordinary changes but cannot transport matching record identity");

    ModProfileModel legacyProfile = new()
    {
        FormatVersion = 2,
        SourceCdbGenerationIdentity = currentPortable.SourceCdbGenerationIdentity,
        Metadata = CreateProfileMetadata("Legacy provenance profile"),
        Snapshot = legacyPortable
    };
    ProjectModel legacyProfileTarget = CreateTrustedProject(10, "portable-trust");
    ModificationSnapshotImportResultModel legacyProfileResult =
        new ModProfileWorkflowService().ApplyProfile(legacyProfileTarget, legacyProfile);
    Check(!legacyProfileResult.HasFailures &&
          legacyProfileTarget.GameplayOperationStates.Count == 0 &&
          legacyProfileTarget.Sheets.Single().Entries.Single()
              .SourceEntry!["value"]!.Value<int>() != 10,
        "92 legacy profile ordinary changes apply without gameplay-state transport");

    ModificationSnapshotModel mismatchedPortable = CloneSnapshot(
        currentPortable,
        ModificationSnapshotFormat.CurrentVersion,
        identities.Calculate(System.Text.Encoding.UTF8.GetBytes("different-root")));
    ProjectModel mismatchedPortableTarget = CreateTrustedProject(10, "portable-trust");
    _ = new ModificationSnapshotWorkflowService().ApplySafely(
        mismatchedPortableTarget, mismatchedPortable);
    Check(mismatchedPortableTarget.GameplayOperationStates.Count == 0,
        "93 portable root and record source identities must agree");

    ProjectModel validatorRollbackProject = CreateTrustedProject(1, "validator-rollback-once");
    EntryModel validatorRollbackEntry = validatorRollbackProject.Sheets.Single().Entries.Single();
    ValidatorRollbackFailureOperation validatorRollbackOperation =
        new(validatorRollbackEntry);
    ProjectOperationResult validatorRollbackResult = new ProjectOperationService(
        new RejectAllValidatorProvider(),
        new ProjectOperationTransactionService()).Execute(
            validatorRollbackOperation,
            validatorRollbackProject);
    Check(!validatorRollbackResult.Succeeded &&
          validatorRollbackResult.Message?.Contains(
              "could not be fully rolled back", StringComparison.Ordinal) == true,
        "94 validator rollback failure is fatal");
    Check(validatorRollbackOperation.RollbackNotificationCount == 1,
        "95 validator rollback is attempted exactly once");
    EditHistoryService failedHistory = new();
    if (validatorRollbackResult.Succeeded && validatorRollbackResult.MutationResult.WasModified)
        failedHistory.Record(new ProjectOperationHistoryAction(
            "invalid", validatorRollbackResult.MutationResult,
            new ProjectOperationTransactionService()));
    Check(!failedHistory.CanUndo,
        "96 validator rollback failure creates no Undo action");

    VerifyAddCampPreflight(Check);
    VerifyUpgradePreflight(Check);
    VerifyProductionCompatibilityAssessment(Check);
    VerifyCompatibilityPresentation(Check);

    ProjectModel historyProject = CreateTrustedProject(10, "history-cycle");
    string historySourceIdentity = historyProject.SourceCdbGenerationIdentity!;
    string historyCurrentIdentity = historyProject.CurrentCdbContentIdentity;
    ProjectOperationResult historyApply = new ProjectOperationService().Execute(
        new GameplayPresetOperation(
            CreatePresetService(), ProgressionType.FishingSpeed, "Fast"),
        historyProject);
    EditHistoryService historyService = new();
    historyService.Record(new ProjectOperationHistoryAction(
        "Fishing Speed", historyApply.MutationResult,
        new ProjectOperationTransactionService()));
    int historyAppliedValue = historyProject.Sheets.Single().Entries.Single()
        .SourceEntry!["value"]!.Value<int>();
    Check(historyApply.Succeeded && historyService.CanUndo &&
          historyProject.GameplayOperationStates.Count == 1,
        "one real contextual Apply creates one Undo action and gameplay state");
    Check(historyService.Undo() &&
          historyProject.Sheets.Single().Entries.Single().SourceEntry!["value"]!.Value<int>() == 10 &&
          historyProject.GameplayOperationStates.Count == 0 &&
          historyProject.SourceCdbGenerationIdentity == historySourceIdentity &&
          historyProject.CurrentCdbContentIdentity == historyCurrentIdentity,
        "Undo restores project and state without changing identities");
    Check(historyService.Redo() &&
          historyProject.Sheets.Single().Entries.Single().SourceEntry!["value"]!.Value<int>() == historyAppliedValue &&
          historyProject.GameplayOperationStates.Count == 1 &&
          historyService.CanUndo && !historyService.CanRedo,
        "Redo restores the one logical operation");

    VerifyUnknownDataRoundTrip(root, data, Check);

    Console.WriteLine($"ALL UPDATE SURVIVAL CHECKS PASSED ({checks})");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
}

void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL " + name);
    checks++;
}

void CheckThrows<T>(Action action, string name) where T : Exception
{
    try { action(); }
    catch (T) { Check(true, name); return; }
    Check(false, name);
}

static void WriteCdb(string path, int value, string future)
{
    JObject root = new()
    {
        ["future"] = future,
        ["sheets"] = new JArray(new JObject
        {
            ["name"] = "constant",
            ["lines"] = new JArray(new JObject
            {
                ["id"] = "FishingDurationControl",
                ["value"] = value
            })
        })
    };
    File.WriteAllText(path, root.ToString(Formatting.None));
}

static void WriteManifest(string cdb, string? source, string current, int version)
{
    JObject manifest = new()
    {
        ["FormatVersion"] = version,
        ["SourceFileName"] = Path.GetFileName(cdb),
        ["SourceCdbGenerationIdentity"] = source,
        ["CurrentCdbContentIdentity"] = current,
        ["Operations"] = new JArray(),
        ["HistoricalOperations"] = new JArray()
    };
    File.WriteAllText(cdb + ".wtstate", manifest.ToString(Formatting.Indented));
}

static ProjectModel CreateTrustedProject(int value, string future)
{
    JObject root = new()
    {
        ["future"] = future,
        ["sheets"] = new JArray(new JObject
        {
            ["name"] = "constant",
            ["lines"] = new JArray(new JObject
            {
                ["id"] = "FishingDurationControl",
                ["value"] = value
            })
        })
    };
    ProjectModel project = new()
    {
        FileName = "synthetic.cdb",
        OriginalJson = root.ToString(Formatting.None),
        RootDocument = root
    };
    project.Sheets.Add(new ProjectModelFactory().CreateSheetModel((JObject)((JArray)root["sheets"]!)[0]!));
    string identity = new CdbGenerationIdentityService().Calculate(
        System.Text.Encoding.UTF8.GetBytes(project.OriginalJson));
    project.EstablishPersistedIdentity(identity, identity, SourceProvenanceStatus.Verified);
    return project;
}

static GameplayPresetService CreatePresetService()
{
    ProjectMutationService mutation = new();
    return new GameplayPresetService(
        mutation,
        new GameplayOperationStateService(mutation));
}

static void WriteManifestWithState(
    string cdb,
    string? source,
    string current,
    int version,
    GameplayOperationStateModel state)
{
    JObject manifest = new()
    {
        ["FormatVersion"] = version,
        ["SourceFileName"] = Path.GetFileName(cdb),
        ["SourceCdbGenerationIdentity"] = source,
        ["CurrentCdbContentIdentity"] = current,
        ["Operations"] = new JArray(JObject.FromObject(state)),
        ["HistoricalOperations"] = new JArray()
    };
    File.WriteAllText(cdb + ".wtstate", manifest.ToString(Formatting.Indented));
}

static ModificationSnapshotModel CloneSnapshot(
    ModificationSnapshotModel source,
    int formatVersion,
    string? sourceIdentity)
{
    ModificationSnapshotModel clone = new()
    {
        FormatVersion = formatVersion,
        CreatedAtUtc = source.CreatedAtUtc,
        EditorVersion = source.EditorVersion,
        SourceFileName = source.SourceFileName,
        GameVersion = source.GameVersion,
        SourceCdbGenerationIdentity = sourceIdentity
    };
    clone.Categories.AddRange(source.Categories);
    clone.GameplayOperationStates.AddRange(
        source.GameplayOperationStates.Select(state => state.DeepClone()));
    return clone;
}

static ModProfileMetadataModel CreateProfileMetadata(string name)
{
    DateTimeOffset now = DateTimeOffset.UtcNow;
    return new ModProfileMetadataModel
    {
        Name = name,
        ProfileVersion = "1.0",
        CreatedAtUtc = now,
        ModifiedAtUtc = now
    };
}

static ProjectModel CreateProject(params JObject[] sheets)
{
    JObject root = new() { ["sheets"] = new JArray(sheets) };
    ProjectModel project = new()
    {
        FileName = "synthetic.cdb",
        OriginalJson = root.ToString(Formatting.None),
        RootDocument = root
    };
    ProjectModelFactory factory = new();
    foreach (JObject sheet in sheets)
        project.Sheets.Add(factory.CreateSheetModel(sheet));
    string identity = new CdbGenerationIdentityService().Calculate(
        System.Text.Encoding.UTF8.GetBytes(project.OriginalJson));
    project.EstablishPersistedIdentity(
        identity, identity, SourceProvenanceStatus.Verified);
    return project;
}

static JObject Sheet(string name, params JObject[] entries) => new()
{
    ["name"] = name,
    ["lines"] = new JArray(entries)
};

static JObject CampItem(string id, JToken? tool = null, JToken? icon = null)
{
    JObject item = new()
    {
        ["id"] = id,
        ["props"] = new JObject
        {
            ["futureMember"] = new JObject { ["keep"] = true }
        }
    };
    if (tool != null) item["tool"] = tool;
    if (icon != null) item["icon"] = icon;
    return item;
}

static JObject Craft(string item, JToken? tool = null) => new()
{
    ["item"] = item,
    ["tool"] = tool ?? "Workshop",
    ["recipe"] = new JArray(),
    ["props"] = new JObject(),
    ["learnCost"] = new JArray(),
    ["jobLevel"] = 1,
    ["group"] = "TinkererTool"
};

static ProjectModel CreateCampProject(
    IEnumerable<JObject>? extraItems = null,
    IEnumerable<JObject>? crafts = null,
    JToken? anvilTool = null,
    JToken? anvilIcon = null,
    JObject? craftSheetOverride = null)
{
    List<JObject> items =
    [
        CampItem("Anvil", anvilTool, anvilIcon),
        CampItem("ApothecaryTable")
    ];
    if (extraItems != null) items.AddRange(extraItems);
    return CreateProject(
        Sheet("item", items.ToArray()),
        craftSheetOverride ??
            Sheet("craft", (crafts ?? Array.Empty<JObject>()).ToArray()));
}

static void VerifyAddCampPreflight(Action<bool, string> check)
{
    static void RejectWithoutMutation(
        ProjectModel project,
        Action<bool, string> assertion,
        string label)
    {
        string before = project.RootDocument.ToString(Formatting.None);
        ProjectOperationResult result = new ProjectOperationService().Execute(
            new AddCampFacilitiesOperation(
                new ContentCreationService(new ProjectMutationService())),
            project);
        EditHistoryService history = new();
        if (result.Succeeded && result.MutationResult.WasModified)
            history.Record(new ProjectOperationHistoryAction(
                "invalid", result.MutationResult,
                new ProjectOperationTransactionService()));
        assertion(!result.Succeeded, $"Add Camp {label} fails preflight");
        assertion(project.RootDocument.ToString(Formatting.None) == before &&
                  project.GameplayOperationStates.Count == 0 &&
                  !result.MutationResult.WasModified &&
                  !history.CanUndo,
            $"Add Camp {label} leaves zero project/state/Undo mutation");
    }

    RejectWithoutMutation(
        CreateCampProject(new[] { CampItem("Anvil") }), check,
        "duplicate Anvil");
    RejectWithoutMutation(
        CreateCampProject(new[] { CampItem("ApothecaryTable") }), check,
        "duplicate Apothecary");
    RejectWithoutMutation(
        CreateCampProject(anvilTool: new JValue(3)), check,
        "invalid tool type");
    RejectWithoutMutation(
        CreateCampProject(anvilIcon: new JArray()), check,
        "invalid icon type");
    RejectWithoutMutation(
        CreateCampProject(crafts: new[] { Craft("Anvil", new JValue(5)) }), check,
        "malformed craft");
    RejectWithoutMutation(
        CreateCampProject(crafts: new[] { Craft("Anvil"), Craft("Anvil") }), check,
        "duplicate craft identity");
    RejectWithoutMutation(
        CreateCampProject(craftSheetOverride: new JObject
        {
            ["name"] = "craft"
        }), check,
        "craft sheet missing lines");
    RejectWithoutMutation(
        CreateCampProject(craftSheetOverride: new JObject
        {
            ["name"] = "craft",
            ["lines"] = "invalid"
        }), check,
        "craft sheet lines wrong type");

    ProjectModel valid = CreateCampProject();
    ProjectOperationResult validResult = new ProjectOperationService().Execute(
        new AddCampFacilitiesOperation(
            new ContentCreationService(new ProjectMutationService())),
        valid);
    check(validResult.Succeeded && validResult.MutationResult.WasModified,
        "Add Camp valid project still applies");
    EditHistoryService validHistory = new();
    validHistory.Record(new ProjectOperationHistoryAction(
        "Add Camp Facilities",
        validResult.MutationResult,
        new ProjectOperationTransactionService()));
    check(validHistory.CanUndo,
        "Add Camp valid creation records exactly one logical Undo action");
    check(valid.RootDocument.SelectToken(
              "sheets[0].lines[0].props.futureMember.keep")?.Value<bool>() == true,
        "Add Camp valid deep merge preserves unknown members");
    check(validHistory.Undo() && !validHistory.CanUndo,
        "Add Camp valid creation is removed by one Undo action");
}

static GameplayCompatibilityAssessment AssessTool(
    ProjectModel project,
    string toolName)
{
    return new GameplayCompatibilityAssessmentService()
        .Assess(project)
        .Single(item => string.Equals(
            item.ToolName, toolName, StringComparison.Ordinal));
}

static void VerifyProductionCompatibilityAssessment(
    Action<bool, string> check)
{
    static void CheckTool(
        ProjectModel project,
        string toolName,
        GameplayCompatibilityStatus expected,
        Action<bool, string> assertion,
        string label)
    {
        string before = project.RootDocument.ToString(Formatting.None);
        int stateCount = project.GameplayOperationStates.Count;
        GameplayCompatibilityAssessment assessment = AssessTool(project, toolName);
        assertion(assessment.Status == expected,
            $"production compatibility classifies {label} as {expected}");
        assertion(project.RootDocument.ToString(Formatting.None) == before &&
                  project.GameplayOperationStates.Count == stateCount,
            $"production compatibility probe for {label} is mutation neutral");
    }

    CheckTool(
        CreateProject(
            Sheet("item", CampItem("ApothecaryTable")),
            Sheet("craft")),
        "Add Camp Facilities",
        GameplayCompatibilityStatus.MissingTarget,
        check,
        "missing Anvil");
    CheckTool(
        CreateCampProject(new[] { CampItem("Anvil") }),
        "Add Camp Facilities",
        GameplayCompatibilityStatus.AmbiguousTarget,
        check,
        "duplicate Anvil");
    CheckTool(
        CreateCampProject(anvilTool: new JValue(3)),
        "Add Camp Facilities",
        GameplayCompatibilityStatus.StructureChanged,
        check,
        "invalid camp tool");
    CheckTool(
        CreateCampProject(craftSheetOverride: new JObject
        {
            ["name"] = "craft"
        }),
        "Add Camp Facilities",
        GameplayCompatibilityStatus.StructureChanged,
        check,
        "missing craft lines");
    CheckTool(
        CreateCampProject(craftSheetOverride: new JObject
        {
            ["name"] = "craft",
            ["lines"] = "invalid"
        }),
        "Add Camp Facilities",
        GameplayCompatibilityStatus.StructureChanged,
        check,
        "wrong-type craft lines");
    CheckTool(
        CreateCampProject(),
        "Add Camp Facilities",
        GameplayCompatibilityStatus.Compatible,
        check,
        "valid camp creation substrate");

    string first = UpgradeAllEquipmentTargetCatalog.EntryIds.First();
    CheckTool(
        CreateUpgradeProject(missingId: first),
        "Upgrade All Equipment",
        GameplayCompatibilityStatus.PartiallyOutdated,
        check,
        "missing approved equipment");
    CheckTool(
        CreateUpgradeProject(duplicateId: first),
        "Upgrade All Equipment",
        GameplayCompatibilityStatus.AmbiguousTarget,
        check,
        "duplicate approved equipment");
    CheckTool(
        CreateUpgradeProject(malformedPropsId: first),
        "Upgrade All Equipment",
        GameplayCompatibilityStatus.StructureChanged,
        check,
        "malformed equipment props");
    CheckTool(
        CreateUpgradeProject(malformedFlagsId: first),
        "Upgrade All Equipment",
        GameplayCompatibilityStatus.TypeChanged,
        check,
        "malformed equipment flags");
    CheckTool(
        CreateUpgradeProject(includeUnknown: true),
        "Upgrade All Equipment",
        GameplayCompatibilityStatus.Compatible,
        check,
        "unknown equipment outside approved catalog");
    CheckTool(
        CreateProject(Sheet("trait")),
        "Random Trait Exclusions",
        GameplayCompatibilityStatus.UnsupportedStructure,
        check,
        "unsupported standard random-trait structure");

    ProjectModel assessmentProject = CreateCampProject();
    string assessmentBefore = assessmentProject.RootDocument.ToString(Formatting.None);
    GameplayCompatibilityAssessmentService failingAssessment = new(
        new ProjectMutationService(),
        new[]
        {
            new GameplayCompatibilityProbe(
                "Unexpected production probe",
                _ => throw new ApplicationException("technical probe failure"))
        });
    UpdateCompatibilityReport report = new UpdateCompatibilityReportService(
        failingAssessment).Create(
        assessmentProject,
        SourceGenerationTransition.ChangedSourceGeneration);
    assessmentProject.SetUpdateCompatibilityReport(report);
    GameplayCompatibilityAssessment failedProbe = report.GameplayTools.Single(item =>
        item.ToolName == "Unexpected production probe");
    check(failedProbe.Status == GameplayCompatibilityStatus.AssessmentFailed &&
          failedProbe.Message ==
              "Compatibility could not be checked for this gameplay tool." &&
          ReferenceEquals(assessmentProject.UpdateCompatibilityReport, report),
        "unexpected production probe becomes AssessmentFailed and report publication continues");
    check(assessmentProject.RootDocument.ToString(Formatting.None) == assessmentBefore &&
          assessmentProject.GameplayOperationStates.Count == 0,
        "AssessmentFailed report composition is mutation neutral");
}

static void VerifyCompatibilityPresentation(
    Action<bool, string> check)
{
    GameplayCompatibilityAssessment compatible = new(
        "Compatible Feature",
        GameplayCompatibilityStatus.Compatible,
        "Available for this game-data version.");
    GameplayCompatibilityAssessment partial = new(
        "Outdated Feature",
        GameplayCompatibilityStatus.PartiallyOutdated,
        "This feature may need updating.");
    GameplayCompatibilityAssessment failed = new(
        "Uncertain Feature",
        GameplayCompatibilityStatus.AssessmentFailed,
        "Compatibility could not be checked for this gameplay tool.");

    UpdateCompatibilityReport clear = new(
        SourceGenerationTransition.SameSourceGeneration,
        0,
        0,
        0,
        new[] { compatible },
        Array.Empty<string>(),
        "Compatibility information is available for this project.",
        string.Empty);
    check(clear.GameplayTools.Count == 1 &&
          clear.ProblematicGameplayTools.Count == 0,
        "compatible assessments remain internal while player issue list is empty");
    check(clear.HasNoIssues && clear.IssueCount == 0 &&
          clear.ResultSummary == "No compatibility issues detected." &&
          clear.ResultDetail.Contains("All supported gameplay features", StringComparison.Ordinal),
        "zero issues produce concise player-facing all-clear state");

    UpdateCompatibilityReport oneIssue = clear with
    {
        GameplayTools = new[] { compatible, partial }
    };
    check(oneIssue.IssueCount == 1 &&
          oneIssue.ProblematicGameplayTools.Single() == partial &&
          oneIssue.ResultSummary == "1 issue found.",
        "one issue shows only its affected gameplay feature");
    check(partial.DisplayStatus == "May need updating",
        "PartiallyOutdated uses player-facing status wording");

    UpdateCompatibilityReport multipleIssues = clear with
    {
        GameplayTools = new[] { compatible, partial, failed }
    };
    check(multipleIssues.IssueCount == 2 &&
          multipleIssues.ProblematicGameplayTools.SequenceEqual(
              new[] { partial, failed }) &&
          multipleIssues.ResultSummary == "2 issues found.",
        "multiple issues exclude compatible rows and retain affected features");
    check(failed.DisplayStatus == "Check could not complete" &&
          !failed.Message.Contains("technical", StringComparison.OrdinalIgnoreCase),
        "AssessmentFailed remains visible with player-safe wording");

    UpdateCompatibilityReport warningOnly = clear with
    {
        ProjectWarnings = new[]
        {
            "Previous restore information could not be verified."
        }
    };
    check(warningOnly.HasProjectWarnings &&
          warningOnly.IssueCount == 1 &&
          !warningOnly.HasNoIssues,
        "relevant non-restorable prior-state warning participates in issue summary");

    ProjectModel currentProject = CreateCampProject();
    string before = currentProject.RootDocument.ToString(Formatting.None);
    string currentIdentity = currentProject.CurrentCdbContentIdentity;
    string? sourceIdentity = currentProject.SourceCdbGenerationIdentity;
    int stateCount = currentProject.GameplayOperationStates.Count;
    EditHistoryService history = new();
    UpdateCompatibilityReportService reportService = new();
    UpdateCompatibilityReport first = reportService.Create(
        currentProject,
        SourceGenerationTransition.SameSourceGeneration);
    currentProject.Sheets.Single(sheet => sheet.Name == "item")
        .Entries.Single(entry => entry.Id == "Anvil")
        .SourceEntry!["tool"] = new JValue(5);
    UpdateCompatibilityReport second = reportService.Create(
        currentProject,
        SourceGenerationTransition.SameSourceGeneration);
    check(!ReferenceEquals(first, second) &&
          second.ProblematicGameplayTools.Any(item =>
              item.ToolName == "Add Camp Facilities" &&
              item.Status == GameplayCompatibilityStatus.StructureChanged),
        "repeated assessment reflects current in-memory project content");
    check(currentProject.GameplayOperationStates.Count == stateCount &&
          currentProject.CurrentCdbContentIdentity == currentIdentity &&
          currentProject.SourceCdbGenerationIdentity == sourceIdentity &&
          !history.CanUndo,
        "compatibility assessment creates no gameplay state, identity change, or Undo history");
    currentProject.Sheets.Single(sheet => sheet.Name == "item")
        .Entries.Single(entry => entry.Id == "Anvil")
        .SourceEntry!.Property("tool")!.Remove();
    check(currentProject.RootDocument.ToString(Formatting.None) == before,
        "compatibility presentation fixture restores its direct test-only setup change");
}

static ProjectModel CreateUpgradeProject(
    string? missingId = null,
    string? duplicateId = null,
    string? malformedPropsId = null,
    string? malformedFlagsId = null,
    bool includeUnknown = false)
{
    List<JObject> entries = UpgradeAllEquipmentTargetCatalog.EntryIds
        .Where(id => !string.Equals(id, missingId, StringComparison.Ordinal))
        .Select(id => new JObject
        {
            ["id"] = id,
            ["props"] = string.Equals(id, malformedPropsId, StringComparison.Ordinal)
                ? new JValue("invalid")
                : new JObject
                {
                    ["flags"] = string.Equals(id, malformedFlagsId, StringComparison.Ordinal)
                        ? new JValue("invalid")
                        : new JValue(0)
                }
        }).ToList();
    if (duplicateId != null)
    {
        entries.Add(new JObject
        {
            ["id"] = duplicateId,
            ["props"] = new JObject { ["flags"] = 0 }
        });
    }
    if (includeUnknown)
    {
        entries.Add(new JObject
        {
            ["id"] = "FutureEquipmentOutsideCatalog",
            ["props"] = new JObject { ["flags"] = 0, ["future"] = true }
        });
    }
    return CreateProject(Sheet("item", entries.ToArray()));
}

static void VerifyUpgradePreflight(Action<bool, string> check)
{
    string first = UpgradeAllEquipmentTargetCatalog.EntryIds.First();
    string second = UpgradeAllEquipmentTargetCatalog.EntryIds.Skip(1).First();

    static void RejectWithoutMutation(
        ProjectModel project,
        Action<bool, string> assertion,
        string label)
    {
        string before = project.RootDocument.ToString(Formatting.None);
        ProjectOperationResult result = new ProjectOperationService().Execute(
            new UpgradeAllEquipmentOperation(
                new ContentCreationService(new ProjectMutationService())),
            project);
        assertion(!result.Succeeded, $"Upgrade {label} fails preflight");
        assertion(project.RootDocument.ToString(Formatting.None) == before &&
                  project.GameplayOperationStates.Count == 0 &&
                  !result.MutationResult.WasModified,
            $"Upgrade {label} leaves zero project/state/Undo mutation");
    }

    RejectWithoutMutation(CreateUpgradeProject(missingId: first), check,
        "missing exact ID");
    RejectWithoutMutation(CreateUpgradeProject(duplicateId: first), check,
        "duplicate exact ID");
    RejectWithoutMutation(
        CreateUpgradeProject(missingId: first, duplicateId: second), check,
        "duplicate plus missing with equal total count");
    RejectWithoutMutation(CreateUpgradeProject(malformedPropsId: first), check,
        "malformed props");
    RejectWithoutMutation(CreateUpgradeProject(malformedFlagsId: first), check,
        "malformed flags");

    ProjectModel valid = CreateUpgradeProject(includeUnknown: true);
    ProjectOperationResult validResult = new ProjectOperationService().Execute(
        new UpgradeAllEquipmentOperation(
            new ContentCreationService(new ProjectMutationService())),
        valid);
    check(validResult.Succeeded && validResult.MutationResult.WasModified,
        "Upgrade valid full exact-ID catalog applies");
    JObject unknown = ((JArray)((JObject)((JArray)valid.RootDocument["sheets"]!)[0]!)["lines"]!)
        .OfType<JObject>().Single(entry =>
            entry.Value<string>("id") == "FutureEquipmentOutsideCatalog");
    check(unknown.SelectToken("props.flags")!.Value<int>() == 0 &&
          unknown.SelectToken("props.future")!.Value<bool>(),
        "Upgrade preserves unknown equipment outside static scope");
}

static void VerifyUnknownDataRoundTrip(
    string root,
    JsonDataService data,
    Action<bool, string> check)
{
    string input = Path.Combine(root, "future-roundtrip.cdb");
    string output = Path.Combine(root, "future-roundtrip-saved.cdb");
    JObject future = new()
    {
        ["futureRoot"] = new JObject { ["enabled"] = true },
        ["sheets"] = new JArray(
            new JValue(42),
            new JObject
            {
                ["name"] = "constant",
                ["futureSheet"] = new JArray(1, 2, 3),
                ["lines"] = new JArray(
                    new JObject
                    {
                        ["id"] = "FishingDurationControl",
                        ["value"] = 10,
                        ["futureScalar"] = "keep",
                        ["futureObject"] = new JObject
                        {
                            ["nested"] = new JArray(
                                new JObject { ["x"] = 1 }, 2, 3)
                        }
                    },
                    new JObject
                    {
                        ["id"] = "FutureUnknownEntry",
                        ["payload"] = new JArray("a", "b")
                    })
            })
    };
    File.WriteAllText(input, future.ToString(Formatting.None));
    ProjectModel project = data.LoadProject(input);
    _ = new ProjectMutationService().EnsurePropertyByPath(
        project.Sheets.Single().Entries.First(entry =>
            entry.Id == "FishingDurationControl"),
        "value",
        new JValue(11));
    data.SaveProject(project, output);
    JObject saved = JObject.Parse(File.ReadAllText(output));
    check(JToken.DeepEquals(saved["futureRoot"], future["futureRoot"]),
        "unknown root member round trips");
    check(JToken.DeepEquals(saved.SelectToken("sheets[1].futureSheet"),
                            future.SelectToken("sheets[1].futureSheet")),
        "unknown sheet member and array round trip");
    check(JToken.DeepEquals(saved.SelectToken("sheets[1].lines[0].futureObject"),
                            future.SelectToken("sheets[1].lines[0].futureObject")) &&
          saved.SelectToken("sheets[1].lines[0].futureScalar")?.Value<string>() == "keep",
        "unknown scalar and nested object round trip");
    check(JToken.DeepEquals(saved.SelectToken("sheets[1].lines[1]"),
                            future.SelectToken("sheets[1].lines[1]")),
        "unknown entry round trips");
    check(saved.SelectToken("sheets[0]")?.Value<int>() == 42 &&
          saved.SelectToken("sheets[1].lines[0].value")?.Value<int>() == 11,
        "unsupported raw structure survives beside supported edit");
}

sealed class ThrowingContextOperation : IProjectOperation, IContextualProjectOperation
{
    private readonly EntryModel entry;
    public ThrowingContextOperation(EntryModel entry) => this.entry = entry;
    public string Name => "Throwing operation";
    public string Description => Name;
    public bool CanExecute(ProjectModel project) => true;
    public ProjectOperationResult Execute(ProjectModel project) => throw new NotSupportedException();
    public void Preflight(ProjectModel project) { }
    public ProjectOperationResult Execute(ProjectModel project, ProjectOperationExecutionContext context)
    {
        context.Record(new ProjectMutationService().EnsurePropertyByPath(entry, "value", 2));
        throw new InvalidOperationException("forced late failure");
    }
}

sealed class SuccessfulContextOperation : IProjectOperation, IContextualProjectOperation
{
    private readonly EntryModel entry;
    public SuccessfulContextOperation(EntryModel entry) => this.entry = entry;
    public string Name => "Successful operation";
    public string Description => Name;
    public bool CanExecute(ProjectModel project) => true;
    public ProjectOperationResult Execute(ProjectModel project) => throw new NotSupportedException();
    public void Preflight(ProjectModel project) { }
    public ProjectOperationResult Execute(ProjectModel project, ProjectOperationExecutionContext context)
    {
        context.Record(new ProjectMutationService().EnsurePropertyByPath(entry, "value", 3));
        return ProjectOperationResult.Success(context.MutationResult);
    }
}

sealed class ThrowingStateContextOperation : IProjectOperation, IContextualProjectOperation
{
    private readonly ProjectModel project;
    public ThrowingStateContextOperation(ProjectModel project) => this.project = project;
    public string Name => "Throwing state operation";
    public string Description => Name;
    public bool CanExecute(ProjectModel candidate) => true;
    public ProjectOperationResult Execute(ProjectModel candidate) => throw new NotSupportedException();
    public void Preflight(ProjectModel candidate) { }
    public ProjectOperationResult Execute(ProjectModel candidate, ProjectOperationExecutionContext context)
    {
        GameplayOperationStateModel replacement = new()
        {
            OperationType = ProgressionType.FishingSpeed,
            TargetSheet = "constant",
            TargetEntry = "FishingDurationControl",
            TargetPath = "value",
            BaselineArray = new JArray(new JObject { ["value"] = 5 }),
            BaselineFingerprint = "baseline",
            ExpectedCurrentFingerprint = "current",
            ElementCount = 1,
            ElementShapeFingerprint = "shape",
            GameplaySettings = new JObject { ["preset"] = "Fast" }
        };
        context.MutationResult.AddGameplayOperationState(
            project,
            null,
            replacement,
            project.IsGameplayOperationStateModified);
        new GameplayOperationStateService().ReplaceState(project, replacement);
        throw new InvalidOperationException("forced state failure");
    }
}

sealed class RollbackFailureContextOperation : IProjectOperation, IContextualProjectOperation
{
    private readonly EntryModel entry;

    public RollbackFailureContextOperation(EntryModel entry) => this.entry = entry;

    public int RollbackAttemptCount { get; private set; }

    public string Name => "Rollback failure operation";
    public string Description => Name;
    public bool CanExecute(ProjectModel project) => true;
    public ProjectOperationResult Execute(ProjectModel project) => throw new NotSupportedException();
    public void Preflight(ProjectModel project) { }
    public ProjectOperationResult Execute(ProjectModel project, ProjectOperationExecutionContext context)
    {
        context.Record(new ProjectMutationService().EnsurePropertyByPath(
            entry, "value", new JValue(99)));
        PropertyModel property = entry.Properties.Single(candidate =>
            candidate.EffectivePropertyPath == "value");
        property.ValueChanged += OnRollbackValueChanged;
        throw new InvalidOperationException("forced failure with invalid journal");
    }

    private void OnRollbackValueChanged(
        object? sender,
        PropertyValueChangedEventArgs e)
    {
        RollbackAttemptCount++;
        throw new InvalidOperationException("forced execution rollback failure");
    }
}

sealed class ValidatorRollbackFailureOperation : IProjectOperation, IContextualProjectOperation
{
    private readonly EntryModel entry;

    public ValidatorRollbackFailureOperation(EntryModel entry) => this.entry = entry;

    public int RollbackNotificationCount { get; private set; }

    public string Name => "Validator rollback failure operation";
    public string Description => Name;
    public bool CanExecute(ProjectModel project) => true;
    public ProjectOperationResult Execute(ProjectModel project) => throw new NotSupportedException();
    public void Preflight(ProjectModel project) { }

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ProjectMutationResult mutation = new ProjectMutationService()
            .EnsurePropertyByPath(entry, "value", new JValue(2));
        context.Record(mutation);
        PropertyModel property = entry.Properties.Single(candidate =>
            candidate.EffectivePropertyPath == "value");
        property.ValueChanged += OnRollbackValueChanged;
        return ProjectOperationResult.Success(context.MutationResult);
    }

    private void OnRollbackValueChanged(
        object? sender,
        PropertyValueChangedEventArgs e)
    {
        RollbackNotificationCount++;
        throw new InvalidOperationException("forced validator rollback failure");
    }
}

sealed class RejectAllValidatorProvider : IOperationValidatorProvider
{
    public OperationValidationResult Validate(IProjectOperation operation, ProjectModel project, ProjectMutationResult mutationResult) =>
        OperationValidationResult.Failure("forced validation failure");
}

sealed class AcceptAllValidatorProvider : IOperationValidatorProvider
{
    public OperationValidationResult Validate(IProjectOperation operation, ProjectModel project, ProjectMutationResult mutationResult) =>
        OperationValidationResult.Success();
}
