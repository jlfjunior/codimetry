using Microsoft.CodeAnalysis.Text;

namespace Codimetry.Metrics.Loc;

/// <summary>
/// Calculates physical Lines of Code (Park, 1992): every line of the file, including blank lines and comments.
/// The empty line left after the final line break is not counted.
/// </summary>
public class PhysicalLocCalculator
{
    public int Calculate(SourceText text)
    {
        var lines = text.Lines;
        return lines[^1].Span.IsEmpty ? lines.Count - 1 : lines.Count;
    }
}
