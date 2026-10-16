#!/usr/bin/env python3
"""Regenerates <results>/index.md from the per-participant CSV files.

Usage: python3 scripts/generate_index.py [results-dir]   (default: results)
"""

import csv
import math
import os
import sys
from datetime import date

RESULTS_DIR = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "results")


def read_csv(path):
    if not os.path.exists(path):
        return []
    with open(path) as f:
        return list(csv.DictReader(f))


def avg(rows, field):
    vals = [float(r[field]) for r in rows if r.get(field, "").strip() != ""]
    return round(sum(vals) / len(vals), 2) if vals else 0


def mingt0(rows, field):
    vals = [float(r[field]) for r in rows if r.get(field, "").strip() != "" and float(r[field]) > 0]
    return round(min(vals), 2) if vals else 0


def maxv(rows, field):
    vals = [float(r[field]) for r in rows if r.get(field, "").strip() != ""]
    return round(max(vals), 2) if vals else 0


def sumf(rows, field):
    return sum(float(r[field]) for r in rows if r.get(field, "").strip() != "")


def pearson(xs, ys):
    n = len(xs)
    mx, my = sum(xs) / n, sum(ys) / n
    num = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    dx = math.sqrt(sum((x - mx) ** 2 for x in xs))
    dy = math.sqrt(sum((y - my) ** 2 for y in ys))
    if dx == 0 or dy == 0:
        return float("nan")
    return num / (dx * dy)


def load_participant(p_dir, pid, pos, lvl):
    ck = read_csv(os.path.join(p_dir, "metrics_ck.csv"))
    misra = read_csv(os.path.join(p_dir, "metrics_misra.csv"))
    loc = read_csv(os.path.join(p_dir, "metrics_loc.csv"))
    cyc = read_csv(os.path.join(p_dir, "metrics_cyclomatic.csv"))

    # LOC — file-level rows (ClassName='' AND MethodName='', FilePath set)
    loc_file = [r for r in loc if r.get("ClassName", "").strip() == "" and r.get("MethodName", "").strip() == "" and r.get("FilePath", "").strip() != ""]
    total_loc = int(sumf(loc_file, "LOC"))

    # Cyclomatic — file-level rows
    cyc_file = [r for r in cyc if r.get("ClassName", "").strip() == "" and r.get("MethodName", "").strip() == "" and r.get("FilePath", "").strip() != ""]
    total_cyc = int(sumf(cyc_file, "Complexity"))

    # MISRA rows by level
    m_method = [r for r in misra if r.get("MethodName", "").strip() != ""]
    m_class  = [r for r in misra if r.get("MethodName", "").strip() == "" and r.get("ClassName", "").strip() != ""]
    m_cb     = next(r for r in misra if r.get("MethodName", "").strip() == "" and r.get("ClassName", "").strip() == "")

    total_mc  = int(sumf(m_method, "MC"))
    total_ac  = int(sumf(m_class,  "AC"))
    total_clc = int(sumf(m_class,  "CLC"))
    total_cwc = int(sumf(m_class,  "CWC"))
    total_cc  = int(float(m_cb["CC"]))

    mc_min  = int(mingt0(m_method, "MC"))
    mc_max  = int(maxv(m_method,   "MC"))
    ac_min  = int(mingt0(m_class,  "AC"))
    ac_max  = int(maxv(m_class,    "AC"))
    clc_min = int(mingt0(m_class,  "CLC"))
    clc_max = int(maxv(m_class,    "CLC"))
    cwc_min = int(mingt0(m_class,  "CWC"))
    cwc_max = int(maxv(m_class,    "CWC"))

    return {
        "pid": pid, "pos": pos, "lvl": lvl,
        "loc": total_loc,
        "classes": len(ck),
        "wmc_avg": avg(ck, "WMC"), "wmc_min": mingt0(ck, "WMC"), "wmc_max": maxv(ck, "WMC"),
        "dit_avg": avg(ck, "DIT"), "dit_min": mingt0(ck, "DIT"), "dit_max": maxv(ck, "DIT"),
        "noc_avg": avg(ck, "NOC"), "noc_min": mingt0(ck, "NOC"), "noc_max": maxv(ck, "NOC"),
        "cbo_avg": avg(ck, "CBO"), "cbo_min": mingt0(ck, "CBO"), "cbo_max": maxv(ck, "CBO"),
        "rfc_avg": avg(ck, "RFC"), "rfc_min": mingt0(ck, "RFC"), "rfc_max": maxv(ck, "RFC"),
        "lcom_avg": avg(ck, "LCOM"), "lcom_min": mingt0(ck, "LCOM"), "lcom_max": maxv(ck, "LCOM"),
        "mc_total": total_mc, "mc_min": mc_min, "mc_max": mc_max,
        "ac_total": total_ac, "ac_min": ac_min, "ac_max": ac_max,
        "clc_total": total_clc, "clc_min": clc_min, "clc_max": clc_max,
        "cwc_total": total_cwc, "cwc_min": cwc_min, "cwc_max": cwc_max,
        "cc": total_cc,
        "cyc": total_cyc,
    }


def table(headers, rows, align=None):
    if align is None:
        align = ["---"] * len(headers)
    lines = ["| " + " | ".join(headers) + " |"]
    lines.append("|" + "|".join(align) + "|")
    for row in rows:
        lines.append("| " + " | ".join(str(v) for v in row) + " |")
    return "\n".join(lines)


def group_avg(data, key, fields):
    groups = {}
    for d in data:
        g = d[key]
        groups.setdefault(g, []).append(d)
    result = {}
    for g, items in groups.items():
        result[g] = {f: round(sum(i[f] for i in items) / len(items)) for f in fields}
        result[g]["count"] = len(items)
    return result


def main():
    participants = sorted(
        [d for d in os.listdir(RESULTS_DIR) if os.path.isdir(os.path.join(RESULTS_DIR, d))],
        key=lambda x: int(x.split("-")[0][1:])
    )

    data = []
    for p in participants:
        parts = p.split("-")
        pid, pos, lvl = parts[0], parts[1], parts[2]
        data.append(load_participant(os.path.join(RESULTS_DIR, p), pid, pos, lvl))

    lines = []

    # Header
    lines += [
        f"# Metrics Summary — Artefatos Mestrado",
        "",
        f"> Generated: {date.today()}  ",
        f"> Tool: [Codimetry](https://github.com/jlfjunior/codimetry)  ",
        f"> Participants: {len(data)}",
        "",
        "---",
        "",
    ]

    # Overview Table
    lines += [
        "## Overview Table",
        "",
        table(
            ["Participant", "Position", "Level", "LOC", "Classes", "Avg WMC", "Avg CBO", "Avg LCOM", "MISRA CC", "Cyclomatic"],
            [[d["pid"], d["pos"], d["lvl"], d["loc"], d["classes"],
              d["wmc_avg"], d["cbo_avg"], d["lcom_avg"], d["cc"], d["cyc"]] for d in data],
            ["---", "---", "---", "---:", "---:", "---:", "---:", "---:", "---:", "---:"],
        ),
        "",
        "---",
        "",
    ]

    # Summary by Level
    level_order = ["jr", "pl", "sn", "esp"]
    by_level = group_avg(data, "lvl", ["loc", "classes", "cc", "cyc"])
    lines += [
        "## Summary by Level",
        "",
        table(
            ["Level", "Count", "Avg LOC", "Avg Classes", "Avg MISRA CC", "Avg Cyclomatic"],
            [[lvl, by_level[lvl]["count"], by_level[lvl]["loc"], by_level[lvl]["classes"],
              by_level[lvl]["cc"], by_level[lvl]["cyc"]]
             for lvl in level_order if lvl in by_level],
            ["---", "---:", "---:", "---:", "---:", "---:"],
        ),
        "",
    ]

    # Summary by Position
    by_pos = group_avg(data, "pos", ["loc", "classes", "cc", "cyc"])
    lines += [
        "## Summary by Position",
        "",
        table(
            ["Position", "Count", "Avg LOC", "Avg Classes", "Avg MISRA CC", "Avg Cyclomatic"],
            [[pos, by_pos[pos]["count"], by_pos[pos]["loc"], by_pos[pos]["classes"],
              by_pos[pos]["cc"], by_pos[pos]["cyc"]]
             for pos in ["dev", "arch"] if pos in by_pos],
            ["---", "---:", "---:", "---:", "---:", "---:"],
        ),
        "",
        "---",
        "",
    ]

    # LOC Distribution
    ranges = [
        ("< 500",     lambda x: x < 500),
        ("500–1000",  lambda x: 500 <= x < 1000),
        ("1000–2000", lambda x: 1000 <= x < 2000),
        ("2000–3000", lambda x: 2000 <= x < 3000),
        ("3000–5000", lambda x: 3000 <= x <= 5000),
    ]
    lines += [
        "## LOC Distribution",
        "",
        table(
            ["Range", "Participants"],
            [[r, ", ".join(d["pid"] for d in data if fn(d["loc"]))] for r, fn in ranges],
        ),
        "",
        "---",
        "",
    ]

    # Complexity Highlights
    top5_cc   = sorted(data, key=lambda d: d["cc"],   reverse=True)[:5]
    top5_cyc  = sorted(data, key=lambda d: d["cyc"],  reverse=True)[:5]
    top5_lcom = sorted(data, key=lambda d: d["lcom_avg"], reverse=True)[:5]

    lines += [
        "## Complexity Highlights",
        "",
        "### Highest MISRA CC (Code Complexity)",
        table(
            ["Rank", "Participant", "MISRA CC"],
            [[i + 1, f"{d['pid']}-{d['pos']}-{d['lvl']}", d["cc"]] for i, d in enumerate(top5_cc)],
            ["---", "---", "---:"],
        ),
        "",
        "### Highest Cyclomatic Complexity",
        table(
            ["Rank", "Participant", "Cyclomatic"],
            [[i + 1, f"{d['pid']}-{d['pos']}-{d['lvl']}", d["cyc"]] for i, d in enumerate(top5_cyc)],
            ["---", "---", "---:"],
        ),
        "",
        "### Highest LCOM (Lack of Cohesion — lower is better)",
        table(
            ["Rank", "Participant", "Avg LCOM"],
            [[i + 1, f"{d['pid']}-{d['pos']}-{d['lvl']}", d["lcom_avg"]] for i, d in enumerate(top5_lcom)],
            ["---", "---", "---:"],
        ),
        "",
        "---",
        "",
    ]

    # Correlation Matrix
    metrics = {
        "LOC":     [d["loc"]       for d in data],
        "Classes": [d["classes"]   for d in data],
        "WMC":     [d["wmc_avg"]   for d in data],
        "DIT":     [d["dit_avg"]   for d in data],
        "NOC":     [d["noc_avg"]   for d in data],
        "CBO":     [d["cbo_avg"]   for d in data],
        "RFC":     [d["rfc_avg"]   for d in data],
        "LCOM":    [d["lcom_avg"]  for d in data],
        "MC":      [d["mc_total"]  for d in data],
        "AC":      [d["ac_total"]  for d in data],
        "CLC":     [d["clc_total"] for d in data],
        "CWC":     [d["cwc_total"] for d in data],
        "CC":      [d["cc"]        for d in data],
        "Cyclo":   [d["cyc"]       for d in data],
    }
    m_names = list(metrics.keys())

    corr_matrix = {}
    corr_rows = []
    for m in m_names:
        row = [f"**{m}**"]
        for m2 in m_names:
            v = pearson(metrics[m], metrics[m2])
            corr_matrix[(m, m2)] = v
            row.append(f"{v:.2f}" if not math.isnan(v) else "—")
        corr_rows.append(row)

    # Compute strong correlations dynamically
    strong = []
    for i, m in enumerate(m_names):
        for m2 in m_names[i + 1:]:
            v = corr_matrix[(m, m2)]
            if not math.isnan(v) and abs(v) >= 0.85:
                strong.append((m, m2, v))
    strong.sort(key=lambda x: abs(x[2]), reverse=True)
    strong_str = ", ".join(f"{m}↔{m2} ({v:.2f})" for m, m2, v in strong)

    wmc_lcom = corr_matrix[("WMC", "LCOM")]

    lines += [
        "## Metric Correlation Matrix",
        "",
        f"Pearson correlation across all {len(data)} participants.",
        "",
        table(
            ["Metric"] + m_names,
            corr_rows,
            ["---"] + ["------:" for _ in m_names],
        ),
        "",
        f"**Strong correlations (|r| ≥ 0.85):** {strong_str}  ",
        f"**Notable:** WMC↔LCOM ({wmc_lcom:.2f}) — classes with more methods also tend to be less cohesive. CBO is negatively correlated with size metrics, suggesting smaller codebases use more coupling per class.",
        "",
        "---",
        "",
    ]

    # CK Metrics
    ck_headers = [
        "Participant", "Position", "Level", "Classes",
        "Avg WMC", "Min WMC", "Max WMC",
        "Avg DIT", "Min DIT", "Max DIT",
        "Avg NOC", "Min NOC", "Max NOC",
        "Avg CBO", "Min CBO", "Max CBO",
        "Avg RFC", "Min RFC", "Max RFC",
        "Avg LCOM", "Min LCOM", "Max LCOM",
    ]
    ck_align = ["---", "---", "---"] + ["---:"] * 19
    ck_rows = [
        [d["pid"], d["pos"], d["lvl"], d["classes"],
         d["wmc_avg"], d["wmc_min"], d["wmc_max"],
         d["dit_avg"], d["dit_min"], d["dit_max"],
         d["noc_avg"], d["noc_min"], d["noc_max"],
         d["cbo_avg"], d["cbo_min"], d["cbo_max"],
         d["rfc_avg"], d["rfc_min"], d["rfc_max"],
         d["lcom_avg"], d["lcom_min"], d["lcom_max"]]
        for d in data
    ]
    lines += [
        "## CK Metrics (per participant, averages/min/max per class)",
        "",
        table(ck_headers, ck_rows, ck_align),
        "",
        "> Min values exclude zero (min > 0). DIT and NOC reflect only inheritance within the analysed codebase; external base classes (e.g. ControllerBase) are not counted.",
        "",
        "---",
        "",
    ]

    # MISRA Metrics
    misra_headers = [
        "Participant", "Position", "Level",
        "Total MC", "Min MC", "Max MC",
        "Total AC", "Min AC", "Max AC",
        "Total CLC", "Min CLC", "Max CLC",
        "Total CWC", "Min CWC", "Max CWC",
        "Total CC",
    ]
    misra_align = ["---", "---", "---"] + ["---:"] * 13
    misra_rows = [
        [d["pid"], d["pos"], d["lvl"],
         d["mc_total"], d["mc_min"], d["mc_max"],
         d["ac_total"], d["ac_min"], d["ac_max"],
         d["clc_total"], d["clc_min"], d["clc_max"],
         d["cwc_total"], d["cwc_min"], d["cwc_max"],
         d["cc"]]
        for d in data
    ]
    lines += [
        "## MISRA Metrics (per participant)",
        "",
        "MC and CC are at method level; AC, CLC, and CWC are at class level. Min values exclude zero (min > 0).",
        "",
        table(misra_headers, misra_rows, misra_align),
        "",
        "---",
        "",
    ]

    # Metric Definitions
    lines += [
        "## Metric Definitions",
        "",
        table(
            ["Metric", "Description"],
            [
                ["**LOC**", "Effective lines of code (excludes comments and blank lines)"],
                ["**WMC**", "Weighted Methods per Class — method count per class"],
                ["**DIT**", "Depth of Inheritance Tree — levels of inheritance from root (external base classes not counted)"],
                ["**NOC**", "Number of Children — direct subclasses within the analysed codebase"],
                ["**CBO**", "Coupling Between Objects — number of coupled classes (lower = better)"],
                ["**RFC**", "Response For a Class — number of methods that can be executed"],
                ["**LCOM**", "Lack of Cohesion of Methods — lower = better cohesion"],
                ["**MC**", "Method Complexity — MISRA cognitive complexity at method level"],
                ["**AC**", "Accumulated Complexity — MISRA complexity accumulated at class level"],
                ["**CLC**", "Class-Level Complexity — sum of complexity within the class"],
                ["**CWC**", "Class Weighted Complexity — weighted complexity at class level"],
                ["**MISRA CC**", "Overall codebase cognitive complexity"],
                ["**Cyclomatic**", "Total cyclomatic complexity (1 + decision points per method, summed)"],
            ],
        ),
        "",
        "---",
        "",
        "## CSV Files",
        "",
        "Each participant folder contains:",
        "- `metrics_loc.csv` — LOC at method, class, and file levels",
        "- `metrics_ck.csv` — CK metrics (WMC, DIT, NOC, CBO, RFC, LCOM) per class",
        "- `metrics_misra.csv` — MISRA metrics (MC, AC, CLC, CWC, CC) at method/class/codebase levels",
        "- `metrics_cyclomatic.csv` — Cyclomatic complexity at method, class, and file levels",
    ]

    output = os.path.join(RESULTS_DIR, "index.md")
    with open(output, "w") as f:
        f.write("\n".join(lines) + "\n")

    print(f"index.md generated at {output}")


if __name__ == "__main__":
    main()
