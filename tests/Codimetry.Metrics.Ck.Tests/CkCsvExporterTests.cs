using Codimetry.Tests.Helpers;

namespace Codimetry.Metrics.Ck.Tests;

public class CkCsvExporterTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private string[] Lines(string fileName) =>
        File.ReadAllLines(_dir.Combine(fileName));

    [Fact]
    public async Task Ck_WritesHeaderAndRows()
    {
        await CkCsvExporter.ExportAsync(
            [new CkMetric { FilePath = "a.cs", ClassName = "A", WMC = 1, DIT = 2, NOC = 3, CBO = 4, RFC = 5, LCOM = 6, LcomMethods = 4, Kind = "record struct" }],
            _dir.Combine("metrics_ck.csv"));

        Assert.Equal(["FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM,LcomMethods,Kind", "a.cs,A,1,2,3,4,5,6,4,record struct"], Lines("metrics_ck.csv"));
    }
}
