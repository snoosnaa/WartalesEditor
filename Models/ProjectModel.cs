using System.Collections.ObjectModel;

namespace WartalesEditor.Models;

public class ProjectModel
{
    public string FileName { get; set; } = "";

    public bool IsModified { get; set; }

    public ObservableCollection<SheetModel> Sheets { get; }
        = new();
}
