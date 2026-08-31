using System;
using System.Windows;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class RequestBoardRewardsDialog : Window
{
    public event EventHandler<RequestBoardRewardsApplyEventArgs>?
        ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public RequestBoardRewardsDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is RequestBoardRewardsDialogViewModel viewModel &&
            viewModel.CanApply &&
            viewModel.SelectedPreset != null)
        {
            ApplyRequested?.Invoke(
                this,
                new RequestBoardRewardsApplyEventArgs(
                    viewModel.SelectedPreset.Percentage));
        }
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RequestBoardRewardsDialogViewModel viewModel ||
            !viewModel.CanRestorePreviousValues)
        {
            return;
        }

        ApplyRequested?.Invoke(
            this,
            new RequestBoardRewardsApplyEventArgs(100, true));
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();

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

public sealed class RequestBoardRewardsApplyEventArgs : EventArgs
{
    public RequestBoardRewardsApplyEventArgs(
        int percentage,
        bool restorePreviousValues = false)
    {
        Percentage = percentage;
        RestorePreviousValues = restorePreviousValues;
    }

    public int Percentage { get; }
    public bool RestorePreviousValues { get; }
}
