namespace WartalesEditor.Models.Profiles;

public sealed class ProfileOperationApplyItemResultModel
{
    public ProfileOperationApplyItemResultModel(
        string operationId,
        string displayName,
        ProfileOperationApplyStatus status,
        string message)
        : this(
            operationId,
            displayName,
            status,
            message,
            null,
            false)
    {
    }

    public ProfileOperationApplyItemResultModel(
        string operationId,
        string displayName,
        ProfileOperationApplyStatus status,
        string message,
        string? alreadyConfiguredSummary)
        : this(
            operationId,
            displayName,
            status,
            message,
            alreadyConfiguredSummary,
            false)
    {
    }

    public ProfileOperationApplyItemResultModel(
        string operationId,
        string displayName,
        ProfileOperationApplyStatus status,
        string message,
        string? alreadyConfiguredSummary,
        bool hasWarning)
    {
        OperationId = operationId;
        DisplayName = displayName;
        Status = status;
        Message = message;
        AlreadyConfiguredSummary = alreadyConfiguredSummary;
        HasWarning = hasWarning;
    }

    public string OperationId { get; }

    public string DisplayName { get; }

    public ProfileOperationApplyStatus Status { get; }

    public string Message { get; }

    public string? AlreadyConfiguredSummary { get; }

    public bool HasWarning { get; }
}
