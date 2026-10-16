using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// How MC treats the code of lambdas, anonymous methods and local functions (D11, --mc-lambdas).
/// </summary>
public enum McLambdaRule
{
    /// <summary>Their body is a layer nested where they are written: its structures, sequence and calls count.</summary>
    Full,
    /// <summary>The rule before INC-17: lambdas contribute only their calls, local function bodies are inlined.</summary>
    Calls,
    /// <summary>Their bodies are left out.</summary>
    Ignore
}

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
/// A call adds the weight given by the caller of <see cref="Calculate"/> to the layer where it appears
/// (<see cref="MethodComplexityGraph"/>: 2 + MC of the called method of the analysed code).
/// <para>The body of a lambda, anonymous method or local function is a layer nested where it is written, of
/// weight 1 × its layer, added to the structures of that layer (D11). An expression body has no sequence of
/// its own: <c>x => x > 0</c> adds nothing, <c>x => x > 0 ? 1 : 0</c> adds its ternary. A call to a local
/// function adds nothing, since its body is already counted.</para>
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
    /// <param name="callWeight">Weight of each invocation, added to the layer where it appears; calls weigh
    /// nothing when it is not given.</param>
    /// <param name="lambdas">How lambdas and local functions are treated (--mc-lambdas).</param>
    public static int? Calculate(BaseMethodDeclarationSyntax method, Func<InvocationExpressionSyntax, int>? callWeight = null,
        McLambdaRule lambdas = McLambdaRule.Full) =>
        new Composer(callWeight ?? (_ => 0), lambdas).Method(method);

    private sealed class Composer(Func<InvocationExpressionSyntax, int> callWeight, McLambdaRule lambdas)
    {
        public McLambdaRule Lambdas => lambdas;

        public int? Method(BaseMethodDeclarationSyntax method)
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

        private int Layer(IEnumerable<StatementSyntax> statements)
        {
            var layer = new LayerWeight(this);
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
        private int Inner(IEnumerable<StatementSyntax?> statements) =>
            Math.Max(1, Layer(statements.OfType<StatementSyntax>()));

        private sealed class LayerWeight(Composer composer)
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
                        _structures += composer.Expressions(ifStatement.Condition) +
                            BranchWeight * composer.Inner([ifStatement.Statement, ifStatement.Else?.Statement]);
                        break;
                    case SwitchStatementSyntax switchStatement:
                        _structures += composer.Expressions(switchStatement.Expression) +
                            SwitchWeight * composer.Inner(switchStatement.Sections.SelectMany(s => s.Statements));
                        break;
                    case ForStatementSyntax forStatement:
                        _structures += composer.Expressions(forStatement.Declaration) +
                            forStatement.Initializers.Sum(composer.Expressions) +
                            composer.Expressions(forStatement.Condition) +
                            forStatement.Incrementors.Sum(composer.Expressions) +
                            IterationWeight * composer.Inner([forStatement.Statement]);
                        break;
                    case CommonForEachStatementSyntax forEach:
                        _structures += composer.Expressions(forEach.Expression) + IterationWeight * composer.Inner([forEach.Statement]);
                        break;
                    case WhileStatementSyntax whileStatement:
                        _structures += composer.Expressions(whileStatement.Condition) + IterationWeight * composer.Inner([whileStatement.Statement]);
                        break;
                    case DoStatementSyntax doStatement:
                        _structures += composer.Expressions(doStatement.Condition) + IterationWeight * composer.Inner([doStatement.Statement]);
                        break;
                    case BreakStatementSyntax or EmptyStatementSyntax:
                        // Syntactic terminators, not sequences
                        break;
                    case BlockSyntax:
                        AddChildren(statement);
                        break;
                    case LocalFunctionStatementSyntax localFunction when composer.Lambdas != McLambdaRule.Calls:
                        // A layer of its own, written here; it is not a simple statement of this layer
                        _structures += composer.LocalFunction(localFunction);
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
                            _structures += composer.Expressions(statement);
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
                            _structures += composer.Expressions(child);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Weight of the ternaries and calls in an expression, which add to the layer where the expression
        /// is. Lambda bodies and local functions are left out.
        /// </summary>
        public int Expressions(SyntaxNode? node)
        {
            switch (node)
            {
                case null:
                case LocalFunctionStatementSyntax:
                    return 0;
                case AnonymousFunctionExpressionSyntax lambda:
                    return Lambda(lambda);
                case ConditionalExpressionSyntax conditional:
                    return Ternary(conditional);
                case InvocationExpressionSyntax invocation:
                    // The receiver and the arguments may hold calls of their own: f(g(x)), a.B().C()
                    return callWeight(invocation) + node.ChildNodes().Sum(Expressions);
                default:
                    return node.ChildNodes().Sum(Expressions);
            }
        }

        /// <summary>
        /// Weight of a lambda or anonymous method in the layer where it is written (D11).
        /// </summary>
        private int Lambda(AnonymousFunctionExpressionSyntax lambda) => lambdas switch
        {
            McLambdaRule.Full => lambda.Block != null ? Layer(lambda.Block.Statements) : Expressions(lambda.ExpressionBody),
            McLambdaRule.Calls => lambda.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Sum(callWeight),
            _ => 0
        };

        /// <summary>
        /// Weight of a local function in the layer where it is declared: its body, like a lambda's (D11).
        /// </summary>
        public int LocalFunction(LocalFunctionStatementSyntax localFunction) => lambdas switch
        {
            McLambdaRule.Full => localFunction.Body != null
                ? Layer(localFunction.Body.Statements)
                : Expressions(localFunction.ExpressionBody?.Expression),
            _ => 0
        };

        /// <summary>
        /// A ternary is a branch of weight 2 × the layer of its two expressions: a nested ternary is a
        /// structure of that layer, any other expression a simple statement.
        /// </summary>
        private int Ternary(ConditionalExpressionSyntax conditional)
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
}
