namespace MetricsExporter.Net.Core.Models;

/// <summary>
/// Represents a Lines of Code metric at different levels (file, class, method).
/// </summary>
public class LocMetric
{
    public string FilePath { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public int Loc { get; set; }
}

