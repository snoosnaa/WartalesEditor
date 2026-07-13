namespace WartalesEditor.Models.Snapshots;

public enum ModificationPreviewStatus
{
    SafeToApply,

    AlreadyApplied,

    Conflict,

    NotMatched,

    InvalidSnapshotChange
}