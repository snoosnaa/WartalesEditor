using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class PropertyEditAction : IEditAction
{
    private readonly JToken previousValue;
    private readonly JToken newValue;

    public PropertyEditAction(
        PropertyModel property,
        JToken previousValue,
        JToken newValue)
    {
        Property =
            property;

        this.previousValue =
            previousValue.DeepClone();

        this.newValue =
            newValue.DeepClone();
    }

    public PropertyModel Property { get; }

    public string Description =>
        $"Change {Property.Name}";

    public void Undo()
    {
        Property.ApplyHistoryValue(
            previousValue);
    }

    public void Redo()
    {
        Property.ApplyHistoryValue(
            newValue);
    }
}