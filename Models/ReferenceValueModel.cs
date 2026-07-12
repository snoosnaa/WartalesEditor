namespace WartalesEditor.Models;

public sealed class ReferenceValueModel
{
    public string Value { get; }

    public string Display { get; }

    public ReferenceValueModel(
        string value,
        string? display = null)
    {
        Value = value;
        Display = string.IsNullOrWhiteSpace(display)
            ? value
            : display;
    }

    public override string ToString()
    {
        return Display;
    }
}
