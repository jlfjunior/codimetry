using static Codimetry.Core.Output.CsvWriter;

namespace Codimetry.Metrics.Misra;

public static class MisraCsvExporter
{
    /// <summary>
    /// Exports MISRA metrics to a CSV file (metrics_misra.csv).
    /// </summary>
    public static Task ExportAsync(IEnumerable<MisraMetric> metrics, string outputPath) =>
        WriteAsync(outputPath, "FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CoC,CC,Kind", metrics, m =>
            $"{Escape(m.FilePath)},{Escape(m.ClassName)},{Escape(m.MethodName)},{m.MC},{m.AC},{m.CLC},{m.CWC},{m.CoC},{m.CC},{Escape(m.Kind)}");

    /// <summary>
    /// Exports the CoC of each inheritance hierarchy to a CSV file (metrics_coc.csv).
    /// </summary>
    public static Task ExportHierarchiesAsync(IEnumerable<CocHierarchyMetric> hierarchies, string outputPath) =>
        WriteAsync(outputPath, "FilePath,ClassName,Classes,Levels,CoC,Kind", hierarchies, h =>
            $"{Escape(h.FilePath)},{Escape(h.ClassName)},{h.Classes},{h.Levels},{h.CoC},{Escape(h.Kind)}");
}
