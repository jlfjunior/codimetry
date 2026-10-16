using Codimetry.Services;
using Codimetry.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp;

namespace Codimetry.Tests.Services;

public class LogicalLocCalculatorTests
{
    private readonly LogicalLocCalculator _calculator = new();

    private int Lloc(string source) =>
        _calculator.Calculate(CSharpSyntaxTree.ParseText(source).GetRoot());

    private static string InMethod(string body) => $"class C {{ void M() {{ {body} }} }}";

    [Fact]
    public void Calculator_FromChapter2_Is4()
    {
        // class + method + assignment + return
        Assert.Equal(4, Lloc(Samples.LocCalculator));
    }

    [Fact]
    public void TwoStatementsOnOneLine_Count2()
    {
        Assert.Equal(2 + 2, Lloc(InMethod("int a = 1; int b = 2;")));
    }

    [Fact]
    public void StatementSpanningLines_Counts1()
    {
        Assert.Equal(2 + 1, Lloc(InMethod("""
            var s = "x"
                .Trim()
                .ToUpper()
                .ToLower();
            """)));
    }

    [Fact]
    public void Dto_CountsTypeAndProperty_NotAutoAccessors()
    {
        Assert.Equal(2, Lloc("public class Dto { public string Nome { get; set; } }"));
    }

    [Fact]
    public void OnlyComments_Is0()
    {
        Assert.Equal(0, Lloc("// a\n/* b\n c */\n/// <summary>d</summary>"));
    }

    [Fact]
    public void Field_CountsOncePerDeclaration()
    {
        Assert.Equal(1 + 1, Lloc("class C { int a, b; }"));
    }

    [Fact]
    public void AccessorWithBody_CountsAccessorAndStatements()
    {
        Assert.Equal(1 + 1 + 1, Lloc("class C { int X { get => 1; } }"));
        Assert.Equal(1 + 1 + 1 + 1, Lloc("class C { int X { get { return 1; } } }"));
    }

    [Fact]
    public void ExpressionBodiedMembers_CountOnlyTheDeclaration()
    {
        Assert.Equal(1 + 1 + 1, Lloc("class C { int X => 1; int M() => 2; }"));
    }

    [Fact]
    public void BracesBlocksAndEmptyStatements_AreNotCounted()
    {
        Assert.Equal(2, Lloc(InMethod("{ { ; } }")));
    }

    [Fact]
    public void ControlFlow_CountsEachStatement()
    {
        // if, else-if, two returns, throw
        Assert.Equal(2 + 5, Lloc(InMethod("if (true) return; else if (false) throw null; else return;")));
    }

    [Fact]
    public void LabeledStatement_CountsOnce()
    {
        Assert.Equal(2 + 1, Lloc(InMethod("end: return;")));
    }

    [Fact]
    public void LocalFunctionAndLambdaBlock_CountTheirStatements()
    {
        // local function + its return; lambda declaration statement + its return
        Assert.Equal(2 + 2 + 2, Lloc(InMethod("int F() { return 1; } System.Func<int> g = () => { return 2; };")));
    }

    [Fact]
    public void UsingsAndNamespace_AreNotCounted()
    {
        Assert.Equal(1, Lloc("using System;\nnamespace N;\nclass C { }"));
    }

    [Fact]
    public void OtherTypeDeclarations_AreCounted()
    {
        Assert.Equal(1, Lloc("record R(int X);"));
        Assert.Equal(1, Lloc("interface I { }"));
        Assert.Equal(1, Lloc("delegate void D();"));
        Assert.Equal(1 + 2, Lloc("enum E { A, B }"));
        Assert.Equal(1 + 1, Lloc("struct S { public S() { } }"));
    }

    [Fact]
    public void TopLevelStatements_AreCounted()
    {
        Assert.Equal(2, Lloc("var x = 1;\nSystem.Console.WriteLine(x);"));
    }
}
