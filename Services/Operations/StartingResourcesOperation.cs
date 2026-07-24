using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class StartingResourcesOperation : IProjectOperation
{
    private readonly StartingResourcesService service;

    public StartingResourcesOperation(
        StartingResourcesService service,
        StartingResourcesSettings settings)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(settings);
        this.service = service;
        Settings = settings.DeepClone();
    }

    public StartingResourcesSettings Settings { get; }

    public string Name => "Starting Resources";

    public string Description =>
        "Adds the selected extra supplies to every supported new campaign start.";

    public bool CanExecute(ProjectModel project) => project != null;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ProjectMutationResult result = service.Apply(project, Settings);
        return ProjectOperationResult.Success(
            result,
            result.WasModified
                ? "Starting Resources were updated."
                : "Starting Resources already match the selected extras.");
    }
}
