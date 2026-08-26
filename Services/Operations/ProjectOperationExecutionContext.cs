using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class ProjectOperationExecutionContext
{
    public ProjectMutationResult MutationResult { get; } = new();

    public void Record(ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(mutationResult);
        MutationResult.Merge(mutationResult);
    }
}

public interface IContextualProjectOperation
{
    void Preflight(ProjectModel project);

    ProjectOperationResult Execute(
        ProjectModel project,
        ProjectOperationExecutionContext context);
}
