using System.Collections.ObjectModel;

namespace WartalesEditor.Models;

public class SheetModel
{
    public string Name { get; set; } = "";

    public ObservableCollection<EntryModel> Entries { get; }
        = new();
}
