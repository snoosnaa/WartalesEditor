using System;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;

namespace WartalesEditor.Services;

public sealed class ProfileOperationReplayBaselineService
{
    private readonly CdbGenerationIdentityService identityService = new();
    private readonly GameplayOperationStateService stateService;
    private readonly ProfileOperationIntentRegistry intentRegistry;

    public ProfileOperationReplayBaselineService()
        : this(
            new GameplayOperationStateService(),
            new ProfileOperationIntentRegistry())
    {
    }

    internal ProfileOperationReplayBaselineService(
        GameplayOperationStateService stateService,
        ProfileOperationIntentRegistry intentRegistry)
    {
        this.stateService = stateService
            ?? throw new ArgumentNullException(nameof(stateService));
        this.intentRegistry = intentRegistry
            ?? throw new ArgumentNullException(nameof(intentRegistry));
    }

    public JArray? SelectExactSourceBaseline(
        ProjectModel targetProject,
        ProgressionType operationType,
        GameplayOperationStateModel? exactSourceSeed)
    {
        return SelectExactSourceBaselineCore(
            targetProject,
            operationType,
            exactSourceSeed);
    }

    internal JArray? SelectExactSourceBaseline(
        ProjectModel targetProject,
        ProgressionType operationType,
        GameplayOperationStateModel? exactSourceSeed,
        ProfileOperationRequestModel intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        JArray? baseline = SelectExactSourceBaselineCore(
            targetProject,
            operationType,
            exactSourceSeed);

        if (baseline != null)
        {
            intentRegistry.ValidateStateMatchesRequest(
                exactSourceSeed!,
                intent);
        }

        return baseline;
    }

    private JArray? SelectExactSourceBaselineCore(
        ProjectModel targetProject,
        ProgressionType operationType,
        GameplayOperationStateModel? exactSourceSeed)
    {
        ArgumentNullException.ThrowIfNull(targetProject);

        if (targetProject.SourceProvenanceStatus !=
                SourceProvenanceStatus.Verified ||
            !identityService.IsValid(
                targetProject.SourceCdbGenerationIdentity))
        {
            throw new InvalidOperationException(
                "Stateful profile replay requires a verified target " +
                "source identity.");
        }

        if (exactSourceSeed == null)
        {
            return null;
        }

        if (exactSourceSeed.OperationType != operationType)
        {
            throw new InvalidOperationException(
                $"The supplied replay baseline belongs to " +
                $"'{exactSourceSeed.OperationType}', not '{operationType}'.");
        }

        if (!identityService.AreEqual(
                targetProject.SourceCdbGenerationIdentity,
                exactSourceSeed.ProjectCompatibilityIdentity))
        {
            return null;
        }

        if (exactSourceSeed.FormatVersion !=
                GameplayOperationStateModel.CurrentFormatVersion ||
            exactSourceSeed.BaselineArray.Count !=
                exactSourceSeed.ElementCount ||
            !string.Equals(
                GameplayOperationFingerprintService
                    .CreateContentFingerprint(
                        exactSourceSeed.BaselineArray),
                exactSourceSeed.BaselineFingerprint,
                StringComparison.Ordinal) ||
            !string.Equals(
                GameplayOperationFingerprintService
                    .CreateShapeFingerprint(
                        exactSourceSeed.BaselineArray),
                exactSourceSeed.ElementShapeFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The exact-source replay baseline is malformed.");
        }

        GameplayOperationStateModel validatedSeed =
            exactSourceSeed.DeepClone();
        stateService.ValidateState(targetProject, validatedSeed);
        if (!validatedSeed.IsCompatible)
        {
            throw new InvalidOperationException(
                validatedSeed.CompatibilityMessage);
        }

        return (JArray)exactSourceSeed.BaselineArray.DeepClone();
    }
}
