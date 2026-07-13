using System.Windows;

namespace WartalesEditor.Services;

public sealed class WpfMessageDialogService :
    IMessageDialogService
{
    public void ShowInformation(
        string message,
        string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowWarning(
        string message,
        string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public void ShowError(
        string message,
        string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public bool ShowConfirmation(
        string message,
        string title)
    {
        MessageBoxResult result =
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        return result ==
            MessageBoxResult.Yes;
    }
}