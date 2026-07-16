using System.Windows;

namespace WartalesEditor.Views;

public partial class ProfileManagerWindow : Window
{
    public ProfileManagerWindow()
    {
        InitializeComponent();
    }

    private void OnCloseButtonClick(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}