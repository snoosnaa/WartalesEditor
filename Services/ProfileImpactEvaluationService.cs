using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ProfileImpactEvaluationService
{
    private readonly ProfileImpactBaselineResolver baselineResolver;
    private readonly JsonDataService jsonDataService;
    private readonly ProfileGameplayContentIdentityService contentIdentityService;
    private readonly ProfileImpactManifestValidationService manifestValidationService;
    private readonly Func<ProjectModel, ModProfileModel,
        ModificationSnapshotImportResultModel> applyProfile;

    public ProfileImpactEvaluationService(
        ProfileImpactBaselineResolver baselineResolver,
        JsonDataService jsonDataService,
        Func<ProjectModel, ModProfileModel,
            ModificationSnapshotImportResultModel> applyProfile)
        : this(
            baselineResolver,
            jsonDataService,
            new ProfileGameplayContentIdentityService(),
            new ProfileImpactManifestValidationService(),
            applyProfile)
    {
    }

    public ProfileImpactEvaluationService(
        ProfileImpactBaselineResolver baselineResolver,
        JsonDataService jsonDataService,
        ProfileGameplayContentIdentityService contentIdentityService,
        ProfileImpactManifestValidationService manifestValidationService,
        Func<ProjectModel, ModProfileModel,
            ModificationSnapshotImportResultModel> applyProfile)
    {
        this.baselineResolver = baselineResolver
            ?? throw new ArgumentNullException(nameof(baselineResolver));
        this.jsonDataService = jsonDataService
            ?? throw new ArgumentNullException(nameof(jsonDataService));
        this.contentIdentityService = contentIdentityService
            ?? throw new ArgumentNullException(nameof(contentIdentityService));
        this.manifestValidationService = manifestValidationService
            ?? throw new ArgumentNullException(nameof(manifestValidationService));
        this.applyProfile = applyProfile
            ?? throw new ArgumentNullException(nameof(applyProfile));
    }

    public ProfileImpactManifestModel? TryEstablish(
        ProjectModel sourceProject,
        ModProfileModel profile,
        string editorVersion,
        out string unavailableReason)
    {
        ArgumentNullException.ThrowIfNull(sourceProject);
        ArgumentNullException.ThrowIfNull(profile);
        unavailableReason = string.Empty;

        try
        {
            if (!baselineResolver.TryResolve(
                    sourceProject,
                    profile.SourceCdbGenerationIdentity,
                    out ProfileImpactBaseline? baseline) ||
                baseline == null)
            {
                unavailableReason =
                    "No exact pristine source is currently available for this profile revision.";
                return null;
            }

            ProjectModel detached = CreateDetachedProject(baseline);
            ProjectLeafIndex before = ProjectLeafIndex.Create(detached);

            ModificationSnapshotImportResultModel result =
                applyProfile(detached, profile);

            if (!IsComplete(profile, result))
            {
                unavailableReason =
                    "The complete profile could not be evaluated against its pristine source.";
                return null;
            }

            ProjectLeafIndex after = ProjectLeafIndex.Create(detached);
            IReadOnlyList<ProfileImpactLeafModel> leaves =
                CreateLeaves(result.MutationResult, before, after);
            string contentIdentity = contentIdentityService.Calculate(profile);

            ProfileImpactManifestModel manifest = new()
            {
                SourceCdbGenerationIdentity = baseline.SourceIdentity,
                ProfileGameplayContentIdentity = contentIdentity,
                TotalCount = leaves.Count,
                Leaves = leaves.ToList(),
                EstablishedAtUtc = DateTimeOffset.UtcNow,
                EstablishedByEditorVersion =
                    string.IsNullOrWhiteSpace(editorVersion)
                        ? "Unknown"
                        : editorVersion.Trim()
            };
            manifest.EvidenceFingerprint =
                manifestValidationService.CalculateEvidenceFingerprint(manifest);

            if (!manifestValidationService.TryValidate(
                    profile,
                    manifest,
                    out unavailableReason))
            {
                return null;
            }

            return manifest;
        }
        catch (Exception exception) when (!IsFatalOrIntegrityFailure(exception))
        {
            unavailableReason = exception.Message;
            return null;
        }
    }

    private static bool IsFatalOrIntegrityFailure(Exception exception) =>
        exception is ProfileImpactEvaluationIntegrityException or
        ProjectRollbackIntegrityException or
        OutOfMemoryException or
        StackOverflowException or
        AccessViolationException or
        AppDomainUnloadedException or
        BadImageFormatException;

    private ProjectModel CreateDetachedProject(ProfileImpactBaseline baseline)
    {
        string json;
        using MemoryStream stream = new(baseline.ExactBytes, writable: false);
        using (StreamReader reader = new(
                   stream,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: true,
                   bufferSize: 1024,
                   leaveOpen: false))
        {
            json = reader.ReadToEnd();
        }

        ProjectModel project = jsonDataService.CreateProjectFromJson(json);
        if (project.Sheets.Count == 0)
        {
            throw new InvalidDataException(
                "The pristine source contains no usable project sheets.");
        }

        project.EstablishPersistedIdentity(
            baseline.SourceIdentity,
            baseline.SourceIdentity,
            SourceProvenanceStatus.Verified);
        return project;
    }

    private static bool IsComplete(
        ModProfileModel profile,
        ModificationSnapshotImportResultModel result)
    {
        if (result.UnmatchedCount != 0 ||
            result.ConflictCount != 0 ||
            result.InvalidSnapshotChangeCount != 0 ||
            result.FailedCount != 0 ||
            result.OperationsFailedCount != 0 ||
            result.ApplyResult.NotMatchedCount != 0)
        {
            return false;
        }

        string[] requested = profile.OperationRequests
            .Select(request => request.OperationId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] evaluated = result.OperationResults
            .Select(operation => operation.OperationId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        return requested.SequenceEqual(evaluated, StringComparer.Ordinal);
    }

    private static IReadOnlyList<ProfileImpactLeafModel> CreateLeaves(
        ProjectMutationResult mutationResult,
        ProjectLeafIndex before,
        ProjectLeafIndex after)
    {
        HashSet<LeafKey> candidates = new();

        foreach (var record in mutationResult.CreatedPropertyRollbackRecords)
        {
            candidates.Add(CreateKey(record.Entry, record.Property));
        }

        foreach (PropertyModel property in mutationResult.UpdatedProperties)
        {
            if (!before.TryGetKey(property, out LeafKey key) &&
                !after.TryGetKey(property, out key))
            {
                throw new ProfileImpactEvaluationIntegrityException(
                    $"Updated property '{property.EffectivePropertyPath}' has no canonical entry identity.");
            }

            candidates.Add(key);
        }

        foreach (var record in mutationResult.RemovedPropertyRollbackRecords)
        {
            candidates.Add(CreateKey(
                record.Entry,
                record.Property,
                record.PropertyPath));
        }

        List<ProfileImpactLeafModel> leaves = new();
        foreach (LeafKey key in candidates.OrderBy(key => key.Identity, StringComparer.Ordinal))
        {
            bool existedBefore = before.Values.TryGetValue(key, out JToken? oldValue);
            bool existsAfter = after.Values.TryGetValue(key, out JToken? newValue);

            if (existedBefore && existsAfter && JToken.DeepEquals(oldValue, newValue))
            {
                continue;
            }

            ProfileImpactMutationKind kind = (existedBefore, existsAfter) switch
            {
                (false, true) => ProfileImpactMutationKind.Created,
                (true, false) => ProfileImpactMutationKind.Removed,
                (true, true) => ProfileImpactMutationKind.Updated,
                _ => throw new ProfileImpactEvaluationIntegrityException(
                    "A recorded profile mutation has no baseline or final property.")
            };

            leaves.Add(new ProfileImpactLeafModel
            {
                SheetName = key.SheetName,
                EntryId = key.EntryId,
                PropertyPath = key.PropertyPath,
                MutationKind = kind
            });
        }

        return leaves;
    }

    private static LeafKey CreateKey(
        EntryModel entry,
        PropertyModel property,
        string? propertyPath = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(property);

        JToken? sourceId = entry.SourceEntry?["id"];
        string entryId = sourceId?.Type is JTokenType.String or
            JTokenType.Integer
                ? sourceId.ToString()
                : string.Empty;
        string path = string.IsNullOrWhiteSpace(propertyPath)
            ? property.EffectivePropertyPath
            : propertyPath;

        if (string.IsNullOrWhiteSpace(property.SheetName) ||
            string.IsNullOrWhiteSpace(entryId) ||
            string.IsNullOrWhiteSpace(path))
        {
            throw new ProfileImpactEvaluationIntegrityException(
                "A changed property does not have a stable canonical identity.");
        }

        return new LeafKey(property.SheetName, entryId, path);
    }

    private sealed class ProjectLeafIndex
    {
        private readonly Dictionary<PropertyModel, LeafKey> keysByProperty;

        private ProjectLeafIndex(
            Dictionary<LeafKey, JToken> values,
            Dictionary<PropertyModel, LeafKey> keysByProperty)
        {
            Values = values;
            this.keysByProperty = keysByProperty;
        }

        public IReadOnlyDictionary<LeafKey, JToken> Values { get; }

        public bool TryGetKey(PropertyModel property, out LeafKey key) =>
            keysByProperty.TryGetValue(property, out key!);

        public static ProjectLeafIndex Create(ProjectModel project)
        {
            Dictionary<LeafKey, JToken> values = new();
            Dictionary<PropertyModel, LeafKey> keys = new();

            foreach (SheetModel sheet in project.Sheets)
            {
                foreach (EntryModel entry in sheet.Entries)
                {
                    JToken? sourceId = entry.SourceEntry?["id"];
                    if (sourceId?.Type is not (JTokenType.String or
                        JTokenType.Integer))
                    {
                        continue;
                    }

                    foreach (PropertyModel property in entry.Properties)
                    {
                        LeafKey key = CreateKey(entry, property);
                        if (!values.TryAdd(key, property.GetCurrentValueSnapshot()))
                        {
                            throw new ProfileImpactEvaluationIntegrityException(
                                $"Canonical profile property '{key.Identity}' is ambiguous.");
                        }

                        keys.Add(property, key);
                    }
                }
            }

            return new ProjectLeafIndex(values, keys);
        }
    }

    private sealed record LeafKey(
        string SheetName,
        string EntryId,
        string PropertyPath)
    {
        public string Identity =>
            $"{SheetName}\u001f{EntryId}\u001f{PropertyPath}";
    }
}
