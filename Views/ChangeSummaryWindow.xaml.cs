using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class ChangeSummaryWindow : Window
{
    public ChangeSummaryWindow()
    {
        InitializeComponent();

        ChangeSummaryDataGrid.MouseDoubleClick +=
            OnChangeSummaryDataGridMouseDoubleClick;
    }

    protected override void OnClosed(
        System.EventArgs e)
    {
        ChangeSummaryDataGrid.MouseDoubleClick -=
            OnChangeSummaryDataGridMouseDoubleClick;

        base.OnClosed(e);
    }

    private void OnChangeSummaryDataGridMouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        DependencyObject? source =
            e.OriginalSource as DependencyObject;

        if (source == null)
            return;

        DataGridRow? clickedRow =
            ItemsControl.ContainerFromElement(
                ChangeSummaryDataGrid,
                source)
            as DataGridRow;

        if (clickedRow?.Item is not
            ChangeSummaryItemModel clickedItem)
        {
            return;
        }

        if (DataContext is not
            ChangeSummaryViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedItem = clickedItem;

        if (!viewModel.NavigateCommand.CanExecute(null))
            return;

        viewModel.NavigateCommand.Execute(null);

        e.Handled = true;
    }

    private void OnCloseButtonClick(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}