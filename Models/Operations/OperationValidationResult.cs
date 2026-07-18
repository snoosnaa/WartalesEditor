using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WartalesEditor.Models.Operations;

public sealed class OperationValidationResult
{
    private readonly ReadOnlyCollection<string> errors;

    public bool IsValid =>
        errors.Count == 0;

    public IReadOnlyList<string> Errors =>
        errors;

    private OperationValidationResult(
        IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(
            errors);

        this.errors =
            errors
                .Where(
                    error =>
                        !string.IsNullOrWhiteSpace(
                            error))
                .ToList()
                .AsReadOnly();
    }

    public static OperationValidationResult Success()
    {
        return new OperationValidationResult(
            Array.Empty<string>());
    }

    public static OperationValidationResult Failure(
        string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            error);

        return new OperationValidationResult(
            new[]
            {
                error
            });
    }

    public static OperationValidationResult Failure(
        IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(
            errors);

        return new OperationValidationResult(
            errors);
    }
}
