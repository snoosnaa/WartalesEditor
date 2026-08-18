using System;
using System.Windows;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class OverworldMovementSpeedDialog : Window
{
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

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OverworldMovementSpeedDialogViewModel vm ||
            !vm.CanRestorePreviousValues)
            return;

        ApplyRequested?.Invoke(
            this,
            new OverworldMovementApplyEventArgs(
                OverworldMovementPreset.Vanilla,
                true));
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        base.OnClosed(e);
    }

    private void OnContentRendered(object? sender, EventArgs e)
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

public sealed class OverworldMovementApplyEventArgs : EventArgs
{
    public OverworldMovementApplyEventArgs(OverworldMovementPreset preset) =>
        Preset = preset;

    public OverworldMovementApplyEventArgs(
        OverworldMovementPreset preset,
        bool restorePreviousValues)
    {
        Preset = preset;
        RestorePreviousValues = restorePreviousValues;
    }

    public OverworldMovementPreset Preset { get; }
    public bool RestorePreviousValues { get; }
}
