using static Codimetry.Core.Output.CsvWriter;

namespace Codimetry.Metrics.Ck;

public static class CkCsvExporter
{
    /// <summary>
    /// Exports CK metrics to a CSV file (metrics_ck.csv).
    /// </summary>
    public static Task ExportAsync(IEnumerable<CkMetric> metrics, string outputPath) =>
        WriteAsync(outputPath, "FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM,LcomMethods,Kind", metrics, m =>
            $"{Escape(m.FilePath)},{Escape(m.ClassName)},{m.WMC},{m.DIT},{m.NOC},{m.CBO},{m.RFC},{m.LCOM},{m.LcomMethods},{Escape(m.Kind)}");
}
