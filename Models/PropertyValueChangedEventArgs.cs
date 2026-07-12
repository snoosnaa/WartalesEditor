using System;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models;

public sealed class PropertyValueChangedEventArgs : EventArgs
{
    public PropertyValueChangedEventArgs(
        JToken previousValue,
        JToken newValue)
    {
        PreviousValue = previousValue.DeepClone();
        NewValue = newValue.DeepClone();
    }

    public JToken PreviousValue { get; }

    public JToken NewValue { get; }
}
