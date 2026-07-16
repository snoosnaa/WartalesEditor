using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using WartalesEditor.Models.Profiles;

namespace WartalesEditor.Services;

public sealed class ModProfileSerializationService
{
    private static readonly JsonSerializerSettings
        serializerSettings =
            CreateSerializerSettings();

    private readonly ModificationSnapshotSerializationService
        snapshotSerializationService;

    public ModProfileSerializationService()
        : this(
            new ModificationSnapshotSerializationService())
    {
    }

    public ModProfileSerializationService(
        ModificationSnapshotSerializationService
            snapshotSerializationService)
    {
        this.snapshotSerializationService =
            snapshotSerializationService
            ?? throw new ArgumentNullException(
                nameof(snapshotSerializationService));
    }

    public string Serialize(
        ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ValidateForSerialization(profile);

        try
        {
            return JsonConvert.SerializeObject(
                profile,
                serializerSettings);
        }
        catch (JsonException exception)
        {
            throw new ModProfileSerializationException(
                "The mod profile could not be serialized.",
                exception);
        }
    }

    public ModProfileModel Deserialize(
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ModProfileSerializationException(
                "The mod profile file is empty.");
        }

        ModProfileModel? profile;

        try
        {
            profile =
                JsonConvert.DeserializeObject<
                    ModProfileModel>(
                    json,
                    serializerSettings);
        }
        catch (JsonException exception)
        {
            throw new ModProfileSerializationException(
                "The file does not contain a valid mod profile.",
                exception);
        }

        if (profile == null)
        {
            throw new ModProfileSerializationException(
                "The file does not contain a mod profile.");
        }

        ValidateAfterDeserialization(profile);

        return profile;
    }

    public void Save(
        ModProfileModel profile,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A mod profile file name is required.",
                nameof(fileName));
        }

        string fullPath =
            Path.GetFullPath(fileName);

        string? directory =
            Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json =
            Serialize(profile);

        string temporaryFile =
            fullPath + ".tmp";

        try
        {
            File.WriteAllText(
                temporaryFile,
                json,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

            File.Move(
                temporaryFile,
                fullPath,
                overwrite: true);
        }
        catch (Exception exception)
            when (exception is IOException
                  or UnauthorizedAccessException)
        {
            TryDeleteTemporaryFile(
                temporaryFile);

            throw new ModProfileSerializationException(
                $"The mod profile could not be saved " +
                $"to '{fullPath}'.",
                exception);
        }
    }

    public ModProfileModel Load(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A mod profile file name is required.",
                nameof(fileName));
        }

        string fullPath =
            Path.GetFullPath(fileName);

        string json;

        try
        {
            json =
                File.ReadAllText(
                    fullPath,
                    Encoding.UTF8);
        }
        catch (Exception exception)
            when (exception is IOException
                  or UnauthorizedAccessException)
        {
            throw new ModProfileSerializationException(
                $"The mod profile could not be read " +
                $"from '{fullPath}'.",
                exception);
        }

        return Deserialize(json);
    }

    private static JsonSerializerSettings
        CreateSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            Formatting =
                Formatting.Indented,

            NullValueHandling =
                NullValueHandling.Include,

            MissingMemberHandling =
                MissingMemberHandling.Ignore,

            DateParseHandling =
                DateParseHandling.DateTimeOffset,

            TypeNameHandling =
                TypeNameHandling.None,

            MetadataPropertyHandling =
                MetadataPropertyHandling.Ignore
        };
    }

    private void ValidateForSerialization(
        ModProfileModel profile)
    {
        if (profile.FormatVersion !=
            ModProfileFormat.CurrentVersion)
        {
            throw new ModProfileSerializationException(
                $"Mod profile format version " +
                $"'{profile.FormatVersion}' cannot be written. " +
                $"The supported version is " +
                $"'{ModProfileFormat.CurrentVersion}'.");
        }

        if (profile.Metadata == null)
        {
            throw new ModProfileSerializationException(
                "The mod profile has no metadata.");
        }

        ValidateMetadata(
            profile.Metadata);

        if (profile.Snapshot == null)
        {
            throw new ModProfileSerializationException(
                "The mod profile has no modification snapshot.");
        }

        ValidateSnapshot(
            profile);
    }

    private void ValidateAfterDeserialization(
        ModProfileModel profile)
    {
        if (profile.FormatVersion <= 0)
        {
            throw new ModProfileSerializationException(
                "The file does not specify a valid mod profile " +
                "format version.");
        }

        if (profile.FormatVersion >
            ModProfileFormat.CurrentVersion)
        {
            throw new ModProfileSerializationException(
                $"Mod profile format version " +
                $"'{profile.FormatVersion}' is newer than the " +
                $"supported version " +
                $"'{ModProfileFormat.CurrentVersion}'.");
        }

        if (profile.FormatVersion <
            ModProfileFormat.CurrentVersion)
        {
            throw new ModProfileSerializationException(
                $"Mod profile format version " +
                $"'{profile.FormatVersion}' is no longer " +
                $"supported. The supported version is " +
                $"'{ModProfileFormat.CurrentVersion}'.");
        }

        ValidateForSerialization(profile);
    }

    private static void ValidateMetadata(
        ModProfileMetadataModel metadata)
    {
        if (string.IsNullOrWhiteSpace(
                metadata.Name))
        {
            throw new ModProfileSerializationException(
                "Every mod profile must have a name.");
        }

        if (string.IsNullOrWhiteSpace(
                metadata.ProfileVersion))
        {
            throw new ModProfileSerializationException(
                "Every mod profile must have a profile version.");
        }

        if (metadata.CreatedAtUtc ==
            default)
        {
            throw new ModProfileSerializationException(
                "The mod profile does not specify when it was " +
                "created.");
        }

        if (metadata.ModifiedAtUtc ==
            default)
        {
            throw new ModProfileSerializationException(
                "The mod profile does not specify when it was " +
                "last modified.");
        }

        if (metadata.ModifiedAtUtc <
            metadata.CreatedAtUtc)
        {
            throw new ModProfileSerializationException(
                "The mod profile modification date cannot be " +
                "earlier than its creation date.");
        }

        if (metadata.Tags == null)
        {
            throw new ModProfileSerializationException(
                "The mod profile has no tag collection.");
        }

        foreach (string? tag in metadata.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ModProfileSerializationException(
                    "Mod profile tags cannot be null, empty, " +
                    "or whitespace.");
            }
        }
    }

    private void ValidateSnapshot(
        ModProfileModel profile)
    {
        try
        {
            snapshotSerializationService.Serialize(
                profile.Snapshot);
        }
        catch (
            ModificationSnapshotSerializationException
            exception)
        {
            throw new ModProfileSerializationException(
                "The mod profile contains an invalid " +
                "modification snapshot.",
                exception);
        }
    }

    private static void TryDeleteTemporaryFile(
        string temporaryFile)
    {
        try
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
        catch
        {
            // Preserve the original save exception.
        }
    }
}
