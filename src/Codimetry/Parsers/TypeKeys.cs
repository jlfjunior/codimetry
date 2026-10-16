using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codimetry.Parsers;

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
    /// Key of a method or constructor within its class: name, arity and parameter list as written,
    /// e.g. "Save(ref int,string)", so that overloads do not overwrite each other.
    /// </summary>
    public static string MethodKey(BaseMethodDeclarationSyntax method)
    {
        var name = method switch
        {
            MethodDeclarationSyntax m => WithArity(m.Identifier.Text, m.TypeParameterList?.Parameters.Count ?? 0),
            ConstructorDeclarationSyntax c => c.Identifier.Text,
            _ => method.Kind().ToString()
        };

        var parameters = method.ParameterList.Parameters.Select(p =>
            string.Join(" ", p.Modifiers.Select(m => m.Text).Append(Normalize(p.Type?.ToString() ?? string.Empty))));
        return $"{name}({string.Join(",", parameters)})";
    }

    /// <summary>
    /// "{classKey}::{methodKey}" of a method declared in the analysed source code (the key used for
    /// MC), or null for methods from referenced assemblies, local functions and accessors.
    /// </summary>
    public static string? SourceMethodKey(IMethodSymbol method)
    {
        var definition = method.ReducedFrom ?? method;
        definition = definition.OriginalDefinition;
        if (definition.ContainingType == null)
        {
            return null;
        }

        var declaration = definition.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<BaseMethodDeclarationSyntax>()
            .FirstOrDefault();
        return declaration == null ? null : $"{Of(definition.ContainingType)}::{MethodKey(declaration)}";
    }

    private static string Normalize(string typeText) =>
        string.Concat(typeText.Where(c => !char.IsWhiteSpace(c)));

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
