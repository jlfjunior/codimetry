using Codimetry.Metrics.Misra;

namespace Codimetry.Tests.Helpers;

public static class MisraCode
{
    public static MisraSyntaxVisitor VisitMisra(string code, bool additiveMc = false, bool acTypeWeights = false)
    {
        var visitor = new MisraSyntaxVisitor(additiveMc: additiveMc, acTypeWeights: acTypeWeights);
        visitor.Visit(Code.Parse(code).GetRoot());
        return visitor;
    }

    /// <summary>
    /// Runs <see cref="MisraSyntaxVisitor"/> with a semantic model, the same way MisraMetricsAnalysisService does.
    /// </summary>
    public static MisraSyntaxVisitor VisitMisraWithModel(string code)
    {
        var (tree, model) = Code.Compile(code);
        var visitor = new MisraSyntaxVisitor(model);
        visitor.Visit(tree.GetRoot());
        return visitor;
    }
}
