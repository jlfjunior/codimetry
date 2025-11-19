using MetricsExporter.Net.Parsers;

namespace MetricsExporter.Net.Services;

/// <summary>
/// Service for discovering C# source files from a directory, solution file, or project file.
/// </summary>
public class FileDiscoveryService
{
    private readonly SolutionParser _solutionParser;
    private readonly ProjectParser _projectParser;

    public FileDiscoveryService()
    {
        _solutionParser = new SolutionParser();
        _projectParser = new ProjectParser();
    }

    public IEnumerable<string> DiscoverFiles(string path, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // Check if path is a file
        if (File.Exists(path))
        {
            return DiscoverFilesFromFile(path);
        }

        // Check if path is a directory
        if (Directory.Exists(path))
        {
            return DiscoverFilesFromDirectory(path, recursive);
        }

        throw new FileNotFoundException($"Path not found: {path}");
    }

    private IEnumerable<string> DiscoverFilesFromFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".sln" => DiscoverFilesFromSolution(filePath),
            ".csproj" => DiscoverFilesFromProject(filePath),
            _ => throw new NotSupportedException($"File type not supported: {extension}. Supported types: .sln, .csproj")
        };
    }

    private IEnumerable<string> DiscoverFilesFromSolution(string solutionPath)
    {
        try
        {
            var projectPaths = _solutionParser.ExtractProjectPaths(solutionPath);
            var allSourceFiles = new List<string>();

            foreach (var projectPath in projectPaths)
            {
                try
                {
                    var sourceFiles = _projectParser.ExtractSourceFilePaths(projectPath);
                    allSourceFiles.AddRange(sourceFiles);
                }
                catch (Exception)
                {
                    // Skip projects that can't be parsed, continue with others
                    continue;
                }
            }

            return allSourceFiles.Distinct();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing solution file: {solutionPath}", ex);
        }
    }

    private IEnumerable<string> DiscoverFilesFromProject(string projectPath)
    {
        try
        {
            return _projectParser.ExtractSourceFilePaths(projectPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing project file: {projectPath}", ex);
        }
    }

    private IEnumerable<string> DiscoverFilesFromDirectory(string directoryPath, bool recursive)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        
        try
        {
            return Directory.EnumerateFiles(directoryPath, "*.cs", searchOption);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied to directory: {directoryPath}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error discovering files in directory: {directoryPath}", ex);
        }
    }
}

