using System;
using System.Windows;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class OverworldMovementSpeedDialog : Window
{
    private WindowState ownerWindowState = WindowState.Normal;

    public event EventHandler<OverworldMovementApplyEventArgs>? ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public OverworldMovementSpeedDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is OverworldMovementSpeedDialogViewModel vm &&
            vm.CanApply &&
            vm.SelectedPreset != null)
            ApplyRequested?.Invoke(
                this,
                new OverworldMovementApplyEventArgs(vm.SelectedPreset.Preset));
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e) =>
        ((OverworldMovementSpeedDialogViewModel)DataContext).SelectVanilla();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        Window? owner = Owner;
        ContentRendered -= OnContentRendered;
        base.OnClosed(e);
        if (owner == null) return;
        if (owner.WindowState == WindowState.Minimized)
            owner.WindowState = ownerWindowState == WindowState.Minimized
                ? WindowState.Normal
                : ownerWindowState;
        owner.Show();
        owner.Activate();
        owner.Focus();
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        try
        {
            if (Owner != null) ownerWindowState = Owner.WindowState;
            Rect work = SystemParameters.WorkArea;
            Rect bounds = new(Left, Top, ActualWidth, ActualHeight);
            if (!bounds.IntersectsWith(work))
            {
                Left = Owner != null
                    ? Owner.Left + Math.Max(0, (Owner.ActualWidth - Width) / 2)
                    : work.Left + (work.Width - Width) / 2;
                Top = Owner != null
                    ? Owner.Top + Math.Max(0, (Owner.ActualHeight - Height) / 2)
                    : work.Top + (work.Height - Height) / 2;
            }
            Activate();
            Focus();
        }
        catch (Exception exception)
        {
            DisplayFailed?.Invoke(exception);
            Close();
        }
    }
}

public sealed class OverworldMovementApplyEventArgs : EventArgs
{
    public OverworldMovementApplyEventArgs(OverworldMovementPreset preset) =>
        Preset = preset;

    public OverworldMovementPreset Preset { get; }
}
