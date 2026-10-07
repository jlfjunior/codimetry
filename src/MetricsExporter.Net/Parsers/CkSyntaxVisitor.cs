using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// Extended code element information for CK metrics calculation.
/// </summary>
public class CkCodeElement
{
    public string Name { get; set; } = string.Empty;
    public string? ContainingClass { get; set; }
    public string? BaseClass { get; set; }
    public List<string> ImplementedInterfaces { get; set; } = new();
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public bool IsClass { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public string? Namespace { get; set; }
    public string? FullyQualifiedName { get; set; }
}

/// <summary>
/// Extended method information for CK metrics calculation.
/// </summary>
public class CkMethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string? ContainingClass { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public List<string> CalledMethods { get; set; } = new();
    public List<string> AccessedFields { get; set; } = new();
    public List<string> ParameterTypes { get; set; } = new();
    public string? ReturnType { get; set; }
}

/// <summary>
/// Class dependency information for CBO calculation.
/// </summary>
public class ClassDependency
{
    public string ClassName { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public HashSet<string> DependentTypes { get; set; } = new();
    public List<string> FieldTypes { get; set; } = new();
    public List<string> PropertyTypes { get; set; } = new();
    public HashSet<string> InstanceMemberNames { get; set; } = new();
}

/// <summary>
/// Enhanced syntax visitor that extracts information needed for CK metrics calculation.
/// </summary>
public class CkSyntaxVisitor : CSharpSyntaxWalker
{
    private readonly List<CkCodeElement> _classes = new();
    private readonly List<CkMethodInfo> _methods = new();
    private readonly List<ClassDependency> _dependencies = new();
    private readonly SemanticModel? _semanticModel;

    public CkSyntaxVisitor(SemanticModel? semanticModel = null) : base(SyntaxWalkerDepth.Node)
    {
        _semanticModel = semanticModel;
    }

    public IReadOnlyList<CkCodeElement> GetClasses() => _classes;
    public IReadOnlyList<CkMethodInfo> GetMethods() => _methods;
    public IReadOnlyList<ClassDependency> GetDependencies() => _dependencies;

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var className = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        // Get base class
        string? baseClass = null;
        if (node.BaseList != null)
        {
            var baseType = node.BaseList.Types
                .FirstOrDefault(t => t.Type is IdentifierNameSyntax || t.Type is QualifiedNameSyntax);
            if (baseType != null)
            {
                baseClass = baseType.Type.ToString();
            }
        }

        // Get implemented interfaces
        var interfaces = new List<string>();
        if (node.BaseList != null)
        {
            foreach (var baseType in node.BaseList.Types.Skip(1)) // Skip first (base class)
            {
                interfaces.Add(baseType.Type.ToString());
            }
        }

        // Get namespace
        var namespaceDeclaration = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        var namespaceName = namespaceDeclaration?.Name.ToString();

        // Get fully qualified name
        var fullyQualifiedName = namespaceName != null ? $"{namespaceName}.{className}" : className;

        // Resolve fully qualified name using semantic model if available
        if (_semanticModel != null)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol != null)
            {
                fullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                baseClass = symbol.BaseType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        _classes.Add(new CkCodeElement
        {
            Name = className,
            BaseClass = baseClass,
            ImplementedInterfaces = interfaces,
            StartLine = startLine,
            EndLine = endLine,
            IsClass = true,
            IsAbstract = node.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            IsSealed = node.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword)),
            Namespace = namespaceName,
            FullyQualifiedName = fullyQualifiedName
        });

        // Collect dependencies for this class
        var dependency = new ClassDependency
        {
            ClassName = className,
            Namespace = namespaceName
        };

        // Collect field types
        foreach (var field in node.Members.OfType<FieldDeclarationSyntax>())
        {
            var fieldType = field.Declaration.Type.ToString();
            dependency.FieldTypes.Add(fieldType);
            dependency.DependentTypes.Add(fieldType);

            if (!IsStaticMember(field.Modifiers))
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    dependency.InstanceMemberNames.Add(variable.Identifier.Text);
                }
            }
        }

        // Collect property types
        foreach (var property in node.Members.OfType<PropertyDeclarationSyntax>())
        {
            var propertyType = property.Type.ToString();
            dependency.PropertyTypes.Add(propertyType);
            dependency.DependentTypes.Add(propertyType);

            if (!IsStaticMember(property.Modifiers))
            {
                dependency.InstanceMemberNames.Add(property.Identifier.Text);
            }
        }

        // Collect method parameter and return types
        foreach (var method in node.Members.OfType<MethodDeclarationSyntax>())
        {
            dependency.DependentTypes.Add(method.ReturnType.ToString());
            foreach (var param in method.ParameterList.Parameters)
            {
                dependency.DependentTypes.Add(param.Type?.ToString() ?? string.Empty);
            }
        }

        _dependencies.Add(dependency);

        base.VisitClassDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        var containingClass = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        var className = containingClass?.Identifier.Text;

        var methodInfo = new CkMethodInfo
        {
            Name = methodName,
            ContainingClass = className,
            StartLine = startLine,
            EndLine = endLine,
            IsStatic = node.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            IsAbstract = node.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            ReturnType = node.ReturnType?.ToString()
        };

        // Collect parameter types
        foreach (var param in node.ParameterList.Parameters)
        {
            if (param.Type != null)
            {
                methodInfo.ParameterTypes.Add(param.Type.ToString());
            }
        }

        // Collect method calls and field accesses from method body
        if (node.Body != null)
        {
            var methodCallCollector = new MethodCallCollector();
            methodCallCollector.Visit(node.Body);
            methodInfo.CalledMethods = methodCallCollector.GetMethodCalls();
            methodInfo.AccessedFields = methodCallCollector.GetFieldAccesses();
        }
        else if (node.ExpressionBody != null)
        {
            var methodCallCollector = new MethodCallCollector();
            methodCallCollector.Visit(node.ExpressionBody);
            methodInfo.CalledMethods = methodCallCollector.GetMethodCalls();
            methodInfo.AccessedFields = methodCallCollector.GetFieldAccesses();
        }

        _methods.Add(methodInfo);

        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var constructorName = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        var containingClass = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        var className = containingClass?.Identifier.Text;

        var methodInfo = new CkMethodInfo
        {
            Name = constructorName,
            ContainingClass = className,
            StartLine = startLine,
            EndLine = endLine,
            IsStatic = false,
            ReturnType = null
        };

        // Collect parameter types
        foreach (var param in node.ParameterList.Parameters)
        {
            if (param.Type != null)
            {
                methodInfo.ParameterTypes.Add(param.Type.ToString());
            }
        }

        // Collect method calls and field accesses
        SyntaxNode? body = (SyntaxNode?)node.Body ?? node.ExpressionBody;
        if (body != null)
        {
            var methodCallCollector = new MethodCallCollector();
            methodCallCollector.Visit(body);
            methodInfo.CalledMethods = methodCallCollector.GetMethodCalls();
            methodInfo.AccessedFields = methodCallCollector.GetFieldAccesses();
        }

        _methods.Add(methodInfo);

        base.VisitConstructorDeclaration(node);
    }

    private static bool IsStaticMember(SyntaxTokenList modifiers) =>
        modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword));
}

/// <summary>
/// Helper visitor to collect method calls and field accesses from method bodies.
/// </summary>
internal class MethodCallCollector : CSharpSyntaxWalker
{
    private readonly List<string> _methodCalls = new();
    private readonly List<string> _fieldAccesses = new();

    public List<string> GetMethodCalls() => _methodCalls;
    public List<string> GetFieldAccesses() => _fieldAccesses;

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        // Extract method name
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            _methodCalls.Add(methodName);
        }
        else if (node.Expression is IdentifierNameSyntax identifier)
        {
            _methodCalls.Add(identifier.Identifier.Text);
        }

        base.VisitInvocationExpression(node);
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        // Only this.Member refers to the current instance; other.Member belongs to another object
        if (node.Parent is not InvocationExpressionSyntax &&
            node.Expression is ThisExpressionSyntax &&
            node.Name is IdentifierNameSyntax identifier)
        {
            _fieldAccesses.Add(identifier.Identifier.Text);
        }

        base.VisitMemberAccessExpression(node);
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        // Simple identifier access (could be field or local variable)
        // We'll be conservative and include it. The receiver of a member access
        // (e.g. _logger in _logger.Log()) is also an access to that identifier.
        var isReceiver = node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == node;
        if (node.Parent is not InvocationExpressionSyntax &&
            (node.Parent is not MemberAccessExpressionSyntax || isReceiver))
        {
            _fieldAccesses.Add(node.Identifier.Text);
        }

        base.VisitIdentifierName(node);
    }
}

