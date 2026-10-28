using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Codimetry.Core.Cyclomatic;

namespace Codimetry.Core.Model;

/// <summary>
/// How DIT treats the ancestors outside the analysed code.
/// </summary>
/// <param name="CountObject">System.Object is the base of a class without an explicit one and counts as one
/// level, as in earlier versions (--dit-count-object).</param>
/// <param name="ResolveFramework">Also count the ancestors of an external base class, except System.Object
/// (--dit-resolve-framework): <c>D : Controller</c> is D → Controller → ControllerBase = 2.</param>
public sealed record DitOptions(bool CountObject = false, bool ResolveFramework = false)
{
    public static DitOptions Default { get; } = new();
}

/// <summary>
/// A measured type (class, record, struct or record struct) declaration.
/// </summary>
public class TypeElement
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
    /// <summary>
    /// class, record, struct or record struct (see <see cref="TypeScope.KindOf"/>).
    /// </summary>
    public string Kind { get; set; } = "class";
    /// <summary>
    /// Ancestors of the base class above it, System.Object excluded (Controller → ControllerBase = 1); used
    /// for DIT when the base class is outside the analysed code and --dit-resolve-framework is on.
    /// </summary>
    public int BaseAncestors { get; set; }
}

/// <summary>
/// What a call executes: a method, or a constructor (<c>new X()</c>, <c>new()</c>, <c>: base(...)</c>, <c>: this(...)</c>).
/// </summary>
public enum CallKind
{
    Method,
    Constructor
}

/// <summary>
/// A method invocation or a constructor call. When the semantic model binds it, the target is identified by symbol;
/// otherwise only the simple name is known.
/// </summary>
/// <param name="Name">Simple name of the called method, as written; ".ctor" for a resolved constructor, the name of the
/// created type for an unresolved one.</param>
/// <param name="TargetId">Unique identity of the called method, or null when unresolved.</param>
/// <param name="TargetClassKey">Key of the type that declares the called method, or null when unresolved.</param>
/// <param name="TargetKey">"{classKey}::{methodKey}" when the called method is declared in the analysed code.</param>
/// <param name="IsAbstract">The called method has no body of its own (interface or abstract member).</param>
/// <param name="Kind">Method or constructor.</param>
public sealed record CallInfo(
    string Name,
    string? TargetId = null,
    string? TargetClassKey = null,
    string? TargetKey = null,
    bool IsAbstract = false,
    CallKind Kind = CallKind.Method)
{
    public bool IsResolved => TargetId != null;
}

/// <summary>
/// A method or constructor of a measured type.
/// </summary>
public class MethodElement
{
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Identifies the method within its class (see <see cref="TypeKeys.MethodKey"/>).
    /// </summary>
    public string Key { get; set; } = string.Empty;
    public string? ContainingClass { get; set; }
    /// <summary>
    /// Key of the type that declares the method; null for members of types outside the <see cref="TypeScope"/>.
    /// </summary>
    public string? ContainingClassKey { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public bool IsStatic { get; set; }
    public bool IsConstructor { get; set; }
    public bool IsAbstract { get; set; }
    /// <summary>
    /// V(G) of the method, by the same visitor as metrics_cyclomatic.csv; 1 when it has no body.
    /// </summary>
    public int CyclomaticComplexity { get; set; } = 1;
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
/// Collects the structure of the measured types that several suites share: the types with their base class,
/// and the methods and constructors with their calls, attribute accesses and V(G).
/// </summary>
public class TypeModelCollector : CSharpSyntaxWalker
{
    private readonly List<TypeElement> _classes = new();
    private readonly List<MethodElement> _methods = new();
    private readonly CyclomaticOptions _cyclomaticOptions;
    private readonly TypeScope _scope;
    private readonly DitOptions _ditOptions;
    private readonly SemanticModel? _semanticModel;

    /// <param name="cyclomaticOptions">Operator rule for the V(G) of each method (WMC with c_i = V(G)).</param>
    /// <param name="scope">Which types are measured (--classes-only).</param>
    /// <param name="ditOptions">Whether System.Object is recorded as a base class (--dit-count-object).</param>
    public TypeModelCollector(SemanticModel? semanticModel = null, CyclomaticOptions? cyclomaticOptions = null, TypeScope? scope = null,
        DitOptions? ditOptions = null)
        : base(SyntaxWalkerDepth.Node)
    {
        _ditOptions = ditOptions ?? DitOptions.Default;
        _scope = scope ?? TypeScope.Default;
        _cyclomaticOptions = cyclomaticOptions ?? CyclomaticOptions.Default;
        _semanticModel = semanticModel;
    }

    public IReadOnlyList<TypeElement> GetClasses() => _classes;
    public IReadOnlyList<MethodElement> GetMethods() => _methods;

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
        CollectType(node);
        base.VisitClassDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        CollectType(node);
        base.VisitRecordDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        CollectType(node);
        base.VisitStructDeclaration(node);
    }

    private void CollectType(TypeDeclarationSyntax node)
    {
        if (!_scope.IsMeasured(node))
        {
            return;
        }

        var isValueType = TypeScope.IsValueType(node);
        var className = node.Identifier.Text;
        var classKey = TypeKeys.Of(node, _semanticModel);
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        // Get base class (a struct has none: its base list holds only interfaces)
        string? baseClass = null;
        var baseAncestors = 0;
        if (node.BaseList != null && !isValueType)
        {
            var baseType = node.BaseList.Types
                .FirstOrDefault(t => t.Type is IdentifierNameSyntax || t.Type is QualifiedNameSyntax);
            // class E : System.Object is the same as no base class
            if (baseType != null && baseType.Type.ToString() is not ("Object" or "System.Object" or "global::System.Object"))
            {
                baseClass = baseType.Type.ToString();
            }
        }

        // Get implemented interfaces
        var interfaces = new List<string>();
        if (node.BaseList != null)
        {
            foreach (var baseType in node.BaseList.Types.Skip(isValueType ? 0 : 1)) // Skip first (base class)
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
                // Only declared inheritance counts: the implicit or explicit System.Object is no base class
                var isObject = symbol.BaseType?.SpecialType == SpecialType.System_Object;
                baseClass = symbol.BaseType != null && !isValueType && (!isObject || _ditOptions.CountObject)
                    ? TypeKeys.Of(symbol.BaseType)
                    : null;
                baseAncestors = Ancestors(symbol.BaseType);
            }
        }

        _classes.Add(new TypeElement
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
            FullyQualifiedName = fullyQualifiedName,
            Kind = TypeScope.KindOf(node),
            BaseAncestors = isValueType ? 0 : baseAncestors
        });
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;
        var startLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        var containingClass = _scope.ContainingType(node);
        var className = containingClass?.Identifier.Text;

        var methodInfo = new MethodElement
        {
            Name = methodName,
            Key = TypeKeys.MethodKey(node),
            ContainingClass = className,
            ContainingClassKey = containingClass != null ? TypeKeys.Of(containingClass, _semanticModel) : null,
            StartLine = startLine,
            EndLine = endLine,
            IsStatic = node.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            IsAbstract = node.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            CyclomaticComplexity = CyclomaticComplexityOf((SyntaxNode?)node.Body ?? node.ExpressionBody),
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

        var containingClass = _scope.ContainingType(node);
        var className = containingClass?.Identifier.Text;

        var methodInfo = new MethodElement
        {
            Name = constructorName,
            Key = TypeKeys.MethodKey(node),
            ContainingClass = className,
            ContainingClassKey = containingClass != null ? TypeKeys.Of(containingClass, _semanticModel) : null,
            StartLine = startLine,
            EndLine = endLine,
            IsStatic = node.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            IsConstructor = true,
            CyclomaticComplexity = CyclomaticComplexityOf((SyntaxNode?)node.Body ?? node.ExpressionBody),
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

        // Collect method calls and field accesses; the constructor initializer (: base(...), : this(...)) is a call too
        CollectBody((SyntaxNode?)node.Body ?? node.ExpressionBody, methodInfo, node.Initializer);

        _methods.Add(methodInfo);

        base.VisitConstructorDeclaration(node);
    }

    private void CollectBody(SyntaxNode? body, MethodElement methodInfo, ConstructorInitializerSyntax? initializer = null)
    {
        if (body == null && initializer == null)
        {
            return;
        }

        var methodCallCollector = new MethodCallCollector(_semanticModel, methodInfo.ContainingClassKey);
        if (initializer != null)
        {
            methodCallCollector.Visit(initializer);
        }

        if (body != null)
        {
            methodCallCollector.Visit(body);
        }

        methodInfo.Calls = methodCallCollector.GetCalls();
        methodInfo.AccessedFields = methodCallCollector.GetFieldAccesses();

        AmbiguousAccesses += methodCallCollector.AmbiguousAccesses;
        if (_semanticModel == null)
        {
            MethodsWithoutSemanticModel++;
        }
    }

    /// <summary>
    /// V(G) of a body; a method without one (abstract, interface, extern) has a single path.
    /// </summary>
    private int CyclomaticComplexityOf(SyntaxNode? body)
    {
        if (body == null)
        {
            return 1;
        }

        var visitor = new CyclomaticComplexityVisitor(_cyclomaticOptions);
        visitor.Visit(body);
        return visitor.GetComplexity();
    }

    /// <summary>
    /// Ancestors of a base type above it, System.Object excluded. Types from referenced assemblies are
    /// followed too; an unresolved ancestor ends the count.
    /// </summary>
    private static int Ancestors(INamedTypeSymbol? baseType)
    {
        var count = 0;
        for (var type = baseType?.BaseType; type != null && type.SpecialType != SpecialType.System_Object && type.TypeKind != TypeKind.Error; type = type.BaseType)
        {
            count++;
        }

        return count;
    }

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

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var target = Resolve(node);
        if (target != null)
        {
            AddConstructor(target);
        }
        else if (CreatedTypeName(node) is { } name)
        {
            _calls.Add(new CallInfo(name, Kind: CallKind.Constructor));
        }

        base.VisitObjectCreationExpression(node);
    }

    public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
    {
        // Without a resolved constructor there is no written type to name it by
        if (Resolve(node) is { } target)
        {
            AddConstructor(target);
        }

        base.VisitImplicitObjectCreationExpression(node);
    }

    public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
    {
        if (Resolve(node) is { } target)
        {
            AddConstructor(target);
        }

        base.VisitConstructorInitializer(node);
    }

    /// <summary>
    /// A call to a constructor, declared or implicit (a class without constructors still has one). Creations of
    /// delegates (<c>new Action(M)</c>) run no constructor of the analysed code and are left out.
    /// </summary>
    private void AddConstructor(IMethodSymbol target)
    {
        if (target.MethodKind != MethodKind.Constructor || target.ContainingType.TypeKind == TypeKind.Delegate)
        {
            return;
        }

        _calls.Add(new CallInfo(
            ".ctor",
            TargetId: $"{TypeKeys.Of(target.ContainingType)}::{target.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}",
            TargetClassKey: TypeKeys.Of(target.ContainingType),
            TargetKey: TypeKeys.SourceMethodKey(target),
            Kind: CallKind.Constructor));
    }

    /// <summary>
    /// Simple name of the type an unresolved <c>new X(...)</c> creates, or null when it is a type parameter
    /// (<c>new T()</c>, which has no concrete constructor) or a delegate.
    /// </summary>
    private string? CreatedTypeName(ObjectCreationExpressionSyntax node)
    {
        if (_semanticModel?.GetTypeInfo(node).Type is { TypeKind: TypeKind.TypeParameter or TypeKind.Delegate })
        {
            return null;
        }

        var name = node.Type switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
            SimpleNameSyntax simple => simple.Identifier.Text,
            _ => null
        };

        var isTypeParameter = name != null && node.Ancestors()
            .SelectMany(a => a switch
            {
                MethodDeclarationSyntax m => m.TypeParameterList?.Parameters ?? default,
                LocalFunctionStatementSyntax f => f.TypeParameterList?.Parameters ?? default,
                TypeDeclarationSyntax t => t.TypeParameterList?.Parameters ?? default,
                _ => default(SeparatedSyntaxList<TypeParameterSyntax>)
            })
            .Any(p => p.Identifier.Text == name);
        return isTypeParameter ? null : name;
    }

    /// <summary>
    /// The method definition an invocation, object creation or constructor initializer binds to (an extension
    /// method in its static form), or null when the semantic model cannot decide. A single candidate of a failed
    /// overload resolution is accepted, since the target method is still known.
    /// </summary>
    private IMethodSymbol? Resolve(SyntaxNode node)
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
