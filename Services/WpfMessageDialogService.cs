using System.Windows;

namespace WartalesEditor.Services;

using System.Linq;

public sealed class WpfMessageDialogService :
    IMessageDialogService
{
    public void ShowInformation(
        string message,
        string title)
    {
        ShowMessage(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowWarning(
        string message,
        string title)
    {
        ShowMessage(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public void ShowError(
        string message,
        string title)
    {
        ShowMessage(
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
            ShowMessage(
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
            ShowMessage(
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

    private static MessageBoxResult ShowMessage(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage image)
    {
        Window? owner =
            ResolveOwner();

        return owner == null
            ? MessageBox.Show(
                message,
                title,
                buttons,
                image)
            : MessageBox.Show(
                owner,
                message,
                title,
                buttons,
                image);
    }

    private static Window? ResolveOwner()
    {
        Application? application =
            Application.Current;

        return application?.Windows
                   .OfType<Window>()
                   .FirstOrDefault(window =>
                       window.IsActive)
               ??
               application?.MainWindow;
    }
}
