using WartalesEditor.Models;

namespace WartalesEditor.Services;

public interface IProfileImpactBaselineProvider
{
    bool TryGetBaseline(
        ProjectModel sourceProject,
        string requiredSourceIdentity,
        out ProfileImpactBaseline? baseline);
}

public sealed record ProfileImpactBaseline(
    byte[] ExactBytes,
    string SourceIdentity,
    string ProviderName);
