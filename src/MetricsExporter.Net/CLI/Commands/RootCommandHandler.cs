using System.CommandLine;
using System.CommandLine.Invocation;
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
            getDefaultValue: () => "./.metrics",
            description: "Output directory for the CSV file (default: ./.metrics)");

        var noRecursiveOption = new Option<bool>(
            aliases: new[] { "--no-recursive" },
            getDefaultValue: () => false,
            description: "Disable recursive directory scanning (only applies to --path)");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            getDefaultValue: () => false,
            description: "Enable verbose logging");

        var locOption = new Option<bool>(
            aliases: new[] { "--loc" },
            getDefaultValue: () => false,
            description: "Calculate and export LOC (Lines of Code) metrics");

        var ckOption = new Option<bool>(
            aliases: new[] { "--ck" },
            getDefaultValue: () => false,
            description: "Calculate and export CK (Chidamber-Kemerer) object-oriented design metrics");

        var misraOption = new Option<bool>(
            aliases: new[] { "--misra" },
            getDefaultValue: () => false,
            description: "Calculate and export MISRA (Misra's Cognitive Complexity) metrics");

        var cyclomaticOption = new Option<bool>(
            aliases: new[] { "--cyclomatic", "--cc" },
            getDefaultValue: () => false,
            description: "Calculate and export Cyclomatic Complexity metrics");

        var mcAdditiveOption = new Option<bool>(
            aliases: new[] { "--mc-additive" },
            getDefaultValue: () => false,
            description: "MISRA: compute MC with the additive rule of v1 (weights summed at any depth) instead of the composition rule of Eq. 23");

        var acTypeWeightsOption = new Option<bool>(
            aliases: new[] { "--ac-type-weights" },
            getDefaultValue: () => false,
            description: "MISRA: compute AC as v1 did, weighting attributes by type (simple = 1, other = 2) and counting static and const members");

        var wmcWeightOption = new Option<WmcWeight>(
            aliases: new[] { "--wmc-weight" },
            getDefaultValue: () => WmcWeight.Unit,
            description: "CK: weight of each method in WMC, unit (c_i = 1) or cyclomatic (c_i = V(G))");

        var wmcExcludeStaticOption = new Option<bool>(
            aliases: new[] { "--wmc-exclude-static" },
            getDefaultValue: () => false,
            description: "CK: leave static methods and constructors out of WMC, as v1 did");

        var ccNullOperatorsOption = new Option<bool>(
            aliases: new[] { "--cc-null-operators" },
            getDefaultValue: () => false,
            description: "Cyclomatic: count ??, ??= and ?. as decision points (also in WMC with --wmc-weight cyclomatic)");

        var ccLegacyOperatorsOption = new Option<bool>(
            aliases: new[] { "--cc-legacy-operators" },
            getDefaultValue: () => false,
            description: "Cyclomatic: operator rule of v1 (?? and goto count; ??=, ?. and the pattern combinators or/and do not)");

        var ccIncludeAutoAccessorsOption = new Option<bool>(
            aliases: new[] { "--cc-include-auto-accessors" },
            getDefaultValue: () => false,
            description: "Cyclomatic: give automatic property accessors ({ get; set; }) a row with V(G) = 1 and add them to the class and file sums, as v1 did");

        var mcLibraryCallWeightOption = new Option<int>(
            aliases: new[] { "--mc-library-call-weight" },
            getDefaultValue: () => 0,
            description: "MISRA: weight of a call to a library method in MC (2 reproduces v1 for these calls); calls to the analysed code add 2 + MC of the called method");

        var mcLambdasOption = new Option<Parsers.McLambdaRule>(
            aliases: new[] { "--mc-lambdas" },
            getDefaultValue: () => Parsers.McLambdaRule.Full,
            description: "MISRA: code of lambdas and local functions in MC: full (a nested layer), calls (only their calls, as before), ignore");

        var cwcLibraryWeightOption = new Option<int>(
            aliases: new[] { "--cwc-library-weight" },
            getDefaultValue: () => 0,
            description: "MISRA: weight of a call to a library method or delegate in CWC (3 reproduces the library rule of v1); calls to other types of the analysed code add 2 + MC");

        var ditCountObjectOption = new Option<bool>(
            aliases: new[] { "--dit-count-object" },
            getDefaultValue: () => false,
            description: "CK: count System.Object as a level of DIT, as v1 did (a class without an explicit base has DIT = 1)");

        var ditResolveFrameworkOption = new Option<bool>(
            aliases: new[] { "--dit-resolve-framework" },
            getDefaultValue: () => false,
            description: "CK: count the ancestors of a base class from a library too (D : Controller = 2), except System.Object");

        var cboDeclaredTypesOnlyOption = new Option<bool>(
            aliases: new[] { "--cbo-declared-types-only" },
            getDefaultValue: () => false,
            description: "CK: CBO counts only the types of fields, properties and method signatures, and only classes, as v1 did (types still matched by symbol)");

        var cboBidirectionalOption = new Option<bool>(
            aliases: new[] { "--cbo-bidirectional" },
            getDefaultValue: () => false,
            description: "CK: CBO also counts the types that use the class (CK 1994), each coupled type once");

        var classesOnlyOption = new Option<bool>(
            aliases: new[] { "--classes-only" },
            getDefaultValue: () => false,
            description: "Measure only classes, as v1 did; by default records, structs and record structs are measured too");

        var rootCommand = new RootCommand("MetricsExporter.Net - Extract LOC, CK, MISRA, and Cyclomatic Complexity metrics from .NET codebases")
        {
            pathOption,
            solutionOption,
            projectOption,
            outputOption,
            noRecursiveOption,
            verboseOption,
            locOption,
            ckOption,
            misraOption,
            cyclomaticOption,
            mcAdditiveOption,
            acTypeWeightsOption,
            wmcWeightOption,
            wmcExcludeStaticOption,
            ccNullOperatorsOption,
            ccLegacyOperatorsOption,
            classesOnlyOption,
            ccIncludeAutoAccessorsOption,
            mcLibraryCallWeightOption,
            mcLambdasOption,
            cwcLibraryWeightOption,
            ditCountObjectOption,
            ditResolveFrameworkOption,
            cboDeclaredTypesOnlyOption,
            cboBidirectionalOption
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
            else if (result.GetValueForOption(ccNullOperatorsOption) && result.GetValueForOption(ccLegacyOperatorsOption))
            {
                result.ErrorMessage = "Error: --cc-null-operators and --cc-legacy-operators cannot be used together.";
            }
        });

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForOption(pathOption);
            var solution = context.ParseResult.GetValueForOption(solutionOption);
            var project = context.ParseResult.GetValueForOption(projectOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var recursive = context.ParseResult.GetValueForOption(noRecursiveOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var loc = context.ParseResult.GetValueForOption(locOption);
            var ck = context.ParseResult.GetValueForOption(ckOption);
            var misra = context.ParseResult.GetValueForOption(misraOption);
            var cyclomatic = context.ParseResult.GetValueForOption(cyclomaticOption);
            var mcAdditive = context.ParseResult.GetValueForOption(mcAdditiveOption);
            var acTypeWeights = context.ParseResult.GetValueForOption(acTypeWeightsOption);
            var wmcWeight = context.ParseResult.GetValueForOption(wmcWeightOption);
            var wmcExcludeStatic = context.ParseResult.GetValueForOption(wmcExcludeStaticOption);
            var cyclomaticOptions = new Parsers.CyclomaticOptions(
                NullOperators: context.ParseResult.GetValueForOption(ccNullOperatorsOption),
                LegacyOperators: context.ParseResult.GetValueForOption(ccLegacyOperatorsOption),
                IncludeAutoAccessors: context.ParseResult.GetValueForOption(ccIncludeAutoAccessorsOption));
            var mcLibraryCallWeight = context.ParseResult.GetValueForOption(mcLibraryCallWeightOption);
            var mcLambdas = context.ParseResult.GetValueForOption(mcLambdasOption);
            var cwcLibraryWeight = context.ParseResult.GetValueForOption(cwcLibraryWeightOption);
            var ditOptions = new Parsers.DitOptions(
                CountObject: context.ParseResult.GetValueForOption(ditCountObjectOption),
                ResolveFramework: context.ParseResult.GetValueForOption(ditResolveFrameworkOption));
            var cboOptions = new Parsers.CboOptions(
                DeclaredTypesOnly: context.ParseResult.GetValueForOption(cboDeclaredTypesOnlyOption),
                Bidirectional: context.ParseResult.GetValueForOption(cboBidirectionalOption));
            var typeScope = new Parsers.TypeScope(ClassesOnly: context.ParseResult.GetValueForOption(classesOnlyOption));
            
            context.ExitCode = await ExecuteAsync(path, solution, project, output ?? "./.metrics", recursive, verbose, loc, ck, misra, cyclomatic, mcAdditive, acTypeWeights, wmcWeight, wmcExcludeStatic, cyclomaticOptions, typeScope, mcLibraryCallWeight, mcLambdas, cwcLibraryWeight, ditOptions, cboOptions);
        });

        return rootCommand;
    }

    private static async Task<int> ExecuteAsync(string? path, string? solution, string? project, string output, bool noRecursive, bool verbose, bool loc, bool ck, bool misra, bool cyclomatic, bool mcAdditive, bool acTypeWeights, WmcWeight wmcWeight, bool wmcExcludeStatic, Parsers.CyclomaticOptions cyclomaticOptions, Parsers.TypeScope typeScope, int mcLibraryCallWeight, Parsers.McLambdaRule mcLambdas, int cwcLibraryWeight, Parsers.DitOptions ditOptions, Parsers.CboOptions cboOptions)
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
            logger.LogInformation("Cyclomatic operators: {Rule}",
                cyclomaticOptions.LegacyOperators ? "legacy (v1: ?? and goto count)"
                : cyclomaticOptions.NullOperators ? "null operators (??, ??= and ?. count)"
                : "default (??, ??=, ?. and goto do not count)");
            logger.LogInformation("Measured types: {Scope}", typeScope.ClassesOnly
                ? "classes only, v1 (--classes-only)"
                : "classes, records, structs and record structs");

            // Determine which metrics to calculate
            // If no metric flags are specified, calculate all (default behavior)
            // If specific flags are specified, calculate only those
            var hasAnyFlag = loc || ck || misra || cyclomatic;
            
            var calculateLoc = !hasAnyFlag || loc; // Calculate LOC if no flags (default) or if --loc is specified
            var calculateCk = !hasAnyFlag || ck; // Calculate CK if no flags (default) or if --ck is specified
            var calculateMisra = !hasAnyFlag || misra; // Calculate MISRA if no flags (default) or if --misra is specified
            var calculateCyclomatic = !hasAnyFlag || cyclomatic; // Calculate Cyclomatic Complexity if no flags (default) or if --cyclomatic/--cc is specified

            // Initialize services
            var fileDiscovery = new FileDiscoveryService();
            var roslynParser = new Parsers.RoslynParser();
            var csvExporter = new CsvExporter();

            // Analyze and export LOC metrics if requested
            if (calculateLoc)
            {
                logger.LogInformation("Calculating LOC metrics...");
                
                var locCalculator = new LocCalculator();
                var analysisService = new MetricsAnalysisService(
                    fileDiscovery,
                    roslynParser,
                    locCalculator,
                    new LogicalLocCalculator(),
                    new PhysicalLocCalculator(),
                    loggerFactory.CreateLogger<MetricsAnalysisService>(),
                    typeScope);

                var metrics = await analysisService.AnalyzeAsync(inputPath, !noRecursive);

                if (metrics.Count == 0)
                {
                    logger.LogWarning("No LOC metrics found. Check if the path contains valid .cs files.");
                }
                else
                {
                    logger.LogInformation("LOC analysis complete. Found {Count} metric entries.", metrics.Count);

                    // Export LOC metrics to CSV
                    var outputPath = Path.Combine(output, "metrics_loc.csv");
                    await csvExporter.ExportAsync(metrics, outputPath);

                    logger.LogInformation("LOC metrics exported successfully to: {OutputPath}", outputPath);
                    logger.LogInformation("Total LOC entries: {Count}", metrics.Count);
                }
            }

            // Calculate and export CK metrics if requested
            if (calculateCk)
            {
                logger.LogInformation("Calculating CK metrics...");
                
                var relationshipBuilder = new ClassRelationshipBuilder();
                var ckAnalysisService = new CkMetricsAnalysisService(
                    fileDiscovery,
                    roslynParser,
                    relationshipBuilder,
                    loggerFactory.CreateLogger<CkMetricsAnalysisService>(),
                    wmcWeight: wmcWeight,
                    wmcExcludeStatic: wmcExcludeStatic,
                    cyclomaticOptions: cyclomaticOptions,
                    typeScope: typeScope,
                    ditOptions: ditOptions,
                    cboOptions: cboOptions);

                var ckMetricsResults = await ckAnalysisService.AnalyzeAsync(inputPath, !noRecursive);

                if (ckMetricsResults.Count > 0)
                {
                    var ckOutputPath = Path.Combine(output, "metrics_ck.csv");
                    await csvExporter.ExportCkMetricsAsync(ckMetricsResults, ckOutputPath);

                    logger.LogInformation("CK metrics exported successfully to: {OutputPath}", ckOutputPath);
                    logger.LogInformation("Total CK entries: {Count}", ckMetricsResults.Count);
                }
                else
                {
                    logger.LogWarning("No CK metrics found.");
                }
            }

            // Calculate and export MISRA metrics if requested
            if (calculateMisra)
            {
                logger.LogInformation("Calculating MISRA metrics...");
                
                var relationshipBuilder = new ClassRelationshipBuilder();
                var misraAnalysisService = new MisraMetricsAnalysisService(
                    fileDiscovery,
                    roslynParser,
                    relationshipBuilder,
                    loggerFactory.CreateLogger<MisraMetricsAnalysisService>(),
                    additiveMc: mcAdditive,
                    acTypeWeights: acTypeWeights,
                    typeScope: typeScope,
                    libraryCallWeight: mcLibraryCallWeight,
                    lambdaRule: mcLambdas,
                    cwcLibraryCallWeight: cwcLibraryWeight,
                    ditOptions: ditOptions);

                var misraMetricsResults = await misraAnalysisService.AnalyzeAsync(inputPath, !noRecursive);

                if (misraMetricsResults.Count > 0)
                {
                    var misraOutputPath = Path.Combine(output, "metrics_misra.csv");
                    await csvExporter.ExportMisraMetricsAsync(misraMetricsResults, misraOutputPath);

                    logger.LogInformation("MISRA metrics exported successfully to: {OutputPath}", misraOutputPath);
                    logger.LogInformation("Total MISRA entries: {Count}", misraMetricsResults.Count);
                }
                else
                {
                    logger.LogWarning("No MISRA metrics found.");
                }
            }

            // Calculate and export Cyclomatic Complexity metrics if requested
            if (calculateCyclomatic)
            {
                logger.LogInformation("Calculating Cyclomatic Complexity metrics...");
                
                var ccAnalysisService = new CyclomaticComplexityAnalysisService(
                    fileDiscovery,
                    roslynParser,
                    loggerFactory.CreateLogger<CyclomaticComplexityAnalysisService>(),
                    cyclomaticOptions,
                    typeScope);

                var ccMetricsResults = await ccAnalysisService.AnalyzeAsync(inputPath, !noRecursive);

                if (ccMetricsResults.Count > 0)
                {
                    var ccOutputPath = Path.Combine(output, "metrics_cyclomatic.csv");
                    await csvExporter.ExportCyclomaticComplexityAsync(ccMetricsResults, ccOutputPath);

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
                else
                {
                    logger.LogWarning("No Cyclomatic Complexity metrics found.");
                }
            }

            return 0;
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError("Directory not found: {Path}", ex.Message);
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError("Access denied: {Message}", ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during analysis");
            return 1;
        }
    }
}

