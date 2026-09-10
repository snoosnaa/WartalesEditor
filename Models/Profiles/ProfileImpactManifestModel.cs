using System;
using System.Collections.Generic;

namespace WartalesEditor.Models.Profiles;

public sealed class ProfileImpactManifestModel
{
    public const int CurrentFormatVersion = 1;

    public const int CurrentImpactSemanticsVersion = 1;

    public int FormatVersion { get; init; } = CurrentFormatVersion;

    public int ImpactSemanticsVersion { get; init; } =
        CurrentImpactSemanticsVersion;

    public string SourceCdbGenerationIdentity { get; init; } =
        string.Empty;

    public string ProfileGameplayContentIdentity { get; init; } =
        string.Empty;

    public int TotalCount { get; init; }

    public List<ProfileImpactLeafModel> Leaves { get; init; } = new();

    public string EvidenceFingerprint { get; set; } =
        string.Empty;

    public DateTimeOffset EstablishedAtUtc { get; init; } =
        DateTimeOffset.UtcNow;

    public string EstablishedByEditorVersion { get; init; } =
        string.Empty;

    public ProfileImpactManifestModel DeepClone() => new()
    {
        FormatVersion = FormatVersion,
        ImpactSemanticsVersion = ImpactSemanticsVersion,
        SourceCdbGenerationIdentity = SourceCdbGenerationIdentity,
        ProfileGameplayContentIdentity = ProfileGameplayContentIdentity,
        TotalCount = TotalCount,
        Leaves = Leaves.ConvertAll(leaf => leaf.DeepClone()),
        EvidenceFingerprint = EvidenceFingerprint,
        EstablishedAtUtc = EstablishedAtUtc,
        EstablishedByEditorVersion = EstablishedByEditorVersion
    };
}
