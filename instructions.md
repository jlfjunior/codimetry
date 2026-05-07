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

After collecting metrics, run the Python script to rebuild `results/index.md`:

```bash
python3 scripts/generate_index.py
```

The script is at `scripts/generate_index.py` and produces all sections:
- Overview Table
- Summary by Level and by Position
- LOC Distribution
- Complexity Highlights (top MISRA CC, Cyclomatic, LCOM)
- Metric Correlation Matrix (Pearson, n=participants)
- CK Metrics table (avg/min/max per class per participant)
- MISRA Metrics table (all metrics: MC, AC, CLC, CWC, CC with min/max)
- Metric Definitions

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
python3 scripts/generate_index.py

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
