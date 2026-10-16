using Codimetry.Parsers;
using Codimetry.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codimetry.Tests.Parsers;

/// <summary>
/// MC by the composition rule of Eq. 23 (INC-14): sum per layer, product along nesting, one sequence
/// per layer. The calculator is used here without a call weight, so the expected values leave calls out;
/// the weight of calls (INC-16) is covered by McCallsTests.
/// </summary>
public class MethodComplexityCalculatorTests
{
    private static int? Mc(string body, string signature = "void M(bool a, bool b, bool c, bool d, int[] l)",
        McLambdaRule lambdas = McLambdaRule.Full) =>
        MethodComplexityCalculator.Calculate(Code.Method($$"""
            class C
            {
                {{signature}}
                {
                    {{body}}
                }

                void x() { }
                void y() { }
            }
            """, "M"), lambdas: lambdas);

    private static int? Article(string name) =>
        MethodComplexityCalculator.Calculate(
            Code.Single<BaseMethodDeclarationSyntax>(Samples.MisraArticle, m => m switch
            {
                MethodDeclarationSyntax method => method.Identifier.Text == name,
                ConstructorDeclarationSyntax constructor => constructor.Identifier.Text == name,
                _ => false
            }));

    // ---------- Examples of Cap. 2 and of the article ----------

    [Fact]
    public void Process_MultipliesNestedLayers()
    {
        var body = """
            if (a)
            {
                for (var i = 0; i < 10; i++)
                {
                    if (b)
                        System.Console.WriteLine("Even");
                }
            }
            """;

        Assert.Equal(2 * (3 * (2 * 1)), Mc(body));
    }

    [Fact]
    public void CalcularDesconto_StructurePlusOneSequence()
    {
        Assert.Equal((2 * 1) + 1, Mc("if (a) return 10; return 0;", "int M(bool a)"));
    }

    [Theory]
    [InlineData("Software", 1 + (3 * 1))]
    [InlineData("IsAppropriate", (3 * (2 * 1)) + 1)]
    [InlineData("Desktop", 1)]
    [InlineData("Notebook", 1)]
    [InlineData("GetName", 1)]
    public void ArticleMethods(string name, int expected)
    {
        Assert.Equal(expected, Article(name));
    }

    [Fact]
    public void CheckSupportedSw_WithoutTheCallWeight()
    {
        // 1 + (2 × 1); with the call graph, the call to IsAppropriate adds 2 + 7, giving the article's 12
        Assert.Equal(1 + (2 * 1), Article("CheckSupportedSw"));
    }

    // ---------- Acceptance criteria of INC-14 ----------

    [Fact]
    public void SimpleStatementsOfALayer_AreOneSequence()
    {
        Assert.Equal(2 + 1, Mc("x(); if (c) { x(); } y();"));
    }

    [Fact]
    public void StructuresInTheSameInnerLayer_AreSummedThenMultiplied()
    {
        Assert.Equal(2 * (2 + 1), Mc("if (c) { x(); if (d) y(); }"));
        Assert.Equal(3 * (2 + 2), Mc("for (var i = 0; i < 3; i++) { if (a) x(); if (b) y(); }"));
    }

    [Fact]
    public void TryCatch_IsTransparent()
    {
        Assert.Equal(1, Mc("try { x(); } catch { y(); }"));
    }

    [Fact]
    public void EmptyBody_IsZero()
    {
        Assert.Equal(0, Mc(""));
    }

    [Fact]
    public void ExpressionBody_IsOneSimpleStatement()
    {
        var method = Code.Method("class C { int M() => 1; }", "M");

        Assert.Equal(1, MethodComplexityCalculator.Calculate(method));
    }

    [Fact]
    public void MethodWithoutBody_HasNoMc()
    {
        var method = Code.Method("abstract class C { public abstract void M(); }", "M");

        Assert.Null(MethodComplexityCalculator.Calculate(method));
    }

    // ---------- Rules for C# constructs ----------

    [Fact]
    public void IfElse_IsOneBranch_WithBothBranchesInTheInnerLayer()
    {
        Assert.Equal(2 * 1, Mc("if (c) return 1; else return 2;", "int M(bool c)"));
    }

    [Fact]
    public void ElseIf_IsAnIfNestedInTheElseBranch()
    {
        Assert.Equal(2 * (1 + (2 * 1)), Mc("if (a) x(); else if (b) y(); else x();"));
    }

    [Fact]
    public void StructureWithEmptyBody_KeepsItsWeight()
    {
        Assert.Equal(2 + 3, Mc("if (a) { } for (;;) { }"));
    }

    [Fact]
    public void Switch_SectionsFormOneInnerLayer_AndBreakIsNotASequence()
    {
        var body = """
            switch (k)
            {
                case 1: x(); break;
                case 2: y(); break;
                default: break;
            }
            """;

        // Case weight (Table 2.1)
        Assert.Equal(3 * 1, Mc(body, "void M(int k)"));
    }

    [Fact]
    public void Loops_WeighThree()
    {
        Assert.Equal(3 * 1, Mc("foreach (var i in l) x();"));
        Assert.Equal(3 * 1, Mc("while (a) x();"));
        Assert.Equal(3 * 1, Mc("do { x(); } while (a);"));
    }

    [Fact]
    public void Ternary_IsABranchOfItsTwoExpressions()
    {
        // D23: the statement that contains it is the if-then-else, not one more sequence
        Assert.Equal(2 * 1, Mc("return c ? 1 : 0;", "int M(bool c)"));
        Assert.Equal(2 * ((2 * 1) + 1), Mc("return a ? (b ? 1 : 2) : 3;", "int M(bool a, bool b)"));
        Assert.Equal((2 * 1) + 1, Mc("var v = c ? 1 : 0; x();", "void M(bool c)"));   // x() is the sequence
    }

    // ---------- INC-15: acceptance criteria ----------

    [Theory]
    [InlineData("if (c) return 1; else return 2;", 2 * 1)]
    [InlineData("if (a) x(); else if (b) y(); else x();", 2 * (1 + (2 * 1)))]   // D12: else if nested in the else
    [InlineData("switch (k) { case 1: return 1; case 2: return 2; default: return 0; }", 3 * 1)]
    [InlineData("return k switch { 1 => 1, 2 => 2, _ => 0 };", 3 * 1)]
    [InlineData("return c ? 1 : 0;", 2 * 1)]
    [InlineData("return a ? (b ? 1 : 2) : 3;", 2 * ((2 * 1) + 1))]
    public void BranchWeights_FollowTable21(string body, int expected)
    {
        Assert.Equal(expected, Mc(body, "int M(int k, bool a, bool b, bool c)"));
    }

    [Theory]
    [InlineData("return k switch { 1 => a ? 1 : 2, _ => 0 };", 3 * ((2 * 1) + 1))]          // a ternary arm is a structure
    [InlineData("return a ? 0 : k switch { 1 => 1, _ => 2 };", 2 * ((3 * 1) + 1))]          // and the other way round
    [InlineData("var v = k switch { 1 => 1, _ => 0 }; x(); return v;", (3 * 1) + 1)]
    [InlineData("if (k switch { 1 => a, _ => b }) x(); return 0;", (3 * 1) + (2 * 1) + 1)] // in a condition: the enclosing layer
    [InlineData("return k switch { > 0 when a => 1, _ => 0 };", 3 * 1)]
    public void SwitchExpression_IsACase(string body, int expected)
    {
        Assert.Equal(expected, Mc(body, "int M(int k, bool a, bool b, bool c)"));
    }

    [Fact]
    public void TernaryInACondition_BelongsToTheLayerOfTheStructure()
    {
        Assert.Equal((2 * 1) + (2 * 1), Mc("if (a ? b : c) x();"));
    }

    // ---------- INC-17: lambdas and local functions (D11) ----------

    [Theory]
    [InlineData("System.Array.ForEach(l, i => { if (i > 0) x(); });", 1 + (2 * 1), 1, 1)]
    [InlineData("var ok = System.Array.Exists(l, i => i > 0);", 1, 1, 1)]                  // expression lambda: no sequence
    [InlineData("System.Func<int, int> f = i => i > 0 ? 1 : 0;", 1 + (2 * 1), 1, 1)]          // its ternary counts
    [InlineData("System.Action f = () => { };", 1, 1, 1)]                                    // empty body adds nothing
    [InlineData("System.Action f = delegate { for (;;) { x(); } };", 1 + (3 * 1), 1, 1)]     // anonymous method
    [InlineData("void P(int i) { if (i > 0) x(); } P(1);", 1 + (2 * 1), 1 + (2 * 1), 1)]   // local function with if, called
    [InlineData("void P() { x(); } P(); y();", 1 + 1, 1, 1)]                               // its own sequence (inlined by calls)
    [InlineData("int P(int i) => i > 0 ? i : 0; P(1);", 1 + (2 * 1), 1, 1)]
    [InlineData("if (a) System.Array.ForEach(l, i => { if (i > 0) x(); });", 2 * (1 + (2 * 1)), 2 * 1, 2 * 1)] // nested in the if's layer
    public void LambdasAndLocalFunctions_ByRule(string body, int full, int calls, int ignore)
    {
        Assert.Equal(full, Mc(body, lambdas: McLambdaRule.Full));
        Assert.Equal(calls, Mc(body, lambdas: McLambdaRule.Calls));
        Assert.Equal(ignore, Mc(body, lambdas: McLambdaRule.Ignore));
    }

    [Fact]
    public void WrapperStatements_AreTransparent()
    {
        var body = """
            lock (this) { x(); }
            using (var s = new System.IO.MemoryStream()) { if (a) y(); }
            checked { x(); }
            """;

        Assert.Equal((2 * 1) + 1, Mc(body));
    }
}
