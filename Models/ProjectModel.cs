using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models;

public class ProjectModel
{
    public string FileName { get; set; } = "";

    public string OriginalJson { get; set; } = "";

    public JObject RootDocument { get; set; } = new();

    public bool IsModified { get; set; }

    public ObservableCollection<SheetModel> Sheets { get; }
        = new();
}
