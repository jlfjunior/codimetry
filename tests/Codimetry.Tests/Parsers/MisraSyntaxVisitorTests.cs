using Codimetry.Parsers;
using Codimetry.Tests.Helpers;

namespace Codimetry.Tests.Parsers;

/// <summary>
/// Checks the Basic Control Structure counts behind MC (sequence 1, branch 2, iteration 3, call 2).
/// Asserting each count, not only the total, makes it clear which rule a failure breaks.
/// </summary>
public class MisraSyntaxVisitorTests
{
    private static BcsInfo Bcs(string body, string signature = "void M(int x, bool a, bool b)", bool additiveMc = false)
    {
        var visitor = Code.VisitMisra($$"""
            class C
            {
                {{signature}}
                {
                    {{body}}
                }

                void Other() { }
            }
            """, additiveMc);
        return visitor.GetMethodBcsInfo().Single(kv => kv.Key.StartsWith("C::M(")).Value;
    }

    private static void AssertCounts(BcsInfo bcs, int sequence, int branch, int iteration, int call)
    {
        Assert.Equal(
            (sequence, branch, iteration, call),
            (bcs.SequenceCount, bcs.BranchCount, bcs.IterationCount, bcs.CallCount));
    }

    [Fact]
    public void CalculateMc_AppliesWeights()
    {
        var bcs = new BcsInfo { SequenceCount = 2, BranchCount = 1, IterationCount = 1, CallCount = 3 };

        Assert.Equal(2 * 1 + 1 * 2 + 1 * 3 + 3 * 2, bcs.CalculateMc());
    }

    [Fact]
    public void SequentialStatements()
    {
        AssertCounts(Bcs("var y = x; y++; return;"), sequence: 3, branch: 0, iteration: 0, call: 0);
    }

    [Fact]
    public void CallsAreCountedInsideStatements()
    {
        AssertCounts(Bcs("Other(); var s = x.ToString().Trim();"), sequence: 2, branch: 0, iteration: 0, call: 3);
    }

    [Fact]
    public void Loops_AreIterations()
    {
        var body = """
            for (var i = 0; i < x; i++) { }
            foreach (var c in "abc") { }
            foreach (var (k, v) in new (int, int)[0]) { }
            while (a) { }
            do { } while (b);
            """;

        AssertCounts(Bcs(body), sequence: 0, branch: 0, iteration: 5, call: 0);
    }

    [Fact]
    public void TernaryAndSwitch_AreBranches()
    {
        var body = """
            var y = a ? 1 : 2;
            switch (x)
            {
                case 1: y = 3; break;
                default: y = 4; break;
            }
            """;

        // sequences: var y, y = 3, y = 4 (break is not counted)
        AssertCounts(Bcs(body), sequence: 3, branch: 2, iteration: 0, call: 0);
    }

    // Regression tests for the MC fix: statements nested in blocks used to be ignored.

    [Fact]
    public void NestedStructuresInsideBlocks_AreCounted()
    {
        var body = """
            if (x > 0)
            {
                for (var i = 0; i < x; i++)
                {
                    if (i % 2 == 0)
                        Other();
                }
            }
            """;

        // Composition rule: 2 × (3 × (2 × 1)) = 12; the legacy additive rule gives 1 + 2*2 + 3 + 2 = 10
        var bcs = Bcs(body);
        AssertCounts(bcs, sequence: 1, branch: 2, iteration: 1, call: 1);
        Assert.Equal(12, bcs.CalculateMc());
        Assert.Equal(10, Bcs(body, additiveMc: true).CalculateMc());
    }

    [Fact]
    public void ElseIf_IsOneBranch_AndElseBodiesAreVisited()
    {
        var body = """
            if (x < 0) { return; }
            else if (x == 0) { return; }
            else { return; }
            """;

        // if (1) + else-if (1) + else (1); three return sequences
        AssertCounts(Bcs(body), sequence: 3, branch: 3, iteration: 0, call: 0);
    }

    [Fact]
    public void TryCatchFinally_BodiesAreVisited()
    {
        var body = """
            try { Other(); }
            catch (System.Exception) { Other(); }
            finally { Other(); }
            """;

        AssertCounts(Bcs(body), sequence: 3, branch: 0, iteration: 0, call: 3);
    }

    [Fact]
    public void WrapperStatements_AreNotSequences_ButTheirContentIs()
    {
        var body = """
            { }
            lock (this) { Other(); }
            using (var d = new System.IO.MemoryStream()) { x++; }
            ;
            """;

        // { } and ; count nothing; lock and using are wrappers; Other() and x++ are the sequences
        AssertCounts(Bcs(body), sequence: 2, branch: 0, iteration: 0, call: 1);
    }

    [Fact]
    public void ExpressionBodiedMethod_IsOneSequencePlusItsBranchesAndCalls()
    {
        var visitor = Code.VisitMisra("class C { int M(bool a) => a ? Other() : 0; int Other() => 1; }");

        AssertCounts(visitor.GetMethodBcsInfo()["C::M(bool)"], sequence: 1, branch: 1, iteration: 0, call: 1);
    }

    [Fact]
    public void AbstractMethod_HasNoStructures()
    {
        var visitor = Code.VisitMisra("abstract class C { public abstract void M(); }");

        Assert.Equal(0, visitor.GetMethodBcsInfo()["C::M()"].CalculateMc());
    }

    [Fact]
    public void Constructors_AreMeasured_IncludingExpressionBodied()
    {
        var visitor = Code.VisitMisra("""
            class A { int _x; A(int x) => _x = x; }
            class B { int _y; B(int y) { _y = y; Init(); } void Init() { } }
            """);

        AssertCounts(visitor.GetMethodBcsInfo()["A::A(int)"], sequence: 1, branch: 0, iteration: 0, call: 0);
        AssertCounts(visitor.GetMethodBcsInfo()["B::B(int)"], sequence: 2, branch: 0, iteration: 0, call: 1);
    }

    [Fact]
    public void Attributes_AreCollectedWithTheirTypes()
    {
        var visitor = Code.VisitMisra("""
            class C
            {
                private int _a, _b;
                public Address Home { get; set; }
            }
            class Empty { }
            """);

        Assert.Equal(["_a", "_b", "Home"], visitor.GetClassAttributes()["C"]);
        Assert.Equal(["int", "int", "Address"], visitor.GetClassAttributeTypes()["C"]);
        Assert.False(visitor.GetClassAttributes().ContainsKey("Empty"));
    }

    [Fact]
    public void NestedClasses_KeepTheirOwnMethodKeys()
    {
        var visitor = Code.VisitMisra("""
            class Outer
            {
                class Inner { void M() { } }
                void N() { }
            }
            """);

        Assert.True(visitor.GetMethodBcsInfo().ContainsKey("Outer.Inner::M()"));
        Assert.True(visitor.GetMethodBcsInfo().ContainsKey("Outer::N()"));
    }
}
