#!/usr/bin/env bash
# Collects the metrics of every participant with the tool as of one commit, then runs the CBTAM analysis.
#
#   scripts/collect_snapshot.sh <commit> <results-dir> <analysis-dir> [tool options...]
#
# The tool is built in a temporary worktree of <commit>. The collection runs from the repository root with
# --path ../artefatos-mestrado/<p>/, as in the v1 protocol, so that FilePath keeps the same form and classes
# pair across collections. The console log of each participant goes to <results-dir>/<p>/run.log.
# Requires the artefacts restored (dotnet restore), see docs/research/protocolo-recoleta.md, Fase 3.
set -euo pipefail

commit="$1"
results="$2"
analysis="$3"
shift 3

root="$(git rev-parse --show-toplevel)"
artefatos="../artefatos-mestrado"
worktree="$(mktemp -d)/tool"

cd "$root"
git worktree add --detach --quiet "$worktree" "$commit"
trap 'git worktree remove --force "$worktree"' EXIT

# The tool was called MetricsExporter.Net before it became Codimetry; older commits keep the old name
project="$worktree/src/Codimetry/Codimetry.csproj"
[ -f "$project" ] || project="$worktree/src/MetricsExporter.Net/MetricsExporter.Net.csproj"
dotnet build "$project" -c Release --nologo -v quiet -o "$worktree/out" >/dev/null
dll="$worktree/out/$(basename "$project" .csproj).dll"

mkdir -p "$results"
for dir in "$artefatos"/p*/; do
  name="$(basename "$dir")"
  mkdir -p "$results/$name"
  dotnet "$dll" --path "$dir" --out "$results/$name" "$@" > "$results/$name/run.log" 2>&1
done

{
  echo "commit: $(git rev-parse "$commit")"
  echo "artefatos: $(git -C "$artefatos" rev-parse HEAD)"
  echo "sdk: $(dotnet --version)"
  echo "options: $*"
  echo "date: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
} > "$results/collection.txt"

python3 scripts/analysis/cbtam_analysis.py --results "$results" --out "$analysis"
