namespace WartalesEditor.Models.Snapshots;

public sealed class ModificationSnapshotExportResultModel
{
    public ModificationSnapshotExportResultModel(
        ModificationSnapshotModel snapshot,
        string fileName)
    {
        Snapshot = snapshot;
        FileName = fileName;
    }

    public ModificationSnapshotModel Snapshot
    {
        get;
    }

    public string FileName { get; }

    public int CategoryCount =>
        Snapshot.Categories.Count;

    public int SettingCount
    {
        get
        {
            int count = 0;

            foreach (ModificationSnapshotCategoryModel category
                     in Snapshot.Categories)
            {
                count += category.Settings.Count;
            }

            return count;
        }
    }

    public int PropertyCount
    {
        get
        {
            int count = 0;

            foreach (ModificationSnapshotCategoryModel category
                     in Snapshot.Categories)
            {
                foreach (ModificationSnapshotSettingModel setting
                         in category.Settings)
                {
                    count += setting.Properties.Count;
                }
            }

            return count;
        }
    }

    public bool HasChanges =>
        PropertyCount > 0;
}