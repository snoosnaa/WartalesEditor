using System;
using System.Windows;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class PathLevelRequirementsDialog : Window
{
    public event EventHandler<PathLevelRequirementsApplyEventArgs>? ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public PathLevelRequirementsDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PathLevelRequirementsDialogViewModel viewModel &&
            viewModel.CanApply && viewModel.SelectedOption != null)
            ApplyRequested?.Invoke(this,
                new PathLevelRequirementsApplyEventArgs(viewModel.SelectedOption.Percentage));
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PathLevelRequirementsDialogViewModel viewModel &&
            viewModel.CanRestorePreviousValues)
            ApplyRequested?.Invoke(this, new PathLevelRequirementsApplyEventArgs(100, true));
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        base.OnClosed(e);
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        try { Activate(); Focus(); }
        catch (Exception exception) { DisplayFailed?.Invoke(exception); Close(); }
    }
}

public sealed class PathLevelRequirementsApplyEventArgs : EventArgs
{
    public PathLevelRequirementsApplyEventArgs(int percentage, bool restorePreviousValues = false)
    {
        Percentage = percentage;
        RestorePreviousValues = restorePreviousValues;
    }
    public int Percentage { get; }
    public bool RestorePreviousValues { get; }
}
