using Codimetry.Tests.Helpers;

namespace Codimetry.Tests.Integration;

/// <summary>
/// INC-04: which members enter the class and file sums of V(G) (D18), and how classes are identified.
/// </summary>
public class CyclomaticAggregationTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private static int ClassRow(List<Core.Models.CyclomaticComplexityMetric> metrics, string className) =>
        Assert.Single(metrics, m => m.ClassName == className && m.MethodName == "").Complexity;

    [Fact]
    public async Task AutomaticProperties_DoNotEnterTheClassSum()
    {
        _dir.WriteFile("Dto.cs", """
            public class Dto
            {
                public int A { get; set; }
                public int B { get; set; }
                public int C { get; init; }
                public string D { get; set; } = "";
                public bool E { get; private set; }
                public int M(bool x, bool y) { if (x) return 1; if (y) return 2; return 0; }
            }
            """);

        var metrics = await Analyzers.Cyclomatic(_dir.Path);

        Assert.Equal(3, ClassRow(metrics, "Dto"));
        Assert.Equal(["M", ""], metrics.Where(m => m.ClassName == "Dto").Select(m => m.MethodName));
    }

    [Fact]
    public async Task MembersWithABody_AreMeasured()
    {
        _dir.WriteFile("Money.cs", """
            using System;

            public class Money
            {
                private int _a;
                private readonly int[] _items = new int[3];
                private EventHandler? _changed;

                public Money(int a) => _a = a > 0 ? a : 0;
                ~Money() { if (_a > 0) _a = 0; }

                public int X => _a > 0 ? 1 : 0;
                public int Y { get => _a; init { if (value > 0) _a = value; } }
                public int this[int i] { get => i >= 0 ? _items[i] : 0; set { _items[i] = value; } }

                public event EventHandler? Changed
                {
                    add { if (value != null) _changed += value; }
                    remove { _changed -= value; }
                }

                public static Money operator +(Money l, Money r) => new(l._a + r._a);
                public static implicit operator int(Money m) => m._a > 0 ? m._a : 0;

                public int Run(int[] xs)
                {
                    int Local(int v) => v > 0 ? v : 0;          // local function: counts for Run
                    Func<int, bool> pos = v => v > 0 && v < 10;  // lambda: counts for Run
                    return xs.Length > 0 ? Local(xs[0]) : 0;
                }
            }
            """);

        var metrics = await Analyzers.Cyclomatic(_dir.Path);
        var vg = metrics.Where(m => m.ClassName == "Money" && m.MethodName != "").ToDictionary(m => m.MethodName, m => m.Complexity);

        Assert.Equal(2, vg["Money"]);          // expression-bodied constructor is visited
        Assert.Equal(2, vg["~Money"]);
        Assert.Equal(2, vg["X.get"]);          // int X => ...
        Assert.Equal(1, vg["Y.get"]);
        Assert.Equal(2, vg["Y.init"]);
        Assert.Equal(2, vg["this[].get"]);
        Assert.Equal(1, vg["this[].set"]);
        Assert.Equal(2, vg["Changed.add"]);
        Assert.Equal(1, vg["Changed.remove"]);
        Assert.Equal(1, vg["op_Addition"]);
        Assert.Equal(2, vg["op_Implicit"]);
        Assert.Equal(1 + 1 + 1 + 1, vg["Run"]);   // local ?:, lambda &&, return ?:
        Assert.Equal(vg.Values.Sum(), ClassRow(metrics, "Money"));
    }

    [Fact]
    public async Task PartialClass_IsOneRow_WithTheMembersOfEveryFile()
    {
        _dir.WriteFile("P.cs", "namespace N; public partial class P { int A(bool b) => b ? 1 : 0; }");
        _dir.WriteFile("P.More.cs", "namespace N; public partial class P { int B(int x) { while (x > 0) x--; return x; } }");

        var metrics = await Analyzers.Cyclomatic(_dir.Path);

        var row = Assert.Single(metrics, m => m.ClassName == "P" && m.MethodName == "");
        Assert.Equal(4, row.Complexity);
        Assert.EndsWith("P.cs", row.FilePath);                    // file of the first declaration
        Assert.EndsWith("|N.P", row.ClassKey);
        Assert.All(metrics.Where(m => m.ClassName == "P"), m => Assert.Equal(row.ClassKey, m.ClassKey));

        // File rows sum the members declared in each file: the total is not counted twice
        var fileRows = metrics.Where(m => m.ClassName == "" && m.MethodName == "").ToList();
        Assert.Equal([2, 2], fileRows.Select(m => m.Complexity));
    }

    [Fact]
    public async Task HomonymousNestedClasses_AreDistinctRows()
    {
        _dir.WriteFile("Builders.cs", """
            public class A { public class Builder { int M(bool b) => b ? 1 : 0; } }
            public class B { public class Builder { int M() => 0; } }
            """);

        var metrics = await Analyzers.Cyclomatic(_dir.Path);

        var builders = metrics.Where(m => m.ClassName == "Builder" && m.MethodName == "").OrderBy(m => m.ClassKey).ToList();
        Assert.Equal(2, builders.Count);
        Assert.Equal([2, 1], builders.Select(m => m.Complexity));
        Assert.Equal(["A.Builder", "B.Builder"], builders.Select(m => m.ClassKey.Split('|')[1]));
        Assert.Equal(0, ClassRow(metrics, "A"));
    }

    [Fact]
    public async Task InterfaceMembers_HaveNoClass_AndStayOutOfTheFileSum()
    {
        _dir.WriteFile("Port.cs", """
            public interface IPort
            {
                void Send();
                int Retry(int n) => n > 3 ? 3 : n;
            }

            public class Port : IPort { public void Send() { } }
            """);

        var metrics = await Analyzers.Cyclomatic(_dir.Path);

        var retry = Assert.Single(metrics, m => m.MethodName == "Retry");
        Assert.Equal(("", 2), (retry.ClassName, retry.Complexity));
        Assert.DoesNotContain(metrics, m => m.ClassName == "IPort");
        Assert.Equal(1, metrics.Single(m => m.ClassName == "" && m.MethodName == "").Complexity);
    }
}
