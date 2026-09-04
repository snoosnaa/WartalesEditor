namespace WartalesEditor.Services;

public interface IFileDialogService
{
    string? ShowOpenFileDialog(
        string filter,
        string? initialFileName = null);

    string? ShowOpenFileDialog(
        string filter,
        string? initialFileName,
        string? initialDirectory) =>
        ShowOpenFileDialog(
            filter,
            initialFileName);

    string? ShowSaveFileDialog(
        string filter,
        string? initialFileName = null);

    string? ShowOpenFolderDialog(
        string title,
        string? initialDirectory = null) =>
        null;
}
