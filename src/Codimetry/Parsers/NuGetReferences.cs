using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace Codimetry.Parsers;

/// <summary>
/// Compile-time assemblies of the NuGet packages of a project, read from the
/// obj/project.assets.json that "dotnet restore" writes. Without a restore the project simply gets
/// no package references. Assemblies that the shared framework already provides are skipped, the
/// same conflict MSBuild resolves in favour of the framework.
/// </summary>
public static class NuGetReferences
{
    private static readonly ConcurrentDictionary<string, MetadataReference> Cache = new(StringComparer.Ordinal);

    public static IReadOnlyList<MetadataReference> For(string projectPath, IReadOnlySet<string> frameworkFileNames)
    {
        var assetsFile = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
        if (!File.Exists(assetsFile))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(assetsFile));
            var root = document.RootElement;

            var packageFolders = root.TryGetProperty("packageFolders", out var folders)
                ? folders.EnumerateObject().Select(f => f.Name).ToList()
                : [];

            // The first framework target; runtime-specific targets ("net8.0/linux-x64") repeat it
            var target = root.GetProperty("targets").EnumerateObject().FirstOrDefault(t => !t.Name.Contains('/'));
            if (target.Value.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            var libraries = root.GetProperty("libraries");
            var references = new List<MetadataReference>();
            foreach (var library in target.Value.EnumerateObject())
            {
                if (!library.Value.TryGetProperty("type", out var type) || type.GetString() != "package" ||
                    !library.Value.TryGetProperty("compile", out var compile) ||
                    !libraries.TryGetProperty(library.Name, out var info) ||
                    !info.TryGetProperty("path", out var libraryPath))
                {
                    continue;
                }

                foreach (var asset in compile.EnumerateObject())
                {
                    var fileName = Path.GetFileName(asset.Name);
                    if (fileName == "_._" || frameworkFileNames.Contains(fileName))
                    {
                        continue;
                    }

                    var file = packageFolders
                        .Select(folder => Path.Combine(folder, libraryPath.GetString()!, asset.Name))
                        .FirstOrDefault(File.Exists);
                    if (file != null)
                    {
                        references.Add(Cache.GetOrAdd(Path.GetFullPath(file), f => MetadataReference.CreateFromFile(f)));
                    }
                }
            }

            return references;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IOException)
        {
            return [];
        }
    }
}
