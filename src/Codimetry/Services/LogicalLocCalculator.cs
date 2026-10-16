using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codimetry.Services;

/// <summary>
/// Calculates logical Lines of Code (Park, 1992): declarations plus executable statements.
/// Braces, blank lines and comments are never counted, and a statement counts once however many lines it spans.
/// </summary>
public class LogicalLocCalculator
{
    public int Calculate(SyntaxNode node)
    {
        var walker = new Walker();
        walker.Visit(node);
        return walker.Count;
    }

    private static bool Counts(SyntaxNode node) => node switch
    {
        BlockSyntax or EmptyStatementSyntax or LabeledStatementSyntax => false,
        AccessorDeclarationSyntax accessor => accessor.Body is not null || accessor.ExpressionBody is not null,
        BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => true,
        BaseMethodDeclarationSyntax => true,
        BaseFieldDeclarationSyntax or EnumMemberDeclarationSyntax => true,
        BasePropertyDeclarationSyntax => true,
        StatementSyntax => true,
        _ => false
    };

    private sealed class Walker : CSharpSyntaxWalker
    {
        public int Count { get; private set; }

        public override void Visit(SyntaxNode? node)
        {
            if (node is not null && Counts(node))
                Count++;

            base.Visit(node);
        }
    }
}
