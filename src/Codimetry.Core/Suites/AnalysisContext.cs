using Codimetry.Core.Cyclomatic;
using Codimetry.Core.Loading;
using Codimetry.Core.Model;
using Microsoft.Extensions.Logging;

namespace Codimetry.Core.Suites;

/// <summary>
/// The analysed code, loaded once and shared by the selected suites, with the options that shape what they share.
/// </summary>
public sealed class AnalysisContext
{
    private readonly Lazy<TypeModel> _types;

    public AnalysisContext(Codebase codebase, TypeScope? typeScope = null, CyclomaticOptions? cyclomaticOptions = null,
        DitOptions? ditOptions = null, ILogger? logger = null)
    {
        Codebase = codebase;
        TypeScope = typeScope ?? TypeScope.Default;
        CyclomaticOptions = cyclomaticOptions ?? CyclomaticOptions.Default;
        DitOptions = ditOptions ?? DitOptions.Default;
        _types = new Lazy<TypeModel>(() => TypeModel.Build(Codebase, CyclomaticOptions, TypeScope, DitOptions, logger));
    }

    /// <summary>
    /// One compilation per project, so that cross-file (and cross-project) types resolve.
    /// </summary>
    public Codebase Codebase { get; }

    /// <summary>
    /// Which types are measured (--classes-only).
    /// </summary>
    public TypeScope TypeScope { get; }

    /// <summary>
    /// Operator and aggregation rules of V(G) (--cc-*), for metrics_cyclomatic.csv and WMC.
    /// </summary>
    public CyclomaticOptions CyclomaticOptions { get; }

    /// <summary>
    /// How inheritance is counted (--dit-*), for DIT and the hierarchies of CoC.
    /// </summary>
    public DitOptions DitOptions { get; }

    /// <summary>
    /// The measured types and their methods, built on first use: LOC and cyclomatic do not need them.
    /// </summary>
    public TypeModel Types => _types.Value;

    /// <summary>
    /// Discovers the C# files of a directory, solution or project and loads them.
    /// </summary>
    public static AnalysisContext Load(string path, bool recursive = true, TypeScope? typeScope = null,
        CyclomaticOptions? cyclomaticOptions = null, DitOptions? ditOptions = null, ILogger? logger = null)
    {
        var files = new FileDiscoveryService().DiscoverFiles(path, recursive).ToList();
        logger?.LogInformation("Discovered {Count} C# files", files.Count);

        var codebase = new CodebaseLoader(new RoslynParser())
            .Load(files, Directory.Exists(path) ? path : Path.GetDirectoryName(Path.GetFullPath(path)));
        return new AnalysisContext(codebase, typeScope, cyclomaticOptions, ditOptions, logger);
    }
}
