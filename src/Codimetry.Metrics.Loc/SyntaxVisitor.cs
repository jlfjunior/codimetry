using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Codimetry.Core.Model;

namespace Codimetry.Metrics.Loc;

/// <summary>
/// Represents a code element (class or method) with its location information.
/// </summary>
public class CodeElement
{
    public string Name { get; set; } = string.Empty;
    public string? ContainingClass { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public bool IsClass { get; set; }
    public SyntaxNode Node { get; set; } = null!;
}

/// <summary>
/// Syntax visitor that extracts classes and methods from a C# syntax tree.
/// </summary>
public class SyntaxVisitor : CSharpSyntaxWalker
{
    private readonly List<CodeElement> _elements = new();
    private readonly TypeScope _scope;

    /// <param name="scope">Which types get a class row (--classes-only).</param>
    public SyntaxVisitor(TypeScope? scope = null) : base(SyntaxWalkerDepth.Node)
    {
        _scope = scope ?? TypeScope.Default;
    }

    public IReadOnlyList<CodeElement> GetElements() => _elements;

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        AddType(node);
        base.VisitClassDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        AddType(node);
        base.VisitRecordDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        AddType(node);
        base.VisitStructDeclaration(node);
    }

    private void AddType(TypeDeclarationSyntax node)
    {
        if (!_scope.IsMeasured(node))
        {
            return;
        }

        var className = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        _elements.Add(new CodeElement
        {
            Name = className,
            StartLine = startLine,
            EndLine = endLine,
            IsClass = true,
            Node = node
        });
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        // Find the containing type
        var containingClass = _scope.ContainingType(node);
        var className = containingClass?.Identifier.Text;

        _elements.Add(new CodeElement
        {
            Name = methodName,
            ContainingClass = className,
            StartLine = startLine,
            EndLine = endLine,
            IsClass = false,
            Node = node
        });

        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var constructorName = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        // Find the containing type
        var containingClass = _scope.ContainingType(node);
        var className = containingClass?.Identifier.Text;

        _elements.Add(new CodeElement
        {
            Name = constructorName,
            ContainingClass = className,
            StartLine = startLine,
            EndLine = endLine,
            IsClass = false,
            Node = node
        });

        base.VisitConstructorDeclaration(node);
    }
}

