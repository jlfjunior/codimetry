using System.CommandLine;
using System.CommandLine.Parsing;
using Codimetry.Core.Suites;
using Microsoft.Extensions.Logging;

namespace Codimetry.Metrics.Misra;

/// <summary>
/// Misra's cognitive complexity metrics: MC per method, AC, CLC and CWC per class, CoC of the codebase
/// (metrics_misra.csv) and of each inheritance hierarchy (metrics_coc.csv).
/// </summary>
public sealed class MisraSuite : IMetricSuite
{
    public Option<bool> Selector { get; } = new(
        aliases: new[] { "--misra" },
        getDefaultValue: () => false,
        description: "Calculate and export MISRA (Misra's Cognitive Complexity) metrics");

    private readonly Option<bool> _mcAdditive = new(
        aliases: new[] { "--mc-additive" },
        getDefaultValue: () => false,
        description: "MISRA: compute MC with the legacy additive rule (weights summed at any depth) instead of the composition rule of Eq. 23");

    private readonly Option<bool> _acTypeWeights = new(
        aliases: new[] { "--ac-type-weights" },
        getDefaultValue: () => false,
        description: "MISRA: compute AC as earlier versions did, weighting attributes by type (simple = 1, other = 2) and counting static and const members");

    private readonly Option<int> _mcLibraryCallWeight = new(
        aliases: new[] { "--mc-library-call-weight" },
        getDefaultValue: () => 0,
        description: "MISRA: weight of a call to a library method in MC (2 reproduces earlier versions for these calls); calls to the analysed code add 2 + MC of the called method");

    private readonly Option<McLambdaRule> _mcLambdas = new(
        aliases: new[] { "--mc-lambdas" },
        getDefaultValue: () => McLambdaRule.Full,
        description: "MISRA: code of lambdas and local functions in MC: full (a nested layer), calls (only their calls, as before), ignore");

    private readonly Option<int> _cwcLibraryWeight = new(
        aliases: new[] { "--cwc-library-weight" },
        getDefaultValue: () => 0,
        description: "MISRA: weight of a call to a library method or delegate in CWC (3 reproduces the legacy library rule); calls to other types of the analysed code add 2 + MC");

    private readonly Option<CocFormula> _cocFormula = new(
        aliases: new[] { "--coc-formula" },
        getDefaultValue: () => CocFormula.Tree,
        description: "MISRA: how CoC combines the CLC of an inheritance hierarchy: tree (CLC of a class × Σ CoC of its subclasses, as in the example of Misra et al.) or levels (Π over levels of Σ CLC, Eq. 27 read literally)");

    private readonly Option<bool> _cocLegacy = new(
        aliases: new[] { "--coc-legacy" },
        getDefaultValue: () => false,
        description: "MISRA: compute CoC as earlier versions did, Σ CLC + Σ CWC / 2, without the inheritance hierarchy");

    public IReadOnlyList<Option> Options =>
        [_mcAdditive, _acTypeWeights, _mcLibraryCallWeight, _mcLambdas, _cwcLibraryWeight, _cocFormula, _cocLegacy];

    public async Task RunAsync(AnalysisContext context, ParseResult parseResult, string outputDirectory, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<MisraSuite>();
        logger.LogInformation("Calculating MISRA metrics...");

        var misraAnalysisService = new MisraMetricsAnalysisService(
            loggerFactory.CreateLogger<MisraMetricsAnalysisService>(),
            additiveMc: parseResult.GetValueForOption(_mcAdditive),
            acTypeWeights: parseResult.GetValueForOption(_acTypeWeights),
            libraryCallWeight: parseResult.GetValueForOption(_mcLibraryCallWeight),
            lambdaRule: parseResult.GetValueForOption(_mcLambdas),
            cwcLibraryCallWeight: parseResult.GetValueForOption(_cwcLibraryWeight),
            cocFormula: parseResult.GetValueForOption(_cocFormula),
            cocLegacy: parseResult.GetValueForOption(_cocLegacy));

        var misraMetricsResults = await misraAnalysisService.AnalyzeAsync(context);

        if (misraMetricsResults.Count == 0)
        {
            logger.LogWarning("No MISRA metrics found.");
            return;
        }

        var misraOutputPath = Path.Combine(outputDirectory, "metrics_misra.csv");
        await MisraCsvExporter.ExportAsync(misraMetricsResults, misraOutputPath);

        logger.LogInformation("MISRA metrics exported successfully to: {OutputPath}", misraOutputPath);
        logger.LogInformation("Total MISRA entries: {Count}", misraMetricsResults.Count);

        var cocOutputPath = Path.Combine(outputDirectory, "metrics_coc.csv");
        await MisraCsvExporter.ExportHierarchiesAsync(misraAnalysisService.Hierarchies, cocOutputPath);
        logger.LogInformation("CoC per inheritance hierarchy exported to: {OutputPath}", cocOutputPath);
    }
}
