using System.Collections.Generic;

namespace WartalesEditor.Models;

public sealed class GameplayOperationStateFileModel
{
    public const int CurrentFormatVersion = 2;

    public const int LegacyFormatVersion = 1;

    public int FormatVersion { get; init; } =
        CurrentFormatVersion;

    public string SourceFileName { get; init; } =
        string.Empty;

    public string? SourceCdbGenerationIdentity { get; init; }

    public string CurrentCdbContentIdentity { get; init; } =
        string.Empty;

    public List<GameplayOperationStateModel> Operations
    {
        get;
        init;
    } = new();

    public List<GameplayOperationStateModel> HistoricalOperations
    {
        get;
        init;
    } = new();
}
