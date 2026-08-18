using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Operations.Rollback;

public sealed class RemovedPropertyRollbackRecord
{
    public EntryModel Entry { get; }

    public PropertyModel Property { get; }

    public JObject ParentObject { get; }

    public int PropertyIndex { get; }

    public int SourcePropertyIndex { get; }

    public string PropertyPath { get; }

    public bool PropertyWasModified { get; }

    public RemovedPropertyRollbackRecord(
        EntryModel entry,
        PropertyModel property)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(property);

        if (property.SourceProperty?.Parent is not JObject parentObject)
        {
            throw new InvalidOperationException(
                $"Property '{property.EffectivePropertyPath}' on entry " +
                $"'{entry.Id}' is not attached to a JSON object.");
        }

        int propertyIndex =
            entry.Properties.IndexOf(property);

        if (propertyIndex < 0)
        {
            throw new InvalidOperationException(
                $"Property '{property.EffectivePropertyPath}' is not " +
                $"present on entry '{entry.Id}'.");
        }

        JProperty[] sourceProperties =
            parentObject.Properties().ToArray();

        int sourcePropertyIndex =
            Array.IndexOf(
                sourceProperties,
                property.SourceProperty);

        if (sourcePropertyIndex < 0)
        {
            throw new InvalidOperationException(
                $"Property '{property.EffectivePropertyPath}' is not " +
                "present in its source JSON object.");
        }

        Entry = entry;
        Property = property;
        ParentObject = parentObject;
        PropertyIndex = propertyIndex;
        SourcePropertyIndex = sourcePropertyIndex;
        PropertyPath = property.EffectivePropertyPath;
        PropertyWasModified = property.IsModified;
    }
}
