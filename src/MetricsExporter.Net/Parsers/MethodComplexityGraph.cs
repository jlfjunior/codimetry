using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// MC of every method of the analysed code with the weight of its calls (INC-16, Cap. 2):
/// <list type="bullet">
/// <item>a call to a method declared in the analysed code adds 2 + MC of the called method, own class
/// included;</item>
/// <item>a call inside a recursion, direct or mutual (a cycle of the call graph), adds 2 only;</item>
/// <item>a call to an abstract or interface method adds 2 + MC of its implementation when the analysed
/// code has exactly one, and 2 + 1 otherwise (D14);</item>
/// <item>a call to a library method adds the library call weight, 0 by default (D10);</item>
/// <item>an unresolved call adds 2 + 1;</item>
/// <item>invoking a delegate, a lambda or a local function, and <c>nameof</c>, add nothing (the body of a
/// lambda or local function written in the method is already counted, INC-17); object creation is not a
/// call (D20).</item>
/// </list>
/// Each strongly connected component of the call graph is a recursion: calls between its methods weigh 2
/// whichever method is computed first, and every other call goes to a component computed before.
/// </summary>
public sealed class MethodComplexityGraph
{
    public const int CallWeight = 2;

    /// <summary>
    /// MC assumed for a called method whose body is unknown: unresolved calls, and abstract methods
    /// without exactly one implementation.
    /// </summary>
    public const int UnknownMc = 1;

    private enum CallKind { Ignored, Library, Unresolved, Abstract, System }

    private sealed record Call(CallKind Kind, string? Target = null);

    private readonly Dictionary<string, (BaseMethodDeclarationSyntax Node, SemanticModel Model)> _methods = new();
    private readonly Dictionary<SyntaxNode, string> _keyOf = new();
    private readonly Dictionary<string, int> _component = new();
    private readonly Dictionary<string, int> _mc = new();
    private readonly Dictionary<InvocationExpressionSyntax, Call> _calls = new();
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _implementations;
    private readonly int _libraryCallWeight;
    private readonly McLambdaRule _lambdas;

    /// <param name="implementations">Abstract or interface method key → keys of its implementations
    /// (<see cref="Codebase.AbstractImplementations"/>).</param>
    /// <param name="libraryCallWeight">Weight of a call to a referenced library (--mc-library-call-weight).</param>
    /// <param name="lambdas">How lambdas and local functions are treated (--mc-lambdas).</param>
    public MethodComplexityGraph(
        IEnumerable<SourceDocument> documents,
        IReadOnlyDictionary<string, IReadOnlyList<string>> implementations,
        int libraryCallWeight = 0,
        McLambdaRule lambdas = McLambdaRule.Full)
    {
        _lambdas = lambdas;
        _implementations = implementations;
        _libraryCallWeight = libraryCallWeight;

        foreach (var document in documents)
        {
            foreach (var method in document.Tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>()
                         .Where(m => m is MethodDeclarationSyntax or ConstructorDeclarationSyntax))
            {
                if ((method.Body != null || method.ExpressionBody != null) &&
                    document.SemanticModel.GetDeclaredSymbol(method) is IMethodSymbol symbol &&
                    TypeKeys.SourceMethodKey(symbol) is { } key)
                {
                    _methods.TryAdd(key, (method, document.SemanticModel));
                    _keyOf[method] = key;
                }
            }
        }

        // Edges are the calls the MC rule actually looks at, so they follow its scope (lambdas, local functions)
        var edges = _methods.ToDictionary(m => m.Key, m =>
        {
            var targets = new List<string>();
            MethodComplexityCalculator.Calculate(m.Value.Node, invocation =>
            {
                if (Resolve(invocation, m.Value.Model) is { Kind: CallKind.System, Target: { } target })
                {
                    targets.Add(target);
                }

                return 0;
            }, _lambdas);
            return targets;
        });

        FindComponents(edges);
    }

    public int SystemCalls { get; private set; }
    public int RecursiveCalls { get; private set; }
    public int LibraryCalls { get; private set; }
    public int AbstractCalls { get; private set; }
    public int UnresolvedCalls { get; private set; }

    /// <summary>
    /// MC of a method or constructor of the analysed code, or null when it has no body.
    /// </summary>
    public int? Mc(BaseMethodDeclarationSyntax method) =>
        _keyOf.TryGetValue(method, out var key) ? Mc(key) : MethodComplexityCalculator.Calculate(method, lambdas: _lambdas);

    private int Mc(string key)
    {
        if (_mc.TryGetValue(key, out var mc))
        {
            return mc;
        }

        var (node, model) = _methods[key];
        mc = MethodComplexityCalculator.Calculate(node, invocation => Weight(invocation, model, key), _lambdas) ?? 0;
        _mc[key] = mc;
        return mc;
    }

    private int Weight(InvocationExpressionSyntax invocation, SemanticModel model, string caller)
    {
        var call = Resolve(invocation, model);
        switch (call.Kind)
        {
            case CallKind.System when _component[call.Target!] == _component[caller]:
                RecursiveCalls++;
                return CallWeight;
            case CallKind.System:
                SystemCalls++;
                return CallWeight + Mc(call.Target!);
            case CallKind.Abstract:
                AbstractCalls++;
                return CallWeight + UnknownMc;
            case CallKind.Unresolved:
                UnresolvedCalls++;
                return CallWeight + UnknownMc;
            case CallKind.Library:
                LibraryCalls++;
                return _libraryCallWeight;
            default:
                return 0;
        }
    }

    private Call Resolve(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (_calls.TryGetValue(invocation, out var cached))
        {
            return cached;
        }

        return _calls[invocation] = ResolveUncached(invocation, model);
    }

    private Call ResolveUncached(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            // nameof(x) binds to no method; anything else is a call the model could not resolve
            return invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" }
                ? new Call(CallKind.Ignored)
                : new Call(CallKind.Unresolved);
        }

        if (method.MethodKind is MethodKind.DelegateInvoke or MethodKind.LocalFunction or MethodKind.AnonymousFunction)
        {
            return new Call(CallKind.Ignored);
        }

        var definition = (method.ReducedFrom ?? method).OriginalDefinition;
        if (!definition.Locations.Any(l => l.IsInSource))
        {
            return new Call(CallKind.Library);
        }

        var key = TypeKeys.SourceMethodKey(definition);
        if (definition.IsAbstract)
        {
            // Interface or abstract member: the MC of its only implementation in the analysed code (D14)
            return key != null && _implementations.TryGetValue(key, out var implementations) &&
                   implementations.Count == 1 && _methods.ContainsKey(implementations[0])
                ? new Call(CallKind.System, implementations[0])
                : new Call(CallKind.Abstract);
        }

        // A source method without a body of its own (extern, partial without implementation)
        return key != null && _methods.ContainsKey(key) ? new Call(CallKind.System, key) : new Call(CallKind.Abstract);
    }

    /// <summary>
    /// Strongly connected components (Tarjan), iterative so that deep call chains do not overflow the stack.
    /// </summary>
    private void FindComponents(Dictionary<string, List<string>> edges)
    {
        var index = new Dictionary<string, int>();
        var lowLink = new Dictionary<string, int>();
        var onStack = new HashSet<string>();
        var stack = new Stack<string>();
        var next = 0;
        var components = 0;

        foreach (var root in edges.Keys)
        {
            if (index.ContainsKey(root))
            {
                continue;
            }

            var work = new Stack<(string Node, int Edge)>();
            work.Push((root, 0));
            index[root] = lowLink[root] = next++;
            stack.Push(root);
            onStack.Add(root);

            while (work.Count > 0)
            {
                var (node, edge) = work.Pop();
                var targets = edges[node];
                if (edge < targets.Count)
                {
                    work.Push((node, edge + 1));
                    var target = targets[edge];
                    if (!index.ContainsKey(target))
                    {
                        index[target] = lowLink[target] = next++;
                        stack.Push(target);
                        onStack.Add(target);
                        work.Push((target, 0));
                    }
                    else if (onStack.Contains(target))
                    {
                        lowLink[node] = Math.Min(lowLink[node], index[target]);
                    }

                    continue;
                }

                if (lowLink[node] == index[node])
                {
                    string member;
                    do
                    {
                        member = stack.Pop();
                        onStack.Remove(member);
                        _component[member] = components;
                    } while (member != node);

                    components++;
                }

                if (work.Count > 0)
                {
                    var parent = work.Peek().Node;
                    lowLink[parent] = Math.Min(lowLink[parent], lowLink[node]);
                }
            }
        }
    }
}
