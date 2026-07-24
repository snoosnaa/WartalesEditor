using System;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ProfileOperationResolver
{
    private readonly AddCampFacilitiesOperation addCampOperation;
    private readonly UpgradeAllEquipmentOperation upgradeOperation;

    public ProfileOperationResolver(
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation)
    {
        this.addCampOperation = addCampOperation
            ?? throw new ArgumentNullException(
                nameof(addCampOperation));
        this.upgradeOperation = upgradeOperation
            ?? throw new ArgumentNullException(
                nameof(upgradeOperation));
    }

    public IProjectOperation Resolve(string operationId)
    {
        return operationId switch
        {
            ProfileOperationIds.AddCampFacilities =>
                addCampOperation,
            ProfileOperationIds.UpgradeAllEquipment =>
                upgradeOperation,
            _ => throw new InvalidOperationException(
                $"The profile requests an unsupported gameplay " +
                $"tool '{operationId}'.")
        };
    }
}
