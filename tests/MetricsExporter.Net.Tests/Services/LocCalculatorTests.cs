using MetricsExporter.Net.Services;

namespace MetricsExporter.Net.Tests.Services;

public class LocCalculatorTests
{
    private readonly LocCalculator _calculator = new();

    private int Loc(string source) =>
        _calculator.CalculateLoc(source, 1, source.Split('\n').Length);

    [Fact]
    public void CountsCodeLines()
    {
        Assert.Equal(3, Loc("int a = 1;\nint b = 2;\nreturn a + b;"));
    }

    [Fact]
    public void SkipsBlankAndWhitespaceLines()
    {
        Assert.Equal(2, Loc("int a = 1;\n\n   \n\t\nreturn a;"));
    }

    [Fact]
    public void SkipsSingleLineAndXmlDocComments()
    {
        Assert.Equal(1, Loc("// comment\n/// <summary>doc</summary>\nreturn 1;"));
    }

    [Fact]
    public void SkipsMultiLineBlockComment()
    {
        Assert.Equal(2, Loc("int a = 1;\n/*\n  long\n  comment\n*/\nreturn a;"));
    }

    [Fact]
    public void SkipsBlockCommentOnSingleLine()
    {
        Assert.Equal(1, Loc("/* only a comment */\nreturn 1;"));
    }

    [Theory]
    [InlineData("int a = 1; /* trailing */")]
    [InlineData("/* leading */ int a = 1;")]
    [InlineData("return x; // done")]
    public void CountsLineMixingCodeAndComment(string line)
    {
        Assert.Equal(1, Loc(line));
    }

    [Fact]
    public void CountsCodeAfterBlockCommentEnds()
    {
        Assert.Equal(1, Loc("/* start\n end */ int a = 1;"));
    }

    [Fact]
    public void CountsCodeBeforeBlockCommentStarts()
    {
        Assert.Equal(1, Loc("int a = 1; /* start\n still comment */"));
    }

    [Fact]
    public void CountsOnlyTheRequestedLineRange()
    {
        var source = "line1;\nline2;\nline3;\nline4;";

        Assert.Equal(2, _calculator.CalculateLoc(source, 2, 3));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(1, 99)]
    public void InvalidRange_ReturnsZero(int start, int end)
    {
        Assert.Equal(0, _calculator.CalculateLoc("a;\nb;", start, end));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptySource_ReturnsZero(string? source)
    {
        Assert.Equal(0, _calculator.CalculateLoc(source!, 1, 1));
    }

    [Fact]
    public void HandlesWindowsLineEndings()
    {
        Assert.Equal(2, _calculator.CalculateLoc("int a = 1;\r\n\r\nreturn a;", 1, 3));
    }
}
