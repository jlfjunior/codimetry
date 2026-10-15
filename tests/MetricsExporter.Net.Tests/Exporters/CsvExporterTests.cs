using MetricsExporter.Net.Core.Models;
using MetricsExporter.Net.Exporters;
using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Exporters;

public class CsvExporterTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly CsvExporter _exporter = new();

    public void Dispose() => _dir.Dispose();

    private string[] Lines(string fileName) =>
        File.ReadAllLines(_dir.Combine(fileName));

    [Fact]
    public async Task Loc_WritesHeaderAndRows_PhysicalLocEmptyBelowFileLevel()
    {
        await _exporter.ExportAsync(
            [
                new LocMetric { FilePath = "a.cs", ClassName = "A", MethodName = "M", Loc = 5, Lloc = 3 },
                new LocMetric { FilePath = "a.cs", Loc = 5, Lloc = 4, PhysicalLoc = 10 }
            ],
            _dir.Combine("metrics_loc.csv"));

        Assert.Equal(["FilePath,ClassName,MethodName,LOC,LLOC,PhysicalLOC", "a.cs,A,M,5,3,", "a.cs,,,5,4,10"], Lines("metrics_loc.csv"));
    }

    [Fact]
    public async Task Ck_WritesHeaderAndRows()
    {
        await _exporter.ExportCkMetricsAsync(
            [new CkMetric { FilePath = "a.cs", ClassName = "A", WMC = 1, DIT = 2, NOC = 3, CBO = 4, RFC = 5, LCOM = 6 }],
            _dir.Combine("metrics_ck.csv"));

        Assert.Equal(["FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM", "a.cs,A,1,2,3,4,5,6"], Lines("metrics_ck.csv"));
    }

    [Fact]
    public async Task Misra_WritesHeaderAndRows()
    {
        await _exporter.ExportMisraMetricsAsync(
            [new MisraMetric { FilePath = "a.cs", ClassName = "A", MethodName = "M", MC = 1, AC = 2, CLC = 3, CWC = 4, CC = 5 }],
            _dir.Combine("metrics_misra.csv"));

        Assert.Equal(["FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CC", "a.cs,A,M,1,2,3,4,5"], Lines("metrics_misra.csv"));
    }

    [Fact]
    public async Task Cyclomatic_WritesHeaderAndRows()
    {
        await _exporter.ExportCyclomaticComplexityAsync(
            [new CyclomaticComplexityMetric { FilePath = "a.cs", ClassName = "A", MethodName = "M", Complexity = 7 }],
            _dir.Combine("metrics_cyclomatic.csv"));

        Assert.Equal(["FilePath,ClassName,MethodName,Complexity", "a.cs,A,M,7"], Lines("metrics_cyclomatic.csv"));
    }

    [Fact]
    public async Task EscapesCommasQuotesAndEmptyFields()
    {
        await _exporter.ExportAsync(
            [new LocMetric { FilePath = "dir, with comma/a.cs", ClassName = "Say\"Hi\"", MethodName = "", Loc = 1 }],
            _dir.Combine("metrics_loc.csv"));

        Assert.Equal("\"dir, with comma/a.cs\",\"Say\"\"Hi\"\"\",,1,0,", Lines("metrics_loc.csv")[1]);
    }

    [Fact]
    public async Task CreatesMissingOutputDirectory()
    {
        var output = _dir.Combine("nested", "out", "metrics_loc.csv");

        await _exporter.ExportAsync([], output);

        Assert.True(File.Exists(output));
    }
}
