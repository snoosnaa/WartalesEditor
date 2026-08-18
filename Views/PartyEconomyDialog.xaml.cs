using System;
using System.Windows;
using System.Windows.Controls;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class PartyEconomyDialog : Window
{
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

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PartyEconomyDialogViewModel vm ||
            !vm.TryRestorePreviousValues() ||
            !vm.CanApply)
            return;

        ApplyRequested?.Invoke(
            this,
            new PartyEconomyApplyEventArgs(
                vm.OperationType,
                vm.CreateSettings(),
                true));
    }

    private void NoWagesButton_Click(object sender, RoutedEventArgs e) =>
        ((PartyEconomyDialogViewModel)DataContext).SetNoWages();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        base.OnClosed(e);
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
        : this(operationType, settings, false)
    {
    }

    public PartyEconomyApplyEventArgs(
        ProgressionType operationType,
        PartyEconomySettings settings,
        bool restorePreviousValues)
    {
        OperationType = operationType;
        Settings = settings.DeepClone();
        RestorePreviousValues = restorePreviousValues;
    }
    public ProgressionType OperationType { get; }
    public PartyEconomySettings Settings { get; }
    public bool RestorePreviousValues { get; }
}
