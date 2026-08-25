using System;
using System.Windows;

namespace WartalesEditor.Views;

public partial class LanguageDataDialog :
    Window
{
    public event EventHandler? SelectionRequested;

    public LanguageDataDialog()
    {
        InitializeComponent();
    }

    private void SelectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectionRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
