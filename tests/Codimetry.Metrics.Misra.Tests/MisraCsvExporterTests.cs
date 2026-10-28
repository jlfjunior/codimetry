using Codimetry.Tests.Helpers;

namespace Codimetry.Metrics.Misra.Tests;

public class MisraCsvExporterTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private string[] Lines(string fileName) =>
        File.ReadAllLines(_dir.Combine(fileName));

    [Fact]
    public async Task Misra_WritesHeaderAndRows()
    {
        await MisraCsvExporter.ExportAsync(
            [new MisraMetric { FilePath = "a.cs", ClassName = "A", MethodName = "M", MC = 1, AC = 2, CLC = 3, CWC = 4, CoC = 5, Kind = "record" }],
            _dir.Combine("metrics_misra.csv"));

        // CC repeats CoC for one version
        Assert.Equal(["FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CoC,CC,Kind", "a.cs,A,M,1,2,3,4,5,5,record"], Lines("metrics_misra.csv"));
    }
}
