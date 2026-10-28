using static Codimetry.Core.Output.CsvWriter;

namespace Codimetry.Metrics.Loc;

public static class LocCsvExporter
{
    /// <summary>
    /// Exports LOC metrics to a CSV file (metrics_loc.csv).
    /// </summary>
    public static Task ExportAsync(IEnumerable<LocMetric> metrics, string outputPath) =>
        WriteAsync(outputPath, "FilePath,ClassName,MethodName,LOC,LLOC,PhysicalLOC", metrics, m =>
            $"{Escape(m.FilePath)},{Escape(m.ClassName)},{Escape(m.MethodName)},{m.Loc},{m.Lloc},{m.PhysicalLoc}");
}
