using System.Xml.Linq;

namespace Codimetry.Parsers;

/// <summary>
/// Parser for .NET project (.csproj) files.
/// </summary>
public class ProjectParser
{
    /// <summary>
    /// Extracts C# source file paths from a project file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Collection of source file paths.</returns>
    public IEnumerable<string> ExtractSourceFilePaths(string projectPath)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        if (!File.Exists(fullProjectPath))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}");
        }

        var projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? string.Empty;
        var sourceFiles = new List<string>();

        try
        {
            var doc = XDocument.Load(projectPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Find all Compile items (C# source files)
            var compileItems = doc.Descendants(ns + "Compile")
                .Concat(doc.Descendants().Where(e => e.Name.LocalName == "Compile"))
                .Select(e => e.Attribute("Include")?.Value)
                .Where(path => !string.IsNullOrEmpty(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

            foreach (var relativePath in compileItems)
            {
                if (relativePath == null) continue;

                // Handle wildcards (e.g., **/*.cs)
                if (relativePath.Contains('*'))
                {
                    var pattern = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                    var basePath = pattern;
                    var searchPattern = Path.GetFileName(pattern);

                    // Extract directory pattern
                    var lastSlash = basePath.LastIndexOf(Path.DirectorySeparatorChar);
                    if (lastSlash >= 0)
                    {
                        basePath = basePath.Substring(0, lastSlash);
                        basePath = basePath.Replace("**", "*"); // Simplify for Directory.EnumerateFiles
                    }

                    var searchDir = Path.Combine(projectDirectory, basePath);
                    if (Directory.Exists(searchDir))
                    {
                        var files = Directory.EnumerateFiles(searchDir, searchPattern, SearchOption.AllDirectories);
                        sourceFiles.AddRange(files);
                    }
                }
                else
                {
                    var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(projectDirectory, normalizedPath);

                    if (File.Exists(fullPath))
                    {
                        sourceFiles.Add(fullPath);
                    }
                }
            }

            // SDK-style projects don't have explicit Compile items, so always scan the directory
            // But exclude files that are already found explicitly
            var foundPaths = new HashSet<string>(sourceFiles.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
            
            if (Directory.Exists(projectDirectory))
            {
                var defaultSourceFiles = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .Where(f => !foundPaths.Contains(f) &&
                                !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                                !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));
                sourceFiles.AddRange(defaultSourceFiles);
            }
        }
        catch (Exception)
        {
            // If XML parsing fails, fall back to directory scanning
            if (Directory.Exists(projectDirectory))
            {
                var fallbackFiles = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                                !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));
                sourceFiles.AddRange(fallbackFiles);
            }
        }

        return sourceFiles.Distinct();
    }
}

