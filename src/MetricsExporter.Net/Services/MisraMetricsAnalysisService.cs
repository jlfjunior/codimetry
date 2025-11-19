using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using MetricsExporter.Net.Core.Models;
using MetricsExporter.Net.Parsers;

namespace MetricsExporter.Net.Services;

/// <summary>
/// Service that orchestrates the calculation of MISRA metrics for all classes and methods.
/// </summary>
public class MisraMetricsAnalysisService
{
    private readonly FileDiscoveryService _fileDiscoveryService;
    private readonly RoslynParser _roslynParser;
    private readonly ClassRelationshipBuilder _relationshipBuilder;
    private readonly McCalculator _mcCalculator;
    private readonly AcCalculator _acCalculator;
    private readonly ClcCalculator _clcCalculator;
    private readonly CwcCalculator _cwcCalculator;
    private readonly CcCalculator _ccCalculator;
    private readonly ILogger<MisraMetricsAnalysisService>? _logger;

    public MisraMetricsAnalysisService(
        FileDiscoveryService fileDiscoveryService,
        RoslynParser roslynParser,
        ClassRelationshipBuilder relationshipBuilder,
        ILogger<MisraMetricsAnalysisService>? logger = null)
    {
        _fileDiscoveryService = fileDiscoveryService;
        _roslynParser = roslynParser;
        _relationshipBuilder = relationshipBuilder;
        _mcCalculator = new McCalculator();
        _acCalculator = new AcCalculator();
        _clcCalculator = new ClcCalculator();
        _cwcCalculator = new CwcCalculator();
        _ccCalculator = new CcCalculator();
        _logger = logger;
    }

    /// <summary>
    /// Analyzes code and calculates MISRA metrics for all classes and methods.
    /// </summary>
    public async Task<List<MisraMetric>> AnalyzeAsync(string path, bool recursive = true)
    {
        var allMetrics = new List<MisraMetric>();
        var files = _fileDiscoveryService.DiscoverFiles(path, recursive).ToList();

        _logger?.LogInformation("Discovered {Count} C# files for MISRA metrics analysis", files.Count);

        // Phase 1: Build class relationship graph and collect BCS information
        _logger?.LogInformation("Phase 1: Building class relationship graph and collecting BCS information...");
        var allClasses = new List<CkCodeElement>();
        var allMethods = new List<CkMethodInfo>();
        var allDependencies = new List<ClassDependency>();
        var allBcsInfo = new Dictionary<string, BcsInfo>();
        var allClassAttributes = new Dictionary<string, List<string>>();
        var allClassAttributeTypes = new Dictionary<string, List<string>>();

        foreach (var filePath in files)
        {
            var fileData = AnalyzeFileForMisraMetrics(filePath);
            if (fileData != null)
            {
                allClasses.AddRange(fileData.Classes);
                allMethods.AddRange(fileData.Methods);
                allDependencies.AddRange(fileData.Dependencies);

                // Merge BCS info
                foreach (var kvp in fileData.BcsInfo)
                {
                    allBcsInfo[kvp.Key] = kvp.Value;
                }

                // Merge class attributes
                foreach (var kvp in fileData.ClassAttributes)
                {
                    allClassAttributes[kvp.Key] = kvp.Value;
                }

                foreach (var kvp in fileData.ClassAttributeTypes)
                {
                    allClassAttributeTypes[kvp.Key] = kvp.Value;
                }

                // Add classes to relationship builder
                foreach (var classElement in fileData.Classes)
                {
                    _relationshipBuilder.AddClass(classElement, filePath);
                }
            }
        }

        _logger?.LogInformation("Built relationship graph with {Count} classes", allClasses.Count);
        _logger?.LogInformation("Collected BCS information for {Count} methods", allBcsInfo.Count);

        // Phase 2: Calculate method-level metrics (MC)
        _logger?.LogInformation("Phase 2: Calculating method-level metrics (MC)...");
        var allClassesMap = _relationshipBuilder.GetAllClasses();
        var methodMetrics = new List<MisraMetric>();

        foreach (var method in allMethods)
        {
            if (string.IsNullOrEmpty(method.ContainingClass))
                continue;

            try
            {
                var className = method.ContainingClass;
                var methodName = method.Name;
                var filePath = allClassesMap.Values
                    .FirstOrDefault(c => c.Name == className)?.FilePath ?? string.Empty;

                var mc = _mcCalculator.CalculateMc(className, methodName, allBcsInfo);

                var metric = new MisraMetric
                {
                    FilePath = filePath,
                    ClassName = className,
                    MethodName = methodName,
                    MC = mc
                };

                methodMetrics.Add(metric);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating MC for method: {MethodName}", method.Name);
            }
        }

        allMetrics.AddRange(methodMetrics);
        _logger?.LogInformation("Calculated MC for {Count} methods", methodMetrics.Count);

        // Phase 3: Calculate class-level metrics (AC, CLC, CWC)
        _logger?.LogInformation("Phase 3: Calculating class-level metrics (AC, CLC, CWC)...");
        var classMetrics = new List<MisraMetric>();

        foreach (var classElement in allClasses)
        {
            try
            {
                var className = classElement.Name;
                var filePath = allClassesMap.Values
                    .FirstOrDefault(c => c.Name == className)?.FilePath ?? string.Empty;

                var ac = _acCalculator.CalculateAc(className, allClassAttributes, allClassAttributeTypes);
                var clc = _clcCalculator.CalculateClc(className, allBcsInfo, allClassAttributes, allClassAttributeTypes);
                var cwc = _cwcCalculator.CalculateCwc(className, allMethods, allBcsInfo, allClassesMap);

                var metric = new MisraMetric
                {
                    FilePath = filePath,
                    ClassName = className,
                    MethodName = string.Empty, // Class-level
                    AC = ac,
                    CLC = clc,
                    CWC = cwc
                };

                classMetrics.Add(metric);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating class-level MISRA metrics for class: {ClassName}", classElement.Name);
            }
        }

        allMetrics.AddRange(classMetrics);
        _logger?.LogInformation("Calculated class-level metrics for {Count} classes", classMetrics.Count);

        // Phase 4: Calculate codebase-level metric (CC)
        _logger?.LogInformation("Phase 4: Calculating codebase-level metric (CC)...");
        var allClassNames = allClasses.Select(c => c.Name).ToList();
        var cc = _ccCalculator.CalculateCc(
            allClassNames,
            allBcsInfo,
            allClassAttributes,
            allClassAttributeTypes,
            allMethods,
            allClassesMap);

        // Add codebase-level metric
        var codebaseMetric = new MisraMetric
        {
            FilePath = string.Empty,
            ClassName = string.Empty,
            MethodName = string.Empty,
            CC = cc
        };

        allMetrics.Add(codebaseMetric);
        _logger?.LogInformation("Calculated codebase complexity (CC): {CC}", cc);

        _logger?.LogInformation("MISRA metrics analysis complete. Total metrics: {Count}", allMetrics.Count);

        return allMetrics;
    }

    private FileMisraAnalysisData? AnalyzeFileForMisraMetrics(string filePath)
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

            // Use CK visitor for class/method/dependency info
            var ckVisitor = new CkSyntaxVisitor(semanticModel);
            ckVisitor.Visit(syntaxTree.GetRoot());

            // Use MISRA visitor for BCS info and attributes
            var misraVisitor = new MisraSyntaxVisitor();
            misraVisitor.Visit(syntaxTree.GetRoot());

            return new FileMisraAnalysisData
            {
                Classes = ckVisitor.GetClasses().ToList(),
                Methods = ckVisitor.GetMethods().ToList(),
                Dependencies = ckVisitor.GetDependencies().ToList(),
                BcsInfo = misraVisitor.GetMethodBcsInfo().ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ClassAttributes = misraVisitor.GetClassAttributes().ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ClassAttributeTypes = misraVisitor.GetClassAttributeTypes().ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error analyzing file for MISRA metrics: {FilePath}", filePath);
            return null;
        }
    }

    private class FileMisraAnalysisData
    {
        public List<CkCodeElement> Classes { get; set; } = new();
        public List<CkMethodInfo> Methods { get; set; } = new();
        public List<ClassDependency> Dependencies { get; set; } = new();
        public Dictionary<string, BcsInfo> BcsInfo { get; set; } = new();
        public Dictionary<string, List<string>> ClassAttributes { get; set; } = new();
        public Dictionary<string, List<string>> ClassAttributeTypes { get; set; } = new();
    }
}

