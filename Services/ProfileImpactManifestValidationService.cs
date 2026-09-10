using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models.Profiles;

namespace WartalesEditor.Services;

public sealed class ProfileImpactManifestValidationService
{
    private readonly CdbGenerationIdentityService identityService = new();
    private readonly ProfileGameplayContentIdentityService contentIdentityService;

    public ProfileImpactManifestValidationService()
        : this(new ProfileGameplayContentIdentityService())
    {
    }

    public ProfileImpactManifestValidationService(
        ProfileGameplayContentIdentityService contentIdentityService)
    {
        this.contentIdentityService = contentIdentityService
            ?? throw new ArgumentNullException(nameof(contentIdentityService));
    }

    public bool TryValidate(
        ModProfileModel profile,
        ProfileImpactManifestModel? manifest,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(profile);
        error = string.Empty;

        if (manifest == null)
        {
            error = "The profile has no established impact manifest.";
            return false;
        }

        if (profile.FormatVersion < ModProfileFormat.ProfileImpactManifestVersion)
        {
            error = "This profile format cannot contain an impact manifest.";
            return false;
        }

        if (manifest.FormatVersion !=
                ProfileImpactManifestModel.CurrentFormatVersion ||
            manifest.ImpactSemanticsVersion !=
                ProfileImpactManifestModel.CurrentImpactSemanticsVersion)
        {
            error = "The profile impact manifest uses an unsupported version.";
            return false;
        }

        if (!identityService.IsValid(manifest.SourceCdbGenerationIdentity) ||
            !identityService.AreEqual(
                manifest.SourceCdbGenerationIdentity,
                profile.SourceCdbGenerationIdentity))
        {
            error = "The profile impact manifest has no valid source identity.";
            return false;
        }

        string expectedContentIdentity = contentIdentityService.Calculate(profile);
        if (!identityService.AreEqual(
                manifest.ProfileGameplayContentIdentity,
                expectedContentIdentity))
        {
            error = "The profile gameplay content no longer matches its impact manifest.";
            return false;
        }

        if (manifest.Leaves == null || manifest.TotalCount < 0 ||
            manifest.TotalCount != manifest.Leaves.Count)
        {
            error = "The profile impact manifest count is inconsistent.";
            return false;
        }

        string? previousIdentity = null;
        foreach (ProfileImpactLeafModel? leaf in manifest.Leaves)
        {
            if (leaf == null ||
                !IsValidIdentityComponent(leaf.SheetName) ||
                !IsValidIdentityComponent(leaf.EntryId) ||
                !IsValidIdentityComponent(leaf.PropertyPath) ||
                !Enum.IsDefined(leaf.MutationKind))
            {
                error = "The profile impact manifest contains an invalid leaf.";
                return false;
            }

            string identity = CreateLeafIdentity(leaf);
            if (previousIdentity != null &&
                StringComparer.Ordinal.Compare(previousIdentity, identity) >= 0)
            {
                error = "The profile impact manifest contains duplicate or unordered leaves.";
                return false;
            }

            previousIdentity = identity;
        }

        if (manifest.EstablishedAtUtc == default ||
            string.IsNullOrWhiteSpace(manifest.EstablishedByEditorVersion))
        {
            error = "The profile impact manifest has incomplete establishment details.";
            return false;
        }

        string expectedEvidence = CalculateEvidenceFingerprint(manifest);
        if (!identityService.AreEqual(
                manifest.EvidenceFingerprint,
                expectedEvidence))
        {
            error = "The profile impact manifest evidence is inconsistent.";
            return false;
        }

        return true;
    }

    public string CalculateEvidenceFingerprint(
        ProfileImpactManifestModel manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        JObject evidence = new()
        {
            ["formatVersion"] = manifest.FormatVersion,
            ["impactSemanticsVersion"] = manifest.ImpactSemanticsVersion,
            ["sourceCdbGenerationIdentity"] =
                manifest.SourceCdbGenerationIdentity,
            ["profileGameplayContentIdentity"] =
                manifest.ProfileGameplayContentIdentity,
            ["totalCount"] = manifest.TotalCount,
            ["leaves"] = new JArray(manifest.Leaves.Select(leaf => new JObject
            {
                ["sheetName"] = leaf.SheetName,
                ["entryId"] = leaf.EntryId,
                ["propertyPath"] = leaf.PropertyPath,
                ["mutationKind"] = leaf.MutationKind.ToString()
            }))
        };

        string canonical = ProfileGameplayContentIdentityService
            .Canonicalize(evidence)
            .ToString(Formatting.None);
        return identityService.Calculate(Encoding.UTF8.GetBytes(canonical));
    }

    internal static string CreateLeafIdentity(ProfileImpactLeafModel leaf) =>
        $"{leaf.SheetName}\u001f{leaf.EntryId}\u001f{leaf.PropertyPath}";

    private static bool IsValidIdentityComponent(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.IndexOf('\u001f') < 0;
}
