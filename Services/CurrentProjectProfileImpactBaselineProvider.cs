using System;
using System.IO;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class CurrentProjectProfileImpactBaselineProvider
    : IProfileImpactBaselineProvider
{
    private readonly CdbGenerationIdentityService identityService = new();

    public bool TryGetBaseline(
        ProjectModel sourceProject,
        string requiredSourceIdentity,
        out ProfileImpactBaseline? baseline)
    {
        ArgumentNullException.ThrowIfNull(sourceProject);
        baseline = null;

        if (sourceProject.SourceProvenanceStatus !=
                SourceProvenanceStatus.Verified ||
            !identityService.AreEqual(
                sourceProject.SourceCdbGenerationIdentity,
                requiredSourceIdentity) ||
            string.IsNullOrWhiteSpace(sourceProject.FileName))
        {
            return false;
        }

        try
        {
            string path = Path.GetFullPath(sourceProject.FileName);
            if (!File.Exists(path))
            {
                return false;
            }

            byte[] exactBytes = File.ReadAllBytes(path);
            string actualIdentity = identityService.Calculate(exactBytes);
            if (!identityService.AreEqual(actualIdentity, requiredSourceIdentity))
            {
                return false;
            }

            baseline = new ProfileImpactBaseline(
                exactBytes,
                actualIdentity,
                "Current persisted pristine source");
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
            ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
