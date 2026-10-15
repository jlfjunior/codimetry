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
    /// <summary>
    /// Identity of the class (see <see cref="TypeKeys"/>); the declarations of a partial class share it.
    /// </summary>
    public string? ClassKey { get; set; }
    public string? ContainingClass { get; set; }
    /// <summary>
    /// Key of the base class when the semantic model resolves it, otherwise the base type as written.
    /// </summary>
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
/// A method invocation. When the semantic model binds it, the target is identified by symbol;
/// otherwise only the simple name is known.
/// </summary>
/// <param name="Name">Simple name of the called method, as written.</param>
/// <param name="TargetId">Unique identity of the called method, or null when unresolved.</param>
/// <param name="TargetClassKey">Key of the type that declares the called method, or null when unresolved.</param>
/// <param name="TargetKey">"{classKey}::{methodKey}" when the called method is declared in the analysed code.</param>
/// <param name="IsAbstract">The called method has no body of its own (interface or abstract member).</param>
public sealed record CallInfo(
    string Name,
    string? TargetId = null,
    string? TargetClassKey = null,
    string? TargetKey = null,
    bool IsAbstract = false)
{
    public bool IsResolved => TargetId != null;
}

/// <summary>
/// Extended method information for CK metrics calculation.
/// </summary>
public class CkMethodInfo
{
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Identifies the method within its class (see <see cref="TypeKeys.MethodKey"/>).
    /// </summary>
    public string Key { get; set; } = string.Empty;
    public string? ContainingClass { get; set; }
    /// <summary>
    /// Key of the class that declares the method; null for members of interfaces, structs and records.
    /// </summary>
    public string? ContainingClassKey { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public List<CallInfo> Calls { get; set; } = new();

    /// <summary>
    /// Simple names of the called methods. Setting it records unresolved calls.
    /// </summary>
    public List<string> CalledMethods
    {
        get => Calls.Select(c => c.Name).ToList();
        set => Calls = value.Select(name => new CallInfo(name)).ToList();
    }
    /// <summary>
    /// Attributes of the current object the method uses (see <see cref="InstanceMemberIds"/>), with repetitions.
    /// </summary>
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
    public string ClassKey { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public HashSet<string> DependentTypes { get; set; } = new();
    public List<string> FieldTypes { get; set; } = new();
    public List<string> PropertyTypes { get; set; } = new();
    /// <summary>
    /// Attributes of the class for LCOM (see <see cref="InstanceMemberIds"/>): instance fields, properties
    /// and field-like events declared in the class, and the parameters of its primary constructor.
    /// </summary>
    public HashSet<string> InstanceMemberNames { get; set; } = new();

    /// <summary>
    /// One dependency per class key, joining the declarations of partial classes.
    /// </summary>
    public static List<ClassDependency> MergeByClass(IEnumerable<ClassDependency> dependencies) =>
        dependencies
            .GroupBy(d => d.ClassKey)
            .Select(g => new ClassDependency
            {
                ClassName = g.First().ClassName,
                ClassKey = g.Key,
                Namespace = g.First().Namespace,
                DependentTypes = g.SelectMany(d => d.DependentTypes).ToHashSet(),
                FieldTypes = g.SelectMany(d => d.FieldTypes).ToList(),
                PropertyTypes = g.SelectMany(d => d.PropertyTypes).ToList(),
                InstanceMemberNames = g.SelectMany(d => d.InstanceMemberNames).ToHashSet()
            })
            .ToList();
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

    /// <summary>
    /// Methods whose attribute accesses were matched by name because there was no semantic model.
    /// </summary>
    public int MethodsWithoutSemanticModel { get; private set; }

    /// <summary>
    /// Identifiers the semantic model could not bind to a single symbol although one candidate is an
    /// attribute of the class; they are not counted as accesses.
    /// </summary>
    public int AmbiguousAccesses { get; private set; }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var className = node.Identifier.Text;
        var classKey = TypeKeys.Of(node, _semanticModel);
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
                baseClass = symbol.BaseType != null ? TypeKeys.Of(symbol.BaseType) : null;
            }
        }

        _classes.Add(new CkCodeElement
        {
            Name = className,
            ClassKey = classKey,
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
            ClassKey = classKey,
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
                    dependency.InstanceMemberNames.Add(MemberId(InstanceMemberIds.Field, variable.Identifier.Text));
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
                dependency.InstanceMemberNames.Add(MemberId(InstanceMemberIds.Property, property.Identifier.Text));
            }
        }

        foreach (var eventField in node.Members.OfType<EventFieldDeclarationSyntax>().Where(e => !IsStaticMember(e.Modifiers)))
        {
            foreach (var variable in eventField.Declaration.Variables)
            {
                dependency.InstanceMemberNames.Add(MemberId(InstanceMemberIds.Event, variable.Identifier.Text));
            }
        }

        foreach (var parameter in node.ParameterList?.Parameters ?? default)
        {
            dependency.InstanceMemberNames.Add(MemberId(InstanceMemberIds.PrimaryConstructorParameter, parameter.Identifier.Text));
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

        var containingClass = TypeKeys.ContainingClass(node);
        var className = containingClass?.Identifier.Text;

        var methodInfo = new CkMethodInfo
        {
            Name = methodName,
            Key = TypeKeys.MethodKey(node),
            ContainingClass = className,
            ContainingClassKey = containingClass != null ? TypeKeys.Of(containingClass, _semanticModel) : null,
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
        CollectBody((SyntaxNode?)node.Body ?? node.ExpressionBody, methodInfo);

        _methods.Add(methodInfo);

        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var constructorName = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        var containingClass = TypeKeys.ContainingClass(node);
        var className = containingClass?.Identifier.Text;

        var methodInfo = new CkMethodInfo
        {
            Name = constructorName,
            Key = TypeKeys.MethodKey(node),
            ContainingClass = className,
            ContainingClassKey = containingClass != null ? TypeKeys.Of(containingClass, _semanticModel) : null,
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
        CollectBody((SyntaxNode?)node.Body ?? node.ExpressionBody, methodInfo);

        _methods.Add(methodInfo);

        base.VisitConstructorDeclaration(node);
    }

    private void CollectBody(SyntaxNode? body, CkMethodInfo methodInfo)
    {
        if (body == null)
        {
            return;
        }

        var methodCallCollector = new MethodCallCollector(_semanticModel, methodInfo.ContainingClassKey);
        methodCallCollector.Visit(body);
        methodInfo.Calls = methodCallCollector.GetCalls();
        methodInfo.AccessedFields = methodCallCollector.GetFieldAccesses();

        AmbiguousAccesses += methodCallCollector.AmbiguousAccesses;
        if (_semanticModel == null)
        {
            MethodsWithoutSemanticModel++;
        }
    }

    private string MemberId(string kind, string name) => InstanceMemberIds.Of(kind, name, _semanticModel != null);

    private static bool IsStaticMember(SyntaxTokenList modifiers) =>
        modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword));
}

/// <summary>
/// Helper visitor to collect method calls and field accesses from method bodies.
/// </summary>
internal class MethodCallCollector : CSharpSyntaxWalker
{
    private readonly SemanticModel? _semanticModel;
    private readonly string? _classKey;
    private readonly List<CallInfo> _calls = new();
    private readonly List<string> _fieldAccesses = new();

    /// <param name="classKey">Key of the class that declares the method; its attributes are the accesses recorded.</param>
    public MethodCallCollector(SemanticModel? semanticModel = null, string? classKey = null)
    {
        _semanticModel = semanticModel;
        _classKey = classKey;
    }

    public int AmbiguousAccesses { get; private set; }

    public List<CallInfo> GetCalls() => _calls;
    public List<string> GetMethodCalls() => _calls.Select(c => c.Name).ToList();
    public List<string> GetFieldAccesses() => _fieldAccesses;

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        // Extract method name
        string? methodName = node.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => null
        };

        var target = Resolve(node);

        // nameof(...) is parsed as an invocation but is a compile-time constant, not a call
        var isNameof = target == null && node.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" };

        if (methodName != null && !isNameof)
        {
            _calls.Add(target == null
                ? new CallInfo(methodName)
                : new CallInfo(
                    methodName,
                    TargetId: $"{TypeKeys.Of(target.ContainingType)}::{target.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}",
                    TargetClassKey: TypeKeys.Of(target.ContainingType),
                    TargetKey: TypeKeys.SourceMethodKey(target),
                    IsAbstract: target.IsAbstract));
        }

        base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// The method definition an invocation binds to (an extension method in its static form), or
    /// null when the semantic model cannot decide. A single candidate of a failed overload resolution
    /// is accepted, since the target method is still known.
    /// </summary>
    private IMethodSymbol? Resolve(InvocationExpressionSyntax node)
    {
        if (_semanticModel == null)
        {
            return null;
        }

        var info = _semanticModel.GetSymbolInfo(node);
        var method = info.Symbol as IMethodSymbol
            ?? (info.CandidateSymbols.Length == 1 ? info.CandidateSymbols[0] as IMethodSymbol : null);
        if (method?.ContainingType == null)
        {
            return null;
        }

        return (method.ReducedFrom ?? method).OriginalDefinition;
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (!IsExcludedBySyntax(node))
        {
            if (_semanticModel == null)
            {
                // Without a semantic model, any identifier other than a called name may be an attribute
                if (!IsCalledName(node))
                {
                    _fieldAccesses.Add(node.Identifier.Text);
                }
            }
            else
            {
                var info = _semanticModel.GetSymbolInfo(node);
                var id = AttributeId(info.Symbol);
                if (id != null)
                {
                    _fieldAccesses.Add(id);
                }
                else if (info.Symbol == null && info.CandidateSymbols.Any(c => AttributeId(c) != null))
                {
                    AmbiguousAccesses++;
                }
            }
        }

        base.VisitIdentifierName(node);
    }

    /// <summary>
    /// Positions where the identifier names a member without using the current object's state, even
    /// when it binds to an attribute of the class: another object's member, an initializer or pattern
    /// target, an argument name, or the operand of nameof.
    /// </summary>
    private bool IsExcludedBySyntax(IdentifierNameSyntax node)
    {
        switch (node.Parent)
        {
            // other.X and base.X belong to another object (or to the base class); only this.X counts
            case MemberAccessExpressionSyntax memberAccess when memberAccess.Name == node &&
                memberAccess.Expression is not ThisExpressionSyntax:
            case MemberBindingExpressionSyntax:
            case NameEqualsSyntax:
                return true;
            // new T { X = ... }: X is a member of the created object
            case AssignmentExpressionSyntax assignment when assignment.Left == node &&
                assignment.Parent is InitializerExpressionSyntax initializer &&
                initializer.IsKind(SyntaxKind.ObjectInitializerExpression):
                return true;
        }

        // Argument names (f(x: 1)) and property patterns ({ X: 1 }, { X.Y: 1 })
        SyntaxNode top = node;
        while (top.Parent is MemberAccessExpressionSyntax)
        {
            top = top.Parent;
        }

        if (top.Parent is BaseExpressionColonSyntax)
        {
            return true;
        }

        return node.Ancestors()
            .TakeWhile(a => a is ExpressionSyntax or ArgumentSyntax or ArgumentListSyntax)
            .OfType<InvocationExpressionSyntax>()
            .Any(IsNameof);
    }

    private bool IsNameof(InvocationExpressionSyntax invocation) =>
        invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" } &&
        _semanticModel?.GetSymbolInfo(invocation).Symbol == null;

    private static bool IsCalledName(IdentifierNameSyntax node) =>
        node.Parent is InvocationExpressionSyntax ||
        node.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } memberAccess && memberAccess.Name == node;

    /// <summary>
    /// Identity of the symbol when it is an attribute of the method's class: an instance field,
    /// property or event declared in it, or a parameter of its primary constructor. Null otherwise.
    /// </summary>
    private string? AttributeId(ISymbol? symbol)
    {
        switch (symbol?.OriginalDefinition)
        {
            case IFieldSymbol { IsStatic: false } field when IsOwnType(field.ContainingType):
                return InstanceMemberIds.Of(InstanceMemberIds.Field, field.Name, semantic: true);
            case IPropertySymbol { IsStatic: false } property when IsOwnType(property.ContainingType):
                return InstanceMemberIds.Of(InstanceMemberIds.Property, property.Name, semantic: true);
            case IEventSymbol { IsStatic: false } @event when IsOwnType(@event.ContainingType):
                return InstanceMemberIds.Of(InstanceMemberIds.Event, @event.Name, semantic: true);
            case IParameterSymbol parameter when
                parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor &&
                IsOwnType(constructor.ContainingType) &&
                IsPrimaryConstructorParameter(parameter):
                return InstanceMemberIds.Of(InstanceMemberIds.PrimaryConstructorParameter, parameter.Name, semantic: true);
            default:
                return null;
        }
    }

    private bool IsOwnType(INamedTypeSymbol? type) => type != null && _classKey != null && TypeKeys.Of(type) == _classKey;

    // Roslyn has no public API that marks the primary constructor: its parameters are declared in the
    // parameter list of the type declaration, those of an ordinary constructor in the constructor.
    private static bool IsPrimaryConstructorParameter(IParameterSymbol parameter) =>
        parameter.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is ParameterSyntax { Parent.Parent: TypeDeclarationSyntax });
}

/// <summary>
/// Identity of an attribute for LCOM. With a semantic model it is "{kind}:{name}", so that a primary
/// constructor parameter and a member with the same name are distinct; without one, the name alone.
/// The class is implied: only attributes of the method's own class are recorded.
/// </summary>
public static class InstanceMemberIds
{
    public const string Field = "field";
    public const string Property = "prop";
    public const string Event = "event";
    public const string PrimaryConstructorParameter = "param";

    public static string Of(string kind, string name, bool semantic) => semantic ? $"{kind}:{name}" : name;
}
