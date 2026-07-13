namespace WartalesEditor.Services;

public interface IFileDialogService
{
    string? ShowOpenFileDialog(
        string filter,
        string? initialFileName = null);

    string? ShowSaveFileDialog(
        string filter,
        string? initialFileName = null);
}