using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// Information about Basic Control Structures (BCSs) found in a method.
/// </summary>
public class BcsInfo
{
    /// <summary>
    /// Number of sequence structures (sequential statements). Weight: 1
    /// </summary>
    public int SequenceCount { get; set; }

    /// <summary>
    /// Number of branch structures (if, switch, ternary). Weight: 2
    /// </summary>
    public int BranchCount { get; set; }

    /// <summary>
    /// Number of iteration structures (for, while, foreach). Weight: 3
    /// </summary>
    public int IterationCount { get; set; }

    /// <summary>
    /// Number of call structures (method invocations). Weight: 2
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// Calculates the total Method Complexity (MC) based on BCS weights.
    /// MC = (SequenceCount * 1) + (BranchCount * 2) + (IterationCount * 3) + (CallCount * 2)
    /// </summary>
    public int CalculateMc()
    {
        return (SequenceCount * 1) + (BranchCount * 2) + (IterationCount * 3) + (CallCount * 2);
    }
}

/// <summary>
/// Syntax visitor that detects Basic Control Structures (BCSs) for MISRA metrics calculation.
/// </summary>
public class MisraSyntaxVisitor : CSharpSyntaxWalker
{
    private readonly Dictionary<string, BcsInfo> _methodBcsInfo = new();
    private readonly Dictionary<string, List<string>> _classAttributes = new();
    private readonly Dictionary<string, List<string>> _classAttributeTypes = new();
    private string? _currentMethodName;
    private string? _currentClassName;

    public MisraSyntaxVisitor() : base(SyntaxWalkerDepth.Node)
    {
    }

    public IReadOnlyDictionary<string, BcsInfo> GetMethodBcsInfo() => _methodBcsInfo;
    public IReadOnlyDictionary<string, List<string>> GetClassAttributes() => _classAttributes;
    public IReadOnlyDictionary<string, List<string>> GetClassAttributeTypes() => _classAttributeTypes;

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var previousClassName = _currentClassName;
        _currentClassName = node.Identifier.Text;
        var attributes = new List<string>();
        var attributeTypes = new List<string>();

        // Collect fields
        foreach (var field in node.Members.OfType<FieldDeclarationSyntax>())
        {
            var fieldType = field.Declaration.Type.ToString();
            foreach (var variable in field.Declaration.Variables)
            {
                attributes.Add(variable.Identifier.Text);
                attributeTypes.Add(fieldType);
            }
        }

        // Collect properties
        foreach (var property in node.Members.OfType<PropertyDeclarationSyntax>())
        {
            attributes.Add(property.Identifier.Text);
            attributeTypes.Add(property.Type.ToString());
        }

        if (attributes.Count > 0)
        {
            _classAttributes[_currentClassName] = attributes;
            _classAttributeTypes[_currentClassName] = attributeTypes;
        }

        base.VisitClassDeclaration(node);
        
        // Restore previous class name (for nested classes)
        _currentClassName = previousClassName;
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        _currentMethodName = node.Identifier.Text;
        var key = $"{_currentClassName}::{_currentMethodName}";
        var bcsInfo = new BcsInfo();

        _methodBcsInfo[key] = bcsInfo;

        // Visit method body to count BCSs
        if (node.Body != null)
        {
            VisitMethodBody(node.Body, bcsInfo);
        }
        else if (node.ExpressionBody != null)
        {
            // Expression-bodied method - count as sequence + call if present
            bcsInfo.SequenceCount = 1;
            VisitExpressionBody(node.ExpressionBody, bcsInfo);
        }
        else
        {
            // Abstract method or interface method - no body
            bcsInfo.SequenceCount = 0;
        }

        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        _currentMethodName = node.Identifier.Text;
        var key = $"{_currentClassName}::{_currentMethodName}";
        var bcsInfo = new BcsInfo();

        _methodBcsInfo[key] = bcsInfo;

        if (node.Body != null)
        {
            VisitMethodBody(node.Body, bcsInfo);
        }
        else if (node.ExpressionBody != null)
        {
            bcsInfo.SequenceCount = 1;
            VisitExpressionBody(node.ExpressionBody, bcsInfo);
        }

        base.VisitConstructorDeclaration(node);
    }

    private void VisitMethodBody(BlockSyntax body, BcsInfo bcsInfo)
    {
        VisitStatement(body, bcsInfo);
    }

    /// <summary>
    /// Statements that only wrap other statements (blocks, try, using, lock...).
    /// They are not sequences themselves; their inner statements are counted instead.
    /// </summary>
    private static bool ContainsStatements(StatementSyntax statement) =>
        statement.ChildNodes().Any(child => child is StatementSyntax or CatchClauseSyntax or FinallyClauseSyntax);

    private void VisitExpressionBody(ArrowExpressionClauseSyntax expressionBody, BcsInfo bcsInfo)
    {
        // Expression-bodied methods are treated as a single sequence
        // Check for method calls in the expression
        VisitNode(expressionBody.Expression, bcsInfo);
    }

    private void VisitStatement(StatementSyntax statement, BcsInfo bcsInfo)
    {
        switch (statement.Kind())
        {
            case SyntaxKind.IfStatement:
                bcsInfo.BranchCount++;
                VisitIfStatement((IfStatementSyntax)statement, bcsInfo);
                break;
            case SyntaxKind.SwitchStatement:
                bcsInfo.BranchCount++;
                VisitSwitchStatement((SwitchStatementSyntax)statement, bcsInfo);
                break;
            case SyntaxKind.ForStatement:
                bcsInfo.IterationCount++;
                VisitForStatement((ForStatementSyntax)statement, bcsInfo);
                break;
            case SyntaxKind.ForEachStatement:
            case SyntaxKind.ForEachVariableStatement:
                bcsInfo.IterationCount++;
                VisitForEachStatement(statement, bcsInfo);
                break;
            case SyntaxKind.WhileStatement:
                bcsInfo.IterationCount++;
                VisitWhileStatement((WhileStatementSyntax)statement, bcsInfo);
                break;
            case SyntaxKind.DoStatement:
                bcsInfo.IterationCount++;
                VisitDoStatement((DoStatementSyntax)statement, bcsInfo);
                break;
            case SyntaxKind.Block:
                VisitStatementChildren(statement, bcsInfo);
                break;
            case SyntaxKind.BreakStatement:
            case SyntaxKind.EmptyStatement:
                // Syntactic terminators, not sequences
                break;
            default:
                if (ContainsStatements(statement))
                {
                    // Block, try/catch/finally, using, lock, checked, unsafe, fixed, labeled...
                    VisitStatementChildren(statement, bcsInfo);
                }
                else
                {
                    // Regular statement - count as sequence and look for calls/ternaries in it
                    bcsInfo.SequenceCount++;
                    VisitNode(statement, bcsInfo);
                }
                break;
        }
    }

    private void VisitStatementChildren(SyntaxNode node, BcsInfo bcsInfo)
    {
        foreach (var child in node.ChildNodes())
        {
            switch (child)
            {
                case StatementSyntax childStatement:
                    VisitStatement(childStatement, bcsInfo);
                    break;
                case CatchClauseSyntax or FinallyClauseSyntax:
                    VisitStatementChildren(child, bcsInfo);
                    break;
                default:
                    // Expressions and declarations (using/lock resources, catch filters...)
                    VisitNode(child, bcsInfo);
                    break;
            }
        }
    }

    private void VisitIfStatement(IfStatementSyntax ifStatement, BcsInfo bcsInfo)
    {
        // Visit condition (may contain calls)
        VisitNode(ifStatement.Condition, bcsInfo);

        // Visit then statement
        if (ifStatement.Statement != null)
        {
            VisitStatement(ifStatement.Statement, bcsInfo);
        }

        // Visit else clause (each else if is another branch)
        if (ifStatement.Else != null)
        {
            bcsInfo.BranchCount++; // Each else/else if is an additional branch

            // An else if is already counted by the line above, so visit it without counting it again
            if (ifStatement.Else.Statement is IfStatementSyntax elseIf)
            {
                VisitIfStatement(elseIf, bcsInfo);
            }
            else
            {
                VisitStatement(ifStatement.Else.Statement, bcsInfo);
            }
        }
    }

    private void VisitSwitchStatement(SwitchStatementSyntax switchStatement, BcsInfo bcsInfo)
    {
        VisitNode(switchStatement.Expression, bcsInfo);

        // Each case is counted, but we already counted the switch itself
        foreach (var section in switchStatement.Sections)
        {
            foreach (var label in section.Labels)
            {
                if (label.Kind() == SyntaxKind.DefaultSwitchLabel)
                {
                    // Default case doesn't add to branch count (switch already counted)
                }
            }

            // Visit statements in case
            foreach (var statement in section.Statements)
            {
                VisitStatement(statement, bcsInfo);
            }
        }
    }

    private void VisitForStatement(ForStatementSyntax forStatement, BcsInfo bcsInfo)
    {
        // Visit initializers, condition, and incrementors (may contain calls)
        foreach (var initializer in forStatement.Initializers)
        {
            VisitNode(initializer, bcsInfo);
        }

        if (forStatement.Condition != null)
        {
            VisitNode(forStatement.Condition, bcsInfo);
        }

        foreach (var incrementor in forStatement.Incrementors)
        {
            VisitNode(incrementor, bcsInfo);
        }

        // Visit statement body
        if (forStatement.Statement != null)
        {
            VisitStatement(forStatement.Statement, bcsInfo);
        }
    }

    private void VisitForEachStatement(StatementSyntax forEachStatement, BcsInfo bcsInfo)
    {
        if (forEachStatement is ForEachStatementSyntax forEach)
        {
            VisitNode(forEach.Expression, bcsInfo);
            if (forEach.Statement != null)
            {
                VisitStatement(forEach.Statement, bcsInfo);
            }
        }
        else if (forEachStatement is ForEachVariableStatementSyntax forEachVar)
        {
            VisitNode(forEachVar.Variable, bcsInfo);
            VisitNode(forEachVar.Expression, bcsInfo);
            if (forEachVar.Statement != null)
            {
                VisitStatement(forEachVar.Statement, bcsInfo);
            }
        }
    }

    private void VisitWhileStatement(WhileStatementSyntax whileStatement, BcsInfo bcsInfo)
    {
        VisitNode(whileStatement.Condition, bcsInfo);
        if (whileStatement.Statement != null)
        {
            VisitStatement(whileStatement.Statement, bcsInfo);
        }
    }

    private void VisitDoStatement(DoStatementSyntax doStatement, BcsInfo bcsInfo)
    {
        if (doStatement.Statement != null)
        {
            VisitStatement(doStatement.Statement, bcsInfo);
        }
        VisitNode(doStatement.Condition, bcsInfo);
    }

    private void VisitNode(SyntaxNode node, BcsInfo bcsInfo)
    {
        // Detect method calls
        if (node is InvocationExpressionSyntax invocation)
        {
            bcsInfo.CallCount++;
        }

        // Detect ternary operator (conditional expression)
        if (node is ConditionalExpressionSyntax)
        {
            bcsInfo.BranchCount++;
        }

        // Detect null-coalescing operator (may be considered a branch)
        if (node is BinaryExpressionSyntax binary && binary.Kind() == SyntaxKind.CoalesceExpression)
        {
            // Null-coalescing is a form of branching, but simpler - count as sequence
            // Could be counted as branch if desired
        }

        // Recursively visit children
        foreach (var child in node.ChildNodes())
        {
            VisitNode(child, bcsInfo);
        }
    }
}

