using System.CommandLine;
using MetricsExporter.Net.CLI.Commands;
using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Cli;

public class RootCommandHandlerTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public RootCommandHandlerTests()
    {
        _dir.WriteFile("src/Calculator.cs", Samples.CkWmc);
    }

    public void Dispose() => _dir.Dispose();

    private string Source => _dir.Combine("src");
    private string Output => _dir.Combine("out");

    private static Task<int> Run(params string[] args) =>
        RootCommandHandler.CreateCommand().InvokeAsync(args);

    private string[] OutputFiles() =>
        Directory.Exists(Output)
            ? [.. Directory.GetFiles(Output).Select(f => Path.GetFileName(f)!).Order()]
            : [];

    [Fact]
    public async Task NoMetricFlags_ExportsAllFourFiles()
    {
        var exitCode = await Run("--path", Source, "--out", Output);

        Assert.Equal(0, exitCode);
        Assert.Equal(["metrics_ck.csv", "metrics_cyclomatic.csv", "metrics_loc.csv", "metrics_misra.csv"], OutputFiles());
    }

    [Theory]
    [InlineData("--loc", "metrics_loc.csv")]
    [InlineData("--ck", "metrics_ck.csv")]
    [InlineData("--misra", "metrics_misra.csv")]
    [InlineData("--cc", "metrics_cyclomatic.csv")]
    public async Task SingleMetricFlag_ExportsOnlyThatFile(string flag, string expectedFile)
    {
        var exitCode = await Run("--path", Source, "--out", Output, flag);

        Assert.Equal(0, exitCode);
        Assert.Equal([expectedFile], OutputFiles());
    }

    [Fact]
    public async Task ProjectInput_IsAnalysed()
    {
        var project = _dir.WriteFile("src/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var exitCode = await Run("--project", project, "--out", Output, "--ck");

        Assert.Equal(0, exitCode);
        Assert.Contains("Calculator", File.ReadAllText(Path.Combine(Output, "metrics_ck.csv")));
    }

    [Fact]
    public async Task McAdditive_ChangesTheMisraMc()
    {
        _dir.WriteFile("src/Nested.cs", "public class Nested { public void M(bool a) { if (a) { for (;;) { } } } }");

        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--misra"));
        var composed = File.ReadAllLines(Path.Combine(Output, "metrics_misra.csv"));
        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--misra", "--mc-additive"));
        var additive = File.ReadAllLines(Path.Combine(Output, "metrics_misra.csv"));

        // Composition: 2 × 3 = 6; additive: 2 + 3 = 5
        Assert.Contains(composed, l => l.EndsWith(",Nested,M,6,0,0,0,0"));
        Assert.Contains(additive, l => l.EndsWith(",Nested,M,5,0,0,0,0"));
    }

    [Fact]
    public async Task NoInput_FailsValidation()
    {
        Assert.NotEqual(0, await Run("--out", Output));
        Assert.Empty(OutputFiles());
    }

    [Fact]
    public async Task MoreThanOneInput_FailsValidation()
    {
        Assert.NotEqual(0, await Run("--path", Source, "--solution", "x.sln", "--out", Output));
        Assert.Empty(OutputFiles());
    }

    [Fact]
    public async Task MissingPath_ReturnsOne()
    {
        // Before the refactoring, this path called Environment.Exit(1) and killed the test host
        Assert.Equal(1, await Run("--path", _dir.Combine("missing"), "--out", Output));
    }
}
