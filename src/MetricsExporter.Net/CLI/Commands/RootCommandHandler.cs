using System.CommandLine;
using Microsoft.Extensions.Logging;
using MetricsExporter.Net.Exporters;
using MetricsExporter.Net.Services;

namespace MetricsExporter.Net.CLI.Commands;

/// <summary>
/// Handles the root command and orchestrates the metrics export process.
/// </summary>
public class RootCommandHandler
{
    public static RootCommand CreateCommand()
    {
        var pathOption = new Option<string>(
            aliases: new[] { "--path", "-p" },
            description: "Path to a directory containing .NET source code to analyze");

        var solutionOption = new Option<string>(
            aliases: new[] { "--solution", "-s" },
            description: "Path to a .sln solution file to analyze");

        var projectOption = new Option<string>(
            aliases: new[] { "--project" },
            description: "Path to a .csproj project file to analyze");

        var outputOption = new Option<string>(
            aliases: new[] { "--out", "-o" },
            getDefaultValue: () => "./metrics",
            description: "Output directory for the CSV file (default: ./metrics)");

        var noRecursiveOption = new Option<bool>(
            aliases: new[] { "--no-recursive" },
            getDefaultValue: () => false,
            description: "Disable recursive directory scanning (only applies to --path)");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            getDefaultValue: () => false,
            description: "Enable verbose logging");

        var rootCommand = new RootCommand("MetricsExporter.Net - Extract LOC metrics from .NET codebases")
        {
            pathOption,
            solutionOption,
            projectOption,
            outputOption,
            noRecursiveOption,
            verboseOption
        };

        rootCommand.AddValidator(result =>
        {
            var path = result.GetValueForOption(pathOption);
            var solution = result.GetValueForOption(solutionOption);
            var project = result.GetValueForOption(projectOption);

            var count = (path != null ? 1 : 0) + (solution != null ? 1 : 0) + (project != null ? 1 : 0);
            
            if (count == 0)
            {
                result.ErrorMessage = "Error: One of --path, --solution, or --project must be specified. Use --help for usage information.";
            }
            else if (count > 1)
            {
                result.ErrorMessage = "Error: Only one of --path, --solution, or --project can be specified at a time.";
            }
        });

        rootCommand.SetHandler(async (path, solution, project, output, recursive, verbose) =>
        {
            await ExecuteAsync(path, solution, project, output, recursive, verbose);
        }, pathOption, solutionOption, projectOption, outputOption, noRecursiveOption, verboseOption);

        return rootCommand;
    }

    private static async Task ExecuteAsync(string? path, string? solution, string? project, string output, bool noRecursive, bool verbose)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            if (verbose)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(LogLevel.Information);
            }
        });

        var logger = loggerFactory.CreateLogger<RootCommandHandler>();

        try
        {
            // Determine the input path (validation is done at command level)
            string inputPath;
            string inputType;
            
            if (solution != null)
            {
                inputPath = solution;
                inputType = "solution file";
            }
            else if (project != null)
            {
                inputPath = project;
                inputType = "project file";
            }
            else
            {
                inputPath = path!;
                inputType = "directory";
            }

            logger.LogInformation("Starting MetricsExporter.Net analysis...");
            logger.LogInformation("Input type: {InputType}", inputType);
            logger.LogInformation("Source path: {Path}", inputPath);
            logger.LogInformation("Output directory: {Output}", output);
            if (path != null)
            {
                logger.LogInformation("Recursive: {Recursive}", !noRecursive);
            }

            // Initialize services
            var fileDiscovery = new FileDiscoveryService();
            var roslynParser = new Parsers.RoslynParser();
            var locCalculator = new LocCalculator();
            var csvExporter = new CsvExporter();

            var analysisService = new MetricsAnalysisService(
                fileDiscovery,
                roslynParser,
                locCalculator,
                loggerFactory.CreateLogger<MetricsAnalysisService>());

            // Analyze code
            var metrics = await analysisService.AnalyzeAsync(inputPath, !noRecursive);

            if (metrics.Count == 0)
            {
                logger.LogWarning("No metrics found. Check if the path contains valid .cs files.");
                return;
            }

            logger.LogInformation("Analysis complete. Found {Count} metric entries.", metrics.Count);

            // Export to CSV
            var outputPath = Path.Combine(output, "metrics_loc.csv");
            await csvExporter.ExportAsync(metrics, outputPath);

            logger.LogInformation("Metrics exported successfully to: {OutputPath}", outputPath);
            logger.LogInformation("Total entries: {Count}", metrics.Count);
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError("Directory not found: {Path}", ex.Message);
            Environment.Exit(1);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError("Access denied: {Message}", ex.Message);
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during analysis");
            Environment.Exit(1);
        }
    }
}

