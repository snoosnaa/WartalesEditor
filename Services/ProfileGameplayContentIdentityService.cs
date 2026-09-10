using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;

namespace WartalesEditor.Services;

public sealed class ProfileGameplayContentIdentityService
{
    private readonly CdbGenerationIdentityService identityService = new();

    public string Calculate(ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        JObject content = new()
        {
            ["profileFormatVersion"] = profile.FormatVersion,
            ["sourceCdbGenerationIdentity"] =
                profile.SourceCdbGenerationIdentity,
            ["snapshot"] = CreateSnapshot(profile.Snapshot),
            ["operationRequests"] = new JArray(
                profile.OperationRequests
                    .OrderBy(request => request.OperationId, StringComparer.Ordinal)
                    .Select(CreateRequest))
        };

        string canonical = Canonicalize(content).ToString(Formatting.None);
        return identityService.Calculate(Encoding.UTF8.GetBytes(canonical));
    }

    internal static JToken Canonicalize(JToken token) => token switch
    {
        JObject source => new JObject(
            source.Properties()
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => new JProperty(
                    property.Name,
                    Canonicalize(property.Value)))),
        JArray source => new JArray(source.Select(Canonicalize)),
        _ => token.DeepClone()
    };

    private static JObject CreateSnapshot(ModificationSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new JObject
        {
            ["formatVersion"] = snapshot.FormatVersion,
            ["sourceCdbGenerationIdentity"] =
                snapshot.SourceCdbGenerationIdentity,
            ["categories"] = new JArray(
                snapshot.Categories
                    .OrderBy(category => category.Name, StringComparer.Ordinal)
                    .Select(category => new JObject
                    {
                        ["name"] = category.Name,
                        ["settings"] = new JArray(
                            category.Settings
                                .OrderBy(setting => setting.Id, StringComparer.Ordinal)
                                .Select(setting => new JObject
                                {
                                    ["id"] = setting.Id,
                                    ["properties"] = new JArray(
                                        setting.Properties
                                            .OrderBy(
                                                GetPropertyIdentity,
                                                StringComparer.Ordinal)
                                            .Select(property => new JObject
                                            {
                                                ["propertyPath"] =
                                                    GetPropertyIdentity(property),
                                                ["originalPropertyExisted"] =
                                                    property.OriginalPropertyExisted,
                                                ["originalValue"] = Canonicalize(
                                                    property.OriginalValue),
                                                ["currentValue"] = Canonicalize(
                                                    property.CurrentValue)
                                            }))
                                }))
                    })),
            ["gameplayOperationStates"] = new JArray(
                snapshot.GameplayOperationStates
                    .OrderBy(state => state.OperationType)
                    .Select(state => Canonicalize(JToken.FromObject(state))))
        };
    }

    private static JObject CreateRequest(ProfileOperationRequestModel request) =>
        new()
        {
            ["formatVersion"] = request.FormatVersion,
            ["operationId"] = request.OperationId,
            ["settings"] = request.Settings == null
                ? JValue.CreateNull()
                : Canonicalize(request.Settings)
        };

    private static string GetPropertyIdentity(
        ModificationSnapshotPropertyModel property) =>
        string.IsNullOrWhiteSpace(property.PropertyPath)
            ? property.Name
            : property.PropertyPath;
}
