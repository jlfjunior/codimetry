using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// Syntax visitor that calculates cyclomatic complexity by counting decision points in method bodies.
/// Cyclomatic Complexity = 1 + number of decision points
/// </summary>
public class CyclomaticComplexityVisitor : CSharpSyntaxWalker
{
    private int _complexity = 1; // Start with base complexity of 1

    public CyclomaticComplexityVisitor() : base(SyntaxWalkerDepth.Node)
    {
    }

    /// <summary>
    /// Gets the calculated cyclomatic complexity.
    /// </summary>
    public int GetComplexity() => _complexity;

    /// <summary>
    /// Resets the complexity counter (for reuse of visitor instance).
    /// </summary>
    public void Reset()
    {
        _complexity = 1;
    }

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        _complexity++; // Each if statement is a decision point

        // An else if is the nested if statement of the else clause: it is counted when the base
        // class visits it, so it must not be counted here too. A plain else adds no decision.

        // Let base class visit children recursively
        base.VisitIfStatement(node);
    }

    public override void VisitSwitchStatement(SwitchStatementSyntax node)
    {
        // Each case label is a decision point
        foreach (var section in node.Sections)
        {
            foreach (var label in section.Labels)
            {
                if (label.Kind() == SyntaxKind.CaseSwitchLabel)
                {
                    _complexity++; // Each case adds complexity
                }
                else if (label.Kind() == SyntaxKind.DefaultSwitchLabel)
                {
                    _complexity++; // Default also adds complexity
                }
            }
        }

        // Let base class visit children recursively
        base.VisitSwitchStatement(node);
    }

    public override void VisitSwitchExpression(SwitchExpressionSyntax node)
    {
        // Each switch expression arm is a decision point
        foreach (var arm in node.Arms)
        {
            _complexity++;
        }

        // Let base class visit children recursively
        base.VisitSwitchExpression(node);
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        _complexity++; // Each for loop is a decision point

        // Let base class visit children recursively
        base.VisitForStatement(node);
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        _complexity++; // Each foreach loop is a decision point

        // Let base class visit children recursively
        base.VisitForEachStatement(node);
    }

    public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
    {
        _complexity++; // Each foreach variable loop is a decision point

        // Let base class visit children recursively
        base.VisitForEachVariableStatement(node);
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        _complexity++; // Each while loop is a decision point

        // Let base class visit children recursively
        base.VisitWhileStatement(node);
    }

    public override void VisitDoStatement(DoStatementSyntax node)
    {
        _complexity++; // Each do-while loop is a decision point

        // Let base class visit children recursively
        base.VisitDoStatement(node);
    }

    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        _complexity++; // Each catch clause is a decision point

        // Let base class visit children recursively
        base.VisitCatchClause(node);
    }

    public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        _complexity++; // Ternary operator (?:) is a decision point

        // Let base class visit children recursively
        base.VisitConditionalExpression(node);
    }

    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        // Logical operators (&&, ||) are decision points
        if (node.Kind() == SyntaxKind.LogicalAndExpression || node.Kind() == SyntaxKind.LogicalOrExpression)
        {
            _complexity++; // Each && or || adds complexity
        }
        // Null-coalescing operator (??) is also a decision point
        else if (node.Kind() == SyntaxKind.CoalesceExpression)
        {
            _complexity++;
        }

        // Let base class visit children recursively
        base.VisitBinaryExpression(node);
    }

    public override void VisitGotoStatement(GotoStatementSyntax node)
    {
        _complexity++; // Goto is a decision point (rare in modern C#)

        // Let base class visit children recursively
        base.VisitGotoStatement(node);
    }
}

