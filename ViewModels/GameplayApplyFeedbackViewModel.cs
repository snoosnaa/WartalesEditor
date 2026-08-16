using WartalesEditor.Helpers;

namespace WartalesEditor.ViewModels;

public sealed class GameplayApplyFeedbackViewModel : ObservableObject
{
    private bool isVisible;
    private string heading = string.Empty;
    private string message = string.Empty;

    public bool IsVisible
    {
        get => isVisible;
        private set => SetProperty(ref isVisible, value);
    }

    public string Heading
    {
        get => heading;
        private set => SetProperty(ref heading, value);
    }

    public string Message
    {
        get => message;
        private set => SetProperty(ref message, value);
    }

    public void ShowApplied(string message) =>
        Show("Applied successfully", message);

    public void ShowAlreadyApplied() =>
        Show(
            "Already applied",
            "The current project already matches this setting.");

    public void Clear()
    {
        Heading = string.Empty;
        Message = string.Empty;
        IsVisible = false;
    }

    private void Show(string heading, string message)
    {
        Heading = heading;
        Message = message;
        IsVisible = true;
    }
}
