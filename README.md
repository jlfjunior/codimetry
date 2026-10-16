# MetricsExporter.Net

A .NET command-line tool designed to extract software metrics from local .NET repositories. This tool calculates **Lines of Code (LOC)** metrics, **Chidamber-Kemerer (CK)** object-oriented design metrics, **Misra's cognitive complexity** metrics (not related to the MISRA C/C++ guidelines), and **Cyclomatic Complexity** metrics to help analyze code quality and maintainability.

## Features

- ✅ **Lines of Code (LOC) Calculation** - Counts effective lines of code excluding comments and empty lines
- ✅ **CK Metrics (Chidamber-Kemerer)** - Calculates six object-oriented design metrics:
  - **WMC** (Weighted Methods per Class) - Method count per class, or sum of their cyclomatic complexity with `--wmc-weight cyclomatic`
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
  - **CC** (Code Complexity, *CoC* in the docs) - Overall complexity of the codebase
- ✅ **Cyclomatic Complexity Metrics** - Measures code complexity by counting decision points:
  - Calculates complexity for methods, classes, and files
  - Formula: CC = 1 + number of decision points (if, switch, loops, catch, operators)
  - Helps identify complex methods that may need refactoring
- ✅ **Multi-level Analysis** - Calculates LOC and Cyclomatic Complexity at method, class, and file levels; MISRA at method, class, and codebase levels
- ✅ **Recursive Directory Scanning** - Analyzes entire codebases recursively
- ✅ **CSV Export** - Exports metrics to standardized CSV formats
- ✅ **Flexible Metric Selection** - Choose to calculate LOC, CK, MISRA, Cyclomatic Complexity, or any combination
- ✅ **Cross-platform** - Runs on Windows, Linux, and macOS (.NET 10+)
- ✅ **Performance Optimized** - Parallel processing for fast analysis

## Installation

### Prerequisites

- .NET 10 SDK or later

### Build from Source

```bash
git clone https://github.com/jlfjunior/net-metrics-collector.git
cd net-metrics-collector
dotnet build src/MetricsExporter.Net/MetricsExporter.Net.csproj
```

## Documentation

| Document | Contents |
|----------|----------|
| [COLLECTING_METRICS.md](docs/COLLECTING_METRICS.md) | Building and running the tool, advanced scenarios, CI, troubleshooting |
| [loc.md](docs/loc.md) | Lines of Code: definition and how lines are counted |
| [cyclomatic.md](docs/cyclomatic.md) | Cyclomatic Complexity: definition, decision points, thresholds |
| [ck.md](docs/ck.md) | CK metrics (WMC, RFC, LCOM, CBO, DIT, NOC): definitions, formulas, C# examples |
| [misra.md](docs/misra.md) | Misra's cognitive complexity metrics (MC, AC, CLC, CWC, CoC): definitions, formulas, C# examples |

Each metric page also has a **"How this tool computes it"** section that describes exactly what the values in the CSV files mean. See [docs/README.md](docs/README.md) for the full documentation index.

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
| `--mc-additive` | | MISRA: compute MC with the additive rule of v1 instead of the composition rule (see [docs/misra.md](docs/misra.md#mc--method-complexity)) | `false` |
| `--ac-type-weights` | | MISRA: compute AC as v1 did, weighting attributes by type and counting `static`/`const` members (see [docs/misra.md](docs/misra.md#ac--attribute-complexity)) | `false` |
| `--wmc-weight` | | CK: weight of each method in WMC, `unit` (c_i = 1) or `cyclomatic` (c_i = V(G)) (see [docs/ck.md](docs/ck.md#wmc--weighted-methods-per-class)) | `unit` |
| `--wmc-exclude-static` | | CK: leave static methods and constructors out of WMC, as v1 did | `false` |
| `--cc-null-operators` | | Cyclomatic: count `??`, `??=` and `?.` as decision points, also in WMC with `--wmc-weight cyclomatic` (see [docs/cyclomatic.md](docs/cyclomatic.md#how-this-tool-computes-it)) | `false` |
| `--cc-legacy-operators` | | Cyclomatic: operator rule of v1 (`??` and `goto` count; `??=`, `?.`, `or`/`and` do not) | `false` |
| `--classes-only` | | Measure only classes, as v1 did; by default records, structs and record structs are measured too (see [docs/README.md](docs/README.md#which-types-are-measured)) | `false` |
| `--cc-include-auto-accessors` | | Cyclomatic: give automatic property accessors a row with V(G) = 1 and add them to the class and file sums, as v1 did (see [docs/cyclomatic.md](docs/cyclomatic.md#how-this-tool-computes-it)) | `false` |
| `--mc-library-call-weight` | | MISRA: weight of a call to a library method in MC (default 0; 2 reproduces v1 for these calls). Calls to the analysed code add 2 + MC of the called method (see [docs/misra.md](docs/misra.md#mc--method-complexity)) | `0` |
| `--mc-lambdas` | | MISRA: code of lambdas and local functions in MC: `full` (a nested layer), `calls` (only their calls), `ignore` (see [docs/misra.md](docs/misra.md#mc--method-complexity)) | `full` |
| `--cwc-library-weight` | | MISRA: weight of a call to a library method or delegate in CWC (default 0; 3 reproduces v1). Calls to other types of the analysed code add 2 + MC of the called method (see [docs/misra.md](docs/misra.md#cwc--coupling-weight-for-a-class)) | `0` |
| `--dit-resolve-framework` | | CK: count the ancestors of a base class from a library too, except `System.Object` (`D : Controller` = 2) (see [docs/ck.md](docs/ck.md#dit--depth-of-inheritance-tree)) | `false` |
| `--dit-count-object` | | CK: count `System.Object` as a level of DIT, as v1 did | `false` |
| `--cbo-bidirectional` | | CK: CBO also counts the types that use the class (CK 1994), each coupled type once (see [docs/ck.md](docs/ck.md#cbo--coupling-between-objects)) | `false` |
| `--cbo-declared-types-only` | | CK: CBO counts only the types of fields, properties and method signatures, and only classes, as v1 did | `false` |
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
FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM,LcomMethods,Kind
```

#### Columns

- **FilePath**: Relative or absolute path to the source file
- **ClassName**: Name of the class
- **WMC**: Weighted Methods per Class (method count, static methods included; sum of V(G) with `--wmc-weight cyclomatic`)
- **DIT**: Depth of Inheritance Tree (declared inheritance edges; `System.Object` does not count, so a class with no explicit base has DIT = 0, and a base class from a library counts 1)
- **NOC**: Number of Children (immediate subclasses count)
- **CBO**: Coupling Between Objects (number of classes coupled to)
- **RFC**: Response For a Class (methods in class + methods called)
- **LCOM**: Lack of Cohesion of Methods (cohesion measure, lower is better)
- **LcomMethods**: Number of methods LCOM is computed over (instance methods, constructors excluded)
- **Kind**: `class`, `record`, `struct` or `record struct`

#### Example Output

```csv
FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM,LcomMethods,Kind
src/Example.cs,ExampleClass,5,1,2,3,15,8,5,class
src/Service.cs,UserService,8,0,0,5,25,12,7,class
```

**Note:** CK metrics are calculated at the class level only (one row per class, record or struct).

### MISRA Metrics Output (`metrics_misra.csv`)

#### CSV Format

```csv
FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CC,Kind
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
- **Kind**: Kind of the type of the row: `class`, `record`, `struct` or `record struct` (empty for the codebase row)

#### Output Levels

The CSV includes metrics at three levels:

1. **Method Level**: Individual methods with their MC (Method Complexity)
2. **Class Level**: Aggregated metrics for each class (AC, CLC, CWC)
3. **Codebase Level**: Overall codebase complexity (CC)

#### Example Output

```csv
FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CC,Kind
src/Example.cs,ExampleClass,SimpleMethod,3,0,0,0,0,class
src/Example.cs,ExampleClass,ComplexMethod,8,0,0,0,0,class
src/Example.cs,ExampleClass,,0,6,25,15,0,class
,,,0,0,0,0,45,
```

**Note:** 
- Method-level entries show MC values only
- Class-level entries show AC, CLC, and CWC values (MC is 0)
- Codebase-level entry shows CC value only (all other fields are empty)

### Cyclomatic Complexity Metrics Output (`metrics_cyclomatic.csv`)

#### CSV Format

```csv
FilePath,ClassName,MethodName,Complexity,ClassKey
```

#### Columns

- **FilePath**: Relative or absolute path to the source file
- **ClassName**: Name of the class (empty for file-level entries)
- **MethodName**: Name of the member: method, constructor, `~Name`, `op_Addition`, `Name.get`, `this[].set`... (empty for class/file-level entries)
- **Complexity**: Cyclomatic Complexity value (CC = 1 + number of decision points)
- **ClassKey**: Identity of the class (project + fully qualified name); partial declarations share it (empty for file-level entries)

#### Output Levels

The CSV includes metrics at three levels:

1. **Member Level**: methods, constructors, destructors, operators and accessors with a body; automatic accessors are left out
2. **Class Level**: Aggregated complexity for each class (sum of its members, joining partial declarations)
3. **File Level**: Aggregated complexity for each file (sum of the members declared in the file)

#### Example Output

```csv
FilePath,ClassName,MethodName,Complexity,ClassKey
src/Example.cs,ExampleClass,SimpleMethod,1,App|Demo.ExampleClass
src/Example.cs,ExampleClass,ComplexMethod,8,App|Demo.ExampleClass
src/Example.cs,ExampleClass,ExampleClass,1,App|Demo.ExampleClass
src/Example.cs,ExampleClass,,10,App|Demo.ExampleClass
src/Example.cs,,,10,
```

#### Complexity Thresholds

See [cyclomatic.md](docs/cyclomatic.md#interpretation) for reference thresholds and their sources. The tool logs complexity statistics, including the maximum and average complexity and the number of methods above each threshold.

## How Metrics are Calculated

| Metric | In one line | Details |
|--------|-------------|---------|
| LOC | Non-blank, non-comment lines of each method and constructor. Classes and files sum their methods | [loc.md](docs/loc.md) |
| Cyclomatic Complexity | 1 + number of decision points (`if`, `case`, loops, `catch`, `?:`, `&&`, `\|\|`, `or`/`and`…) | [cyclomatic.md](docs/cyclomatic.md) |
| WMC, RFC, LCOM, CBO, DIT, NOC | Chidamber & Kemerer object-oriented design metrics | [ck.md](docs/ck.md) |
| MC, AC, CLC, CWC, CC | Misra et al. cognitive complexity metrics, using Basic Control Structure weights | [misra.md](docs/misra.md) |

The tool's implementation differs from the textbook definitions in some places, for example in how WMC weights methods or how MC treats nesting. Each page documents these differences, so read the "How this tool computes it" section before comparing values with other tools or with published studies.

## Running Tests

The test suite lives in `tests/MetricsExporter.Net.Tests` (xUnit). It covers the parsers, metric calculators, analysis services, CSV exporter, and CLI. It also checks every worked example in `docs/*.md`, so the documentation and the implementation cannot drift apart unnoticed.

```bash
dotnet test

# with line coverage (Cobertura XML under TestResults/)
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture

The project follows **Simple Layered Architecture** with the following structure:

- **Core/Models**: Data models (LocMetric, CkMetric, MisraMetric, CyclomaticComplexityMetric)
- **Services**: Business logic (file discovery, metrics analysis, LOC calculation, CK metrics calculation, MISRA metrics calculation, Cyclomatic Complexity calculation)
- **Parsers**: File parsing (Roslyn, Solution, Project parsers, CK syntax analysis, MISRA BCS detection, Cyclomatic Complexity decision point detection)
- **Exporters**: Output generation (CSV export for LOC, CK, MISRA, and Cyclomatic Complexity metrics)
- **CLI**: Command-line interface

## Requirements

- .NET 10 or later
- Windows, Linux, or macOS

## Performance

The tool is optimized for performance:
- Parallel file processing
- Efficient Roslyn syntax tree analysis
- Memory-efficient processing
- Two-phase CK metrics analysis (relationship graph building, then metric calculation)

Target performance: Analyzes repositories up to 100k LOC in under 2 minutes (LOC metrics). CK and MISRA metrics analysis may take additional time depending on codebase complexity. MISRA metrics require detailed syntax analysis and may take 3-5 minutes for 100k LOC codebases. Cyclomatic Complexity metrics analysis is efficient and typically adds minimal overhead, completing in under 3 minutes for 100k LOC codebases.

## License

This project is licensed under the [MIT License](LICENSE).

## Contributing

[Add contribution guidelines here]
