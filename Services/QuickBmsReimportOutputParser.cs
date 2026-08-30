using System.IO;
using System.Text.RegularExpressions;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed partial class QuickBmsReimportOutputParser
{
    public QuickBmsReimportParseResult Parse(
        string standardOutput,
        string standardError)
    {
        List<int> summaries = new();
        List<string> files = new();

        foreach (string line in EnumerateLines(
                     standardOutput,
                     standardError))
        {
            Match summary = ReimportSummary().Match(line);

            if (summary.Success &&
                int.TryParse(
                    summary.Groups["count"].Value,
                    out int count))
            {
                summaries.Add(count);
            }

            Match file = FileRecord().Match(line);

            if (file.Success)
            {
                files.Add(
                    file.Groups["name"].Value.Trim());
            }
        }

        if (summaries.Count != 1)
        {
            return Failure(
                summaries,
                files,
                summaries.Count == 0
                    ? "The QuickBMS reimport summary was missing."
                    : "QuickBMS returned multiple reimport summaries.");
        }

        if (summaries[0] != 1)
        {
            return Failure(
                summaries,
                files,
                $"QuickBMS reported {summaries[0]} reimported files instead of one.");
        }

        if (files.Count != 1)
        {
            return Failure(
                summaries,
                files,
                files.Count == 0
                    ? "The QuickBMS data.cdb file record was missing."
                    : "QuickBMS returned multiple reimport file records.");
        }

        string normalized =
            files[0]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

        if (!string.Equals(
                Path.GetFileName(normalized),
                "data.cdb",
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                normalized.TrimStart(Path.DirectorySeparatorChar),
                "data.cdb",
                StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                summaries,
                files,
                "QuickBMS reimported a file other than data.cdb.");
        }

        return new QuickBmsReimportParseResult(
            true,
            1,
            files,
            string.Empty);
    }

    private static IEnumerable<string> EnumerateLines(
        params string[] values)
    {
        foreach (string value in values)
        {
            foreach (string line in
                     (value ?? string.Empty).Split(
                         new[] { '\r', '\n' },
                         StringSplitOptions.RemoveEmptyEntries |
                         StringSplitOptions.TrimEntries))
            {
                yield return line;
            }
        }
    }

    private static QuickBmsReimportParseResult Failure(
        IReadOnlyList<int> summaries,
        IReadOnlyList<string> files,
        string reason)
    {
        return new QuickBmsReimportParseResult(
            false,
            summaries.Count == 1
                ? summaries[0]
                : null,
            files,
            reason);
    }

    [GeneratedRegex(
        @"^\s*-\s+(?<count>\d+)\s+files?\s+reimported\s+in\b",
        RegexOptions.CultureInvariant |
        RegexOptions.IgnoreCase)]
    private static partial Regex ReimportSummary();

    [GeneratedRegex(
        @"^\s*[<>]\s+[0-9a-f]+\s+\d+\s+(?<name>.+?)\s*$",
        RegexOptions.CultureInvariant |
        RegexOptions.IgnoreCase)]
    private static partial Regex FileRecord();
}
