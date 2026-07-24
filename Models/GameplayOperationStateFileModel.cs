using System.Collections.Generic;

namespace WartalesEditor.Models;

public sealed class GameplayOperationStateFileModel
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; init; } =
        CurrentFormatVersion;

    public string SourceFileName { get; init; } =
        string.Empty;

    public List<GameplayOperationStateModel> Operations
    {
        get;
        init;
    } = new();
}
