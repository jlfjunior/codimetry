using MetricsExporter.Net.Parsers;

namespace MetricsExporter.Net.Services;

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
/// Calculator for CWC (Coupling Weight for a Class) metric.
/// </summary>
public class CwcCalculator
{
    private readonly McCalculator _mcCalculator;

    public CwcCalculator()
    {
        _mcCalculator = new McCalculator();
    }

    /// <summary>
    /// Calculates CWC (Coupling Weight for a Class).
    /// CWC = Σ(MC_called_method + 2) for all external method calls.
    /// A resolved call to another class of the analysed code adds that method's MC + 2; through an
    /// interface or abstract method, the MC of its implementation when there is exactly one.
    /// Calls to referenced libraries, and to abstract members with no or several implementations,
    /// add 3 (MC 1 + 2). Unresolved calls fall back to the first method with the same name.
    /// </summary>
    /// <param name="implementations">Abstract or interface method key → keys of its implementations.</param>
    public int CalculateCwc(string classKey, 
        IReadOnlyList<CkMethodInfo> allMethods,
        IReadOnlyDictionary<string, BcsInfo> methodBcsInfo,
        IReadOnlyDictionary<string, ClassRelationshipBuilder.ClassInfo> allClasses,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? implementations = null)
    {
        // Get all methods in this class
        var classMethods = allMethods.Where(m => m.ContainingClassKey == classKey).ToList();

        var totalCwc = 0;

        foreach (var method in classMethods)
        {
            foreach (var call in method.Calls.Where(c => c.IsResolved))
            {
                if (call.TargetClassKey == classKey)
                {
                    continue; // own method or local function: not coupling
                }

                var targetKey = call.TargetKey;
                if (call.IsAbstract && targetKey != null)
                {
                    targetKey = implementations != null && implementations.TryGetValue(targetKey, out var impls) && impls.Count == 1
                        ? impls[0]
                        : null;
                }

                totalCwc += targetKey != null && methodBcsInfo.TryGetValue(targetKey, out var bcs)
                    ? bcs.CalculateMc() + 2
                    : 3;
            }

            // Unresolved calls: matched by simple name
            foreach (var calledMethodName in method.Calls.Where(c => !c.IsResolved).Select(c => c.Name))
            {
                // Calls to the class's own methods are not coupling
                if (allMethods.Any(m => m.Name == calledMethodName && m.ContainingClassKey == classKey))
                {
                    continue;
                }

                // Find the method that was called
                var calledMethod = allMethods.FirstOrDefault(m => 
                    m.Name == calledMethodName && 
                    m.ContainingClassKey != classKey); // External call only

                if (calledMethod != null)
                {
                    // Get MC of the called method
                    var calledMc = _mcCalculator.CalculateMc(
                        calledMethod.ContainingClassKey ?? string.Empty,
                        calledMethod.Key,
                        methodBcsInfo);

                    // CWC contribution = MC of called method + call weight (2)
                    totalCwc += calledMc + 2;
                }
                else
                {
                    // Method call to external library or unresolved
                    // Use default MC of 1 + call weight (2) = 3
                    totalCwc += 3;
                }
            }
        }

        return totalCwc;
    }
}

/// <summary>
/// Calculator for CC (Code Complexity) metric.
/// </summary>
public class CcCalculator
{
    private readonly ClcCalculator _clcCalculator;
    private readonly CwcCalculator _cwcCalculator;

    public CcCalculator(bool acTypeWeights = false)
    {
        _clcCalculator = new ClcCalculator(acTypeWeights);
        _cwcCalculator = new CwcCalculator();
    }

    /// <summary>
    /// Calculates CC (Code Complexity) for the entire codebase.
    /// CC = Σ(CLC for all classes) + Σ(CWC for all classes) / 2
    /// The division by 2 avoids double-counting coupling (each coupling affects two classes).
    /// </summary>
    public int CalculateCc(
        IReadOnlyList<string> allClassNames,
        IReadOnlyDictionary<string, BcsInfo> methodBcsInfo,
        IReadOnlyDictionary<string, List<string>> classAttributes,
        IReadOnlyDictionary<string, List<string>> classAttributeTypes,
        IReadOnlyList<CkMethodInfo> allMethods,
        IReadOnlyDictionary<string, ClassRelationshipBuilder.ClassInfo> allClasses)
    {
        int totalClc = 0;
        int totalCwc = 0;

        foreach (var classKey in allClassNames)
        {
            // Calculate CLC for each class
            var clc = _clcCalculator.CalculateClc(classKey, methodBcsInfo, classAttributes, classAttributeTypes);
            totalClc += clc;

            // Calculate CWC for each class
            var cwc = _cwcCalculator.CalculateCwc(classKey, allMethods, methodBcsInfo, allClasses);
            totalCwc += cwc;
        }

        // CC = sum of all CLC + half of sum of all CWC (to avoid double-counting)
        return totalClc + (totalCwc / 2);
    }
}

