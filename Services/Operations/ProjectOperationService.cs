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

        try
        {
            ProjectOperationSnapshot snapshot =
                transactionService.Capture(
                    project);

            _ = snapshot;

            ProjectOperationResult result =
                operation.Execute(project);

            OperationValidationResult validationResult =
                operationValidatorProvider.Validate(
                    operation,
                    project,
                    result.MutationResult);

            if (!validationResult.IsValid)
            {
                transactionService.Rollback(
                    result.MutationResult);

                return ProjectOperationResult.Failure(
                    string.Join(
                        Environment.NewLine,
                        validationResult.Errors));
            }

            return result;
        }
        catch (Exception exception)
        {
            return ProjectOperationResult.Failure(
                $"The operation '{operation.Name}' failed." +
                Environment.NewLine +
                Environment.NewLine +
                exception.Message);
        }
    }
}