using System;
using System.Windows;
using System.Windows.Controls;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class PartyEconomyDialog : Window
{
    private WindowState ownerWindowState = WindowState.Normal;

    public event EventHandler<PartyEconomyApplyEventArgs>? ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public PartyEconomyDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PartyEconomyDialogViewModel vm && vm.CanApply)
            ApplyRequested?.Invoke(this, new PartyEconomyApplyEventArgs(
                vm.OperationType, vm.CreateSettings()));
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e) =>
        ((PartyEconomyDialogViewModel)DataContext).ResetToGameDefaults();

    private void NoWagesButton_Click(object sender, RoutedEventArgs e) =>
        ((PartyEconomyDialogViewModel)DataContext).SetNoWages();

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

    private void InputValidationError(object sender, ValidationErrorEventArgs e)
    {
        if (DataContext is PartyEconomyDialogViewModel vm)
            vm.SetInputBindingValid(!HasError(this));
    }

    private static bool HasError(DependencyObject parent)
    {
        if (System.Windows.Controls.Validation.GetHasError(parent)) return true;
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
            if (HasError(System.Windows.Media.VisualTreeHelper.GetChild(parent, index))) return true;
        return false;
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        try
        {
            if (Owner != null)
                ownerWindowState = Owner.WindowState;

            Rect work = SystemParameters.WorkArea;
            Rect bounds = new(Left, Top, ActualWidth, ActualHeight);
            if (!bounds.IntersectsWith(work))
            {
                Left = Owner != null ? Owner.Left + Math.Max(0, (Owner.ActualWidth - Width) / 2) : work.Left + (work.Width - Width) / 2;
                Top = Owner != null ? Owner.Top + Math.Max(0, (Owner.ActualHeight - Height) / 2) : work.Top + (work.Height - Height) / 2;
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

public sealed class PartyEconomyApplyEventArgs : EventArgs
{
    public PartyEconomyApplyEventArgs(ProgressionType operationType, PartyEconomySettings settings)
    {
        OperationType = operationType;
        Settings = settings.DeepClone();
    }
    public ProgressionType OperationType { get; }
    public PartyEconomySettings Settings { get; }
}
