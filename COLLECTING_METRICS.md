# Collecting Metrics with MetricsExporter.Net

This guide provides step-by-step instructions on how to collect Lines of Code (LOC) metrics from your .NET codebase using MetricsExporter.Net.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Building the Tool](#building-the-tool)
3. [Basic Usage](#basic-usage)
4. [Command-Line Options](#command-line-options)
5. [Usage Examples](#usage-examples)
6. [Understanding the Output](#understanding-the-output)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)
9. [Advanced Scenarios](#advanced-scenarios)

---

## Prerequisites

Before collecting metrics, ensure you have:

- **.NET 9.0 SDK** or later installed
  - Verify installation: `dotnet --version`
- **Access** to the source code directory you want to analyze
- **Read permissions** for the target directory and its files

---

## Building the Tool

### Option 1: Build from Source

```bash
# Clone or navigate to the repository
cd metrics-exporter-net

# Build the project
dotnet build src/MetricsExporter.Net/MetricsExporter.Net.csproj
```

### Option 2: Build Release Version

```bash
dotnet build src/MetricsExporter.Net/MetricsExporter.Net.csproj --configuration Release
```

The compiled executable will be located at:
- Debug: `src/MetricsExporter.Net/bin/Debug/net9.0/MetricsExporter.Net`
- Release: `src/MetricsExporter.Net/bin/Release/net9.0/MetricsExporter.Net`

---

## Basic Usage

### Quick Start

You can analyze code using one of three input options:

1. **`--path`** (`-p`): Analyze a directory containing .NET source code
2. **`--solution`** (`-s`): Analyze a .sln solution file
3. **`--project`**: Analyze a .csproj project file

**Note:** Only one of these options can be specified at a time.

**Example - Directory:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src
```

**Example - Solution file:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --solution MySolution.sln
```

**Example - Project file:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --project ./src/MyProject/MyProject.csproj
```

**Example - Get help:**
```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --help
# or using the short form
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- -h
```

This will:
- Discover all `.cs` files (from directory, solution, or project)
- Calculate LOC metrics
- Export results to `./metrics/metrics_loc.csv`

---

## Command-Line Options

### Required Options (Choose One)

| Option | Short | Description |
|--------|-------|-------------|
| `--path` | `-p` | Path to a directory containing .NET source code to analyze |
| `--solution` | `-s` | Path to a .sln solution file to analyze |
| `--project` | | Path to a .csproj project file to analyze |

**Note:** Exactly one of `--path`, `--solution`, or `--project` must be specified.

### Optional Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--out` | `-o` | Output directory for the CSV file | `./metrics` |
| `--no-recursive` | | Disable recursive directory scanning (only applies to directory paths) | `false` (recursive enabled) |
| `--verbose` | `-v` | Enable verbose logging for debugging | `false` |
| `--help` | `-h`, `-?` | Show help and usage information | |

---

## Usage Examples

### Example 1: Analyze a Directory

Analyze all C# files in a directory:

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path /path/to/your/project/src
```

**Output:** `./metrics/metrics_loc.csv`

### Example 1a: Analyze a Solution File

Analyze all projects in a solution:

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --solution MySolution.sln
```

**Output:** `./metrics/metrics_loc.csv`

### Example 1b: Analyze a Project File

Analyze a specific project:

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --project ./src/MyProject/MyProject.csproj
```

**Output:** `./metrics/metrics_loc.csv`

### Example 2: Get Help Information

View all available command-line options:

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --help
```

Or using the short form:

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- -h
```

This displays:
- All available command-line options
- Option descriptions
- Default values
- Usage examples

### Example 3: Custom Output Directory

Specify where to save the results:

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --out ./analysis-results
```

**Output:** `./analysis-results/metrics_loc.csv`

### Example 4: Non-Recursive Analysis

Analyze only files in the root directory (no subdirectories):

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --no-recursive
```

### Example 5: Verbose Logging

Enable detailed logging to see progress and debug issues:

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --verbose
```

**Verbose output includes:**
- Each file being analyzed
- Number of metrics extracted per file
- Detailed error messages

### Example 6: Complete Example with All Options

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path /Users/john/projects/myapp/src \
  --out /Users/john/projects/myapp/metrics \
  --verbose
```

### Example 7: Using the Compiled Executable

If you've built the Release version:

```bash
./src/MetricsExporter.Net/bin/Release/net9.0/MetricsExporter.Net \
  --path ./src \
  --out ./results
```

---

## Understanding the Output

### CSV File Structure

The tool generates a `metrics_loc.csv` file with the following columns:

| Column | Description | Example |
|--------|-------------|---------|
| `FilePath` | Path to the source file | `src/Controllers/UserController.cs` |
| `ClassName` | Name of the class (empty for file-level) | `UserController` |
| `MethodName` | Name of the method/constructor (empty for class/file-level) | `GetUser` |
| `LOC` | Lines of Code count | `15` |

### Output Levels

The CSV contains metrics at **three hierarchical levels**:

#### 1. Method Level
Individual methods and constructors with their LOC:

```csv
FilePath,ClassName,MethodName,LOC
src/UserService.cs,UserService,UserService,3
src/UserService.cs,UserService,GetUser,12
src/UserService.cs,UserService,CreateUser,18
```

#### 2. Class Level
Aggregated LOC for each class (sum of all methods in the class):

```csv
FilePath,ClassName,MethodName,LOC
src/UserService.cs,UserService,,33
```

#### 3. File Level
Aggregated LOC for each file (sum of all classes in the file):

```csv
FilePath,ClassName,MethodName,LOC
src/UserService.cs,,,33
```

### Complete Example Output

```csv
FilePath,ClassName,MethodName,LOC
src/Controllers/UserController.cs,UserController,UserController,4
src/Controllers/UserController.cs,UserController,Get,8
src/Controllers/UserController.cs,UserController,Post,12
src/Controllers/UserController.cs,UserController,Delete,6
src/Controllers/UserController.cs,UserController,,30
src/Services/UserService.cs,UserService,UserService,3
src/Services/UserService.cs,UserService,GetUser,15
src/Services/UserService.cs,UserService,CreateUser,20
src/Services/UserService.cs,UserService,,38
src/Controllers/UserController.cs,,,30
src/Services/UserService.cs,,,38
```

### How LOC is Calculated

The tool counts **effective lines of code** by:

✅ **Including:**
- Executable statements
- Variable declarations
- Control flow statements (if, for, while, etc.)
- Return statements
- Method calls

❌ **Excluding:**
- Single-line comments (`// comment`)
- Multi-line comments (`/* comment */`)
- XML documentation comments (`/// <summary>`)
- Empty lines (whitespace only)
- Using statements (at file level)
- Namespace declarations
- Class/method signatures (only body is counted)

---

## Best Practices

### 1. Organize Your Analysis

Create a dedicated directory for metric results:

```bash
mkdir -p metrics-reports
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --out ./metrics-reports/$(date +%Y%m%d)
```

### 2. Use Version Control

Track metrics over time by including timestamps:

```bash
OUTPUT_DIR="./metrics/$(date +%Y-%m-%d_%H-%M-%S)"
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --out "$OUTPUT_DIR"
```

### 3. Analyze Specific Components

For large codebases, analyze components separately:

```bash
# Analyze only controllers
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src/Controllers \
  --out ./metrics/controllers

# Analyze only services
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src/Services \
  --out ./metrics/services
```

### 4. Use Verbose Mode for Debugging

When encountering issues, use verbose mode:

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --verbose 2>&1 | tee analysis.log
```

### 5. Validate Large Codebases

For repositories with 100k+ LOC, monitor performance:

```bash
time dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./large-codebase \
  --out ./metrics
```

---

## Troubleshooting

### Issue: "Directory not found"

**Error:**
```
Directory not found: /path/to/directory
```

**Solution:**
- Verify the path exists: `ls -la /path/to/directory`
- Use absolute paths: `--path /absolute/path/to/directory`
- Check for typos in the path

### Issue: "Access denied"

**Error:**
```
Access denied: /path/to/directory
```

**Solution:**
- Check file permissions: `chmod -R +r /path/to/directory`
- Run with appropriate user permissions
- Verify you have read access to all subdirectories

### Issue: "No metrics found"

**Possible Causes:**
1. No `.cs` files in the directory
2. All files have parse errors
3. Directory is empty

**Solution:**
- Verify `.cs` files exist: `find ./src -name "*.cs" | head -5`
- Check for syntax errors in your code
- Use `--verbose` to see which files are being processed

### Issue: Parse Errors

**Symptom:** Some files are skipped with warnings

**Solution:**
- The tool continues processing other files even if some fail
- Check the verbose log for specific parse errors
- Fix syntax errors in the problematic files
- Re-run the analysis

### Issue: Performance Issues

**Symptom:** Analysis takes too long

**Solutions:**
1. Use non-recursive mode for specific directories:
   ```bash
   --path ./src/Controllers --no-recursive
   ```

2. Analyze smaller subsets:
   ```bash
   --path ./src/Module1
   ```

3. Ensure you're using Release build:
   ```bash
   dotnet build --configuration Release
   ```

---

## Advanced Scenarios

### Scenario 1: Analyzing Multiple Projects

Create a script to analyze multiple projects:

```bash
#!/bin/bash
PROJECTS=("ProjectA" "ProjectB" "ProjectC")
BASE_DIR="./src"

for project in "${PROJECTS[@]}"; do
  echo "Analyzing $project..."
  dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
    --path "$BASE_DIR/$project" \
    --out "./metrics/$project"
done
```

### Scenario 2: Comparing Metrics Over Time

```bash
# Baseline
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --out ./metrics/baseline

# After changes
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --out ./metrics/current

# Compare (using diff or a spreadsheet tool)
diff ./metrics/baseline/metrics_loc.csv ./metrics/current/metrics_loc.csv
```

### Scenario 3: CI/CD Integration

Example GitHub Actions workflow:

```yaml
name: Collect Metrics

on:
  push:
    branches: [ main ]

jobs:
  collect-metrics:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Build MetricsExporter
        run: dotnet build src/MetricsExporter.Net/MetricsExporter.Net.csproj
      
      - name: Collect Metrics
        run: |
          dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
            --path ./src \
            --out ./metrics
      
      - name: Upload Metrics
        uses: actions/upload-artifact@v3
        with:
          name: metrics
          path: ./metrics/metrics_loc.csv
```

### Scenario 4: Filtering Results

After collecting metrics, you can filter the CSV:

```bash
# Get only method-level metrics
grep -v ',,,' metrics_loc.csv | grep -v ',,' > methods_only.csv

# Get metrics for a specific class
grep "UserService" metrics_loc.csv > userservice_metrics.csv

# Get files with LOC > 100
awk -F',' '$4 > 100 && $2 == "" && $3 == ""' metrics_loc.csv
```

### Scenario 5: Generating Reports

Use the CSV with analysis tools:

**Python Example:**
```python
import pandas as pd

# Load metrics
df = pd.read_csv('metrics_loc.csv')

# Total LOC
total_loc = df[df['MethodName'] == ''].groupby('FilePath')['LOC'].sum()
print(f"Total LOC: {total_loc.sum()}")

# Top 10 largest classes
class_metrics = df[(df['ClassName'] != '') & (df['MethodName'] == '')]
top_classes = class_metrics.nlargest(10, 'LOC')
print(top_classes)
```

---

## Quick Reference

### Most Common Commands

```bash
# Get help information
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --help
# or
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- -h

# Analyze directory
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src

# Analyze solution file
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --solution MySolution.sln

# Analyze project file
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --project ./src/MyProject/MyProject.csproj

# With custom output
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --out ./results

# Verbose mode
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --verbose

# Non-recursive (directory only)
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --no-recursive
```

---

## Next Steps

After collecting metrics, you can:

1. **Analyze the CSV** using spreadsheet tools (Excel, Google Sheets)
2. **Create visualizations** with Python, R, or BI tools
3. **Track trends** over time by collecting metrics regularly
4. **Set thresholds** for code quality gates
5. **Identify refactoring opportunities** (large classes/methods)

---

## Support

For issues or questions:
- Check the [README.md](README.md) for general information
- Review the [PRD.md](PRD.md) for requirements and specifications
- Use `--verbose` flag for detailed debugging information

---

**Last Updated:** 2024

