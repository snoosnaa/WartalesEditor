using WartalesEditor.Helpers;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models;

public class ProjectModel : ObservableObject
{
    private bool isModified;

    public string FileName { get; set; } = "";

    public string OriginalJson { get; set; } = "";

    public JObject RootDocument { get; set; } = new();

    public bool IsModified
    {
        get => isModified;
        set => SetProperty(ref isModified, value);
    }

    public ObservableCollection<SheetModel> Sheets { get; }
        = new();
}