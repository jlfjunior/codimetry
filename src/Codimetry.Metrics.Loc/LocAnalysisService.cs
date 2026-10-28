using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Codimetry.Core.Model;
using Codimetry.Core.Suites;

namespace Codimetry.Metrics.Loc;

/// <summary>
/// Service that orchestrates the analysis of source code and calculation of metrics.
/// </summary>
public class LocAnalysisService
{
    private readonly LocCalculator _locCalculator;
    private readonly LogicalLocCalculator _logicalLocCalculator;
    private readonly PhysicalLocCalculator _physicalLocCalculator;
    private readonly ILogger<LocAnalysisService>? _logger;

    public LocAnalysisService(
        LocCalculator locCalculator,
        LogicalLocCalculator logicalLocCalculator,
        PhysicalLocCalculator physicalLocCalculator,
        ILogger<LocAnalysisService>? logger = null)
    {
        _locCalculator = locCalculator;
        _logicalLocCalculator = logicalLocCalculator;
        _physicalLocCalculator = physicalLocCalculator;
        _logger = logger;
    }

    public async Task<List<LocMetric>> AnalyzeAsync(AnalysisContext context)
    {
        var allMetrics = new List<LocMetric>();

        // Process files in parallel for better performance
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = context.Codebase.Documents.Select(async document =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await Task.Run(() => AnalyzeFile(document.FilePath, document.Tree, context.TypeScope));
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

    private List<LocMetric>? AnalyzeFile(string filePath, SyntaxTree syntaxTree, TypeScope typeScope)
    {
        try
        {
            _logger?.LogDebug("Analyzing file: {FilePath}", filePath);

            var sourceText = File.ReadAllText(filePath);
            var visitor = new SyntaxVisitor(typeScope);
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

