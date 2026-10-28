using Codimetry.Metrics.Ck;

namespace Codimetry.Tests.Helpers;

public static class CkCode
{
    /// <summary>
    /// Runs <see cref="CouplingCollector"/> with a semantic model, the same way CkMetricsAnalysisService does.
    /// </summary>
    public static CouplingCollector VisitCoupling(string code)
    {
        var (tree, model) = Code.Compile(code);
        var visitor = new CouplingCollector(model);
        visitor.Visit(tree.GetRoot());
        return visitor;
    }
}
