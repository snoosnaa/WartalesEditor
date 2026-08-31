using System;
using System.Linq;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ProfileOperationResolver
{
    private readonly AddCampFacilitiesOperation addCampOperation;
    private readonly UpgradeAllEquipmentOperation upgradeOperation;
    private readonly RequestBoardRewardsService requestBoardRewardsService;

    public ProfileOperationResolver(
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation)
        : this(
            addCampOperation,
            upgradeOperation,
            CreateDefaultRequestBoardRewardsService())
    {
    }

    public ProfileOperationResolver(
        AddCampFacilitiesOperation addCampOperation,
        UpgradeAllEquipmentOperation upgradeOperation,
        RequestBoardRewardsService requestBoardRewardsService)
    {
        this.addCampOperation = addCampOperation
            ?? throw new ArgumentNullException(
                nameof(addCampOperation));
        this.upgradeOperation = upgradeOperation
            ?? throw new ArgumentNullException(
                nameof(upgradeOperation));
        this.requestBoardRewardsService = requestBoardRewardsService
            ?? throw new ArgumentNullException(
                nameof(requestBoardRewardsService));
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

    public IProjectOperation Resolve(ProfileOperationRequestModel request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OperationId != ProfileOperationIds.RequestBoardRewards)
            return Resolve(request.OperationId);

        if (request.Settings?["percentage"]?.Type !=
                Newtonsoft.Json.Linq.JTokenType.Integer ||
            request.Settings.Properties().Count() != 1)
        {
            throw new InvalidOperationException(
                "The profile's Request Board Rewards preset is invalid.");
        }

        int percentage = request.Settings["percentage"]!.ToObject<int>();
        RequestBoardRewardsService.ValidateProfilePercentage(percentage);
        return new RequestBoardRewardsOperation(
            requestBoardRewardsService,
            percentage);
    }

    private static RequestBoardRewardsService
        CreateDefaultRequestBoardRewardsService()
    {
        ProjectMutationService mutationService = new();
        return new RequestBoardRewardsService(
            mutationService,
            new GameplayOperationStateService(mutationService));
    }
}
