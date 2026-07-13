using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModificationSnapshotPreviewService
{
    public ModificationPreviewResultModel Preview(
        ModificationMatchResultModel matchResult)
    {
        ArgumentNullException.ThrowIfNull(
            matchResult);

        List<ModificationPreviewItemModel> results =
            new();

        foreach (ModificationMatchItemModel matchItem
                 in matchResult.Items)
        {
            results.Add(
                PreviewItem(matchItem));
        }

        return new ModificationPreviewResultModel(
            results);
    }

    private static ModificationPreviewItemModel
        PreviewItem(
            ModificationMatchItemModel matchItem)
    {
        if (!matchItem.IsMatched ||
            matchItem.TargetProperty == null)
        {
            return new ModificationPreviewItemModel(
                matchItem,
                ModificationPreviewStatus.NotMatched,
                matchItem.Reason,
                targetValue: null);
        }

        JToken snapshotOriginalValue =
            matchItem.SnapshotProperty
                .OriginalValue;

        JToken snapshotCurrentValue =
            matchItem.SnapshotProperty
                .CurrentValue;

        JToken targetCurrentValue =
            matchItem.TargetProperty
                .GetCurrentValueSnapshot();

        if (JToken.DeepEquals(
                snapshotOriginalValue,
                snapshotCurrentValue))
        {
            return new ModificationPreviewItemModel(
                matchItem,
                ModificationPreviewStatus
                    .InvalidSnapshotChange,
                "The snapshot original and requested " +
                "values are identical.",
                targetCurrentValue);
        }

        if (JToken.DeepEquals(
                targetCurrentValue,
                snapshotCurrentValue))
        {
            return new ModificationPreviewItemModel(
                matchItem,
                ModificationPreviewStatus
                    .AlreadyApplied,
                "The target property already contains " +
                "the snapshot value.",
                targetCurrentValue);
        }

        if (JToken.DeepEquals(
                targetCurrentValue,
                snapshotOriginalValue))
        {
            return new ModificationPreviewItemModel(
                matchItem,
                ModificationPreviewStatus
                    .SafeToApply,
                "The target property still contains " +
                "the snapshot's original value.",
                targetCurrentValue);
        }

        return new ModificationPreviewItemModel(
            matchItem,
            ModificationPreviewStatus.Conflict,
            "The target property has changed from the " +
            "snapshot's original value and does not " +
            "already contain the requested value.",
            targetCurrentValue);
    }
}