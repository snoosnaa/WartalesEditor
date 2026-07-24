namespace WartalesEditor.Models.Profiles;

public sealed class ProfileOperationApplyItemResultModel
{
    public ProfileOperationApplyItemResultModel(
        string operationId,
        string displayName,
        ProfileOperationApplyStatus status,
        string message)
    {
        OperationId = operationId;
        DisplayName = displayName;
        Status = status;
        Message = message;
    }

    public string OperationId { get; }

    public string DisplayName { get; }

    public ProfileOperationApplyStatus Status { get; }

    public string Message { get; }
}
