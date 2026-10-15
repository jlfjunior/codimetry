using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Parsers;

/// <summary>
/// The attributes behind AC (INC-18): one per instance attribute, whatever its type; static, const,
/// computed and inherited members do not count. Each case runs with and without a semantic model.
/// </summary>
public class InstanceAttributesTests
{
    private static List<string> Attributes(string code, string @class, bool semantic)
    {
        var visitor = semantic ? Code.VisitMisraWithModel(code) : Code.VisitMisra(code);
        // With a semantic model the class key carries the assembly name ("Test|C")
        var key = semantic ? $"Test|{@class}" : @class;
        return visitor.GetClassAttributes().TryGetValue(key, out var attributes) ? attributes : [];
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StaticAndConst_DoNotCount_AndTypeDoesNotMatter(bool semantic)
    {
        var code = """
            class Item { }
            class AcProbe
            {
                const int Max = 1;
                static int Count;
                static int Shared { get; set; }
                private Item _item;
                public int Value { get; set; }
            }
            """;

        Assert.Equal(["_item", "Value"], Attributes(code, "AcProbe", semantic));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ComputedProperty_HoldsNoState(bool semantic)
    {
        var code = "class C { int a, b; public int Total => a + b; public int Twice { get { return 2 * a; } } }";

        Assert.Equal(["a", "b"], Attributes(code, "C", semantic));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PropertyOverAField_IsCountedOnceByTheField(bool semantic)
    {
        var code = "class C { int _x; public int X { get => _x; set => _x = value; } }";

        Assert.Equal(["_x"], Attributes(code, "C", semantic));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FieldLikeEvent_Counts_ButEventWithAccessorsDoesNot(bool semantic)
    {
        var code = """
            class C
            {
                public event System.EventHandler Changed;
                public static event System.EventHandler Global;
                public event System.EventHandler Custom { add { } remove { } }
            }
            """;

        Assert.Equal(["Changed"], Attributes(code, "C", semantic));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AbstractProperty_HoldsNoState(bool semantic)
    {
        Assert.Empty(Attributes("abstract class A { public abstract int X { get; } }", "A", semantic));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InheritedAttributes_DoNotCount(bool semantic)
    {
        var code = "class B { protected int a; } class D : B { int b; }";

        Assert.Equal(["b"], Attributes(code, "D", semantic));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PrimaryConstructorParameter_CountsWhenAMemberUsesIt(bool semantic)
    {
        // x is captured by Get; y only initializes _y, which is the attribute
        var code = "class S(int x, int y) { private readonly int _y = y; public int Get() => x + _y; }";

        Assert.Equal(["_y", "x"], Attributes(code, "S", semantic));
    }

    [Fact]
    public void PrimaryConstructorParameter_ShadowedByALocal_IsNotCaptured()
    {
        var code = "class S(int x) { public int Get() { var x = 1; return x; } }";

        Assert.Empty(Attributes(code, "S", semantic: true));
    }

    [Theory]
    [InlineData("Computer", 2)]
    [InlineData("Hardware", 4)]
    [InlineData("Software", 2)]
    [InlineData("Desktop", 1)]
    [InlineData("Notebook", 1)]
    public void ArticleClasses_CountOnlyTheirOwnAttributes(string @class, int expected)
    {
        Assert.Equal(expected, Attributes(Samples.MisraArticle, @class, semantic: true).Count);
    }

    [Fact]
    public void TypeWeightsMode_CollectsEveryFieldAndProperty_AsV1()
    {
        var code = "class C { const int Max = 1; static int Count; int Total => 0; }";

        Assert.Equal(["Max", "Count", "Total"], Code.VisitMisra(code, acTypeWeights: true).GetClassAttributes()["C"]);
    }
}
