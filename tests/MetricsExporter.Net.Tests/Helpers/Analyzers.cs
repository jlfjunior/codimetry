using MetricsExporter.Net.Core.Models;
using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Services;

namespace MetricsExporter.Net.Tests.Helpers;

/// <summary>
/// Builds each analysis service the same way the CLI does and runs it over a path.
/// </summary>
public static class Analyzers
{
    public static Task<List<LocMetric>> Loc(string path, bool recursive = true, TypeScope? typeScope = null) =>
        new MetricsAnalysisService(new FileDiscoveryService(), new RoslynParser(), new LocCalculator(),
                new LogicalLocCalculator(), new PhysicalLocCalculator(), typeScope: typeScope)
            .AnalyzeAsync(path, recursive);

    public static Task<List<CyclomaticComplexityMetric>> Cyclomatic(string path, CyclomaticOptions? options = null,
        TypeScope? typeScope = null) =>
        new CyclomaticComplexityAnalysisService(new FileDiscoveryService(), new RoslynParser(), options: options, scope: typeScope)
            .AnalyzeAsync(path);

    public static Task<List<CkMetric>> Ck(string path, WmcWeight wmcWeight = WmcWeight.Unit, bool wmcExcludeStatic = false,
        CyclomaticOptions? cyclomaticOptions = null, TypeScope? typeScope = null) =>
        new CkMetricsAnalysisService(new FileDiscoveryService(), new RoslynParser(), new ClassRelationshipBuilder(),
                wmcWeight: wmcWeight, wmcExcludeStatic: wmcExcludeStatic, cyclomaticOptions: cyclomaticOptions, typeScope: typeScope)
            .AnalyzeAsync(path);

    public static Task<List<MisraMetric>> Misra(string path, bool additiveMc = false, bool acTypeWeights = false,
        TypeScope? typeScope = null, int libraryCallWeight = 0, McLambdaRule lambdaRule = McLambdaRule.Full) =>
        new MisraMetricsAnalysisService(new FileDiscoveryService(), new RoslynParser(), new ClassRelationshipBuilder(),
                additiveMc: additiveMc, acTypeWeights: acTypeWeights, typeScope: typeScope, libraryCallWeight: libraryCallWeight,
                lambdaRule: lambdaRule)
            .AnalyzeAsync(path);
}
