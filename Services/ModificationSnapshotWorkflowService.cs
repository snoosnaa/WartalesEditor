using System;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModificationSnapshotWorkflowService
{
    private readonly ModificationSnapshotService
        snapshotService;

    private readonly ModificationSnapshotSerializationService
        serializationService;

    private readonly ModificationSnapshotMatcher
        matcher;

    private readonly ModificationSnapshotPreviewService
        previewService;

    private readonly ModificationSnapshotApplyService
        applyService;

    private readonly GameplayOperationStateService
        gameplayOperationStateService;

    public ModificationSnapshotWorkflowService()
        : this(
            new ModificationSnapshotService(),
            new ModificationSnapshotSerializationService(),
            new ModificationSnapshotMatcher(),
            new ModificationSnapshotPreviewService(),
            new ModificationSnapshotApplyService(),
            new GameplayOperationStateService())
    {
    }

    public ModificationSnapshotWorkflowService(
        ModificationSnapshotService snapshotService,
        ModificationSnapshotSerializationService
            serializationService,
        ModificationSnapshotMatcher matcher,
        ModificationSnapshotPreviewService previewService,
        ModificationSnapshotApplyService applyService)
        : this(
            snapshotService,
            serializationService,
            matcher,
            previewService,
            applyService,
            new GameplayOperationStateService())
    {
    }

    public ModificationSnapshotWorkflowService(
        ModificationSnapshotService snapshotService,
        ModificationSnapshotSerializationService
            serializationService,
        ModificationSnapshotMatcher matcher,
        ModificationSnapshotPreviewService previewService,
        ModificationSnapshotApplyService applyService,
        GameplayOperationStateService
            gameplayOperationStateService)
    {
        this.snapshotService =
            snapshotService
            ?? throw new ArgumentNullException(
                nameof(snapshotService));

        this.serializationService =
            serializationService
            ?? throw new ArgumentNullException(
                nameof(serializationService));

        this.matcher =
            matcher
            ?? throw new ArgumentNullException(
                nameof(matcher));

        this.previewService =
            previewService
            ?? throw new ArgumentNullException(
                nameof(previewService));

        this.applyService =
            applyService
            ?? throw new ArgumentNullException(
                nameof(applyService));

        this.gameplayOperationStateService =
            gameplayOperationStateService
            ?? throw new ArgumentNullException(
                nameof(gameplayOperationStateService));
    }

    public ModificationSnapshotExportResultModel
        Export(
            ProjectModel project,
            string fileName,
            string editorVersion = "")
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A snapshot file name is required.",
                nameof(fileName));
        }

        ModificationSnapshotModel snapshot =
            snapshotService.CreateSnapshot(
                project,
                editorVersion);

        serializationService.Save(
            snapshot,
            fileName);

        return new ModificationSnapshotExportResultModel(
            snapshot,
            fileName);
    }

    public ModificationSnapshotModel Load(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A snapshot file name is required.",
                nameof(fileName));
        }

        return serializationService.Load(
            fileName);
    }

    public ModificationMatchResultModel Match(
        ProjectModel targetProject,
        ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            snapshot);

        return matcher.Match(
            targetProject,
            snapshot);
    }

    public ModificationPreviewResultModel Preview(
        ProjectModel targetProject,
        ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            snapshot);

        ModificationMatchResultModel matchResult =
            matcher.Match(
                targetProject,
                snapshot);

        return previewService.Preview(
            matchResult);
    }

    public ModificationSnapshotImportResultModel
        ApplySafely(
            ProjectModel targetProject,
            ModificationSnapshotModel snapshot,
            string sourceName = "")
    {
        return ApplySafely(
            targetProject,
            snapshot,
            sourceName,
            snapshot.SourceCdbGenerationIdentity,
            snapshot.FormatVersion == ModificationSnapshotFormat.CurrentVersion);
    }

    internal ModificationSnapshotImportResultModel
        ApplySafely(
            ProjectModel targetProject,
            ModificationSnapshotModel snapshot,
            string sourceName,
            string? portableRootSourceIdentity,
            bool portableFormatSupportsProvenance)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        ArgumentNullException.ThrowIfNull(
            snapshot);

        ProjectMutationResult mutationResult =
            new();

        try
        {
            GameplayOperationStateModel[] portableStates =
                snapshot.GameplayOperationStates
                    .Where(state =>
                        CanTransportGameplayState(
                            targetProject,
                            snapshot,
                            portableRootSourceIdentity,
                            portableFormatSupportsProvenance,
                            state))
                    .ToArray();

            GameplayOperationStateModel? randomTraitState =
                portableStates
                    .SingleOrDefault(state =>
                        state.OperationType ==
                        ProgressionType.RandomTraitExclusions);

            if (randomTraitState != null)
            {
                try
                {
                    RandomTraitExclusionsService exclusionsService =
                        new(
                            new ProjectMutationService(),
                            gameplayOperationStateService);

                    mutationResult.Merge(
                        exclusionsService.RestoreState(
                            targetProject,
                            randomTraitState));
                }
                catch (InvalidOperationException)
                {
                    // Portable previous-value history is optional. Ordinary
                    // snapshot changes remain independently reviewable.
                }
            }

            ModificationMatchResultModel matchResult =
                matcher.Match(
                    targetProject,
                    snapshot);

            ModificationPreviewResultModel previewResult =
                previewService.Preview(
                    matchResult);

            ModificationApplyResultModel applyResult =
                applyService.Apply(
                    previewResult);

            mutationResult.Merge(
                applyResult.MutationResult);

            if (portableStates.Length > 0)
            {
                GameplayOperationStateModel[] remainingStates =
                    portableStates
                        .Where(state =>
                            state.OperationType !=
                            ProgressionType.RandomTraitExclusions)
                        .ToArray();

                if (remainingStates.Length > 0)
                {
                    try
                    {
                        mutationResult.Merge(
                            gameplayOperationStateService
                                .RestoreSnapshotStatesWithMutations(
                                    targetProject,
                                    remainingStates));
                    }
                    catch (InvalidOperationException)
                    {
                        // Do not make optional previous-value transport an
                        // all-or-nothing gate for ordinary profile changes.
                    }
                }
            }
            else
            {
                gameplayOperationStateService.ValidateProjectStates(
                    targetProject);
            }

            return new ModificationSnapshotImportResultModel(
                snapshot,
                matchResult,
                previewResult,
                applyResult,
                sourceName,
                System.Array.Empty<
                    Models.Profiles
                        .ProfileOperationApplyItemResultModel>(),
                mutationResult);
        }
        catch
        {
            if (mutationResult.WasModified)
            {
                new Operations.ProjectOperationTransactionService()
                    .Rollback(mutationResult);
            }

            throw;
        }
    }

    private bool CanTransportGameplayState(
        ProjectModel targetProject,
        ModificationSnapshotModel snapshot,
        string? portableRootSourceIdentity,
        bool portableFormatSupportsProvenance,
        GameplayOperationStateModel state)
    {
        CdbGenerationIdentityService identities = new();

        if (!portableFormatSupportsProvenance ||
            snapshot.FormatVersion != ModificationSnapshotFormat.CurrentVersion ||
            !identities.IsValid(portableRootSourceIdentity) ||
            !identities.AreEqual(
                portableRootSourceIdentity,
                snapshot.SourceCdbGenerationIdentity) ||
            !identities.IsValid(state.ProjectCompatibilityIdentity) ||
            !identities.AreEqual(
                portableRootSourceIdentity,
                state.ProjectCompatibilityIdentity) ||
            targetProject.SourceProvenanceStatus !=
                SourceProvenanceStatus.Verified ||
            !identities.AreEqual(
                targetProject.SourceCdbGenerationIdentity,
                portableRootSourceIdentity))
        {
            return false;
        }

        return true;
    }

    public ModificationSnapshotImportResultModel
        ImportAndApplySafely(
            ProjectModel targetProject,
            string fileName)
    {
        ArgumentNullException.ThrowIfNull(
            targetProject);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A snapshot file name is required.",
                nameof(fileName));
        }

        ModificationSnapshotModel snapshot =
            serializationService.Load(
                fileName);

        return ApplySafely(
            targetProject,
            snapshot,
            fileName);
    }
}
