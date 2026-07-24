using System;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Operations.Rollback;

public sealed class GameplayOperationStateRollbackRecord
{
    public GameplayOperationStateRollbackRecord(
        ProjectModel project,
        GameplayOperationStateModel? previousState,
        GameplayOperationStateModel replacementState,
        bool previousStateWasModified)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(replacementState);

        Project = project;
        PreviousState = previousState?.DeepClone();
        ReplacementState = replacementState.DeepClone();
        PreviousStateWasModified = previousStateWasModified;
    }

    public ProjectModel Project { get; }

    public GameplayOperationStateModel? PreviousState { get; }

    public GameplayOperationStateModel ReplacementState { get; }

    public bool PreviousStateWasModified { get; }
}
