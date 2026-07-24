using System;
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
