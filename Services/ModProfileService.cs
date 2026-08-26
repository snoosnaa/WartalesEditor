using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ModProfileService
{
    private readonly ModificationSnapshotService
        snapshotService;

    private readonly ProfileOperationCaptureService
        operationCaptureService;

    private readonly ProfileSnapshotReconciliationService
        reconciliationService;

    private readonly GameplayOperationStateService
        gameplayOperationStateService = new();

    public ModProfileService()
        : this(
            new ModificationSnapshotService(),
            ProfileOperationCaptureService.CreateDefault(),
            new ProfileSnapshotReconciliationService())
    {
    }

    public ModProfileService(
        ModificationSnapshotService snapshotService)
        : this(
            snapshotService,
            ProfileOperationCaptureService.CreateDefault(),
            new ProfileSnapshotReconciliationService())
    {
    }

    public ModProfileService(
        ModificationSnapshotService snapshotService,
        ProfileOperationCaptureService
            operationCaptureService)
        : this(
            snapshotService,
            operationCaptureService,
            new ProfileSnapshotReconciliationService())
    {
    }

    public ModProfileService(
        ModificationSnapshotService snapshotService,
        ProfileOperationCaptureService operationCaptureService,
        ProfileSnapshotReconciliationService reconciliationService)
    {
        this.snapshotService =
            snapshotService
            ?? throw new ArgumentNullException(
                nameof(snapshotService));

        this.operationCaptureService =
            operationCaptureService
            ?? throw new ArgumentNullException(
                nameof(operationCaptureService));

        this.reconciliationService =
            reconciliationService
            ?? throw new ArgumentNullException(
                nameof(reconciliationService));
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

        System.Collections.Generic.IReadOnlyList<
            ProfileOperationRequestModel>
            operationRequests =
                operationCaptureService.Capture(
                    project,
                    snapshot);

        return new ModProfileModel
        {
            SourceCdbGenerationIdentity =
                project.SourceCdbGenerationIdentity,

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
            ,

            OperationRequests =
                System.Linq.Enumerable.ToList(
                    operationRequests)
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

    public ModProfileModel CreateUpdatedProfile(
        ProjectModel project,
        ModProfileModel existingProfile,
        string editorVersion = "")
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(existingProfile);

        ModProfileMetadataModel existingMetadata =
            existingProfile.Metadata
            ?? throw new InvalidOperationException(
                "The selected profile does not contain metadata.");

        gameplayOperationStateService.ValidateProjectStates(project);

        ModProfileModel captured = CreateProfile(
            project,
            existingMetadata.Name,
            existingMetadata.Description,
            existingMetadata.Author,
            existingMetadata.ProfileVersion,
            editorVersion);

        reconciliationService.Reconcile(
            project,
            existingProfile.Snapshot,
            captured.Snapshot);

        DateTimeOffset modifiedAt = captured.Metadata.ModifiedAtUtc <
            existingMetadata.CreatedAtUtc
                ? existingMetadata.CreatedAtUtc
                : captured.Metadata.ModifiedAtUtc;

        return new ModProfileModel
        {
            FormatVersion = ModProfileFormat.CurrentVersion,
            SourceCdbGenerationIdentity =
                captured.SourceCdbGenerationIdentity,
            Metadata = new ModProfileMetadataModel
            {
                Name = existingMetadata.Name,
                Description = existingMetadata.Description,
                Author = existingMetadata.Author,
                ProfileVersion = existingMetadata.ProfileVersion,
                CreatedAtUtc = existingMetadata.CreatedAtUtc,
                ModifiedAtUtc = modifiedAt,
                Tags = new(existingMetadata.Tags)
            },
            Snapshot = captured.Snapshot,
            OperationRequests = captured.OperationRequests
        };
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

            SourceCdbGenerationIdentity =
                profile.SourceCdbGenerationIdentity,

            OperationRequests =
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.Select(
                        profile.OperationRequests,
                        request =>
                        new ProfileOperationRequestModel
                        {
                            FormatVersion =
                                request.FormatVersion,
                            OperationId =
                                request.OperationId,
                            Settings =
                                (Newtonsoft.Json.Linq.JObject?)
                                    request.Settings?.DeepClone()
                        })),

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
