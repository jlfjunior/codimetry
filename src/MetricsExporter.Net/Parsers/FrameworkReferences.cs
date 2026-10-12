using Microsoft.CodeAnalysis;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// Reference assemblies of the .NET and ASP.NET Core shared frameworks, taken from the targeting
/// packs of the SDK that runs the tool (the newest version installed). Projects that target older
/// frameworks are compiled against these too: for telling calls apart, the newer API surface is a
/// superset. NuGet packages are not referenced, so calls into them stay unresolved.
/// </summary>
public static class FrameworkReferences
{
    private static readonly string[] Packs = ["Microsoft.NETCore.App.Ref", "Microsoft.AspNetCore.App.Ref"];

    private static readonly Lazy<(IReadOnlyList<MetadataReference> References, string Description)> Loaded = new(LoadPacks);

    public static IReadOnlyList<MetadataReference> References => Loaded.Value.References;

    /// <summary>
    /// File names of the framework assemblies, so that package copies of them can be skipped.
    /// </summary>
    public static IReadOnlySet<string> FileNames { get; } = new HashSet<string>(
        References.OfType<PortableExecutableReference>().Select(r => Path.GetFileName(r.FilePath!)),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Which packs were loaded, e.g. "Microsoft.NETCore.App.Ref 10.0.1, Microsoft.AspNetCore.App.Ref 10.0.1".
    /// </summary>
    public static string Description => Loaded.Value.Description;

    private static (IReadOnlyList<MetadataReference>, string) LoadPacks()
    {
        // .../dotnet/shared/Microsoft.NETCore.App/<version>/System.Private.CoreLib.dll -> .../dotnet
        var coreLib = typeof(object).Assembly.Location;
        var dotnetRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(coreLib)!, "..", "..", ".."));

        var references = new List<MetadataReference>();
        var loaded = new List<string>();
        foreach (var pack in Packs)
        {
            var directory = NewestRefDirectory(Path.Combine(dotnetRoot, "packs", pack));
            if (directory == null)
            {
                continue;
            }

            references.AddRange(Directory.EnumerateFiles(directory.Value.Path, "*.dll")
                .OrderBy(f => f, StringComparer.Ordinal)
                .Select(f => MetadataReference.CreateFromFile(f)));
            loaded.Add($"{pack} {directory.Value.Version}");
        }

        if (references.Count == 0)
        {
            return ([MetadataReference.CreateFromFile(coreLib)], "System.Private.CoreLib only (no targeting packs found)");
        }

        return (references, string.Join(", ", loaded));
    }

    private static (string Path, Version Version)? NewestRefDirectory(string packDirectory)
    {
        if (!Directory.Exists(packDirectory))
        {
            return null;
        }

        var newest = Directory.EnumerateDirectories(packDirectory)
            .Select(d => (Path: d, Version: Version.TryParse(System.IO.Path.GetFileName(d), out var v) ? v : null))
            .Where(d => d.Version != null)
            .OrderByDescending(d => d.Version)
            .FirstOrDefault();
        if (newest.Version == null)
        {
            return null;
        }

        var refDirectory = Directory.EnumerateDirectories(System.IO.Path.Combine(newest.Path, "ref")).FirstOrDefault();
        return refDirectory == null ? null : (refDirectory, newest.Version);
    }
}
