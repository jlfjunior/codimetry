using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// How CBO collects and counts couplings (D8, D9).
/// </summary>
/// <param name="DeclaredTypesOnly">Only the types of fields, properties, method parameters and return types, and
/// only classes as targets, as v1 did (--cbo-declared-types-only); types are still matched by key (INC-10).</param>
/// <param name="Bidirectional">Also count the types that use the class, as in CK 1994 (--cbo-bidirectional).</param>
public sealed record CboOptions(bool DeclaredTypesOnly = false, bool Bidirectional = false)
{
    public static CboOptions Default { get; } = new();
}

/// <summary>
/// Collects the types a type declaration references (INC-11): base class and interfaces, member signatures,
/// locals, <c>new</c>, patterns, <c>typeof</c>, casts, <c>catch</c>, attributes, generic arguments, receivers of
/// static members, and the types whose members it uses. Nested types are left to their own declaration.
/// </summary>
public static class TypeReferenceCollector
{
    public static void Collect(TypeDeclarationSyntax declaration, SemanticModel? semanticModel, ClassDependency dependency)
    {
        var ancestors = semanticModel?.GetDeclaredSymbol(declaration) is INamedTypeSymbol self
            ? Ancestors(self)
            : new HashSet<string>();

        var nodes = declaration.DescendantNodes(n => n == declaration || n is not (BaseTypeDeclarationSyntax or DelegateDeclarationSyntax));
        foreach (var node in nodes)
        {
            switch (node)
            {
                case SimpleNameSyntax name:
                    CollectName(name, semanticModel, dependency, ancestors);
                    break;
                case ImplicitObjectCreationExpressionSyntax creation when semanticModel?.GetTypeInfo(creation).Type is { } created:
                    Add(dependency, created);
                    break;
            }
        }
    }

    /// <summary>
    /// Records the types a type reference names, with the semantic model the key of each named type,
    /// decomposing generic arguments, array elements and nullables (<c>Dictionary&lt;string, List&lt;Item&gt;&gt;</c>
    /// gives Dictionary, string, List and Item); without it, or when a type does not resolve, its simple name.
    /// </summary>
    public static void Add(ClassDependency dependency, TypeSyntax? type, SemanticModel? semanticModel)
    {
        if (type == null)
        {
            return;
        }

        if (semanticModel?.GetTypeInfo(type).Type is { } symbol)
        {
            Add(dependency, symbol);
            return;
        }

        foreach (var name in type.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().Where(IsTypeName))
        {
            dependency.UnresolvedTypeNames.Add(name.Identifier.Text);
        }
    }

    public static void Add(ClassDependency dependency, ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                Add(dependency, array.ElementType);
                break;
            case IPointerTypeSymbol pointer:
                Add(dependency, pointer.PointedAtType);
                break;
            case INamedTypeSymbol { TypeKind: TypeKind.Error } error:
                dependency.UnresolvedTypeNames.Add(error.Name);
                break;
            case INamedTypeSymbol named:
                dependency.DependentTypes.Add(TypeKeys.Of(named));
                foreach (var argument in named.TypeArguments)
                {
                    Add(dependency, argument);
                }
                break;
        }
    }

    private static void CollectName(SimpleNameSyntax name, SemanticModel? semanticModel, ClassDependency dependency, HashSet<string> ancestors)
    {
        if (semanticModel == null)
        {
            // Without a model only type positions can be told apart from variables; static receivers are lost
            if (SyntaxFacts.IsInTypeOnlyContext(name) && !name.IsVar && IsTypeName(name))
            {
                dependency.UnresolvedTypeNames.Add(name.Identifier.Text);
            }

            return;
        }

        var symbol = semanticModel.GetSymbolInfo(name).Symbol;
        switch (symbol)
        {
            case ITypeSymbol type:
                Add(dependency, type);
                break;
            // Use of a method, field, property or event of another type; members inherited from the class's
            // own ancestors are its own state and behaviour (the base class is already coupled by inheritance)
            case IMethodSymbol or IFieldSymbol or IPropertySymbol or IEventSymbol when symbol.ContainingType is { } owner:
                if (owner.TypeKind != TypeKind.Error && !ancestors.Contains(TypeKeys.Of(owner)))
                {
                    dependency.DependentTypes.Add(TypeKeys.Of(owner));
                }
                break;
            case null when SyntaxFacts.IsInTypeOnlyContext(name) && IsTypeName(name):
                dependency.UnresolvedTypeNames.Add(name.Identifier.Text);
                break;
        }
    }

    private static HashSet<string> Ancestors(INamedTypeSymbol type)
    {
        var keys = new HashSet<string>();
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            keys.Add(TypeKeys.Of(current));
        }

        return keys;
    }

    /// <summary>
    /// A simple name inside a type reference names a type unless it is a qualifier (the <c>N</c> of <c>N.Logger</c>).
    /// </summary>
    private static bool IsTypeName(SimpleNameSyntax name)
    {
        SyntaxNode node = name;
        while (node.Parent is NameSyntax parent and (QualifiedNameSyntax or AliasQualifiedNameSyntax))
        {
            if (parent is QualifiedNameSyntax { Left: var left } && left == node
                || parent is AliasQualifiedNameSyntax { Alias: var alias } && alias == node)
            {
                return false;
            }

            node = parent;
        }

        return true;
    }
}
