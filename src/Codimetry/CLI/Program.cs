using Codimetry.CLI.Commands;
using System.CommandLine;

namespace Codimetry.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = RootCommandHandler.CreateCommand();
        return await rootCommand.InvokeAsync(args);
    }
}

