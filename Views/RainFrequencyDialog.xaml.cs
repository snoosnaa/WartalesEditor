using System;
using System.Windows;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class RainFrequencyDialog : Window
{
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
        ContentRendered -= OnContentRendered;
        base.OnClosed(e);
    }

    private void OnContentRendered(
        object? sender,
        EventArgs e)
    {
        try
        {
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
