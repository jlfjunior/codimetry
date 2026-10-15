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
    private readonly CodebaseLoader _codebaseLoader;
    private readonly bool _additiveMc;
    private readonly bool _acTypeWeights;
    private readonly ILogger<MisraMetricsAnalysisService>? _logger;

    public MisraMetricsAnalysisService(
        FileDiscoveryService fileDiscoveryService,
        RoslynParser roslynParser,
        ClassRelationshipBuilder relationshipBuilder,
        ILogger<MisraMetricsAnalysisService>? logger = null,
        CodebaseLoader? codebaseLoader = null,
        bool additiveMc = false,
        bool acTypeWeights = false)
    {
        _fileDiscoveryService = fileDiscoveryService;
        _roslynParser = roslynParser;
        _relationshipBuilder = relationshipBuilder;
        _mcCalculator = new McCalculator();
        _acCalculator = new AcCalculator(acTypeWeights);
        _clcCalculator = new ClcCalculator(acTypeWeights);
        _cwcCalculator = new CwcCalculator();
        _ccCalculator = new CcCalculator(acTypeWeights);
        _codebaseLoader = codebaseLoader ?? new CodebaseLoader(roslynParser);
        _additiveMc = additiveMc;
        _acTypeWeights = acTypeWeights;
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
        _logger?.LogInformation("MC rule: {Rule}", _additiveMc
            ? "additive, v1 (--mc-additive)"
            : "composition, Eq. 23 (sum per layer, product along nesting)");
        _logger?.LogInformation("AC rule: {Rule}", _acTypeWeights
            ? "fields and properties weighted by type, static and const included, v1 (--ac-type-weights)"
            : "one per instance attribute (Eq. 25)");

        // Phase 1: Build class relationship graph and collect BCS information
        _logger?.LogInformation("Phase 1: Building class relationship graph and collecting BCS information...");
        var allClasses = new List<CkCodeElement>();
        var allMethods = new List<CkMethodInfo>();
        var allDependencies = new List<ClassDependency>();
        var allBcsInfo = new Dictionary<string, BcsInfo>();
        var allClassAttributes = new Dictionary<string, List<string>>();
        var allClassAttributeTypes = new Dictionary<string, List<string>>();

        // One compilation per project, so that class keys match across files
        var codebase = _codebaseLoader.Load(files, Directory.Exists(path) ? path : Path.GetDirectoryName(Path.GetFullPath(path)));
        var implementations = codebase.AbstractImplementations();

        foreach (var document in codebase.Documents)
        {
            var filePath = document.FilePath;
            var fileData = AnalyzeFileForMisraMetrics(document);
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

                // Merge class attributes (a partial class contributes from several files)
                foreach (var kvp in fileData.ClassAttributes)
                {
                    if (allClassAttributes.TryGetValue(kvp.Key, out var attributes))
                    {
                        attributes.AddRange(kvp.Value);
                        allClassAttributeTypes[kvp.Key].AddRange(fileData.ClassAttributeTypes[kvp.Key]);
                    }
                    else
                    {
                        allClassAttributes[kvp.Key] = new List<string>(kvp.Value);
                        allClassAttributeTypes[kvp.Key] = new List<string>(fileData.ClassAttributeTypes[kvp.Key]);
                    }
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
            if (string.IsNullOrEmpty(method.ContainingClassKey))
                continue;

            // A method without a body has no MC; v1 reported it as 0
            if (!_additiveMc && allBcsInfo.TryGetValue($"{method.ContainingClassKey}::{method.Key}", out var bcs) && !bcs.HasBody)
                continue;

            try
            {
                var classKey = method.ContainingClassKey;
                var filePath = allClassesMap.TryGetValue(classKey, out var classInfo) ? classInfo.FilePath : string.Empty;

                var mc = _mcCalculator.CalculateMc(classKey, method.Key, allBcsInfo);

                var metric = new MisraMetric
                {
                    FilePath = filePath,
                    ClassName = method.ContainingClass ?? string.Empty,
                    MethodName = method.Name,
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

        // One row per class, joining partial declarations
        var distinctClasses = allClasses.GroupBy(c => c.ClassKey!).Select(g => g.First()).ToList();

        foreach (var classElement in distinctClasses)
        {
            try
            {
                var classKey = classElement.ClassKey!;
                var filePath = allClassesMap.TryGetValue(classKey, out var classInfo) ? classInfo.FilePath : string.Empty;

                var ac = _acCalculator.CalculateAc(classKey, allClassAttributes, allClassAttributeTypes);
                var clc = _clcCalculator.CalculateClc(classKey, allBcsInfo, allClassAttributes, allClassAttributeTypes);
                var cwc = _cwcCalculator.CalculateCwc(classKey, allMethods, allBcsInfo, allClassesMap, implementations);

                var metric = new MisraMetric
                {
                    FilePath = filePath,
                    ClassName = classElement.Name,
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
        var allClassKeys = distinctClasses.Select(c => c.ClassKey!).ToList();
        var cc = _ccCalculator.CalculateCc(
            allClassKeys,
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

    private FileMisraAnalysisData? AnalyzeFileForMisraMetrics(SourceDocument document)
    {
        var filePath = document.FilePath;
        try
        {
            var syntaxTree = document.Tree;
            var semanticModel = document.SemanticModel;

            // Use CK visitor for class/method/dependency info
            var ckVisitor = new CkSyntaxVisitor(semanticModel);
            ckVisitor.Visit(syntaxTree.GetRoot());

            // Use MISRA visitor for BCS info and attributes
            var misraVisitor = new MisraSyntaxVisitor(semanticModel, _additiveMc, _acTypeWeights);
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

