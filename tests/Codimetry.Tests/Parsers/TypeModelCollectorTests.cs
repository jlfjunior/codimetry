using Codimetry.Parsers;
using Codimetry.Services;
using Codimetry.Tests.Helpers;

namespace Codimetry.Tests.Parsers;

public class TypeModelCollectorTests
{
    [Fact]
    public void CollectsClassesWithBaseClassAndModifiers()
    {
        var visitor = Code.VisitTypes("""
            namespace N
            {
                public abstract class Base { }
                public sealed class Derived : Base, System.IDisposable { public void Dispose() { } }
            }
            """);

        var classes = visitor.GetClasses().ToDictionary(c => c.Name);

        Assert.True(classes["Base"].IsAbstract);
        Assert.True(classes["Derived"].IsSealed);
        Assert.Equal("N", classes["Derived"].Namespace);
        Assert.Equal("Test|N.Base", classes["Derived"].BaseClass);   // key of the base class
        Assert.Equal("Test|N.Derived", classes["Derived"].ClassKey);
        Assert.Equal("global::N.Derived", classes["Derived"].FullyQualifiedName);
        Assert.Contains("System.IDisposable", classes["Derived"].ImplementedInterfaces);
    }

    [Fact]
    public void Methods_RecordStaticAbstractAndCalls()
    {
        var visitor = Code.VisitTypes("""
            abstract class C
            {
                public static void S() { }
                public abstract void A();
                public void M() { S(); other.Run(); Console.WriteLine(); }
            }
            """);

        var methods = visitor.GetMethods().ToDictionary(m => m.Name);

        Assert.True(methods["S"].IsStatic);
        Assert.True(methods["A"].IsAbstract);
        Assert.Equal(["S", "Run", "WriteLine"], methods["M"].CalledMethods);
        Assert.All(methods.Values, m => Assert.Equal("C", m.ContainingClass));
    }

    // An access is a use of an attribute of the current object, identified by the symbol
    // the semantic model binds the identifier to ("field:X", "prop:X", "event:X", "param:X").

    private const string AccessClass = """
        using System;
        using System.Collections.Generic;
        enum Status { Ativo }
        class Dto { public string Nome = ""; }
        class Base { protected int Herdado; }
        class C : Base
        {
            public string Nome = "";
            string nome = "";
            public Status Status { get; set; }
            Action _handler = () => { };
            public event EventHandler? Changed;
            C Criar(string nome) => this;
            MEMBER
        }
        """;

    private static IEnumerable<string> Accessed(TypeModelCollector visitor, string method) =>
        visitor.GetMethods().Single(m => m.Name == method).AccessedFields.Distinct().Order(StringComparer.Ordinal);

    [Theory]
    [InlineData("object M() => new Dto { Nome = \"x\" };", "")]
    [InlineData("object M() => new Dto { Nome = Nome };", "field:Nome")]
    [InlineData("object M() => new C { Nome = \"x\" };", "")]
    [InlineData("object? M(Dto? d) => d?.Nome;", "")]
    [InlineData("object M(C outro) => outro.Nome;", "")]
    [InlineData("bool M(object outro) => outro is C { Nome: \"x\" };", "")]
    [InlineData("bool M(object outro) => outro is C { Nome.Length: 1 };", "")]
    [InlineData("string M() => nameof(Nome);", "")]
    [InlineData("object M() => Criar(nome: \"x\");", "")]
    [InlineData("void M() { var Nome = 1; Console.WriteLine(Nome); }", "")]
    [InlineData("object M() => Status.Ativo;", "")]
    [InlineData("object M() => this.Nome;", "field:Nome")]
    [InlineData("int M() => Nome.Length + nome.Length;", "field:Nome,field:nome")]
    [InlineData("bool M() => Status == Status.Ativo;", "prop:Status")]
    [InlineData("void M() => _handler();", "field:_handler")]
    [InlineData("void M() => Changed?.Invoke(this, EventArgs.Empty);", "event:Changed")]
    [InlineData("int M() => Herdado;", "")]
    [InlineData("object M() => new List<string> { Nome };", "field:Nome")]
    [InlineData("void M() { Action a = () => Console.WriteLine(Nome); }", "field:Nome")]
    public void AccessedFields_AreAttributesOfTheCurrentObject(string member, string expected)
    {
        var visitor = Code.VisitTypes(AccessClass.Replace("MEMBER", member));

        Assert.Equal(expected.Split(',', StringSplitOptions.RemoveEmptyEntries), Accessed(visitor, "M"));
    }

    [Fact]
    public void AccessedFields_PrimaryConstructorParameter_OnlyWhenNoMemberHidesIt()
    {
        var visitor = Code.VisitTypes("""
            class Repo { public void Save() { } }
            class C(Repo repo, int n)
            {
                private readonly Repo repo = repo;
                void M() { repo.Save(); }
                int N() => n;
                C Clone() => new C(repo: repo, n: 1);
                C(Repo repo, int x, int y) : this(repo, x) { Use(repo); }
                void Use(Repo r) { }
            }
            """);

        Assert.Equal(["field:repo"], Accessed(visitor, "M"));
        Assert.Equal(["param:n"], Accessed(visitor, "N"));
        Assert.Equal(["field:repo"], Accessed(visitor, "Clone"));   // the name in repo: is not an access
        Assert.Empty(Accessed(visitor, "C"));                       // parameter of an ordinary constructor
    }

    [Fact]
    public void AccessedFields_WithoutSemanticModel_MatchNamesWithSyntacticExclusions()
    {
        var visitor = new TypeModelCollector();
        visitor.Visit(Code.Parse("""
            class C
            {
                string Nome;
                object M(Dto d) => new Dto { Nome = "x" };
                int N() => this.Nome.Length + nameof(Nome).Length;
            }
            """).GetRoot());

        Assert.DoesNotContain("Nome", Accessed(visitor, "M"));
        Assert.Equal(["Nome"], Accessed(visitor, "N"));
        Assert.Equal(2, visitor.MethodsWithoutSemanticModel);
    }

    [Fact]
    public void Constructors_AreCollected_IncludingExpressionBodied()
    {
        var visitor = Code.VisitTypes("""
            class A { int _x; A(int x) => _x = x; }
            class B { int _y; B(int y) { _y = y; Init(); } void Init() { } }
            """);

        var ctorA = visitor.GetMethods().Single(m => m.Name == "A");
        var ctorB = visitor.GetMethods().Single(m => m.Name == "B");

        Assert.Contains("field:_x", ctorA.AccessedFields);
        Assert.Contains("field:_y", ctorB.AccessedFields);
        Assert.Equal(["Init"], ctorB.CalledMethods);
        Assert.Equal(["int"], ctorB.ParameterTypes);
    }

    [Fact]
    public void Constructors_IsStatic_ReflectsTheStaticModifier()
    {
        var visitor = Code.VisitTypes("class C { static C() { } C() { } }");

        Assert.Equal([true, false], visitor.GetMethods().Select(m => m.IsStatic));
    }

    [Fact]
    public void Constructors_AreMarked_MethodsAreNot()
    {
        var visitor = Code.VisitTypes("class C { C() { } void M() { } }");

        Assert.True(visitor.GetMethods().Single(m => m.Name == "C").IsConstructor);
        Assert.False(visitor.GetMethods().Single(m => m.Name == "M").IsConstructor);
    }

    [Fact]
    public void CyclomaticComplexity_IsTheVgOfEachMethodAndConstructor()
    {
        var visitor = Code.VisitTypes("""
            abstract class C
            {
                C(int x) { if (x > 0) { } }
                int Sign(int x) => x > 0 ? 1 : 0;
                void Loop() { for (;;) { if (true) { } } }
                abstract void NoBody();
            }
            """);

        var vg = visitor.GetMethods().ToDictionary(m => m.Name, m => m.CyclomaticComplexity);

        Assert.Equal(2, vg["C"]);
        Assert.Equal(2, vg["Sign"]);
        Assert.Equal(3, vg["Loop"]);
        Assert.Equal(1, vg["NoBody"]);   // no body: a single path
    }
}
