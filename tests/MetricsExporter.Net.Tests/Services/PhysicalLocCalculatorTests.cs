using MetricsExporter.Net.Services;
using MetricsExporter.Net.Tests.Helpers;
using Microsoft.CodeAnalysis.Text;

namespace MetricsExporter.Net.Tests.Services;

public class PhysicalLocCalculatorTests
{
    private readonly PhysicalLocCalculator _calculator = new();

    private int Physical(string source) => _calculator.Calculate(SourceText.From(source));

    [Fact]
    public void Calculator_FromChapter2_Is10()
    {
        Assert.Equal(10, Physical(Samples.LocCalculator));
    }

    [Fact]
    public void CountsBlankAndCommentLines()
    {
        Assert.Equal(4, Physical("// a\n\n/* b */\nint a;"));
    }

    [Fact]
    public void TwoStatementsOnOneLine_Is1()
    {
        Assert.Equal(1, Physical("int a = 1; int b = 2;"));
    }

    [Theory]
    [InlineData("a;\nb;", 2)]
    [InlineData("a;\nb;\n", 2)]
    [InlineData("a;\nb;\n\n", 3)]
    [InlineData("a;\r\nb;\r\n", 2)]
    public void TrailingLineBreak_DoesNotAddALine(string source, int expected)
    {
        Assert.Equal(expected, Physical(source));
    }

    [Fact]
    public void EmptyFile_Is0()
    {
        Assert.Equal(0, Physical(""));
    }
}
