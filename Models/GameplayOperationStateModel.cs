using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Models;

public sealed class GameplayOperationStateModel
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; init; } =
        CurrentFormatVersion;

    public ProgressionType OperationType { get; init; }

    public string TargetSheet { get; init; } =
        string.Empty;

    public string TargetEntry { get; init; } =
        string.Empty;

    public string TargetPath { get; init; } =
        string.Empty;

    public JArray BaselineArray { get; init; } =
        new();

    public int AppliedPercentage { get; init; } = 100;

    public string BaselineFingerprint { get; init; } =
        string.Empty;

    public string ExpectedCurrentFingerprint { get; init; } =
        string.Empty;

    public int ElementCount { get; init; }

    public string ElementShapeFingerprint { get; init; } =
        string.Empty;

    public string ProjectCompatibilityIdentity { get; init; } =
        string.Empty;

    public StartingResourcesSettings? StartingResources { get; init; }

    public JObject? GameplaySettings { get; init; }

    [JsonIgnore]
    public bool IsCompatible { get; set; } = true;

    [JsonIgnore]
    public string CompatibilityMessage { get; set; } =
        string.Empty;

    [JsonIgnore]
    public string PersistedStateFingerprint { get; set; } =
        string.Empty;

    public GameplayOperationStateModel DeepClone()
    {
        return new GameplayOperationStateModel
        {
            FormatVersion = FormatVersion,
            OperationType = OperationType,
            TargetSheet = TargetSheet,
            TargetEntry = TargetEntry,
            TargetPath = TargetPath,
            BaselineArray = (JArray)BaselineArray.DeepClone(),
            AppliedPercentage = AppliedPercentage,
            BaselineFingerprint = BaselineFingerprint,
            ExpectedCurrentFingerprint = ExpectedCurrentFingerprint,
            ElementCount = ElementCount,
            ElementShapeFingerprint = ElementShapeFingerprint,
            ProjectCompatibilityIdentity = ProjectCompatibilityIdentity,
            StartingResources = StartingResources?.DeepClone(),
            GameplaySettings = (JObject?)GameplaySettings?.DeepClone(),
            IsCompatible = IsCompatible,
            CompatibilityMessage = CompatibilityMessage,
            PersistedStateFingerprint = PersistedStateFingerprint
        };
    }
}
