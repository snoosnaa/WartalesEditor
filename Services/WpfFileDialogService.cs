using Microsoft.Win32;

namespace WartalesEditor.Services;

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

        return dialog.ShowDialog() == true
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

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}