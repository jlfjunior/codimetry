# Instructions — Collecting Metrics for Artefatos Mestrado

This document explains how to re-run the full metrics collection process for all participants in `artefatos-mestrado` and regenerate the `results/index.md` summary.

## Prerequisites

- .NET 10.0 SDK installed (`dotnet --version`)
- `artefatos-mestrado` repository cloned at `../artefatos-mestrado` relative to this repo
- Participant folders follow the pattern `p{number}-{position}-{level}` (e.g., `p1-dev-pl`)

## Step 1 — Build the Collector

From the root of this repository:

```bash
dotnet build src/MetricsExporter.Net/MetricsExporter.Net.csproj -c Release
```

## Step 2 — Run Metrics on All Participants

This script collects all metrics and outputs each participant's CSVs to `results/{participant}/`:

```bash
COLLECTOR="$(pwd)/src/MetricsExporter.Net/MetricsExporter.Net.csproj"
ARTEFATOS="../artefatos-mestrado"
OUTPUT_BASE="$(pwd)/results"

for attended_dir in "$ARTEFATOS"/p*/; do
  name=$(basename "$attended_dir")
  out_dir="$OUTPUT_BASE/$name"
  mkdir -p "$out_dir"
  echo "Processing $name..."
  dotnet run --project "$COLLECTOR" -c Release --no-build -- \
    --path "$attended_dir" \
    --out "$out_dir"
done

echo "Done. Results saved to $OUTPUT_BASE"
```

Each participant folder will contain:
- `metrics_loc.csv` — Lines of Code at method, class, and file levels
- `metrics_ck.csv` — CK metrics (WMC, DIT, NOC, CBO, RFC, LCOM) per class
- `metrics_misra.csv` — MISRA metrics (MC, AC, CLC, CWC, CC) at method/class/codebase levels
- `metrics_cyclomatic.csv` — Cyclomatic complexity at method, class, and file levels

## Step 3 — Regenerate index.md

After collecting metrics, run the following script to rebuild `results/index.md`:

```bash
OUTPUT_BASE="$(pwd)/results"

{
  echo "# Metrics Summary — Artefatos Mestrado"
  echo ""
  echo "> Generated: $(date +%Y-%m-%d)"
  echo "> Tool: net-metrics-collector"
  echo "> Participants: $(ls -d $OUTPUT_BASE/p*/ | wc -l | tr -d ' ')"
  echo ""
  echo "---"
  echo ""
  echo "## Overview Table"
  echo ""
  echo "| Participant | Position | Level | LOC | Classes | Avg WMC | Avg CBO | Avg LCOM | MISRA CC | Cyclomatic |"
  echo "|-------------|----------|-------|----:|--------:|--------:|--------:|---------:|---------:|-----------:|"

  for attended in $(ls "$OUTPUT_BASE" | grep -E "^p[0-9]+-" | sort -V); do
    dir="$OUTPUT_BASE/$attended"
    num=$(echo $attended | grep -oE "^p[0-9]+" | tr -d 'p')
    position=$(echo $attended | cut -d'-' -f2)
    level=$(echo $attended | cut -d'-' -f3)

    loc=$(awk -F',' 'NR>1 && $2=="" && $3=="" {sum+=$4} END {print sum+0}' "$dir/metrics_loc.csv" 2>/dev/null)
    classes=$(awk -F',' 'NR>1 {c++} END {print c+0}' "$dir/metrics_ck.csv" 2>/dev/null)
    avg_wmc=$(awk -F',' 'NR>1 {c++; s+=$3} END {if(c>0) printf "%.1f", s/c; else print "0"}' "$dir/metrics_ck.csv" 2>/dev/null)
    avg_cbo=$(awk -F',' 'NR>1 {c++; s+=$6} END {if(c>0) printf "%.1f", s/c; else print "0"}' "$dir/metrics_ck.csv" 2>/dev/null)
    avg_lcom=$(awk -F',' 'NR>1 {c++; s+=$8} END {if(c>0) printf "%.1f", s/c; else print "0"}' "$dir/metrics_ck.csv" 2>/dev/null)
    misra_cc=$(awk -F',' 'NR>1 && $8!="" && $8!="0" {cc=$8} END {print cc+0}' "$dir/metrics_misra.csv" 2>/dev/null)
    cyclomatic=$(awk -F',' 'NR>1 && $2=="" && $3=="" {sum+=$4} END {print sum+0}' "$dir/metrics_cyclomatic.csv" 2>/dev/null)

    echo "| p$num | $position | $level | $loc | $classes | $avg_wmc | $avg_cbo | $avg_lcom | $misra_cc | $cyclomatic |"
  done

  echo ""
  echo "---"
  echo ""
  echo "## Metric Definitions"
  echo ""
  echo "| Metric | Description |"
  echo "|--------|-------------|"
  echo "| **LOC** | Effective lines of code (excludes comments and blank lines) |"
  echo "| **WMC** | Weighted Methods per Class — method count per class |"
  echo "| **CBO** | Coupling Between Objects — number of coupled classes (lower = better) |"
  echo "| **LCOM** | Lack of Cohesion of Methods — lower = better cohesion |"
  echo "| **MISRA CC** | Overall codebase cognitive complexity |"
  echo "| **Cyclomatic** | Total cyclomatic complexity (1 + decision points per method, summed) |"

} > "$OUTPUT_BASE/index.md"

echo "index.md generated at $OUTPUT_BASE/index.md"
```

## Running Everything at Once

Steps 2 and 3 combined as a single script:

```bash
#!/usr/bin/env bash
set -e

COLLECTOR="$(pwd)/src/MetricsExporter.Net/MetricsExporter.Net.csproj"
ARTEFATOS="../artefatos-mestrado"
OUTPUT_BASE="$(pwd)/results"

# Build
dotnet build "$COLLECTOR" -c Release --nologo -q

# Collect metrics
for attended_dir in "$ARTEFATOS"/p*/; do
  name=$(basename "$attended_dir")
  out_dir="$OUTPUT_BASE/$name"
  mkdir -p "$out_dir"
  echo "Processing $name..."
  dotnet run --project "$COLLECTOR" -c Release --no-build -- \
    --path "$attended_dir" \
    --out "$out_dir"
done

# Generate index.md
{
  echo "# Metrics Summary — Artefatos Mestrado"
  echo ""
  echo "> Generated: $(date +%Y-%m-%d)"
  echo "> Tool: net-metrics-collector"
  echo "> Participants: $(ls -d $OUTPUT_BASE/p*/ | wc -l | tr -d ' ')"
  echo ""
  echo "---"
  echo ""
  echo "## Overview Table"
  echo ""
  echo "| Participant | Position | Level | LOC | Classes | Avg WMC | Avg CBO | Avg LCOM | MISRA CC | Cyclomatic |"
  echo "|-------------|----------|-------|----:|--------:|--------:|--------:|---------:|---------:|-----------:|"

  for attended in $(ls "$OUTPUT_BASE" | grep -E "^p[0-9]+-" | sort -V); do
    dir="$OUTPUT_BASE/$attended"
    num=$(echo $attended | grep -oE "^p[0-9]+" | tr -d 'p')
    position=$(echo $attended | cut -d'-' -f2)
    level=$(echo $attended | cut -d'-' -f3)

    loc=$(awk -F',' 'NR>1 && $2=="" && $3=="" {sum+=$4} END {print sum+0}' "$dir/metrics_loc.csv" 2>/dev/null)
    classes=$(awk -F',' 'NR>1 {c++} END {print c+0}' "$dir/metrics_ck.csv" 2>/dev/null)
    avg_wmc=$(awk -F',' 'NR>1 {c++; s+=$3} END {if(c>0) printf "%.1f", s/c; else print "0"}' "$dir/metrics_ck.csv" 2>/dev/null)
    avg_cbo=$(awk -F',' 'NR>1 {c++; s+=$6} END {if(c>0) printf "%.1f", s/c; else print "0"}' "$dir/metrics_ck.csv" 2>/dev/null)
    avg_lcom=$(awk -F',' 'NR>1 {c++; s+=$8} END {if(c>0) printf "%.1f", s/c; else print "0"}' "$dir/metrics_ck.csv" 2>/dev/null)
    misra_cc=$(awk -F',' 'NR>1 && $8!="" && $8!="0" {cc=$8} END {print cc+0}' "$dir/metrics_misra.csv" 2>/dev/null)
    cyclomatic=$(awk -F',' 'NR>1 && $2=="" && $3=="" {sum+=$4} END {print sum+0}' "$dir/metrics_cyclomatic.csv" 2>/dev/null)

    echo "| p$num | $position | $level | $loc | $classes | $avg_wmc | $avg_cbo | $avg_lcom | $misra_cc | $cyclomatic |"
  done

  echo ""
  echo "---"
  echo ""
  echo "## Metric Definitions"
  echo ""
  echo "| Metric | Description |"
  echo "|--------|-------------|"
  echo "| **LOC** | Effective lines of code (excludes comments and blank lines) |"
  echo "| **WMC** | Weighted Methods per Class — method count per class |"
  echo "| **CBO** | Coupling Between Objects — number of coupled classes (lower = better) |"
  echo "| **LCOM** | Lack of Cohesion of Methods — lower = better cohesion |"
  echo "| **MISRA CC** | Overall codebase cognitive complexity |"
  echo "| **Cyclomatic** | Total cyclomatic complexity (1 + decision points per method, summed) |"

} > "$OUTPUT_BASE/index.md"

echo "All done. Results at $OUTPUT_BASE"
```

## Results Structure

```
results/
├── index.md              ← summary of all participants
├── p1-dev-pl/
│   ├── metrics_loc.csv
│   ├── metrics_ck.csv
│   ├── metrics_misra.csv
│   └── metrics_cyclomatic.csv
├── p2-arch-esp/
│   └── ...
└── p29-arch-sn/
    └── ...
```
