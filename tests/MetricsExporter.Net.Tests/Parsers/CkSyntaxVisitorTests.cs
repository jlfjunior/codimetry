using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Parsers;

public class CkSyntaxVisitorTests
{
    [Fact]
    public void CollectsClassesWithBaseClassAndModifiers()
    {
        var visitor = Code.VisitCk("""
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
        Assert.Equal("global::N.Base", classes["Derived"].BaseClass);
        Assert.Equal("global::N.Derived", classes["Derived"].FullyQualifiedName);
        Assert.Contains("System.IDisposable", classes["Derived"].ImplementedInterfaces);
    }

    [Fact]
    public void InstanceMemberNames_ExcludeStaticAndConst()
    {
        var visitor = Code.VisitCk("""
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

        Assert.Equal(["_a", "_b", "Name"], dependency.InstanceMemberNames.Order());
    }

    [Fact]
    public void DependentTypes_IncludeFieldPropertyReturnAndParameterTypes()
    {
        var visitor = Code.VisitCk("""
            class C
            {
                private A _a;
                public B B { get; set; }
                public D Get(E e) => default;
            }
            """);

        var dependency = visitor.GetDependencies().Single();

        Assert.Superset(new HashSet<string> { "A", "B", "D", "E" }, dependency.DependentTypes);
        Assert.Equal(["A"], dependency.FieldTypes);
        Assert.Equal(["B"], dependency.PropertyTypes);
    }

    [Fact]
    public void Methods_RecordStaticAbstractAndCalls()
    {
        var visitor = Code.VisitCk("""
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

    // Regression tests for the LCOM fix: accessed fields are matched by name, including
    // the receiver of a call (_logger.Log()) and this.X, but not another object's members.

    [Fact]
    public void AccessedFields_IncludeReceiverOfMemberCall()
    {
        var visitor = Code.VisitCk("class C { Logger _logger; void M() { _logger.Log(); } }");

        Assert.Contains("_logger", visitor.GetMethods().Single().AccessedFields);
    }

    [Fact]
    public void AccessedFields_IncludeThisMemberAccess()
    {
        var visitor = Code.VisitCk("class C { int _count; void M() { this._count++; } }");

        Assert.Contains("_count", visitor.GetMethods().Single().AccessedFields);
    }

    [Fact]
    public void AccessedFields_ExcludeMembersOfOtherObjects()
    {
        var visitor = Code.VisitCk("class C { string Status; bool M(C other) => other.Status == \"x\"; }");

        var accessed = visitor.GetMethods().Single().AccessedFields;

        Assert.Contains("other", accessed);
        Assert.DoesNotContain("Status", accessed);
    }

    [Fact]
    public void Constructors_AreCollected_IncludingExpressionBodied()
    {
        var visitor = Code.VisitCk("""
            class A { int _x; A(int x) => _x = x; }
            class B { int _y; B(int y) { _y = y; Init(); } void Init() { } }
            """);

        var ctorA = visitor.GetMethods().Single(m => m.Name == "A");
        var ctorB = visitor.GetMethods().Single(m => m.Name == "B");

        Assert.Contains("_x", ctorA.AccessedFields);
        Assert.Contains("_y", ctorB.AccessedFields);
        Assert.Equal(["Init"], ctorB.CalledMethods);
        Assert.Equal(["int"], ctorB.ParameterTypes);
    }
}
