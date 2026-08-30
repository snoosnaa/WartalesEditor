using System.ComponentModel;
using System.Windows;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class QuickBmsExportProgressDialog : Window
{
    private bool allowClose;

    public QuickBmsExportProgressDialog(
        QuickBmsExportProgressViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
        Closing += OnClosing;
    }

    public void AllowCloseAndClose()
    {
        allowClose = true;
        Closing -= OnClosing;
        Close();
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        (DataContext as QuickBmsExportProgressViewModel)?
            .RequestCancellation();
    }

    private void OnClosing(
        object? sender,
        CancelEventArgs e)
    {
        if (allowClose)
            return;

        e.Cancel = true;
        (DataContext as QuickBmsExportProgressViewModel)?
            .RequestCancellation();
    }
}
