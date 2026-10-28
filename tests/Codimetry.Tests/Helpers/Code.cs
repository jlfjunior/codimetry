using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Codimetry.Core.Model;
using Codimetry.Metrics.Ck;
using Codimetry.Metrics.Misra;

namespace Codimetry.Tests.Helpers;

/// <summary>
/// Helpers to parse in-memory C# snippets and run the tool's syntax visitors over them.
/// </summary>
public static class Code
{
    public static SyntaxTree Parse(string code) =>
        CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

    public static T Single<T>(string code, Func<T, bool>? predicate = null) where T : SyntaxNode =>
        Parse(code).GetRoot().DescendantNodes().OfType<T>().Single(n => predicate?.Invoke(n) ?? true);

    public static MethodDeclarationSyntax Method(string code, string name) =>
        Single<MethodDeclarationSyntax>(code, m => m.Identifier.Text == name);

    /// <summary>
    /// Runs <see cref="TypeModelCollector"/> with a semantic model, the same way the analysis services do.
    /// </summary>
    public static TypeModelCollector VisitTypes(string code)
    {
        var (tree, model) = Compile(code);
        var visitor = new TypeModelCollector(model);
        visitor.Visit(tree.GetRoot());
        return visitor;
    }

    /// <summary>
    /// Runs <see cref="CouplingCollector"/> with a semantic model, the same way CkMetricsAnalysisService does.
    /// </summary>
    public static CouplingCollector VisitCoupling(string code)
    {
        var (tree, model) = Compile(code);
        var visitor = new CouplingCollector(model);
        visitor.Visit(tree.GetRoot());
        return visitor;
    }

    private static (SyntaxTree Tree, SemanticModel Model) Compile(string code)
    {
        var tree = Parse(code);
        var compilation = CSharpCompilation.Create("Test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        return (tree, compilation.GetSemanticModel(tree));
    }

    public static MisraSyntaxVisitor VisitMisra(string code, bool additiveMc = false, bool acTypeWeights = false)
    {
        var visitor = new MisraSyntaxVisitor(additiveMc: additiveMc, acTypeWeights: acTypeWeights);
        visitor.Visit(Parse(code).GetRoot());
        return visitor;
    }

    /// <summary>
    /// Runs <see cref="MisraSyntaxVisitor"/> with a semantic model, the same way MisraMetricsAnalysisService does.
    /// </summary>
    public static MisraSyntaxVisitor VisitMisraWithModel(string code)
    {
        var tree = Parse(code);
        var compilation = CSharpCompilation.Create("Test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var visitor = new MisraSyntaxVisitor(compilation.GetSemanticModel(tree));
        visitor.Visit(tree.GetRoot());
        return visitor;
    }
}
