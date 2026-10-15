using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Integration;

/// <summary>
/// A class is identified by its project and fully qualified name, not by its simple name:
/// homonymous classes are measured separately and partial declarations are measured as one class.
/// </summary>
public class ClassIdentityTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private const string Csproj = """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>""";

    [Fact]
    public async Task HomonymousClassesInDifferentNamespaces_AreMeasuredSeparately()
    {
        _dir.WriteFile("A/Program.cs", "namespace A; public class Program { void M1() { } }");
        _dir.WriteFile("B/Program.cs", "namespace B; public class Program { void M1() { } void M2() { } void M3() { } }");

        var ck = await Analyzers.Ck(_dir.Path);
        var misra = await Analyzers.Misra(_dir.Path);

        var programs = ck.Where(m => m.ClassName == "Program").OrderBy(m => m.FilePath).ToList();
        Assert.Equal(2, programs.Count);
        Assert.EndsWith("A/Program.cs", programs[0].FilePath);
        Assert.Equal(1, programs[0].WMC);
        Assert.EndsWith("B/Program.cs", programs[1].FilePath);
        Assert.Equal(3, programs[1].WMC);
        Assert.Equal(3, programs[1].LCOM);

        var classRows = misra.Where(m => m.ClassName == "Program" && m.MethodName == "").OrderBy(m => m.FilePath).ToList();
        Assert.Equal(2, classRows.Count);
        Assert.EndsWith("A/Program.cs", classRows[0].FilePath);
        Assert.EndsWith("B/Program.cs", classRows[1].FilePath);
    }

    [Fact]
    public async Task HomonymousMethods_InHomonymousClasses_KeepTheirOwnMc()
    {
        _dir.WriteFile("A/Svc.cs", "namespace A; public class Svc { void Run() { } }");
        _dir.WriteFile("B/Svc.cs", "namespace B; public class Svc { void Run() { if (true) { } for (;;) { } } }");

        var misra = await Analyzers.Misra(_dir.Path);

        var runs = misra.Where(m => m.MethodName == "Run").OrderBy(m => m.FilePath).Select(m => m.MC).ToList();
        Assert.Equal([0, 2 + 3], runs);
    }

    [Fact]
    public async Task SameFullyQualifiedName_InDifferentProjects_AreDifferentClasses()
    {
        _dir.WriteFile("svc1/Svc1.csproj", Csproj);
        _dir.WriteFile("svc1/Startup.cs", "public class Startup { int _a; void A() { _a++; } }");
        _dir.WriteFile("svc2/Svc2.csproj", Csproj);
        _dir.WriteFile("svc2/Startup.cs", "public class Startup { int _a; void A() { _a++; } void B() { } }");

        var ck = await Analyzers.Ck(_dir.Path);
        var misra = await Analyzers.Misra(_dir.Path);

        Assert.Equal([1, 2], ck.Where(m => m.ClassName == "Startup").OrderBy(m => m.FilePath).Select(m => m.WMC));
        Assert.Equal([1, 1], misra.Where(m => m.ClassName == "Startup" && m.MethodName == "").Select(m => m.AC));
    }

    [Fact]
    public async Task PartialClass_IsOneClass_WithTheMembersOfAllDeclarations()
    {
        _dir.WriteFile("Order.cs", "namespace N; public partial class Order { int _total; void Add() { _total++; } }");
        _dir.WriteFile("Order.Rules.cs", "namespace N; public partial class Order { int _items; void Clear() { _items = 0; } void Validate() { _items++; } }");

        var ck = await Analyzers.Ck(_dir.Path);
        var misra = await Analyzers.Misra(_dir.Path);

        var order = Assert.Single(ck, m => m.ClassName == "Order");
        Assert.EndsWith("Order.cs", order.FilePath);   // first declaration
        Assert.Equal(3, order.WMC);
        Assert.Equal(2 - 1, order.LCOM);                // (Clear,Validate) share _items; Add shares nothing

        var orderRow = Assert.Single(misra, m => m.ClassName == "Order" && m.MethodName == "");
        Assert.Equal(2, orderRow.AC);                   // _total + _items
    }

    [Fact]
    public async Task PartialClass_AttributeDeclaredInOneFile_IsSharedByMethodsInTheOther()
    {
        _dir.WriteFile("Cart.cs", "namespace N; public partial class Cart { private int _count; }");
        _dir.WriteFile("Cart.Ops.cs", "namespace N; public partial class Cart { void Add() { _count++; } int Count() => _count; }");

        var ck = await Analyzers.Ck(_dir.Path);

        Assert.Equal(0, Assert.Single(ck, m => m.ClassName == "Cart").LCOM);
    }

    [Fact]
    public async Task Inheritance_IsResolvedAcrossProjects_ThroughProjectReferences()
    {
        _dir.WriteFile("core/Core.csproj", Csproj);
        _dir.WriteFile("core/Entity.cs", "namespace Core; public class Entity { }");
        _dir.WriteFile("api/Api.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup><ProjectReference Include="..\core\Core.csproj" /></ItemGroup>
            </Project>
            """);
        _dir.WriteFile("api/Customer.cs", "namespace Api; public class Customer : Core.Entity { }");

        var ck = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);

        Assert.Equal(2, ck["Customer"].DIT);
        Assert.Equal(1, ck["Entity"].NOC);
    }

    [Fact]
    public async Task Overloads_AreMeasuredSeparately_AndAllCountForTheClass()
    {
        _dir.WriteFile("Repo.cs", """
            public class Repo
            {
                public void Save(int id) { }
                public void Save(string name) { if (name == null) { } for (;;) { } }
                public void Save(ref int id, string name) { }
            }
            """);

        var misra = await Analyzers.Misra(_dir.Path);

        Assert.Equal([0, 2 + 3, 0], misra.Where(m => m.MethodName == "Save").Select(m => m.MC));
        Assert.Equal(0 + 5 + 0, Assert.Single(misra, m => m.ClassName == "Repo" && m.MethodName == "").CLC);
    }

    [Fact]
    public async Task MethodsOfNestedNonClassTypes_DoNotCountForTheEnclosingClass()
    {
        _dir.WriteFile("Outer.cs", """
            public class Outer
            {
                void Own() { }
                public interface IPort { void Send(); }
                public record Message(string Text) { public string Upper() => Text.ToUpper(); }
                public struct Point { public int Sum() => 0; }
            }
            """);

        var ck = await Analyzers.Ck(_dir.Path);

        Assert.Equal(1, Assert.Single(ck, m => m.ClassName == "Outer").WMC);
    }
}
