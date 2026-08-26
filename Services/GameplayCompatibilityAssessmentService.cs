using System.Diagnostics;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class GameplayCompatibilityAssessmentService
{
    private readonly ProjectMutationService mutationService;
    private readonly ContentCreationService contentCreationService;
    private readonly IReadOnlyList<GameplayCompatibilityProbe> additionalProbes;

    public GameplayCompatibilityAssessmentService()
        : this(new ProjectMutationService())
    {
    }

    public GameplayCompatibilityAssessmentService(
        ProjectMutationService mutationService)
        : this(mutationService, Array.Empty<GameplayCompatibilityProbe>())
    {
    }

    internal GameplayCompatibilityAssessmentService(
        ProjectMutationService mutationService,
        IReadOnlyList<GameplayCompatibilityProbe> additionalProbes)
    {
        this.mutationService = mutationService ??
            throw new ArgumentNullException(nameof(mutationService));
        this.additionalProbes = additionalProbes ??
            throw new ArgumentNullException(nameof(additionalProbes));
        contentCreationService = new ContentCreationService(mutationService);
    }

    public IReadOnlyList<GameplayCompatibilityAssessment> Assess(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        List<GameplayCompatibilityAssessment> results = new();

        Probe(results, "Character XP", () =>
            _ = new ProgressionTableResolver(mutationService)
                .Resolve(project, ProgressionType.Character));
        Probe(results, "Profession XP", () =>
            _ = new ProgressionTableResolver(mutationService)
                .Resolve(project, ProgressionType.Profession));
        Probe(results, "Starting Resources", () =>
            _ = StartingResourcesService.ResolveTargets(project));
        Probe(results, "Volunteer Wages", () =>
            _ = PartyEconomyService.ResolveTargets(project, ProgressionType.VolunteerWages));
        Probe(results, "Valour Points", () =>
            _ = PartyEconomyService.ResolveTargets(project, ProgressionType.ValourPoints));
        Probe(results, "Carrying Capacity", () =>
            _ = PartyEconomyService.ResolveTargets(project, ProgressionType.CarryingCapacity));
        Probe(results, "Overworld Movement Speed", () =>
            _ = OverworldMovementSpeedService.ResolveTargets(project));
        Probe(results, "Rain Frequency", () =>
            _ = RainFrequencyService.ResolveTargets(project));
        Probe(results, "Random Trait Exclusions", () =>
            _ = RandomTraitExclusionsService.ResolveCandidateIds(project));

        foreach (ProgressionType type in Enum.GetValues<ProgressionType>()
                     .Where(GameplayPresetCatalog.IsSupported))
        {
            GameplayPresetDefinition definition = GameplayPresetCatalog.Get(type);
            Probe(results, definition.Title, () =>
                _ = GameplayPresetService.ResolveTargets(project, type));
        }

        Probe(results, "Add Camp Facilities", () =>
            contentCreationService.ValidateAddCampFacilitiesCompatibility(project));
        Probe(results, "Upgrade All Equipment", () =>
            contentCreationService.ValidateUpgradeAllEquipmentCompatibility(project));

        foreach (GameplayCompatibilityProbe probe in additionalProbes)
            Probe(results, probe.ToolName, () => probe.Action(project));

        return results.ToArray();
    }

    private static void Probe(
        ICollection<GameplayCompatibilityAssessment> results,
        string toolName,
        Action action)
    {
        try
        {
            action();
            results.Add(new GameplayCompatibilityAssessment(
                toolName,
                GameplayCompatibilityStatus.Compatible,
                "Available for this game-data version."));
        }
        catch (InvalidOperationException exception)
        {
            results.Add(new GameplayCompatibilityAssessment(
                toolName,
                Classify(exception.Message),
                exception.Message));
        }
        catch (GameplayCompatibilityException exception)
        {
            results.Add(new GameplayCompatibilityAssessment(
                toolName,
                exception.Status,
                exception.PlayerMessage));
        }
        catch (Exception exception)
        {
            Trace.WriteLine(exception);
            results.Add(new GameplayCompatibilityAssessment(
                toolName,
                GameplayCompatibilityStatus.AssessmentFailed,
                "Compatibility could not be checked for this gameplay tool."));
        }
    }

    private static GameplayCompatibilityStatus Classify(string message)
    {
        if (message.Contains("complete", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("catalog", StringComparison.OrdinalIgnoreCase))
            return GameplayCompatibilityStatus.PartiallyOutdated;
        if (message.Contains("more than one", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("exactly one", StringComparison.OrdinalIgnoreCase))
            return GameplayCompatibilityStatus.AmbiguousTarget;
        if (message.Contains("type", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("numeric", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("integer", StringComparison.OrdinalIgnoreCase))
            return GameplayCompatibilityStatus.TypeChanged;
        if (message.Contains("structure", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("array", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("object", StringComparison.OrdinalIgnoreCase))
            return GameplayCompatibilityStatus.StructureChanged;
        if (message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not available", StringComparison.OrdinalIgnoreCase))
            return GameplayCompatibilityStatus.MissingTarget;
        return GameplayCompatibilityStatus.UnsupportedStructure;
    }
}

internal sealed record GameplayCompatibilityProbe(
    string ToolName,
    Action<ProjectModel> Action);

internal sealed class GameplayCompatibilityException : Exception
{
    public GameplayCompatibilityException(
        GameplayCompatibilityStatus status,
        string playerMessage,
        string? technicalMessage = null)
        : base(technicalMessage ?? playerMessage)
    {
        Status = status;
        PlayerMessage = playerMessage;
    }

    public GameplayCompatibilityStatus Status { get; }

    public string PlayerMessage { get; }
}
