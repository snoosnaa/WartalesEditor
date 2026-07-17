using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WartalesEditor.Models.Validation;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class ValidationResultsWindow : Window
{
    public ValidationResultsWindow()
    {
        InitializeComponent();

        ValidationResultsDataGrid.MouseDoubleClick +=
            OnValidationResultsDataGridMouseDoubleClick;
    }

    protected override void OnClosed(
        EventArgs e)
    {
        ValidationResultsDataGrid.MouseDoubleClick -=
            OnValidationResultsDataGridMouseDoubleClick;

        base.OnClosed(e);
    }

    private void OnValidationResultsDataGridMouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        DependencyObject? source =
            e.OriginalSource
                as DependencyObject;

        if (source == null)
        {
            return;
        }

        DataGridRow? clickedRow =
            ItemsControl.ContainerFromElement(
                ValidationResultsDataGrid,
                source)
            as DataGridRow;

        if (clickedRow?.Item
            is not ValidationIssueModel clickedIssue)
        {
            return;
        }

        if (DataContext
            is not ValidationResultsViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedIssue =
            clickedIssue;

        if (!viewModel.NavigateCommand
                .CanExecute(null))
        {
            return;
        }

        viewModel.NavigateCommand.Execute(
            null);

        e.Handled =
            true;
    }

    private void OnCloseButtonClick(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}