using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using WartalesEditor.Models;
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

    private const uint SetWindowPositionNoActivate =
        0x0010;

    private const uint SetWindowPositionNoOwnerOrder =
        0x0200;

    private const uint SetWindowPositionNoZOrder =
        0x0004;

    private const int MinimumReachableTitleBarWidth =
        64;

    private const int MinimumReachableTitleBarHeight =
        16;

    private const int EstimatedTitleBarHeight =
        32;

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

        ApplyStartupWindowPlacement();
    }

    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message == WmDpiChanged &&
            lParam != IntPtr.Zero)
        {
            NativeRect suggestedRectangle =
                Marshal.PtrToStructure<NativeRect>(
                    lParam);

            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                    ApplyDpiChangedPlacement(
                        hwnd,
                        suggestedRectangle)));
        }
        else if (message == WmExitSizeMove)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                    RecoverWindowIfNecessary()));
        }

        return IntPtr.Zero;
    }

    private void ApplyDpiChangedPlacement(
        IntPtr handle,
        NativeRect suggestedRectangle)
    {
        if (WindowState !=
            WindowState.Normal)
        {
            return;
        }

        IntPtr monitor =
            MonitorFromRect(
                ref suggestedRectangle,
                MonitorDefaultToNearest);

        if (monitor == IntPtr.Zero)
        {
            return;
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
            return;
        }

        NativeRect workArea =
            monitorInfo.WorkArea;
        int width =
            Math.Min(
                suggestedRectangle.Right -
                    suggestedRectangle.Left,
                workArea.Right -
                    workArea.Left);
        int height =
            Math.Min(
                suggestedRectangle.Bottom -
                    suggestedRectangle.Top,
                workArea.Bottom -
                    workArea.Top);

        width =
            Math.Max(
                1,
                width);
        height =
            Math.Max(
                1,
                height);

        int left =
            Math.Max(
                workArea.Left,
                Math.Min(
                    suggestedRectangle.Left,
                    workArea.Right -
                        width));
        int top =
            Math.Max(
                workArea.Top,
                Math.Min(
                    suggestedRectangle.Top,
                    workArea.Bottom -
                        height));

        SetWindowPos(
            handle,
            IntPtr.Zero,
            left,
            top,
            width,
            height,
            SetWindowPositionNoZOrder |
            SetWindowPositionNoActivate |
            SetWindowPositionNoOwnerOrder);

        Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() =>
                RecoverWindowIfNecessary()));
    }

    private void ApplyStartupWindowPlacement()
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
    }

    private void RecoverWindowIfNecessary()
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

        bool isOversized =
            Width > area.Width
            ||
            Height > area.Height;

        MaxWidth =
            area.Width;
        MaxHeight =
            area.Height;

        if (isOversized)
        {
            Width =
                Math.Min(
                    Width,
                    area.Width);
            Height =
                Math.Min(
                    Height,
                    area.Height);
        }

        if (!isOversized
            &&
            IsTitleBarReachable())
        {
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

    private bool IsTitleBarReachable()
    {
        IntPtr handle =
            new WindowInteropHelper(this).Handle;

        if (handle == IntPtr.Zero
            ||
            !GetWindowRect(
                handle,
                out NativeRect windowRect))
        {
            return true;
        }

        int titleBarBottom =
            Math.Min(
                windowRect.Bottom,
                windowRect.Top +
                EstimatedTitleBarHeight);

        foreach (NativeRect workArea in
                 GetMonitorWorkAreas())
        {
            int intersectionWidth =
                Math.Min(
                    windowRect.Right,
                    workArea.Right)
                -
                Math.Max(
                    windowRect.Left,
                    workArea.Left);

            int intersectionHeight =
                Math.Min(
                    titleBarBottom,
                    workArea.Bottom)
                -
                Math.Max(
                    windowRect.Top,
                    workArea.Top);

            if (intersectionWidth >=
                    MinimumReachableTitleBarWidth
                &&
                intersectionHeight >=
                    MinimumReachableTitleBarHeight)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<NativeRect>
        GetMonitorWorkAreas()
    {
        List<NativeRect> workAreas =
            new();

        MonitorEnumProcedure callback =
            (
                IntPtr monitor,
                IntPtr monitorDeviceContext,
                ref NativeRect monitorRectangle,
                IntPtr data) =>
            {
                MonitorInfo monitorInfo =
                    new()
                    {
                        Size =
                            Marshal.SizeOf<MonitorInfo>()
                    };

                if (GetMonitorInfo(
                        monitor,
                        ref monitorInfo))
                {
                    workAreas.Add(
                        monitorInfo.WorkArea);
                }

                return true;
            };

        EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            callback,
            IntPtr.Zero);

        return workAreas;
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

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(
        ref NativeRect rectangle,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        IntPtr deviceContext,
        IntPtr clipRectangle,
        MonitorEnumProcedure callback,
        IntPtr data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(
        IntPtr hwnd,
        out NativeRect rectangle);

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

    private delegate bool MonitorEnumProcedure(
        IntPtr monitor,
        IntPtr monitorDeviceContext,
        ref NativeRect monitorRectangle,
        IntPtr data);

    private void Window_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Escape
            &&
            Keyboard.Modifiers == ModifierKeys.None
            &&
            ViewModel.HasSearchText
            &&
            (SearchBox.IsKeyboardFocusWithin
             ||
             SearchResultsList.IsKeyboardFocusWithin))
        {
            ViewModel.ClearSearchCommand
                .Execute(null);

            Dispatcher.BeginInvoke(
                new Action(() =>
                    SearchBox.Focus()));

            e.Handled = true;
            return;
        }

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

    private void PropertiesListView_PreviewGotKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs e)
    {
        SynchronizePropertySelection(
            e.NewFocus as DependencyObject);
    }

    private void PropertiesListView_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        SynchronizePropertySelection(
            e.OriginalSource as DependencyObject);
    }

    private void SynchronizePropertySelection(
        DependencyObject? interactionSource)
    {
        if (interactionSource == null)
        {
            return;
        }

        if (ItemsControl.ContainerFromElement(
                PropertiesListView,
                interactionSource)
            is not ListViewItem item
            ||
            item.DataContext
            is not PropertyModel property)
        {
            return;
        }

        PropertiesListView.SelectedItem =
            property;
        ViewModel.SelectedProperty =
            property;
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
                .ConfirmApplicationClose())
        {
            e.Cancel = true;
        }
    }
}
