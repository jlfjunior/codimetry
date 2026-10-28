using Codimetry.Metrics.Ck;
using Codimetry.Metrics.Cyclomatic;
using Codimetry.Metrics.Loc;
using Codimetry.Metrics.Misra;
using Codimetry.Tests.Helpers;

namespace Codimetry.Tests.Exporters;

public class CsvExporterTests : IDisposable
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

    [Fact]
    public async Task Ck_WritesHeaderAndRows()
    {
        await CkCsvExporter.ExportAsync(
            [new CkMetric { FilePath = "a.cs", ClassName = "A", WMC = 1, DIT = 2, NOC = 3, CBO = 4, RFC = 5, LCOM = 6, LcomMethods = 4, Kind = "record struct" }],
            _dir.Combine("metrics_ck.csv"));

        Assert.Equal(["FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM,LcomMethods,Kind", "a.cs,A,1,2,3,4,5,6,4,record struct"], Lines("metrics_ck.csv"));
    }

    [Fact]
    public async Task Misra_WritesHeaderAndRows()
    {
        await MisraCsvExporter.ExportAsync(
            [new MisraMetric { FilePath = "a.cs", ClassName = "A", MethodName = "M", MC = 1, AC = 2, CLC = 3, CWC = 4, CoC = 5, Kind = "record" }],
            _dir.Combine("metrics_misra.csv"));

        // CC repeats CoC for one version
        Assert.Equal(["FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CoC,CC,Kind", "a.cs,A,M,1,2,3,4,5,5,record"], Lines("metrics_misra.csv"));
    }

    [Fact]
    public async Task Cyclomatic_WritesHeaderAndRows()
    {
        await CyclomaticCsvExporter.ExportAsync(
            [new CyclomaticComplexityMetric { FilePath = "a.cs", ClassName = "A", MethodName = "M", Complexity = 7, ClassKey = "App|N.A" }],
            _dir.Combine("metrics_cyclomatic.csv"));

        Assert.Equal(["FilePath,ClassName,MethodName,Complexity,ClassKey", "a.cs,A,M,7,App|N.A"], Lines("metrics_cyclomatic.csv"));
    }

    [Fact]
    public async Task EscapesCommasQuotesAndEmptyFields()
    {
        await LocCsvExporter.ExportAsync(
            [new LocMetric { FilePath = "dir, with comma/a.cs", ClassName = "Say\"Hi\"", MethodName = "", Loc = 1 }],
            _dir.Combine("metrics_loc.csv"));

        Assert.Equal("\"dir, with comma/a.cs\",\"Say\"\"Hi\"\"\",,1,0,", Lines("metrics_loc.csv")[1]);
    }

    [Fact]
    public async Task CreatesMissingOutputDirectory()
    {
        var output = _dir.Combine("nested", "out", "metrics_loc.csv");

        await LocCsvExporter.ExportAsync([], output);

        Assert.True(File.Exists(output));
    }
}
