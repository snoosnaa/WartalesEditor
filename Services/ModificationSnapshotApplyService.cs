using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModificationSnapshotApplyService
{
    public ModificationApplyResultModel Apply(
        ModificationPreviewResultModel previewResult)
    {
        ArgumentNullException.ThrowIfNull(
            previewResult);

        List<ModificationApplyItemResultModel>
            results =
                new();

        foreach (ModificationPreviewItemModel previewItem
                 in previewResult.Items)
        {
            results.Add(
                ApplyItem(previewItem));
        }

        return new ModificationApplyResultModel(
            results);
    }

    private static ModificationApplyItemResultModel
        ApplyItem(
            ModificationPreviewItemModel previewItem)
    {
        return previewItem.Status switch
        {
            ModificationPreviewStatus.SafeToApply =>
                ApplySafeItem(previewItem),

            ModificationPreviewStatus.AlreadyApplied =>
                CreateAlreadyAppliedResult(
                    previewItem),

            ModificationPreviewStatus.Conflict =>
                CreateSkippedResult(
                    previewItem,
                    "The snapshot value was not applied " +
                    "because the target property conflicts " +
                    "with the snapshot's original value."),

            ModificationPreviewStatus.NotMatched =>
                CreateSkippedResult(
                    previewItem,
                    previewItem.Reason),

            ModificationPreviewStatus
                .InvalidSnapshotChange =>
                    CreateSkippedResult(
                        previewItem,
                        "The snapshot value was not applied " +
                        "because the snapshot does not contain " +
                        "an actual value change."),

            _ =>
                CreateSkippedResult(
                    previewItem,
                    "The snapshot value was not applied " +
                    "because the preview status is not " +
                    "supported.")
        };
    }

    private static ModificationApplyItemResultModel
        ApplySafeItem(
            ModificationPreviewItemModel previewItem)
    {
        ModificationMatchItemModel matchItem =
            previewItem.MatchItem;

        if (!matchItem.IsMatched ||
            matchItem.TargetProperty == null)
        {
            return new ModificationApplyItemResultModel(
                matchItem,
                ModificationApplyStatus.NotMatched,
                "The preview marked this item as safe, " +
                "but the target property is no longer " +
                "available.");
        }

        JToken requestedValue =
            matchItem.SnapshotProperty
                .CurrentValue;

        JToken currentTargetValue =
            matchItem.TargetProperty
                .GetCurrentValueSnapshot();

        if (!JToken.DeepEquals(
                currentTargetValue,
                previewItem.TargetValue))
        {
            return new ModificationApplyItemResultModel(
                matchItem,
                ModificationApplyStatus.NotMatched,
                "The target property changed after the " +
                "merge preview was created. The snapshot " +
                "value was not applied.");
        }

        if (JToken.DeepEquals(
                currentTargetValue,
                requestedValue))
        {
            return new ModificationApplyItemResultModel(
                matchItem,
                ModificationApplyStatus
                    .NoChangeRequired,
                "The target property already contains " +
                "the snapshot value.");
        }

        try
        {
            matchItem.TargetProperty
                .ApplySnapshotValue(
                    requestedValue);

            JToken appliedValue =
                matchItem.TargetProperty
                    .GetCurrentValueSnapshot();

            if (!JToken.DeepEquals(
                    appliedValue,
                    requestedValue))
            {
                return new ModificationApplyItemResultModel(
                    matchItem,
                    ModificationApplyStatus.Failed,
                    "The target property did not retain " +
                    "the requested snapshot value.");
            }

            return new ModificationApplyItemResultModel(
                matchItem,
                ModificationApplyStatus.Applied,
                "The snapshot value was applied safely.");
        }
        catch (Exception exception)
        {
            return new ModificationApplyItemResultModel(
                matchItem,
                ModificationApplyStatus.Failed,
                "The snapshot value could not be applied: " +
                exception.Message);
        }
    }

    private static ModificationApplyItemResultModel
        CreateAlreadyAppliedResult(
            ModificationPreviewItemModel previewItem)
    {
        return new ModificationApplyItemResultModel(
            previewItem.MatchItem,
            ModificationApplyStatus
                .NoChangeRequired,
            "The target property already contains " +
            "the snapshot value.");
    }

    private static ModificationApplyItemResultModel
        CreateSkippedResult(
            ModificationPreviewItemModel previewItem,
            string reason)
    {
        return new ModificationApplyItemResultModel(
            previewItem.MatchItem,
            ModificationApplyStatus.NotMatched,
            reason);
    }
}