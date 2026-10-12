using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// A source file of the analysed codebase with its semantic model.
/// </summary>
public sealed record SourceDocument(string FilePath, SyntaxTree Tree, SemanticModel SemanticModel, string Project);

/// <summary>
/// The analysed codebase: one compilation per .csproj, so that classes of different projects are
/// different symbols even when they share a fully qualified name.
/// </summary>
public sealed class Codebase
{
    public Codebase(IReadOnlyList<SourceDocument> documents) => Documents = documents;

    public IReadOnlyList<SourceDocument> Documents { get; }

    /// <summary>
    /// How many method invocations inside classes the semantic model binds to a method symbol.
    /// A low rate means missing references, and then calls cannot be told apart by their target.
    /// </summary>
    public (int Resolved, int Total) InvocationResolution()
    {
        int resolved = 0, total = 0;
        foreach (var document in Documents)
        {
            foreach (var invocation in document.Tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (TypeKeys.ContainingClass(invocation) == null)
                {
                    continue;
                }

                total++;
                if (document.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol)
                {
                    resolved++;
                }
            }
        }

        return (resolved, total);
    }
}

/// <summary>
/// Groups source files by the nearest .csproj above them, builds one compilation per project and
/// links projects through their &lt;ProjectReference&gt; items. Files outside any project form one
/// extra compilation. MSBuild is not run: only the project files themselves are read.
/// </summary>
public class CodebaseLoader
{
    private const string NoProject = "(no project)";

    private readonly RoslynParser _parser;
    private readonly IReadOnlyList<MetadataReference> _references;

    public CodebaseLoader(RoslynParser parser, IReadOnlyList<MetadataReference>? references = null)
    {
        _parser = parser;
        _references = references ?? [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
    }

    public Codebase Load(IEnumerable<string> files)
    {
        var parsed = new List<(string Path, SyntaxTree Tree)>();
        foreach (var file in files)
        {
            var tree = _parser.ParseFile(file);
            if (tree != null)
            {
                parsed.Add((file, tree));
            }
        }

        var root = CommonDirectory(parsed.Select(p => Path.GetFullPath(p.Path)));
        var projectOf = new Dictionary<string, string>();
        foreach (var (path, _) in parsed)
        {
            projectOf[path] = FindProject(Path.GetFullPath(path), root) ?? NoProject;
        }

        var treesByProject = parsed
            .GroupBy(p => projectOf[p.Path])
            .ToDictionary(g => g.Key, g => g.ToList());

        var compilations = new Dictionary<string, CSharpCompilation>();
        foreach (var project in treesByProject.Keys)
        {
            Compile(project, treesByProject, root, compilations, []);
        }

        var documents = parsed
            .Select(p =>
            {
                var project = projectOf[p.Path];
                return new SourceDocument(p.Path, p.Tree, compilations[project].GetSemanticModel(p.Tree), project);
            })
            .ToList();

        return new Codebase(documents);
    }

    private CSharpCompilation Compile(
        string project,
        IReadOnlyDictionary<string, List<(string Path, SyntaxTree Tree)>> treesByProject,
        string root,
        Dictionary<string, CSharpCompilation> compilations,
        HashSet<string> visiting)
    {
        if (compilations.TryGetValue(project, out var existing))
        {
            return existing;
        }

        visiting.Add(project);
        var references = new List<MetadataReference>(_references);
        if (project != NoProject)
        {
            foreach (var referenced in ProjectReferences(project))
            {
                if (treesByProject.ContainsKey(referenced) && !visiting.Contains(referenced))
                {
                    references.Add(Compile(referenced, treesByProject, root, compilations, visiting).ToMetadataReference());
                }
            }
        }

        var compilation = CSharpCompilation.Create(
            AssemblyName(project, root),
            treesByProject[project].Select(t => t.Tree),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        visiting.Remove(project);
        compilations[project] = compilation;
        return compilation;
    }

    /// <summary>
    /// Assembly name that identifies the project in class keys: the project path relative to the
    /// analysed root (two services may both have an "Api.csproj").
    /// </summary>
    private static string AssemblyName(string project, string root) =>
        project == NoProject ? NoProject : Path.GetRelativePath(root, project).Replace('\\', '/');

    private static IEnumerable<string> ProjectReferences(string projectPath)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(projectPath);
        }
        catch (Exception)
        {
            yield break;
        }

        var directory = Path.GetDirectoryName(projectPath)!;
        foreach (var element in document.Descendants().Where(e => e.Name.LocalName == "ProjectReference"))
        {
            var include = element.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(include))
            {
                yield return Path.GetFullPath(Path.Combine(directory, include.Replace('\\', Path.DirectorySeparatorChar)));
            }
        }
    }

    /// <summary>
    /// Nearest .csproj in the file's directory or above it, without leaving the analysed root.
    /// </summary>
    private static string? FindProject(string filePath, string root)
    {
        var directory = Path.GetDirectoryName(filePath);
        while (directory != null && directory.StartsWith(root, StringComparison.Ordinal))
        {
            var project = Directory.EnumerateFiles(directory, "*.csproj").OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (project != null)
            {
                return Path.GetFullPath(project);
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    private static string CommonDirectory(IEnumerable<string> paths)
    {
        string? common = null;
        foreach (var directory in paths.Select(Path.GetDirectoryName))
        {
            if (directory == null)
            {
                continue;
            }

            if (common == null)
            {
                common = directory;
                continue;
            }

            while (!(directory + Path.DirectorySeparatorChar).StartsWith(common + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                common = Path.GetDirectoryName(common) ?? string.Empty;
            }
        }

        return common ?? string.Empty;
    }
}
