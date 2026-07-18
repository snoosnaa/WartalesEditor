using System;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Operations.Rollback;

public sealed class PropertyRollbackRecord
{
    public PropertyModel Property { get; }

    public JToken PreviousValue { get; }

    public PropertyRollbackRecord(
        PropertyModel property,
        JToken previousValue)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(previousValue);

        Property = property;
        PreviousValue = previousValue.DeepClone();
    }
}
