using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Services;
using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Integration;

/// <summary>
/// INC-06: constructors called by <c>new</c> and <c>: base(...)</c> enter the response set R (D20); an unresolved
/// call to a method of the class does not enter R again.
/// </summary>
public class RfcTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private const string App = """
        namespace App;

        // Spec probe
        public class Repo { public void Save() { } }
        public class Helper { public static string Fmt() => ""; }
        public class CboForms
        {
            public void Go() { var r = new Repo(); r.Save(); Helper.Fmt(); }
        }

        public class A
        {
            public A() { }
            public A(int x) { }
            public void Run() { }
        }

        public class TwoNews { void Go() { var a = new A(); var b = new A(); } }
        public class TargetTyped { void Go() { A a = new(); a.Run(); } }
        public class B : A { B() : base(1) { } }
        public class C { C() : this(0) { } C(int x) { } }
        public class S { static S Make() => new S(); }
        public class Dto { public string Nome { get; set; } = ""; }
        public class ReadsProperty { string Get(Dto other) => other.Nome; }
        public class Generic { void Go<T>() where T : new() { new T(); } }
        public class DelegateCreation { void Go() { var f = new System.Action(Go); } }
        """;

    private async Task<Dictionary<string, int>> Rfc(bool excludeConstructors = false)
    {
        _dir.WriteFile("App.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>""");
        _dir.WriteFile("App.cs", App);
        return (await Analyzers.Ck(_dir.Path, rfcExcludeConstructors: excludeConstructors)).ToDictionary(m => m.ClassName, m => m.RFC);
    }

    [Theory]
    [InlineData("CboForms", 4)]          // Go, Repo..ctor, Save, Fmt
    [InlineData("TwoNews", 2)]           // Go, A..ctor (once)
    [InlineData("TargetTyped", 3)]       // Go, A..ctor, Run
    [InlineData("B", 2)]                 // B(), A(int)
    [InlineData("C", 2)]                 // this(0) calls a constructor already in M
    [InlineData("S", 2)]                 // Make, the implicit S..ctor
    [InlineData("ReadsProperty", 1)]     // property accessors are not methods of R
    [InlineData("Generic", 1)]           // new T() has no concrete constructor
    [InlineData("DelegateCreation", 1)]  // new Action(Go) runs no constructor of the analysed code
    public async Task ConstructorsEnterTheResponseSet(string className, int rfc)
    {
        Assert.Equal(rfc, (await Rfc())[className]);
    }

    [Theory]
    [InlineData("CboForms", 3)]
    [InlineData("B", 1)]
    [InlineData("S", 1)]
    public async Task ExcludeConstructors_ReproducesV1(string className, int rfc)
    {
        Assert.Equal(rfc, (await Rfc(excludeConstructors: true))[className]);
    }

    private static int RfcWithoutModel(string code)
    {
        var visitor = new CkSyntaxVisitor();
        visitor.Visit(Code.Parse(code).GetRoot());
        var methods = visitor.GetMethods().ToList();
        return new RfcCalculator().CalculateRfc(methods[0].ContainingClassKey!, methods);
    }

    [Fact]
    public void WithoutModel_CallToOwnMethodIsNotCountedTwice()
    {
        Assert.Equal(2, RfcWithoutModel("class K { void A() { B(); } void B() { } }"));
    }

    [Fact]
    public void WithoutModel_CreationEntersByTypeName()
    {
        Assert.Equal(2, RfcWithoutModel("class K { void Go() { new Repo(); new K(); new(); } }"));   // Go, ctor:Repo
    }

    [Fact]
    public void WithoutModel_CreationOfTypeParameterIsIgnored()
    {
        Assert.Equal(1, RfcWithoutModel("class K { void Go<T>() where T : new() { new T(); } }"));
    }
}
