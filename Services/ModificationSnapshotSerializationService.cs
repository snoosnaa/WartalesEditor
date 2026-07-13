using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ModificationSnapshotSerializationService
{
    private static readonly JsonSerializerSettings
        serializerSettings =
            CreateSerializerSettings();

    public string Serialize(
        ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        ValidateForSerialization(snapshot);

        try
        {
            return JsonConvert.SerializeObject(
                snapshot,
                serializerSettings);
        }
        catch (JsonException exception)
        {
            throw new ModificationSnapshotSerializationException(
                "The modification snapshot could not be serialized.",
                exception);
        }
    }

    public ModificationSnapshotModel Deserialize(
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ModificationSnapshotSerializationException(
                "The modification snapshot file is empty.");
        }

        ModificationSnapshotModel? snapshot;

        try
        {
            snapshot =
                JsonConvert.DeserializeObject<
                    ModificationSnapshotModel>(
                    json,
                    serializerSettings);
        }
        catch (JsonException exception)
        {
            throw new ModificationSnapshotSerializationException(
                "The file does not contain a valid " +
                "modification snapshot.",
                exception);
        }

        if (snapshot == null)
        {
            throw new ModificationSnapshotSerializationException(
                "The file does not contain a modification snapshot.");
        }

        ValidateAfterDeserialization(snapshot);

        return snapshot;
    }

    public void Save(
        ModificationSnapshotModel snapshot,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A snapshot file name is required.",
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
            Serialize(snapshot);

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

            throw new ModificationSnapshotSerializationException(
                $"The modification snapshot could not be saved " +
                $"to '{fullPath}'.",
                exception);
        }
    }

    public ModificationSnapshotModel Load(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "A snapshot file name is required.",
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
            throw new ModificationSnapshotSerializationException(
                $"The modification snapshot could not be read " +
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

    private static void ValidateForSerialization(
        ModificationSnapshotModel snapshot)
    {
        if (snapshot.FormatVersion !=
            ModificationSnapshotFormat.CurrentVersion)
        {
            throw new ModificationSnapshotSerializationException(
                $"Snapshot format version " +
                $"'{snapshot.FormatVersion}' cannot be written. " +
                $"The supported version is " +
                $"'{ModificationSnapshotFormat.CurrentVersion}'.");
        }

        if (snapshot.Categories == null)
        {
            throw new ModificationSnapshotSerializationException(
                "The modification snapshot has no " +
                "category collection.");
        }

        foreach (ModificationSnapshotCategoryModel category
                 in snapshot.Categories)
        {
            if (category == null)
            {
                throw new ModificationSnapshotSerializationException(
                    "The modification snapshot contains a null " +
                    "category.");
            }

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new ModificationSnapshotSerializationException(
                    "Every snapshot category must have a name.");
            }

            if (category.Settings == null)
            {
                throw new ModificationSnapshotSerializationException(
                    $"Snapshot category '{category.Name}' has no " +
                    "settings collection.");
            }

            foreach (ModificationSnapshotSettingModel setting
                     in category.Settings)
            {
                if (setting == null)
                {
                    throw new ModificationSnapshotSerializationException(
                        $"Snapshot category '{category.Name}' " +
                        "contains a null setting.");
                }

                if (setting.Properties == null)
                {
                    throw new ModificationSnapshotSerializationException(
                        "A snapshot setting has no properties " +
                        "collection.");
                }

                foreach (ModificationSnapshotPropertyModel property
                         in setting.Properties)
                {
                    if (property == null)
                    {
                        throw new ModificationSnapshotSerializationException(
                            "A snapshot setting contains a null " +
                            "property.");
                    }

                    if (string.IsNullOrWhiteSpace(property.Name))
                    {
                        throw new ModificationSnapshotSerializationException(
                            "Every snapshot property must have a name.");
                    }

                    if (property.OriginalValue == null)
                    {
                        throw new ModificationSnapshotSerializationException(
                            $"Snapshot property '{property.Name}' " +
                            "has no original value.");
                    }

                    if (property.CurrentValue == null)
                    {
                        throw new ModificationSnapshotSerializationException(
                            $"Snapshot property '{property.Name}' " +
                            "has no current value.");
                    }
                }
            }
        }
    }

    private static void ValidateAfterDeserialization(
        ModificationSnapshotModel snapshot)
    {
        if (snapshot.FormatVersion <= 0)
        {
            throw new ModificationSnapshotSerializationException(
                "The file does not specify a valid snapshot " +
                "format version.");
        }

        if (snapshot.FormatVersion >
            ModificationSnapshotFormat.CurrentVersion)
        {
            throw new ModificationSnapshotSerializationException(
                $"Snapshot format version " +
                $"'{snapshot.FormatVersion}' is newer than the " +
                $"supported version " +
                $"'{ModificationSnapshotFormat.CurrentVersion}'.");
        }

        if (snapshot.FormatVersion <
            ModificationSnapshotFormat.CurrentVersion)
        {
            throw new ModificationSnapshotSerializationException(
                $"Snapshot format version " +
                $"'{snapshot.FormatVersion}' is no longer supported. " +
                $"The supported version is " +
                $"'{ModificationSnapshotFormat.CurrentVersion}'.");
        }

        ValidateForSerialization(snapshot);
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