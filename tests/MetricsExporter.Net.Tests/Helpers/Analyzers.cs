using MetricsExporter.Net.Core.Models;
using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Services;

namespace MetricsExporter.Net.Tests.Helpers;

/// <summary>
/// Builds each analysis service the same way the CLI does and runs it over a path.
/// </summary>
public static class Analyzers
{
    public static Task<List<LocMetric>> Loc(string path, bool recursive = true) =>
        new MetricsAnalysisService(new FileDiscoveryService(), new RoslynParser(), new LocCalculator(),
                new LogicalLocCalculator(), new PhysicalLocCalculator())
            .AnalyzeAsync(path, recursive);

    public static Task<List<CyclomaticComplexityMetric>> Cyclomatic(string path) =>
        new CyclomaticComplexityAnalysisService(new FileDiscoveryService(), new RoslynParser())
            .AnalyzeAsync(path);

    public static Task<List<CkMetric>> Ck(string path) =>
        new CkMetricsAnalysisService(new FileDiscoveryService(), new RoslynParser(), new ClassRelationshipBuilder())
            .AnalyzeAsync(path);

    public static Task<List<MisraMetric>> Misra(string path) =>
        new MisraMetricsAnalysisService(new FileDiscoveryService(), new RoslynParser(), new ClassRelationshipBuilder())
            .AnalyzeAsync(path);
}
