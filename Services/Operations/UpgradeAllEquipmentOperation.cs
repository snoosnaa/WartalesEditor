using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class UpgradeAllEquipmentOperation :
    IProjectOperation, IContextualProjectOperation
{
    private readonly ContentCreationService
        contentCreationService;

    public UpgradeAllEquipmentOperation(
        ContentCreationService contentCreationService)
    {
        ArgumentNullException.ThrowIfNull(
            contentCreationService);

        this.contentCreationService =
            contentCreationService;
    }

    public string Name =>
        "Upgrade All Equipment";

    public string Description =>
        "Allows normal obtainable equipment that is not " +
        "normally upgradeable to be upgraded at the " +
        "Brotherhood Training Grounds.";

    public bool CanExecute(
        ProjectModel project)
    {
        return project != null;
    }

    public ProjectOperationResult Execute(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        ProjectMutationResult mutationResult =
            contentCreationService
                .UpgradeAllEquipment(
                    project);

        string message =
            mutationResult.WasModified
                ? "Eligible equipment was made upgradeable."
                : "All eligible equipment was already upgradeable.";

        return ProjectOperationResult.Success(
            mutationResult,
            message);
    }

    public void Preflight(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        contentCreationService.ValidateUpgradeAllEquipmentCompatibility(project);
    }

    public ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(context);
        ProjectMutationResult mutationResult =
            contentCreationService.UpgradeAllEquipment(project, context);
        return ProjectOperationResult.Success(
            mutationResult,
            mutationResult.WasModified
                ? "Eligible equipment was made upgradeable."
                : "All eligible equipment was already upgradeable.");
    }
}
