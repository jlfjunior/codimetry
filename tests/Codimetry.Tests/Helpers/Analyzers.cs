using Codimetry.Core.Cyclomatic;
using Codimetry.Core.Model;
using Codimetry.Core.Suites;
using Codimetry.Metrics.Ck;
using Codimetry.Metrics.Cyclomatic;
using Codimetry.Metrics.Loc;
using Codimetry.Metrics.Misra;

namespace Codimetry.Tests.Helpers;

/// <summary>
/// Builds each analysis service the same way the CLI does and runs it over a path.
/// </summary>
public static class Analyzers
{
    public static Task<List<LocMetric>> Loc(string path, bool recursive = true, TypeScope? typeScope = null) =>
        new LocAnalysisService(new LocCalculator(), new LogicalLocCalculator(), new PhysicalLocCalculator())
            .AnalyzeAsync(AnalysisContext.Load(path, recursive, typeScope));

    public static Task<List<CyclomaticComplexityMetric>> Cyclomatic(string path, CyclomaticOptions? options = null,
        TypeScope? typeScope = null) =>
        new CyclomaticComplexityAnalysisService()
            .AnalyzeAsync(AnalysisContext.Load(path, typeScope: typeScope, cyclomaticOptions: options));

    public static Task<List<CkMetric>> Ck(string path, WmcWeight wmcWeight = WmcWeight.Unit, bool wmcExcludeStatic = false,
        CyclomaticOptions? cyclomaticOptions = null, TypeScope? typeScope = null, DitOptions? ditOptions = null,
        CboOptions? cboOptions = null, bool rfcExcludeConstructors = false) =>
        new CkMetricsAnalysisService(wmcWeight: wmcWeight, wmcExcludeStatic: wmcExcludeStatic, cboOptions: cboOptions,
                rfcExcludeConstructors: rfcExcludeConstructors)
            .AnalyzeAsync(AnalysisContext.Load(path, typeScope: typeScope, cyclomaticOptions: cyclomaticOptions, ditOptions: ditOptions));

    public static Task<List<MisraMetric>> Misra(string path, bool additiveMc = false, bool acTypeWeights = false,
        TypeScope? typeScope = null, int libraryCallWeight = 0, McLambdaRule lambdaRule = McLambdaRule.Full,
        int cwcLibraryCallWeight = 0, CocFormula cocFormula = CocFormula.Tree, bool cocLegacy = false) =>
        new MisraMetricsAnalysisService(additiveMc: additiveMc, acTypeWeights: acTypeWeights, libraryCallWeight: libraryCallWeight,
                lambdaRule: lambdaRule, cwcLibraryCallWeight: cwcLibraryCallWeight, cocFormula: cocFormula, cocLegacy: cocLegacy)
            .AnalyzeAsync(AnalysisContext.Load(path, typeScope: typeScope));
}
