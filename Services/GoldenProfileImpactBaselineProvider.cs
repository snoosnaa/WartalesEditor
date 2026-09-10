using System;
using System.IO;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class GoldenProfileImpactBaselineProvider
    : IProfileImpactBaselineProvider
{
    private readonly GoldenCdbService goldenCdbService;
    private readonly CdbGenerationIdentityService identityService = new();

    public GoldenProfileImpactBaselineProvider(
        GoldenCdbService goldenCdbService)
    {
        this.goldenCdbService = goldenCdbService
            ?? throw new ArgumentNullException(nameof(goldenCdbService));
    }

    public bool TryGetBaseline(
        ProjectModel sourceProject,
        string requiredSourceIdentity,
        out ProfileImpactBaseline? baseline)
    {
        ArgumentNullException.ThrowIfNull(sourceProject);
        baseline = null;

        try
        {
            GoldenCdbReference reference = goldenCdbService.LoadReference();
            if (!identityService.AreEqual(
                    reference.Identity,
                    requiredSourceIdentity))
            {
                return false;
            }

            byte[] exactBytes = File.ReadAllBytes(reference.CanonicalPath);
            string actualIdentity = identityService.Calculate(exactBytes);
            if (!identityService.AreEqual(actualIdentity, requiredSourceIdentity))
            {
                return false;
            }

            baseline = new ProfileImpactBaseline(
                exactBytes,
                actualIdentity,
                "Exact matching Golden CDB");
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
            Newtonsoft.Json.JsonException or ArgumentException or
            InvalidOperationException or NotSupportedException or
            PathTooLongException)
        {
            return false;
        }
    }
}
