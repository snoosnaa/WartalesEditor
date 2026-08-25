namespace WartalesEditor.Models;

public sealed class LanguageDataMetadata
{
    public LanguageDataMetadata(
        string project,
        string languageCode,
        string version,
        string revision,
        string softwareVersion,
        string date)
    {
        Project = project;
        LanguageCode = languageCode;
        Version = version;
        Revision = revision;
        SoftwareVersion = softwareVersion;
        Date = date;
    }

    public string Project { get; }

    public string LanguageCode { get; }

    public string Version { get; }

    public string Revision { get; }

    public string SoftwareVersion { get; }

    public string Date { get; }
}
