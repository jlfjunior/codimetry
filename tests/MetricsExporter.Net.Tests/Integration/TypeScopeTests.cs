using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Integration;

/// <summary>
/// INC-13: records, structs and record structs are measured like classes in every CSV (D19);
/// --classes-only restores the scope of v1.
/// </summary>
public class TypeScopeTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private const string Pessoa = """
        public record Pessoa(string Nome, int Idade)
        {
            public bool Maior()
            {
                if (Idade >= 18) return true;
                return false;
            }
        }

        public record Aluno(string Nome, int Idade, int Ra) : Pessoa(Nome, Idade);
        """;

    [Fact]
    public async Task Record_IsMeasuredAsAClass()
    {
        _dir.WriteFile("Pessoa.cs", Pessoa);

        var ck = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);
        var misra = await Analyzers.Misra(_dir.Path);

        Assert.Equal("record", ck["Pessoa"].Kind);
        Assert.Equal(1, ck["Pessoa"].WMC);   // Maior; the members synthesized by the compiler do not count

        var pessoa = Assert.Single(misra, m => m.ClassName == "Pessoa" && m.MethodName == "");
        Assert.Equal(2, pessoa.AC);          // Nome and Idade are public properties
        Assert.Equal("record", pessoa.Kind);

        var maior = Assert.Single(misra, m => m.MethodName == "Maior");
        Assert.Equal(2 * 1 + 1, maior.MC);
        Assert.Equal("record", maior.Kind);
    }

    [Fact]
    public async Task Record_Inheritance_EntersDitAndNoc()
    {
        _dir.WriteFile("Pessoa.cs", Pessoa);

        var ck = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);

        // System.Object still counts as one level until INC-12: Pessoa 1, Aluno 2
        Assert.Equal(ck["Pessoa"].DIT + 1, ck["Aluno"].DIT);
        Assert.Equal(1, ck["Pessoa"].NOC);
        Assert.Equal(0, ck["Aluno"].NOC);
    }

    [Fact]
    public async Task Record_PositionalParameters_AreAttributesForLcomAndAc()
    {
        _dir.WriteFile("Faixa.cs", """
            public record Faixa(int Min, int Max)
            {
                public int Largura() => Max - Min;
                public bool Vazia() => Min == Max;
                public int Inicio() => Min;
                public string Nome() => "faixa";
            }

            public record Explicita(string Nome)
            {
                public string Nome { get; init; } = Nome.Trim();
            }
            """);

        var ck = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);
        var misra = (await Analyzers.Misra(_dir.Path)).Where(m => m.MethodName == "").ToDictionary(m => m.ClassName);

        // Pairs sharing Min or Max: (Largura,Vazia), (Largura,Inicio), (Vazia,Inicio) -> Q = 3; with Nome: P = 3
        Assert.Equal(0, ck["Faixa"].LCOM);
        Assert.Equal(2, misra["Faixa"].AC);
        Assert.Equal(1, misra["Explicita"].AC);   // the declared property replaces the positional one
    }

    [Fact]
    public async Task Struct_IsMeasured_WithoutInheritance()
    {
        _dir.WriteFile("Ponto.cs", """
            public interface IMedida { double Dist(); }

            public struct Ponto : IMedida
            {
                int X;
                int Y;
                public double Dist() => System.Math.Sqrt(X * X + Y * Y);
            }

            public record struct Par(int A, int B)
            {
                public int Soma() => A + B;
            }
            """);

        var ck = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);
        var misra = (await Analyzers.Misra(_dir.Path)).Where(m => m.MethodName == "").ToDictionary(m => m.ClassName);

        Assert.Equal(("struct", 1, 0, 0), (ck["Ponto"].Kind, ck["Ponto"].WMC, ck["Ponto"].DIT, ck["Ponto"].NOC));
        Assert.Equal(("record struct", 1, 0, 0), (ck["Par"].Kind, ck["Par"].WMC, ck["Par"].DIT, ck["Par"].NOC));
        Assert.Equal(2, misra["Ponto"].AC);
        Assert.Equal(2, misra["Par"].AC);
        Assert.DoesNotContain("IMedida", ck.Keys);   // interfaces stay out
    }

    private const string Nested = """
        public class A
        {
            public void Own() { }

            public record B
            {
                public void M() { if (true) { } }
            }
        }
        """;

    [Fact]
    public async Task MethodsOfANestedRecord_BelongToTheRecord_InEveryCsv()
    {
        _dir.WriteFile("A.cs", Nested);

        var ck = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);
        var misra = await Analyzers.Misra(_dir.Path);
        var cyclomatic = await Analyzers.Cyclomatic(_dir.Path);
        var loc = await Analyzers.Loc(_dir.Path);

        Assert.Equal((1, 1), (ck["A"].WMC, ck["B"].WMC));
        Assert.Equal("B", Assert.Single(misra, m => m.MethodName == "M").ClassName);
        Assert.Equal("B", Assert.Single(cyclomatic, m => m.MethodName == "M").ClassName);
        Assert.Equal("B", Assert.Single(loc, m => m.MethodName == "M").ClassName);

        // Class rows of V(G) and LOC sum only their own methods
        Assert.Equal(1, Assert.Single(cyclomatic, m => m.ClassName == "A" && m.MethodName == "").Complexity);
        Assert.Equal(2, Assert.Single(cyclomatic, m => m.ClassName == "B" && m.MethodName == "").Complexity);
    }

    [Fact]
    public async Task ClassesOnly_RestoresTheScopeOfV1()
    {
        _dir.WriteFile("A.cs", Nested);
        _dir.WriteFile("Pessoa.cs", Pessoa);
        var classesOnly = new TypeScope(ClassesOnly: true);

        var ck = await Analyzers.Ck(_dir.Path, typeScope: classesOnly);
        var misra = await Analyzers.Misra(_dir.Path, typeScope: classesOnly);
        var cyclomatic = await Analyzers.Cyclomatic(_dir.Path, typeScope: classesOnly);
        var loc = await Analyzers.Loc(_dir.Path, typeScope: classesOnly);

        Assert.Equal(["A"], ck.Select(m => m.ClassName));
        Assert.Equal(["A"], misra.Where(m => m.ClassName != "").Select(m => m.ClassName).Distinct());
        Assert.DoesNotContain(misra, m => m.MethodName == "M" || m.MethodName == "Maior");

        // V(G) and LOC still measure the methods, but they belong to no class (never to the enclosing A)
        Assert.Equal("", Assert.Single(cyclomatic, m => m.MethodName == "M").ClassName);
        Assert.Equal("", Assert.Single(loc, m => m.MethodName == "M").ClassName);
        Assert.DoesNotContain(loc, m => m.ClassName is "B" or "Pessoa" or "Aluno");
    }
}
