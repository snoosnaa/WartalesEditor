using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Models.Operations;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ModProfileWorkflowService
{
    private readonly ModProfileService
        profileService;

    private readonly ModProfileSerializationService
        serializationService;

    private readonly ModificationSnapshotWorkflowService
        snapshotWorkflowService;

    private readonly ProfileOperationResolver
        operationResolver;

    private readonly ProjectOperationService
        projectOperationService;

    private readonly ProjectOperationTransactionService
        transactionService;

    private readonly ProfileEffectiveChangeCountService
        effectiveChangeCountService =
            new();

    private readonly UpdatedProfileCandidateValidationService
        updatedProfileCandidateValidationService =
            new();

    public ModProfileWorkflowService()
        : this(
            new ModProfileService(),
            new ModProfileSerializationService(),
            new ModificationSnapshotWorkflowService(),
            CreateDefaultResolver(),
            new ProjectOperationService(),
            new ProjectOperationTransactionService())
    {
    }

    public ModProfileWorkflowService(
        ModProfileService profileService,
        ModProfileSerializationService
            serializationService,
        ModificationSnapshotWorkflowService
            snapshotWorkflowService)
        : this(
            profileService,
            serializationService,
            snapshotWorkflowService,
            CreateDefaultResolver(),
            new ProjectOperationService(),
            new ProjectOperationTransactionService())
    {
    }

    public ModProfileWorkflowService(
        ModProfileService profileService,
        ModProfileSerializationService
            serializationService,
        ModificationSnapshotWorkflowService
            snapshotWorkflowService,
        ProfileOperationResolver operationResolver,
        ProjectOperationService projectOperationService,
        ProjectOperationTransactionService transactionService)
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

        this.operationResolver =
            operationResolver
            ?? throw new ArgumentNullException(
                nameof(operationResolver));

        this.projectOperationService =
            projectOperationService
            ?? throw new ArgumentNullException(
                nameof(projectOperationService));

        this.transactionService =
            transactionService
            ?? throw new ArgumentNullException(
                nameof(transactionService));
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

    public ModProfileModel CreateUpdatedProfile(
        ProjectModel project,
        ModProfileModel existingProfile,
        string editorVersion = "")
    {
        return profileService.CreateUpdatedProfile(
            project,
            existingProfile,
            editorVersion);
    }

    public void ValidateUpdatedProfileCandidate(
        ProjectModel intendedProject,
        ModProfileModel candidate)
    {
        ArgumentNullException.ThrowIfNull(intendedProject);
        ArgumentNullException.ThrowIfNull(candidate);

        throw new InvalidOperationException(
            "Update Profile validation requires the selected existing " +
            "profile as reconciliation input.");
    }

    public void ValidateUpdatedProfileCandidate(
        ProjectModel intendedProject,
        ModProfileModel existingProfile,
        ModProfileModel candidate)
    {
        ArgumentNullException.ThrowIfNull(intendedProject);
        ArgumentNullException.ThrowIfNull(existingProfile);
        ArgumentNullException.ThrowIfNull(candidate);

        updatedProfileCandidateValidationService.Validate(
            intendedProject,
            existingProfile,
            candidate);
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

        _ = serializationService.Serialize(
            profile);

        ModificationSnapshotModel snapshot =
            profileService.GetSnapshot(
                profile);

        List<ProfileOperationApplyItemResultModel>
            operationResults =
                new();

        ProjectMutationResult mutationResult =
            new();

        try
        {
            foreach (ProfileOperationRequestModel request in
                     OrderRequests(profile.OperationRequests))
            {
                IProjectOperation operation =
                    operationResolver.Resolve(
                        request.OperationId);

                ProjectOperationResult result =
                    projectOperationService.Execute(
                        operation,
                        targetProject);

                if (!result.Succeeded)
                {
                    operationResults.Add(
                        new ProfileOperationApplyItemResultModel(
                            request.OperationId,
                            operation.Name,
                            ProfileOperationApplyStatus.Failed,
                            result.Message ??
                                "The gameplay tool could not be applied."));

                    throw new InvalidOperationException(
                        $"{operation.Name} could not be restored." +
                        Environment.NewLine +
                        Environment.NewLine +
                        result.Message);
                }

                mutationResult.Merge(
                    result.MutationResult);

                operationResults.Add(
                    new ProfileOperationApplyItemResultModel(
                        request.OperationId,
                        operation.Name,
                        result.MutationResult.WasModified
                            ? ProfileOperationApplyStatus.Applied
                            : ProfileOperationApplyStatus
                                .AlreadyConfigured,
                        result.Message ?? string.Empty));
            }

            ModificationSnapshotImportResultModel
                snapshotResult =
                    snapshotWorkflowService.ApplySafely(
                        targetProject,
                        snapshot,
                        profile.Metadata.Name,
                        profile.SourceCdbGenerationIdentity,
                        profile.FormatVersion == ModProfileFormat.CurrentVersion);

            mutationResult.Merge(
                snapshotResult.MutationResult);

            if (snapshotResult.HasFailures)
            {
                throw new InvalidOperationException(
                    "The profile's property changes could not all " +
                    "be applied. No profile changes were kept.");
            }

            return new ModificationSnapshotImportResultModel(
                snapshotResult.Snapshot,
                snapshotResult.MatchResult,
                snapshotResult.PreviewResult,
                snapshotResult.ApplyResult,
                snapshotResult.FileName,
                operationResults,
                mutationResult,
                effectiveChangeCountService.Calculate(
                    profile));
        }
        catch
        {
            if (mutationResult.WasModified)
            {
                transactionService.Rollback(
                    mutationResult);
            }

            throw;
        }
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

    private static IReadOnlyList<
        ProfileOperationRequestModel> OrderRequests(
            IEnumerable<ProfileOperationRequestModel> requests)
    {
        return requests
            .OrderBy(request =>
                request.OperationId ==
                    ProfileOperationIds.AddCampFacilities
                    ? 0
                    : 1)
            .ToList();
    }

    private static ProfileOperationResolver
        CreateDefaultResolver()
    {
        ProjectMutationService mutationService =
            new();

        ContentCreationService contentCreationService =
            new(mutationService);

        return new ProfileOperationResolver(
            new AddCampFacilitiesOperation(
                contentCreationService),
            new UpgradeAllEquipmentOperation(
                contentCreationService));
    }
}
