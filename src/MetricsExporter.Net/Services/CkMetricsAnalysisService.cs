using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using MetricsExporter.Net.Core.Models;
using MetricsExporter.Net.Parsers;

namespace MetricsExporter.Net.Services;

/// <summary>
/// Service that orchestrates the calculation of CK metrics for all classes.
/// </summary>
public class CkMetricsAnalysisService
{
    private readonly FileDiscoveryService _fileDiscoveryService;
    private readonly RoslynParser _roslynParser;
    private readonly ClassRelationshipBuilder _relationshipBuilder;
    private readonly WmcCalculator _wmcCalculator;
    private readonly DitCalculator _ditCalculator;
    private readonly NocCalculator _nocCalculator;
    private readonly CboCalculator _cboCalculator;
    private readonly RfcCalculator _rfcCalculator;
    private readonly LcomCalculator _lcomCalculator;
    private readonly CodebaseLoader _codebaseLoader;
    private readonly ILogger<CkMetricsAnalysisService>? _logger;

    public CkMetricsAnalysisService(
        FileDiscoveryService fileDiscoveryService,
        RoslynParser roslynParser,
        ClassRelationshipBuilder relationshipBuilder,
        ILogger<CkMetricsAnalysisService>? logger = null,
        CodebaseLoader? codebaseLoader = null)
    {
        _fileDiscoveryService = fileDiscoveryService;
        _roslynParser = roslynParser;
        _relationshipBuilder = relationshipBuilder;
        _wmcCalculator = new WmcCalculator();
        _ditCalculator = new DitCalculator(relationshipBuilder);
        _nocCalculator = new NocCalculator(relationshipBuilder);
        _cboCalculator = new CboCalculator();
        _rfcCalculator = new RfcCalculator();
        _lcomCalculator = new LcomCalculator();
        _codebaseLoader = codebaseLoader ?? new CodebaseLoader(roslynParser);
        _logger = logger;
    }

    /// <summary>
    /// Analyzes code and calculates CK metrics for all classes.
    /// </summary>
    public async Task<List<CkMetric>> AnalyzeAsync(string path, bool recursive = true)
    {
        var allMetrics = new List<CkMetric>();
        var files = _fileDiscoveryService.DiscoverFiles(path, recursive).ToList();

        _logger?.LogInformation("Discovered {Count} C# files for CK metrics analysis", files.Count);

        // Phase 1: Build class relationship graph
        _logger?.LogInformation("Phase 1: Building class relationship graph...");
        var allClasses = new List<CkCodeElement>();
        var allMethods = new List<CkMethodInfo>();
        var allDependencies = new List<ClassDependency>();

        // One compilation per project, so that cross-file (and cross-project) types resolve
        var codebase = _codebaseLoader.Load(files, Directory.Exists(path) ? path : Path.GetDirectoryName(Path.GetFullPath(path)));
        _logger?.LogInformation("References: {References}", FrameworkReferences.Description);
        var (resolved, total) = codebase.InvocationResolution();
        _logger?.LogInformation("Invocation resolution: {Resolved} of {Total} invocations bound to a method ({Percent:F1}%)",
            resolved, total, total == 0 ? 100.0 : 100.0 * resolved / total);

        int methodsWithoutModel = 0, ambiguousAccesses = 0;
        foreach (var document in codebase.Documents)
        {
            var fileData = AnalyzeFileWithModel(document.Tree, document.SemanticModel);
            if (fileData != null)
            {
                allClasses.AddRange(fileData.Classes);
                allMethods.AddRange(fileData.Methods);
                allDependencies.AddRange(fileData.Dependencies);
                methodsWithoutModel += fileData.MethodsWithoutSemanticModel;
                ambiguousAccesses += fileData.AmbiguousAccesses;

                foreach (var classElement in fileData.Classes)
                {
                    _relationshipBuilder.AddClass(classElement, document.FilePath);
                }
            }
        }

        _logger?.LogInformation("Built relationship graph with {Count} class declarations", allClasses.Count);
        _logger?.LogInformation("LCOM attribute accesses: {Methods} methods matched by name (no semantic model), {Ambiguous} ambiguous identifiers not counted",
            methodsWithoutModel, ambiguousAccesses);

        // Phase 2: Calculate CK metrics for each class (one row per class, joining partial declarations)
        _logger?.LogInformation("Phase 2: Calculating CK metrics...");
        var allClassesMap = _relationshipBuilder.GetAllClasses();
        var dependencies = ClassDependency.MergeByClass(allDependencies);

        foreach (var classElement in allClasses.GroupBy(c => c.ClassKey!).Select(g => g.First()))
        {
            try
            {
                var classKey = classElement.ClassKey!;
                var filePath = allClassesMap.TryGetValue(classKey, out var classInfo) ? classInfo.FilePath : string.Empty;

                var wmc = _wmcCalculator.CalculateWmc(classKey, allMethods);
                var dit = _ditCalculator.CalculateDit(classKey);
                var noc = _nocCalculator.CalculateNoc(classKey);

                var dependency = dependencies.FirstOrDefault(d => d.ClassKey == classKey);
                var cbo = dependency != null 
                    ? _cboCalculator.CalculateCbo(dependency, allClassesMap) 
                    : 0;

                var rfc = _rfcCalculator.CalculateRfc(classKey, allMethods);
                var lcom = _lcomCalculator.CalculateLcom(classKey, allMethods, dependencies);

                var metric = new CkMetric
                {
                    FilePath = filePath,
                    ClassName = classElement.Name,
                    WMC = wmc,
                    DIT = dit,
                    NOC = noc,
                    CBO = cbo,
                    RFC = rfc,
                    LCOM = lcom
                };

                allMetrics.Add(metric);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating CK metrics for class: {ClassName}", classElement.Name);
            }
        }

        _logger?.LogInformation("Calculated CK metrics for {Count} classes", allMetrics.Count);

        return allMetrics;
    }

    private FileAnalysisData? AnalyzeFileWithModel(Microsoft.CodeAnalysis.SyntaxTree syntaxTree, Microsoft.CodeAnalysis.SemanticModel semanticModel)
    {
        try
        {
            var visitor = new CkSyntaxVisitor(semanticModel);
            visitor.Visit(syntaxTree.GetRoot());

            return new FileAnalysisData
            {
                Classes = visitor.GetClasses().ToList(),
                Methods = visitor.GetMethods().ToList(),
                Dependencies = visitor.GetDependencies().ToList(),
                MethodsWithoutSemanticModel = visitor.MethodsWithoutSemanticModel,
                AmbiguousAccesses = visitor.AmbiguousAccesses
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error analyzing syntax tree for relationships");
            return null;
        }
    }

    private class FileAnalysisData
    {
        public List<CkCodeElement> Classes { get; set; } = new();
        public List<CkMethodInfo> Methods { get; set; } = new();
        public List<ClassDependency> Dependencies { get; set; } = new();
        public int MethodsWithoutSemanticModel { get; set; }
        public int AmbiguousAccesses { get; set; }
    }
}

