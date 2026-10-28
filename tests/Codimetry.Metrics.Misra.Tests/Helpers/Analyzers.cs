using Codimetry.Core.Model;
using Codimetry.Core.Suites;
using Codimetry.Metrics.Misra;

namespace Codimetry.Tests.Helpers;

/// <summary>
/// Builds the analysis service the same way the CLI does and runs it over a path.
/// </summary>
public static class Analyzers
{
    public static Task<List<MisraMetric>> Misra(string path, bool additiveMc = false, bool acTypeWeights = false,
        TypeScope? typeScope = null, int libraryCallWeight = 0, McLambdaRule lambdaRule = McLambdaRule.Full,
        int cwcLibraryCallWeight = 0, CocFormula cocFormula = CocFormula.Tree, bool cocLegacy = false) =>
        new MisraMetricsAnalysisService(additiveMc: additiveMc, acTypeWeights: acTypeWeights, libraryCallWeight: libraryCallWeight,
                lambdaRule: lambdaRule, cwcLibraryCallWeight: cwcLibraryCallWeight, cocFormula: cocFormula, cocLegacy: cocLegacy)
            .AnalyzeAsync(AnalysisContext.Load(path, typeScope: typeScope));
}
