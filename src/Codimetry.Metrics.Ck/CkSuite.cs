using System.CommandLine;
using System.CommandLine.Parsing;
using Codimetry.Core.Suites;
using Microsoft.Extensions.Logging;

namespace Codimetry.Metrics.Ck;

/// <summary>
/// The CK metrics WMC, DIT, NOC, CBO, RFC and LCOM per class (metrics_ck.csv). The DIT rules (--dit-*) are
/// shared with the hierarchies of MISRA and come with the <see cref="AnalysisContext"/>.
/// </summary>
public sealed class CkSuite : IMetricSuite
{
    public Option<bool> Selector { get; } = new(
        aliases: new[] { "--ck" },
        getDefaultValue: () => false,
        description: "Calculate and export CK (Chidamber-Kemerer) object-oriented design metrics");

    private readonly Option<WmcWeight> _wmcWeight = new(
        aliases: new[] { "--wmc-weight" },
        getDefaultValue: () => WmcWeight.Unit,
        description: "CK: weight of each method in WMC, unit (c_i = 1) or cyclomatic (c_i = V(G))");

    private readonly Option<bool> _wmcExcludeStatic = new(
        aliases: new[] { "--wmc-exclude-static" },
        getDefaultValue: () => false,
        description: "CK: leave static methods and constructors out of WMC, as earlier versions did");

    private readonly Option<bool> _cboDeclaredTypesOnly = new(
        aliases: new[] { "--cbo-declared-types-only" },
        getDefaultValue: () => false,
        description: "CK: CBO counts only the types of fields, properties and method signatures, and only classes, as earlier versions did (types still matched by symbol)");

    private readonly Option<bool> _cboBidirectional = new(
        aliases: new[] { "--cbo-bidirectional" },
        getDefaultValue: () => false,
        description: "CK: CBO also counts the types that use the class (CK 1994), each coupled type once");

    private readonly Option<bool> _rfcExcludeConstructors = new(
        aliases: new[] { "--rfc-exclude-constructors" },
        getDefaultValue: () => false,
        description: "CK: leave constructor calls (new, base(...)) out of RFC, as earlier versions did");

    public IReadOnlyList<Option> Options =>
        [_wmcWeight, _wmcExcludeStatic, _cboDeclaredTypesOnly, _cboBidirectional, _rfcExcludeConstructors];

    public async Task RunAsync(AnalysisContext context, ParseResult parseResult, string outputDirectory, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<CkSuite>();
        logger.LogInformation("Calculating CK metrics...");

        var ckAnalysisService = new CkMetricsAnalysisService(
            loggerFactory.CreateLogger<CkMetricsAnalysisService>(),
            wmcWeight: parseResult.GetValueForOption(_wmcWeight),
            wmcExcludeStatic: parseResult.GetValueForOption(_wmcExcludeStatic),
            cboOptions: new CboOptions(
                DeclaredTypesOnly: parseResult.GetValueForOption(_cboDeclaredTypesOnly),
                Bidirectional: parseResult.GetValueForOption(_cboBidirectional)),
            rfcExcludeConstructors: parseResult.GetValueForOption(_rfcExcludeConstructors));

        var ckMetricsResults = await ckAnalysisService.AnalyzeAsync(context);

        if (ckMetricsResults.Count == 0)
        {
            logger.LogWarning("No CK metrics found.");
            return;
        }

        var ckOutputPath = Path.Combine(outputDirectory, "metrics_ck.csv");
        await CkCsvExporter.ExportAsync(ckMetricsResults, ckOutputPath);

        logger.LogInformation("CK metrics exported successfully to: {OutputPath}", ckOutputPath);
        logger.LogInformation("Total CK entries: {Count}", ckMetricsResults.Count);
    }
}
