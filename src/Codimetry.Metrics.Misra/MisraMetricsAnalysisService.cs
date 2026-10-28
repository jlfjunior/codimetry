using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Codimetry.Core.Loading;
using Codimetry.Core.Model;
using Codimetry.Core.Suites;

namespace Codimetry.Metrics.Misra;

/// <summary>
/// Service that orchestrates the calculation of MISRA metrics for all classes and methods.
/// </summary>
public class MisraMetricsAnalysisService
{
    private readonly McCalculator _mcCalculator;
    private readonly AcCalculator _acCalculator;
    private readonly ClcCalculator _clcCalculator;
    private readonly CwcCalculator _cwcCalculator;
    private readonly CcCalculator _ccCalculator;
    private readonly bool _additiveMc;
    private readonly bool _acTypeWeights;
    private readonly int _libraryCallWeight;
    private readonly McLambdaRule _lambdaRule;
    private readonly int _cwcLibraryCallWeight;
    private readonly CocFormula _cocFormula;
    private readonly bool _cocLegacy;
    private readonly ILogger<MisraMetricsAnalysisService>? _logger;

    public MisraMetricsAnalysisService(
        ILogger<MisraMetricsAnalysisService>? logger = null,
        bool additiveMc = false,
        bool acTypeWeights = false,
        int libraryCallWeight = 0,
        McLambdaRule lambdaRule = McLambdaRule.Full,
        int cwcLibraryCallWeight = 0,
        CocFormula cocFormula = CocFormula.Tree,
        bool cocLegacy = false)
    {
        _cocFormula = cocFormula;
        _cocLegacy = cocLegacy;
        _mcCalculator = new McCalculator();
        _acCalculator = new AcCalculator(acTypeWeights);
        _clcCalculator = new ClcCalculator(acTypeWeights);
        _cwcCalculator = new CwcCalculator(cwcLibraryCallWeight);
        _ccCalculator = new CcCalculator(acTypeWeights, cwcLibraryCallWeight);
        _cwcLibraryCallWeight = cwcLibraryCallWeight;
        _additiveMc = additiveMc;
        _acTypeWeights = acTypeWeights;
        _libraryCallWeight = libraryCallWeight;
        _lambdaRule = lambdaRule;
        _logger = logger;
    }

    /// <summary>
    /// The CoC of each inheritance hierarchy of the last analysis, root first by key.
    /// </summary>
    public IReadOnlyList<CocHierarchyMetric> Hierarchies { get; private set; } = [];

    /// <summary>
    /// Analyzes code and calculates MISRA metrics for all classes and methods.
    /// </summary>
    public Task<List<MisraMetric>> AnalyzeAsync(AnalysisContext context)
    {
        var allMetrics = new List<MisraMetric>();

        _logger?.LogInformation("MC rule: {Rule}", _additiveMc
            ? "additive, legacy (--mc-additive)"
            : $"composition, Eq. 23 (sum per layer, product along nesting); a call to the analysed code adds 2 + MC, a library call adds {_libraryCallWeight}; lambdas and local functions: {_lambdaRule.ToString().ToLowerInvariant()}");
        _logger?.LogInformation("AC rule: {Rule}", _acTypeWeights
            ? "fields and properties weighted by type, static and const included, legacy (--ac-type-weights)"
            : "one per instance attribute (Eq. 25)");
        _logger?.LogInformation("CWC rule: 2 + MC per call to another type of the analysed code (Eq. 24); a library call adds {Weight}",
            _cwcLibraryCallWeight);
        _logger?.LogInformation("CoC rule: {Rule}", _cocLegacy
            ? "Σ CLC + Σ CWC / 2, legacy (--coc-legacy)"
            : _cocFormula == CocFormula.Tree
                ? "Eq. 27 by subtree, as in the example of Misra et al.: CLC of a class × Σ CoC of its subclasses; hierarchies add"
                : "Eq. 27 by level (--coc-formula levels): Π over levels of Σ CLC of the level; hierarchies add");

        // Phase 1: Build class relationship graph and collect BCS information
        _logger?.LogInformation("Phase 1: Building class relationship graph and collecting BCS information...");
        var types = context.Types;
        var allClasses = types.Classes;
        var allMethods = types.Methods;
        var relationships = types.Relationships;
        var allBcsInfo = new Dictionary<string, BcsInfo>();
        var allClassAttributes = new Dictionary<string, List<string>>();
        var allClassAttributeTypes = new Dictionary<string, List<string>>();

        var codebase = context.Codebase;
        var implementations = codebase.AbstractImplementations();
        var mcGraph = _additiveMc ? null : new MethodComplexityGraph(codebase.Documents, implementations, _libraryCallWeight, _lambdaRule);

        foreach (var document in codebase.Documents)
        {
            var fileData = AnalyzeFileForMisraMetrics(document, mcGraph, context.TypeScope);
            if (fileData != null)
            {
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
            }
        }

        _logger?.LogInformation("Built relationship graph with {Count} classes", allClasses.Count);
        _logger?.LogInformation("Collected BCS information for {Count} methods", allBcsInfo.Count);
        if (mcGraph != null)
        {
            _logger?.LogInformation("MC calls: {System} to the analysed code, {Recursive} recursive, {Abstract} abstract without a single implementation, {Library} to libraries, {Unresolved} unresolved",
                mcGraph.SystemCalls, mcGraph.RecursiveCalls, mcGraph.AbstractCalls, mcGraph.LibraryCalls, mcGraph.UnresolvedCalls);
        }

        // Phase 2: Calculate method-level metrics (MC)
        _logger?.LogInformation("Phase 2: Calculating method-level metrics (MC)...");
        var allClassesMap = relationships.GetAllClasses();
        var kinds = allClasses.GroupBy(c => c.ClassKey!).ToDictionary(g => g.Key, g => g.First().Kind);
        var methodMetrics = new List<MisraMetric>();

        foreach (var method in allMethods)
        {
            if (string.IsNullOrEmpty(method.ContainingClassKey))
                continue;

            // A method without a body has no MC; earlier versions reported it as 0
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
                    MC = mc,
                    Kind = kinds.GetValueOrDefault(classKey, string.Empty)
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
        var clcByClass = new Dictionary<string, int>();

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
                    CWC = cwc,
                    Kind = classElement.Kind
                };

                classMetrics.Add(metric);
                clcByClass[classKey] = clc;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating class-level MISRA metrics for class: {ClassName}", classElement.Name);
            }
        }

        allMetrics.AddRange(classMetrics);
        var unresolved = distinctClasses.Select(c => CwcCalculator.UnresolvedCalls(c.ClassKey!, allMethods)).Where(n => n > 0).ToList();
        _logger?.LogInformation("CWC: {Calls} unresolved calls in {Classes} classes, not counted", unresolved.Sum(), unresolved.Count);
        _logger?.LogInformation("Calculated class-level metrics for {Count} classes", classMetrics.Count);

        // Phase 4: Calculate codebase-level metric (CoC)
        _logger?.LogInformation("Phase 4: Calculating codebase-level metric (CoC)...");
        var cocCalculator = new CocCalculator(_cocFormula);
        var hierarchies = cocCalculator.Hierarchies(
            clcByClass,
            key => allClassesMap.TryGetValue(key, out var info) ? info.BaseClass : null,
            relationships.GetChildren);
        Hierarchies = hierarchies
            .Select(h => new CocHierarchyMetric
            {
                FilePath = allClassesMap.TryGetValue(h.RootKey, out var info) ? info.FilePath : string.Empty,
                ClassName = allClassesMap.TryGetValue(h.RootKey, out var root) ? root.Name : h.RootKey,
                Classes = h.Classes,
                Levels = h.Levels,
                CoC = h.CoC,
                Kind = kinds.GetValueOrDefault(h.RootKey, string.Empty)
            })
            .ToList();

        long cc;
        if (_cocLegacy)
        {
            cc = _ccCalculator.CalculateCc(
                distinctClasses.Select(c => c.ClassKey!).ToList(),
                allBcsInfo,
                allClassAttributes,
                allClassAttributeTypes,
                allMethods,
                allClassesMap,
                implementations);
        }
        else
        {
            cc = cocCalculator.CalculateCoc(hierarchies);
        }

        _logger?.LogInformation("CoC: {Hierarchies} inheritance hierarchies, {Inheriting} of them with subclasses, deepest {Levels} levels",
            hierarchies.Count, hierarchies.Count(h => h.Levels > 1), hierarchies.Count == 0 ? 0 : hierarchies.Max(h => h.Levels));
        if (cocCalculator.Overflows > 0)
        {
            _logger?.LogWarning("CoC overflowed 64 bits in {Count} places; reported as {Max}", cocCalculator.Overflows, long.MaxValue);
        }

        // Add codebase-level metric
        var codebaseMetric = new MisraMetric
        {
            FilePath = string.Empty,
            ClassName = string.Empty,
            MethodName = string.Empty,
            CoC = cc
        };

        allMetrics.Add(codebaseMetric);
        _logger?.LogInformation("Calculated codebase complexity (CoC): {CoC}", cc);

        _logger?.LogInformation("MISRA metrics analysis complete. Total metrics: {Count}", allMetrics.Count);

        return Task.FromResult(allMetrics);
    }

    private FileMisraAnalysisData? AnalyzeFileForMisraMetrics(SourceDocument document, MethodComplexityGraph? mcGraph, TypeScope typeScope)
    {
        var filePath = document.FilePath;
        try
        {
            var syntaxTree = document.Tree;
            var semanticModel = document.SemanticModel;

            // Use MISRA visitor for BCS info and attributes
            var misraVisitor = new MisraSyntaxVisitor(semanticModel, _additiveMc, _acTypeWeights, typeScope, mcGraph);
            misraVisitor.Visit(syntaxTree.GetRoot());

            return new FileMisraAnalysisData
            {
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
        public Dictionary<string, BcsInfo> BcsInfo { get; set; } = new();
        public Dictionary<string, List<string>> ClassAttributes { get; set; } = new();
        public Dictionary<string, List<string>> ClassAttributeTypes { get; set; } = new();
    }
}

