using Codimetry.Core.Model;


namespace Codimetry.Metrics.Ck;

/// <summary>
/// Weight c_i of each method in WMC.
/// </summary>
public enum WmcWeight
{
    /// <summary>c_i = 1: WMC is the number of methods.</summary>
    Unit,
    /// <summary>c_i = V(G) of the method.</summary>
    Cyclomatic
}

/// <summary>
/// Calculator for WMC (Weighted Methods per Class) metric.
/// </summary>
public class WmcCalculator
{
    private readonly WmcWeight _weight;
    private readonly bool _excludeStatic;

    /// <param name="weight">Weight of each method (--wmc-weight).</param>
    /// <param name="excludeStatic">Leave static methods and constructors out, as earlier versions did (--wmc-exclude-static).</param>
    public WmcCalculator(WmcWeight weight = WmcWeight.Unit, bool excludeStatic = false)
    {
        _weight = weight;
        _excludeStatic = excludeStatic;
    }

    /// <summary>
    /// Calculates WMC = Σ c_i over the methods and constructors declared in the class, static ones
    /// included unless excluded.
    /// </summary>
    public int CalculateWmc(string classKey, IReadOnlyList<MethodElement> allMethods)
    {
        return allMethods
            .Where(m => m.ContainingClassKey == classKey && !(_excludeStatic && m.IsStatic))
            .Sum(m => _weight == WmcWeight.Cyclomatic ? m.CyclomaticComplexity : 1);
    }
}

/// <summary>
/// Calculator for DIT (Depth of Inheritance Tree) metric.
/// </summary>
public class DitCalculator
{
    private readonly ClassRelationshipBuilder _relationshipBuilder;
    private readonly bool _resolveFramework;

    /// <param name="resolveFramework">Count the ancestors of an external base class too (--dit-resolve-framework).</param>
    public DitCalculator(ClassRelationshipBuilder relationshipBuilder, bool resolveFramework = false)
    {
        _relationshipBuilder = relationshipBuilder;
        _resolveFramework = resolveFramework;
    }

    /// <summary>
    /// Calculates DIT as the number of declared inheritance edges above the class: the base classes
    /// in the analysed code, plus one for a base class outside it (Exception, ControllerBase). System.Object
    /// is not a base class, so a class without an explicit base has DIT = 0.
    /// </summary>
    public int CalculateDit(string className)
    {
        // The chain runs from the class through its bases in the code, ending with an external base if any
        var chain = _relationshipBuilder.GetInheritanceChain(className);
        var dit = chain.Count - 1;

        var classes = _relationshipBuilder.GetAllClasses();
        if (_resolveFramework && dit > 0 && !classes.ContainsKey(chain[^1]) &&
            classes.TryGetValue(chain[^2], out var lastInCode))
        {
            dit += lastInCode.BaseAncestors;
        }

        return dit;
    }
}

/// <summary>
/// Calculator for NOC (Number of Children) metric.
/// </summary>
public class NocCalculator
{
    private readonly ClassRelationshipBuilder _relationshipBuilder;

    public NocCalculator(ClassRelationshipBuilder relationshipBuilder)
    {
        _relationshipBuilder = relationshipBuilder;
    }

    /// <summary>
    /// Calculates NOC as the count of immediate subclasses.
    /// </summary>
    public int CalculateNoc(string className)
    {
        var children = _relationshipBuilder.GetChildren(className);
        return children.Count;
    }
}

/// <summary>
/// Calculator for CBO (Coupling Between Objects) metric.
/// </summary>
public class CboCalculator
{
    /// <summary>
    /// The types a class can be coupled to: the measured types and, unless the legacy rule is reproduced, the interfaces
    /// declared in the analysed code. Framework and package types are never targets.
    /// </summary>
    public static Dictionary<string, CouplingTarget> Targets(
        IReadOnlyDictionary<string, ClassRelationshipBuilder.ClassInfo> allClasses, IEnumerable<CouplingTarget> interfaces)
    {
        var targets = allClasses.ToDictionary(c => c.Key, c => new CouplingTarget(c.Key, c.Value.Name, c.Value.Namespace));
        foreach (var target in interfaces)
        {
            targets.TryAdd(target.Key, target);
        }

        return targets;
    }

    /// <summary>
    /// Calculates CBO as the number of other types of the analysed code the class uses (outgoing coupling).
    /// </summary>
    public int CalculateCbo(ClassDependency dependency, IReadOnlyDictionary<string, CouplingTarget> targets) =>
        Coupled(dependency, targets).Count;

    /// <summary>
    /// The keys of the other types of the analysed code the class references: resolved types are matched
    /// by key, so <c>Task</c> does not couple to <c>BackgroundTask</c> and homonymous types stay apart. Unresolved
    /// type names are matched by exact simple name; among homonyms, the one in the class's namespace is preferred,
    /// then the first by key.
    /// </summary>
    public HashSet<string> Coupled(ClassDependency dependency, IReadOnlyDictionary<string, CouplingTarget> targets)
    {
        var coupled = dependency.DependentTypes.Where(targets.ContainsKey).ToHashSet();

        foreach (var name in dependency.UnresolvedTypeNames)
        {
            var match = targets.Values
                .Where(t => t.Name == name)
                .OrderBy(t => t.Namespace == dependency.Namespace ? 0 : 1)
                .ThenBy(t => t.Key, StringComparer.Ordinal)
                .Select(t => t.Key)
                .FirstOrDefault();
            if (match != null)
            {
                coupled.Add(match);
            }
        }

        coupled.Remove(dependency.ClassKey);

        return coupled;
    }

    /// <summary>
    /// CBO in both directions (CK 1994, --cbo-bidirectional), from the outgoing couplings of every measured type:
    /// the types each class uses plus the types that use it, each counted once.
    /// </summary>
    public static Dictionary<string, int> Bidirectional(IReadOnlyDictionary<string, HashSet<string>> outgoing)
    {
        var coupled = outgoing.ToDictionary(o => o.Key, o => new HashSet<string>(o.Value));
        foreach (var (user, used) in outgoing)
        {
            foreach (var key in used)
            {
                if (coupled.TryGetValue(key, out var set))
                {
                    set.Add(user);
                }
            }
        }

        return coupled.ToDictionary(c => c.Key, c => c.Value.Count);
    }

    /// <summary>
    /// Number of type references matched by name, for the log.
    /// </summary>
    public static int UnresolvedReferences(IEnumerable<ClassDependency> dependencies) =>
        dependencies.Sum(d => d.UnresolvedTypeNames.Count);
}

/// <summary>
/// Calculator for RFC (Response For a Class) metric.
/// </summary>
public class RfcCalculator
{
    private readonly bool _excludeConstructors;

    /// <param name="excludeConstructors">Leave constructor calls out of R, as earlier versions did (--rfc-exclude-constructors).</param>
    public RfcCalculator(bool excludeConstructors = false)
    {
        _excludeConstructors = excludeConstructors;
    }

    /// <summary>
    /// Calculates RFC as the number of methods that can be invoked in response to a message:
    /// |M ∪ R|, where M is the methods of the class and R the methods and constructors they call.
    /// <list type="bullet">
    /// <item>a resolved call enters R by its target unless the target is in M; calls to the class's other members
    /// (local functions, members the compiler generates) stay out, but its implicit constructor
    /// (<c>new Self()</c> without a declared one) enters;</item>
    /// <item>constructors enter as methods: <c>new X()</c>, <c>new()</c> and <c>: base(...)</c>; <c>: this(...)</c>
    /// calls a constructor already in M;</item>
    /// <item>an unresolved call enters as <c>name:X</c>, unless a method of the class is called X; an unresolved
    /// creation enters as <c>ctor:X</c>, unless X is the class itself;</item>
    /// <item>property accessors are attribute accesses and do not enter.</item>
    /// </list>
    /// </summary>
    public int CalculateRfc(string classKey, IReadOnlyList<MethodElement> allMethods)
    {
        var classMethods = allMethods.Where(m => m.ContainingClassKey == classKey).ToList();
        var m = classMethods.Count;
        var methodKeys = classMethods.Select(method => $"{classKey}::{method.Key}").ToHashSet();
        var methodNames = classMethods.Where(method => !method.IsConstructor).Select(method => method.Name).ToHashSet();
        var className = classMethods.FirstOrDefault()?.ContainingClass;

        var calledMethods = new HashSet<string>();
        foreach (var call in classMethods.SelectMany(method => method.Calls))
        {
            if (call.Kind == CallKind.Constructor && _excludeConstructors)
            {
                continue;
            }

            if (!call.IsResolved)
            {
                if (call.Kind == CallKind.Constructor && call.Name != className)
                {
                    calledMethods.Add("ctor:" + call.Name);
                }
                else if (call.Kind == CallKind.Method && !methodNames.Contains(call.Name))
                {
                    calledMethods.Add("name:" + call.Name);
                }
            }
            else if (call.TargetKey != null && methodKeys.Contains(call.TargetKey))
            {
                // already in M
            }
            else if (call.TargetClassKey != classKey || call.Kind == CallKind.Constructor)
            {
                calledMethods.Add(call.TargetId!);
            }
        }

        var r = calledMethods.Count;

        return m + r;
    }
}

/// <summary>
/// Calculator for LCOM (Lack of Cohesion of Methods) metric.
/// </summary>
public class LcomCalculator
{
    /// <summary>
    /// The methods LCOM is computed over: the instance methods of the class, without constructors.
    /// </summary>
    public static IEnumerable<MethodElement> Methods(string classKey, IReadOnlyList<MethodElement> allMethods) =>
        allMethods.Where(m => m.ContainingClassKey == classKey && !m.IsStatic && !m.IsConstructor);

    /// <summary>
    /// Calculates LCOM using the original Chidamber-Kemerer formula.
    /// LCOM = P - Q, where:
    /// P = number of method pairs that don't share instance variables
    /// Q = number of method pairs that share instance variables
    /// </summary>
    public int CalculateLcom(string classKey, IReadOnlyList<MethodElement> allMethods, IReadOnlyList<ClassDependency> dependencies)
    {
        var classMethods = Methods(classKey, allMethods).ToList();

        if (classMethods.Count <= 1)
        {
            return 0; // LCOM is 0 for 0 or 1 method
        }

        // Get the names of all instance fields/properties for this class
        var classDependency = dependencies.FirstOrDefault(d => d.ClassKey == classKey);
        var instanceFields = classDependency?.InstanceMemberNames ?? new HashSet<string>();

        // For each method, get the fields it accesses
        var methodFieldSets = new List<HashSet<string>>();
        foreach (var method in classMethods)
        {
            var fields = new HashSet<string>();
            foreach (var accessedField in method.AccessedFields)
            {
                // Check if this is an instance field
                if (instanceFields.Contains(accessedField))
                {
                    fields.Add(accessedField);
                }
            }
            methodFieldSets.Add(fields);
        }

        // Calculate P and Q
        int p = 0; // Pairs that don't share fields
        int q = 0; // Pairs that share fields

        for (int i = 0; i < methodFieldSets.Count; i++)
        {
            for (int j = i + 1; j < methodFieldSets.Count; j++)
            {
                var set1 = methodFieldSets[i];
                var set2 = methodFieldSets[j];

                // Check if they share any fields
                bool shareFields = set1.Intersect(set2).Any();

                if (shareFields)
                {
                    q++;
                }
                else
                {
                    p++;
                }
            }
        }

        // LCOM = P - Q (if P > Q, else 0)
        return Math.Max(0, p - q);
    }
}

