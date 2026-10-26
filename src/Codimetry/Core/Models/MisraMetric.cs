namespace Codimetry.Core.Models;

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
    /// MC (Method Complexity, Eq. 23): cognitive weights of the Basic Control Structures (BCSs) of the method,
    /// added within a layer and multiplied along nesting. Weights: sequence 1, branch (if, ternary) 2,
    /// case (switch) 3, iteration 3; a call to a method of the analysed code adds 2 + MC of the called method.
    /// Only populated for method-level entries.
    /// </summary>
    public int MC { get; set; }

    /// <summary>
    /// AC (Attribute Complexity, Eq. 25): number of instance attributes the class declares (fields, auto-properties,
    /// field-like events, used primary constructor parameters), 1 each. Only populated for class-level entries.
    /// </summary>
    public int AC { get; set; }

    /// <summary>
    /// CLC (Class Complexity): AC + Σ MC of the methods of the class. Only populated for class-level entries.
    /// </summary>
    public int CLC { get; set; }

    /// <summary>
    /// CWC (Coupling Weight for a Class, Eq. 24): Σ (2 + MC of the called method) over the calls to methods of other
    /// types of the analysed code; library calls and constructors do not count. Only populated for class-level entries.
    /// </summary>
    public int CWC { get; set; }

    /// <summary>
    /// CoC (Code Complexity, Eq. 27): CLC of classes in the same level of an inheritance hierarchy add, levels
    /// multiply, hierarchies add. Only populated for codebase-level entries.
    /// </summary>
    public long CoC { get; set; }

    /// <summary>
    /// The former name of <see cref="CoC"/>, kept in the CSV with the same value for one version.
    /// </summary>
    public long CC
    {
        get => CoC;
        set => CoC = value;
    }

    /// <summary>
    /// Kind of the type of the row (class, record, struct or record struct). Empty for the codebase row.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
}
