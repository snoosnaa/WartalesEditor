using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModProfileWorkflowService
{
    private readonly ModProfileService
        profileService;

    private readonly ModProfileSerializationService
        serializationService;

    private readonly ModificationSnapshotWorkflowService
        snapshotWorkflowService;

    public ModProfileWorkflowService()
        : this(
            new ModProfileService(),
            new ModProfileSerializationService(),
            new ModificationSnapshotWorkflowService())
    {
    }

    public ModProfileWorkflowService(
        ModProfileService profileService,
        ModProfileSerializationService
            serializationService,
        ModificationSnapshotWorkflowService
            snapshotWorkflowService)
    {
        this.profileService =
            profileService
            ?? throw new ArgumentNullException(
                nameof(profileService));

        this.serializationService =
            serializationService
            ?? throw new ArgumentNullException(
                nameof(serializationService));

        this.snapshotWorkflowService =
            snapshotWorkflowService
            ?? throw new ArgumentNullException(
                nameof(snapshotWorkflowService));
    }

    public ModProfileModel CreateProfile(
        ProjectModel project,
        string profileName,
        string description = "",
        string author = "",
        string profileVersion = "1.0",
        string editorVersion = "")
    {
        return profileService.CreateProfile(
            project,
            profileName,
            description,
            author,
            profileVersion,
            editorVersion);
    }

    public void Save(
        ModProfileModel profile,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(profile);

        serializationService.Save(
            profile,
            fileName);
    }

    public ModProfileModel Load(
        string fileName)
    {
        return serializationService.Load(
            fileName);
    }

    public ModificationSnapshotImportResultModel
        ApplyProfile(
            ProjectModel targetProject,
            ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            profile);

        ModificationSnapshotModel snapshot =
            profileService.GetSnapshot(
                profile);

        return snapshotWorkflowService.ApplySafely(
            targetProject,
            snapshot,
            profile.Metadata.Name);
    }

    public ModificationSnapshotImportResultModel
        LoadAndApplyProfile(
            ProjectModel targetProject,
            string fileName)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ModProfileModel profile =
            Load(fileName);

        return ApplyProfile(
            targetProject,
            profile);
    }
}
