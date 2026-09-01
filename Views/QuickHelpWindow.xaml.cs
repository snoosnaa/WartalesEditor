using System.Windows;

namespace WartalesEditor.Views;

public partial class QuickHelpWindow : Window
{
    public event EventHandler? OpenUserGuideRequested;

    public QuickHelpWindow()
    {
        InitializeComponent();
    }

    private void OpenUserGuideButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenUserGuideRequested?.Invoke(
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
