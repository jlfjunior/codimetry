using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Logging;

namespace Codimetry.Core.Suites;

/// <summary>
/// A suite of metrics: the options it adds to the command line and the CSV files it writes.
/// </summary>
public interface IMetricSuite
{
    /// <summary>
    /// The flag that selects the suite (--loc, --ck...). When no suite is selected, every suite runs.
    /// </summary>
    Option<bool> Selector { get; }

    /// <summary>
    /// Options of the suite's own rules, listed in --help after its selector.
    /// </summary>
    IReadOnlyList<Option> Options { get; }

    /// <summary>
    /// Calculates the metrics of the suite and writes its CSV files to the output directory.
    /// </summary>
    Task RunAsync(AnalysisContext context, ParseResult parseResult, string outputDirectory, ILoggerFactory loggerFactory);
}
