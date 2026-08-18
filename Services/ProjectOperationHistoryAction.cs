using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models.Operations.Rollback;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class ProjectOperationHistoryAction :
    IEditAction
{
    private readonly ProjectOperationTransactionService
        transactionService;

    private readonly PropertyRollbackRecord[]
        propertyRollbackRecords;

    private readonly RemovedPropertyRollbackRecord[]
        removedPropertyRollbackRecords;

    private readonly CreatedPropertyRollbackRecord[]
        createdPropertyRollbackRecords;

    private readonly CreatedEntryRollbackRecord[]
        createdEntryRollbackRecords;

    private readonly CreatedJsonPropertyRollbackRecord[]
        createdJsonPropertyRollbackRecords;

    private readonly JToken[]
        updatedPropertyValues;

    private readonly GameplayOperationStateRollbackRecord[]
        gameplayOperationStateRollbackRecords;

    public ProjectOperationHistoryAction(
        string description,
        ProjectMutationResult mutationResult,
        ProjectOperationTransactionService
            transactionService)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            description);

        ArgumentNullException.ThrowIfNull(
            mutationResult);

        ArgumentNullException.ThrowIfNull(
            transactionService);

        Description =
            description;

        this.transactionService =
            transactionService;

        propertyRollbackRecords =
            mutationResult
                .PropertyRollbackRecords
                .ToArray();

        createdPropertyRollbackRecords =
            mutationResult
                .CreatedPropertyRollbackRecords
                .ToArray();

        createdEntryRollbackRecords =
            mutationResult
                .CreatedEntryRollbackRecords
                .ToArray();

        createdJsonPropertyRollbackRecords =
            mutationResult
                .CreatedJsonPropertyRollbackRecords
                .ToArray();

        updatedPropertyValues =
            propertyRollbackRecords
                .Select(record =>
                    record.Property
                        .GetCurrentValueSnapshot())
                .ToArray();

        removedPropertyRollbackRecords =
            mutationResult
                .RemovedPropertyRollbackRecords
                .ToArray();

        gameplayOperationStateRollbackRecords =
            mutationResult.GameplayOperationStateRollbackRecords
                .ToArray();
    }

    public string Description { get; }

    public void Undo()
    {
        transactionService.Rollback(
            propertyRollbackRecords,
            removedPropertyRollbackRecords,
            createdPropertyRollbackRecords,
            createdJsonPropertyRollbackRecords,
            createdEntryRollbackRecords,
            gameplayOperationStateRollbackRecords);
    }

    public void Redo()
    {
        transactionService.Replay(
            propertyRollbackRecords,
            updatedPropertyValues,
            removedPropertyRollbackRecords,
            createdPropertyRollbackRecords,
            createdJsonPropertyRollbackRecords,
            createdEntryRollbackRecords,
            gameplayOperationStateRollbackRecords);
    }
}
