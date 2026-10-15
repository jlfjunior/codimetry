using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using MetricsExporter.Net.Core.Models;
using MetricsExporter.Net.Parsers;

namespace MetricsExporter.Net.Services;

/// <summary>
/// Service that orchestrates the calculation of Cyclomatic Complexity metrics for all methods, classes, and files.
/// </summary>
public class CyclomaticComplexityAnalysisService
{
    private readonly FileDiscoveryService _fileDiscoveryService;
    private readonly RoslynParser _roslynParser;
    private readonly CyclomaticComplexityCalculator _calculator;
    private readonly TypeScope _scope;
    private readonly ILogger<CyclomaticComplexityAnalysisService>? _logger;

    public CyclomaticComplexityAnalysisService(
        FileDiscoveryService fileDiscoveryService,
        RoslynParser roslynParser,
        ILogger<CyclomaticComplexityAnalysisService>? logger = null,
        CyclomaticOptions? options = null,
        TypeScope? scope = null)
    {
        _fileDiscoveryService = fileDiscoveryService;
        _roslynParser = roslynParser;
        _calculator = new CyclomaticComplexityCalculator(options);
        _scope = scope ?? TypeScope.Default;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes code and calculates Cyclomatic Complexity metrics for all methods, classes, and files.
    /// </summary>
    public async Task<List<CyclomaticComplexityMetric>> AnalyzeAsync(string path, bool recursive = true)
    {
        var allMetrics = new List<CyclomaticComplexityMetric>();
        var files = _fileDiscoveryService.DiscoverFiles(path, recursive).ToList();

        _logger?.LogInformation("Discovered {Count} C# files for cyclomatic complexity analysis", files.Count);

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
            .Select(g => new CyclomaticComplexityMetric
            {
                FilePath = g.Key,
                ClassName = string.Empty,
                MethodName = string.Empty,
                Complexity = g.Sum(c => c.Complexity)
            })
            .ToList();

        allMetrics.AddRange(fileLevelMetrics);

        _logger?.LogInformation("Calculated cyclomatic complexity for {Count} metric entries", allMetrics.Count);

        return allMetrics;
    }

    private List<CyclomaticComplexityMetric>? AnalyzeFile(string filePath)
    {
        try
        {
            _logger?.LogDebug("Analyzing file for cyclomatic complexity: {FilePath}", filePath);

            var syntaxTree = _roslynParser.ParseFile(filePath);
            if (syntaxTree == null)
            {
                _logger?.LogWarning("Failed to parse file: {FilePath}", filePath);
                return null;
            }

            var root = syntaxTree.GetRoot();
            var metrics = new List<CyclomaticComplexityMetric>();

            // Extract all methods and constructors
            var methods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .ToList();

            var constructors = root.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .ToList();

            // Process methods
            foreach (var method in methods)
            {
                var containingClass = _scope.ContainingType(method);
                var className = containingClass?.Identifier.Text ?? string.Empty;

                // Skip abstract methods (they have no body)
                if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)))
                {
                    continue;
                }

                var complexity = _calculator.CalculateComplexity(method);

                metrics.Add(new CyclomaticComplexityMetric
                {
                    FilePath = filePath,
                    ClassName = className,
                    MethodName = method.Identifier.Text,
                    Complexity = complexity
                });
            }

            // Process constructors
            foreach (var constructor in constructors)
            {
                var containingClass = _scope.ContainingType(constructor);
                var className = containingClass?.Identifier.Text ?? string.Empty;

                var complexity = _calculator.CalculateComplexity(constructor);

                metrics.Add(new CyclomaticComplexityMetric
                {
                    FilePath = filePath,
                    ClassName = className,
                    MethodName = constructor.Identifier.Text,
                    Complexity = complexity
                });
            }

            // Process properties with accessors (getters/setters)
            var properties = root.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .ToList();

            foreach (var property in properties)
            {
                var containingClass = _scope.ContainingType(property);
                var className = containingClass?.Identifier.Text ?? string.Empty;

                // Process getter
                if (property.AccessorList != null)
                {
                    var getter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
                    if (getter != null)
                    {
                        var complexity = _calculator.CalculateComplexity(getter);
                        metrics.Add(new CyclomaticComplexityMetric
                        {
                            FilePath = filePath,
                            ClassName = className,
                            MethodName = $"{property.Identifier.Text}.get",
                            Complexity = complexity
                        });
                    }

                    // Process setter
                    var setter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
                    if (setter != null)
                    {
                        var complexity = _calculator.CalculateComplexity(setter);
                        metrics.Add(new CyclomaticComplexityMetric
                        {
                            FilePath = filePath,
                            ClassName = className,
                            MethodName = $"{property.Identifier.Text}.set",
                            Complexity = complexity
                        });
                    }
                }
            }

            // Add class-level metrics (sum of all methods in class)
            var classes = root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Where(_scope.IsMeasured)
                .ToList();

            foreach (var classDecl in classes)
            {
                var className = classDecl.Identifier.Text;
                var classMethods = metrics
                    .Where(m => m.FilePath == filePath && m.ClassName == className)
                    .ToList();

                var classComplexity = classMethods.Sum(m => m.Complexity);

                metrics.Add(new CyclomaticComplexityMetric
                {
                    FilePath = filePath,
                    ClassName = className,
                    MethodName = string.Empty,
                    Complexity = classComplexity
                });
            }

            _logger?.LogDebug("Extracted {Count} cyclomatic complexity metrics from {FilePath}", metrics.Count, filePath);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error analyzing file for cyclomatic complexity: {FilePath}", filePath);
            return null;
        }
    }
}

