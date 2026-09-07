using System;
using System.Collections.Generic;

namespace WartalesEditor.Models.Profiles;

public sealed class ModProfileSummaryModel
{
    public string FileName { get; init; }
        = string.Empty;

    public string FilePath { get; init; }
        = string.Empty;

    public string Name { get; init; }
        = string.Empty;

    public string Description { get; init; }
        = string.Empty;

    public string Author { get; init; }
        = string.Empty;

    public string ProfileVersion { get; init; }
        = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset ModifiedAtUtc { get; init; }

    public IReadOnlyList<string> Tags { get; init; }
        = Array.Empty<string>();

    public int CategoryCount { get; init; }

    public int SettingCount { get; init; }

    public int PropertyCount { get; init; }

    public int OperationCount { get; init; }

    public int EffectiveChangeCount { get; init; }

    public bool IsEffectiveChangeCountExact { get; init; }

    public string ChangeSummaryText
    {
        get
        {
            if (IsEffectiveChangeCountExact)
            {
                return EffectiveChangeCount == 1
                    ? "1 change"
                    : $"{EffectiveChangeCount:N0} changes";
            }

            string propertyText = EffectiveChangeCount == 1
                ? "1 property change"
                : $"{EffectiveChangeCount:N0} property changes";
            string operationText = OperationCount == 1
                ? "1 gameplay setting"
                : $"{OperationCount:N0} gameplay settings";

            if (EffectiveChangeCount == 0)
            {
                return operationText;
            }

            if (OperationCount == 0)
            {
                return propertyText;
            }

            return $"{propertyText} + {operationText}";
        }
    }

    public bool HasChanges =>
        EffectiveChangeCount > 0 ||
        OperationCount > 0;
}
