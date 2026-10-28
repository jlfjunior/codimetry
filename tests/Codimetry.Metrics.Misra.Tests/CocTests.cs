using Codimetry.Core.Suites;
using Codimetry.Tests.Helpers;

namespace Codimetry.Metrics.Misra.Tests;

/// <summary>
/// CoC follows the inheritance hierarchy (Eq. 27): CLC of a class times the CoC of its subclasses
/// (tree, the default, as in the example of Misra et al.) or the product of the Σ CLC of each level (levels);
/// hierarchies add and CWC does not enter.
/// </summary>
public class CocTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private static long Coc(CocFormula formula, Dictionary<string, int> clc, Dictionary<string, string> baseClass)
    {
        var calculator = new CocCalculator(formula);
        var hierarchies = calculator.Hierarchies(
            clc,
            key => baseClass.GetValueOrDefault(key),
            key => baseClass.Where(b => b.Value == key).Select(b => b.Key));
        return calculator.CalculateCoc(hierarchies);
    }

    private static readonly Dictionary<string, string> Article = new()
    {
        ["Software"] = "Computer",
        ["Hardware"] = "Computer",
        ["Desktop"] = "Hardware",
        ["Notebook"] = "Hardware",
    };

    [Theory]
    [InlineData(CocFormula.Tree, 536)]     // 4 × (14 + 20 × (3 + 3))
    [InlineData(CocFormula.Levels, 816)]   // 4 × (14 + 20) × (3 + 3)
    public void Article_WithItsClc(CocFormula formula, long coc)
    {
        var clc = new Dictionary<string, int> { ["Computer"] = 4, ["Software"] = 14, ["Hardware"] = 20, ["Desktop"] = 3, ["Notebook"] = 3 };

        Assert.Equal(coc, Coc(formula, clc, Article));
    }

    [Theory]
    [InlineData(CocFormula.Tree)]
    [InlineData(CocFormula.Levels)]
    public void IsolatedClassesAdd(CocFormula formula)
    {
        Assert.Equal(12, Coc(formula, new() { ["A"] = 5, ["B"] = 7 }, []));
    }

    [Theory]
    [InlineData(CocFormula.Tree)]
    [InlineData(CocFormula.Levels)]
    public void ParentTimesTheSumOfItsChildren(CocFormula formula)
    {
        Assert.Equal(14, Coc(formula, new() { ["A"] = 2, ["B"] = 3, ["C"] = 4 }, new() { ["B"] = "A", ["C"] = "A" }));
    }

    [Fact]
    public void BaseClassOutsideTheCode_MakesARoot()
    {
        Assert.Equal(3, Coc(CocFormula.Tree, new() { ["E"] = 3 }, new() { ["E"] = "System.Private.CoreLib|System.Exception" }));
    }

    [Fact]
    public void Overflow_IsReportedAsMaxValue()
    {
        var clc = Enumerable.Range(0, 8).ToDictionary(i => $"C{i}", _ => 1_000_000);
        var chain = Enumerable.Range(1, 7).ToDictionary(i => $"C{i}", i => $"C{i - 1}");
        var calculator = new CocCalculator();

        var hierarchy = Assert.Single(calculator.Hierarchies(clc, chain.GetValueOrDefault, key => chain.Where(c => c.Value == key).Select(c => c.Key)));

        Assert.Equal(long.MaxValue, hierarchy.CoC);
        Assert.Equal((8, 8), (hierarchy.Classes, hierarchy.Levels));
        Assert.Equal(1, calculator.Overflows);
    }

    private async Task<(long CoC, IReadOnlyList<CocHierarchyMetric> Hierarchies, List<MisraMetric> Metrics)> Misra(
        string code, CocFormula formula = CocFormula.Tree, bool legacy = false)
    {
        _dir.WriteFile("App.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>""");
        _dir.WriteFile("App.cs", code);
        var service = new MisraMetricsAnalysisService(cocFormula: formula, cocLegacy: legacy);
        var metrics = await service.AnalyzeAsync(AnalysisContext.Load(_dir.Path));
        return (metrics.Single(m => m.ClassName == "" && m.MethodName == "").CoC, service.Hierarchies, metrics);
    }

    [Theory]
    [InlineData(CocFormula.Tree)]
    [InlineData(CocFormula.Levels)]
    public async Task UsuarioCliente_Is4(CocFormula formula)
    {
        var (coc, hierarchies, _) = await Misra(Samples.UsuarioClienteCoc, formula);

        Assert.Equal(4, coc);
        var hierarchy = Assert.Single(hierarchies);
        Assert.Equal(("Usuario", 2, 2, 4L), (hierarchy.ClassName, hierarchy.Classes, hierarchy.Levels, hierarchy.CoC));
    }

    [Theory]
    [InlineData(CocFormula.Tree)]
    [InlineData(CocFormula.Levels)]
    public async Task ExceptionSubclassWithoutChildren_IsItsCLC(CocFormula formula)
    {
        var (coc, _, metrics) = await Misra("public class DominioException : System.Exception { int _codigo; public int Codigo() => _codigo; }", formula);

        Assert.Equal(metrics.Single(m => m.ClassName == "DominioException" && m.MethodName == "").CLC, coc);
    }

    [Theory]
    [InlineData(CocFormula.Tree, 536)]
    [InlineData(CocFormula.Levels, 816)]
    public async Task Article_Is536_AsInThePaper(CocFormula formula, long expected)
    {
        var (coc, hierarchies, metrics) = await Misra(Samples.MisraArticle, formula);
        var clc = metrics.Where(m => m.ClassName != "" && m.MethodName == "").ToDictionary(m => m.ClassName, m => m.CLC);

        Assert.Equal((4, 14, 20, 3, 3), (clc["Computer"], clc["Software"], clc["Hardware"], clc["Desktop"], clc["Notebook"]));
        Assert.Equal(expected, coc);
        var computer = Assert.Single(hierarchies, h => h.Levels > 1);
        Assert.Equal(("Computer", 5, 3), (computer.ClassName, computer.Classes, computer.Levels));
    }

    [Fact]
    public async Task Legacy_IsSumOfClcPlusHalfOfCwc()
    {
        var (coc, _, metrics) = await Misra(Samples.MisraArticle, legacy: true);
        var classes = metrics.Where(m => m.ClassName != "" && m.MethodName == "").ToList();

        Assert.Equal(classes.Sum(m => m.CLC) + classes.Sum(m => m.CWC) / 2, coc);
    }
}
