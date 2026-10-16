using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Integration;

/// <summary>
/// INC-10: CBO matches the referenced types by their resolved key, not by a suffix of the name.
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

    private async Task<List<Core.Models.CkMetric>> Ck()
    {
        _dir.WriteFile("App.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>""");
        _dir.WriteFile("App.cs", App);
        return await Analyzers.Ck(_dir.Path);
    }

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
}
