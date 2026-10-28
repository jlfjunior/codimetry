using Codimetry.Tests.Helpers;

namespace Codimetry.Metrics.Misra.Tests;

/// <summary>
/// CWC = Σ (2 + MC of the called method) over the calls to other types of the analysed code (Eq. 24).
/// </summary>
public class CwcTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private async Task<Dictionary<string, MisraMetric>> Classes(string code, int cwcLibraryCallWeight = 0)
    {
        _dir.WriteFile("Code.cs", "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\n\n" + code);
        var metrics = await Analyzers.Misra(_dir.Path, cwcLibraryCallWeight: cwcLibraryCallWeight);
        return metrics.Where(m => m.ClassName != "" && m.MethodName == "").ToDictionary(m => m.ClassName);
    }

    private const string Carrinho = """
        public class Produto { public bool Ativo { get; set; } }

        public class EstoqueService
        {
            public bool VerificarDisponibilidade(Produto p) { if (p.Ativo) return true; else return false; }
        }

        public class CarrinhoService
        {
            private readonly EstoqueService _estoque = new();

            public void Finalizar(Produto p)
            {
                if (_estoque.VerificarDisponibilidade(p))
                    Console.WriteLine("Compra finalizada");
            }
        }
        """;

    [Fact]
    public async Task CarrinhoService_CountsOnlyTheCallToTheSystem()
    {
        var classes = await Classes(Carrinho);

        Assert.Equal(2 + 2, classes["CarrinhoService"].CWC);   // VerificarDisponibilidade has MC 2; WriteLine does not count
        Assert.Equal(0, classes["EstoqueService"].CWC);
    }

    [Fact]
    public async Task Article_Hardware9_Software0_Others0()
    {
        _dir.WriteFile("Article.cs", Samples.MisraArticle);

        var classes = (await Analyzers.Misra(_dir.Path)).Where(m => m.MethodName == "").Where(m => m.ClassName != "")
            .ToDictionary(m => m.ClassName, m => m.CWC);

        Assert.Equal(2 + 7, classes["Hardware"]);   // s.IsAppropriate
        Assert.Equal(0, classes["Software"]);        // string.Compare only
        Assert.Equal(0, classes["Computer"]);
        Assert.Equal(0, classes["Desktop"]);
        Assert.Equal(0, classes["Notebook"]);
    }

    [Fact]
    public async Task LibraryCallsDelegatesLocalFunctionsAndUnresolvedCalls_DoNotCount()
    {
        var classes = await Classes("""
            public class Infra
            {
                private readonly List<int> _items = new();
                private readonly Func<int> _next = () => 1;

                public string Run()
                {
                    int Twice(int v) => v * 2;
                    var sb = new StringBuilder();
                    sb.Append(_items.Where(i => i > 0).Sum());
                    Console.WriteLine(sb.ToString());
                    Missing();
                    return Twice(_next()).ToString();
                }
            }
            """);

        Assert.Equal(0, classes["Infra"].CWC);
    }

    [Fact]
    public async Task LibraryWeight_ReproducesTheV1RuleForLibraryCalls()
    {
        var classes = await Classes(Carrinho, cwcLibraryCallWeight: 3);

        Assert.Equal((2 + 2) + 3, classes["CarrinhoService"].CWC);
    }

    [Fact]
    public async Task EachCallCounts_AbstractCallsUseTheOnlyImplementation()
    {
        var classes = await Classes("""
            public interface IRepo { void Salvar(); }
            public interface ILog { void Write(); }
            public class RepoA : IRepo { public void Salvar() { } }
            public class RepoB : IRepo { public void Salvar() { } }
            public class Log : ILog { public void Write() { if (DateTime.Now.Second > 30) { } } }
            public class Util { public static int Um() => 1; }

            public class Servico
            {
                private readonly IRepo _repo = new RepoA();
                private readonly ILog _log = new Log();
                public int Duas() => Util.Um() + Util.Um();
                public void Salvar() => _repo.Salvar();
                public void Registrar() => _log.Write();
            }
            """);

        // Util.Um twice (2 + 1 each); IRepo has two implementations (2 + 1); ILog has one, Log.Write (2 + 2)
        Assert.Equal(2 * (2 + 1) + (2 + 1) + (2 + 2), classes["Servico"].CWC);
    }

    [Fact]
    public async Task InheritedMethodsOfABaseClass_Count_OwnAndPartialMethodsDoNot()
    {
        var classes = await Classes("""
            public class Hardware { protected string os = ""; public string GetOS() { return os; } }

            public partial class Desktop : Hardware
            {
                public string Os() => base.GetOS();
                public string Os2() => GetOS();
                public string Case() => Helper();
            }

            public partial class Desktop { private string Helper() => Os(); }
            """);

        Assert.Equal(2 * (2 + 1), classes["Desktop"].CWC);   // two calls to Hardware.GetOS (MC 1); Helper and Os are own
    }
}
