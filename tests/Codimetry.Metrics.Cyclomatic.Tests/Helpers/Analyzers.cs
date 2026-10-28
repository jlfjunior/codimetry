using Codimetry.Core.Cyclomatic;
using Codimetry.Core.Model;
using Codimetry.Core.Suites;
using Codimetry.Metrics.Cyclomatic;

namespace Codimetry.Tests.Helpers;

/// <summary>
/// Builds the analysis service the same way the CLI does and runs it over a path.
/// </summary>
public static class Analyzers
{
    public static Task<List<CyclomaticComplexityMetric>> Cyclomatic(string path, CyclomaticOptions? options = null,
        TypeScope? typeScope = null) =>
        new CyclomaticComplexityAnalysisService()
            .AnalyzeAsync(AnalysisContext.Load(path, typeScope: typeScope, cyclomaticOptions: options));
}
