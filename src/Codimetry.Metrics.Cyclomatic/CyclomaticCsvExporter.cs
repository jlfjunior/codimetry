using static Codimetry.Core.Output.CsvWriter;

namespace Codimetry.Metrics.Cyclomatic;

public static class CyclomaticCsvExporter
{
    /// <summary>
    /// Exports Cyclomatic Complexity metrics to a CSV file (metrics_cyclomatic.csv).
    /// </summary>
    public static Task ExportAsync(IEnumerable<CyclomaticComplexityMetric> metrics, string outputPath) =>
        WriteAsync(outputPath, "FilePath,ClassName,MethodName,Complexity,ClassKey", metrics, m =>
            $"{Escape(m.FilePath)},{Escape(m.ClassName)},{Escape(m.MethodName)},{m.Complexity},{Escape(m.ClassKey)}");
}
