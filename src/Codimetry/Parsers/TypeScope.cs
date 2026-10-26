using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codimetry.Parsers;

/// <summary>
/// The types measured as "classes" by every metric: classes, records (<c>record</c>,
/// <c>record class</c>), structs and record structs. Interfaces and enums are not measured. The same
/// scope is used by every service, so that all CSV files agree on which types exist.
/// </summary>
/// <param name="ClassesOnly">Measure only classes, as v1 did (--classes-only).</param>
public sealed record TypeScope(bool ClassesOnly = false)
{
    public static TypeScope Default { get; } = new();

    public bool IsMeasured(TypeDeclarationSyntax declaration) => declaration switch
    {
        ClassDeclarationSyntax => true,
        RecordDeclarationSyntax or StructDeclarationSyntax => !ClassesOnly,
        _ => false
    };

    /// <summary>
    /// The measured type that owns a member: the nearest enclosing type declaration, if it is measured.
    /// A member of an interface (or of a record or struct with --classes-only) belongs to no type, never
    /// to an enclosing class.
    /// </summary>
    public TypeDeclarationSyntax? ContainingType(SyntaxNode member)
    {
        var nearest = member.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return nearest != null && IsMeasured(nearest) ? nearest : null;
    }

    /// <summary>
    /// Value of the Kind column: class, record, struct or record struct.
    /// </summary>
    public static string KindOf(TypeDeclarationSyntax declaration) => declaration.Kind() switch
    {
        SyntaxKind.RecordDeclaration => "record",
        SyntaxKind.RecordStructDeclaration => "record struct",
        SyntaxKind.StructDeclaration => "struct",
        _ => "class"
    };

    /// <summary>
    /// Structs and record structs: value types, which have no base class (DIT = NOC = 0).
    /// </summary>
    public static bool IsValueType(TypeDeclarationSyntax declaration) =>
        declaration.IsKind(SyntaxKind.StructDeclaration) || declaration.IsKind(SyntaxKind.RecordStructDeclaration);

    /// <summary>
    /// The type declares a field or property with this name. A positional parameter of a record becomes a
    /// public property unless such a member exists; with a semantic model all partial declarations are seen.
    /// </summary>
    public static bool DeclaresMember(TypeDeclarationSyntax declaration, string name, SemanticModel? semanticModel)
    {
        if (semanticModel?.GetDeclaredSymbol(declaration) is INamedTypeSymbol type)
        {
            // The property synthesized from the parameter is declared by the parameter itself
            return type.GetMembers(name).Any(m => m is IFieldSymbol or IPropertySymbol &&
                m.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is PropertyDeclarationSyntax or VariableDeclaratorSyntax));
        }

        return declaration.Members.Any(member => member switch
        {
            PropertyDeclarationSyntax property => property.Identifier.Text == name,
            FieldDeclarationSyntax field => field.Declaration.Variables.Any(v => v.Identifier.Text == name),
            _ => false
        });
    }
}
