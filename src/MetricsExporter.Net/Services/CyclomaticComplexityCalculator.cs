using Microsoft.CodeAnalysis.CSharp.Syntax;
using MetricsExporter.Net.Parsers;

namespace MetricsExporter.Net.Services;

/// <summary>
/// Calculator for Cyclomatic Complexity metrics.
/// Cyclomatic Complexity = 1 + number of decision points
/// </summary>
public class CyclomaticComplexityCalculator
{
    /// <summary>
    /// Calculates cyclomatic complexity for a method declaration.
    /// </summary>
    public int CalculateComplexity(MethodDeclarationSyntax method)
    {
        var visitor = new CyclomaticComplexityVisitor();

        if (method.Body != null)
        {
            visitor.Visit(method.Body);
        }
        else if (method.ExpressionBody != null)
        {
            visitor.Visit(method.ExpressionBody);
        }
        else
        {
            // Abstract method or interface method - no body
            return 0;
        }

        return visitor.GetComplexity();
    }

    /// <summary>
    /// Calculates cyclomatic complexity for a constructor declaration.
    /// </summary>
    public int CalculateComplexity(ConstructorDeclarationSyntax constructor)
    {
        var visitor = new CyclomaticComplexityVisitor();

        if (constructor.Body != null)
        {
            visitor.Visit(constructor.Body);
        }
        else
        {
            // Expression-bodied constructor (rare, but possible)
            return 1;
        }

        return visitor.GetComplexity();
    }

    /// <summary>
    /// Calculates cyclomatic complexity for a property accessor.
    /// </summary>
    public int CalculateComplexity(AccessorDeclarationSyntax accessor)
    {
        var visitor = new CyclomaticComplexityVisitor();

        if (accessor.Body != null)
        {
            visitor.Visit(accessor.Body);
        }
        else if (accessor.ExpressionBody != null)
        {
            visitor.Visit(accessor.ExpressionBody);
        }
        else
        {
            // Auto-property accessor - no body
            return 1;
        }

        return visitor.GetComplexity();
    }

    /// <summary>
    /// Calculates cyclomatic complexity for a block of statements.
    /// </summary>
    public int CalculateComplexity(BlockSyntax block)
    {
        var visitor = new CyclomaticComplexityVisitor();
        visitor.Visit(block);
        return visitor.GetComplexity();
    }
}

