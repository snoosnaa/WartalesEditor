using System;

namespace WartalesEditor.Services;

public sealed class ModProfileSerializationException
    : Exception
{
    public ModProfileSerializationException(
        string message)
        : base(message)
    {
    }

    public ModProfileSerializationException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}
