using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;
using System.Collections.Generic;

namespace WartalesEditor.ViewModels;

public class MainViewModel : ObservableObject
{
    private ProjectModel? project;

    public ProjectModel? Project
    {
        get => project;
        set
        {
            if (SetProperty(ref project, value))
            {
                OnPropertyChanged(nameof(Sheets));
            }
        }
    }

    public ObservableCollection<SheetModel> Sheets
    {
        get
        {
            if (Project == null)
                return new ObservableCollection<SheetModel>();

            IEnumerable<SheetModel> visibleSheets = Project.Sheets;

            if (!ShowEmptyCategories)
            {
                visibleSheets =
                    visibleSheets.Where(sheet => sheet.Entries.Count > 0);
            }

            if (SearchScope == "Categories" &&
                !string.IsNullOrWhiteSpace(SearchText))
            {
                visibleSheets = visibleSheets.Where(sheet =>
                    sheet.Name.Contains(
                        SearchText,
                        StringComparison.OrdinalIgnoreCase));
            }

            return new ObservableCollection<SheetModel>(visibleSheets);
        }
    }

    private bool showEmptyCategories;

    public bool ShowEmptyCategories
    {
        get => showEmptyCategories;
        set
        {
            if (SetProperty(ref showEmptyCategories, value))
            {
                OnPropertyChanged(nameof(Sheets));
            }
        }
    }

    private SheetModel? selectedSheet;

    public SheetModel? SelectedSheet
    {
        get => selectedSheet;
        set
        {
            if (SetProperty(ref selectedSheet, value))
            {
                SelectedEntry = null;
                OnPropertyChanged(nameof(Entries));
            }
        }
    }

    public ObservableCollection<EntryModel> Entries
    {
        get
        {
            if (SelectedSheet == null)
                return new ObservableCollection<EntryModel>();

            if (string.IsNullOrWhiteSpace(SearchText))
                return SelectedSheet.Entries;

            return new ObservableCollection<EntryModel>(
                SelectedSheet.Entries.Where(entry =>
                    entry.DisplayName.Contains(
                        SearchText,
                        StringComparison.OrdinalIgnoreCase)));
        }
    }

    private EntryModel? selectedEntry;

    public EntryModel? SelectedEntry
    {
        get => selectedEntry;
        set
        {
            if (SetProperty(ref selectedEntry, value))
            {
                OnPropertyChanged(nameof(Properties));
            }
        }
    }

    public ObservableCollection<PropertyModel> Properties =>
        SelectedEntry?.Properties ?? new ObservableCollection<PropertyModel>();

    private string searchText = string.Empty;

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                OnPropertyChanged(nameof(Sheets));
                OnPropertyChanged(nameof(Entries));
            }
        }
    }

    private string searchScope = "Settings";

    public string SearchScope
    {
        get => searchScope;
        set
        {
            if (SetProperty(ref searchScope, value))
            {
                OnPropertyChanged(nameof(Sheets));
                OnPropertyChanged(nameof(Entries));
            }
        }
    }

    private string currentFile = string.Empty;

    private string status = "Ready";

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }
    public string CurrentFile
    {
        get => currentFile;
        set => SetProperty(ref currentFile, value);
    }
    private readonly JsonDataService jsonDataService = new();

    public ICommand OpenCommand { get; }

    public ICommand SaveCommand { get; }

    public MainViewModel()
    {
        OpenCommand = new RelayCommand(_ =>
        {
            OpenFileDialog dialog = new();

            dialog.Filter = "CDB Files (*.cdb)|*.cdb|JSON Files (*.json)|*.json|All Files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                CurrentFile = dialog.FileName;

                Project = jsonDataService.LoadProject(CurrentFile);

                Status = $"Loaded: {System.IO.Path.GetFileName(CurrentFile)}";

            }
        });

        SaveCommand = new RelayCommand(
            _ =>
            {
                SaveFileDialog dialog = new()
                {
                    Filter = "CDB Files (*.cdb)|*.cdb|All Files (*.*)|*.*",
                    FileName = "data.cdb"
                };

                if (dialog.ShowDialog() == true && Project != null)
                {
                    jsonDataService.SaveProject(Project, dialog.FileName);

                    Status = $"Saved: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
            },
            _ => Project != null);
    }
}