using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Integration;

/// <summary>
/// Service-level behaviour that the documentation describes in prose: which members get rows,
/// and how class-, file- and codebase-level rows are aggregated.
/// </summary>
public class AnalysisServicesTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private const string Account = """
        public abstract class Base
        {
            public abstract void Run();
        }

        public class Account : Base
        {
            private decimal _balance;

            public Account(decimal initial)
            {
                if (initial < 0)
                    throw new System.ArgumentException();
                _balance = initial;
            }

            public decimal Balance
            {
                get { return _balance > 0 ? _balance : 0; }
                set { if (value >= 0) _balance = value; }
            }

            public int Count { get; set; }

            public override void Run() { }
        }
        """;

    // ---------- LOC ----------

    [Fact]
    public async Task Loc_MeasuresMethodsAndConstructors_ClassAndFileSumThem()
    {
        _dir.WriteFile("Account.cs", Account);
        _dir.WriteFile("Other.cs", "public class Other { public void M() { } }");

        var metrics = await Analyzers.Loc(_dir.Path);
        var account = metrics.Where(m => m.ClassName == "Account").ToDictionary(m => m.MethodName);

        Assert.Equal(6, account["Account"].Loc); // constructor: signature, braces, if, throw, assignment
        Assert.Equal(1, account["Run"].Loc);
        Assert.False(account.ContainsKey("Balance")); // properties are not measured
        Assert.Equal(7, account[""].Loc);           // class row = sum of its methods only
        Assert.Equal(1, metrics.Single(m => m.ClassName == "Base" && m.MethodName == "Run").Loc); // abstract declarations are counted
        Assert.Equal(1 + 7, metrics.Single(m => m.FilePath.EndsWith("Account.cs") && m.ClassName == "").Loc);
        Assert.Equal(1, metrics.Single(m => m.FilePath.EndsWith("Other.cs") && m.ClassName == "").Loc);
    }

    [Fact]
    public async Task Lloc_ClassAndFileAreMeasuredOnTheirNodes()
    {
        _dir.WriteFile("Account.cs", Account);

        var metrics = await Analyzers.Loc(_dir.Path);
        var account = metrics.Where(m => m.ClassName == "Account").ToDictionary(m => m.MethodName);

        Assert.Equal(4, account["Account"].Lloc); // declaration, if, throw, assignment
        Assert.Equal(1, account["Run"].Lloc);
        // class, field, constructor (4), Balance (property 1 + get 2 + set 3), Count, Run
        Assert.Equal(1 + 1 + 4 + 6 + 1 + 1, account[""].Lloc);
        Assert.Equal(2 + 14, metrics.Single(m => m.ClassName == "" && m.MethodName == "").Lloc);
    }

    [Fact]
    public async Task Loc_EveryFileGetsAFileRow_EvenWithoutClasses()
    {
        _dir.WriteFile("Dto.cs", "public record Dto(string Nome);\npublic enum E { A }\n");
        _dir.WriteFile("Comments.cs", "// one\n// two\n// three\n");

        var metrics = await Analyzers.Loc(_dir.Path);
        var dto = metrics.Single(m => m.FilePath.EndsWith("Dto.cs") && m.ClassName == "");
        var comments = metrics.Single(m => m.FilePath.EndsWith("Comments.cs"));

        Assert.Equal((0, 1 + 1 + 1, 2), (dto.Loc, dto.Lloc, dto.PhysicalLoc));
        Assert.Equal((0, 0, 3), (comments.Loc, comments.Lloc, comments.PhysicalLoc));
    }

    [Fact]
    public async Task Loc_NestedClass_IsCountedOnceInTheFileRow()
    {
        _dir.WriteFile("Outer.cs", "class Outer { class Inner { void M() { } } }");

        var metrics = await Analyzers.Loc(_dir.Path);

        Assert.Equal(3, metrics.Single(m => m.ClassName == "Outer").Lloc);
        Assert.Equal(2, metrics.Single(m => m.ClassName == "Inner" && m.MethodName == "").Lloc);
        Assert.Equal(3, metrics.Single(m => m.ClassName == "" && m.MethodName == "").Lloc);
    }

    [Fact]
    public async Task Loc_NonRecursive_IgnoresSubdirectories()
    {
        _dir.WriteFile("Top.cs", "public class Top { public void M() { } }");
        _dir.WriteFile("Sub/Deep.cs", "public class Deep { public void M() { } }");

        var metrics = await Analyzers.Loc(_dir.Path, recursive: false);

        Assert.DoesNotContain(metrics, m => m.ClassName == "Deep");
        Assert.Contains(metrics, m => m.ClassName == "Top");
    }

    // ---------- Cyclomatic ----------

    [Fact]
    public async Task Cyclomatic_MeasuresConstructorsAndAccessors_SkipsAbstractMethods()
    {
        _dir.WriteFile("Account.cs", Account);

        var metrics = await Analyzers.Cyclomatic(_dir.Path);
        var account = metrics.Where(m => m.ClassName == "Account").ToDictionary(m => m.MethodName);

        Assert.Equal(2, account["Account"].Complexity);
        Assert.Equal(2, account["Balance.get"].Complexity);
        Assert.Equal(2, account["Balance.set"].Complexity);
        Assert.Equal(1, account["Count.get"].Complexity);
        Assert.Equal(1, account["Count.set"].Complexity);
        Assert.Equal(1, account["Run"].Complexity);
        Assert.Equal(2 + 2 + 2 + 1 + 1 + 1, account[""].Complexity);
        Assert.DoesNotContain(metrics, m => m.ClassName == "Base" && m.MethodName == "Run");
    }

    [Fact]
    public async Task Cyclomatic_FileRow_SumsAllClassesInTheFile()
    {
        _dir.WriteFile("Two.cs", """
            public class A { public int M(bool b) => b ? 1 : 0; }
            public class B { public int N(int x) { while (x > 0) x--; return x; } }
            """);

        var metrics = await Analyzers.Cyclomatic(_dir.Path);

        Assert.Equal(4, metrics.Single(m => m.ClassName == "" && m.MethodName == "").Complexity);
    }

    // ---------- CK ----------

    [Fact]
    public async Task Ck_ResolvesInheritanceAcrossFiles()
    {
        _dir.WriteFile("Base.cs", "namespace N; public class Base { }");
        _dir.WriteFile("Child.cs", "namespace N; public class Child : Base { }");

        var metrics = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);

        Assert.Equal(2, metrics["Child"].DIT);
        Assert.Equal(1, metrics["Base"].NOC);
        Assert.EndsWith("Child.cs", metrics["Child"].FilePath);
    }

    // ---------- Misra ----------

    [Fact]
    public async Task Misra_ProducesMethodClassAndSingleCodebaseRow()
    {
        _dir.WriteFile("A.cs", "public class A { int _x; public void M() { _x++; } }");

        var metrics = await Analyzers.Misra(_dir.Path);

        var method = Assert.Single(metrics, m => m.MethodName == "M");
        var @class = Assert.Single(metrics, m => m.ClassName == "A" && m.MethodName == "");
        var codebase = Assert.Single(metrics, m => m.ClassName == "" && m.MethodName == "");

        Assert.Equal(1, method.MC);
        Assert.Equal((1, 2, 0), (@class.AC, @class.CLC, @class.CWC));
        Assert.Equal(2, codebase.CC);
    }

    [Fact]
    public async Task Misra_MethodWithoutBody_HasNoMcRow_ExceptInAdditiveMode()
    {
        _dir.WriteFile("A.cs", "public abstract class A { public abstract void M(); public void N() { } }");

        var composed = await Analyzers.Misra(_dir.Path);
        var additive = await Analyzers.Misra(_dir.Path, additiveMc: true);

        Assert.DoesNotContain(composed, m => m.MethodName == "M");
        Assert.Equal(0, Assert.Single(composed, m => m.MethodName == "N").MC);
        Assert.Equal(0, Assert.Single(additive, m => m.MethodName == "M").MC);
    }
}
