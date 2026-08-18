using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public enum SnapshotPropertyResolutionStatus
{
    ExactPathMatch,
    UniqueLegacyNameMatch,
    Ambiguous,
    NotFound
}

public sealed class SnapshotPropertyResolutionResult
{
    public SnapshotPropertyResolutionResult(
        SnapshotPropertyResolutionStatus status,
        IReadOnlyList<PropertyModel> matches)
    {
        Status = status;
        Matches = matches ?? throw new ArgumentNullException(nameof(matches));
    }

    public SnapshotPropertyResolutionStatus Status { get; }

    public IReadOnlyList<PropertyModel> Matches { get; }

    public PropertyModel? Property =>
        Matches.Count == 1
            ? Matches[0]
            : null;
}

public sealed class SnapshotPropertyResolutionService
{
    public SnapshotPropertyResolutionResult Resolve(
        EntryModel entry,
        ModificationSnapshotPropertyModel snapshotProperty)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(snapshotProperty);

        bool isLegacy = string.IsNullOrWhiteSpace(
            snapshotProperty.PropertyPath);

        List<PropertyModel> matches = entry.Properties
            .Where(property =>
                isLegacy
                    ? string.Equals(
                        property.Name,
                        snapshotProperty.Name,
                        StringComparison.Ordinal)
                    : string.Equals(
                        property.EffectivePropertyPath,
                        snapshotProperty.PropertyPath,
                        StringComparison.Ordinal))
            .ToList();

        if (matches.Count == 0)
        {
            return new SnapshotPropertyResolutionResult(
                SnapshotPropertyResolutionStatus.NotFound,
                matches);
        }

        if (matches.Count > 1)
        {
            return new SnapshotPropertyResolutionResult(
                SnapshotPropertyResolutionStatus.Ambiguous,
                matches);
        }

        return new SnapshotPropertyResolutionResult(
            isLegacy
                ? SnapshotPropertyResolutionStatus.UniqueLegacyNameMatch
                : SnapshotPropertyResolutionStatus.ExactPathMatch,
            matches);
    }
}
