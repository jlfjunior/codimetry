using Codimetry.Core.Output;
using Codimetry.Tests.Helpers;

namespace Codimetry.Core.Tests;

public class CsvWriterTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void EscapesCommasQuotesAndEmptyFields()
    {
        Assert.Equal("\"dir, with comma/a.cs\"", CsvWriter.Escape("dir, with comma/a.cs"));
        Assert.Equal("\"Say\"\"Hi\"\"\"", CsvWriter.Escape("Say\"Hi\""));
        Assert.Equal("", CsvWriter.Escape(""));
        Assert.Equal("plain", CsvWriter.Escape("plain"));
    }

    [Fact]
    public async Task CreatesMissingOutputDirectory()
    {
        var output = _dir.Combine("nested", "out", "metrics.csv");

        await CsvWriter.WriteAsync(output, "A,B", Array.Empty<int>(), n => n.ToString());

        Assert.Equal(["A,B"], File.ReadAllLines(output));
    }
}
