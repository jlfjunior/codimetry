using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Services;

namespace MetricsExporter.Net.Services;

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
    /// <param name="excludeStatic">Leave static methods and constructors out, as v1 did (--wmc-exclude-static).</param>
    public WmcCalculator(WmcWeight weight = WmcWeight.Unit, bool excludeStatic = false)
    {
        _weight = weight;
        _excludeStatic = excludeStatic;
    }

    /// <summary>
    /// Calculates WMC = Σ c_i over the methods and constructors declared in the class, static ones
    /// included unless excluded.
    /// </summary>
    public int CalculateWmc(string classKey, IReadOnlyList<CkMethodInfo> allMethods)
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
    /// Calculates DIT as the number of declared inheritance edges above the class (D7): the base classes
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
    /// Calculates CBO as the number of classes to which a class is coupled.
    /// </summary>
    public int CalculateCbo(ClassDependency dependency, IReadOnlyDictionary<string, ClassRelationshipBuilder.ClassInfo> allClasses)
    {
        // Keys of the coupled classes in the relationship graph
        var coupledClasses = new HashSet<string>();

        // Get all types referenced by this class
        foreach (var type in dependency.DependentTypes)
        {
            var normalizedType = NormalizeTypeName(type);
            
            // Check if this type is a class in our codebase
            if (allClasses.ContainsKey(normalizedType))
            {
                coupledClasses.Add(normalizedType);
            }
            else
            {
                // Check if it's a fully qualified name
                var matchingClass = allClasses.FirstOrDefault(c => 
                    c.Value.FullyQualifiedName?.EndsWith(normalizedType) == true ||
                    c.Value.Name == normalizedType);
                
                if (matchingClass.Value != null)
                {
                    coupledClasses.Add(matchingClass.Key);
                }
            }
        }

        // Exclude self
        coupledClasses.Remove(dependency.ClassKey);

        return coupledClasses.Count;
    }

    private string NormalizeTypeName(string typeName)
    {
        // Remove generic parameters
        var index = typeName.IndexOf('<');
        if (index > 0)
        {
            typeName = typeName.Substring(0, index);
        }

        // Remove array brackets
        typeName = typeName.Replace("[]", "").Trim();

        // Remove nullable marker
        typeName = typeName.TrimEnd('?');

        // Extract class name from fully qualified name
        var lastDot = typeName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            typeName = typeName.Substring(lastDot + 1);
        }

        return typeName.Trim();
    }
}

/// <summary>
/// Calculator for RFC (Response For a Class) metric.
/// </summary>
public class RfcCalculator
{
    /// <summary>
    /// Calculates RFC as the number of methods that can be invoked in response to a message:
    /// |M ∪ R|, where M is the methods of the class and R the methods they call.
    /// Resolved calls are identified by the called method; a call to a method of M is already in the
    /// set, as are calls to local functions. Unresolved calls are identified by their simple name.
    /// </summary>
    public int CalculateRfc(string classKey, IReadOnlyList<CkMethodInfo> allMethods)
    {
        var classMethods = allMethods.Where(m => m.ContainingClassKey == classKey).ToList();
        var m = classMethods.Count;

        // Collect all methods called by class methods
        var calledMethods = new HashSet<string>();
        foreach (var call in classMethods.SelectMany(method => method.Calls))
        {
            if (!call.IsResolved)
            {
                calledMethods.Add("name:" + call.Name);
            }
            else if (call.TargetClassKey != classKey)
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
    public static IEnumerable<CkMethodInfo> Methods(string classKey, IReadOnlyList<CkMethodInfo> allMethods) =>
        allMethods.Where(m => m.ContainingClassKey == classKey && !m.IsStatic && !m.IsConstructor);

    /// <summary>
    /// Calculates LCOM using the original Chidamber-Kemerer formula.
    /// LCOM = P - Q, where:
    /// P = number of method pairs that don't share instance variables
    /// Q = number of method pairs that share instance variables
    /// </summary>
    public int CalculateLcom(string classKey, IReadOnlyList<CkMethodInfo> allMethods, IReadOnlyList<ClassDependency> dependencies)
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

