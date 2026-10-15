using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

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

    public SyntaxVisitor() : base(SyntaxWalkerDepth.Node)
    {
    }

    public IReadOnlyList<CodeElement> GetElements() => _elements;

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
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

        // Visit methods in this class
        base.VisitClassDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        // Find the containing class
        var containingClass = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
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

        // Find the containing class
        var containingClass = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
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

