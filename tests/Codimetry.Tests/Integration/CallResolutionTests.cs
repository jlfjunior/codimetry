using Codimetry.Tests.Helpers;

namespace Codimetry.Tests.Integration;

/// <summary>
/// RFC and CWC identify the called method through the semantic model, not by its simple name.
/// </summary>
public class CallResolutionTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private const string Csproj = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
        </Project>
        """;

    [Fact]
    public async Task Rfc_CountsHomonymousMethodsOfDifferentClassesSeparately()
    {
        _dir.WriteFile("app/App.csproj", Csproj);
        _dir.WriteFile("app/Code.cs", """
            public class Orders { public void Save() { } }
            public class Invoices { public void Save() { } }
            public class Service
            {
                private readonly Orders _orders = new();
                private readonly Invoices _invoices = new();
                public void Run() { _orders.Save(); _invoices.Save(); }
            }
            """);

        var ck = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);

        Assert.Equal(1 + 2, ck["Service"].RFC); // Run + Orders.Save + Invoices.Save
    }

    [Fact]
    public async Task Rfc_IsTheUnionOfOwnAndCalledMethods()
    {
        _dir.WriteFile("app/App.csproj", Csproj);
        _dir.WriteFile("app/Code.cs", """
            public class Calculator
            {
                public int Add(int a, int b) => a + b;
                public int Add(int a, int b, int c) => Add(Add(a, b), c);
                public int Twice(int a) => Add(a, a);
            }
            """);

        var ck = Assert.Single(await Analyzers.Ck(_dir.Path));

        Assert.Equal(3, ck.RFC); // the three methods; the calls are to methods already in the set
    }

    [Fact]
    public async Task Cwc_UsesTheMcOfTheMethodActuallyCalled()
    {
        _dir.WriteFile("app/App.csproj", Csproj);
        _dir.WriteFile("app/Code.cs", """
            public class Decoy { public void Process() { if (true) { } for (;;) { } } }   // MC 5
            public class Worker { public void Process() { } }                              // MC 0
            public class Client
            {
                private readonly Worker _worker = new();
                public void Run() { _worker.Process(); }
            }
            """);

        var misra = await Analyzers.Misra(_dir.Path);

        Assert.Equal(0 + 2, Assert.Single(misra, m => m.ClassName == "Client" && m.MethodName == "").CWC);
    }

    [Fact]
    public async Task Cwc_CallThroughInterface_UsesTheSingleImplementation()
    {
        _dir.WriteFile("app/App.csproj", Csproj);
        _dir.WriteFile("app/Code.cs", """
            public interface IRepo { void Save(); }
            public class Repo : IRepo { public void Save() { if (true) { } } }             // MC 2
            public class Client
            {
                private readonly IRepo _repo = new Repo();
                public void Run() { _repo.Save(); }
            }
            """);

        var misra = await Analyzers.Misra(_dir.Path);

        Assert.Equal(2 + 2, Assert.Single(misra, m => m.ClassName == "Client" && m.MethodName == "").CWC);
    }

    [Fact]
    public async Task Cwc_FrameworkCalls_AddNothing_AndNameofIsNotACall()
    {
        _dir.WriteFile("app/App.csproj", Csproj);
        _dir.WriteFile("app/Code.cs", """
            public class Process { public void Save() { } }
            public class Client
            {
                private readonly List<int> _items = new();
                public string Run() { _items.Add(1); Console.WriteLine(); return nameof(Process.Save); }
            }
            """);

        var ck = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);
        var misra = await Analyzers.Misra(_dir.Path);

        Assert.Equal(0, Assert.Single(misra, m => m.ClassName == "Client" && m.MethodName == "").CWC);   // libraries do not count
        Assert.Equal(1 + 2, ck["Client"].RFC); // Run + List.Add + Console.WriteLine
    }
}
