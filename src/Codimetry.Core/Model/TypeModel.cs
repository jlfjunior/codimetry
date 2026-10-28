using Codimetry.Core.Cyclomatic;
using Codimetry.Core.Loading;
using Microsoft.Extensions.Logging;

namespace Codimetry.Core.Model;

/// <summary>
/// The measured types of the codebase and their methods, collected once by <see cref="TypeModelCollector"/>
/// for every suite that reads them (CK and MISRA), with the inheritance graph they form.
/// </summary>
public sealed class TypeModel
{
    private TypeModel(List<TypeElement> classes, List<MethodElement> methods, ClassRelationshipBuilder relationships,
        int methodsWithoutSemanticModel, int ambiguousAccesses)
    {
        Classes = classes;
        Methods = methods;
        Relationships = relationships;
        MethodsWithoutSemanticModel = methodsWithoutSemanticModel;
        AmbiguousAccesses = ambiguousAccesses;
    }

    /// <summary>
    /// Type declarations in source order; a partial type has one per declaration.
    /// </summary>
    public IReadOnlyList<TypeElement> Classes { get; }

    public IReadOnlyList<MethodElement> Methods { get; }

    public ClassRelationshipBuilder Relationships { get; }

    /// <summary>
    /// Methods whose attribute accesses were matched by name because there was no semantic model.
    /// </summary>
    public int MethodsWithoutSemanticModel { get; }

    /// <summary>
    /// Identifiers not counted as attribute accesses because the semantic model could not bind them to a single symbol.
    /// </summary>
    public int AmbiguousAccesses { get; }

    /// <param name="cyclomaticOptions">Operator rule for the V(G) of each method.</param>
    /// <param name="scope">Which types are measured (--classes-only).</param>
    /// <param name="ditOptions">Whether System.Object is recorded as a base class (--dit-count-object).</param>
    public static TypeModel Build(Codebase codebase, CyclomaticOptions? cyclomaticOptions = null, TypeScope? scope = null,
        DitOptions? ditOptions = null, ILogger? logger = null)
    {
        var classes = new List<TypeElement>();
        var methods = new List<MethodElement>();
        var relationships = new ClassRelationshipBuilder();
        int methodsWithoutModel = 0, ambiguousAccesses = 0;

        foreach (var document in codebase.Documents)
        {
            try
            {
                var collector = new TypeModelCollector(document.SemanticModel, cyclomaticOptions, scope, ditOptions);
                collector.Visit(document.Tree.GetRoot());

                classes.AddRange(collector.GetClasses());
                methods.AddRange(collector.GetMethods());
                methodsWithoutModel += collector.MethodsWithoutSemanticModel;
                ambiguousAccesses += collector.AmbiguousAccesses;

                foreach (var classElement in collector.GetClasses())
                {
                    relationships.AddClass(classElement, document.FilePath);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error collecting the types of: {FilePath}", document.FilePath);
            }
        }

        return new TypeModel(classes, methods, relationships, methodsWithoutModel, ambiguousAccesses);
    }
}
