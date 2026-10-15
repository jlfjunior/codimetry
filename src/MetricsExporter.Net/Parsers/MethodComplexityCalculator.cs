using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// MC by the composition rule of Misra et al. (Eq. 23): structures in the same layer are summed,
/// nested layers are multiplied, and the simple statements of a layer form one sequence of weight 1.
/// <list type="bullet">
/// <item>weight(list) = Σ weight(structures of the list) + (1 when the list has a simple statement); an empty list is 0.</item>
/// <item>weight(structure) = W × weight(inner layer), where the inner layer joins all its branches
/// (then and else; every switch section) and counts at least 1.</item>
/// <item>Blocks, try/catch/finally, using, lock, checked, unsafe, fixed and labels are transparent: their
/// statements belong to the list that contains them.</item>
/// </list>
/// Calls are not structures yet, and lambda bodies are not looked into (INC-16, INC-17).
/// </summary>
public static class MethodComplexityCalculator
{
    public const int BranchWeight = 2;
    public const int SwitchWeight = 2;
    public const int IterationWeight = 3;

    /// <summary>
    /// MC of a method or constructor, or null when it has no body (abstract, interface, extern, partial).
    /// An expression body is a list with one simple statement.
    /// </summary>
    public static int? Calculate(BaseMethodDeclarationSyntax method)
    {
        if (method.Body != null)
        {
            return Layer(method.Body.Statements);
        }

        if (method.ExpressionBody != null)
        {
            return 1 + Expressions(method.ExpressionBody.Expression);
        }

        return null;
    }

    private static int Layer(IEnumerable<StatementSyntax> statements)
    {
        var layer = new LayerWeight();
        foreach (var statement in statements)
        {
            layer.Add(statement);
        }

        return layer.Total;
    }

    /// <summary>
    /// Weight of the layer nested in a structure. A structure with an empty body still exists, so the
    /// layer counts at least 1 and the structure keeps its own weight.
    /// </summary>
    private static int Inner(IEnumerable<StatementSyntax?> statements) =>
        Math.Max(1, Layer(statements.OfType<StatementSyntax>()));

    private sealed class LayerWeight
    {
        private int _structures;
        private bool _hasSimpleStatement;

        public int Total => _structures + (_hasSimpleStatement ? 1 : 0);

        public void Add(StatementSyntax statement)
        {
            switch (statement)
            {
                case IfStatementSyntax ifStatement:
                    // else if is an if nested in the else branch, as written
                    _structures += Expressions(ifStatement.Condition) +
                        BranchWeight * Inner([ifStatement.Statement, ifStatement.Else?.Statement]);
                    break;
                case SwitchStatementSyntax switchStatement:
                    _structures += Expressions(switchStatement.Expression) +
                        SwitchWeight * Inner(switchStatement.Sections.SelectMany(s => s.Statements));
                    break;
                case ForStatementSyntax forStatement:
                    _structures += Expressions(forStatement.Declaration) +
                        forStatement.Initializers.Sum(Expressions) +
                        Expressions(forStatement.Condition) +
                        forStatement.Incrementors.Sum(Expressions) +
                        IterationWeight * Inner([forStatement.Statement]);
                    break;
                case CommonForEachStatementSyntax forEach:
                    _structures += Expressions(forEach.Expression) + IterationWeight * Inner([forEach.Statement]);
                    break;
                case WhileStatementSyntax whileStatement:
                    _structures += Expressions(whileStatement.Condition) + IterationWeight * Inner([whileStatement.Statement]);
                    break;
                case DoStatementSyntax doStatement:
                    _structures += Expressions(doStatement.Condition) + IterationWeight * Inner([doStatement.Statement]);
                    break;
                case BreakStatementSyntax or EmptyStatementSyntax:
                    // Syntactic terminators, not sequences
                    break;
                case BlockSyntax:
                    AddChildren(statement);
                    break;
                default:
                    if (statement.ChildNodes().Any(child => child is StatementSyntax or CatchClauseSyntax or FinallyClauseSyntax))
                    {
                        // try/catch/finally, using, lock, checked, unsafe, fixed, labeled, local function...
                        AddChildren(statement);
                    }
                    else
                    {
                        _hasSimpleStatement = true;
                        _structures += Expressions(statement);
                    }
                    break;
            }
        }

        private void AddChildren(SyntaxNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                switch (child)
                {
                    case StatementSyntax childStatement:
                        Add(childStatement);
                        break;
                    case CatchClauseSyntax or FinallyClauseSyntax:
                        AddChildren(child);
                        break;
                    default:
                        // Resources, catch declarations and filters belong to this layer
                        _structures += Expressions(child);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Weight of the ternaries in an expression, which add to the layer where the expression is.
    /// Lambda bodies and local functions are left out.
    /// </summary>
    private static int Expressions(SyntaxNode? node)
    {
        switch (node)
        {
            case null:
            case AnonymousFunctionExpressionSyntax:
            case LocalFunctionStatementSyntax:
                return 0;
            case ConditionalExpressionSyntax conditional:
                return Ternary(conditional);
            default:
                return node.ChildNodes().Sum(Expressions);
        }
    }

    /// <summary>
    /// A ternary is a branch of weight 2 × the layer of its two expressions: a nested ternary is a
    /// structure of that layer, any other expression a simple statement.
    /// </summary>
    private static int Ternary(ConditionalExpressionSyntax conditional)
    {
        var structures = 0;
        var hasSimpleExpression = false;
        foreach (var branch in new[] { conditional.WhenTrue, conditional.WhenFalse })
        {
            var expression = branch;
            while (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                expression = parenthesized.Expression;
            }

            if (expression is ConditionalExpressionSyntax nested)
            {
                structures += Ternary(nested);
            }
            else
            {
                hasSimpleExpression = true;
                structures += Expressions(branch);
            }
        }

        return Expressions(conditional.Condition) +
            BranchWeight * Math.Max(1, structures + (hasSimpleExpression ? 1 : 0));
    }
}
