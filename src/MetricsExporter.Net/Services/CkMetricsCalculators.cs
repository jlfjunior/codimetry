using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Services;

namespace MetricsExporter.Net.Services;

/// <summary>
/// Calculator for WMC (Weighted Methods per Class) metric.
/// </summary>
public class WmcCalculator
{
    /// <summary>
    /// Calculates WMC as the count of methods in a class.
    /// </summary>
    public int CalculateWmc(string className, IReadOnlyList<CkMethodInfo> allMethods)
    {
        return allMethods.Count(m => 
            m.ContainingClass == className && 
            !m.IsStatic); // Typically exclude static methods, but can be configurable
    }
}

/// <summary>
/// Calculator for DIT (Depth of Inheritance Tree) metric.
/// </summary>
public class DitCalculator
{
    private readonly ClassRelationshipBuilder _relationshipBuilder;

    public DitCalculator(ClassRelationshipBuilder relationshipBuilder)
    {
        _relationshipBuilder = relationshipBuilder;
    }

    /// <summary>
    /// Calculates DIT as the depth of a class in the inheritance hierarchy.
    /// </summary>
    public int CalculateDit(string className)
    {
        var chain = _relationshipBuilder.GetInheritanceChain(className);
        
        // DIT is the number of levels from the class to System.Object
        // If chain has the class itself, subtract 1
        // If no base class, DIT = 0 (directly inherits from Object)
        if (chain.Count <= 1)
        {
            return 0; // No inheritance or only self
        }

        // Count levels excluding the class itself
        return chain.Count - 1;
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
                var matchingClass = allClasses.Values.FirstOrDefault(c => 
                    c.FullyQualifiedName?.EndsWith(normalizedType) == true ||
                    c.Name == normalizedType);
                
                if (matchingClass != null)
                {
                    coupledClasses.Add(matchingClass.Name);
                }
            }
        }

        // Exclude self
        coupledClasses.Remove(dependency.ClassName);

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
    /// Calculates RFC as the number of methods that can be invoked in response to a message.
    /// RFC = M + R, where M is methods in class, R is methods called by those methods.
    /// </summary>
    public int CalculateRfc(string className, IReadOnlyList<CkMethodInfo> allMethods)
    {
        var classMethods = allMethods.Where(m => m.ContainingClass == className).ToList();
        var m = classMethods.Count;

        // Collect all methods called by class methods
        var calledMethods = new HashSet<string>();
        foreach (var method in classMethods)
        {
            foreach (var calledMethod in method.CalledMethods)
            {
                calledMethods.Add(calledMethod);
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
    /// Calculates LCOM using the original Chidamber-Kemerer formula.
    /// LCOM = P - Q, where:
    /// P = number of method pairs that don't share instance variables
    /// Q = number of method pairs that share instance variables
    /// </summary>
    public int CalculateLcom(string className, IReadOnlyList<CkMethodInfo> allMethods, IReadOnlyList<ClassDependency> dependencies)
    {
        var classMethods = allMethods
            .Where(m => m.ContainingClass == className && !m.IsStatic)
            .ToList();

        if (classMethods.Count <= 1)
        {
            return 0; // LCOM is 0 for 0 or 1 method
        }

        // Get all instance fields/properties for this class
        var classDependency = dependencies.FirstOrDefault(d => d.ClassName == className);
        var instanceFields = new HashSet<string>();
        if (classDependency != null)
        {
            foreach (var field in classDependency.FieldTypes)
            {
                instanceFields.Add(field);
            }
            foreach (var prop in classDependency.PropertyTypes)
            {
                instanceFields.Add(prop);
            }
        }

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

