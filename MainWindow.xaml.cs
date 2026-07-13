using System.Windows;
using System.Windows.Input;
using WartalesEditor.Services;
using WartalesEditor.ViewModels;

namespace WartalesEditor;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        ViewModel =
            new MainViewModel(
                new JsonDataService(),
                new SearchService(),
                new LocalizationService(),
                new EditHistoryService(),
                new ModificationSnapshotService(),
                new ModificationSnapshotWorkflowService(),
                new ChangeSummaryService(),
                ReferenceDataService.Instance,
                new WpfFileDialogService(),
                new WpfMessageDialogService());

        InitializeComponent();

        DataContext =
            ViewModel;
    }

    private void Window_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (Keyboard.Modifiers !=
            ModifierKeys.Control)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.O:
                if (ViewModel.OpenCommand
                    .CanExecute(null))
                {
                    ViewModel.OpenCommand
                        .Execute(null);

                    e.Handled = true;
                }

                break;

            case Key.S:
                if (ViewModel.SaveCommand
                    .CanExecute(null))
                {
                    ViewModel.SaveCommand
                        .Execute(null);

                    e.Handled = true;
                }

                break;

            case Key.Z:
                if (ViewModel.UndoCommand
                    .CanExecute(null))
                {
                    ViewModel.UndoCommand
                        .Execute(null);

                    e.Handled = true;
                }

                break;

            case Key.Y:
                if (ViewModel.RedoCommand
                    .CanExecute(null))
                {
                    ViewModel.RedoCommand
                        .Execute(null);

                    e.Handled = true;
                }

                break;
        }
    }

    private void ExitMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}