using System;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models.Operations.Rollback;

public sealed class CreatedJsonPropertyRollbackRecord
{
    public JObject ParentObject { get; }

    public JProperty Property { get; }

    public CreatedJsonPropertyRollbackRecord(
        JObject parentObject,
        JProperty property)
    {
        ArgumentNullException.ThrowIfNull(parentObject);
        ArgumentNullException.ThrowIfNull(property);

        ParentObject = parentObject;
        Property = property;
    }
}