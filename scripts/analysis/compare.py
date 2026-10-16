#!/usr/bin/env python3
"""Compares two or more outputs of cbtam_analysis.py, in order (waterfall).

Usage:
    python3 scripts/analysis/compare.py analysis/v1 analysis/01-x analysis/02-y [--out comparison.md]

For each step it reports the headline numbers, how many classes changed in each metric
relative to the previous step (classes paired by participant + file + class), and which
participants changed predicted level.
"""
import argparse
import csv
import json
import os

METRICS = ["WMC", "DIT", "NOC", "CBO", "RFC", "LCOM", "AC", "CLC", "CWC"]
LEVELS = ["Beginner", "Mid-Level", "Senior", "Expert"]


def load(directory):
    with open(os.path.join(directory, "summary.json")) as f:
        summary = json.load(f)
    with open(os.path.join(directory, "per_class.csv"), newline="") as f:
        classes = {(r["pid"], r["file"], r["class"]): r for r in csv.DictReader(f)}
    with open(os.path.join(directory, "per_participant_results.csv"), newline="") as f:
        participants = {r["pid"]: r for r in csv.DictReader(f)}
    return {"dir": directory, "summary": summary, "classes": classes, "participants": participants}


def f(value, digits=1):
    return f"{value:.{digits}f}".replace(".", ",")


def headline(steps):
    rows = [
        ("Classes", lambda s: str(s["classes"])),
        ("Métodos (MC)", lambda s: str(s["methods"])),
        ("LCOM = n(n−1)/2 (%)", lambda s: f(s["lcom_equals_max_pairs_pct"], 2)),
        ("Acurácia (%)", lambda s: f(s["accuracy_pct"])),
        ("Linha de base (%)", lambda s: f(s["baseline_pct"])),
        ("ρ regra em estágios", lambda s: f(s["spearman_staged"], 3)),
        ("ρ média do % Normal", lambda s: f(s["spearman_mean_pct"], 3)),
        ("Erros (adjacentes / ≥ 2)", lambda s: f"{s['errors']['total']} ({s['errors']['adjacent']} / {s['errors']['two_or_more']})"),
    ]
    for metric in ["MC", "AC", "CWC", "LCOM"]:
        rows.append((f"P50 / P90 {metric}", lambda s, m=metric: " / ".join(f(v) for v in s["thresholds"][m])))
    for level in ["Mid-Level", "Senior", "Expert"]:
        for field, name in [("wmc_avg", "WMC"), ("lcom_avg", "LCOM"), ("cwc_per_class", "CWC/classe")]:
            rows.append((f"{name} médio — {level}",
                         lambda s, l=level, k=field: f(s["by_level"][l][k], 2) if l in s["by_level"] else "—"))
    out = ["| Número | " + " | ".join(s["summary"]["label"] for s in steps) + " |",
           "|---|" + "---:|" * len(steps)]
    for name, get in rows:
        out.append(f"| {name} | " + " | ".join(get(s["summary"]) for s in steps) + " |")
    return out


def class_changes(prev, cur):
    common = prev["classes"].keys() & cur["classes"].keys()
    out = [f"Classes pareadas: {len(common)} · só em {prev['summary']['label']}: "
           f"{len(prev['classes'].keys() - cur['classes'].keys())} · só em {cur['summary']['label']}: "
           f"{len(cur['classes'].keys() - prev['classes'].keys())}", "",
           "| Métrica | Classes que mudaram | Soma antes | Soma depois |", "|---|---:|---:|---:|"]
    for m in METRICS:
        num = lambda r: float(r[m]) if r[m] != "" else 0.0
        changed = sum(1 for k in common if prev["classes"][k][m] != cur["classes"][k][m])
        before = sum(num(prev["classes"][k]) for k in common)
        after = sum(num(cur["classes"][k]) for k in common)
        out.append(f"| {m} | {changed} | {before:.0f} | {after:.0f} |")
    return out


def level_changes(prev, cur):
    moved = [(pid, prev["participants"][pid], cur["participants"][pid])
             for pid in prev["participants"] if pid in cur["participants"]
             and prev["participants"][pid]["predicted"] != cur["participants"][pid]["predicted"]]
    if not moved:
        return ["Nenhum participante mudou de nível previsto."]
    out = ["| Participante | Nível conhecido | Antes | Depois |", "|---|---|---|---|"]
    for pid, a, b in sorted(moved, key=lambda x: int(x[0][1:])):
        out.append(f"| {pid} | {a['level']} | {a['predicted']} | {b['predicted']} |")
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("dirs", nargs="+", help="analysis directories, in order")
    parser.add_argument("--out", help="write the comparison to this markdown file")
    args = parser.parse_args()

    steps = [load(d) for d in args.dirs]
    lines = ["# Comparação entre passos", "", "## Números principais", ""] + headline(steps)
    for prev, cur in zip(steps, steps[1:]):
        lines += ["", f"## {prev['summary']['label']} → {cur['summary']['label']}", ""]
        lines += class_changes(prev, cur) + [""] + level_changes(prev, cur)
    text = "\n".join(lines) + "\n"
    if args.out:
        with open(args.out, "w") as fh:
            fh.write(text)
    print(text)


if __name__ == "__main__":
    main()
