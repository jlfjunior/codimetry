using Codimetry.Core.Model;

namespace Codimetry.Metrics.Cyclomatic;

/// <summary>
/// Represents Cyclomatic Complexity metrics at different levels (file, class, method).
/// Cyclomatic Complexity measures code complexity based on decision points in the control flow.
/// Formula: CC = 1 + number of decision points
/// </summary>
public class CyclomaticComplexityMetric
{
    /// <summary>
    /// Path to the source file containing the class or method.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class. Empty for file-level entries.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the method. Empty for class/file-level entries.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Cyclomatic Complexity value.
    /// Calculated as: CC = 1 + number of decision points (if, switch, loops, catch, conditional operators, etc.)
    /// </summary>
    public int Complexity { get; set; }

    /// <summary>
    /// Identity of the type (project + fully qualified name, see TypeKeys) that the row belongs to;
    /// partial declarations share it. Empty for file rows and for members outside the measured types.
    /// </summary>
    public string ClassKey { get; set; } = string.Empty;
}
