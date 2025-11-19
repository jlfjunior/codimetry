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
    private readonly ILogger<CkMetricsAnalysisService>? _logger;

    public CkMetricsAnalysisService(
        FileDiscoveryService fileDiscoveryService,
        RoslynParser roslynParser,
        ClassRelationshipBuilder relationshipBuilder,
        ILogger<CkMetricsAnalysisService>? logger = null)
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

        foreach (var filePath in files)
        {
            var fileData = AnalyzeFileForRelationships(filePath);
            if (fileData != null)
            {
                allClasses.AddRange(fileData.Classes);
                allMethods.AddRange(fileData.Methods);
                allDependencies.AddRange(fileData.Dependencies);

                // Add classes to relationship builder
                foreach (var classElement in fileData.Classes)
                {
                    _relationshipBuilder.AddClass(classElement, filePath);
                }
            }
        }

        _logger?.LogInformation("Built relationship graph with {Count} classes", allClasses.Count);

        // Phase 2: Calculate CK metrics for each class
        _logger?.LogInformation("Phase 2: Calculating CK metrics...");
        var allClassesMap = _relationshipBuilder.GetAllClasses();

        foreach (var classElement in allClasses)
        {
            try
            {
                var className = classElement.Name;
                var filePath = allClassesMap.Values
                    .FirstOrDefault(c => c.Name == className)?.FilePath ?? string.Empty;

                var wmc = _wmcCalculator.CalculateWmc(className, allMethods);
                var dit = _ditCalculator.CalculateDit(className);
                var noc = _nocCalculator.CalculateNoc(className);

                // Find dependency for this class
                var dependency = allDependencies.FirstOrDefault(d => d.ClassName == className);
                var cbo = dependency != null 
                    ? _cboCalculator.CalculateCbo(dependency, allClassesMap) 
                    : 0;

                var rfc = _rfcCalculator.CalculateRfc(className, allMethods);
                var lcom = _lcomCalculator.CalculateLcom(className, allMethods, allDependencies);

                var metric = new CkMetric
                {
                    FilePath = filePath,
                    ClassName = className,
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

    private FileAnalysisData? AnalyzeFileForRelationships(string filePath)
    {
        try
        {
            var syntaxTree = _roslynParser.ParseFile(filePath);
            if (syntaxTree == null)
            {
                return null;
            }

            // Create compilation to get semantic model
            var compilation = CSharpCompilation.Create("Temp")
                .AddSyntaxTrees(syntaxTree)
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var visitor = new CkSyntaxVisitor(semanticModel);
            visitor.Visit(syntaxTree.GetRoot());

            return new FileAnalysisData
            {
                Classes = visitor.GetClasses().ToList(),
                Methods = visitor.GetMethods().ToList(),
                Dependencies = visitor.GetDependencies().ToList()
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error analyzing file for relationships: {FilePath}", filePath);
            return null;
        }
    }

    private class FileAnalysisData
    {
        public List<CkCodeElement> Classes { get; set; } = new();
        public List<CkMethodInfo> Methods { get; set; } = new();
        public List<ClassDependency> Dependencies { get; set; } = new();
    }
}

