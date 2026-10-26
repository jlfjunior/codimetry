namespace Codimetry.Core.Models;

/// <summary>
/// Represents a Lines of Code metric at different levels (file, class, method).
/// </summary>
public class LocMetric
{
    public string FilePath { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    /// <summary>Legacy count: non-blank, non-comment lines of methods and constructors.</summary>
    public int Loc { get; set; }

    /// <summary>Logical LOC: declarations plus executable statements.</summary>
    public int Lloc { get; set; }

    /// <summary>Physical LOC: every line of the file. Only set on file rows.</summary>
    public int? PhysicalLoc { get; set; }
}

