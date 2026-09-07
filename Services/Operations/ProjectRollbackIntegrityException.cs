using System;

namespace WartalesEditor.Services.Operations;

public sealed class ProjectRollbackIntegrityException :
    InvalidOperationException
{
    public ProjectRollbackIntegrityException(string message)
        : base(message)
    {
    }

    public ProjectRollbackIntegrityException(
        string message,
        Exception operationException,
        Exception rollbackException)
        : base(
            message,
            new AggregateException(
                operationException,
                rollbackException))
    {
        OperationException = operationException;
        RollbackException = rollbackException;
    }

    public Exception? OperationException { get; }

    public Exception? RollbackException { get; }
}
