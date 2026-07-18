using System;
using WartalesEditor.Services;

namespace WartalesEditor.Models.Operations;

public sealed class ProjectOperationResult
{
    public ProjectMutationResult MutationResult
    {
        get;
    }

    public bool Succeeded
    {
        get;
    }

    public string? Message
    {
        get;
    }

    public ProjectOperationResult(
        ProjectMutationResult mutationResult,
        bool succeeded,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(
            mutationResult);

        MutationResult =
            mutationResult;

        Succeeded =
            succeeded;

        Message =
            message;
    }

    public static ProjectOperationResult Success(
        ProjectMutationResult mutationResult,
        string? message = null)
    {
        return new ProjectOperationResult(
            mutationResult,
            true,
            message);
    }

    public static ProjectOperationResult Failure(
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        return new ProjectOperationResult(
            new ProjectMutationResult(),
            false,
            message);
    }
}