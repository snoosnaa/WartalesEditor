using System;

namespace WartalesEditor.Services;

internal sealed class ProfileImpactEvaluationIntegrityException : Exception
{
    public ProfileImpactEvaluationIntegrityException(string message)
        : base(message)
    {
    }
}
