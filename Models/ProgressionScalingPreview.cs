using System;
using System.Collections.Generic;

namespace WartalesEditor.Models;

public sealed class ProgressionScalingPreview
{
    public ProgressionScalingPreview(
        IReadOnlyList<long> baselineValues,
        IReadOnlyList<long> scaledValues)
    {
        ArgumentNullException.ThrowIfNull(
            baselineValues);

        ArgumentNullException.ThrowIfNull(
            scaledValues);

        BaselineValues = baselineValues;
        ScaledValues = scaledValues;
    }

    public IReadOnlyList<long> BaselineValues { get; }

    public IReadOnlyList<long> ScaledValues { get; }
}
