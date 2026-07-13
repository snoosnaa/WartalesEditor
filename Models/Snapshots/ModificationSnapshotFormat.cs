namespace WartalesEditor.Models.Snapshots;

public static class ModificationSnapshotFormat
{
    public const int CurrentVersion = 1;

    public const string FileExtension =
        ".wtsnapshot";

    public const string FileFilter =
        "Wartales Modification Snapshots (*.wtsnapshot)|" +
        "*.wtsnapshot|" +
        "JSON Files (*.json)|*.json|" +
        "All Files (*.*)|*.*";
}