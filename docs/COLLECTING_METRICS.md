# Collecting Metrics with MetricsExporter.Net

A practical guide for building and running MetricsExporter.Net, with examples, advanced scenarios, and troubleshooting tips. For a full CLI reference and metric definitions, see [README.md](../README.md).

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Building the Tool](#building-the-tool)
3. [Running the Tool](#running-the-tool)
4. [Advanced Scenarios](#advanced-scenarios)
5. [Best Practices](#best-practices)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- **.NET 9.0 SDK** or later — verify with `dotnet --version`
- Read access to the source code directory you want to analyze

---

## Building the Tool

### Debug build

```bash
dotnet build src/MetricsExporter.Net/MetricsExporter.Net.csproj
```

### Release build (recommended for large codebases)

```bash
dotnet build src/MetricsExporter.Net/MetricsExporter.Net.csproj --configuration Release
```

Compiled binaries:
- Debug: `src/MetricsExporter.Net/bin/Debug/net9.0/MetricsExporter.Net`
- Release: `src/MetricsExporter.Net/bin/Release/net9.0/MetricsExporter.Net`

---

## Running the Tool

### All metrics (default)

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src
```

Generates all four CSV files in `./.metrics/`.

### Specific metrics

```bash
# LOC only
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --loc

# CK metrics only
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --ck

# MISRA only
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --misra

# Cyclomatic Complexity only
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --cyclomatic

# Combine as needed
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --loc --ck
```

### Analyze a solution or project file

```bash
# Solution file
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --solution MySolution.sln

# Specific project
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --project ./src/MyProject/MyProject.csproj
```

### Custom output directory with verbose logging

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --out ./results \
  --verbose
```

### Using the compiled executable

```bash
./src/MetricsExporter.Net/bin/Release/net9.0/MetricsExporter.Net --path ./src --out ./results
```

---

## Advanced Scenarios

### Timestamped output for tracking over time

```bash
OUTPUT_DIR="./.metrics/$(date +%Y-%m-%d_%H-%M-%S)"
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src \
  --out "$OUTPUT_DIR"
```

### Analyze multiple projects in a loop

```bash
#!/bin/bash
for project in ProjectA ProjectB ProjectC; do
  dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
    --path "./src/$project" \
    --out "./.metrics/$project"
done
```

### Compare metrics before and after a change

```bash
# Baseline
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src --out ./.metrics/baseline

# After changes
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
  --path ./src --out ./.metrics/current

diff ./.metrics/baseline/metrics_loc.csv ./.metrics/current/metrics_loc.csv
```

### CI/CD integration (GitHub Actions)

```yaml
name: Collect Metrics

on:
  push:
    branches: [main]

jobs:
  collect-metrics:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Collect Metrics
        run: |
          dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- \
            --path ./src \
            --out ./.metrics

      - name: Upload Metrics
        uses: actions/upload-artifact@v3
        with:
          name: metrics
          path: ./.metrics/
```

### Filtering CSV output

```bash
# Methods with Cyclomatic Complexity > 10
awk -F',' '$3 != "" && $4 > 10' .metrics/metrics_cyclomatic.csv

# Specific class across all metric files
grep "UserService" .metrics/metrics_ck.csv

# Files with LOC > 100
awk -F',' '$2 == "" && $3 == "" && $4 > 100' .metrics/metrics_loc.csv
```

### Analyzing results with Python

```python
import pandas as pd

loc = pd.read_csv('.metrics/metrics_loc.csv')
ck  = pd.read_csv('.metrics/metrics_ck.csv')

# Top 10 classes by LOC
class_loc = loc[(loc['ClassName'] != '') & (loc['MethodName'] == '')]
print(class_loc.nlargest(10, 'LOC')[['FilePath', 'ClassName', 'LOC']])

# Classes with high coupling (CBO > 10)
print(ck[ck['CBO'] > 10][['FilePath', 'ClassName', 'CBO', 'LCOM']])
```

---

## Best Practices

- **Use Release build** for large codebases (100k+ LOC) — noticeably faster.
- **Analyze specific modules** rather than the full repo when you only care about one area.
- **Store outputs in version control** (or an artifact store) to track metric trends over time.
- **Combine verbose mode with `tee`** to capture logs: `--verbose 2>&1 | tee analysis.log`
- **Start with `--loc`** to verify the tool runs correctly before adding heavier metrics like CK or MISRA.

---

## Troubleshooting

### "Directory not found"

Verify the path exists and use an absolute path if needed:
```bash
ls -la /path/to/directory
--path /absolute/path/to/directory
```

### "Access denied"

Check read permissions:
```bash
chmod -R +r /path/to/directory
```

### "No metrics found"

1. Confirm `.cs` files exist: `find ./src -name "*.cs" | head -5`
2. Run with `--verbose` to see which files are being processed and any parse warnings.

### Slow performance

1. Use the Release build.
2. Use `--no-recursive` to limit scope.
3. Calculate only the metrics you need (e.g., `--loc` instead of all metrics).
4. Analyze smaller directory subsets.
