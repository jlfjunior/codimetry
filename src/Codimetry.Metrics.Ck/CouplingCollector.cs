using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Codimetry.Core.Model;

namespace Codimetry.Metrics.Ck;

/// <summary>
/// A type of the analysed code that a class can be coupled to (CBO): a measured type or an interface.
/// </summary>
public sealed record CouplingTarget(string Key, string Name, string? Namespace);

/// <summary>
/// Class dependency information for CBO calculation.
/// </summary>
public class ClassDependency
{
    public string ClassName { get; set; } = string.Empty;
    public string ClassKey { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    /// <summary>
    /// Keys (see <see cref="TypeKeys"/>) of the types the class references, resolved by the semantic model;
    /// generic arguments and array element types are included.
    /// </summary>
    public HashSet<string> DependentTypes { get; set; } = new();
    /// <summary>
    /// Simple names of the referenced types the semantic model could not resolve (or all of them, without a
    /// model); CBO matches them by exact name.
    /// </summary>
    public HashSet<string> UnresolvedTypeNames { get; set; } = new();
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
                UnresolvedTypeNames = g.SelectMany(d => d.UnresolvedTypeNames).ToHashSet(),
                FieldTypes = g.SelectMany(d => d.FieldTypes).ToList(),
                PropertyTypes = g.SelectMany(d => d.PropertyTypes).ToList(),
                InstanceMemberNames = g.SelectMany(d => d.InstanceMemberNames).ToHashSet()
            })
            .ToList();
}

/// <summary>
/// Collects what CBO and LCOM need of each measured type: the types it references and its attributes. Interfaces
/// are not measured but are coupling targets.
/// </summary>
public class CouplingCollector : CSharpSyntaxWalker
{
    private readonly TypeScope _scope;
    private readonly CboOptions _cboOptions;
    private readonly List<ClassDependency> _dependencies = new();
    private readonly List<CouplingTarget> _interfaces = new();
    private readonly SemanticModel? _semanticModel;

    /// <param name="scope">Which types are measured (--classes-only).</param>
    /// <param name="cboOptions">Which type references CBO collects (--cbo-declared-types-only).</param>
    public CouplingCollector(SemanticModel? semanticModel = null, TypeScope? scope = null, CboOptions? cboOptions = null)
        : base(SyntaxWalkerDepth.Node)
    {
        _cboOptions = cboOptions ?? CboOptions.Default;
        _scope = scope ?? TypeScope.Default;
        _semanticModel = semanticModel;
    }

    public IReadOnlyList<ClassDependency> GetDependencies() => _dependencies;

    /// <summary>
    /// Interfaces declared in the code: not measured, but coupling targets of CBO.
    /// </summary>
    public IReadOnlyList<CouplingTarget> GetInterfaces() => _interfaces;

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        CollectType(node);
        base.VisitClassDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var namespaceName = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
        _interfaces.Add(new CouplingTarget(TypeKeys.Of(node, _semanticModel), node.Identifier.Text, namespaceName));
        base.VisitInterfaceDeclaration(node);
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

        var className = node.Identifier.Text;
        var classKey = TypeKeys.Of(node, _semanticModel);
        var namespaceName = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();

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
            AddDeclaredType(dependency, field.Declaration.Type);

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
            AddDeclaredType(dependency, property.Type);

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
            var name = parameter.Identifier.Text;
            if (node is RecordDeclarationSyntax)
            {
                // A positional parameter of a record is a public property, unless a member with its name is declared
                if (!TypeScope.DeclaresMember(node, name, _semanticModel))
                {
                    var parameterType = parameter.Type?.ToString() ?? string.Empty;
                    dependency.PropertyTypes.Add(parameterType);
                    AddDeclaredType(dependency, parameter.Type);
                    dependency.InstanceMemberNames.Add(MemberId(InstanceMemberIds.Property, name));
                }
            }
            else
            {
                dependency.InstanceMemberNames.Add(MemberId(InstanceMemberIds.PrimaryConstructorParameter, name));
            }
        }

        // Collect method parameter and return types
        foreach (var method in node.Members.OfType<MethodDeclarationSyntax>())
        {
            AddDeclaredType(dependency, method.ReturnType);
            foreach (var param in method.ParameterList.Parameters)
            {
                AddDeclaredType(dependency, param.Type);
            }
        }

        if (!_cboOptions.DeclaredTypesOnly)
        {
            TypeReferenceCollector.Collect(node, _semanticModel, dependency);
        }

        _dependencies.Add(dependency);
    }

    /// <summary>
    /// A type of the class's fields, properties and method signatures: the whole legacy CBO (--cbo-declared-types-only),
    /// a subset of what <see cref="TypeReferenceCollector"/> collects otherwise.
    /// </summary>
    private void AddDeclaredType(ClassDependency dependency, TypeSyntax? type)
    {
        if (_cboOptions.DeclaredTypesOnly)
        {
            TypeReferenceCollector.Add(dependency, type, _semanticModel);
        }
    }
    private string MemberId(string kind, string name) => InstanceMemberIds.Of(kind, name, _semanticModel != null);

    private static bool IsStaticMember(SyntaxTokenList modifiers) =>
        modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword));
}
