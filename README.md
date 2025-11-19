# MetricsExporter.Net

A .NET command-line tool designed to extract software metrics from local .NET repositories. This MVP version focuses on calculating **Lines of Code (LOC)** at file, class, and method levels.

## Features

- ✅ **Lines of Code (LOC) Calculation** - Counts effective lines of code excluding comments and empty lines
- ✅ **Multi-level Analysis** - Calculates LOC at method, class, and file levels
- ✅ **Recursive Directory Scanning** - Analyzes entire codebases recursively
- ✅ **CSV Export** - Exports metrics to a standardized CSV format
- ✅ **Cross-platform** - Runs on Windows, Linux, and macOS (.NET 9.0+)
- ✅ **Performance Optimized** - Parallel processing for fast analysis

## Installation

### Prerequisites

- .NET 9.0 SDK or later

### Build from Source

```bash
git clone <repository-url>
cd metrics-exporter-net
dotnet build src/MetricsExporter.Net/MetricsExporter.Net.csproj
```

## Documentation

📖 **For detailed instructions on collecting metrics, see [COLLECTING_METRICS.md](COLLECTING_METRICS.md)**

## Usage

### Basic Usage

You can analyze code using one of three input options:

- **`--path`** (`-p`): Analyze a directory containing .NET source code
- **`--solution`** (`-s`): Analyze a .sln solution file
- **`--project`**: Analyze a .csproj project file

**Note:** Only one of these options can be specified at a time.

### Command-Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--path` | `-p` | Path to a directory containing .NET source code | One required |
| `--solution` | `-s` | Path to a .sln solution file | One required |
| `--project` | | Path to a .csproj project file | One required |
| `--out` | `-o` | Output directory for the CSV file | `./metrics` |
| `--no-recursive` | | Disable recursive directory scanning (only applies to `--path`) | `false` |
| `--verbose` | `-v` | Enable verbose logging | `false` |
| `--help` | `-h`, `-?` | Show help and usage information | |

### Examples

**Analyze a directory recursively:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src
```

**Analyze a solution file:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --solution MySolution.sln
```

**Analyze a project file:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --project ./src/MyProject/MyProject.csproj
```

**Get help information:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --help
# or
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- -h
```

**Specify custom output directory:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --out ./results
```

**Non-recursive analysis with verbose logging:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --no-recursive --verbose
```

## Output Format

The tool generates a `metrics_loc.csv` file in the specified output directory with the following structure:

### CSV Format

```csv
FilePath,ClassName,MethodName,LOC
```

### Columns

- **FilePath**: Relative or absolute path to the source file
- **ClassName**: Name of the class (empty for file-level entries)
- **MethodName**: Name of the method or constructor (empty for class/file-level entries)
- **LOC**: Lines of Code count

### Output Levels

The CSV includes metrics at three levels:

1. **Method Level**: Individual methods and constructors with their LOC
2. **Class Level**: Aggregated LOC for each class (sum of all methods)
3. **File Level**: Aggregated LOC for each file (sum of all classes)

### Example Output

```csv
FilePath,ClassName,MethodName,LOC
src/Example.cs,ExampleClass,ExampleClass,3
src/Example.cs,ExampleClass,DoWork,5
src/Example.cs,ExampleClass,GetResult,4
src/Example.cs,ExampleClass,,12
src/Example.cs,,,12
```

## How LOC is Calculated

The tool counts **effective lines of code** by:

- ✅ Including executable statements
- ❌ Excluding single-line comments (`//`)
- ❌ Excluding multi-line comments (`/* */`)
- ❌ Excluding XML documentation comments (`///`)
- ❌ Excluding empty lines (whitespace only)

## Architecture

The project follows **Simple Layered Architecture** with the following structure:

- **Core/Models**: Data models (LocMetric)
- **Services**: Business logic (file discovery, metrics analysis, LOC calculation)
- **Parsers**: File parsing (Roslyn, Solution, Project parsers)
- **Exporters**: Output generation (CSV export)
- **CLI**: Command-line interface

## Requirements

- .NET 9.0 or later
- Windows, Linux, or macOS

## Performance

The tool is optimized for performance:
- Parallel file processing
- Efficient Roslyn syntax tree analysis
- Memory-efficient processing

Target performance: Analyzes repositories up to 100k LOC in under 2 minutes.

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]
