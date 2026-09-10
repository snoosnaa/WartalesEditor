using System.IO;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;

int checks = 0;
string root = Path.Combine(
    Path.GetTempPath(),
    $"wartales-profile-impact-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
ModProfileSerializationService serializer = new();

try
{
    string sourcePath = Path.Combine(root, "source.cdb");
    byte[] pristineBytes = Encoding.UTF8.GetBytes(CreateJson(1));
    File.WriteAllBytes(sourcePath, pristineBytes);
    string sourceIdentity = new CdbGenerationIdentityService()
        .Calculate(pristineBytes);

    ProjectModel source = LoadVerified(sourcePath, sourceIdentity);
    _ = new ProjectMutationService().EnsurePropertyByPath(
        Entry(source),
        "value",
        new JValue(2));

    ModProfileWorkflowService workflow = new();
    ModProfileModel profile = workflow.CreateProfile(
        source,
        "Impact",
        "description",
        "author",
        "1.0",
        "test");

    Check(profile.FormatVersion == 5, "new profiles use format 5");
    Check(profile.ImpactManifest != null, "exact persisted baseline establishes manifest");
    Check(profile.ImpactManifest!.TotalCount == 1, "raw update counts one canonical leaf");
    Check(profile.ImpactManifest.Leaves.Single().MutationKind ==
          ProfileImpactMutationKind.Updated, "updated mutation kind is persisted");
    Check(profile.ImpactManifest.Leaves.Single().SheetName == "constant" &&
          profile.ImpactManifest.Leaves.Single().EntryId == "Value" &&
          profile.ImpactManifest.Leaves.Single().PropertyPath == "value",
        "leaf identity is canonical");

    string json = serializer.Serialize(profile);
    ModProfileModel roundTrip = serializer.Deserialize(json);
    Check(roundTrip.ImpactManifest != null, "valid manifest round-trips");
    string roundTripJson = serializer.Serialize(roundTrip);
    Check(roundTripJson == json, "format-5 serialization is deterministic");

    string contentBefore = new ProfileGameplayContentIdentityService()
        .Calculate(profile);
    ModProfileModel metadataOnly = new ModProfileService().UpdateMetadata(
        profile,
        description: "changed metadata");
    Check(new ProfileGameplayContentIdentityService().Calculate(metadataOnly) ==
          contentBefore, "metadata is excluded from gameplay identity");
    Check(metadataOnly.ImpactManifest?.EvidenceFingerprint ==
          profile.ImpactManifest.EvidenceFingerprint,
        "metadata update preserves manifest authority");

    JObject malformedRoot = JObject.Parse(json);
    malformedRoot[nameof(ModProfileModel.ImpactManifest)] = "invalid";
    ModProfileModel malformed = serializer.Deserialize(malformedRoot.ToString());
    Check(malformed.ImpactManifest == null, "malformed optional manifest is isolated");
    Check(malformed.Snapshot.Categories.Count == profile.Snapshot.Categories.Count,
        "malformed manifest does not poison core profile");

    foreach (string alias in new[] { "impactmanifest", "IMPACTMANIFEST" })
    {
        string aliasedJson = json.Replace(
            $"\"{nameof(ModProfileModel.ImpactManifest)}\"",
            $"\"{alias}\"",
            StringComparison.Ordinal);
        ModProfileModel aliased = serializer.Deserialize(aliasedJson);
        Check(aliased.ImpactManifest != null,
            $"single {alias} alias is isolated and validated");
        string canonicalized = serializer.Serialize(aliased);
        Check(CountRootManifestAliases(canonicalized) == 1 &&
              JObject.Parse(canonicalized).Property(
                  nameof(ModProfileModel.ImpactManifest)) != null,
            $"single {alias} alias serializes once with canonical casing");
    }

    string validManifestJson = JObject.Parse(json)
        [nameof(ModProfileModel.ImpactManifest)]!.ToString(
            Newtonsoft.Json.Formatting.None);
    foreach ((string first, string second, string secondValue, string label) in
             new[]
             {
                 ("ImpactManifest", "ImpactManifest", validManifestJson,
                     "duplicate exact-name manifests"),
                 ("ImpactManifest", "impactmanifest", validManifestJson,
                     "canonical and lowercase manifests"),
                 ("impactmanifest", "IMPACTMANIFEST", validManifestJson,
                     "lowercase and uppercase manifests"),
                 ("ImpactManifest", "impactmanifest", "\"invalid\"",
                     "valid and malformed duplicate manifests"),
                 ("impactmanifest", "IMPACTMANIFEST", "null",
                     "two invalid duplicate manifests"),
                 ("ImpactManifest", "impactmanifest",
                     "{\"FormatVersion\":99}",
                     "duplicate containing unsupported format"),
                 ("ImpactManifest", "IMPACTMANIFEST", "[]",
                     "duplicate containing non-object JSON")
             })
    {
        string duplicateJson = AddRootManifestAlias(
            json, first, second, secondValue);
        ModProfileModel duplicate = serializer.Deserialize(duplicateJson);
        Check(duplicate.ImpactManifest == null,
            $"{label} make optional authority unavailable");
        Check(Entry(LoadAndApplyCore(duplicate, sourcePath, sourceIdentity))
                  .SourceEntry!["value"]!.Value<int>() == 2,
            $"{label} do not block core Apply");
        string rewrittenDuplicate = serializer.Serialize(duplicate);
        Check(JObject.Parse(rewrittenDuplicate).Properties().Count(property =>
                  string.Equals(property.Name,
                      nameof(ModProfileModel.ImpactManifest),
                      StringComparison.OrdinalIgnoreCase)) == 0,
            $"{label} cannot survive canonical rewrite");
    }

    JObject invalidCoreRoot = JObject.Parse(json);
    invalidCoreRoot[nameof(ModProfileModel.Metadata)] = JValue.CreateNull();
    CheckThrows(() => serializer.Deserialize(invalidCoreRoot.ToString()),
        "invalid core profile still fails");

    CheckTamper(json, manifest => manifest["TotalCount"] = 9,
        "count tampering becomes unavailable");
    CheckTamper(json, manifest => manifest["EvidenceFingerprint"] =
        new string('0', 64), "evidence tampering becomes unavailable");
    CheckTamper(json, manifest => manifest["ImpactSemanticsVersion"] = 99,
        "unsupported semantics becomes unavailable");
    CheckTamper(json, manifest => manifest["FormatVersion"] = 99,
        "unsupported manifest format becomes unavailable");
    CheckTamper(json, manifest => manifest["SourceCdbGenerationIdentity"] =
        new string('a', 64), "source mismatch becomes unavailable");
    CheckTamper(json, manifest => manifest["ProfileGameplayContentIdentity"] =
        new string('b', 64), "gameplay mismatch becomes unavailable");
    CheckTamper(json, manifest =>
    {
        JArray leaves = (JArray)manifest["Leaves"]!;
        leaves.Add(leaves[0]!.DeepClone());
        manifest["TotalCount"] = 2;
    }, "duplicate leaves become unavailable");
    CheckTamper(json, manifest =>
        ((JObject)((JArray)manifest["Leaves"]!)[0]!)["MutationKind"] =
            "Unknown", "invalid mutation kind becomes unavailable");
    CheckTamper(json, manifest =>
        ((JObject)((JArray)manifest["Leaves"]!)[0]!)["SheetName"] = "",
        "malformed leaf identity becomes unavailable");
    CheckTamper(json, manifest =>
        ((JObject)((JArray)manifest["Leaves"]!)[0]!)["EntryId"] =
            "bad\u001fid",
        "reserved leaf identity separator becomes unavailable");
    CheckTamper(json, manifest => manifest["EstablishedAtUtc"] =
        DateTimeOffset.MinValue, "missing establishment timestamp becomes unavailable");
    CheckTamper(json, manifest => manifest["EstablishedByEditorVersion"] = "",
        "missing establishment editor version becomes unavailable");
    CheckTamper(json, manifest =>
        ((JObject)((JArray)manifest["Leaves"]!)[0]!)["MutationKind"] =
            "Created", "add-leaf tampering becomes unavailable");
    CheckTamper(json, manifest =>
        ((JObject)((JArray)manifest["Leaves"]!)[0]!)["MutationKind"] =
            "Removed", "remove-leaf tampering becomes unavailable");
    CheckTamper(json, manifest =>
        ((JObject)((JArray)manifest["Leaves"]!)[0]!)["PropertyPath"] =
            "other", "path tampering becomes unavailable");

    ProfileImpactManifestValidationService directValidator = new();
    ProfileImpactManifestModel validManifest = profile.ImpactManifest!;
    ProfileImpactManifestModel unorderedManifest = CloneManifestWithLeaves(
        validManifest,
        new[]
        {
            new ProfileImpactLeafModel
            {
                SheetName = "constant",
                EntryId = "Zed",
                PropertyPath = "value",
                MutationKind = ProfileImpactMutationKind.Updated
            },
            validManifest.Leaves.Single().DeepClone()
        },
        directValidator);
    Check(!directValidator.TryValidate(
              profile, unorderedManifest, out string unorderedError) &&
          unorderedError.Contains("unordered", StringComparison.Ordinal),
        "two individually valid leaves in noncanonical order are rejected structurally");

    ProfileImpactManifestModel blankPathManifest = CloneManifestWithLeaves(
        validManifest,
        new[]
        {
            new ProfileImpactLeafModel
            {
                SheetName = "constant",
                EntryId = "Value",
                PropertyPath = "",
                MutationKind = ProfileImpactMutationKind.Updated
            }
        },
        directValidator);
    Check(!directValidator.TryValidate(
              profile, blankPathManifest, out string blankPathError) &&
          blankPathError.Contains("invalid leaf", StringComparison.Ordinal),
        "blank PropertyPath is rejected by structural identity validation");

    ProjectModel applied = LoadVerified(sourcePath, sourceIdentity);
    _ = workflow.ApplyProfile(applied, profile);
    Check(Entry(applied).SourceEntry!["value"]!.Value<int>() == 2,
        "core profile Apply remains unchanged");

    string libraryPath = Path.Combine(root, "profiles");
    ModProfileLibraryService library = new(
        new ModProfileLibraryPathService(libraryPath),
        serializer);
    ModProfileSummaryModel added = library.AddProfile(profile);
    Check(added.IsEffectiveChangeCountExact &&
          added.EffectiveChangeCount == 1 &&
          added.ChangeSummaryText == "1 change",
        "Profile Manager summary uses manifest count");
    ModProfileSummaryModel duplicated = library.DuplicateProfile(
        added, "Impact Copy", "copy", "author", "1.0");
    Check(duplicated.IsEffectiveChangeCountExact &&
          duplicated.EffectiveChangeCount == 1,
        "profile duplication preserves valid historical authority");
    string importSource = Path.Combine(root, "import-source.wtprofile");
    serializer.Save(profile, importSource);
    ModProfileSummaryModel imported = library.ImportProfile(importSource);
    Check(imported.IsEffectiveChangeCountExact &&
          imported.EffectiveChangeCount == 1,
        "profile import preserves a validated manifest");
    ModProfileSummaryModel appliedSummary = library.GetProfiles(applied)
        .Single(summary => summary.FilePath == added.FilePath);
    Check(appliedSummary.EffectiveChangeCount == 1 &&
          appliedSummary.IsEffectiveChangeCountExact,
        "fully applied target does not redefine historical count");
    Entry(applied).SourceEntry!["value"] = 9;
    ModProfileSummaryModel divergentSummary = library.GetProfiles(applied)
        .Single(summary => summary.FilePath == added.FilePath);
    Check(divergentSummary.EffectiveChangeCount == 1 &&
          divergentSummary.IsEffectiveChangeCountExact,
        "partially divergent target does not redefine historical count");

    ProjectModel fullyCompatibleTarget = LoadVerified(sourcePath, sourceIdentity);
    int fullyCompatibleCount = new ProfileEffectiveChangeCountService()
        .Calculate(fullyCompatibleTarget, profile, out bool fullyCompatibleExact);
    Check(fullyCompatibleExact && fullyCompatibleCount == 1,
        "target-context fully compatible evaluation is exact");
    ProjectModel conflictingTarget = CreateDetached(CreateJson(9),
        "conflicting.cdb");
    _ = new ProfileEffectiveChangeCountService().Calculate(
        conflictingTarget, profile, out bool conflictingExact);
    Check(!conflictingExact,
        "target-context conflicting raw property is not exact");

    ModProfileModel invalidRawProfile = serializer.Deserialize(json);
    ModificationSnapshotPropertyModel validRaw = invalidRawProfile.Snapshot
        .Categories.SelectMany(category => category.Settings)
        .SelectMany(setting => setting.Properties)
        .Single();
    invalidRawProfile.Snapshot.Categories.Single().Settings.Single()
        .Properties[0] = new ModificationSnapshotPropertyModel
        {
            Name = validRaw.Name,
            PropertyPath = validRaw.PropertyPath,
            OriginalPropertyExisted = validRaw.OriginalPropertyExisted,
            OriginalValue = validRaw.OriginalValue.DeepClone(),
            CurrentValue = validRaw.OriginalValue.DeepClone()
        };
    _ = new ProfileEffectiveChangeCountService().Calculate(
        fullyCompatibleTarget, invalidRawProfile, out bool invalidRawExact);
    Check(!invalidRawExact,
        "target-context invalid raw value is not exact");

    ModProfileModel unsupportedOperationProfile = serializer.Deserialize(json);
    unsupportedOperationProfile.OperationRequests.Add(
        new ProfileOperationRequestModel
        {
            OperationId = "unsupported.test.operation",
            Settings = new JObject()
        });
    _ = new ProfileEffectiveChangeCountService().Calculate(
        fullyCompatibleTarget,
        unsupportedOperationProfile,
        out bool unsupportedExact);
    Check(!unsupportedExact,
        "target-context unsupported semantic operation is not exact");

    ModProfileModel failedOperationProfile = serializer.Deserialize(json);
    failedOperationProfile.OperationRequests.Add(
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.RequestBoardRewards,
            Settings = new JObject { ["percentage"] = 200 }
        });
    _ = new ProfileEffectiveChangeCountService().Calculate(
        fullyCompatibleTarget,
        failedOperationProfile,
        out bool failedOperationExact);
    Check(!failedOperationExact,
        "target-context failed semantic operation is not exact");

    ProjectModel historyTarget = LoadVerified(sourcePath, sourceIdentity);
    ModificationSnapshotImportResultModel historyResult =
        workflow.ApplyProfile(historyTarget, profile);
    ProjectOperationHistoryAction impactHistory = new(
        "Historical impact stability",
        historyResult.MutationResult,
        new ProjectOperationTransactionService());
    impactHistory.Undo();
    Check(library.GetProfiles(historyTarget)
              .Single(summary => summary.FilePath == added.FilePath)
              .EffectiveChangeCount == 1,
        "Undo does not redefine historical profile impact");
    impactHistory.Redo();
    Check(library.GetProfiles(historyTarget)
              .Single(summary => summary.FilePath == added.FilePath)
              .EffectiveChangeCount == 1,
        "Redo does not redefine historical profile impact");

    string reopenedTargetPath = Path.Combine(root, "reopened-target.cdb");
    new JsonDataService().SaveProject(historyTarget, reopenedTargetPath);
    ProjectModel reopenedTarget = new JsonDataService().LoadProject(
        reopenedTargetPath);
    ModProfileModel reopenedProfile = serializer.Load(added.FilePath);
    Check(reopenedProfile.ImpactManifest?.TotalCount == 1 &&
          library.GetProfiles(reopenedTarget)
              .Single(summary => summary.FilePath == added.FilePath)
              .EffectiveChangeCount == 1,
        "profile and applied project save/reopen retain historical count");

    ModProfileModel unavailable = serializer.Deserialize(json);
    unavailable.ImpactManifest = null;
    string unavailablePath = Path.Combine(root, "unavailable.wtprofile");
    serializer.Save(unavailable, unavailablePath);
    ModProfileSummaryModel unavailableSummary = library.ImportProfile(
        unavailablePath);
    Check(!unavailableSummary.IsEffectiveChangeCountExact &&
          unavailableSummary.ChangeSummaryText == "Unavailable",
        "missing manifest displays Unavailable");

    ProjectModel zeroSource = LoadVerified(sourcePath, sourceIdentity);
    ModProfileModel zero = workflow.CreateProfile(
        zeroSource, "Zero", editorVersion: "test");
    Check(zero.ImpactManifest?.TotalCount == 0,
        "valid zero-leaf manifest remains distinct from unavailable");

    ModProfileModel unchangedUpdate = workflow.CreateUpdatedProfile(
        source, profile, "test");
    Check(unchangedUpdate.ImpactManifest?.EvidenceFingerprint ==
          profile.ImpactManifest.EvidenceFingerprint,
        "unchanged gameplay update preserves manifest");

    _ = new ProjectMutationService().EnsurePropertyByPath(
        Entry(source), "value", new JValue(3));
    ModProfileModel changedUpdate = workflow.CreateUpdatedProfile(
        source, profile, "test");
    Check(changedUpdate.ImpactManifest != null &&
          changedUpdate.ImpactManifest.ProfileGameplayContentIdentity !=
          profile.ImpactManifest.ProfileGameplayContentIdentity,
        "changed gameplay update replaces manifest with exact authority");

    ProjectModel noBaseline = CreateDetached(CreateJson(1), "missing.cdb");
    _ = new ProjectMutationService().EnsurePropertyByPath(
        Entry(noBaseline), "value", new JValue(2));
    ModProfileModel withoutBaseline = workflow.CreateProfile(
        noBaseline, "No baseline", editorVersion: "test");
    Check(withoutBaseline.ImpactManifest == null,
        "Create succeeds without exact baseline and omits manifest");

    _ = new ProjectMutationService().EnsurePropertyByPath(
        Entry(noBaseline), "value", new JValue(3));
    ModProfileModel changedWithoutBaseline = workflow.CreateUpdatedProfile(
        noBaseline, profile, "test");
    Check(changedWithoutBaseline.ImpactManifest == null,
        "changed update without exact authority removes stale manifest");

    JObject incompleteRoot = JObject.Parse(json);
    incompleteRoot.Property(nameof(ModProfileModel.ImpactManifest))!.Remove();
    ((JObject)incompleteRoot[nameof(ModProfileModel.Snapshot)]!
        ["Categories"]![0]!["Settings"]![0]!)["Id"] = "Missing";
    ModProfileModel incompleteProfile = serializer.Deserialize(
        incompleteRoot.ToString());
    ProfileImpactEvaluationService incompleteEvaluator = new(
        new ProfileImpactBaselineResolver(
            new IProfileImpactBaselineProvider[]
            {
                new FixedProvider(pristineBytes, "incomplete exact baseline")
            }),
        new JsonDataService(),
        new ModProfileWorkflowService().ApplyProfile);
    ProfileImpactManifestModel? incompleteManifest =
        incompleteEvaluator.TryEstablish(
            LoadVerified(sourcePath, sourceIdentity),
            incompleteProfile,
            "test",
            out _);
    Check(incompleteManifest == null,
        "incomplete unmatched evaluation persists no partial exact manifest");

    JObject conflictRoot = JObject.Parse(json);
    conflictRoot.Property(nameof(ModProfileModel.ImpactManifest))!.Remove();
    JObject conflictProperty = (JObject)conflictRoot[
        nameof(ModProfileModel.Snapshot)]!["Categories"]![0]!["Settings"]![0]![
        "Properties"]![0]!;
    conflictProperty["OriginalValue"] = 9;
    ModProfileModel conflictProfile = serializer.Deserialize(
        conflictRoot.ToString());
    CheckUnavailableImpact(
        sourcePath, sourceIdentity, pristineBytes, conflictProfile,
        "raw conflict persists no partial exact manifest");

    JObject invalidRawRoot = JObject.Parse(json);
    invalidRawRoot.Property(nameof(ModProfileModel.ImpactManifest))!.Remove();
    JObject invalidRawProperty = (JObject)invalidRawRoot[
        nameof(ModProfileModel.Snapshot)]!["Categories"]![0]!["Settings"]![0]![
        "Properties"]![0]!;
    invalidRawProperty["CurrentValue"] =
        invalidRawProperty["OriginalValue"]!.DeepClone();
    ModProfileModel invalidManifestProfile = serializer.Deserialize(
        invalidRawRoot.ToString());
    CheckUnavailableImpact(
        sourcePath, sourceIdentity, pristineBytes, invalidManifestProfile,
        "invalid raw record persists no partial exact manifest");

    ModProfileModel unsupportedManifestProfile = serializer.Deserialize(json);
    unsupportedManifestProfile.ImpactManifest = null;
    unsupportedManifestProfile.OperationRequests.Add(
        new ProfileOperationRequestModel
        {
            OperationId = "unsupported.test.operation",
            Settings = new JObject()
        });
    CheckUnavailableImpact(
        sourcePath, sourceIdentity, pristineBytes,
        unsupportedManifestProfile,
        "unsupported semantic operation persists no partial exact manifest");

    ModProfileModel failedManifestProfile = serializer.Deserialize(json);
    failedManifestProfile.ImpactManifest = null;
    failedManifestProfile.OperationRequests.Add(
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.RequestBoardRewards,
            Settings = new JObject { ["percentage"] = 200 }
        });
    CheckUnavailableImpact(
        sourcePath, sourceIdentity, pristineBytes, failedManifestProfile,
        "failed semantic preflight persists no partial exact manifest");

    ModProfileModel missingResultProfile = serializer.Deserialize(json);
    missingResultProfile.OperationRequests.Add(
        new ProfileOperationRequestModel
        {
            OperationId = ProfileOperationIds.RequestBoardRewards,
            Settings = new JObject { ["percentage"] = 200 }
        });
    ProfileImpactEvaluationService missingResultEvaluator = new(
        new ProfileImpactBaselineResolver(
            new IProfileImpactBaselineProvider[]
            {
                new FixedProvider(pristineBytes, "missing-result baseline")
            }),
        new JsonDataService(),
        (detached, _) => new ModProfileWorkflowService().ApplyProfile(
            detached, profile));
    Check(missingResultEvaluator.TryEstablish(
              LoadVerified(sourcePath, sourceIdentity),
              missingResultProfile,
              "test",
              out _) == null,
        "missing requested operation result persists no partial exact manifest");
    System.Reflection.MethodInfo targetCompleteness =
        typeof(EffectiveChangeCountService).GetMethod(
            "IsComplete",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static)
        ?? throw new InvalidOperationException(
            "Target-context completeness predicate was not found.");
    Check(!(bool)targetCompleteness.Invoke(
              null,
              new object[] { missingResultProfile, historyResult })!,
        "target-context completion rejects a missing requested operation result");

    VerifyEvaluatorIntegrityFailure(
        sourcePath,
        sourceIdentity,
        pristineBytes,
        profile,
        "updated property without canonical identity",
        (detached, mutation) =>
        {
            PropertyModel foreignProperty = new()
            {
                SheetName = "constant",
                Name = "value",
                PropertyPath = "value",
                SourceProperty = new JProperty("value", 2)
            };
            mutation.AddUpdatedProperty(foreignProperty, new JValue(1));
        });
    VerifyEvaluatorIntegrityFailure(
        sourcePath,
        sourceIdentity,
        pristineBytes,
        profile,
        "mutation with neither baseline nor final property",
        (detached, mutation) =>
        {
            JObject foreignSource = new()
            {
                ["id"] = "Ghost",
                ["value"] = 2
            };
            EntryModel foreignEntry = new()
            {
                Id = "Ghost",
                SourceEntry = foreignSource
            };
            PropertyModel foreignProperty = new()
            {
                SheetName = "constant",
                Name = "value",
                PropertyPath = "value",
                SourceProperty = foreignSource.Property("value")
            };
            mutation.AddProperty(foreignEntry, foreignProperty);
        });
    VerifyEvaluatorIntegrityFailure(
        sourcePath,
        sourceIdentity,
        pristineBytes,
        profile,
        "ambiguous canonical property identity",
        (detached, mutation) =>
        {
            EntryModel detachedEntry = Entry(detached);
            PropertyModel original = detachedEntry.Properties.Single(
                property => property.EffectivePropertyPath == "value");
            detachedEntry.Properties.Add(new PropertyModel
            {
                SheetName = original.SheetName,
                Name = original.Name,
                PropertyPath = original.PropertyPath,
                SourceProperty = original.SourceProperty
            });
        });
    VerifyEvaluatorIntegrityFailure(
        sourcePath,
        sourceIdentity,
        pristineBytes,
        profile,
        "changed property missing stable identity",
        (detached, mutation) =>
        {
            JObject unidentifiedSource = new() { ["value"] = 2 };
            EntryModel unidentifiedEntry = new()
            {
                SourceEntry = unidentifiedSource
            };
            PropertyModel unidentifiedProperty = new()
            {
                Name = "value",
                PropertyPath = "value",
                SourceProperty = unidentifiedSource.Property("value")
            };
            mutation.AddProperty(unidentifiedEntry, unidentifiedProperty);
        });

    byte[] otherBytes = Encoding.UTF8.GetBytes(CreateJson(9));

    foreach ((Exception exception, string label) in new[]
             {
                 ((Exception)new IOException("provider IO"), "IOException"),
                 (new UnauthorizedAccessException("provider access"),
                     "UnauthorizedAccessException"),
                 (new InvalidDataException("provider data"),
                     "InvalidDataException"),
                 (new InvalidOperationException("provider unexpected"),
                     "unexpected provider exception")
             })
    {
        ProjectModel failureSource = LoadVerified(sourcePath, sourceIdentity);
        _ = new ProjectMutationService().EnsurePropertyByPath(
            Entry(failureSource), "value", new JValue(2));
        string beforeFailure = failureSource.RootDocument.ToString();
        int beforeStateCount = failureSource.GameplayOperationStates.Count;
        bool beforeProjectModified = failureSource.IsModified;
        bool beforeStateModified = failureSource.IsGameplayOperationStateModified;
        ModProfileWorkflowService failingWorkflow = CreateWorkflow(
            new ThrowingProvider(exception));
        ModProfileModel createdDespiteFailure = failingWorkflow.CreateProfile(
            failureSource, $"Create {label}", editorVersion: "test");
        Check(createdDespiteFailure.FormatVersion == 5 &&
              createdDespiteFailure.ImpactManifest == null,
            $"Create contains {label} as unavailable impact authority");
        Check(failureSource.RootDocument.ToString() == beforeFailure,
            $"Create {label} evaluation leaves the real project unchanged");
        Check(failureSource.GameplayOperationStates.Count == beforeStateCount &&
              failureSource.IsModified == beforeProjectModified &&
              failureSource.IsGameplayOperationStateModified == beforeStateModified,
            $"Create {label} evaluation leaves state and modification flags unchanged");

        _ = new ProjectMutationService().EnsurePropertyByPath(
            Entry(failureSource), "value", new JValue(3));
        string beforeUpdateFailure = failureSource.RootDocument.ToString();
        ModProfileModel updatedDespiteFailure =
            failingWorkflow.CreateUpdatedProfile(
                failureSource, profile, "test");
        Check(updatedDespiteFailure.ImpactManifest == null,
            $"Update contains {label} and removes stale manifest authority");
        Check(failureSource.RootDocument.ToString() == beforeUpdateFailure,
            $"Update {label} evaluation leaves the real project unchanged");
    }

    foreach ((IProfileImpactBaselineProvider provider, string label) in new[]
             {
                 ((IProfileImpactBaselineProvider)new MalformedProvider(),
                     "malformed provider result"),
                 (new FixedProvider(otherBytes, "byte mismatch"),
                     "provider byte/hash mismatch"),
                 (new UnavailableProvider(), "missing provider file")
             })
    {
        ProjectModel providerSource = LoadVerified(sourcePath, sourceIdentity);
        _ = new ProjectMutationService().EnsurePropertyByPath(
            Entry(providerSource), "value", new JValue(2));
        ModProfileModel providerFailure = CreateWorkflow(provider)
            .CreateProfile(providerSource, label, editorVersion: "test");
        Check(providerFailure.ImpactManifest == null,
            $"{label} remains optional and unavailable");
    }

    ModProfileWorkflowService preservingWorkflow = CreateWorkflow(
        new ThrowingProvider(new IOException("must not be called")));
    ProjectModel preservationSource = LoadVerified(sourcePath, sourceIdentity);
    _ = new ProjectMutationService().EnsurePropertyByPath(
        Entry(preservationSource), "value", new JValue(2));
    ModProfileModel preservedWithoutProvider =
        preservingWorkflow.CreateUpdatedProfile(
            preservationSource, profile, "test");
    Check(preservedWithoutProvider.ImpactManifest?.EvidenceFingerprint ==
          profile.ImpactManifest.EvidenceFingerprint,
        "unchanged source and gameplay preserve authority without provider access");

    string sourceBPath = Path.Combine(root, "source-b.cdb");
    byte[] sourceBBytes = Encoding.UTF8.GetBytes(CreateJsonWithExtra(1));
    File.WriteAllBytes(sourceBPath, sourceBBytes);
    string sourceBIdentity = new CdbGenerationIdentityService()
        .Calculate(sourceBBytes);
    ProjectModel sourceB = LoadVerified(sourceBPath, sourceBIdentity);
    _ = new ProjectMutationService().EnsurePropertyByPath(
        Entry(sourceB), "value", new JValue(2));
    ModProfileModel changedSourceWithBaseline = workflow.CreateUpdatedProfile(
        sourceB, profile, "test");
    Check(changedSourceWithBaseline.ImpactManifest != null &&
          changedSourceWithBaseline.ImpactManifest.SourceCdbGenerationIdentity ==
              sourceBIdentity &&
          changedSourceWithBaseline.ImpactManifest.SourceCdbGenerationIdentity !=
              profile.ImpactManifest.SourceCdbGenerationIdentity,
        "source A to B update replaces authority using exact pristine B");

    ProjectModel sourceBWithoutBaseline = CreateDetached(CreateJsonWithExtra(1),
        "source-b-missing.cdb");
    _ = new ProjectMutationService().EnsurePropertyByPath(
        Entry(sourceBWithoutBaseline), "value", new JValue(2));
    ModProfileModel changedSourceUnavailable = workflow.CreateUpdatedProfile(
        sourceBWithoutBaseline, profile, "test");
    Check(changedSourceUnavailable.ImpactManifest == null,
        "source A to B update removes old authority when exact B is unavailable");

    CurrentProjectProfileImpactBaselineProvider currentProvider = new();
    Check(currentProvider.TryGetBaseline(source, sourceIdentity, out _),
        "exact current persisted pristine source is accepted");
    File.WriteAllBytes(sourcePath, Encoding.UTF8.GetBytes(CreateJson(8)));
    Check(!currentProvider.TryGetBaseline(source, sourceIdentity, out _),
        "modified persisted source is rejected by exact hash");

    string goldenDirectory = Path.Combine(root, "golden");
    File.WriteAllBytes(sourcePath, pristineBytes);
    GoldenCdbService goldenService = new(new JsonDataService(), goldenDirectory);
    _ = goldenService.SetFromFile(sourcePath);
    GoldenProfileImpactBaselineProvider goldenProvider = new(goldenService);
    Check(goldenProvider.TryGetBaseline(source, sourceIdentity, out _),
        "exact matching Golden baseline is accepted");
    File.WriteAllBytes(goldenService.GetCanonicalPath(), otherBytes);
    Check(!goldenProvider.TryGetBaseline(source, sourceIdentity, out _),
        "stale or nonmatching Golden baseline is rejected");

    ProjectModel unmatchedTarget = CreateDetached(
        CreateJsonWithId("Other", 1), "unmatched.cdb");
    _ = new ProfileEffectiveChangeCountService().Calculate(
        unmatchedTarget, profile, out bool unmatchedExact);
    Check(!unmatchedExact,
        "target-context API does not label unmatched partial evaluation exact");

    ProfileImpactBaselineResolver resolver = new(new IProfileImpactBaselineProvider[]
    {
        new FixedProvider(otherBytes, "wrong"),
        new FixedProvider(pristineBytes, "right")
    });
    Check(resolver.TryResolve(source, sourceIdentity, out ProfileImpactBaseline? resolved) &&
          resolved?.ProviderName == "right",
        "resolver rejects wrong bytes and uses deterministic provider order");

    JObject legacyRoot = JObject.Parse(json);
    legacyRoot.Property(nameof(ModProfileModel.ImpactManifest))!.Remove();
    for (int version = 1; version <= 4; version++)
    {
        legacyRoot[nameof(ModProfileModel.FormatVersion)] = version;
        Check(serializer.Deserialize(legacyRoot.ToString()).FormatVersion == version,
            $"format {version} remains readable without migration");
    }
    legacyRoot[nameof(ModProfileModel.FormatVersion)] = 6;
    CheckThrows(() => serializer.Deserialize(legacyRoot.ToString()),
        "newer root formats remain safely rejected");

    string largePath = Path.Combine(root, "large.cdb");
    byte[] largeBytes = Encoding.UTF8.GetBytes(CreateLargeJson(744));
    File.WriteAllBytes(largePath, largeBytes);
    string largeIdentity = new CdbGenerationIdentityService().Calculate(largeBytes);
    ProjectModel largeProject = LoadVerified(largePath, largeIdentity);
    ProjectMutationService largeMutations = new();
    foreach (EntryModel entry in largeProject.Sheets.Single().Entries)
        _ = largeMutations.EnsurePropertyByPath(entry, "value", new JValue(2));
    Stopwatch impactTimer = Stopwatch.StartNew();
    ModProfileModel largeProfile = workflow.CreateProfile(
        largeProject, "Large", editorVersion: "test");
    impactTimer.Stop();
    Check(largeProfile.ImpactManifest?.TotalCount == 744,
        "744-leaf profile establishes the exact stable count");
    Console.WriteLine(
        $"744-leaf detached impact evaluation: {impactTimer.ElapsedMilliseconds} ms");

    Console.WriteLine($"Profile Impact Manifest smoke checks passed: {checks}");
}
finally
{
    if (Directory.Exists(root))
        Directory.Delete(root, recursive: true);
}

ProfileImpactManifestModel CloneManifestWithLeaves(
    ProfileImpactManifestModel sourceManifest,
    IEnumerable<ProfileImpactLeafModel> leaves,
    ProfileImpactManifestValidationService validationService)
{
    List<ProfileImpactLeafModel> clonedLeaves = leaves
        .Select(leaf => leaf.DeepClone())
        .ToList();
    ProfileImpactManifestModel clone = new()
    {
        FormatVersion = sourceManifest.FormatVersion,
        ImpactSemanticsVersion = sourceManifest.ImpactSemanticsVersion,
        SourceCdbGenerationIdentity =
            sourceManifest.SourceCdbGenerationIdentity,
        ProfileGameplayContentIdentity =
            sourceManifest.ProfileGameplayContentIdentity,
        TotalCount = clonedLeaves.Count,
        Leaves = clonedLeaves,
        EstablishedAtUtc = sourceManifest.EstablishedAtUtc,
        EstablishedByEditorVersion =
            sourceManifest.EstablishedByEditorVersion
    };
    clone.EvidenceFingerprint =
        validationService.CalculateEvidenceFingerprint(clone);
    return clone;
}

void CheckUnavailableImpact(
    string path,
    string identity,
    byte[] baselineBytes,
    ModProfileModel evaluatedProfile,
    string message)
{
    ProjectModel realProject = LoadVerified(path, identity);
    string beforeJson = realProject.RootDocument.ToString();
    int beforeStateCount = realProject.GameplayOperationStates.Count;
    bool beforeModified = realProject.IsModified;
    bool beforeStateModified = realProject.IsGameplayOperationStateModified;
    ProfileImpactEvaluationService evaluator = new(
        new ProfileImpactBaselineResolver(
            new IProfileImpactBaselineProvider[]
            {
                new FixedProvider(baselineBytes, "incomplete evaluation")
            }),
        new JsonDataService(),
        new ModProfileWorkflowService().ApplyProfile);

    ProfileImpactManifestModel? result = evaluator.TryEstablish(
        realProject,
        evaluatedProfile,
        "test",
        out _);

    Check(result == null, message);
    Check(realProject.RootDocument.ToString() == beforeJson &&
          realProject.GameplayOperationStates.Count == beforeStateCount &&
          realProject.IsModified == beforeModified &&
          realProject.IsGameplayOperationStateModified == beforeStateModified,
        $"{message} leaves the real project and gameplay state unchanged");
}

void VerifyEvaluatorIntegrityFailure(
    string path,
    string identity,
    byte[] baselineBytes,
    ModProfileModel evaluatedProfile,
    string label,
    Action<ProjectModel, ProjectMutationResult> arrange)
{
    ProjectModel realProject = LoadVerified(path, identity);
    string beforeJson = realProject.RootDocument.ToString();
    int beforeStateCount = realProject.GameplayOperationStates.Count;
    bool beforeModified = realProject.IsModified;
    bool beforeStateModified = realProject.IsGameplayOperationStateModified;
    ProfileImpactEvaluationService evaluator = new(
        new ProfileImpactBaselineResolver(
            new IProfileImpactBaselineProvider[]
            {
                new FixedProvider(baselineBytes, "integrity evaluation")
            }),
        new JsonDataService(),
        (detached, evaluatedProfile) =>
        {
            ProjectMutationResult mutation = new();
            arrange(detached, mutation);
            return CreateEmptyEvaluationResult(evaluatedProfile, mutation);
        });

    bool integrityFailure = false;
    ProfileImpactManifestModel? unexpectedManifest = null;
    string unavailableReason = string.Empty;
    try
    {
        unexpectedManifest = evaluator.TryEstablish(
            realProject,
            evaluatedProfile,
            "test",
            out unavailableReason);
    }
    catch (ProfileImpactEvaluationIntegrityException)
    {
        integrityFailure = true;
    }

    Check(integrityFailure,
        $"{label} propagates as a distinct evaluator integrity failure " +
        $"(manifest: {unexpectedManifest != null}, reason: '{unavailableReason}')");
    Check(realProject.RootDocument.ToString() == beforeJson &&
          realProject.GameplayOperationStates.Count == beforeStateCount &&
          realProject.IsModified == beforeModified &&
          realProject.IsGameplayOperationStateModified == beforeStateModified,
        $"{label} cannot mutate the real project or gameplay state");
}

ModificationSnapshotImportResultModel CreateEmptyEvaluationResult(
    ModProfileModel evaluatedProfile,
    ProjectMutationResult mutation)
{
    ModificationMatchResultModel match = new(
        Array.Empty<ModificationMatchItemModel>());
    ModificationPreviewResultModel preview = new(
        Array.Empty<ModificationPreviewItemModel>());
    ModificationApplyResultModel apply = new(
        Array.Empty<ModificationApplyItemResultModel>(),
        mutation);
    return new ModificationSnapshotImportResultModel(
        evaluatedProfile.Snapshot,
        match,
        preview,
        apply,
        "integrity-test",
        Array.Empty<ProfileOperationApplyItemResultModel>(),
        mutation);
}

void CheckTamper(string sourceJson, Action<JObject> tamper, string message)
{
    JObject rootObject = JObject.Parse(sourceJson);
    JObject manifest = (JObject)rootObject[nameof(ModProfileModel.ImpactManifest)]!;
    tamper(manifest);
    Check(serializer.Deserialize(rootObject.ToString()).ImpactManifest == null, message);
}

void CheckThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch (ModProfileSerializationException)
    {
        Check(true, message);
        return;
    }

    Check(false, message);
}

void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException($"FAIL: {message}");
    checks++;
    Console.WriteLine($"PASS {message}");
}

string AddRootManifestAlias(
    string sourceJson,
    string firstName,
    string secondName,
    string secondValue)
{
    JObject rootObject = JObject.Parse(sourceJson);
    string firstValue = rootObject[nameof(ModProfileModel.ImpactManifest)]!
        .ToString(Newtonsoft.Json.Formatting.None);
    rootObject.Property(nameof(ModProfileModel.ImpactManifest))!.Remove();
    string core = rootObject.ToString(Newtonsoft.Json.Formatting.None);
    return core[..^1] +
           $",\"{firstName}\":{firstValue}," +
           $"\"{secondName}\":{secondValue}}}";
}

int CountRootManifestAliases(string sourceJson)
{
    int count = 0;
    using StringReader textReader = new(sourceJson);
    using JsonTextReader reader = new(textReader);
    if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
        return 0;

    while (reader.Read() && reader.TokenType != JsonToken.EndObject)
    {
        if (reader.TokenType != JsonToken.PropertyName)
            throw new InvalidDataException("Invalid test JSON root.");
        string name = reader.Value?.ToString() ?? string.Empty;
        if (!reader.Read())
            throw new InvalidDataException("Incomplete test JSON root.");
        _ = JToken.ReadFrom(reader);
        if (string.Equals(name, nameof(ModProfileModel.ImpactManifest),
                StringComparison.OrdinalIgnoreCase))
            count++;
    }

    return count;
}

ProjectModel LoadAndApplyCore(
    ModProfileModel coreProfile,
    string path,
    string identity)
{
    ProjectModel target = LoadVerified(path, identity);
    _ = new ModProfileWorkflowService().ApplyProfile(target, coreProfile);
    return target;
}

ModProfileWorkflowService CreateWorkflow(
    IProfileImpactBaselineProvider provider)
{
    ProjectMutationService mutations = new();
    ContentCreationService content = new(mutations);
    GameplayOperationStateService states = new(mutations);
    return new ModProfileWorkflowService(
        new ModProfileService(),
        new ModProfileSerializationService(),
        new ModificationSnapshotWorkflowService(),
        new ProfileOperationResolver(
            new AddCampFacilitiesOperation(content),
            new UpgradeAllEquipmentOperation(content),
            new RequestBoardRewardsService(mutations, states)),
        new ProjectOperationService(),
        new ProjectOperationTransactionService(),
        new LocalizationService(),
        new ProfileImpactBaselineResolver(new[] { provider }));
}

ProjectModel LoadVerified(string path, string identity)
{
    ProjectModel project = new JsonDataService().LoadProject(path);
    project.EstablishPersistedIdentity(identity, identity, SourceProvenanceStatus.Verified);
    return project;
}

ProjectModel CreateDetached(string sourceJson, string fileName)
{
    ProjectModel project = new JsonDataService().CreateProjectFromJson(
        sourceJson, Path.Combine(root, fileName));
    string identity = new CdbGenerationIdentityService().Calculate(
        Encoding.UTF8.GetBytes(sourceJson));
    project.EstablishPersistedIdentity(identity, identity, SourceProvenanceStatus.Verified);
    return project;
}

EntryModel Entry(ProjectModel project) => project.Sheets.Single()
    .Entries.Single(entry => entry.Id == "Value");

string CreateJson(int value) => new JObject
{
    ["sheets"] = new JArray(new JObject
    {
        ["name"] = "constant",
        ["columns"] = new JArray(
            new JObject { ["typeStr"] = "0", ["name"] = "id" },
            new JObject { ["typeStr"] = "3", ["name"] = "value" }),
        ["lines"] = new JArray(
            new JObject { ["id"] = "Value", ["value"] = value })
    })
}.ToString(Newtonsoft.Json.Formatting.None);

string CreateJsonWithId(string id, int value) => new JObject
{
    ["sheets"] = new JArray(new JObject
    {
        ["name"] = "constant",
        ["columns"] = new JArray(
            new JObject { ["typeStr"] = "0", ["name"] = "id" },
            new JObject { ["typeStr"] = "3", ["name"] = "value" }),
        ["lines"] = new JArray(
            new JObject { ["id"] = id, ["value"] = value })
    })
}.ToString(Newtonsoft.Json.Formatting.None);

string CreateJsonWithExtra(int value) => new JObject
{
    ["sheets"] = new JArray(new JObject
    {
        ["name"] = "constant",
        ["columns"] = new JArray(
            new JObject { ["typeStr"] = "0", ["name"] = "id" },
            new JObject { ["typeStr"] = "3", ["name"] = "value" }),
        ["lines"] = new JArray(
            new JObject { ["id"] = "Value", ["value"] = value },
            new JObject { ["id"] = "Unrelated", ["value"] = 7 })
    })
}.ToString(Newtonsoft.Json.Formatting.None);

string CreateLargeJson(int count) => new JObject
{
    ["sheets"] = new JArray(new JObject
    {
        ["name"] = "constant",
        ["columns"] = new JArray(
            new JObject { ["typeStr"] = "0", ["name"] = "id" },
            new JObject { ["typeStr"] = "3", ["name"] = "value" }),
        ["lines"] = new JArray(Enumerable.Range(0, count).Select(index =>
            new JObject { ["id"] = $"Value{index:D4}", ["value"] = 1 }))
    })
}.ToString(Newtonsoft.Json.Formatting.None);

sealed class FixedProvider : IProfileImpactBaselineProvider
{
    private readonly byte[] bytes;
    private readonly string name;

    public FixedProvider(byte[] bytes, string name)
    {
        this.bytes = bytes;
        this.name = name;
    }

    public bool TryGetBaseline(
        ProjectModel sourceProject,
        string requiredSourceIdentity,
        out ProfileImpactBaseline? baseline)
    {
        baseline = new ProfileImpactBaseline(
            bytes,
            new CdbGenerationIdentityService().Calculate(bytes),
            name);
        return true;
    }
}

sealed class ThrowingProvider : IProfileImpactBaselineProvider
{
    private readonly Exception exception;

    public ThrowingProvider(Exception exception) => this.exception = exception;

    public bool TryGetBaseline(
        ProjectModel sourceProject,
        string requiredSourceIdentity,
        out ProfileImpactBaseline? baseline) =>
        throw exception;
}

sealed class MalformedProvider : IProfileImpactBaselineProvider
{
    public bool TryGetBaseline(
        ProjectModel sourceProject,
        string requiredSourceIdentity,
        out ProfileImpactBaseline? baseline)
    {
        baseline = new ProfileImpactBaseline(
            null!, requiredSourceIdentity, "malformed");
        return true;
    }
}

sealed class UnavailableProvider : IProfileImpactBaselineProvider
{
    public bool TryGetBaseline(
        ProjectModel sourceProject,
        string requiredSourceIdentity,
        out ProfileImpactBaseline? baseline)
    {
        baseline = null;
        return false;
    }
}
