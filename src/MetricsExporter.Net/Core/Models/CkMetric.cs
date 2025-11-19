namespace MetricsExporter.Net.Core.Models;

/// <summary>
/// Represents Chidamber and Kemerer (CK) object-oriented design metrics for a class.
/// </summary>
public class CkMetric
{
    /// <summary>
    /// Path to the source file containing the class.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// WMC (Weighted Methods per Class): Sum of complexity of all methods in a class.
    /// For simplicity, this is the count of methods in the class.
    /// </summary>
    public int WMC { get; set; }

    /// <summary>
    /// DIT (Depth of Inheritance Tree): Maximum depth of a class in the inheritance hierarchy.
    /// Distance from the root class (System.Object = 0).
    /// </summary>
    public int DIT { get; set; }

    /// <summary>
    /// NOC (Number of Children): Count of immediate subclasses of a class.
    /// </summary>
    public int NOC { get; set; }

    /// <summary>
    /// CBO (Coupling Between Objects): Number of classes to which a class is coupled.
    /// Counts classes that are used by or use this class.
    /// </summary>
    public int CBO { get; set; }

    /// <summary>
    /// RFC (Response For a Class): Number of methods that can be invoked in response to a message.
    /// Includes methods in the class plus methods called by those methods.
    /// </summary>
    public int RFC { get; set; }

    /// <summary>
    /// LCOM (Lack of Cohesion of Methods): Measures the dissimilarity of methods in a class.
    /// Based on instance variable usage by methods.
    /// </summary>
    public int LCOM { get; set; }
}

