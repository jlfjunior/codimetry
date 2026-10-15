namespace MetricsExporter.Net.Core.Models;

/// <summary>
/// Represents MISRA (Misra's Cognitive Complexity) metrics for methods and classes.
/// </summary>
public class MisraMetric
{
    /// <summary>
    /// Path to the source file containing the class or method.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class. Empty for codebase-level metrics.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the method. Empty for class-level or codebase-level metrics.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// MC (Method Complexity): Cognitive complexity of a method calculated by summing 
    /// the cognitive weights of all Basic Control Structures (BCSs) within the method.
    /// Weights: Sequence=1, Branch=2, Iteration=3, Call=2.
    /// Only populated for method-level entries.
    /// </summary>
    public int MC { get; set; }

    /// <summary>
    /// AC (Attribute Complexity): Measures the complexity introduced by attributes 
    /// (fields/properties) within a class. Only populated for class-level entries.
    /// </summary>
    public int AC { get; set; }

    /// <summary>
    /// CLC (Class Complexity): Overall complexity of a class, aggregating complexities 
    /// from all methods and attributes within the class. Only populated for class-level entries.
    /// </summary>
    public int CLC { get; set; }

    /// <summary>
    /// CWC (Coupling Weight for a Class): Evaluates the complexity due to inter-class 
    /// interactions, calculated by summing the weights of calls to other classes' methods 
    /// (including the call weight of 2). Only populated for class-level entries.
    /// </summary>
    public int CWC { get; set; }

    /// <summary>
    /// CC (Code Complexity): Overall complexity of the codebase, aggregating complexities 
    /// from all classes and their interactions. Only populated for codebase-level entries.
    /// </summary>
    public int CC { get; set; }

    /// <summary>
    /// Kind of the type of the row (class, record, struct or record struct). Empty for the codebase row.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
}
