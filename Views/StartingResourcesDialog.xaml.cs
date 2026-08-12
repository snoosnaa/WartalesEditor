using System;
using System.Windows;
using System.Windows.Controls;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class StartingResourcesDialog : Window
{
    public event EventHandler? InitializeRequested;
    public event EventHandler<StartingResourcesApplyEventArgs>? ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public StartingResourcesDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    protected override void OnClosed(EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        base.OnClosed(e);
    }

    private void InitializeButton_Click(object sender, RoutedEventArgs e) =>
        InitializeRequested?.Invoke(this, EventArgs.Empty);

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is StartingResourcesDialogViewModel vm && vm.CanApply)
        {
            ApplyRequested?.Invoke(this, new StartingResourcesApplyEventArgs(vm.CreateSettings()));
        }
    }

    private void AddFiveButton_Click(object sender, RoutedEventArgs e) =>
        ((StartingResourcesDialogViewModel)DataContext).AddToAllMaterials(5);

    private void AddTenButton_Click(object sender, RoutedEventArgs e) =>
        ((StartingResourcesDialogViewModel)DataContext).AddToAllMaterials(10);

    private void ClearButton_Click(object sender, RoutedEventArgs e) =>
        ((StartingResourcesDialogViewModel)DataContext).ClearExtras();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void InputValidationError(object sender, ValidationErrorEventArgs e)
    {
        if (DataContext is StartingResourcesDialogViewModel vm)
        {
            vm.SetInputBindingValid(
                !FindVisualErrors(this));
        }
    }

    private static bool FindVisualErrors(DependencyObject parent)
    {
        if (System.Windows.Controls.Validation.GetHasError(parent)) return true;
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            if (FindVisualErrors(System.Windows.Media.VisualTreeHelper.GetChild(parent, index))) return true;
        }
        return false;
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

public sealed class StartingResourcesApplyEventArgs : EventArgs
{
    public StartingResourcesApplyEventArgs(StartingResourcesSettings settings) =>
        Settings = settings.DeepClone();
    public StartingResourcesSettings Settings { get; }
}
