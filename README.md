# MetricsExporter.Net

A .NET command-line tool designed to extract software metrics from local .NET repositories. This tool calculates **Lines of Code (LOC)** metrics and **Chidamber-Kemerer (CK)** object-oriented design metrics to help analyze code quality and maintainability.

## Features

- ✅ **Lines of Code (LOC) Calculation** - Counts effective lines of code excluding comments and empty lines
- ✅ **CK Metrics (Chidamber-Kemerer)** - Calculates six object-oriented design metrics:
  - **WMC** (Weighted Methods per Class) - Method count per class
  - **DIT** (Depth of Inheritance Tree) - Inheritance depth
  - **NOC** (Number of Children) - Immediate subclasses count
  - **CBO** (Coupling Between Objects) - Class coupling measure
  - **RFC** (Response For a Class) - Methods invocable in response
  - **LCOM** (Lack of Cohesion of Methods) - Method cohesion measure
- ✅ **Multi-level Analysis** - Calculates LOC at method, class, and file levels
- ✅ **Recursive Directory Scanning** - Analyzes entire codebases recursively
- ✅ **CSV Export** - Exports metrics to standardized CSV formats
- ✅ **Flexible Metric Selection** - Choose to calculate LOC, CK, or both metrics
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

#### Required Options (Choose One)

| Option | Short | Description |
|--------|-------|-------------|
| `--path` | `-p` | Path to a directory containing .NET source code |
| `--solution` | `-s` | Path to a .sln solution file |
| `--project` | | Path to a .csproj project file |

**Note:** Only one of `--path`, `--solution`, or `--project` can be specified at a time.

#### Optional Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--out` | `-o` | Output directory for the CSV files | `./metrics` |
| `--no-recursive` | | Disable recursive directory scanning (only applies to `--path`) | `false` |
| `--verbose` | `-v` | Enable verbose logging | `false` |
| `--loc` | | Calculate and export LOC (Lines of Code) metrics | `false` |
| `--ck` | | Calculate and export CK (Chidamber-Kemerer) object-oriented design metrics | `false` |
| `--help` | `-h`, `-?` | Show help and usage information | |

**Metric Selection:**
- If neither `--loc` nor `--ck` is specified, **both metrics are calculated** (default behavior)
- Use `--loc` to calculate only LOC metrics
- Use `--ck` to calculate only CK metrics
- Use `--loc --ck` to explicitly calculate both metrics

### Examples

**Analyze a directory (both LOC and CK metrics - default):**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src
```

**Calculate only LOC metrics:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --loc
```

**Calculate only CK metrics:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --ck
```

**Explicitly calculate both metrics:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --loc --ck
```

**Analyze a solution file:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --solution MySolution.sln
```

**Analyze a project file:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --project ./src/MyProject/MyProject.csproj
```

**Specify custom output directory:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --out ./results
```

**Non-recursive analysis with verbose logging:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --no-recursive --verbose
```

**Get help information:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --help
# or
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- -h
```

## Output Format

The tool generates CSV files in the specified output directory. The files generated depend on which metrics are calculated:

- **`metrics_loc.csv`** - Generated when LOC metrics are calculated
- **`metrics_ck.csv`** - Generated when CK metrics are calculated

### LOC Metrics Output (`metrics_loc.csv`)

#### CSV Format

```csv
FilePath,ClassName,MethodName,LOC
```

#### Columns

- **FilePath**: Relative or absolute path to the source file
- **ClassName**: Name of the class (empty for file-level entries)
- **MethodName**: Name of the method or constructor (empty for class/file-level entries)
- **LOC**: Lines of Code count

#### Output Levels

The CSV includes metrics at three levels:

1. **Method Level**: Individual methods and constructors with their LOC
2. **Class Level**: Aggregated LOC for each class (sum of all methods)
3. **File Level**: Aggregated LOC for each file (sum of all classes)

#### Example Output

```csv
FilePath,ClassName,MethodName,LOC
src/Example.cs,ExampleClass,ExampleClass,3
src/Example.cs,ExampleClass,DoWork,5
src/Example.cs,ExampleClass,GetResult,4
src/Example.cs,ExampleClass,,12
src/Example.cs,,,12
```

### CK Metrics Output (`metrics_ck.csv`)

#### CSV Format

```csv
FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM
```

#### Columns

- **FilePath**: Relative or absolute path to the source file
- **ClassName**: Name of the class
- **WMC**: Weighted Methods per Class (method count)
- **DIT**: Depth of Inheritance Tree (inheritance depth, 0 = directly inherits from Object)
- **NOC**: Number of Children (immediate subclasses count)
- **CBO**: Coupling Between Objects (number of classes coupled to)
- **RFC**: Response For a Class (methods in class + methods called)
- **LCOM**: Lack of Cohesion of Methods (cohesion measure, lower is better)

#### Example Output

```csv
FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM
src/Example.cs,ExampleClass,5,1,2,3,15,8
src/Service.cs,UserService,8,0,0,5,25,12
```

**Note:** CK metrics are calculated at the class level only (one row per class).

## How Metrics are Calculated

### LOC (Lines of Code)

The tool counts **effective lines of code** by:

- ✅ Including executable statements
- ❌ Excluding single-line comments (`//`)
- ❌ Excluding multi-line comments (`/* */`)
- ❌ Excluding XML documentation comments (`///`)
- ❌ Excluding empty lines (whitespace only)

### CK Metrics (Chidamber-Kemerer)

The six CK metrics provide insights into object-oriented design quality:

- **WMC (Weighted Methods per Class)**: Counts the number of methods in a class. Higher values may indicate increased complexity.
- **DIT (Depth of Inheritance Tree)**: Measures how deep a class is in the inheritance hierarchy. Higher values indicate deeper inheritance chains.
- **NOC (Number of Children)**: Counts immediate subclasses. Higher values indicate more specialization.
- **CBO (Coupling Between Objects)**: Measures how many classes a class is coupled to. Lower values indicate better encapsulation.
- **RFC (Response For a Class)**: Counts methods that can be invoked in response to a message (class methods + methods they call). Higher values may indicate more complex interactions.
- **LCOM (Lack of Cohesion of Methods)**: Measures how dissimilar methods are based on instance variable usage. Lower values indicate better cohesion.

These metrics help identify:
- Classes that may need refactoring (high WMC, CBO, or LCOM)
- Complex inheritance hierarchies (high DIT)
- Tightly coupled code (high CBO)
- Classes with poor cohesion (high LCOM)

## Architecture

The project follows **Simple Layered Architecture** with the following structure:

- **Core/Models**: Data models (LocMetric, CkMetric)
- **Services**: Business logic (file discovery, metrics analysis, LOC calculation, CK metrics calculation)
- **Parsers**: File parsing (Roslyn, Solution, Project parsers, CK syntax analysis)
- **Exporters**: Output generation (CSV export for LOC and CK metrics)
- **CLI**: Command-line interface

## Requirements

- .NET 9.0 or later
- Windows, Linux, or macOS

## Performance

The tool is optimized for performance:
- Parallel file processing
- Efficient Roslyn syntax tree analysis
- Memory-efficient processing
- Two-phase CK metrics analysis (relationship graph building, then metric calculation)

Target performance: Analyzes repositories up to 100k LOC in under 2 minutes (LOC metrics). CK metrics analysis may take additional time depending on codebase complexity.

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]
