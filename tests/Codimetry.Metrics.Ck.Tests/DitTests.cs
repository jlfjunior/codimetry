using Codimetry.Core.Model;
using Codimetry.Tests.Helpers;

namespace Codimetry.Metrics.Ck.Tests;

/// <summary>
/// DIT counts the declared inheritance edges; System.Object never counts.
/// </summary>
public class DitTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private const string App = """
        using System;

        namespace App;

        public class Entidade { }
        public class Pessoa : Entidade { }
        public class Funcionario : Pessoa { }

        public class X : Exception { }
        public class Y : X { }
        public class C : SystemException { }        // SystemException → Exception
        public class D : ArgumentNullException { }  // ArgumentNullException → ArgumentException → SystemException → Exception
        public class E : object { }
        """;

    private async Task<Dictionary<string, CkMetric>> Ck(DitOptions? options = null)
    {
        // Base classes from the .NET libraries are outside the analysed code, like those of a NuGet package
        _dir.WriteFile("App.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>""");
        _dir.WriteFile("App.cs", App);

        return (await Analyzers.Ck(_dir.Path, ditOptions: options)).ToDictionary(m => m.ClassName);
    }

    [Theory]
    [InlineData("Entidade", 0, 0, 1)]
    [InlineData("Pessoa", 1, 1, 2)]
    [InlineData("Funcionario", 2, 2, 3)]
    [InlineData("X", 1, 1, 1)]           // Exception: its own base is System.Object
    [InlineData("Y", 2, 2, 2)]
    [InlineData("C", 1, 1 + 1, 1)]
    [InlineData("D", 1, 1 + 3, 1)]       // the library ancestors count with --dit-resolve-framework
    [InlineData("E", 0, 0, 1)]           // an explicit object is the same as no base
    public async Task Dit_ByRule(string className, int declared, int resolveFramework, int countObject)
    {
        Assert.Equal(declared, (await Ck())[className].DIT);
        Assert.Equal(resolveFramework, (await Ck(new DitOptions(ResolveFramework: true)))[className].DIT);
        Assert.Equal(countObject, (await Ck(new DitOptions(CountObject: true)))[className].DIT);
    }

    [Fact]
    public async Task Noc_HasNoArtificialObjectRoot()
    {
        var ck = await Ck();

        Assert.Equal(1, ck["Entidade"].NOC);
        Assert.Equal(1, ck["X"].NOC);
        Assert.DoesNotContain("Object", ck.Keys);
    }
}
