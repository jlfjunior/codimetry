using Codimetry.Tests.Helpers;

namespace Codimetry.Metrics.Loc.Tests;

public class LocCsvExporterTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private string[] Lines(string fileName) =>
        File.ReadAllLines(_dir.Combine(fileName));

    [Fact]
    public async Task Loc_WritesHeaderAndRows_PhysicalLocEmptyBelowFileLevel()
    {
        await LocCsvExporter.ExportAsync(
            [
                new LocMetric { FilePath = "a.cs", ClassName = "A", MethodName = "M", Loc = 5, Lloc = 3 },
                new LocMetric { FilePath = "a.cs", Loc = 5, Lloc = 4, PhysicalLoc = 10 }
            ],
            _dir.Combine("metrics_loc.csv"));

        Assert.Equal(["FilePath,ClassName,MethodName,LOC,LLOC,PhysicalLOC", "a.cs,A,M,5,3,", "a.cs,,,5,4,10"], Lines("metrics_loc.csv"));
    }
}
