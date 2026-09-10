using System;
using System.Collections.Generic;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class ProfileImpactBaselineResolver
{
    private readonly IReadOnlyList<IProfileImpactBaselineProvider> providers;
    private readonly CdbGenerationIdentityService identityService = new();

    public ProfileImpactBaselineResolver(
        IEnumerable<IProfileImpactBaselineProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        this.providers = new List<IProfileImpactBaselineProvider>(providers);
    }

    public static ProfileImpactBaselineResolver CreateDefault()
    {
        JsonDataService jsonDataService = new();
        return new ProfileImpactBaselineResolver(new IProfileImpactBaselineProvider[]
        {
            new CurrentProjectProfileImpactBaselineProvider(),
            new GoldenProfileImpactBaselineProvider(
                new GoldenCdbService(jsonDataService))
        });
    }

    public bool TryResolve(
        ProjectModel sourceProject,
        string? requiredSourceIdentity,
        out ProfileImpactBaseline? baseline)
    {
        ArgumentNullException.ThrowIfNull(sourceProject);
        baseline = null;

        if (!identityService.IsValid(requiredSourceIdentity))
        {
            return false;
        }

        foreach (IProfileImpactBaselineProvider provider in providers)
        {
            if (!provider.TryGetBaseline(
                    sourceProject,
                    requiredSourceIdentity!,
                    out ProfileImpactBaseline? candidate) ||
                candidate == null)
            {
                continue;
            }

            string actualIdentity = identityService.Calculate(candidate.ExactBytes);
            if (!identityService.AreEqual(actualIdentity, requiredSourceIdentity) ||
                !identityService.AreEqual(candidate.SourceIdentity, actualIdentity))
            {
                continue;
            }

            baseline = candidate with
            {
                ExactBytes = (byte[])candidate.ExactBytes.Clone(),
                SourceIdentity = actualIdentity
            };
            return true;
        }

        return false;
    }
}
