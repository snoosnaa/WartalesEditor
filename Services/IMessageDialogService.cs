namespace WartalesEditor.Services;

public interface IMessageDialogService
{
    void ShowInformation(
        string message,
        string title);

    void ShowWarning(
        string message,
        string title);

    void ShowError(
        string message,
        string title);

    bool ShowConfirmation(
        string message,
        string title);

    UnsavedChangesResult ShowUnsavedChanges(
        string message,
        string title);
}