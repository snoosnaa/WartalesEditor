namespace WartalesEditor.Models.Validation;

public sealed class ValidationPresentationModel
{
    public ValidationPresentationModel(
        string title,
        string summary,
        ValidationSeverity highestSeverity,
        bool canContinue)
    {
        Title =
            title;

        Summary =
            summary;

        HighestSeverity =
            highestSeverity;

        CanContinue =
            canContinue;
    }

    public string Title { get; }

    public string Summary { get; }

    public ValidationSeverity HighestSeverity
    {
        get;
    }

    public bool CanContinue { get; }

    public bool HasErrors =>
        HighestSeverity
        == ValidationSeverity.Error;

    public bool HasWarnings =>
        HighestSeverity
        == ValidationSeverity.Warning;
}
