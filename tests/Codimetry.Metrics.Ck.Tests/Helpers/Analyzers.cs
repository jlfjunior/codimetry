using Codimetry.Core.Cyclomatic;
using Codimetry.Core.Model;
using Codimetry.Core.Suites;
using Codimetry.Metrics.Ck;

namespace Codimetry.Tests.Helpers;

/// <summary>
/// Builds the analysis service the same way the CLI does and runs it over a path.
/// </summary>
public static class Analyzers
{
    public static Task<List<CkMetric>> Ck(string path, WmcWeight wmcWeight = WmcWeight.Unit, bool wmcExcludeStatic = false,
        CyclomaticOptions? cyclomaticOptions = null, TypeScope? typeScope = null, DitOptions? ditOptions = null,
        CboOptions? cboOptions = null, bool rfcExcludeConstructors = false) =>
        new CkMetricsAnalysisService(wmcWeight: wmcWeight, wmcExcludeStatic: wmcExcludeStatic, cboOptions: cboOptions,
                rfcExcludeConstructors: rfcExcludeConstructors)
            .AnalyzeAsync(AnalysisContext.Load(path, typeScope: typeScope, cyclomaticOptions: cyclomaticOptions, ditOptions: ditOptions));
}
