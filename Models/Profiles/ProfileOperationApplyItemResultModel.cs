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
            null)
    {
    }

    public ProfileOperationApplyItemResultModel(
        string operationId,
        string displayName,
        ProfileOperationApplyStatus status,
        string message,
        string? alreadyConfiguredSummary)
    {
        OperationId = operationId;
        DisplayName = displayName;
        Status = status;
        Message = message;
        AlreadyConfiguredSummary = alreadyConfiguredSummary;
    }

    public string OperationId { get; }

    public string DisplayName { get; }

    public ProfileOperationApplyStatus Status { get; }

    public string Message { get; }

    public string? AlreadyConfiguredSummary { get; }
}
