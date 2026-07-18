using System;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class OperationValidatorProvider
    : IOperationValidatorProvider
{
    private readonly AddCampFacilitiesOperationValidator
        addCampFacilitiesValidator =
            new();

    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        ArgumentNullException.ThrowIfNull(
            project);

        ArgumentNullException.ThrowIfNull(
            mutationResult);

        IProjectOperationValidator? validator =
            operation switch
            {
                AddCampFacilitiesOperation =>
                    addCampFacilitiesValidator,

                _ => null
            };

        if (validator == null)
        {
            return OperationValidationResult.Success();
        }

        return validator.Validate(
            operation,
            project,
            mutationResult);
    }
}