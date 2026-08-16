using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models;

public sealed class GameplayPresetOption
{
    public GameplayPresetOption(
        string key,
        string name,
        string valueSummary,
        string description,
        JArray values)
    {
        Key = key;
        Name = name;
        ValueSummary = valueSummary;
        Description = description;
        Values = (JArray)values.DeepClone();
    }

    public string Key { get; }
    public string Name { get; }
    public string ValueSummary { get; }
    public string Description { get; }
    internal JArray Values { get; }

    public string DisplayText =>
        $"{Name} — {ValueSummary}";
}
