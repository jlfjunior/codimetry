using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Codimetry.Core.Model;

namespace Codimetry.Metrics.Misra;

/// <summary>
/// The attributes (instance variables) a type declaration (class, record or struct) adds to the state of its objects, for AC.
/// Each one is a unit of state, whatever its type:
/// <list type="bullet">
/// <item>instance fields, one per variable (not static, not const);</item>
/// <item>field-like events (they are instance fields of a delegate type);</item>
/// <item>instance properties with their own storage: auto-properties and properties that use <c>field</c>.
/// A property that reads or writes a field of the class is counted by that field, and a computed
/// property (<c>int Total => a + b</c>) holds no state;</item>
/// <item>primary constructor parameters captured as state, i.e. used by a member other than a field or
/// property initializer;</item>
/// <item>positional parameters of a record, which are public properties.</item>
/// </list>
/// Inherited attributes are not declared in the class and do not count.
/// </summary>
public static class InstanceAttributes
{
    public static IEnumerable<(string Name, string Type)> Of(TypeDeclarationSyntax declaration, SemanticModel? semanticModel)
    {
        foreach (var member in declaration.Members)
        {
            switch (member)
            {
                case BaseFieldDeclarationSyntax field when !IsStaticOrConst(field.Modifiers):
                    // Fields and field-like events
                    foreach (var variable in field.Declaration.Variables)
                    {
                        yield return (variable.Identifier.Text, field.Declaration.Type.ToString());
                    }
                    break;
                case PropertyDeclarationSyntax property when !IsStaticOrConst(property.Modifiers) && HasStorage(property, semanticModel):
                    yield return (property.Identifier.Text, property.Type.ToString());
                    break;
            }
        }

        if (declaration.ParameterList != null)
        {
            foreach (var parameter in declaration.ParameterList.Parameters)
            {
                // A positional parameter of a record is a public property with its own storage, unless a
                // member with its name is declared (then that member is the attribute)
                if (declaration is RecordDeclarationSyntax
                        ? !TypeScope.DeclaresMember(declaration, parameter.Identifier.Text, semanticModel)
                        : IsCaptured(parameter, declaration, semanticModel))
                {
                    yield return (parameter.Identifier.Text, parameter.Type?.ToString() ?? string.Empty);
                }
            }
        }
    }

    private static bool IsStaticOrConst(SyntaxTokenList modifiers) =>
        modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword));

    /// <summary>
    /// The property has a backing field of its own. With a semantic model this is exact (the compiler
    /// synthesizes the field); without one, a property whose accessors all lack a body is taken as automatic.
    /// </summary>
    private static bool HasStorage(PropertyDeclarationSyntax property, SemanticModel? semanticModel)
    {
        if (semanticModel?.GetDeclaredSymbol(property) is IPropertySymbol symbol)
        {
            return symbol.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(f =>
                SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, symbol));
        }

        return property.ExpressionBody == null &&
            property.AccessorList != null &&
            !property.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword) || m.IsKind(SyntaxKind.ExternKeyword)) &&
            property.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null);
    }

    /// <summary>
    /// A primary constructor parameter becomes state when a member other than a field or property
    /// initializer uses it. The members may be in any part of a partial class.
    /// </summary>
    private static bool IsCaptured(ParameterSyntax parameter, TypeDeclarationSyntax declaration, SemanticModel? semanticModel)
    {
        var symbol = semanticModel?.GetDeclaredSymbol(parameter);
        IEnumerable<TypeDeclarationSyntax> parts = symbol?.ContainingSymbol.ContainingType is { } type
            ? type.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<TypeDeclarationSyntax>()
            : [declaration];

        foreach (var part in parts)
        {
            var model = semanticModel?.Compilation.GetSemanticModel(part.SyntaxTree);
            var uses = part.Members
                .Where(member => member is not BaseFieldDeclarationSyntax)
                .SelectMany(member => member.DescendantNodes())
                .Where(node => !node.Ancestors().Any(a => a is EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax }))
                .OfType<IdentifierNameSyntax>()
                .Where(identifier => identifier.Identifier.Text == parameter.Identifier.Text);

            if (uses.Any(identifier => model == null ||
                SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol, symbol)))
            {
                return true;
            }
        }

        return false;
    }
}
