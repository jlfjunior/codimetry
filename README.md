# MetricsExporter.Net

A .NET command-line tool designed to extract software metrics from local .NET repositories. This tool calculates **Lines of Code (LOC)** metrics, **Chidamber-Kemerer (CK)** object-oriented design metrics, **MISRA (Misra's Cognitive Complexity)** metrics, and **Cyclomatic Complexity** metrics to help analyze code quality and maintainability.

## Features

- ✅ **Lines of Code (LOC) Calculation** - Counts effective lines of code excluding comments and empty lines
- ✅ **CK Metrics (Chidamber-Kemerer)** - Calculates six object-oriented design metrics:
  - **WMC** (Weighted Methods per Class) - Method count per class
  - **DIT** (Depth of Inheritance Tree) - Inheritance depth
  - **NOC** (Number of Children) - Immediate subclasses count
  - **CBO** (Coupling Between Objects) - Class coupling measure
  - **RFC** (Response For a Class) - Methods invocable in response
  - **LCOM** (Lack of Cohesion of Methods) - Method cohesion measure
- ✅ **MISRA Metrics (Misra's Cognitive Complexity)** - Calculates five cognitive complexity metrics:
  - **MC** (Method Complexity) - Cognitive complexity of methods based on Basic Control Structures
  - **AC** (Attribute Complexity) - Complexity introduced by class attributes
  - **CLC** (Class Complexity) - Overall complexity of classes
  - **CWC** (Coupling Weight for a Class) - Complexity due to inter-class interactions
  - **CC** (Code Complexity) - Overall complexity of the codebase
- ✅ **Cyclomatic Complexity Metrics** - Measures code complexity by counting decision points:
  - Calculates complexity for methods, classes, and files
  - Formula: CC = 1 + number of decision points (if, switch, loops, catch, operators)
  - Helps identify complex methods that may need refactoring
- ✅ **Multi-level Analysis** - Calculates LOC and Cyclomatic Complexity at method, class, and file levels; MISRA at method, class, and codebase levels
- ✅ **Recursive Directory Scanning** - Analyzes entire codebases recursively
- ✅ **CSV Export** - Exports metrics to standardized CSV formats
- ✅ **Flexible Metric Selection** - Choose to calculate LOC, CK, MISRA, Cyclomatic Complexity, or any combination
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

📖 **For detailed instructions on collecting metrics, see [COLLECTING_METRICS.md](docs/COLLECTING_METRICS.md)**

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
| `--out` | `-o` | Output directory for the CSV files | `./.metrics` |
| `--no-recursive` | | Disable recursive directory scanning (only applies to `--path`) | `false` |
| `--verbose` | `-v` | Enable verbose logging | `false` |
| `--loc` | | Calculate and export LOC (Lines of Code) metrics | `false` |
| `--ck` | | Calculate and export CK (Chidamber-Kemerer) object-oriented design metrics | `false` |
| `--misra` | | Calculate and export MISRA (Misra's Cognitive Complexity) metrics | `false` |
| `--cyclomatic` | `--cc` | Calculate and export Cyclomatic Complexity metrics | `false` |
| `--help` | `-h`, `-?` | Show help and usage information | |

**Metric Selection:**
- If no metric flags (`--loc`, `--ck`, `--misra`, `--cyclomatic`/`--cc`) are specified, **all metrics are calculated** (default behavior)
- Use `--loc` to calculate only LOC metrics
- Use `--ck` to calculate only CK metrics
- Use `--misra` to calculate only MISRA metrics
- Use `--cyclomatic` or `--cc` to calculate only Cyclomatic Complexity metrics
- Combine flags to calculate specific metrics: `--loc --ck`, `--loc --cyclomatic`, `--ck --misra --cyclomatic`, etc.

### Examples

**Analyze a directory (all metrics - default):**
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

**Calculate only MISRA metrics:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --misra
```

**Calculate only Cyclomatic Complexity metrics:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --cyclomatic
# or using the short form
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --cc
```

**Calculate specific metric combinations:**
```bash
# LOC and CK metrics
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --loc --ck

# LOC and MISRA metrics
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --loc --misra

# CK and MISRA metrics
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --ck --misra

# All metrics explicitly
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --loc --ck --misra --cyclomatic

# LOC and Cyclomatic Complexity
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --loc --cc
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
- **`metrics_misra.csv`** - Generated when MISRA metrics are calculated
- **`metrics_cyclomatic.csv`** - Generated when Cyclomatic Complexity metrics are calculated

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

### MISRA Metrics Output (`metrics_misra.csv`)

#### CSV Format

```csv
FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CC
```

#### Columns

- **FilePath**: Relative or absolute path to the source file
- **ClassName**: Name of the class (empty for codebase-level entries)
- **MethodName**: Name of the method (empty for class/codebase-level entries)
- **MC**: Method Complexity - Cognitive complexity of the method
- **AC**: Attribute Complexity - Complexity from class attributes (class-level only)
- **CLC**: Class Complexity - Overall complexity of the class (class-level only)
- **CWC**: Coupling Weight for a Class - Complexity from inter-class interactions (class-level only)
- **CC**: Code Complexity - Overall complexity of the codebase (codebase-level only)

#### Output Levels

The CSV includes metrics at three levels:

1. **Method Level**: Individual methods with their MC (Method Complexity)
2. **Class Level**: Aggregated metrics for each class (AC, CLC, CWC)
3. **Codebase Level**: Overall codebase complexity (CC)

#### Example Output

```csv
FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CC
src/Example.cs,ExampleClass,SimpleMethod,3,0,0,0,0
src/Example.cs,ExampleClass,ComplexMethod,8,0,0,0,0
src/Example.cs,ExampleClass,,0,6,25,15,0
,,,0,0,0,0,45
```

**Note:** 
- Method-level entries show MC values only
- Class-level entries show AC, CLC, and CWC values (MC is 0)
- Codebase-level entry shows CC value only (all other fields are empty)

### Cyclomatic Complexity Metrics Output (`metrics_cyclomatic.csv`)

#### CSV Format

```csv
FilePath,ClassName,MethodName,Complexity
```

#### Columns

- **FilePath**: Relative or absolute path to the source file
- **ClassName**: Name of the class (empty for file-level entries)
- **MethodName**: Name of the method or constructor (empty for class/file-level entries)
- **Complexity**: Cyclomatic Complexity value (CC = 1 + number of decision points)

#### Output Levels

The CSV includes metrics at three levels:

1. **Method Level**: Individual methods, constructors, and property accessors with their cyclomatic complexity
2. **Class Level**: Aggregated complexity for each class (sum of all methods in the class)
3. **File Level**: Aggregated complexity for each file (sum of all methods in the file)

#### Example Output

```csv
FilePath,ClassName,MethodName,Complexity
src/Example.cs,ExampleClass,SimpleMethod,1
src/Example.cs,ExampleClass,ComplexMethod,8
src/Example.cs,ExampleClass,ExampleClass,1
src/Example.cs,ExampleClass,,10
src/Example.cs,,,10
```

#### Complexity Threshold Recommendations

- **CC 1-10**: Simple (low risk) - Generally acceptable
- **CC 11-20**: Moderate (medium risk) - Consider reviewing
- **CC 21-50**: Complex (high risk) - Should be refactored
- **CC > 50**: Very complex (very high risk) - Requires immediate refactoring

The tool logs complexity statistics including maximum complexity, average complexity, and counts of methods exceeding thresholds.

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

### MISRA Metrics (Misra's Cognitive Complexity)

MISRA metrics measure cognitive complexity based on Basic Control Structures (BCSs) with assigned weights:

- **MC (Method Complexity)**: Calculated by summing cognitive weights of all BCSs in a method:
  - **Sequence** (sequential statements): Weight = 1
  - **Branch** (if, switch, ternary): Weight = 2
  - **Iteration** (for, while, foreach): Weight = 3
  - **Call** (method invocations): Weight = 2
  
  Example: A method with 2 sequences, 1 branch, and 1 iteration has MC = (2×1) + (1×2) + (1×3) = 7

- **AC (Attribute Complexity)**: Counts and weights class attributes (fields and properties):
  - Simple types (int, string, bool): Weight = 1
  - Complex types (custom classes, collections): Weight = 2

- **CLC (Class Complexity)**: Aggregates class complexity:
  - CLC = AC + Σ(MC for all methods in the class)

- **CWC (Coupling Weight for a Class)**: Measures complexity from external method calls:
  - CWC = Σ(MC_called_method + 2) for all external method calls
  - The "+2" represents the call weight

- **CC (Code Complexity)**: Overall codebase complexity:
  - CC = Σ(CLC for all classes) + Σ(CWC for all classes) / 2
  - Division by 2 avoids double-counting coupling between classes

These metrics help identify:
- Methods with high cognitive complexity (high MC) that may be difficult to understand
- Classes with high overall complexity (high CLC) that may need refactoring
- Codebases with high coupling (high CWC) indicating tight dependencies
- Overall codebase complexity (CC) for project-level assessment

### Cyclomatic Complexity

Cyclomatic Complexity measures code complexity by counting the number of linearly independent paths through a program's source code. The formula is:

**CC = 1 + number of decision points**

Decision points include:
- **Conditional statements**: `if`, `else if`, `else`
- **Switch statements**: `switch`, each `case` label, `default`
- **Loop statements**: `for`, `foreach`, `while`, `do-while`
- **Exception handling**: `catch` clauses
- **Conditional operators**: Ternary operator (`?:`), null-coalescing (`??`)
- **Logical operators**: `&&` (logical AND), `||` (logical OR)

**Calculation:**
- Each method starts with a base complexity of 1
- Each decision point adds 1 to the complexity
- Class-level complexity = sum of all method complexities in the class
- File-level complexity = sum of all method complexities in the file

**Example:**
```csharp
public void Example(int x)  // Base: 1
{
    if (x > 0)              // +1 = 2
    {
        if (x < 10)          // +1 = 3
        {
            // ...
        }
    }
    
    for (int i = 0; i < x; i++)  // +1 = 4
    {
        // ...
    }
}
// Total complexity = 4
```

This metric helps identify:
- Methods with high complexity that may be difficult to test and maintain
- Classes with accumulated complexity that may need refactoring
- Files with high overall complexity indicating potential design issues
- Code that may benefit from simplification or decomposition

## Architecture

The project follows **Simple Layered Architecture** with the following structure:

- **Core/Models**: Data models (LocMetric, CkMetric, MisraMetric, CyclomaticComplexityMetric)
- **Services**: Business logic (file discovery, metrics analysis, LOC calculation, CK metrics calculation, MISRA metrics calculation, Cyclomatic Complexity calculation)
- **Parsers**: File parsing (Roslyn, Solution, Project parsers, CK syntax analysis, MISRA BCS detection, Cyclomatic Complexity decision point detection)
- **Exporters**: Output generation (CSV export for LOC, CK, MISRA, and Cyclomatic Complexity metrics)
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

Target performance: Analyzes repositories up to 100k LOC in under 2 minutes (LOC metrics). CK and MISRA metrics analysis may take additional time depending on codebase complexity. MISRA metrics require detailed syntax analysis and may take 3-5 minutes for 100k LOC codebases. Cyclomatic Complexity metrics analysis is efficient and typically adds minimal overhead, completing in under 3 minutes for 100k LOC codebases.

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]
