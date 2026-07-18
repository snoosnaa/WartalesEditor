using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models;

public class SheetModel
{
    public string Name { get; set; } =
        string.Empty;

    public JObject? SourceSheet { get; set; }

    public ObservableCollection<EntryModel> Entries
    {
        get;
    } = new();
}