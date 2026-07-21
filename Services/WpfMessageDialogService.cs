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

    public UnsavedChangesResult ShowUnsavedChanges(
        string message,
        string title)
    {
        MessageBoxResult result =
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

        return result switch
        {
            MessageBoxResult.Yes =>
                UnsavedChangesResult.Save,

            MessageBoxResult.No =>
                UnsavedChangesResult.Discard,

            _ =>
                UnsavedChangesResult.Cancel
        };
    }
}