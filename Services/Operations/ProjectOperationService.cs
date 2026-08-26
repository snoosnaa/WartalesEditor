using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class ProjectOperationService
{
    private readonly IOperationValidatorProvider
        operationValidatorProvider;

    private readonly ProjectOperationTransactionService
        transactionService;

    public ProjectOperationService()
        : this(
            new OperationValidatorProvider(),
            new ProjectOperationTransactionService())
    {
    }

    public ProjectOperationService(
        IOperationValidatorProvider operationValidatorProvider,
        ProjectOperationTransactionService transactionService)
    {
        ArgumentNullException.ThrowIfNull(
            operationValidatorProvider);

        ArgumentNullException.ThrowIfNull(
            transactionService);

        this.operationValidatorProvider =
            operationValidatorProvider;

        this.transactionService =
            transactionService;
    }

    public ProjectOperationResult Execute(
        IProjectOperation operation,
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        ArgumentNullException.ThrowIfNull(
            project);

        if (!operation.CanExecute(project))
        {
            return ProjectOperationResult.Failure(
                $"The operation '{operation.Name}' cannot be executed on the current project.");
        }

        ProjectOperationExecutionContext context = new();

        try
        {
            ProjectOperationResult result;
            if (operation is IContextualProjectOperation contextual)
            {
                contextual.Preflight(project);
                result = contextual.Execute(project, context);
            }
            else
            {
                result = operation.Execute(project);
                context.Record(result.MutationResult);
            }

            OperationValidationResult validationResult =
                operationValidatorProvider.Validate(
                    operation,
                    project,
                    context.MutationResult);

            if (!validationResult.IsValid)
            {
                try
                {
                    RollbackIfRequired(context.MutationResult);
                }
                catch (Exception rollbackException)
                {
                    return CreateFatalRollbackFailure(
                        operation,
                        new InvalidOperationException(
                            string.Join(
                                Environment.NewLine,
                                validationResult.Errors)),
                        rollbackException);
                }

                return ProjectOperationResult.Failure(
                    string.Join(
                        Environment.NewLine,
                        validationResult.Errors));
            }

            return ProjectOperationResult.Success(
                context.MutationResult,
                result.Message);
        }
        catch (Exception exception)
        {
            try
            {
                RollbackIfRequired(context.MutationResult);
            }
            catch (Exception rollbackException)
            {
                return CreateFatalRollbackFailure(
                    operation,
                    exception,
                    rollbackException);
            }

            return ProjectOperationResult.Failure(
                $"The operation '{operation.Name}' failed." +
                Environment.NewLine +
                Environment.NewLine +
                exception.Message);
        }
    }

    private void RollbackIfRequired(ProjectMutationResult mutationResult)
    {
        if (mutationResult.WasModified)
        {
            transactionService.Rollback(mutationResult);
        }
    }

    private static ProjectOperationResult CreateFatalRollbackFailure(
        IProjectOperation operation,
        Exception operationException,
        Exception rollbackException)
    {
        return ProjectOperationResult.Failure(
            $"The operation '{operation.Name}' failed and its changes could not be fully rolled back." +
            Environment.NewLine + Environment.NewLine +
            $"Operation error: {operationException.Message}" +
            Environment.NewLine +
            $"Rollback error: {rollbackException.Message}");
    }
}
