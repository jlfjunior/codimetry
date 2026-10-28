using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Codimetry.Core.Cyclomatic;
using Codimetry.Core.Model;
using Codimetry.Core.Suites;

namespace Codimetry.Metrics.Cyclomatic;

/// <summary>
/// Service that orchestrates the calculation of Cyclomatic Complexity metrics for all methods, classes, and files.
/// </summary>
/// <remarks>
/// The members measured are those with control flow written by the developer: methods,
/// constructors, destructors, operators, conversions, and the accessors with a body (block or
/// expression) of properties, indexers and events. Accessors without a body, and members without a body
/// (abstract, interface, extern, partial definitions), have no flow graph and are left out. Decisions in
/// lambdas and local functions count for the member that contains them.
/// </remarks>
public class CyclomaticComplexityAnalysisService
{
    private readonly ILogger<CyclomaticComplexityAnalysisService>? _logger;

    public CyclomaticComplexityAnalysisService(ILogger<CyclomaticComplexityAnalysisService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// A measured member: its name in the CSV and the body whose decisions are counted (null for an
    /// automatic accessor, which has V(G) = 1).
    /// </summary>
    private sealed record Member(SyntaxNode Node, string Name, SyntaxNode? Body);

    /// <summary>
    /// Analyzes code and calculates Cyclomatic Complexity metrics for all methods, classes, and files.
    /// </summary>
    public Task<List<CyclomaticComplexityMetric>> AnalyzeAsync(AnalysisContext context)
    {
        var options = context.CyclomaticOptions;
        var scope = context.TypeScope;

        _logger?.LogInformation("Cyclomatic aggregation: {Rule}", options.IncludeAutoAccessors
            ? "automatic accessors included with V(G) = 1, legacy (--cc-include-auto-accessors)"
            : "methods, constructors, destructors, operators and accessors with a body");

        // The semantic model gives the class key (project + fully qualified name)
        var codebase = context.Codebase;

        var memberRows = new List<CyclomaticComplexityMetric>();
        var classes = new Dictionary<string, (string FilePath, string Name)>();
        var documentFiles = new List<string>();

        foreach (var document in codebase.Documents)
        {
            try
            {
                documentFiles.Add(document.FilePath);
                var root = document.Tree.GetRoot();

                // A class row is placed in the file of the first declaration, as in metrics_ck.csv
                foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(scope.IsMeasured))
                {
                    classes.TryAdd(TypeKeys.Of(type, document.SemanticModel), (document.FilePath, type.Identifier.Text));
                }

                foreach (var member in Members(root, document.SemanticModel, options, scope))
                {
                    var type = scope.ContainingType(member.Node);
                    memberRows.Add(new CyclomaticComplexityMetric
                    {
                        FilePath = document.FilePath,
                        ClassName = type?.Identifier.Text ?? string.Empty,
                        MethodName = member.Name,
                        Complexity = Complexity(member.Body, options),
                        ClassKey = type != null ? TypeKeys.Of(type, document.SemanticModel) : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error analyzing file for cyclomatic complexity: {FilePath}", document.FilePath);
            }
        }

        // Class rows: one per class key, joining the members of every partial declaration
        var classRows = classes.Select(c => new CyclomaticComplexityMetric
        {
            FilePath = c.Value.FilePath,
            ClassName = c.Value.Name,
            MethodName = string.Empty,
            Complexity = memberRows.Where(m => m.ClassKey == c.Key).Sum(m => m.Complexity),
            ClassKey = c.Key
        });

        // File rows: the members of measured types declared in the file
        var fileRows = documentFiles.Select(file => new CyclomaticComplexityMetric
        {
            FilePath = file,
            ClassName = string.Empty,
            MethodName = string.Empty,
            Complexity = memberRows.Where(m => m.FilePath == file && m.ClassKey != string.Empty).Sum(m => m.Complexity)
        });

        var allMetrics = memberRows.Concat(classRows).Concat(fileRows).ToList();
        _logger?.LogInformation("Calculated cyclomatic complexity for {Count} metric entries", allMetrics.Count);

        return Task.FromResult(allMetrics);
    }

    private static int Complexity(SyntaxNode? body, CyclomaticOptions options)
    {
        var visitor = new CyclomaticComplexityVisitor(options);
        if (body != null)
        {
            visitor.Visit(body);
        }

        return visitor.GetComplexity();
    }

    /// <summary>
    /// The members measured, in source order. Abstract and interface members, and partial method
    /// definitions, have no body and are skipped.
    /// </summary>
    private static IEnumerable<Member> Members(SyntaxNode root, SemanticModel semanticModel, CyclomaticOptions options, TypeScope scope)
    {
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case BaseMethodDeclarationSyntax method when ((SyntaxNode?)method.Body ?? method.ExpressionBody) is { } methodBody:
                    yield return new Member(method, MethodName(method, semanticModel), methodBody);
                    break;

                // int X => ...: a getter with an expression body
                case PropertyDeclarationSyntax { ExpressionBody: { } expression } expressionProperty:
                    yield return new Member(expressionProperty, $"{expressionProperty.Identifier.Text}.get", expression);
                    break;
                case IndexerDeclarationSyntax { ExpressionBody: { } expression } indexer:
                    yield return new Member(indexer, "this[].get", expression);
                    break;

                case AccessorDeclarationSyntax accessor when accessor.Parent?.Parent is BasePropertyDeclarationSyntax owner:
                    var body = (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody;
                    var isAutoProperty = body == null && owner is PropertyDeclarationSyntax property &&
                        !property.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword) || m.IsKind(SyntaxKind.ExternKeyword)) &&
                        scope.ContainingType(property) != null;
                    if (body != null || (isAutoProperty && options.IncludeAutoAccessors))
                    {
                        yield return new Member(accessor, $"{OwnerName(owner)}.{accessor.Keyword.Text}", body);
                    }
                    break;
            }
        }
    }

    private static string OwnerName(BasePropertyDeclarationSyntax owner) => owner switch
    {
        PropertyDeclarationSyntax property => property.Identifier.Text,
        EventDeclarationSyntax @event => @event.Identifier.Text,
        IndexerDeclarationSyntax => "this[]",
        _ => owner.Kind().ToString()
    };

    /// <summary>
    /// Name of a method-like member in the CSV: the identifier of methods and constructors, ~Name for a
    /// destructor, and the metadata name of operators and conversions (op_Addition, op_Implicit...).
    /// </summary>
    private static string MethodName(BaseMethodDeclarationSyntax method, SemanticModel semanticModel) => method switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        ConstructorDeclarationSyntax c => c.Identifier.Text,
        DestructorDeclarationSyntax d => $"~{d.Identifier.Text}",
        _ => semanticModel.GetDeclaredSymbol(method)?.Name ?? method.Kind().ToString()
    };
}
