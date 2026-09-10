using System.IO;

namespace WartalesEditor.Services;

internal static class UserGuidePathResolver
{
    internal const string FileName = "USER-GUIDE.pdf";
    private const string PackagedApplicationDirectoryName = "App";

    internal static string Resolve(string applicationBaseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            applicationBaseDirectory);

        string baseDirectory =
            Path.GetFullPath(applicationBaseDirectory);
        string besideApplication =
            Path.Combine(baseDirectory, FileName);

        if (File.Exists(besideApplication))
        {
            return besideApplication;
        }

        DirectoryInfo applicationDirectory =
            new(Path.TrimEndingDirectorySeparator(baseDirectory));

        if (string.Equals(
                applicationDirectory.Name,
                PackagedApplicationDirectoryName,
                StringComparison.OrdinalIgnoreCase) &&
            applicationDirectory.Parent != null)
        {
            string packageRootGuide =
                Path.Combine(
                    applicationDirectory.Parent.FullName,
                    FileName);

            if (File.Exists(packageRootGuide))
            {
                return packageRootGuide;
            }
        }

        return besideApplication;
    }
}
