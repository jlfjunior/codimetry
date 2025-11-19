using System.Text.RegularExpressions;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// Parser for Visual Studio solution (.sln) files.
/// </summary>
public class SolutionParser
{
    private static readonly Regex ProjectRegex = new(
        @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+)"",\s*""\{[^}]+\}""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Extracts project file paths from a solution file.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file.</param>
    /// <returns>Collection of project file paths.</returns>
    public IEnumerable<string> ExtractProjectPaths(string solutionPath)
    {
        if (!File.Exists(solutionPath))
        {
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");
        }

        var fullSolutionPath = Path.GetFullPath(solutionPath);
        var solutionDirectory = Path.GetDirectoryName(fullSolutionPath) ?? string.Empty;
        var solutionContent = File.ReadAllText(solutionPath);
        var projectPaths = new List<string>();

        var matches = ProjectRegex.Matches(solutionContent);
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var relativePath = match.Groups[1].Value;
                // Convert Windows path separators to current OS format
                var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, normalizedPath));

                if (File.Exists(fullPath) && fullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    projectPaths.Add(fullPath);
                }
            }
        }

        return projectPaths;
    }
}

