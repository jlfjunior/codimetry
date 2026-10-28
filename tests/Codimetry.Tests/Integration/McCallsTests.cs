using Codimetry.Metrics.Misra;
using Codimetry.Tests.Helpers;

namespace Codimetry.Tests.Integration;

/// <summary>
/// A call to a method of the analysed code adds 2 + MC of the called method; library calls add
/// nothing by default; recursion adds 2; abstract calls use the only implementation.
/// </summary>
public class McCallsTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private async Task<Dictionary<string, int>> Mc(string code, int libraryCallWeight = 0,
        McLambdaRule lambdas = McLambdaRule.Full)
    {
        _dir.WriteFile("Code.cs", "using System;\nusing System.Collections.Generic;\nusing System.Linq;\n\n" + code);
        var metrics = await Analyzers.Misra(_dir.Path, libraryCallWeight: libraryCallWeight, lambdaRule: lambdas);
        return metrics.Where(m => m.MethodName != "").ToDictionary(m => $"{m.ClassName}.{m.MethodName}", m => m.MC);
    }

    private const string Loja = """
        public class Loja
        {
            public void RealizarCompra(string produto)
            {
                Console.WriteLine($"Compra de {produto} realizada");
            }
        }
        """;

    [Fact]
    public async Task LibraryCalls_AddNothing()
    {
        var mc = await Mc(Loja + """
            public class Filtro
            {
                private List<int> _list = new();
                public void M() => _list.Where(x => x > 0).ToList();
            }
            """);

        Assert.Equal(1, mc["Loja.RealizarCompra"]);
        Assert.Equal(1, mc["Filtro.M"]);
    }

    [Fact]
    public async Task LibraryCallWeight_IsConfigurable()
    {
        var mc = await Mc(Loja, libraryCallWeight: 2);

        Assert.Equal(1 + 2, mc["Loja.RealizarCompra"]);
    }

    [Fact]
    public async Task CheckSupportedSw_AddsTheMcOfIsAppropriate()
    {
        _dir.WriteFile("Article.cs", Samples.MisraArticle);

        var mc = (await Analyzers.Misra(_dir.Path)).Where(m => m.MethodName != "").ToDictionary(m => m.MethodName, m => m.MC);

        Assert.Equal(7, mc["IsAppropriate"]);
        Assert.Equal(1 + (2 + 7) + (2 * 1), mc["CheckSupportedSw"]);   // the article's 12
    }

    [Fact]
    public async Task OwnClassCalls_AddTwoPlusTheirMc()
    {
        var mc = await Mc("""
            public class C
            {
                bool _c;
                public void A() { B(); }
                public void B() { if (_c) _c = false; }
                public int D(int x) => x > 0 ? E(x) : 0;
                private int E(int x) => x;
            }
            """);

        Assert.Equal(2, mc["C.B"]);
        Assert.Equal(1 + (2 + 2), mc["C.A"]);
        Assert.Equal(2 * (1 + (2 + 1)), mc["C.D"]);   // the call is in the ternary's layer; the ternary is the body
    }

    [Fact]
    public async Task Recursion_AddsTwo_WithoutTheMcOfTheCalledMethod()
    {
        var mc = await Mc("""
            public class R
            {
                public int F(int n) { if (n == 0) return 1; return n * F(n - 1); }
                public void A() { B(); }
                public void B() { A(); }
                public int G(int n) => F(n);
            }
            """);

        Assert.Equal((2 * 1) + 1 + 2, mc["R.F"]);
        Assert.Equal(1 + 2, mc["R.A"]);
        Assert.Equal(1 + 2, mc["R.B"]);
        Assert.Equal(1 + (2 + 5), mc["R.G"]);   // F is outside G's cycle: 2 + MC(F)
    }

    [Fact]
    public async Task AbstractCalls_UseTheOnlyImplementation()
    {
        var mc = await Mc("""
            public interface IRepo { int Salvar(int v); }
            public interface IFila { void Enviar(); }

            public class Repo : IRepo { public int Salvar(int v) { if (v > 0) v = 1; return v; } }
            public class FilaA : IFila { public void Enviar() { } }
            public class FilaB : IFila { public void Enviar() { } }

            public class Servico
            {
                private readonly IRepo _repo = new Repo();
                private readonly IFila _fila = new FilaA();
                public void M() => _repo.Salvar(1);
                public void N() => _fila.Enviar();
            }
            """);

        Assert.Equal(3, mc["Repo.Salvar"]);
        Assert.Equal(1 + 2 + 3, mc["Servico.M"]);
        Assert.Equal(1 + 2 + 1, mc["Servico.N"]);   // two implementations: MC 1 assumed
    }

    [Fact]
    public async Task DelegatesNameofAndObjectCreation_AreNotCalls()
    {
        var mc = await Mc("""
            public class Item { }

            public class D
            {
                private Func<int> _f = () => 1;
                public int M()
                {
                    var g = new Func<int, int>(x => x);
                    var item = new Item();
                    var name = nameof(M);
                    return _f() + g(1);
                }
            }
            """);

        Assert.Equal(1, mc["D.M"]);
    }

    [Fact]
    public async Task UnresolvedCalls_AddTwoPlusOne()
    {
        var mc = await Mc("public class U { public void M() { Missing(); } }");

        Assert.Equal(1 + 2 + 1, mc["U.M"]);
    }

    // ---------- Lambdas and local functions ----------

    private const string Lambdas = """
        public class L
        {
            private List<int> _items = new();
            private bool _c;

            public void ForEach() => _items.ForEach(x => { if (x > 0) Console.WriteLine(x); });
            public void Calls() => _items.ForEach(i => B());
            public void B() { if (_c) _c = false; }
            public int Local(int v)
            {
                int Clamp(int n) { if (n < 0) return 0; return n; }
                return Clamp(v);
            }
        }
        """;

    [Theory]
    [InlineData(McLambdaRule.Full, 1 + (2 * 1), 1 + (2 + 2), (2 * 1) + 1 + 1)]
    [InlineData(McLambdaRule.Calls, 1, 1 + (2 + 2), (2 * 1) + 1)]
    [InlineData(McLambdaRule.Ignore, 1, 1, 1)]
    public async Task Lambdas_ByRule(McLambdaRule rule, int forEach, int calls, int local)
    {
        var mc = await Mc(Lambdas, lambdas: rule);

        Assert.Equal(forEach, mc["L.ForEach"]);   // library calls add nothing
        Assert.Equal(calls, mc["L.Calls"]);       // a call in a lambda adds 2 + MC(B)
        Assert.Equal(local, mc["L.Local"]);       // the call to the local function adds nothing
    }
}
