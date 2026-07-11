using System.Globalization;
using Newtonsoft.Json.Linq;
using WartalesEditor.Helpers;

namespace WartalesEditor.Models;

public class PropertyModel : ObservableObject
{
    private object? value;

    public string Name { get; set; } = "";

    public JProperty? SourceProperty { get; set; }

    public object? Value
    {
        get => value;
        set
        {
            if (SetProperty(ref this.value, value))
            {
                UpdateSourceProperty(value);
            }
        }
    }

    private void UpdateSourceProperty(object? newValue)
    {
        if (SourceProperty == null)
            return;

        JToken currentValue = SourceProperty.Value;

        string textValue = newValue?.ToString() ?? string.Empty;

        switch (currentValue.Type)
        {
            case JTokenType.String:
                SourceProperty.Value = textValue;
                break;

            case JTokenType.Integer:
                if (long.TryParse(
                    textValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out long integerValue))
                {
                    SourceProperty.Value = integerValue;
                }

                break;

            case JTokenType.Float:
                if (double.TryParse(
                    textValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double decimalValue))
                {
                    SourceProperty.Value = decimalValue;
                }

                break;

            case JTokenType.Boolean:
                if (bool.TryParse(textValue, out bool booleanValue))
                {
                    SourceProperty.Value = booleanValue;
                }

                break;

            case JTokenType.Null:
                SourceProperty.Value = textValue;
                break;

            case JTokenType.Array:
            case JTokenType.Object:
                // Complex values are read-only until dedicated editors are added.
                break;
        }
    }
}