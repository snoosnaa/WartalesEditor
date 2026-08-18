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

    private readonly ProfileEffectiveChangeCountService
        effectiveChangeCountService;

    public ModProfileLibraryService()
        : this(
            new ModProfileLibraryPathService(),
            new ModProfileSerializationService(),
            new ProfileEffectiveChangeCountService())
    {
    }

    public ModProfileLibraryService(
        ModProfileLibraryPathService pathService,
        ModProfileSerializationService
            serializationService)
        : this(
            pathService,
            serializationService,
            new ProfileEffectiveChangeCountService())
    {
    }

    public ModProfileLibraryService(
        ModProfileLibraryPathService pathService,
        ModProfileSerializationService
            serializationService,
        ProfileEffectiveChangeCountService
            effectiveChangeCountService)
    {
        this.pathService =
            pathService
            ?? throw new ArgumentNullException(
                nameof(pathService));

        this.serializationService =
            serializationService
            ?? throw new ArgumentNullException(
                nameof(serializationService));

        this.effectiveChangeCountService =
            effectiveChangeCountService
            ?? throw new ArgumentNullException(
                nameof(effectiveChangeCountService));
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

    public ModProfileSummaryModel AddProfile(
        ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        string profileName =
            ValidateProfileName(
                profile.Metadata.Name,
                nameof(profile));

        string destinationFile =
            GetUniqueProfileFilePath(
                profileName);

        serializationService.Save(
            profile,
            destinationFile);

        return CreateSummary(
            profile,
            destinationFile);
    }

    public ModProfileModel LoadProfile(
        ModProfileSummaryModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        string profileFile = GetValidatedLibraryProfilePath(profile);
        if (!File.Exists(profileFile))
        {
            throw new FileNotFoundException(
                "The selected profile file could not be found.",
                profileFile);
        }

        return serializationService.Load(profileFile);
    }

    public ModProfileSummaryModel UpdateProfile(
        ModProfileSummaryModel selectedProfile,
        ModProfileModel updatedProfile)
    {
        ArgumentNullException.ThrowIfNull(selectedProfile);
        ArgumentNullException.ThrowIfNull(updatedProfile);

        throw new InvalidOperationException(
            "Managed profile replacement requires semantic candidate " +
            "validation through the profile workflow.");
    }

    public ModProfileSummaryModel UpdateProfile(
        ModProfileSummaryModel selectedProfile,
        ModProfileModel updatedProfile,
        Action<ModProfileModel> validateCandidate)
    {
        ArgumentNullException.ThrowIfNull(selectedProfile);
        ArgumentNullException.ThrowIfNull(updatedProfile);
        ArgumentNullException.ThrowIfNull(validateCandidate);

        string profileFile = GetValidatedLibraryProfilePath(selectedProfile);
        if (!File.Exists(profileFile))
        {
            throw new FileNotFoundException(
                "The selected profile file could not be found.",
                profileFile);
        }

        if (!string.Equals(
                selectedProfile.Name,
                updatedProfile.Metadata.Name,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The updated profile must preserve the selected profile name.");
        }

        string candidateFile =
            profileFile + ".update-" + Guid.NewGuid().ToString("N");

        try
        {
            serializationService.Save(updatedProfile, candidateFile);
            ModProfileModel reloadedCandidate =
                serializationService.Load(candidateFile);

            validateCandidate(reloadedCandidate);
            ModProfileSummaryModel candidateSummary =
                CreateSummary(reloadedCandidate, profileFile);

            File.Replace(
                candidateFile,
                profileFile,
                destinationBackupFileName: null,
                ignoreMetadataErrors: true);

            return candidateSummary;
        }
        finally
        {
            TryDeleteFile(candidateFile);
            TryDeleteFile(candidateFile + ".tmp");
        }
    }

    public ModProfileSummaryModel RenameProfile(
        ModProfileSummaryModel profile,
        string newName,
        string description,
        string author,
        string profileVersion)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        string normalizedName =
            ValidateProfileName(
                newName,
                nameof(newName));

        string sourceFile =
            GetValidatedLibraryProfilePath(
                profile);

        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException(
                "The selected profile file could not be found.",
                sourceFile);
        }

        ModProfileModel sourceProfile =
            serializationService.Load(
                sourceFile);

        ModProfileModel renamedProfile =
            CreateProfileCopy(
                sourceProfile,
                normalizedName,
                description,
                author,
                profileVersion,
                preserveCreationDate: true);

        string destinationFile =
            GetUniqueProfileFilePath(
                normalizedName,
                sourceFile);

        serializationService.Save(
            renamedProfile,
            destinationFile);

        if (!string.Equals(
                sourceFile,
                destinationFile,
                StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(
                sourceFile);
        }

        return CreateSummary(
            renamedProfile,
            destinationFile);
    }

    public ModProfileSummaryModel DuplicateProfile(
        ModProfileSummaryModel profile,
        string newName,
        string description,
        string author,
        string profileVersion)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        string normalizedName =
            ValidateProfileName(
                newName,
                nameof(newName));

        string sourceFile =
            GetValidatedLibraryProfilePath(
                profile);

        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException(
                "The selected profile file could not be found.",
                sourceFile);
        }

        ModProfileModel sourceProfile =
            serializationService.Load(
                sourceFile);

        ModProfileModel duplicatedProfile =
            CreateProfileCopy(
                sourceProfile,
                normalizedName,
                description,
                author,
                profileVersion,
                preserveCreationDate: false);

        string destinationFile =
            GetUniqueProfileFilePath(
                normalizedName);

        serializationService.Save(
            duplicatedProfile,
            destinationFile);

        return CreateSummary(
            duplicatedProfile,
            destinationFile);
    }

    public ModProfileSummaryModel ImportProfile(
        string sourceFile)
    {
        if (string.IsNullOrWhiteSpace(
                sourceFile))
        {
            throw new ArgumentException(
                "A source profile file is required.",
                nameof(sourceFile));
        }

        string fullSourcePath =
            Path.GetFullPath(sourceFile);

        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException(
                "The profile file could not be found.",
                fullSourcePath);
        }

        ValidateProfileFileExtension(
            fullSourcePath);

        ModProfileModel profile =
            serializationService.Load(
                fullSourcePath);

        string libraryDirectory =
            pathService.EnsureLibraryDirectory();

        string destinationFile =
            Path.Combine(
                libraryDirectory,
                Path.GetFileName(
                    fullSourcePath));

        string fullDestinationPath =
            Path.GetFullPath(
                destinationFile);

        if (!string.Equals(
                fullSourcePath,
                fullDestinationPath,
                StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(
                fullSourcePath,
                fullDestinationPath,
                overwrite: true);
        }

        return CreateSummary(
            profile,
            fullDestinationPath);
    }

    public void ExportProfile(
        ModProfileSummaryModel profile,
        string destinationFile)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        if (string.IsNullOrWhiteSpace(
                destinationFile))
        {
            throw new ArgumentException(
                "A destination profile file is required.",
                nameof(destinationFile));
        }

        string sourceFile =
            GetValidatedLibraryProfilePath(
                profile);

        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException(
                "The selected profile file could not be found.",
                sourceFile);
        }

        string fullDestinationPath =
            Path.GetFullPath(
                destinationFile);

        ValidateProfileFileExtension(
            fullDestinationPath);

        string? destinationDirectory =
            Path.GetDirectoryName(
                fullDestinationPath);

        if (!string.IsNullOrWhiteSpace(
                destinationDirectory))
        {
            Directory.CreateDirectory(
                destinationDirectory);
        }

        if (string.Equals(
                sourceFile,
                fullDestinationPath,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        File.Copy(
            sourceFile,
            fullDestinationPath,
            overwrite: true);
    }

    public void DeleteProfile(
        ModProfileSummaryModel profile)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        string profileFile =
            GetValidatedLibraryProfilePath(
                profile);

        if (!File.Exists(profileFile))
        {
            throw new FileNotFoundException(
                "The selected profile file could not be found.",
                profileFile);
        }

        File.Delete(
            profileFile);
    }

    private string GetUniqueProfileFilePath(
        string profileName,
        string? allowedExistingFile = null)
    {
        string libraryDirectory =
            Path.GetFullPath(
                pathService.EnsureLibraryDirectory());

        string safeFileName =
            CreateSafeFileName(
                profileName);

        string candidate =
            Path.Combine(
                libraryDirectory,
                safeFileName +
                ModProfileFormat.DefaultFileExtension);

        string fullAllowedExistingFile =
            string.IsNullOrWhiteSpace(
                allowedExistingFile)
                ? string.Empty
                : Path.GetFullPath(
                    allowedExistingFile);

        if (!File.Exists(candidate)
            ||
            string.Equals(
                Path.GetFullPath(candidate),
                fullAllowedExistingFile,
                StringComparison.OrdinalIgnoreCase))
        {
            return candidate;
        }

        int suffix = 2;

        while (true)
        {
            candidate =
                Path.Combine(
                    libraryDirectory,
                    $"{safeFileName} ({suffix})" +
                    ModProfileFormat.DefaultFileExtension);

            if (!File.Exists(candidate)
                ||
                string.Equals(
                    Path.GetFullPath(candidate),
                    fullAllowedExistingFile,
                    StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private string GetValidatedLibraryProfilePath(
        ModProfileSummaryModel profile)
    {
        if (string.IsNullOrWhiteSpace(
                profile.FilePath))
        {
            throw new InvalidOperationException(
                "The selected profile does not have " +
                "a valid library file path.");
        }

        string libraryDirectory =
            Path.GetFullPath(
                pathService.EnsureLibraryDirectory());

        string profileFile =
            Path.GetFullPath(
                profile.FilePath);

        string relativePath =
            Path.GetRelativePath(
                libraryDirectory,
                profileFile);

        bool isOutsideLibrary =
            Path.IsPathRooted(relativePath)
            ||
            string.Equals(
                relativePath,
                "..",
                StringComparison.Ordinal)
            ||
            relativePath.StartsWith(
                ".." +
                Path.DirectorySeparatorChar,
                StringComparison.Ordinal)
            ||
            relativePath.StartsWith(
                ".." +
                Path.AltDirectorySeparatorChar,
                StringComparison.Ordinal);

        if (isOutsideLibrary)
        {
            throw new InvalidOperationException(
                "The selected profile is not located " +
                "inside the profile library.");
        }

        ValidateProfileFileExtension(
            profileFile);

        return profileFile;
    }

    private static string ValidateProfileName(
        string profileName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(
                profileName))
        {
            throw new ArgumentException(
                "A profile name is required.",
                parameterName);
        }

        return profileName.Trim();
    }

    private static string CreateSafeFileName(
        string profileName)
    {
        char[] invalidCharacters =
            Path.GetInvalidFileNameChars();

        string safeFileName =
            new(
                profileName
                    .Select(character =>
                        invalidCharacters.Contains(character)
                            ? '_'
                            : character)
                    .ToArray());

        safeFileName =
            safeFileName
                .Trim()
                .TrimEnd(
                    '.');

        if (string.IsNullOrWhiteSpace(
                safeFileName))
        {
            safeFileName =
                "Profile";
        }

        return safeFileName;
    }

    private static ModProfileModel CreateProfileCopy(
        ModProfileModel sourceProfile,
        string profileName,
        string description,
        string author,
        string profileVersion,
        bool preserveCreationDate)
    {
        ArgumentNullException.ThrowIfNull(
            sourceProfile);

        DateTimeOffset now =
            DateTimeOffset.UtcNow;

        return new ModProfileModel
        {
            FormatVersion =
                sourceProfile.FormatVersion,

            Metadata =
                new ModProfileMetadataModel
                {
                    Name =
                        profileName,

                    Description =
                        description?.Trim()
                        ?? string.Empty,

                    Author =
                        author?.Trim()
                        ?? string.Empty,

                    ProfileVersion =
                        string.IsNullOrWhiteSpace(
                            profileVersion)
                            ? sourceProfile.Metadata.ProfileVersion
                            : profileVersion.Trim(),

                    CreatedAtUtc =
                        preserveCreationDate
                            ? sourceProfile.Metadata.CreatedAtUtc
                            : now,

                    ModifiedAtUtc =
                        now,

                    Tags =
                        sourceProfile.Metadata.Tags
                            .ToList()
                },

            Snapshot =
                sourceProfile.Snapshot,

            OperationRequests =
                sourceProfile.OperationRequests
                    .Select(request =>
                        new ProfileOperationRequestModel
                        {
                            FormatVersion =
                                request.FormatVersion,
                            OperationId =
                                request.OperationId,
                            Settings =
                                (Newtonsoft.Json.Linq.JObject?)
                                    request.Settings?.DeepClone()
                        })
                    .ToList()
        };
    }

    private static void ValidateProfileFileExtension(
        string fileName)
    {
        string extension =
            Path.GetExtension(
                fileName);

        if (!string.Equals(
                extension,
                ModProfileFormat.DefaultFileExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Profile files must use the " +
                $"'{ModProfileFormat.DefaultFileExtension}' " +
                "file extension.");
        }
    }

    private static void TryDeleteFile(
        string fileName)
    {
        try
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
        catch
        {
            // Preserve the update or validation exception.
        }
    }

    private ModProfileSummaryModel
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
                propertyCount,

            OperationCount =
                profile.OperationRequests.Count,

            EffectiveChangeCount =
                effectiveChangeCountService.Calculate(
                    profile)
        };
    }
}
