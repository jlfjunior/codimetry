using System.CommandLine;
using System.CommandLine.Parsing;
using Codimetry.Core.Suites;
using Microsoft.Extensions.Logging;

namespace Codimetry.Metrics.Cyclomatic;

/// <summary>
/// McCabe's V(G) per member, class and file (metrics_cyclomatic.csv). Its rules (--cc-*) are shared with WMC
/// and come with the <see cref="AnalysisContext"/>.
/// </summary>
public sealed class CyclomaticSuite : IMetricSuite
{
    public Option<bool> Selector { get; } = new(
        aliases: new[] { "--cyclomatic", "--cc" },
        getDefaultValue: () => false,
        description: "Calculate and export Cyclomatic Complexity metrics");

    public IReadOnlyList<Option> Options => [];

    public async Task RunAsync(AnalysisContext context, ParseResult parseResult, string outputDirectory, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<CyclomaticSuite>();
        logger.LogInformation("Calculating Cyclomatic Complexity metrics...");

        var ccAnalysisService = new CyclomaticComplexityAnalysisService(
            loggerFactory.CreateLogger<CyclomaticComplexityAnalysisService>());

        var ccMetricsResults = await ccAnalysisService.AnalyzeAsync(context);

        if (ccMetricsResults.Count == 0)
        {
            logger.LogWarning("No Cyclomatic Complexity metrics found.");
            return;
        }

        var ccOutputPath = Path.Combine(outputDirectory, "metrics_cyclomatic.csv");
        await CyclomaticCsvExporter.ExportAsync(ccMetricsResults, ccOutputPath);

        logger.LogInformation("Cyclomatic Complexity metrics exported successfully to: {OutputPath}", ccOutputPath);
        logger.LogInformation("Total Cyclomatic Complexity entries: {Count}", ccMetricsResults.Count);

        // Log complexity statistics
        var methodMetrics = ccMetricsResults.Where(m => !string.IsNullOrEmpty(m.MethodName)).ToList();
        if (methodMetrics.Any())
        {
            var maxComplexity = methodMetrics.Max(m => m.Complexity);
            var avgComplexity = methodMetrics.Average(m => m.Complexity);
            var highComplexity = methodMetrics.Count(m => m.Complexity > 10);
            var veryHighComplexity = methodMetrics.Count(m => m.Complexity > 20);

            logger.LogInformation("Complexity Statistics:");
            logger.LogInformation("  Maximum complexity: {Max}", maxComplexity);
            logger.LogInformation("  Average complexity: {Avg:F2}", avgComplexity);
            logger.LogInformation("  Methods with complexity > 10: {High}", highComplexity);
            logger.LogInformation("  Methods with complexity > 20: {VeryHigh}", veryHighComplexity);
        }
    }
}
