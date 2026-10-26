using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codimetry.Parsers;

/// <summary>
/// Which operators count as decision points in V(G), besides the default ones.
/// </summary>
/// <param name="NullOperators">`??`, `??=` and `?.` add one each (--cc-null-operators).</param>
/// <param name="LegacyOperators">The legacy operator rule (--cc-legacy-operators): `??` and `goto` add one,
/// `??=`, `?.` and the pattern combinators `or`/`and` do not. Takes precedence over <paramref name="NullOperators"/>.</param>
/// <param name="IncludeAutoAccessors">Automatic property accessors (<c>{ get; set; }</c>) get a row with V(G) = 1 and
/// enter the class and file sums, as in earlier versions (--cc-include-auto-accessors).</param>
public sealed record CyclomaticOptions(bool NullOperators = false, bool LegacyOperators = false, bool IncludeAutoAccessors = false)
{
    public static CyclomaticOptions Default { get; } = new();
}

/// <summary>
/// Syntax visitor that calculates cyclomatic complexity by counting decision points in method bodies.
/// Cyclomatic Complexity = 1 + number of decision points
/// </summary>
public class CyclomaticComplexityVisitor : CSharpSyntaxWalker
{
    private int _complexity = 1; // Start with base complexity of 1
    private readonly CyclomaticOptions _options;

    public CyclomaticComplexityVisitor(CyclomaticOptions? options = null) : base(SyntaxWalkerDepth.Node)
    {
        _options = options ?? CyclomaticOptions.Default;
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
        // Each case label is one more outgoing edge; default and a catch-all pattern (case _: or
        // case var x: without when) are the complementary path, like an else. A when clause is
        // part of its case and adds nothing of its own.
        foreach (var label in node.Sections.SelectMany(s => s.Labels))
        {
            switch (label)
            {
                case CaseSwitchLabelSyntax:
                case CasePatternSwitchLabelSyntax patternLabel when !IsCatchAll(patternLabel.Pattern, patternLabel.WhenClause):
                    _complexity++;
                    break;
            }
        }

        // Let base class visit children recursively
        base.VisitSwitchStatement(node);
    }

    public override void VisitSwitchExpression(SwitchExpressionSyntax node)
    {
        // Each arm is a decision point, except the catch-all arm (_ or var x without when)
        _complexity += node.Arms.Count(arm => !IsCatchAll(arm.Pattern, arm.WhenClause));

        // Let base class visit children recursively
        base.VisitSwitchExpression(node);
    }

    /// <summary>
    /// A pattern that matches every value: the discard or a var pattern, with no when clause.
    /// </summary>
    private static bool IsCatchAll(PatternSyntax pattern, WhenClauseSyntax? whenClause) =>
        whenClause == null &&
        pattern is DiscardPatternSyntax or VarPatternSyntax { Designation: SingleVariableDesignationSyntax or DiscardDesignationSyntax };

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
        // Logical operators (&&, ||) are decision points in any expression: short-circuiting
        // creates the path even outside a condition
        if (node.Kind() == SyntaxKind.LogicalAndExpression || node.Kind() == SyntaxKind.LogicalOrExpression)
        {
            _complexity++; // Each && or || adds complexity
        }
        else if (node.Kind() == SyntaxKind.CoalesceExpression && (_options.LegacyOperators || _options.NullOperators))
        {
            _complexity++;
        }

        // Let base class visit children recursively
        base.VisitBinaryExpression(node);
    }

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        if (node.Kind() == SyntaxKind.CoalesceAssignmentExpression && CountsNewNullOperators)
        {
            _complexity++;
        }

        base.VisitAssignmentExpression(node);
    }

    public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
    {
        if (CountsNewNullOperators)
        {
            _complexity++;
        }

        base.VisitConditionalAccessExpression(node);
    }

    public override void VisitBinaryPattern(BinaryPatternSyntax node)
    {
        // The pattern combinators or/and branch like || and && (earlier versions did not count them)
        if (!_options.LegacyOperators)
        {
            _complexity++;
        }

        base.VisitBinaryPattern(node);
    }

    public override void VisitGotoStatement(GotoStatementSyntax node)
    {
        // An unconditional jump has a single outgoing edge; earlier versions counted it
        if (_options.LegacyOperators)
        {
            _complexity++;
        }

        base.VisitGotoStatement(node);
    }

    // ??= and ?. were not counted by earlier versions, so the legacy rule leaves them out
    private bool CountsNewNullOperators => _options.NullOperators && !_options.LegacyOperators;
}
