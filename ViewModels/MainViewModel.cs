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

    public ObservableCollection<SheetModel> Sheets =>
        Project?.Sheets ?? new ObservableCollection<SheetModel>();

    private SheetModel? selectedSheet;

    public SheetModel? SelectedSheet
    {
        get => selectedSheet;
        set
        {
            if (SetProperty(ref selectedSheet, value))
            {
                OnPropertyChanged(nameof(Entries));
            }
        }
    }

    public ObservableCollection<EntryModel> Entries =>
        SelectedSheet?.Entries ?? new ObservableCollection<EntryModel>();

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
    }
}