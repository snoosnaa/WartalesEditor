using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModProfileLibraryService
{
    private readonly ModProfileLibraryPathService
        pathService;

    private readonly ModProfileSerializationService
        serializationService;

    public ModProfileLibraryService()
        : this(
            new ModProfileLibraryPathService(),
            new ModProfileSerializationService())
    {
    }

    public ModProfileLibraryService(
        ModProfileLibraryPathService pathService,
        ModProfileSerializationService
            serializationService)
    {
        this.pathService =
            pathService
            ?? throw new ArgumentNullException(
                nameof(pathService));

        this.serializationService =
            serializationService
            ?? throw new ArgumentNullException(
                nameof(serializationService));
    }

    public IReadOnlyList<ModProfileSummaryModel>
        GetProfiles()
    {
        string libraryDirectory =
            pathService.EnsureLibraryDirectory();

        List<ModProfileSummaryModel> profiles =
            new();

        foreach (string file in Directory.EnumerateFiles(
                     libraryDirectory,
                     "*" +
                     ModProfileFormat.DefaultFileExtension,
                     SearchOption.TopDirectoryOnly))
        {
            try
            {
                ModProfileModel profile =
                    serializationService.Load(file);

                profiles.Add(
                    CreateSummary(
                        profile,
                        file));
            }
            catch
            {
                // Ignore invalid profile files for now.
                // Future versions may expose these in
                // a validation report.
            }
        }

        return profiles
            .OrderBy(profile => profile.Name)
            .ThenBy(profile => profile.FileName)
            .ToList();
    }

    private static ModProfileSummaryModel
        CreateSummary(
            ModProfileModel profile,
            string filePath)
    {
        int settingCount = 0;
        int propertyCount = 0;

        foreach (ModificationSnapshotCategoryModel category
                 in profile.Snapshot.Categories)
        {
            settingCount +=
                category.Settings.Count;

            foreach (ModificationSnapshotSettingModel setting
                     in category.Settings)
            {
                propertyCount +=
                    setting.Properties.Count;
            }
        }

        return new ModProfileSummaryModel
        {
            FileName =
                Path.GetFileName(filePath),

            FilePath =
                filePath,

            Name =
                profile.Metadata.Name,

            Description =
                profile.Metadata.Description,

            Author =
                profile.Metadata.Author,

            ProfileVersion =
                profile.Metadata.ProfileVersion,

            CreatedAtUtc =
                profile.Metadata.CreatedAtUtc,

            ModifiedAtUtc =
                profile.Metadata.ModifiedAtUtc,

            Tags =
                profile.Metadata.Tags
                    .ToList(),

            CategoryCount =
                profile.Snapshot.Categories.Count,

            SettingCount =
                settingCount,

            PropertyCount =
                propertyCount
        };
    }
}