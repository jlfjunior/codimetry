using System.Text;

namespace Codimetry.Core.Output;

/// <summary>
/// Writes the CSV files of the metric suites: a header line, then one line per row.
/// </summary>
public static class CsvWriter
{
    public static async Task WriteAsync<T>(string outputPath, string header, IEnumerable<T> rows, Func<T, string> line)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var csv = new StringBuilder();
        csv.AppendLine(header);
        foreach (var row in rows)
        {
            csv.AppendLine(line(row));
        }

        await File.WriteAllTextAsync(outputPath, csv.ToString());
    }

    public static string Escape(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        // If field contains comma, quote, or newline, wrap in quotes and escape quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
