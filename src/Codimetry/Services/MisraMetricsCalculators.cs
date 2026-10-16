using Codimetry.Parsers;

namespace Codimetry.Services;

/// <summary>
/// Calculator for MC (Method Complexity) metric.
/// </summary>
public class McCalculator
{
    /// <summary>
    /// Calculates MC (Method Complexity) for a method using BCS information.
    /// </summary>
    public int CalculateMc(string classKey, string methodKey, IReadOnlyDictionary<string, BcsInfo> methodBcsInfo)
    {
        var key = $"{classKey}::{methodKey}";
        if (methodBcsInfo.TryGetValue(key, out var bcsInfo))
        {
            return bcsInfo.CalculateMc();
        }

        return 0; // Method not found or has no body
    }

    /// <summary>
    /// Gets all method complexities for a class.
    /// </summary>
    public Dictionary<string, int> GetMethodComplexities(string classKey, IReadOnlyDictionary<string, BcsInfo> methodBcsInfo)
    {
        var result = new Dictionary<string, int>();
        var prefix = $"{classKey}::";

        foreach (var kvp in methodBcsInfo)
        {
            if (kvp.Key.StartsWith(prefix))
            {
                var methodName = kvp.Key.Substring(prefix.Length);
                result[methodName] = kvp.Value.CalculateMc();
            }
        }

        return result;
    }
}

/// <summary>
/// Calculator for AC (Attribute Complexity) metric.
/// </summary>
public class AcCalculator
{
    private readonly bool _typeWeights;

    /// <param name="typeWeights">Weight each attribute by its type as v1 did (simple = 1, other = 2),
    /// instead of counting it as one unit (--ac-type-weights).</param>
    public AcCalculator(bool typeWeights = false)
    {
        _typeWeights = typeWeights;
    }

    /// <summary>
    /// Calculates AC (Attribute Complexity) for a class: the number of its attributes, each one a
    /// cognitive unit whatever its type (Eq. 25). With type weights, simple types count 1 and others 2.
    /// </summary>
    public int CalculateAc(string classKey, IReadOnlyDictionary<string, List<string>> classAttributes, 
        IReadOnlyDictionary<string, List<string>> classAttributeTypes)
    {
        if (!classAttributes.TryGetValue(classKey, out var attributes))
        {
            return 0; // No attributes
        }

        if (!_typeWeights || !classAttributeTypes.TryGetValue(classKey, out var attributeTypes))
        {
            return attributes.Count; // Fallback to simple count
        }

        // Weighted approach: simple types = 1, complex types = 2
        int complexity = 0;
        for (int i = 0; i < attributes.Count && i < attributeTypes.Count; i++)
        {
            var type = attributeTypes[i];
            if (IsSimpleType(type))
            {
                complexity += 1;
            }
            else
            {
                complexity += 2; // Complex type (custom class, collection, etc.)
            }
        }

        return complexity;
    }

    private static bool IsSimpleType(string typeName)
    {
        // Simple types: primitives, strings, value types
        var simpleTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "int", "Int32", "System.Int32",
            "long", "Int64", "System.Int64",
            "short", "Int16", "System.Int16",
            "byte", "Byte", "System.Byte",
            "bool", "Boolean", "System.Boolean",
            "char", "Char", "System.Char",
            "float", "Single", "System.Single",
            "double", "Double", "System.Double",
            "decimal", "Decimal", "System.Decimal",
            "string", "String", "System.String",
            "object", "Object", "System.Object",
            "DateTime", "System.DateTime",
            "TimeSpan", "System.TimeSpan",
            "Guid", "System.Guid"
        };

        // Remove generic parameters and array brackets for comparison
        var normalized = typeName.Split('<')[0].Split('[')[0].TrimEnd('?');
        return simpleTypes.Contains(normalized);
    }
}

/// <summary>
/// Calculator for CLC (Class Complexity) metric.
/// </summary>
public class ClcCalculator
{
    private readonly McCalculator _mcCalculator;
    private readonly AcCalculator _acCalculator;

    public ClcCalculator(bool acTypeWeights = false)
    {
        _mcCalculator = new McCalculator();
        _acCalculator = new AcCalculator(acTypeWeights);
    }

    /// <summary>
    /// Calculates CLC (Class Complexity) for a class.
    /// CLC = AC + Σ(MC for all methods in the class)
    /// </summary>
    public int CalculateClc(string classKey, 
        IReadOnlyDictionary<string, BcsInfo> methodBcsInfo,
        IReadOnlyDictionary<string, List<string>> classAttributes,
        IReadOnlyDictionary<string, List<string>> classAttributeTypes)
    {
        // Calculate AC
        var ac = _acCalculator.CalculateAc(classKey, classAttributes, classAttributeTypes);

        // Calculate sum of all MC values for methods in the class
        var methodComplexities = _mcCalculator.GetMethodComplexities(classKey, methodBcsInfo);
        var sumMc = methodComplexities.Values.Sum();

        return ac + sumMc;
    }
}

/// <summary>
/// Calculator for CWC (Coupling Weight for a Class) metric, Eq. 24: CWC = Σ (2 + MC of the called method)
/// over the calls to methods of **other types of the analysed code** (D13).
/// </summary>
public class CwcCalculator
{
    public const int CallWeight = 2;

    /// <summary>
    /// MC assumed for a called abstract or interface method without exactly one implementation (D14).
    /// </summary>
    public const int UnknownMc = 1;

    private readonly int _libraryCallWeight;

    /// <param name="libraryCallWeight">Weight of a call to a library method (--cwc-library-weight); 0 leaves
    /// them out, 3 reproduces the library rule of v1.</param>
    public CwcCalculator(int libraryCallWeight = 0)
    {
        _libraryCallWeight = libraryCallWeight;
    }

    /// <summary>
    /// Calculates CWC. Each call counts, repeated ones included (RF-6):
    /// <list type="bullet">
    /// <item>a call to a method of another measured type of the analysed code, inherited methods of a base
    /// class included, adds 2 + MC of the called method;</item>
    /// <item>a call to an abstract or interface method declared in the analysed code adds 2 + MC of its only
    /// implementation, or 2 + 1 when there are none or several;</item>
    /// <item>calls to the class's own methods (any partial declaration) and local functions add nothing;</item>
    /// <item>calls to libraries and delegates add the library call weight (0 by default);</item>
    /// <item>unresolved calls add nothing (see <see cref="UnresolvedCalls"/>).</item>
    /// </list>
    /// </summary>
    /// <param name="implementations">Abstract or interface method key → keys of its implementations.</param>
    public int CalculateCwc(string classKey, 
        IReadOnlyList<CkMethodInfo> allMethods,
        IReadOnlyDictionary<string, BcsInfo> methodBcsInfo,
        IReadOnlyDictionary<string, ClassRelationshipBuilder.ClassInfo> allClasses,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? implementations = null)
    {
        var totalCwc = 0;

        // Constructors are not calls of eq. 24 (RF-6 of INC-06): new X() couples through CBO, not CWC
        foreach (var call in allMethods.Where(m => m.ContainingClassKey == classKey).SelectMany(m => m.Calls)
                     .Where(c => c.Kind != CallKind.Constructor))
        {
            if (!call.IsResolved || call.TargetClassKey == classKey)
            {
                continue; // unresolved, or own method / local function: not coupling
            }

            if (call.TargetKey == null)
            {
                totalCwc += _libraryCallWeight; // library method or delegate
                continue;
            }

            if (call.IsAbstract)
            {
                // Interface or abstract member of the analysed code: its only implementation (D14)
                var implementation = implementations != null && implementations.TryGetValue(call.TargetKey, out var impls) && impls.Count == 1
                    ? impls[0]
                    : null;
                totalCwc += CallWeight + (implementation != null && methodBcsInfo.TryGetValue(implementation, out var implBcs)
                    ? implBcs.CalculateMc()
                    : UnknownMc);
                continue;
            }

            // A method of another type: it counts when that type is measured (D19)
            if (allClasses.ContainsKey(call.TargetClassKey!))
            {
                totalCwc += CallWeight + (methodBcsInfo.TryGetValue(call.TargetKey, out var bcs) ? bcs.CalculateMc() : UnknownMc);
            }
        }

        return totalCwc;
    }

    /// <summary>
    /// Calls of the class that the semantic model could not resolve; they do not enter CWC.
    /// </summary>
    public static int UnresolvedCalls(string classKey, IReadOnlyList<CkMethodInfo> allMethods) =>
        allMethods.Where(m => m.ContainingClassKey == classKey).SelectMany(m => m.Calls)
            .Count(c => !c.IsResolved && c.Kind != CallKind.Constructor);
}

/// <summary>
/// Calculator for CC (Code Complexity) metric.
/// </summary>
public class CcCalculator
{
    private readonly ClcCalculator _clcCalculator;
    private readonly CwcCalculator _cwcCalculator;

    public CcCalculator(bool acTypeWeights = false, int cwcLibraryCallWeight = 0)
    {
        _clcCalculator = new ClcCalculator(acTypeWeights);
        _cwcCalculator = new CwcCalculator(cwcLibraryCallWeight);
    }

    /// <summary>
    /// The CoC of v1 (--coc-legacy): Σ(CLC for all classes) + Σ(CWC for all classes) / 2, without the inheritance
    /// hierarchy. See <see cref="CocCalculator"/> for Eq. 27.
    /// </summary>
    public int CalculateCc(
        IReadOnlyList<string> allClassNames,
        IReadOnlyDictionary<string, BcsInfo> methodBcsInfo,
        IReadOnlyDictionary<string, List<string>> classAttributes,
        IReadOnlyDictionary<string, List<string>> classAttributeTypes,
        IReadOnlyList<CkMethodInfo> allMethods,
        IReadOnlyDictionary<string, ClassRelationshipBuilder.ClassInfo> allClasses,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? implementations = null)
    {
        int totalClc = 0;
        int totalCwc = 0;

        foreach (var classKey in allClassNames)
        {
            // Calculate CLC for each class
            var clc = _clcCalculator.CalculateClc(classKey, methodBcsInfo, classAttributes, classAttributeTypes);
            totalClc += clc;

            // Calculate CWC for each class
            // The same CWC as the class rows (implementations included)
            var cwc = _cwcCalculator.CalculateCwc(classKey, allMethods, methodBcsInfo, allClasses, implementations);
            totalCwc += cwc;
        }

        // CC = sum of all CLC + half of sum of all CWC (to avoid double-counting)
        return totalClc + (totalCwc / 2);
    }
}

/// <summary>
/// How CoC combines the CLC of the classes of an inheritance hierarchy (D1).
/// </summary>
public enum CocFormula
{
    /// <summary>Recursive, as in the example of Misra et al. (2018): CoC(c) = CLC(c) × Σ CoC(child), or CLC(c) for a leaf.</summary>
    Tree,
    /// <summary>Eq. 27 read level by level: the product, over the levels of the hierarchy, of the Σ CLC of each level.</summary>
    Levels
}

/// <summary>
/// CoC of one inheritance hierarchy: its root, the number of classes and levels, and its CoC.
/// </summary>
public sealed record CocHierarchy(string RootKey, int Classes, int Levels, long CoC);

/// <summary>
/// Calculator for CoC (Code Complexity, Eq. 27 of Misra et al., 2018): CLC of classes in the same level add, levels of
/// a hierarchy multiply, distinct hierarchies add (INC-20). CWC does not enter.
/// </summary>
public class CocCalculator
{
    private readonly CocFormula _formula;

    public CocCalculator(CocFormula formula = CocFormula.Tree)
    {
        _formula = formula;
    }

    /// <summary>
    /// Number of hierarchies whose CoC exceeded 64 bits; their CoC is reported as <see cref="long.MaxValue"/>.
    /// </summary>
    public int Overflows { get; private set; }

    /// <summary>
    /// The CoC of each hierarchy of the measured types. A root is a type whose base class is not in the analysed
    /// code (an external base class or System.Object, as for DIT); a type without subclasses or base class in the
    /// code is a hierarchy of its own.
    /// </summary>
    /// <param name="clc">CLC of each measured type, by class key.</param>
    /// <param name="baseClass">Base class key of each measured type, or null.</param>
    /// <param name="children">Subclasses of a type in the analysed code.</param>
    public List<CocHierarchy> Hierarchies(
        IReadOnlyDictionary<string, int> clc,
        Func<string, string?> baseClass,
        Func<string, IEnumerable<string>> children)
    {
        IEnumerable<string> Children(string key) => children(key).Where(clc.ContainsKey).Distinct();

        var roots = clc.Keys
            .Where(key => baseClass(key) is not { } b || !clc.ContainsKey(b))
            .OrderBy(key => key, StringComparer.Ordinal);

        var hierarchies = new List<CocHierarchy>();
        foreach (var root in roots)
        {
            var levels = Levels(root, Children);
            long coc;
            try
            {
                coc = _formula == CocFormula.Tree
                    ? Tree(root, clc, Children, new HashSet<string>())
                    : levels.Aggregate(1L, (product, level) => checked(product * level.Sum(key => (long)clc[key])));
            }
            catch (OverflowException)
            {
                Overflows++;
                coc = long.MaxValue;
            }

            hierarchies.Add(new CocHierarchy(root, levels.Sum(level => level.Count), levels.Count, coc));
        }

        return hierarchies;
    }

    /// <summary>
    /// CoC of the analysed code: the sum of the CoC of its hierarchies (<see cref="long.MaxValue"/> on overflow).
    /// </summary>
    public long CalculateCoc(IEnumerable<CocHierarchy> hierarchies)
    {
        try
        {
            return hierarchies.Aggregate(0L, (sum, h) => checked(sum + h.CoC));
        }
        catch (OverflowException)
        {
            Overflows++;
            return long.MaxValue;
        }
    }

    private static long Tree(string key, IReadOnlyDictionary<string, int> clc, Func<string, IEnumerable<string>> children,
        HashSet<string> visiting)
    {
        if (!visiting.Add(key))
        {
            return 0; // not valid C#, but never loop
        }

        var below = children(key).ToList();
        var coc = below.Count == 0
            ? clc[key]
            : checked(clc[key] * below.Aggregate(0L, (sum, child) => checked(sum + Tree(child, clc, children, visiting))));
        visiting.Remove(key);
        return coc;
    }

    private static List<List<string>> Levels(string root, Func<string, IEnumerable<string>> children)
    {
        var levels = new List<List<string>>();
        var seen = new HashSet<string> { root };
        var level = new List<string> { root };
        while (level.Count > 0)
        {
            levels.Add(level);
            level = level.SelectMany(children).Where(seen.Add).ToList();
        }

        return levels;
    }
}
