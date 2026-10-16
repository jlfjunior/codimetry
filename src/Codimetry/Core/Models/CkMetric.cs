namespace Codimetry.Core.Models;

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
    /// WMC (Weighted Methods per Class): Sum of c_i over the methods and constructors declared in
    /// the class, static ones included; c_i = 1 or V(G) (see <c>WmcWeight</c>).
    /// </summary>
    public int WMC { get; set; }

    /// <summary>
    /// DIT (Depth of Inheritance Tree): Number of declared inheritance edges from the class to the root of its
    /// hierarchy. System.Object does not count (a class without an explicit base has DIT = 0); a base class from
    /// outside the analysed code counts 1 (see <c>DitOptions</c>).
    /// </summary>
    public int DIT { get; set; }

    /// <summary>
    /// NOC (Number of Children): Count of immediate subclasses of a class in the analysed code.
    /// </summary>
    public int NOC { get; set; }

    /// <summary>
    /// CBO (Coupling Between Objects): Number of other types of the analysed code (classes, records, structs and
    /// interfaces) the class uses, through any type reference: inheritance, members, locals, <c>new</c>, static
    /// members, attributes, generic arguments. Outgoing only by default (see <c>CboOptions</c>).
    /// </summary>
    public int CBO { get; set; }

    /// <summary>
    /// RFC (Response For a Class): Number of methods that can be invoked in response to a message: the methods and
    /// constructors of the class plus the distinct methods and constructors they call directly, identified by symbol.
    /// </summary>
    public int RFC { get; set; }

    /// <summary>
    /// LCOM (Lack of Cohesion of Methods), LCOM1: pairs of instance methods (constructors excluded) that share no
    /// attribute of the class, minus the pairs that share one, or 0 if negative.
    /// </summary>
    public int LCOM { get; set; }

    /// <summary>
    /// Number of methods LCOM is computed over (instance methods, no constructors), so that its
    /// maximum n(n−1)/2 can be checked without assuming it equals WMC.
    /// </summary>
    public int LcomMethods { get; set; }

    /// <summary>
    /// Kind of type: class, record, struct or record struct.
    /// </summary>
    public string Kind { get; set; } = "class";
}

