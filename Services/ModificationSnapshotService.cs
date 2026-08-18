using System;
using System.IO;
using WartalesEditor.Models;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModificationSnapshotService
{
    public ModificationSnapshotModel CreateSnapshot(
        ProjectModel project,
        string editorVersion = "")
    {
        ArgumentNullException.ThrowIfNull(project);

        ModificationSnapshotModel snapshot = new()
        {
            CreatedAtUtc = DateTimeOffset.UtcNow,
            EditorVersion = editorVersion,
            SourceFileName =
                GetSourceFileName(project)
        };

        snapshot.GameplayOperationStates.AddRange(
            project.GameplayOperationStates
                .Where(state => state.IsCompatible)
                .Select(state => state.DeepClone()));

        foreach (SheetModel category in project.Sheets)
        {
            ModificationSnapshotCategoryModel?
                snapshotCategory = null;

            foreach (EntryModel setting in category.Entries)
            {
                ModificationSnapshotSettingModel?
                    snapshotSetting = null;

                foreach (PropertyModel property in
                         setting.Properties)
                {
                    if (!property.IsModified)
                        continue;

                    snapshotCategory ??=
                        CreateCategory(category);

                    snapshotSetting ??=
                        CreateSetting(setting);

                    snapshotSetting.Properties.Add(
                        CreateProperty(property));
                }

                if (snapshotSetting != null)
                {
                    snapshotCategory!.Settings.Add(
                        snapshotSetting);
                }
            }

            if (snapshotCategory != null)
            {
                snapshot.Categories.Add(
                    snapshotCategory);
            }
        }

        return snapshot;
    }

    private static ModificationSnapshotCategoryModel
        CreateCategory(
            SheetModel category)
    {
        return new ModificationSnapshotCategoryModel
        {
            Name = category.Name
        };
    }

    private static ModificationSnapshotSettingModel
        CreateSetting(
            EntryModel setting)
    {
        return new ModificationSnapshotSettingModel
        {
            Id = setting.Id,
            Name = setting.Name,
            DisplayName = setting.DisplayName
        };
    }

    private static ModificationSnapshotPropertyModel
        CreateProperty(
            PropertyModel property)
    {
        return new ModificationSnapshotPropertyModel
        {
            Name = property.Name,
            PropertyPath = property.EffectivePropertyPath,
            OriginalPropertyExisted = !property.IsStructurallyAdded,
            OriginalValue =
                property.GetOriginalValueSnapshot(),
            CurrentValue =
                property.GetCurrentValueSnapshot()
        };
    }

    private static string GetSourceFileName(
        ProjectModel project)
    {
        if (string.IsNullOrWhiteSpace(
                project.FileName))
        {
            return string.Empty;
        }

        return Path.GetFileName(
            project.FileName);
    }
}
