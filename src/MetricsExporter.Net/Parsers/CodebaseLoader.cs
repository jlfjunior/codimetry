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
/// extra compilation. MSBuild is not run: only the project files themselves are read, including
/// the implicit global usings the SDK would generate (see <see cref="ImplicitUsings"/>) and, when the
/// project was restored, its NuGet packages (see <see cref="NuGetReferences"/>).
/// </summary>
public class CodebaseLoader
{
    private const string NoProject = "(no project)";

    private readonly RoslynParser _parser;
    private readonly IReadOnlyList<MetadataReference> _references;

    public CodebaseLoader(RoslynParser parser, IReadOnlyList<MetadataReference>? references = null)
    {
        _parser = parser;
        _references = references ?? FrameworkReferences.References;
    }

    /// <param name="files">Source files to analyse.</param>
    /// <param name="rootDirectory">The analysed path: projects are looked up from each file up to it.
    /// Defaults to the common directory of the files.</param>
    public Codebase Load(IEnumerable<string> files, string? rootDirectory = null)
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

        var root = rootDirectory != null
            ? Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar)
            : CommonDirectory(parsed.Select(p => Path.GetFullPath(p.Path)));
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
            references.AddRange(NuGetReferences.For(project, FrameworkReferences.FileNames));

            foreach (var referenced in TransitiveProjectReferences(project))
            {
                if (treesByProject.ContainsKey(referenced) && !visiting.Contains(referenced))
                {
                    references.Add(Compile(referenced, treesByProject, root, compilations, visiting).ToMetadataReference());
                }
            }
        }

        var trees = treesByProject[project].Select(t => t.Tree).ToList();
        if (project != NoProject)
        {
            var globalUsings = ImplicitUsings.For(project);
            if (globalUsings.Count > 0)
            {
                trees.Add(CSharpSyntaxTree.ParseText(
                    string.Concat(globalUsings.Select(u => $"global using {u};\n")),
                    (CSharpParseOptions)trees[0].Options,
                    Path.Combine(Path.GetDirectoryName(project)!, "ImplicitUsings.g.cs")));
            }
        }

        var compilation = CSharpCompilation.Create(
            AssemblyName(project, root),
            trees,
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

    /// <summary>
    /// Direct and indirect project references: SDK-style projects see the projects their
    /// references depend on, so a test project referencing only the API also sees the domain.
    /// </summary>
    private static IEnumerable<string> TransitiveProjectReferences(string projectPath)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { projectPath };
        var pending = new Queue<string>(ProjectReferences(projectPath));
        var result = new List<string>();
        while (pending.Count > 0)
        {
            var next = pending.Dequeue();
            if (!seen.Add(next))
            {
                continue;
            }

            result.Add(next);
            foreach (var reference in ProjectReferences(next))
            {
                pending.Enqueue(reference);
            }
        }

        return result;
    }

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
