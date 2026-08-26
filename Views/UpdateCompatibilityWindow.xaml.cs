using System.Windows;

namespace WartalesEditor.Views;

public partial class UpdateCompatibilityWindow : Window
{
    public UpdateCompatibilityWindow()
    {
        InitializeComponent();
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
