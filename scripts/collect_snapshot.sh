#!/usr/bin/env bash
# Collects the metrics of every participant with the tool as of one commit, then runs the CBTAM analysis.
#
#   PARTICIPANTS_DIR=<dir> scripts/collect_snapshot.sh <commit> <results-dir> <analysis-dir> [tool options...]
#
# PARTICIPANTS_DIR holds one folder per participant, named p{n}-{position}-{level}.
#
# The tool is built in a temporary worktree of <commit>. The collection runs from the repository root with
# --path $PARTICIPANTS_DIR/<p>/; give PARTICIPANTS_DIR relative to the repository root, as earlier collections
# did, so that FilePath keeps the same form and classes pair across collections. The console log of each
# participant goes to <results-dir>/<p>/run.log.
# Requires the participants' projects restored (dotnet restore in each participant folder).
set -euo pipefail

commit="$1"
results="$2"
analysis="$3"
shift 3

root="$(git rev-parse --show-toplevel)"
participants="${PARTICIPANTS_DIR:?set PARTICIPANTS_DIR to the folder of the participants}"
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
for dir in "$participants"/p*/; do
  name="$(basename "$dir")"
  mkdir -p "$results/$name"
  dotnet "$dll" --path "$dir" --out "$results/$name" "$@" > "$results/$name/run.log" 2>&1
done

{
  echo "commit: $(git rev-parse "$commit^{commit}")"
  echo "artefatos: $(git -C "$participants" rev-parse HEAD)"
  echo "sdk: $(dotnet --version)"
  echo "options: $*"
  echo "date: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
} > "$results/collection.txt"

python3 scripts/analysis/cbtam_analysis.py --results "$results" --out "$analysis"
