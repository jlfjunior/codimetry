using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// Stable identifiers for classes and methods.
/// A class key is "{assembly}|{Namespace.Outer.Name`arity}"; the assembly part identifies the
/// project, so homonymous classes in different namespaces or projects get different keys and the
/// declarations of a partial class share one key.
/// </summary>
public static class TypeKeys
{
    /// <summary>
    /// Key of a type symbol. Works for source types and for types referenced from metadata.
    /// </summary>
    public static string Of(INamedTypeSymbol symbol)
    {
        var type = symbol.OriginalDefinition;
        var assembly = type.ContainingAssembly?.Name ?? string.Empty;
        return $"{assembly}|{QualifiedName(type)}";
    }

    /// <summary>
    /// Key of a class declaration: from the semantic model when available, otherwise from the
    /// syntax alone (namespace, enclosing types, name and arity, without the assembly part).
    /// </summary>
    public static string Of(TypeDeclarationSyntax declaration, SemanticModel? semanticModel)
    {
        if (semanticModel?.GetDeclaredSymbol(declaration) is INamedTypeSymbol symbol)
        {
            return Of(symbol);
        }

        var parts = new List<string>();
        foreach (var node in declaration.AncestorsAndSelf())
        {
            switch (node)
            {
                case TypeDeclarationSyntax type:
                    parts.Add(WithArity(type.Identifier.Text, type.TypeParameterList?.Parameters.Count ?? 0));
                    break;
                case BaseNamespaceDeclarationSyntax ns:
                    parts.Add(ns.Name.ToString());
                    break;
            }
        }

        parts.Reverse();
        return string.Join(".", parts);
    }

    /// <summary>
    /// The class that owns a member: the nearest enclosing type declaration, if it is a class.
    /// Members of interfaces, structs and records (even when nested in a class) belong to no class.
    /// </summary>
    public static ClassDeclarationSyntax? ContainingClass(SyntaxNode member) =>
        member.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() as ClassDeclarationSyntax;

    /// <summary>
    /// Key of a method or constructor within its class.
    /// </summary>
    public static string MethodKey(BaseMethodDeclarationSyntax method) => method switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        ConstructorDeclarationSyntax c => c.Identifier.Text,
        _ => method.ToString()
    };

    private static string QualifiedName(INamedTypeSymbol type)
    {
        var name = WithArity(type.Name, type.Arity);
        if (type.ContainingType != null)
        {
            return $"{QualifiedName(type.ContainingType)}.{name}";
        }

        var ns = type.ContainingNamespace;
        return ns == null || ns.IsGlobalNamespace ? name : $"{ns.ToDisplayString()}.{name}";
    }

    private static string WithArity(string name, int arity) => arity > 0 ? $"{name}`{arity}" : name;
}
