using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public interface IProjectOperation
{
    string Name
    {
        get;
    }

    string Description
    {
        get;
    }

    bool CanExecute(
        ProjectModel project);

    ProjectOperationResult Execute(
        ProjectModel project);
}