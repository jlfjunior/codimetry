using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Logging;
using Codimetry.Core.Cyclomatic;
using Codimetry.Core.Model;
using Codimetry.Core.Suites;
using Codimetry.Metrics.Ck;
using Codimetry.Metrics.Cyclomatic;
using Codimetry.Metrics.Loc;
using Codimetry.Metrics.Misra;

namespace Codimetry.CLI.Commands;

/// <summary>
/// Handles the root command: loads the code once and runs the selected metric suites over it.
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

        var classesOnlyOption = new Option<bool>(
            aliases: new[] { "--classes-only" },
            getDefaultValue: () => false,
            description: "Measure only classes, as earlier versions did; by default records, structs and record structs are measured too");

        var ccNullOperatorsOption = new Option<bool>(
            aliases: new[] { "--cc-null-operators" },
            getDefaultValue: () => false,
            description: "Cyclomatic: count ??, ??= and ?. as decision points (also in WMC with --wmc-weight cyclomatic)");

        var ccLegacyOperatorsOption = new Option<bool>(
            aliases: new[] { "--cc-legacy-operators" },
            getDefaultValue: () => false,
            description: "Cyclomatic: legacy operator rule (?? and goto count; ??=, ?. and the pattern combinators or/and do not)");

        var ccIncludeAutoAccessorsOption = new Option<bool>(
            aliases: new[] { "--cc-include-auto-accessors" },
            getDefaultValue: () => false,
            description: "Cyclomatic: give automatic property accessors ({ get; set; }) a row with V(G) = 1 and add them to the class and file sums, as earlier versions did");

        var ditCountObjectOption = new Option<bool>(
            aliases: new[] { "--dit-count-object" },
            getDefaultValue: () => false,
            description: "CK: count System.Object as a level of DIT, as earlier versions did (a class without an explicit base has DIT = 1)");

        var ditResolveFrameworkOption = new Option<bool>(
            aliases: new[] { "--dit-resolve-framework" },
            getDefaultValue: () => false,
            description: "CK: count the ancestors of a base class from a library too (D : Controller = 2), except System.Object");

        // In this order they appear in --help and run
        IMetricSuite[] suites = [new LocSuite(), new CkSuite(), new MisraSuite(), new CyclomaticSuite()];

        var rootCommand = new RootCommand("Codimetry - Extract LOC, CK, MISRA, and Cyclomatic Complexity metrics from .NET codebases")
        {
            pathOption,
            solutionOption,
            projectOption,
            outputOption,
            noRecursiveOption,
            verboseOption,
            classesOnlyOption,
            ccNullOperatorsOption,
            ccLegacyOperatorsOption,
            ccIncludeAutoAccessorsOption,
            ditCountObjectOption,
            ditResolveFrameworkOption
        };

        foreach (var suite in suites)
        {
            rootCommand.AddOption(suite.Selector);
            foreach (var option in suite.Options)
            {
                rootCommand.AddOption(option);
            }
        }

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
            var parseResult = context.ParseResult;
            var cyclomaticOptions = new CyclomaticOptions(
                NullOperators: parseResult.GetValueForOption(ccNullOperatorsOption),
                LegacyOperators: parseResult.GetValueForOption(ccLegacyOperatorsOption),
                IncludeAutoAccessors: parseResult.GetValueForOption(ccIncludeAutoAccessorsOption));
            var ditOptions = new DitOptions(
                CountObject: parseResult.GetValueForOption(ditCountObjectOption),
                ResolveFramework: parseResult.GetValueForOption(ditResolveFrameworkOption));
            var typeScope = new TypeScope(ClassesOnly: parseResult.GetValueForOption(classesOnlyOption));

            context.ExitCode = await ExecuteAsync(
                parseResult,
                suites,
                parseResult.GetValueForOption(pathOption),
                parseResult.GetValueForOption(solutionOption),
                parseResult.GetValueForOption(projectOption),
                parseResult.GetValueForOption(outputOption) ?? "./.metrics",
                parseResult.GetValueForOption(noRecursiveOption),
                parseResult.GetValueForOption(verboseOption),
                cyclomaticOptions,
                ditOptions,
                typeScope);
        });

        return rootCommand;
    }

    private static async Task<int> ExecuteAsync(ParseResult parseResult, IReadOnlyList<IMetricSuite> suites, string? path, string? solution, string? project, string output, bool noRecursive, bool verbose, CyclomaticOptions cyclomaticOptions, DitOptions ditOptions, TypeScope typeScope)
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

            logger.LogInformation("Starting Codimetry analysis...");
            logger.LogInformation("Input type: {InputType}", inputType);
            logger.LogInformation("Source path: {Path}", inputPath);
            logger.LogInformation("Output directory: {Output}", output);
            if (path != null)
            {
                logger.LogInformation("Recursive: {Recursive}", !noRecursive);
            }
            logger.LogInformation("Cyclomatic operators: {Rule}",
                cyclomaticOptions.LegacyOperators ? "legacy (?? and goto count)"
                : cyclomaticOptions.NullOperators ? "null operators (??, ??= and ?. count)"
                : "default (??, ??=, ?. and goto do not count)");
            logger.LogInformation("Measured types: {Scope}", typeScope.ClassesOnly
                ? "classes only, legacy (--classes-only)"
                : "classes, records, structs and record structs");

            // If no suite flag is specified, every suite runs; otherwise only the selected ones
            var selected = suites.Where(s => parseResult.GetValueForOption(s.Selector)).ToList();
            if (selected.Count == 0)
            {
                selected = suites.ToList();
            }

            // The code is loaded once and shared by the suites
            var analysis = AnalysisContext.Load(inputPath, !noRecursive, typeScope, cyclomaticOptions, ditOptions,
                loggerFactory.CreateLogger<AnalysisContext>());

            foreach (var suite in selected)
            {
                await suite.RunAsync(analysis, parseResult, output, loggerFactory);
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

