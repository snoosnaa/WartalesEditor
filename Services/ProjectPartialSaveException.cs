using System;

namespace WartalesEditor.Services;

public sealed class ProjectPartialSaveException : Exception
{
    public ProjectPartialSaveException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
