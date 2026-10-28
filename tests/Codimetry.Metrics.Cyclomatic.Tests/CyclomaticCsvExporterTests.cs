using Codimetry.Tests.Helpers;

namespace Codimetry.Metrics.Cyclomatic.Tests;

public class CyclomaticCsvExporterTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private string[] Lines(string fileName) =>
        File.ReadAllLines(_dir.Combine(fileName));

    [Fact]
    public async Task Cyclomatic_WritesHeaderAndRows()
    {
        await CyclomaticCsvExporter.ExportAsync(
            [new CyclomaticComplexityMetric { FilePath = "a.cs", ClassName = "A", MethodName = "M", Complexity = 7, ClassKey = "App|N.A" }],
            _dir.Combine("metrics_cyclomatic.csv"));

        Assert.Equal(["FilePath,ClassName,MethodName,Complexity,ClassKey", "a.cs,A,M,7,App|N.A"], Lines("metrics_cyclomatic.csv"));
    }
}
