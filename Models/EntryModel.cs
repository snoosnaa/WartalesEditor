using System.Collections.ObjectModel;

namespace WartalesEditor.Models;

public class EntryModel
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Name { get; set; } = "";

    public ObservableCollection<PropertyModel> Properties { get; }
        = new();
}