using MetricsExporter.Net.CLI.Commands;
using System.CommandLine;

namespace MetricsExporter.Net.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = RootCommandHandler.CreateCommand();
        return await rootCommand.InvokeAsync(args);
    }
}

