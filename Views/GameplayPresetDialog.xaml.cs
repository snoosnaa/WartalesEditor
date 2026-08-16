using System;
using System.Windows;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class GameplayPresetDialog : Window
{
    public event EventHandler<GameplayPresetApplyEventArgs>? ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public GameplayPresetDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameplayPresetDialogViewModel vm &&
            vm.CanApply &&
            vm.SelectedPreset != null)
            RequestApply(vm);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameplayPresetDialogViewModel vm) return;
        vm.SelectVanilla();
        if (vm.CanApply && vm.SelectedPreset != null)
            RequestApply(vm);
    }

    private void RequestApply(GameplayPresetDialogViewModel vm) =>
        ApplyRequested?.Invoke(
            this,
            new GameplayPresetApplyEventArgs(
                vm.OperationType,
                vm.SelectedPreset!.Key));

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

public sealed class GameplayPresetApplyEventArgs : EventArgs
{
    public GameplayPresetApplyEventArgs(
        ProgressionType operationType,
        string presetKey)
    {
        OperationType = operationType;
        PresetKey = presetKey;
    }

    public ProgressionType OperationType { get; }
    public string PresetKey { get; }
}
