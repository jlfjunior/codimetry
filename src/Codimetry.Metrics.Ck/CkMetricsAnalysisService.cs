using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Codimetry.Core.Loading;
using Codimetry.Core.Model;
using Codimetry.Core.Suites;

namespace Codimetry.Metrics.Ck;

/// <summary>
/// Service that orchestrates the calculation of CK metrics for all classes.
/// </summary>
public class CkMetricsAnalysisService
{
    private readonly WmcCalculator _wmcCalculator;
    private readonly CboCalculator _cboCalculator;
    private readonly RfcCalculator _rfcCalculator;
    private readonly LcomCalculator _lcomCalculator;
    private readonly CboOptions _cboOptions;
    private readonly bool _rfcExcludeConstructors;
    private readonly ILogger<CkMetricsAnalysisService>? _logger;

    public CkMetricsAnalysisService(
        ILogger<CkMetricsAnalysisService>? logger = null,
        WmcWeight wmcWeight = WmcWeight.Unit,
        bool wmcExcludeStatic = false,
        CboOptions? cboOptions = null,
        bool rfcExcludeConstructors = false)
    {
        _wmcCalculator = new WmcCalculator(wmcWeight, wmcExcludeStatic);
        _cboOptions = cboOptions ?? CboOptions.Default;
        _cboCalculator = new CboCalculator();
        _rfcExcludeConstructors = rfcExcludeConstructors;
        _rfcCalculator = new RfcCalculator(rfcExcludeConstructors);
        _lcomCalculator = new LcomCalculator();
        _logger = logger;
    }

    /// <summary>
    /// Analyzes code and calculates CK metrics for all classes.
    /// </summary>
    public Task<List<CkMetric>> AnalyzeAsync(AnalysisContext context)
    {
        var allMetrics = new List<CkMetric>();
        var ditOptions = context.DitOptions;

        _logger?.LogInformation("DIT rule: {Rule}", ditOptions.CountObject
            ? "System.Object counts as a level, legacy (--dit-count-object)"
            : ditOptions.ResolveFramework
                ? "declared inheritance, with the ancestors of external base classes (--dit-resolve-framework)"
                : "declared inheritance; an external base class counts 1, System.Object does not count");
        _logger?.LogInformation("CBO rule: {Rule}, {Direction}", _cboOptions.DeclaredTypesOnly
                ? "types of fields, properties and method signatures, classes only, legacy (--cbo-declared-types-only)"
                : "every type reference to classes and interfaces of the analysed code",
            _cboOptions.Bidirectional ? "outgoing and incoming (--cbo-bidirectional)" : "outgoing");
        _logger?.LogInformation("RFC rule: {Rule}", _rfcExcludeConstructors
            ? "methods only, legacy (--rfc-exclude-constructors)"
            : "methods and constructors (new, base(...))");

        // Phase 1: Build class relationship graph
        _logger?.LogInformation("Phase 1: Building class relationship graph...");
        var codebase = context.Codebase;
        _logger?.LogInformation("References: {References}", FrameworkReferences.Description);
        var (resolved, total) = codebase.InvocationResolution();
        _logger?.LogInformation("Invocation resolution: {Resolved} of {Total} invocations bound to a method ({Percent:F1}%)",
            resolved, total, total == 0 ? 100.0 : 100.0 * resolved / total);
        var (resolvedCtors, totalCtors) = codebase.ConstructorCallResolution();
        _logger?.LogInformation("Constructor call resolution: {Resolved} of {Total} object creations and constructor initializers bound to a constructor ({Percent:F1}%)",
            resolvedCtors, totalCtors, totalCtors == 0 ? 100.0 : 100.0 * resolvedCtors / totalCtors);

        var types = context.Types;
        var allClasses = types.Classes;
        var allMethods = types.Methods;
        var relationships = types.Relationships;
        var allDependencies = new List<ClassDependency>();
        var allInterfaces = new List<CouplingTarget>();

        foreach (var document in codebase.Documents)
        {
            try
            {
                var coupling = new CouplingCollector(document.SemanticModel, context.TypeScope, _cboOptions);
                coupling.Visit(document.Tree.GetRoot());
                allDependencies.AddRange(coupling.GetDependencies());
                allInterfaces.AddRange(coupling.GetInterfaces());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error analyzing syntax tree for relationships");
            }
        }

        _logger?.LogInformation("Built relationship graph with {Count} class declarations", allClasses.Count);
        _logger?.LogInformation("Measured types by kind: {Kinds}", string.Join(", ",
            allClasses.GroupBy(c => c.ClassKey).Select(g => g.First().Kind).GroupBy(k => k).OrderBy(g => g.Key)
                .Select(g => $"{g.Key} {g.Count()}")));
        _logger?.LogInformation("LCOM attribute accesses: {Methods} methods matched by name (no semantic model), {Ambiguous} ambiguous identifiers not counted",
            types.MethodsWithoutSemanticModel, types.AmbiguousAccesses);

        // Phase 2: Calculate CK metrics for each class (one row per class, joining partial declarations)
        _logger?.LogInformation("Phase 2: Calculating CK metrics...");
        var allClassesMap = relationships.GetAllClasses();
        var ditCalculator = new DitCalculator(relationships, ditOptions.ResolveFramework);
        var nocCalculator = new NocCalculator(relationships);
        var dependencies = ClassDependency.MergeByClass(allDependencies);
        _logger?.LogInformation("CBO type references: {Count} not resolved by the semantic model, matched by exact name",
            CboCalculator.UnresolvedReferences(dependencies));
        var cboTargets = CboCalculator.Targets(allClassesMap, _cboOptions.DeclaredTypesOnly ? [] : allInterfaces);
        var coupled = dependencies.ToDictionary(d => d.ClassKey, d => _cboCalculator.Coupled(d, cboTargets));
        var cboByClass = _cboOptions.Bidirectional
            ? CboCalculator.Bidirectional(coupled)
            : coupled.ToDictionary(c => c.Key, c => c.Value.Count);

        foreach (var classElement in allClasses.GroupBy(c => c.ClassKey!).Select(g => g.First()))
        {
            try
            {
                var classKey = classElement.ClassKey!;
                var filePath = allClassesMap.TryGetValue(classKey, out var classInfo) ? classInfo.FilePath : string.Empty;

                var wmc = _wmcCalculator.CalculateWmc(classKey, allMethods);
                var dit = ditCalculator.CalculateDit(classKey);
                var noc = nocCalculator.CalculateNoc(classKey);

                var cbo = cboByClass.GetValueOrDefault(classKey);

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
                    LCOM = lcom,
                    LcomMethods = LcomCalculator.Methods(classKey, allMethods).Count(),
                    Kind = classElement.Kind
                };

                allMetrics.Add(metric);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating CK metrics for class: {ClassName}", classElement.Name);
            }
        }

        _logger?.LogInformation("Calculated CK metrics for {Count} classes", allMetrics.Count);

        return Task.FromResult(allMetrics);
    }
}
