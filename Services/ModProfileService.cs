using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModProfileService
{
    private readonly ModificationSnapshotService
        snapshotService;

    public ModProfileService()
        : this(
            new ModificationSnapshotService())
    {
    }

    public ModProfileService(
        ModificationSnapshotService snapshotService)
    {
        this.snapshotService =
            snapshotService
            ?? throw new ArgumentNullException(
                nameof(snapshotService));
    }

    public ModProfileModel CreateProfile(
        ProjectModel project,
        string profileName,
        string description = "",
        string author = "",
        string profileVersion = "1.0",
        string editorVersion = "")
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException(
                "A profile name is required.",
                nameof(profileName));
        }

        DateTimeOffset now =
            DateTimeOffset.UtcNow;

        ModificationSnapshotModel snapshot =
            snapshotService.CreateSnapshot(
                project,
                editorVersion);

        return new ModProfileModel
        {
            Metadata =
                new ModProfileMetadataModel
                {
                    Name =
                        profileName.Trim(),

                    Description =
                        description.Trim(),

                    Author =
                        author.Trim(),

                    ProfileVersion =
                        string.IsNullOrWhiteSpace(
                            profileVersion)
                        ? "1.0"
                        : profileVersion.Trim(),

                    CreatedAtUtc = now,

                    ModifiedAtUtc = now
                },

            Snapshot = snapshot
        };
    }

    public ModificationSnapshotModel
        GetSnapshot(
            ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        if (profile.Snapshot == null)
        {
            throw new InvalidOperationException(
                "The profile does not contain a modification snapshot.");
        }

        return profile.Snapshot;
    }

    public ModProfileModel
        UpdateMetadata(
            ModProfileModel profile,
            string? name = null,
            string? description = null,
            string? author = null,
            string? profileVersion = null)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        ModProfileMetadataModel existing =
            profile.Metadata;

        return new ModProfileModel
        {
            FormatVersion =
                profile.FormatVersion,

            Snapshot =
                profile.Snapshot,

            Metadata =
                new ModProfileMetadataModel
                {
                    Name =
                        string.IsNullOrWhiteSpace(name)
                            ? existing.Name
                            : name.Trim(),

                    Description =
                        description == null
                            ? existing.Description
                            : description.Trim(),

                    Author =
                        author == null
                            ? existing.Author
                            : author.Trim(),

                    ProfileVersion =
                        string.IsNullOrWhiteSpace(
                            profileVersion)
                            ? existing.ProfileVersion
                            : profileVersion.Trim(),

                    CreatedAtUtc =
                        existing.CreatedAtUtc,

                    ModifiedAtUtc =
                        DateTimeOffset.UtcNow,

                    Tags =
                        new(existing.Tags)
                }
        };
    }
}