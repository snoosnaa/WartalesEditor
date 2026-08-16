using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

internal abstract class GameplayPresetOperationValidatorBase
    : IProjectOperationValidator
{
    protected abstract ProgressionType OperationType { get; }

    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        List<string> errors = new();
        if (operation is not GameplayPresetOperation presetOperation ||
            presetOperation.OperationType != OperationType)
            return OperationValidationResult.Failure(
                "The gameplay-tool validator received an unsupported operation.");

        try
        {
            GameplayPresetDefinition definition = GameplayPresetCatalog.Get(OperationType);
            GameplayPresetOption preset = definition.Presets.Single(x =>
                string.Equals(x.Key, presetOperation.PresetKey, StringComparison.Ordinal));
            GameplayPresetService.ValidatePreset(definition, preset);

            IReadOnlyList<ResolvedGameplayTarget> targets =
                GameplayPresetService.ResolveTargets(project, OperationType);
            GameplayOperationStateModel state =
                project.GameplayOperationStates.Single(x =>
                    x.OperationType == OperationType);
            GameplayPresetService.ValidateState(project, state);

            HashSet<PropertyModel> allowed = targets.Select(x => x.Property).ToHashSet();
            foreach (PropertyModel property in mutationResult.UpdatedProperties)
            {
                if (property.SourceProperty == null || !allowed.Contains(property))
                    errors.Add("An unrelated project value was changed.");
            }

            if (mutationResult.CreatedEntries.Count != 0 ||
                mutationResult.CreatedProperties.Count != 0 ||
                mutationResult.CreatedJsonPropertyRollbackRecords.Count != 0)
                errors.Add("The gameplay change unexpectedly created project data.");

            int distinctTargetCount = targets.Select(x => x.Property).Distinct().Count();
            if (mutationResult.UpdatedProperties.Count > distinctTargetCount)
                errors.Add("The gameplay change recorded unexpected target mutations.");

            int expectedStateChanges = mutationResult.WasModified ? 1 : 0;
            if (mutationResult.GameplayOperationStateRollbackRecords.Count !=
                expectedStateChanges)
                errors.Add("The gameplay selection was not recorded correctly.");
        }
        catch (Exception exception)
        {
            errors.Add(exception.Message);
        }

        return errors.Count == 0
            ? OperationValidationResult.Success()
            : OperationValidationResult.Failure(errors);
    }
}

internal sealed class DeliciousMealChanceOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.DeliciousMealChance; }

internal sealed class ForgingAssistanceOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.ForgingAssistance; }

internal sealed class MiningWoodcuttingTimingOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.MiningWoodcuttingTiming; }

internal sealed class FishingSpeedOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.FishingSpeed; }

internal sealed class LockpickingToleranceOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.LockpickingTolerance; }

internal sealed class NinePuzzleAssistanceOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.NinePuzzleAssistance; }

internal sealed class RunStaminaRecoveryOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.RunStaminaRecovery; }

internal sealed class BattleCameraZoomOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.BattleCameraZoom; }

internal sealed class CampfireExpansionOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.CampfireExpansion; }

internal sealed class CookingPotFoodReductionOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.CookingPotFoodReduction; }

internal sealed class WorkshopMaterialsOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.WorkshopMaterials; }

internal sealed class VendorRefreshOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.VendorRefresh; }

internal sealed class ResourceReplenishmentOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.ResourceReplenishment; }

internal sealed class RubySapphireValueOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.RubySapphireValue; }

internal sealed class TimeBetweenRestsOperationValidator
    : GameplayPresetOperationValidatorBase
{ protected override ProgressionType OperationType => ProgressionType.TimeBetweenRests; }
