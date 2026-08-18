using System;
using System.Collections.Generic;
using System.Windows;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class RandomTraitExclusionsDialog : Window
{
    public event EventHandler<RandomTraitExclusionsApplyEventArgs>? ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public RandomTraitExclusionsDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e) => RequestApply();

    private void SelectAllButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel?.SelectAll();

    private void ClearAllButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel?.ClearAll();

    private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.TryRestorePreviousValues() == true)
            RequestApply();
    }

    private void RequestApply()
    {
        if (ViewModel is not { CanApply: true } viewModel) return;
        ApplyRequested?.Invoke(
            this,
            new RandomTraitExclusionsApplyEventArgs(viewModel.GetAllowedTraitIds()));
    }

    private RandomTraitExclusionsDialogViewModel? ViewModel =>
        DataContext as RandomTraitExclusionsDialogViewModel;

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

public sealed class RandomTraitExclusionsApplyEventArgs : EventArgs
{
    public RandomTraitExclusionsApplyEventArgs(IReadOnlyCollection<string> allowedTraitIds) =>
        AllowedTraitIds = allowedTraitIds;

    public IReadOnlyCollection<string> AllowedTraitIds { get; }
}
