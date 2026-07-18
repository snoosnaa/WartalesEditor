using System;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Operations;

public sealed class ProjectOperationSnapshot
{
    public JObject RootDocument
    {
        get;
    }

    public string OriginalJson
    {
        get;
    }

    public string FileName
    {
        get;
    }

    public ProjectOperationSnapshot(
        JObject rootDocument,
        string originalJson,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(
            rootDocument);

        RootDocument =
            (JObject)rootDocument.DeepClone();

        OriginalJson =
            originalJson ?? string.Empty;

        FileName =
            fileName ?? string.Empty;
    }
}