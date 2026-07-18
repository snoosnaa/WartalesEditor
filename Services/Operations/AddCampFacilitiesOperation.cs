using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class AddCampFacilitiesOperation
    : IProjectOperation
{
    private readonly ContentCreationService
        contentCreationService;

    public AddCampFacilitiesOperation(
        ContentCreationService contentCreationService)
    {
        ArgumentNullException.ThrowIfNull(
            contentCreationService);

        this.contentCreationService =
            contentCreationService;
    }

    public string Name =>
        "Add Camp Facilities";

    public string Description =>
        "Enables the Anvil and Apothecary Table " +
        "and adds their Workshop crafting recipes.";

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
            contentCreationService.AddCampFacilities(
                project);

        string message =
            mutationResult.WasModified
                ? "Camp facilities were added successfully."
                : "The camp facilities were already configured.";

        return ProjectOperationResult.Success(
            mutationResult,
            message);
    }
}
