using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models;

public class EntryModel
{
    public string Id { get; set; } =
        string.Empty;

    public string DisplayName { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public JObject? SourceEntry { get; set; }

    public ObservableCollection<PropertyModel> Properties
    {
        get;
    } = new();
}