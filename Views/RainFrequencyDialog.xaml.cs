using System;
using System.Windows;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class RainFrequencyDialog : Window
{
    private WindowState ownerWindowState = WindowState.Normal;

    public event EventHandler<RainFrequencyApplyEventArgs>?
        ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public RainFrequencyDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    private void ApplyButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is RainFrequencyDialogViewModel viewModel &&
            viewModel.CanApply &&
            viewModel.SelectedPreset != null)
            ApplyRequested?.Invoke(
                this,
                new RainFrequencyApplyEventArgs(
                    viewModel.SelectedPreset.Preset));
    }

    private void ResetButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ((RainFrequencyDialogViewModel)DataContext)
            .SelectVanilla();
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        Window? owner = Owner;
        ContentRendered -= OnContentRendered;
        base.OnClosed(e);
        if (owner == null)
            return;
        if (owner.WindowState == WindowState.Minimized)
            owner.WindowState =
                ownerWindowState == WindowState.Minimized
                    ? WindowState.Normal
                    : ownerWindowState;
        owner.Show();
        owner.Activate();
        owner.Focus();
    }

    private void OnContentRendered(
        object? sender,
        EventArgs e)
    {
        try
        {
            if (Owner != null)
                ownerWindowState = Owner.WindowState;
            Rect workArea = SystemParameters.WorkArea;
            Rect bounds =
                new(Left, Top, ActualWidth, ActualHeight);
            if (!bounds.IntersectsWith(workArea))
            {
                Left = Owner != null
                    ? Owner.Left +
                      Math.Max(
                          0,
                          (Owner.ActualWidth - Width) / 2)
                    : workArea.Left +
                      (workArea.Width - Width) / 2;
                Top = Owner != null
                    ? Owner.Top +
                      Math.Max(
                          0,
                          (Owner.ActualHeight - Height) / 2)
                    : workArea.Top +
                      (workArea.Height - Height) / 2;
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

public sealed class RainFrequencyApplyEventArgs : EventArgs
{
    public RainFrequencyApplyEventArgs(
        RainFrequencyPreset preset)
    {
        Preset = preset;
    }

    public RainFrequencyPreset Preset { get; }
}
