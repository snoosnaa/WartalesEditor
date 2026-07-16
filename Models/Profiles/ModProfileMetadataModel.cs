using System;
using System.Collections.Generic;

namespace WartalesEditor.Models.Profiles;

public sealed class ModProfileMetadataModel
{
    public string Name { get; init; }
        = string.Empty;

    public string Description { get; init; }
        = string.Empty;

    public string Author { get; init; }
        = string.Empty;

    public string ProfileVersion { get; init; }
        = "1.0";

    public DateTimeOffset CreatedAtUtc { get; init; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset ModifiedAtUtc { get; init; }
        = DateTimeOffset.UtcNow;

    public List<string> Tags
    {
        get;
        init;
    } = new();
}