using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Services;

namespace MetricsExporter.Net.Tests.Services;

public class CkMetricsCalculatorsTests
{
    private static CkMethodInfo Method(string name, string @class = "C", bool isStatic = false,
        string[]? calls = null, string[]? fields = null, bool isConstructor = false) => new()
    {
        Name = name,
        Key = name,
        ContainingClass = @class,
        ContainingClassKey = @class,
        IsStatic = isStatic,
        IsConstructor = isConstructor,
        CalledMethods = [.. calls ?? []],
        AccessedFields = [.. fields ?? []]
    };

    private static ClassDependency Dependency(string @class, params string[] instanceMembers) => new()
    {
        ClassName = @class,
        ClassKey = @class,
        InstanceMemberNames = [.. instanceMembers]
    };

    // ---------- WMC ----------

    [Fact]
    public void Wmc_CountsNonStaticMethodsOfTheClass()
    {
        List<CkMethodInfo> methods = [Method("A"), Method("B"), Method("S", isStatic: true), Method("X", @class: "Other")];

        Assert.Equal(2, new WmcCalculator().CalculateWmc("C", methods));
    }

    // ---------- RFC ----------

    [Fact]
    public void Rfc_IsMethodsPlusDistinctCalledNames()
    {
        List<CkMethodInfo> methods =
        [
            Method("A", calls: ["Save", "Log"]),
            Method("B", calls: ["Save", "Send"]),
            Method("S", isStatic: true),
            Method("X", @class: "Other", calls: ["Ignored"])
        ];

        Assert.Equal(3 + 3, new RfcCalculator().CalculateRfc("C", methods));
    }

    // ---------- LCOM ----------

    [Fact]
    public void Lcom_IsPMinusQ_WhenPGreaterThanQ()
    {
        // (A,B) share x -> Q; (A,C), (B,C) share nothing -> P
        List<CkMethodInfo> methods = [Method("A", fields: ["x"]), Method("B", fields: ["x", "y"]), Method("C")];

        Assert.Equal(2 - 1, new LcomCalculator().CalculateLcom("C", methods, [Dependency("C", "x", "y")]));
    }

    [Fact]
    public void Lcom_IsZero_WhenQAtLeastP()
    {
        List<CkMethodInfo> methods = [Method("A", fields: ["x"]), Method("B", fields: ["x"])];

        Assert.Equal(0, new LcomCalculator().CalculateLcom("C", methods, [Dependency("C", "x")]));
    }

    [Fact]
    public void Lcom_IsZero_ForSingleMethod()
    {
        Assert.Equal(0, new LcomCalculator().CalculateLcom("C", [Method("A")], [Dependency("C", "x")]));
    }

    [Fact]
    public void Lcom_IgnoresStaticMethods()
    {
        List<CkMethodInfo> methods = [Method("A", fields: ["x"]), Method("S", isStatic: true)];

        Assert.Equal(0, new LcomCalculator().CalculateLcom("C", methods, [Dependency("C", "x")]));
    }

    [Fact]
    public void Lcom_IgnoresConstructors()
    {
        // Without the constructor: (A,B) share nothing -> P = 1, Q = 0
        List<CkMethodInfo> methods =
        [
            Method("C", isConstructor: true, fields: ["a", "b"]),
            Method("A", fields: ["a"]),
            Method("B", fields: ["b"])
        ];

        Assert.Equal(1, new LcomCalculator().CalculateLcom("C", methods, [Dependency("C", "a", "b")]));
    }

    [Fact]
    public void Lcom_OnlyCountsAccessesToInstanceMemberNames()
    {
        // Regression: LCOM used to compare accessed names against field *types*.
        // Locals/parameters ("value") and type names ("string") must not create sharing.
        List<CkMethodInfo> methods =
        [
            Method("A", fields: ["_name", "value", "string"]),
            Method("B", fields: ["_name"]),
            Method("C", fields: ["value", "string"])
        ];

        // (A,B) share _name -> Q = 1; (A,C), (B,C) -> P = 2
        Assert.Equal(1, new LcomCalculator().CalculateLcom("C", methods, [Dependency("C", "_name")]));
    }

    // ---------- CBO ----------

    [Fact]
    public void Cbo_CountsOnlyAnalysedClasses_NormalisingTypeNames()
    {
        var builder = new ClassRelationshipBuilder();
        foreach (var name in new[] { "C", "Repo", "Logger", "Item" })
        {
            builder.AddClass(new CkCodeElement { Name = name, FullyQualifiedName = $"N.{name}" }, "f.cs");
        }

        var dependency = new ClassDependency
        {
            ClassName = "C",
            ClassKey = "N.C",
            DependentTypes = ["Repo<Item>", "N.Logger", "Item[]", "Logger?", "string", "List<int>", "C", "void"]
        };

        // Repo, Logger, Item; framework types and the class itself are excluded
        Assert.Equal(3, new CboCalculator().CalculateCbo(dependency, builder.GetAllClasses()));
    }

    // ---------- DIT / NOC ----------

    private static ClassRelationshipBuilder Hierarchy()
    {
        var builder = new ClassRelationshipBuilder();
        builder.AddClass(new CkCodeElement { Name = "Entity", FullyQualifiedName = "Entity", BaseClass = "object" }, "f.cs");
        builder.AddClass(new CkCodeElement { Name = "Person", FullyQualifiedName = "Person", BaseClass = "Entity" }, "f.cs");
        builder.AddClass(new CkCodeElement { Name = "Employee", FullyQualifiedName = "Employee", BaseClass = "Person" }, "f.cs");
        builder.AddClass(new CkCodeElement { Name = "Customer", FullyQualifiedName = "Customer", BaseClass = "Person" }, "f.cs");
        builder.AddClass(new CkCodeElement { Name = "Standalone", FullyQualifiedName = "Standalone" }, "f.cs");
        return builder;
    }

    [Theory]
    [InlineData("Standalone", 0)] // no base resolved at all
    [InlineData("Entity", 1)]     // object counts as a level
    [InlineData("Person", 2)]
    [InlineData("Employee", 3)]
    public void Dit_CountsLevelsToTheRoot(string className, int expected)
    {
        Assert.Equal(expected, new DitCalculator(Hierarchy()).CalculateDit(className));
    }

    [Theory]
    [InlineData("Person", 2)]
    [InlineData("Entity", 1)]
    [InlineData("Employee", 0)]
    public void Noc_CountsDirectChildrenOnly(string className, int expected)
    {
        Assert.Equal(expected, new NocCalculator(Hierarchy()).CalculateNoc(className));
    }
}
