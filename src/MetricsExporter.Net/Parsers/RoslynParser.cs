using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MetricsExporter.Net.Parsers;

/// <summary>
/// Parser for C# source code using Roslyn.
/// </summary>
public class RoslynParser
{
    public SyntaxTree? ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var sourceText = File.ReadAllText(filePath);
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, parseOptions, filePath);

            // Check for parse errors
            var diagnostics = syntaxTree.GetDiagnostics();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                // Log parse errors but still return the tree for partial analysis
                return syntaxTree;
            }

            return syntaxTree;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

