using System.Xml.Linq;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// The global usings that the .NET SDK generates for a project with &lt;ImplicitUsings&gt; enabled,
/// plus its &lt;Using&gt; items. The property is read from the project file or, failing that, from
/// the nearest Directory.Build.props above it (the first one MSBuild would import).
/// </summary>
public static class ImplicitUsings
{
    private static readonly string[] Base =
    [
        "System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Net.Http",
        "System.Threading", "System.Threading.Tasks"
    ];

    private static readonly string[] Hosting =
    [
        "Microsoft.Extensions.Configuration", "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting", "Microsoft.Extensions.Logging"
    ];

    private static readonly string[] Web =
    [
        "System.Net.Http.Json", "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Routing", .. Hosting
    ];

    /// <summary>
    /// Namespaces (or "static X" / "Alias = X") to emit as global usings for the project.
    /// </summary>
    public static IReadOnlyList<string> For(string projectPath)
    {
        var project = TryLoad(projectPath);
        if (project == null)
        {
            return [];
        }

        var usings = new List<string>();
        var enabled = Property(project, "ImplicitUsings") ?? DirectoryBuildProperty(projectPath, "ImplicitUsings");
        if (enabled is "enable" or "true")
        {
            var sdk = Sdk(project);
            usings.AddRange(Base);
            if (sdk.StartsWith("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
            {
                usings.AddRange(Web);
            }
            else if (sdk.StartsWith("Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase))
            {
                usings.AddRange(Hosting);
            }
        }

        foreach (var item in project.Descendants().Where(e => e.Name.LocalName == "Using"))
        {
            var include = item.Attribute("Include")?.Value;
            var remove = item.Attribute("Remove")?.Value;
            if (!string.IsNullOrWhiteSpace(remove))
            {
                usings.Remove(remove.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(include))
            {
                var alias = item.Attribute("Alias")?.Value;
                var isStatic = string.Equals(item.Attribute("Static")?.Value, "true", StringComparison.OrdinalIgnoreCase);
                usings.Add(alias != null ? $"{alias} = global::{include.Trim()}" : isStatic ? $"static global::{include.Trim()}" : include.Trim());
            }
        }

        return usings
            .Distinct()
            .Select(u => u.Contains('=') || u.StartsWith("static ") ? u : $"global::{u}")
            .ToList();
    }

    private static string Sdk(XDocument project)
    {
        var root = project.Root!;
        return root.Attribute("Sdk")?.Value
            ?? root.Elements().FirstOrDefault(e => e.Name.LocalName == "Sdk")?.Attribute("Name")?.Value
            ?? root.Elements().FirstOrDefault(e => e.Name.LocalName == "Import" && e.Attribute("Sdk") != null)?.Attribute("Sdk")?.Value
            ?? string.Empty;
    }

    private static string? Property(XDocument document, string name) =>
        document.Descendants().LastOrDefault(e => e.Name.LocalName == name)?.Value.Trim().ToLowerInvariant();

    private static string? DirectoryBuildProperty(string projectPath, string name)
    {
        var directory = Path.GetDirectoryName(projectPath);
        while (directory != null)
        {
            var props = Path.Combine(directory, "Directory.Build.props");
            if (File.Exists(props))
            {
                // MSBuild stops at the first Directory.Build.props unless it imports its parent
                var document = TryLoad(props);
                return document == null ? null : Property(document, name);
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    private static XDocument? TryLoad(string path)
    {
        try
        {
            return XDocument.Load(path);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
