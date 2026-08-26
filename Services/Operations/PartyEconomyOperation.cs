using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class PartyEconomyOperation : IProjectOperation, IContextualProjectOperation
{
    private readonly PartyEconomyService service;

    public PartyEconomyOperation(
        PartyEconomyService service,
        ProgressionType operationType,
        PartyEconomySettings settings)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        Settings = settings?.DeepClone() ?? throw new ArgumentNullException(nameof(settings));
        if (operationType is not (ProgressionType.VolunteerWages or ProgressionType.ValourPoints or ProgressionType.CarryingCapacity))
            throw new ArgumentOutOfRangeException(nameof(operationType));
        OperationType = operationType;
    }

    public ProgressionType OperationType { get; }
    public PartyEconomySettings Settings { get; }

    public string Name => OperationType switch
    {
        ProgressionType.VolunteerWages => "Volunteer Wage Reduction",
        ProgressionType.ValourPoints => "Valour Points",
        ProgressionType.CarryingCapacity => "Carrying Capacity",
        _ => "Party Economy"
    };

    public string Description => $"Updates the selected {Name} settings.";
    public bool CanExecute(ProjectModel project) => project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ProjectMutationResult result = service.Apply(project, OperationType, Settings);
        return ProjectOperationResult.Success(result,
            result.WasModified
                ? $"{Name} settings were updated."
                : "No changes were applied." + Environment.NewLine +
                  Environment.NewLine +
                  "These settings already match the current project.");
    }

    public void Preflight(ProjectModel project) =>
        _ = PartyEconomyService.ResolveTargets(project, OperationType);

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ProjectMutationResult result = service.Apply(
            project, OperationType, Settings, context);
        return ProjectOperationResult.Success(result,
            result.WasModified
                ? $"{Name} settings were updated."
                : "No changes were applied." + Environment.NewLine + Environment.NewLine +
                  "These settings already match the current project.");
    }
}
