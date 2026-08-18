using System;
using System.Collections.Generic;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class OperationValidatorProvider
    : IOperationValidatorProvider
{
    private readonly AddCampFacilitiesOperationValidator
        addCampFacilitiesValidator =
            new();

    private readonly UpgradeAllEquipmentOperationValidator
        upgradeAllEquipmentValidator =
            new();

    private readonly ProgressionXpOperationValidator
        progressionXpValidator =
            new();

    private readonly StartingResourcesOperationValidator
        startingResourcesValidator =
            new();

    private readonly PartyEconomyOperationValidator
        partyEconomyValidator = new();

    private readonly OverworldMovementSpeedOperationValidator
        overworldMovementValidator = new();

    private readonly RainFrequencyOperationValidator
        rainFrequencyValidator = new();

    private readonly RandomTraitExclusionsOperationValidator
        randomTraitExclusionsValidator = new();

    private readonly IReadOnlyDictionary<ProgressionType, IProjectOperationValidator>
        gameplayPresetValidators =
            new Dictionary<ProgressionType, IProjectOperationValidator>
            {
                [ProgressionType.DeliciousMealChance] = new DeliciousMealChanceOperationValidator(),
                [ProgressionType.ForgingAssistance] = new ForgingAssistanceOperationValidator(),
                [ProgressionType.MiningWoodcuttingTiming] = new MiningWoodcuttingTimingOperationValidator(),
                [ProgressionType.FishingSpeed] = new FishingSpeedOperationValidator(),
                [ProgressionType.LockpickingTolerance] = new LockpickingToleranceOperationValidator(),
                [ProgressionType.NinePuzzleAssistance] = new NinePuzzleAssistanceOperationValidator(),
                [ProgressionType.RunStaminaRecovery] = new RunStaminaRecoveryOperationValidator(),
                [ProgressionType.BattleCameraZoom] = new BattleCameraZoomOperationValidator(),
                [ProgressionType.CampfireExpansion] = new CampfireExpansionOperationValidator(),
                [ProgressionType.CookingPotFoodReduction] = new CookingPotFoodReductionOperationValidator(),
                [ProgressionType.WorkshopMaterials] = new WorkshopMaterialsOperationValidator(),
                [ProgressionType.VendorRefresh] = new VendorRefreshOperationValidator(),
                [ProgressionType.ResourceReplenishment] = new ResourceReplenishmentOperationValidator(),
                [ProgressionType.LecternKnowledgeGain] = new LecternKnowledgeGainOperationValidator(),
                [ProgressionType.PositiveRandomTraits] = new PositiveRandomTraitsOperationValidator(),
                [ProgressionType.RubySapphireValue] = new RubySapphireValueOperationValidator(),
                [ProgressionType.TimeBetweenRests] = new TimeBetweenRestsOperationValidator()
            };

    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        ArgumentNullException.ThrowIfNull(
            project);

        ArgumentNullException.ThrowIfNull(
            mutationResult);

        IProjectOperationValidator? validator =
            operation switch
            {
                AddCampFacilitiesOperation =>
                    addCampFacilitiesValidator,

                UpgradeAllEquipmentOperation =>
                    upgradeAllEquipmentValidator,

                CharacterXpRequirementsOperation =>
                    progressionXpValidator,

                ProfessionXpRequirementsOperation =>
                    progressionXpValidator,

                StartingResourcesOperation =>
                    startingResourcesValidator,

                PartyEconomyOperation =>
                    partyEconomyValidator,

                OverworldMovementSpeedOperation =>
                    overworldMovementValidator,

                RainFrequencyOperation =>
                    rainFrequencyValidator,

                RandomTraitExclusionsOperation =>
                    randomTraitExclusionsValidator,

                GameplayPresetOperation presetOperation =>
                    gameplayPresetValidators[presetOperation.OperationType],

                _ => null
            };

        if (validator == null)
        {
            return OperationValidationResult.Success();
        }

        return validator.Validate(
            operation,
            project,
            mutationResult);
    }
}
