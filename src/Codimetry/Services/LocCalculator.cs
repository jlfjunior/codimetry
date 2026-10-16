namespace Codimetry.Services;

/// <summary>
/// Service for calculating Lines of Code, excluding comments and empty lines.
/// </summary>
public class LocCalculator
{
    public int CalculateLoc(string sourceText, int startLine, int endLine)
    {
        if (string.IsNullOrEmpty(sourceText))
            return 0;

        var lines = sourceText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        if (startLine < 1 || endLine > lines.Length || startLine > endLine)
            return 0;

        var locCount = 0;
        var inMultiLineComment = false;

        for (int i = startLine - 1; i < endLine && i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // Handle multi-line comments
            if (inMultiLineComment)
            {
                var commentEnd = trimmedLine.IndexOf("*/", StringComparison.Ordinal);
                if (commentEnd >= 0)
                {
                    inMultiLineComment = false;
                    // Check if there's code after the comment end
                    var remaining = trimmedLine.Substring(commentEnd + 2).Trim();
                    if (!string.IsNullOrWhiteSpace(remaining) && !remaining.StartsWith("//"))
                    {
                        locCount++;
                    }
                }
                continue;
            }

            // Check for start of multi-line comment
            var multiLineStart = trimmedLine.IndexOf("/*", StringComparison.Ordinal);
            if (multiLineStart >= 0)
            {
                var multiLineEnd = trimmedLine.IndexOf("*/", multiLineStart + 2, StringComparison.Ordinal);
                if (multiLineEnd >= 0)
                {
                    // Single-line multi-line comment (/* ... */)
                    var beforeComment = trimmedLine.Substring(0, multiLineStart).Trim();
                    var afterComment = trimmedLine.Substring(multiLineEnd + 2).Trim();
                    
                    if (!string.IsNullOrWhiteSpace(beforeComment) && !beforeComment.StartsWith("//"))
                        locCount++;
                    else if (!string.IsNullOrWhiteSpace(afterComment) && !afterComment.StartsWith("//"))
                        locCount++;
                }
                else
                {
                    // Multi-line comment starts here
                    inMultiLineComment = true;
                    var beforeComment = trimmedLine.Substring(0, multiLineStart).Trim();
                    if (!string.IsNullOrWhiteSpace(beforeComment) && !beforeComment.StartsWith("//"))
                        locCount++;
                }
                continue;
            }

            // Skip single-line comments
            if (trimmedLine.StartsWith("//", StringComparison.Ordinal))
                continue;

            // Skip XML documentation comments
            if (trimmedLine.StartsWith("///", StringComparison.Ordinal))
                continue;

            // Count this line as LOC
            locCount++;
        }

        return locCount;
    }
}

