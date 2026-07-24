using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Services;

public static class GameplayOperationFingerprintService
{
    public static string CreateContentFingerprint(
        JToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        string canonical =
            token.ToString(Formatting.None);

        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(canonical)));
    }

    public static string CreateShapeFingerprint(
        JArray array)
    {
        ArgumentNullException.ThrowIfNull(array);

        JArray shape =
            new(
                array.Select(CreateShapeToken));

        return CreateContentFingerprint(shape);
    }

    private static JToken CreateShapeToken(
        JToken token)
    {
        return token switch
        {
            JObject sourceObject =>
                new JObject(
                    sourceObject.Properties()
                        .OrderBy(property =>
                            property.Name,
                            StringComparer.Ordinal)
                        .Select(property =>
                            new JProperty(
                                property.Name,
                                CreateShapeToken(
                                    property.Value)))),

            JArray sourceArray =>
                new JArray(
                    sourceArray.Select(CreateShapeToken)),

            _ => new JValue(token.Type.ToString())
        };
    }
}
