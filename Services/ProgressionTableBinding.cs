using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

internal sealed class ProgressionTableBinding
{
    public ProgressionTableBinding(
        EntryModel entry,
        PropertyModel arrayProperty,
        string elementValuePropertyName)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(arrayProperty);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            elementValuePropertyName);

        Entry = entry;
        ArrayProperty = arrayProperty;
        ElementValuePropertyName = elementValuePropertyName;
    }

    public EntryModel Entry { get; }

    public PropertyModel ArrayProperty { get; }

    public string ArrayPropertyPath =>
        ArrayProperty.EffectivePropertyPath;

    public string ElementValuePropertyName { get; }

    public IReadOnlyList<long> ReadValues(
        JToken token)
    {
        if (token is not JArray array)
        {
            throw new InvalidOperationException(
                $"Progression table '{Entry.Id}' property " +
                $"'{ArrayPropertyPath}' must be a JSON array.");
        }

        if (array.Count == 0)
        {
            throw new InvalidOperationException(
                $"Progression table '{Entry.Id}' must not be empty.");
        }

        List<long> values =
            new(array.Count);

        for (int index = 0;
             index < array.Count;
             index++)
        {
            if (array[index] is not JObject element ||
                element[ElementValuePropertyName]?.Type !=
                    JTokenType.Integer)
            {
                throw new InvalidOperationException(
                    $"Progression table '{Entry.Id}' contains an " +
                    $"invalid element at index {index}.");
            }

            values.Add(
                element[ElementValuePropertyName]!
                    .Value<long>());
        }

        return values;
    }

    public JArray CreateArray(
        JToken baselineToken,
        IReadOnlyList<long> scaledValues)
    {
        ArgumentNullException.ThrowIfNull(baselineToken);
        ArgumentNullException.ThrowIfNull(scaledValues);

        if (baselineToken is not JArray baselineArray ||
            baselineArray.Count != scaledValues.Count)
        {
            throw new InvalidOperationException(
                $"Progression table '{Entry.Id}' baseline does not " +
                "match the scaled values.");
        }

        JArray result =
            (JArray)baselineArray.DeepClone();

        for (int index = 0;
             index < result.Count;
             index++)
        {
            JObject element =
                (JObject)result[index];

            element[ElementValuePropertyName] =
                scaledValues[index];
        }

        return result;
    }
}
