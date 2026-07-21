using System;
using System.Windows;
using System.Windows.Input;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.Services.Validation;
using WartalesEditor.ViewModels;

namespace WartalesEditor;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        JsonDataService jsonDataService =
            new();

        ModificationSnapshotWorkflowService
            snapshotWorkflowService =
                new();

        ValidationService validationService =
            new(jsonDataService);

        ValidationWorkflowService
            validationWorkflowService =
                new(validationService);

        ValidationPresentationService
            validationPresentationService =
                new();

        ProjectMutationService projectMutationService =
    new();

        ContentCreationService contentCreationService =
            new(
                projectMutationService);

        AddCampFacilitiesOperation
            addCampFacilitiesOperation =
                new(
                    contentCreationService);

        UpgradeAllEquipmentOperation
            upgradeAllEquipmentOperation =
                new(
                    contentCreationService);

        EditHistoryService editHistoryService =
            new();

        ProjectOperationTransactionService
            projectOperationTransactionService =
                new();

        ProjectOperationService projectOperationService =
            new(
                new OperationValidatorProvider(),
                projectOperationTransactionService);

        ViewModel =
            new MainViewModel(
                jsonDataService,
                new SearchService(),
                new LocalizationService(),
                editHistoryService,
                new ModificationSnapshotService(),
                snapshotWorkflowService,
                new ChangeSummaryService(),
                new ModProfileLibraryService(),
                new ModProfileWorkflowService(
                    new ModProfileService(),
                    new ModProfileSerializationService(),
                    snapshotWorkflowService),
                ReferenceDataService.Instance,
                validationWorkflowService,
                validationPresentationService,
                projectOperationService,
                projectOperationTransactionService,
                addCampFacilitiesOperation,
                upgradeAllEquipmentOperation,
                new WpfFileDialogService(),
                new WpfMessageDialogService());

        InitializeComponent();

        DataContext =
            ViewModel;

        Closing +=
            OnWindowClosing;
    }

    private void Window_SourceInitialized(
        object? sender,
        EventArgs e)
    {
        Rect workArea =
            SystemParameters.WorkArea;

        MaxWidth =
            workArea.Width;

        MaxHeight =
            workArea.Height;

        Width =
            Math.Min(
                Width,
                workArea.Width);

        Height =
            Math.Min(
                Height,
                workArea.Height);

        Left =
            workArea.Left +
            Math.Max(
                0,
                (workArea.Width - Width) / 2);

        Top =
            workArea.Top +
            Math.Max(
                0,
                (workArea.Height - Height) / 2);
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

    private void OnWindowClosing(
    object? sender,
    System.ComponentModel.CancelEventArgs e)
    {
        if (!ViewModel
                .ConfirmAbandonUnsavedChanges())
        {
            e.Cancel = true;
        }
    }
}