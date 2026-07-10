using System.Collections.ObjectModel;

namespace WartalesEditor.Models;

public class EntryModel
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public ObservableCollection<KeyValuePair<string, object?>> Properties { get; }
        = new();
}