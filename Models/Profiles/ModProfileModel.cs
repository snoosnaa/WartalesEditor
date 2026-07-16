using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Models.Profiles;

public sealed class ModProfileModel
{
    public int FormatVersion { get; init; }
        = ModProfileFormat.CurrentVersion;

    public ModProfileMetadataModel Metadata
    {
        get;
        init;
    } = new();

    public ModificationSnapshotModel Snapshot
    {
        get;
        init;
    } = new();
}