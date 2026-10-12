using MetricsExporter.Net.Services;
using MetricsExporter.Net.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Tests.Parsers;

/// <summary>
/// Covers CyclomaticComplexityCalculator and the decision points counted by CyclomaticComplexityVisitor
/// (the table in docs/cyclomatic.md).
/// </summary>
public class CyclomaticComplexityCalculatorTests
{
    private readonly CyclomaticComplexityCalculator _calculator = new();

    private int Complexity(string body) =>
        _calculator.CalculateComplexity(Code.Method($$"""
            class C
            {
                object M(int x, int y, bool a, bool b, string? s, object o)
                {
                    {{body}}
                }
            }
            """, "M"));

    [Fact]
    public void EmptyMethod_Is1()
    {
        Assert.Equal(1, Complexity(""));
    }

    [Theory]
    [InlineData("if (a) return 1; return 0;", 2)]
    [InlineData("if (a) return 1; else return 0;", 2)] // a plain else is not a decision point
    [InlineData("for (var i = 0; i < x; i++) { } return 0;", 2)]
    [InlineData("foreach (var c in s!) { } return 0;", 2)]
    [InlineData("foreach (var (k, v) in new (int, int)[0]) { } return 0;", 2)]
    [InlineData("while (a) { } return 0;", 2)]
    [InlineData("do { } while (a); return 0;", 2)]
    [InlineData("try { } catch (System.IO.IOException) { } catch { } return 0;", 3)]
    [InlineData("return a ? 1 : 0;", 2)]
    [InlineData("return a && b || a;", 3)]
    [InlineData("return s ?? \"\";", 2)]
    [InlineData("start: if (a) goto start; return 0;", 3)]
    public void EachDecisionPointAddsOne(string body, int expected)
    {
        Assert.Equal(expected, Complexity(body));
    }

    [Fact]
    public void Switch_CountsEachCaseAndDefault()
    {
        var body = """
            switch (x)
            {
                case 1: return 1;
                case 2:
                case 3: return 2;
                default: return 0;
            }
            """;

        Assert.Equal(5, Complexity(body)); // 1 + case 1 + case 2 + case 3 + default
    }

    [Fact]
    public void SwitchExpression_CountsEachArmIncludingDiscard()
    {
        Assert.Equal(4, Complexity("return x switch { 1 => \"a\", 2 => \"b\", _ => \"c\" };"));
    }

    [Fact]
    public void NestedDecisionsAreCountedWithoutNestingWeight()
    {
        Assert.Equal(4, Complexity("if (a) { if (b) { for (;;) { } } } return 0;"));
    }

    [Fact]
    public void LambdaInsideMethod_IsAddedToMethod()
    {
        Assert.Equal(2, Complexity("System.Func<int> f = () => a ? 1 : 0; return f();"));
    }

    [Fact(Skip = "Known bug: else-if is counted twice (CyclomaticComplexityVisitor.cs:37-44). Expected 3, the tool returns 4.")]
    public void ElseIf_CountsOnce()
    {
        Assert.Equal(3, Complexity("if (x > 1) return 1; else if (x > 0) return 2; else return 3;"));
    }

    [Fact]
    public void ExpressionBodiedMethod()
    {
        var method = Code.Method("class C { int M(bool a) => a ? 1 : 0; }", "M");

        Assert.Equal(2, _calculator.CalculateComplexity(method));
    }

    [Fact]
    public void AbstractMethod_IsZero()
    {
        var method = Code.Method("abstract class C { public abstract int M(); }", "M");

        Assert.Equal(0, _calculator.CalculateComplexity(method));
    }

    [Fact]
    public void Constructor()
    {
        var ctor = Code.Single<ConstructorDeclarationSyntax>("class C { C(bool a) { if (a) { } } }");

        Assert.Equal(2, _calculator.CalculateComplexity(ctor));
    }

    [Fact]
    public void ExpressionBodiedConstructor_Is1()
    {
        var ctor = Code.Single<ConstructorDeclarationSyntax>("class C { int _x; C(int x) => _x = x; }");

        Assert.Equal(1, _calculator.CalculateComplexity(ctor));
    }

    [Theory]
    [InlineData("int P { get; set; }", 1)]
    [InlineData("int P { get => _x > 0 ? _x : 0; }", 2)]
    [InlineData("int P { get { if (_x > 0) return _x; return 0; } }", 2)]
    public void PropertyGetter(string property, int expected)
    {
        var getter = Code.Single<AccessorDeclarationSyntax>($"class C {{ int _x; {property} }}",
            a => a.Keyword.Text == "get");

        Assert.Equal(expected, _calculator.CalculateComplexity(getter));
    }

    [Fact]
    public void Block()
    {
        var block = Code.Single<BlockSyntax>("class C { void M(bool a) { while (a) { } } }",
            b => b.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax);

        Assert.Equal(2, _calculator.CalculateComplexity(block));
    }
}
