using Microsoft.Win32;

namespace WartalesEditor.Services;

using System.Linq;
using System.Windows;

public sealed class WpfFileDialogService :
    IFileDialogService
{
    public string? ShowOpenFileDialog(
        string filter,
        string? initialFileName = null)
    {
        OpenFileDialog dialog = new()
        {
            Filter = filter
        };

        if (!string.IsNullOrWhiteSpace(
                initialFileName))
        {
            dialog.FileName =
                initialFileName;
        }

        Window? owner =
            ResolveOwner();

        bool? result =
            owner == null
                ? dialog.ShowDialog()
                : dialog.ShowDialog(owner);

        return result == true
            ? dialog.FileName
            : null;
    }

    public string? ShowSaveFileDialog(
        string filter,
        string? initialFileName = null)
    {
        SaveFileDialog dialog = new()
        {
            Filter = filter
        };

        if (!string.IsNullOrWhiteSpace(
                initialFileName))
        {
            dialog.FileName =
                initialFileName;
        }

        Window? owner =
            ResolveOwner();

        bool? result =
            owner == null
                ? dialog.ShowDialog()
                : dialog.ShowDialog(owner);

        return result == true
            ? dialog.FileName
            : null;
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
