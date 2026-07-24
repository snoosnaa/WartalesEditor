using WartalesEditor.Helpers;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace WartalesEditor.Models;

public class ProjectModel : ObservableObject
{
    private bool isModified;

    private bool isGameplayOperationStateModified;

    public string FileName { get; set; } = "";

    public string OriginalJson { get; set; } = "";

    public JObject RootDocument { get; set; } = new();

    public bool IsModified
    {
        get => isModified;
        set => SetProperty(ref isModified, value);
    }

    public bool IsGameplayOperationStateModified
    {
        get => isGameplayOperationStateModified;
        set => SetProperty(
            ref isGameplayOperationStateModified,
            value);
    }

    public ObservableCollection<SheetModel> Sheets { get; }
        = new();

    public ObservableCollection<GameplayOperationStateModel>
        GameplayOperationStates
    {
        get;
    } = new();

    public List<string> GameplayOperationStateWarnings
    {
        get;
    } = new();
}
