using MetricsExporter.Net.Parsers;

namespace MetricsExporter.Net.Services;

/// <summary>
/// Builds and maintains class relationship graphs for CK metrics calculation.
/// </summary>
public class ClassRelationshipBuilder
{
    private readonly Dictionary<string, ClassInfo> _classMap = new();
    private readonly Dictionary<string, List<string>> _inheritanceTree = new();
    private readonly Dictionary<string, List<string>> _childrenMap = new();

    /// <summary>
    /// Information about a class for relationship building.
    /// </summary>
    public class ClassInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? FullyQualifiedName { get; set; }
        public string? BaseClass { get; set; }
        public string? Namespace { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        /// <summary>
        /// Ancestors of the base class above it, System.Object excluded (see <see cref="CkCodeElement.BaseAncestors"/>).
        /// </summary>
        public int BaseAncestors { get; set; }
    }

    /// <summary>
    /// Adds a class to the relationship graph.
    /// </summary>
    public void AddClass(CkCodeElement classElement, string filePath)
    {
        var key = GetClassKey(classElement);
        var classInfo = new ClassInfo
        {
            Name = classElement.Name,
            FullyQualifiedName = classElement.FullyQualifiedName ?? classElement.Name,
            BaseClass = classElement.BaseClass,
            Namespace = classElement.Namespace,
            FilePath = filePath,
            IsAbstract = classElement.IsAbstract,
            IsSealed = classElement.IsSealed,
            BaseAncestors = classElement.BaseAncestors
        };

        // A partial class is added once per declaration: keep the file of the first one
        if (_classMap.TryGetValue(key, out var existing))
        {
            classInfo.FilePath = existing.FilePath;
        }

        _classMap[key] = classInfo;

        // Build inheritance tree
        if (!string.IsNullOrEmpty(classElement.BaseClass))
        {
            var baseKey = NormalizeClassName(classElement.BaseClass);
            if (!_inheritanceTree.ContainsKey(key))
            {
                _inheritanceTree[key] = new List<string>();
            }
            _inheritanceTree[key].Add(baseKey);

            // Build children map (reverse of inheritance)
            if (!_childrenMap.ContainsKey(baseKey))
            {
                _childrenMap[baseKey] = new List<string>();
            }
            if (!_childrenMap[baseKey].Contains(key))
            {
                _childrenMap[baseKey].Add(key);
            }
        }
    }

    /// <summary>
    /// Gets the base class for a given class.
    /// </summary>
    public string? GetBaseClass(string className)
    {
        var key = NormalizeClassName(className);
        if (_classMap.TryGetValue(key, out var classInfo))
        {
            return classInfo.BaseClass;
        }
        return null;
    }

    /// <summary>
    /// Gets all immediate children (subclasses) of a class.
    /// </summary>
    public List<string> GetChildren(string className)
    {
        var key = NormalizeClassName(className);
        if (_childrenMap.TryGetValue(key, out var children))
        {
            return children.ToList();
        }
        return new List<string>();
    }

    /// <summary>
    /// Gets the inheritance chain for a class (from class to root).
    /// </summary>
    public List<string> GetInheritanceChain(string className)
    {
        var chain = new List<string>();
        var current = NormalizeClassName(className);
        var visited = new HashSet<string>();

        while (!string.IsNullOrEmpty(current) && !visited.Contains(current))
        {
            visited.Add(current);
            chain.Add(current);

            if (_classMap.TryGetValue(current, out var classInfo) && !string.IsNullOrEmpty(classInfo.BaseClass))
            {
                current = NormalizeClassName(classInfo.BaseClass);
            }
            else
            {
                break;
            }
        }

        return chain;
    }

    /// <summary>
    /// Gets all classes in the relationship graph.
    /// </summary>
    public IReadOnlyDictionary<string, ClassInfo> GetAllClasses() => _classMap;

    /// <summary>
    /// Clears all relationship data.
    /// </summary>
    public void Clear()
    {
        _classMap.Clear();
        _inheritanceTree.Clear();
        _childrenMap.Clear();
    }

    private string GetClassKey(CkCodeElement classElement)
    {
        if (!string.IsNullOrEmpty(classElement.ClassKey))
        {
            return NormalizeClassName(classElement.ClassKey);
        }
        if (!string.IsNullOrEmpty(classElement.FullyQualifiedName))
        {
            return NormalizeClassName(classElement.FullyQualifiedName);
        }
        if (!string.IsNullOrEmpty(classElement.Namespace))
        {
            return NormalizeClassName($"{classElement.Namespace}.{classElement.Name}");
        }
        return NormalizeClassName(classElement.Name);
    }

    private string NormalizeClassName(string className)
    {
        // Remove generic parameters for matching
        var index = className.IndexOf('<');
        if (index > 0)
        {
            className = className.Substring(0, index);
        }
        return className.Trim();
    }
}

