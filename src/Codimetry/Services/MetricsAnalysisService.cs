using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Codimetry.Core.Models;
using Codimetry.Parsers;

namespace Codimetry.Services;

/// <summary>
/// Service that orchestrates the analysis of source code and calculation of metrics.
/// </summary>
public class MetricsAnalysisService
{
    private readonly FileDiscoveryService _fileDiscoveryService;
    private readonly RoslynParser _roslynParser;
    private readonly LocCalculator _locCalculator;
    private readonly LogicalLocCalculator _logicalLocCalculator;
    private readonly PhysicalLocCalculator _physicalLocCalculator;
    private readonly ILogger<MetricsAnalysisService>? _logger;
    private readonly TypeScope? _typeScope;

    public MetricsAnalysisService(
        FileDiscoveryService fileDiscoveryService,
        RoslynParser roslynParser,
        LocCalculator locCalculator,
        LogicalLocCalculator logicalLocCalculator,
        PhysicalLocCalculator physicalLocCalculator,
        ILogger<MetricsAnalysisService>? logger = null,
        TypeScope? typeScope = null)
    {
        _typeScope = typeScope;
        _fileDiscoveryService = fileDiscoveryService;
        _roslynParser = roslynParser;
        _locCalculator = locCalculator;
        _logicalLocCalculator = logicalLocCalculator;
        _physicalLocCalculator = physicalLocCalculator;
        _logger = logger;
    }

    public async Task<List<LocMetric>> AnalyzeAsync(string path, bool recursive = true)
    {
        var allMetrics = new List<LocMetric>();
        var files = _fileDiscoveryService.DiscoverFiles(path, recursive).ToList();

        _logger?.LogInformation("Discovered {Count} C# files", files.Count);

        // Process files in parallel for better performance
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = files.Select(async filePath =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await Task.Run(() => AnalyzeFile(filePath));
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        foreach (var fileMetrics in results)
        {
            if (fileMetrics != null)
            {
                allMetrics.AddRange(fileMetrics);
            }
        }

        return allMetrics;
    }

    private List<LocMetric>? AnalyzeFile(string filePath)
    {
        try
        {
            _logger?.LogDebug("Analyzing file: {FilePath}", filePath);

            var syntaxTree = _roslynParser.ParseFile(filePath);
            if (syntaxTree == null)
            {
                _logger?.LogWarning("Failed to parse file: {FilePath}", filePath);
                return null;
            }

            var sourceText = File.ReadAllText(filePath);
            var visitor = new SyntaxVisitor(_typeScope);
            visitor.Visit(syntaxTree.GetRoot());

            var elements = visitor.GetElements();
            var metrics = new List<LocMetric>();

            // Process methods first
            var methods = elements.Where(e => !e.IsClass).ToList();
            foreach (var method in methods)
            {
                var methodLoc = _locCalculator.CalculateLoc(sourceText, method.StartLine, method.EndLine);
                metrics.Add(new LocMetric
                {
                    FilePath = filePath,
                    ClassName = method.ContainingClass ?? string.Empty,
                    MethodName = method.Name,
                    Loc = methodLoc,
                    Lloc = _logicalLocCalculator.Calculate(method.Node)
                });
            }

            // Process classes (aggregate method LOCs)
            var classes = elements.Where(e => e.IsClass).ToList();
            foreach (var classElement in classes)
            {
                var classMethods = metrics
                    .Where(m => m.FilePath == filePath && m.ClassName == classElement.Name)
                    .ToList();

                var classLoc = classMethods.Sum(m => m.Loc);

                metrics.Add(new LocMetric
                {
                    FilePath = filePath,
                    ClassName = classElement.Name,
                    MethodName = string.Empty,
                    Loc = classLoc,
                    Lloc = _logicalLocCalculator.Calculate(classElement.Node)
                });
            }

            // File row: LOC keeps the legacy sum of classes; LLOC and physical LOC are measured on the whole file,
            // so records, structs, enums, nested types and files without classes are all covered.
            metrics.Add(new LocMetric
            {
                FilePath = filePath,
                ClassName = string.Empty,
                MethodName = string.Empty,
                Loc = metrics.Where(m => !string.IsNullOrEmpty(m.ClassName) && string.IsNullOrEmpty(m.MethodName)).Sum(m => m.Loc),
                Lloc = _logicalLocCalculator.Calculate(syntaxTree.GetRoot()),
                PhysicalLoc = _physicalLocCalculator.Calculate(syntaxTree.GetText())
            });

            _logger?.LogDebug("Extracted {Count} metrics from {FilePath}", metrics.Count, filePath);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error analyzing file: {FilePath}", filePath);
            return null;
        }
    }
}

