using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models.Profiles;

public sealed class ProfileOperationRequestModel
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; init; } =
        CurrentFormatVersion;

    public string OperationId { get; init; } =
        string.Empty;

    public JObject? Settings { get; init; }
}
