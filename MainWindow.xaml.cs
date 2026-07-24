using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.Services.Validation;
using WartalesEditor.ViewModels;

namespace WartalesEditor;

public partial class MainWindow : Window
{
    private const int WmDpiChanged =
        0x02E0;

    private const int WmExitSizeMove =
        0x0232;

    private const uint MonitorDefaultToNearest =
        0x00000002;

    private HwndSource? windowSource;

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

        ProfileOperationCaptureService
            profileOperationCaptureService =
                new(
                    new OperationValidatorProvider(),
                    addCampFacilitiesOperation,
                    upgradeAllEquipmentOperation);

        ModProfileService modProfileService =
            new(
                new ModificationSnapshotService(),
                profileOperationCaptureService);

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
                    modProfileService,
                    new ModProfileSerializationService(),
                    snapshotWorkflowService,
                    new ProfileOperationResolver(
                        addCampFacilitiesOperation,
                        upgradeAllEquipmentOperation),
                    projectOperationService,
                    projectOperationTransactionService),
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
        IntPtr handle =
            new WindowInteropHelper(this).Handle;

        windowSource =
            HwndSource.FromHwnd(handle);
        windowSource?.AddHook(
            WindowMessageHook);

        FitToNearestMonitor(
            centerWindow: true);
    }

    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message == WmExitSizeMove ||
            message == WmDpiChanged)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                    FitToNearestMonitor(
                        centerWindow: false)));
        }

        return IntPtr.Zero;
    }

    private void FitToNearestMonitor(
        bool centerWindow)
    {
        if (WindowState !=
            WindowState.Normal)
        {
            return;
        }

        Rect? workArea =
            GetNearestMonitorWorkArea();

        if (workArea == null)
        {
            return;
        }

        Rect area =
            workArea.Value;

        MaxWidth =
            area.Width;
        MaxHeight =
            area.Height;

        Width =
            Math.Min(
                Width,
                area.Width);
        Height =
            Math.Min(
                Height,
                area.Height);

        if (centerWindow ||
            double.IsNaN(Left) ||
            double.IsNaN(Top))
        {
            Left =
                area.Left +
                Math.Max(
                    0,
                    (area.Width - Width) / 2);
            Top =
                area.Top +
                Math.Max(
                    0,
                    (area.Height - Height) / 2);

            return;
        }

        Left =
            Math.Max(
                area.Left,
                Math.Min(
                    Left,
                    area.Right - Width));
        Top =
            Math.Max(
                area.Top,
                Math.Min(
                    Top,
                    area.Bottom - Height));
    }

    private Rect? GetNearestMonitorWorkArea()
    {
        IntPtr handle =
            new WindowInteropHelper(this).Handle;
        IntPtr monitor =
            MonitorFromWindow(
                handle,
                MonitorDefaultToNearest);

        if (monitor == IntPtr.Zero)
        {
            return null;
        }

        MonitorInfo monitorInfo =
            new()
            {
                Size =
                    Marshal.SizeOf<MonitorInfo>()
            };

        if (!GetMonitorInfo(
                monitor,
                ref monitorInfo))
        {
            return null;
        }

        if (windowSource?.CompositionTarget == null)
        {
            return null;
        }

        Point topLeft =
            windowSource.CompositionTarget
                .TransformFromDevice.Transform(
                    new Point(
                        monitorInfo.WorkArea.Left,
                        monitorInfo.WorkArea.Top));
        Point bottomRight =
            windowSource.CompositionTarget
                .TransformFromDevice.Transform(
                    new Point(
                        monitorInfo.WorkArea.Right,
                        monitorInfo.WorkArea.Bottom));

        return new Rect(
            topLeft,
            bottomRight);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(
        IntPtr hwnd,
        uint flags);

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(
        IntPtr monitor,
        ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;

        public NativeRect MonitorArea;

        public NativeRect WorkArea;

        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;

        public int Top;

        public int Right;

        public int Bottom;
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

            case Key.F:
                if (!ViewModel.HasProject)
                {
                    break;
                }

                ViewModel
                    .ShowDetailedEditorWorkspaceCommand
                    .Execute(null);

                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        SearchBox.Focus();
                        SearchBox.SelectAll();
                    }));

                e.Handled = true;
                break;
        }
    }

    private void PropertiesListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (PropertiesListView.SelectedItem == null)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            new Action(() =>
                PropertiesListView.ScrollIntoView(
                    PropertiesListView.SelectedItem)));
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
