using System.Text;
using MetricsExporter.Net.Core.Models;

namespace MetricsExporter.Net.Exporters;

/// <summary>
/// Exporter for metrics to CSV format.
/// </summary>
public class CsvExporter
{
    public async Task ExportAsync(IEnumerable<LocMetric> metrics, string outputPath)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var csv = new StringBuilder();
        
        // Write header
        csv.AppendLine("FilePath,ClassName,MethodName,LOC,LLOC,PhysicalLOC");

        // Write data rows
        foreach (var metric in metrics)
        {
            var filePath = EscapeCsvField(metric.FilePath);
            var className = EscapeCsvField(metric.ClassName);
            var methodName = EscapeCsvField(metric.MethodName);
            
            csv.AppendLine($"{filePath},{className},{methodName},{metric.Loc},{metric.Lloc},{metric.PhysicalLoc}");
        }

        await File.WriteAllTextAsync(outputPath, csv.ToString());
    }

    /// <summary>
    /// Exports CK metrics to a CSV file.
    /// </summary>
    public async Task ExportCkMetricsAsync(IEnumerable<CkMetric> metrics, string outputPath)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var csv = new StringBuilder();
        
        // Write header
        csv.AppendLine("FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM,LcomMethods,Kind");

        // Write data rows
        foreach (var metric in metrics)
        {
            var filePath = EscapeCsvField(metric.FilePath);
            var className = EscapeCsvField(metric.ClassName);
            
            csv.AppendLine($"{filePath},{className},{metric.WMC},{metric.DIT},{metric.NOC},{metric.CBO},{metric.RFC},{metric.LCOM},{metric.LcomMethods},{EscapeCsvField(metric.Kind)}");
        }

        await File.WriteAllTextAsync(outputPath, csv.ToString());
    }

    /// <summary>
    /// Exports MISRA metrics to a CSV file.
    /// </summary>
    public async Task ExportMisraMetricsAsync(IEnumerable<MisraMetric> metrics, string outputPath)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var csv = new StringBuilder();
        
        // Write header
        csv.AppendLine("FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CC,Kind");

        // Write data rows
        foreach (var metric in metrics)
        {
            var filePath = EscapeCsvField(metric.FilePath);
            var className = EscapeCsvField(metric.ClassName);
            var methodName = EscapeCsvField(metric.MethodName);
            
            csv.AppendLine($"{filePath},{className},{methodName},{metric.MC},{metric.AC},{metric.CLC},{metric.CWC},{metric.CC},{EscapeCsvField(metric.Kind)}");
        }

        await File.WriteAllTextAsync(outputPath, csv.ToString());
    }

    /// <summary>
    /// Exports Cyclomatic Complexity metrics to a CSV file.
    /// </summary>
    public async Task ExportCyclomaticComplexityAsync(IEnumerable<CyclomaticComplexityMetric> metrics, string outputPath)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var csv = new StringBuilder();
        
        // Write header
        csv.AppendLine("FilePath,ClassName,MethodName,Complexity");

        // Write data rows
        foreach (var metric in metrics)
        {
            var filePath = EscapeCsvField(metric.FilePath);
            var className = EscapeCsvField(metric.ClassName);
            var methodName = EscapeCsvField(metric.MethodName);
            
            csv.AppendLine($"{filePath},{className},{methodName},{metric.Complexity}");
        }

        await File.WriteAllTextAsync(outputPath, csv.ToString());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        // If field contains comma, quote, or newline, wrap in quotes and escape quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}

