namespace Codimetry.Core.Models;

/// <summary>
/// CoC of one inheritance hierarchy of the analysed code (a row of metrics_coc.csv). The CoC of the codebase,
/// in metrics_misra.csv, is the sum of these rows.
/// </summary>
public class CocHierarchyMetric
{
    /// <summary>
    /// File of the root class.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Root of the hierarchy: a type whose base class is not in the analysed code.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Number of types in the hierarchy, the root included.
    /// </summary>
    public int Classes { get; set; }

    /// <summary>
    /// Number of levels of the hierarchy; 1 for a type without subclasses.
    /// </summary>
    public int Levels { get; set; }

    public long CoC { get; set; }

    /// <summary>
    /// Kind of the root (class, record, struct or record struct).
    /// </summary>
    public string Kind { get; set; } = string.Empty;
}
