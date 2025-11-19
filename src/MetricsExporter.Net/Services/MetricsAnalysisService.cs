using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MetricsExporter.Net.Core.Models;
using MetricsExporter.Net.Parsers;

namespace MetricsExporter.Net.Services;

/// <summary>
/// Service that orchestrates the analysis of source code and calculation of metrics.
/// </summary>
public class MetricsAnalysisService
{
    private readonly FileDiscoveryService _fileDiscoveryService;
    private readonly RoslynParser _roslynParser;
    private readonly LocCalculator _locCalculator;
    private readonly ILogger<MetricsAnalysisService>? _logger;

    public MetricsAnalysisService(
        FileDiscoveryService fileDiscoveryService,
        RoslynParser roslynParser,
        LocCalculator locCalculator,
        ILogger<MetricsAnalysisService>? logger = null)
    {
        _fileDiscoveryService = fileDiscoveryService;
        _roslynParser = roslynParser;
        _locCalculator = locCalculator;
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

        // Add file-level metrics (sum of all classes in file)
        var fileLevelMetrics = allMetrics
            .Where(m => !string.IsNullOrEmpty(m.ClassName) && string.IsNullOrEmpty(m.MethodName))
            .GroupBy(m => m.FilePath)
            .Select(g => new LocMetric
            {
                FilePath = g.Key,
                ClassName = string.Empty,
                MethodName = string.Empty,
                Loc = g.Sum(c => c.Loc)
            })
            .ToList();

        allMetrics.AddRange(fileLevelMetrics);

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
            var visitor = new SyntaxVisitor();
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
                    Loc = methodLoc
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
                    Loc = classLoc
                });
            }

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

