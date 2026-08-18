using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;

namespace WartalesEditor.Services;

public sealed class ProfileEffectiveChangeCountService
{
    private readonly EffectiveChangeCountService accountingService;

    public ProfileEffectiveChangeCountService()
        : this(new CampFacilityJsonBuilder())
    {
    }

    public ProfileEffectiveChangeCountService(
        CampFacilityJsonBuilder campBuilder)
    {
        accountingService = new EffectiveChangeCountService(
            campBuilder ?? throw new ArgumentNullException(nameof(campBuilder)));
    }

    public int Calculate(ModProfileModel profile) =>
        accountingService.Calculate(profile);

    public int Calculate(ProjectModel project) =>
        accountingService.Calculate(project);

    public int Calculate(ProjectMutationResult mutationResult) =>
        accountingService.Calculate(mutationResult);

    public bool HasUnrepresentedRandomTraitExclusionChange(
        ProjectModel project) =>
        accountingService.HasUnrepresentedRandomTraitExclusionChange(project);
}
