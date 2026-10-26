using System.CommandLine;
using Codimetry.CLI.Commands;
using Codimetry.Tests.Helpers;

namespace Codimetry.Tests.Cli;

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
        Assert.Equal(["metrics_ck.csv", "metrics_coc.csv", "metrics_cyclomatic.csv", "metrics_loc.csv", "metrics_misra.csv"], OutputFiles());
    }

    [Theory]
    [InlineData("--loc", "metrics_loc.csv")]
    [InlineData("--ck", "metrics_ck.csv")]
    [InlineData("--misra", "metrics_coc.csv,metrics_misra.csv")]   // CoC per inheritance hierarchy, with MISRA
    [InlineData("--cc", "metrics_cyclomatic.csv")]
    public async Task SingleMetricFlag_ExportsOnlyThatFile(string flag, string expectedFiles)
    {
        var exitCode = await Run("--path", Source, "--out", Output, flag);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedFiles.Split(','), OutputFiles());
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
        Assert.Contains(composed, l => l.EndsWith(",Nested,M,6,0,0,0,0,0,class"));
        Assert.Contains(additive, l => l.EndsWith(",Nested,M,5,0,0,0,0,0,class"));
    }

    [Fact]
    public async Task AcTypeWeights_ChangesTheMisraAc()
    {
        _dir.WriteFile("src/Holder.cs", "public class Item { } public class Holder { const int Max = 1; Item _item; int _n; }");

        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--misra"));
        var counted = File.ReadAllLines(Path.Combine(Output, "metrics_misra.csv"));
        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--misra", "--ac-type-weights"));
        var weighted = File.ReadAllLines(Path.Combine(Output, "metrics_misra.csv"));

        // One per instance attribute: _item, _n = 2; legacy rule: Max (1) + Item (2) + int (1) = 4
        Assert.Contains(counted, l => l.EndsWith(",Holder,,0,2,2,0,0,0,class"));
        Assert.Contains(weighted, l => l.EndsWith(",Holder,,0,4,4,0,0,0,class"));
    }

    [Fact]
    public async Task RfcExcludeConstructors_ChangesTheCkRfc()
    {
        _dir.WriteFile("src/Rfc.cs", "public class Repo { } public class Uses { public void Go() { var r = new Repo(); } }");

        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--ck"));
        var withCtors = File.ReadAllLines(Path.Combine(Output, "metrics_ck.csv"));
        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--ck", "--rfc-exclude-constructors"));
        var withoutCtors = File.ReadAllLines(Path.Combine(Output, "metrics_ck.csv"));

        // Columns after the class name: WMC, DIT, NOC, CBO, RFC; Go and Repo..ctor, or Go alone
        Assert.Contains(withCtors, l => l.Contains(",Uses,1,0,0,1,2,"));
        Assert.Contains(withoutCtors, l => l.Contains(",Uses,1,0,0,1,1,"));
    }

    [Fact]
    public async Task CboOptions_ChangeTheCkCbo()
    {
        _dir.WriteFile("src/Cbo.cs", "public class Repo { } public class Uses { public void Go() { var r = new Repo(); } }");

        async Task<string[]> Ck(params string[] options)
        {
            Assert.Equal(0, await Run(["--path", Source, "--out", Output, "--ck", .. options]));
            return File.ReadAllLines(Path.Combine(Output, "metrics_ck.csv"));
        }

        // Columns after the class name: WMC, DIT, NOC, CBO
        var outgoing = await Ck();
        var declaredOnly = await Ck("--cbo-declared-types-only");
        var bidirectional = await Ck("--cbo-bidirectional");

        Assert.Contains(outgoing, l => l.Contains(",Uses,1,0,0,1,"));
        Assert.Contains(outgoing, l => l.Contains(",Repo,0,0,0,0,"));
        Assert.Contains(declaredOnly, l => l.Contains(",Uses,1,0,0,0,"));   // a local is not a declared type
        Assert.Contains(bidirectional, l => l.Contains(",Repo,0,0,0,1,"));  // used by Uses
    }

    [Fact]
    public async Task WmcOptions_ChangeTheCkWmc()
    {
        _dir.WriteFile("src/Util.cs", "public class Util { public static int A() => 1; public int B() => 2; }");

        async Task<string[]> Ck(params string[] options)
        {
            Assert.Equal(0, await Run(["--path", Source, "--out", Output, "--ck", .. options]));
            return File.ReadAllLines(Path.Combine(Output, "metrics_ck.csv"));
        }

        // Calculator: Add, Subtract (V(G) 1) and Multiply (V(G) 2); Util: one static and one instance method
        var unit = await Ck();
        var cyclomatic = await Ck("--wmc-weight", "cyclomatic");
        var excludeStatic = await Ck("--wmc-exclude-static");

        Assert.Contains(unit, l => l.Contains(",Calculator,3,"));
        Assert.Contains(unit, l => l.Contains(",Util,2,"));
        Assert.Contains(cyclomatic, l => l.Contains(",Calculator,4,"));
        Assert.Contains(excludeStatic, l => l.Contains(",Util,1,"));
    }

    [Fact]
    public async Task CcOperatorOptions_ChangeTheVgAndTheCyclomaticWmc()
    {
        _dir.WriteFile("src/Names.cs", "public class Names { public string Of(string? s) => s ?? \"\"; }");

        async Task<(string[] Cc, string[] Ck)> Collect(params string[] options)
        {
            Assert.Equal(0, await Run(["--path", Source, "--out", Output, "--cc", "--ck", "--wmc-weight", "cyclomatic", .. options]));
            return (File.ReadAllLines(Path.Combine(Output, "metrics_cyclomatic.csv")),
                    File.ReadAllLines(Path.Combine(Output, "metrics_ck.csv")));
        }

        var byDefault = await Collect();
        var nullOperators = await Collect("--cc-null-operators");
        var legacy = await Collect("--cc-legacy-operators");

        Assert.Contains(byDefault.Cc, l => l.Contains(",Names,Of,1,"));
        Assert.Contains(byDefault.Ck, l => l.Contains(",Names,1,"));
        Assert.Contains(nullOperators.Cc, l => l.Contains(",Names,Of,2,"));
        Assert.Contains(nullOperators.Ck, l => l.Contains(",Names,2,"));
        Assert.Contains(legacy.Cc, l => l.Contains(",Names,Of,2,"));
    }

    [Fact]
    public async Task CcNullAndLegacyOperators_FailValidationTogether()
    {
        Assert.NotEqual(0, await Run("--path", Source, "--out", Output, "--cc-null-operators", "--cc-legacy-operators"));
        Assert.Empty(OutputFiles());
    }

    [Fact]
    public async Task ClassesOnly_DropsRecordsAndStructs_KindColumnNamesTheType()
    {
        _dir.WriteFile("src/Types.cs", "public record Dto(string Nome); public struct Ponto { int X; }");

        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--ck"));
        var all = File.ReadAllLines(Path.Combine(Output, "metrics_ck.csv"));
        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--ck", "--classes-only"));
        var classesOnly = File.ReadAllLines(Path.Combine(Output, "metrics_ck.csv"));

        Assert.EndsWith(",Kind", all[0]);
        Assert.Contains(all, l => l.Contains(",Dto,") && l.EndsWith(",record"));
        Assert.Contains(all, l => l.Contains(",Ponto,") && l.EndsWith(",struct"));
        Assert.Contains(all, l => l.Contains(",Calculator,") && l.EndsWith(",class"));
        Assert.DoesNotContain(classesOnly, l => l.Contains(",Dto,") || l.Contains(",Ponto,"));
        Assert.Contains(classesOnly, l => l.Contains(",Calculator,"));
    }

    [Fact]
    public async Task McLibraryCallWeight_ChangesTheMcOfLibraryCalls()
    {
        _dir.WriteFile("src/Log.cs", "public class Log { public void Write() { System.Console.WriteLine(1); } }");

        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--misra"));
        var byDefault = File.ReadAllLines(Path.Combine(Output, "metrics_misra.csv"));
        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--misra", "--mc-library-call-weight", "2"));
        var weighted = File.ReadAllLines(Path.Combine(Output, "metrics_misra.csv"));

        Assert.Contains(byDefault, l => l.EndsWith(",Log,Write,1,0,0,0,0,0,class"));
        Assert.Contains(weighted, l => l.EndsWith(",Log,Write,3,0,0,0,0,0,class"));
    }

    [Fact]
    public async Task McLambdas_ChangesTheMcOfLambdaBodies()
    {
        _dir.WriteFile("src/Each.cs", "public class Each { public void Run(int[] l) => System.Array.ForEach(l, i => { if (i > 0) { } }); }");

        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--misra"));
        var full = File.ReadAllLines(Path.Combine(Output, "metrics_misra.csv"));
        Assert.Equal(0, await Run("--path", Source, "--out", Output, "--misra", "--mc-lambdas", "ignore"));
        var ignore = File.ReadAllLines(Path.Combine(Output, "metrics_misra.csv"));

        Assert.Contains(full, l => l.EndsWith(",Each,Run,3,0,0,0,0,0,class"));
        Assert.Contains(ignore, l => l.EndsWith(",Each,Run,1,0,0,0,0,0,class"));
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
