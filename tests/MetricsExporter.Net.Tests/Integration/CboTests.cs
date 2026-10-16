using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Integration;

/// <summary>
/// INC-10: CBO matches the referenced types by their resolved key, not by a suffix of the name.
/// INC-11: every form of type reference couples, interfaces of the analysed code included (D8); outgoing by
/// default, both directions with --cbo-bidirectional (D9).
/// </summary>
public class CboTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private const string App = """
        using System.Collections.Generic;
        using System.Threading.Tasks;

        namespace A
        {
            public class Pedido { }

            public class BackgroundTask { }
            public class PurchaseOrder { }
            public class Order { }

            public class CboSuffix
            {
                public Task Run() => Task.CompletedTask;
            }

            public class Item { }

            public class Lista
            {
                private List<Item> _items = new();
            }

            public class Aninhado
            {
                public Dictionary<string, List<Item[]>>? Mapa { get; set; }
            }

            public class Cliente
            {
                private A.Pedido _pedido = new();
            }
        }

        namespace B
        {
            public class Pedido { }
        }
        """;

    private const string Forms = """
        using System;
        using System.Collections.Generic;

        namespace F;

        // Spec probe: primary constructor parameter, generic argument, new, static call
        public class Repo { public void Save() { } }
        public class Helper { public static string Fmt() => ""; }
        public class Item { }
        public class CboForms(Repo injected)
        {
            private List<Item> items = new();
            public void Go() { var r = new Repo(); r.Save(); Helper.Fmt(); injected.Save(); }
        }

        public interface IRepo { void Save(); }
        public class ComInterface { private IRepo _repo; }

        public class DominioException : Exception { }
        public class ComCatch
        {
            public void Run() { try { } catch (DominioException) { } }
        }

        // One target per form of reference
        public class Base { }
        public class Local { }
        public class Pattern { }
        public class TypeOf { }
        public class Cast { }
        public class Config { public static int Value = 1; }
        public class FilterAttribute : Attribute { }
        public class Element { }
        public class Lambda { }
        public class Formas : Base
        {
            [Filter]
            public void Run(object o)
            {
                Local l = null;
                if (o is Pattern p) { }
                var t = typeof(TypeOf);
                var c = (Cast)o;
                var v = Config.Value;
                var e = new List<Element[]>();
                Func<Lambda, int> f = x => 0;
            }
        }

        // Inherited members are the class's own; nested types are distinct types
        public class Herdeira : Base
        {
            public class Aninhada { private Item _item; }
        }
        """;

    private async Task<List<Core.Models.CkMetric>> Ck(string code = App, CboOptions? options = null)
    {
        _dir.WriteFile("App.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>""");
        _dir.WriteFile("App.cs", code);
        return await Analyzers.Ck(_dir.Path, cboOptions: options);
    }

    private static int Cbo(List<Core.Models.CkMetric> metrics, string className) =>
        metrics.Single(m => m.ClassName == className).CBO;

    [Fact]
    public async Task FrameworkTypeDoesNotCoupleToClassWhoseNameEndsWithIt()
    {
        var metrics = await Ck();

        Assert.Equal(0, metrics.Single(m => m.ClassName == "CboSuffix").CBO);
    }

    [Fact]
    public async Task GenericArgumentsAndArrayElementsAreCoupled()
    {
        var metrics = await Ck();

        Assert.Equal(1, metrics.Single(m => m.ClassName == "Lista").CBO);
        Assert.Equal(1, metrics.Single(m => m.ClassName == "Aninhado").CBO);
    }

    [Fact]
    public async Task HomonymousClassesAreNotConfused()
    {
        var metrics = await Ck();

        Assert.Equal(1, metrics.Single(m => m.ClassName == "Cliente").CBO);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SpecProbe_CountsPrimaryConstructorGenericArgumentNewAndStaticCall(bool bidirectional)
    {
        var metrics = await Ck(Forms, new CboOptions(Bidirectional: bidirectional));

        Assert.Equal(3, Cbo(metrics, "CboForms"));   // Repo, Item, Helper
    }

    [Fact]
    public async Task InterfaceOfTheAnalysedCode_Couples()
    {
        var metrics = await Ck(Forms);

        Assert.Equal(1, Cbo(metrics, "ComInterface"));
    }

    [Fact]
    public async Task CaughtException_Couples()
    {
        var metrics = await Ck(Forms);

        Assert.Equal(1, Cbo(metrics, "ComCatch"));
    }

    [Fact]
    public async Task EveryFormOfReference_Couples()
    {
        var metrics = await Ck(Forms);

        // Base, Local, Pattern, TypeOf, Cast, Config, FilterAttribute, Element, Lambda
        Assert.Equal(9, Cbo(metrics, "Formas"));
    }

    [Fact]
    public async Task InheritedMembersDoNotCouple_AndNestedTypesAreSeparate()
    {
        var metrics = await Ck(Forms);

        Assert.Equal(1, Cbo(metrics, "Herdeira"));    // Base; the field of Aninhada is not Herdeira's
        Assert.Equal(1, Cbo(metrics, "Aninhada"));    // Item
    }

    [Theory]
    [InlineData(false, 1, 2)]
    [InlineData(true, 1, 4)]
    public async Task MisraArticle_DesktopAndHardware(bool bidirectional, int desktop, int hardware)
    {
        var metrics = await Ck(Samples.MisraArticle, new CboOptions(Bidirectional: bidirectional));

        Assert.Equal(desktop, Cbo(metrics, "Desktop"));     // Hardware
        Assert.Equal(hardware, Cbo(metrics, "Hardware"));   // Computer, Software (+ Desktop, Notebook)
    }

    [Fact]
    public async Task DeclaredTypesOnly_ReproducesV1_WithSymbolMatching()
    {
        var metrics = await Ck(Forms, new CboOptions(DeclaredTypesOnly: true));

        Assert.Equal(1, Cbo(metrics, "CboForms"));       // only the field List<Item>
        Assert.Equal(0, Cbo(metrics, "ComInterface"));   // interfaces are not targets
        Assert.Equal(0, Cbo(metrics, "Formas"));
    }
}
