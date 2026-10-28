using Codimetry.Metrics.Ck;
using Codimetry.Tests.Helpers;

namespace Codimetry.Tests.Parsers;

public class CouplingCollectorTests
{
    [Fact]
    public void InstanceMemberNames_ExcludeStaticAndConst()
    {
        var visitor = Code.VisitCoupling("""
            class C
            {
                private int _a, _b;
                private static int _shared;
                private const int Max = 1;
                public string Name { get; set; } = "";
                public static string Default { get; } = "";
            }
            """);

        var dependency = visitor.GetDependencies().Single();

        Assert.Equal(["field:_a", "field:_b", "prop:Name"], dependency.InstanceMemberNames.Order());
    }

    [Fact]
    public void InstanceMemberNames_IncludeFieldLikeEventsAndPrimaryConstructorParameters()
    {
        var visitor = Code.VisitCoupling("""
            using System;
            class C(int n)
            {
                public event EventHandler? Changed;
                public static event EventHandler? Shared;
            }
            """);

        var dependency = visitor.GetDependencies().Single();

        Assert.Equal(["event:Changed", "param:n"], dependency.InstanceMemberNames.Order());
    }

    [Fact]
    public void InstanceMemberNames_WithoutSemanticModel_AreNames()
    {
        var visitor = new CouplingCollector();
        visitor.Visit(Code.Parse("class C { int _a; public string Name { get; set; } }").GetRoot());

        Assert.Equal(["_a", "Name"], visitor.GetDependencies().Single().InstanceMemberNames.Order());
    }

    [Fact]
    public void DependentTypes_IncludeFieldPropertyReturnAndParameterTypes()
    {
        var visitor = Code.VisitCoupling("""
            class A { } class B { } class D { } class E { }
            class C
            {
                private A _a;
                public B B { get; set; }
                public D Get(E e) => default;
            }
            """);

        var dependency = visitor.GetDependencies().Single(d => d.ClassName == "C");

        Assert.Superset(new HashSet<string> { "Test|A", "Test|B", "Test|D", "Test|E" }, dependency.DependentTypes);
        Assert.Empty(dependency.UnresolvedTypeNames);
        Assert.Equal(["A"], dependency.FieldTypes);
        Assert.Equal(["B"], dependency.PropertyTypes);
    }

    [Fact]
    public void DependentTypes_DecomposeGenericArgumentsArraysAndNullables()
    {
        var visitor = Code.VisitCoupling("""
            using System.Collections.Generic;
            class Item { }
            struct Point { }
            class C<T>
            {
                private Dictionary<string, List<Item[]>> _map;
                private Point? _p;
                private T _t;
            }
            """);

        var dependency = visitor.GetDependencies().Single(d => d.ClassName == "C");

        Assert.Contains("Test|Item", dependency.DependentTypes);
        Assert.Contains("Test|Point", dependency.DependentTypes);
        Assert.DoesNotContain(dependency.DependentTypes, k => k.EndsWith("|T"));
    }

    [Fact]
    public void DependentTypes_WithoutSemanticModel_RecordSimpleNames()
    {
        var visitor = new CouplingCollector();
        visitor.Visit(Code.Parse("class C { private N.Repo<Item>[] _r; public global::X.Logger? L { get; set; } }").GetRoot());

        var dependency = visitor.GetDependencies().Single();

        Assert.Empty(dependency.DependentTypes);
        Assert.Equal(["Item", "Logger", "Repo"], dependency.UnresolvedTypeNames.Order());
    }

    private static int Lcom(string code)
    {
        var types = Code.VisitTypes(code);
        var coupling = Code.VisitCoupling(code);
        var key = types.GetClasses().Single(c => c.Name == "C").ClassKey!;
        return new LcomCalculator().CalculateLcom(key, types.GetMethods(), ClassDependency.MergeByClass(coupling.GetDependencies()));
    }

    [Theory]
    [InlineData("class C { string Nome; void A() { var Nome = 1; } int B() => Nome.Length; }", 1)]
    [InlineData("class C { string Nome; int A() => this.Nome.Length; int B() => Nome.Length; }", 0)]
    [InlineData("class Repo { public void Save() { } } class C(Repo repo) { void A() => repo.Save(); void B() => repo.Save(); }", 0)]
    [InlineData("class C { System.Action _h; void A() => _h(); void B() => _h(); }", 0)]
    [InlineData("class C { int a, b; C() { a = 1; b = 2; } int M1() => a; int M2() => b; }", 1)]  // the constructor does not enter
    [InlineData("class C(int a, int b) { int M1() => a; int M2() => b; }", 1)]                       // primary constructor never enters
    public void Lcom_CountsSharedAttributesOfTheCurrentObject(string code, int expected)
    {
        Assert.Equal(expected, Lcom(code));
    }
}
