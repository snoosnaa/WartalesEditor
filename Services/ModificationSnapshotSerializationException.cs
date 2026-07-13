using System;

namespace WartalesEditor.Services;

public sealed class ModificationSnapshotSerializationException
    : Exception
{
    public ModificationSnapshotSerializationException(
        string message)
        : base(message)
    {
    }

    public ModificationSnapshotSerializationException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}