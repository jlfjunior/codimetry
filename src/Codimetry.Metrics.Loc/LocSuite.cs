using System.CommandLine;
using System.CommandLine.Parsing;
using Codimetry.Core.Suites;
using Microsoft.Extensions.Logging;

namespace Codimetry.Metrics.Loc;

/// <summary>
/// LOC, LLOC and physical LOC per method, class and file (metrics_loc.csv).
/// </summary>
public sealed class LocSuite : IMetricSuite
{
    public Option<bool> Selector { get; } = new(
        aliases: new[] { "--loc" },
        getDefaultValue: () => false,
        description: "Calculate and export LOC (Lines of Code) metrics");

    public IReadOnlyList<Option> Options => [];

    public async Task RunAsync(AnalysisContext context, ParseResult parseResult, string outputDirectory, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<LocSuite>();
        logger.LogInformation("Calculating LOC metrics...");

        var analysisService = new LocAnalysisService(
            new LocCalculator(),
            new LogicalLocCalculator(),
            new PhysicalLocCalculator(),
            loggerFactory.CreateLogger<LocAnalysisService>());

        var metrics = await analysisService.AnalyzeAsync(context);

        if (metrics.Count == 0)
        {
            logger.LogWarning("No LOC metrics found. Check if the path contains valid .cs files.");
            return;
        }

        logger.LogInformation("LOC analysis complete. Found {Count} metric entries.", metrics.Count);

        var outputPath = Path.Combine(outputDirectory, "metrics_loc.csv");
        await LocCsvExporter.ExportAsync(metrics, outputPath);

        logger.LogInformation("LOC metrics exported successfully to: {OutputPath}", outputPath);
        logger.LogInformation("Total LOC entries: {Count}", metrics.Count);
    }
}
