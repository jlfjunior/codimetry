#!/usr/bin/env python3
"""Reproducible CBTAM analysis of the case-study results (Cap. 4 of the dissertation).

Reads a results directory (one folder per participant, named p{n}-{position}-{level},
each holding the four CSVs exported by the tool) and writes CSV tables and a
report.md with every number reported in Cap. 4.

Standard library only (Python >= 3.9), so the numbers do not depend on the
versions of numpy/pandas/scipy installed on the machine.

Usage:
    python3 scripts/analysis/cbtam_analysis.py --results results-v1 --out analysis/v1
    python3 scripts/analysis/cbtam_analysis.py --results results-v1 --out analysis/v1 \
        --check scripts/analysis/published_v1.json
"""
import argparse
import csv
import json
import math
import os
import re
import statistics
import sys

LEVELS = {"jr": "Beginner", "pl": "Mid-Level", "sn": "Senior", "esp": "Expert"}
LEVEL_ORDER = ["Beginner", "Mid-Level", "Senior", "Expert"]
POSITIONS = {"dev": "Developer", "arch": "Architect"}

# The seven CBTAM metrics, in the column order of tab:per_participant_results,
# with the population each one is classified over.
CBTAM_METRICS = [
    ("WMC", "classes"),
    ("DIT", "classes"),
    ("NOC", "classes"),
    ("LCOM", "classes"),
    ("AC", "misra_classes"),
    ("CWC", "misra_classes"),
    ("MC", "methods"),
]

# Absolute literature thresholds (tab:reference_thresholds), upper bound of Normal.
ABSOLUTE_NORMAL_MAX = {"WMC": 20, "DIT": 5, "NOC": 5}

# Staged aggregation rule (empirical_application): level -> (min metrics, min % Normal).
STAGES = [("Mid-Level", 4, 40.0), ("Senior", 5, 50.0), ("Expert", 6, 60.0)]


# --------------------------------------------------------------------------- statistics

def percentile(values, q):
    """Linear interpolation between closest ranks (numpy's default method)."""
    v = sorted(values)
    h = (len(v) - 1) * q
    lo = math.floor(h)
    hi = min(lo + 1, len(v) - 1)
    return v[lo] + (v[hi] - v[lo]) * (h - lo)


def pearson(xs, ys):
    mx, my = statistics.fmean(xs), statistics.fmean(ys)
    num = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    dx = math.sqrt(sum((x - mx) ** 2 for x in xs))
    dy = math.sqrt(sum((y - my) ** 2 for y in ys))
    return float("nan") if dx == 0 or dy == 0 else num / (dx * dy)


def average_ranks(values):
    order = sorted(range(len(values)), key=lambda i: values[i])
    ranks = [0.0] * len(values)
    i = 0
    while i < len(order):
        j = i
        while j + 1 < len(order) and values[order[j + 1]] == values[order[i]]:
            j += 1
        for k in range(i, j + 1):
            ranks[order[k]] = (i + j) / 2 + 1
        i = j + 1
    return ranks


def spearman(xs, ys):
    """Pearson correlation of average ranks (same as scipy.stats.spearmanr)."""
    return pearson(average_ranks(xs), average_ranks(ys))


def mean(values):
    return statistics.fmean(values) if values else float("nan")


def median(values):
    return statistics.median(values) if values else float("nan")


# --------------------------------------------------------------------------- loading

def read_csv(path):
    if not os.path.exists(path):
        return []
    with open(path, newline="") as f:
        return list(csv.DictReader(f))


def last_per_key(rows, keys):
    """One record per key, keeping the last occurrence (rule used in Cap. 4).

    v1 of the tool emits a partial or homonymous class once per declaration, all
    with the same FilePath; when the copies disagree, the last one is kept.
    """
    out = {}
    for r in rows:
        out[tuple(r[k] for k in keys)] = r
    return list(out.values())


def num(row, field):
    return float(row[field]) if row.get(field, "").strip() != "" else 0.0


def load_participant(directory):
    name = os.path.basename(directory.rstrip("/"))
    m = re.fullmatch(r"(p\d+)-(\w+)-(\w+)", name)
    if not m:
        return None
    pid, pos, lvl = m.groups()

    ck_raw = read_csv(os.path.join(directory, "metrics_ck.csv"))
    misra = read_csv(os.path.join(directory, "metrics_misra.csv"))
    loc = read_csv(os.path.join(directory, "metrics_loc.csv"))
    cyc = read_csv(os.path.join(directory, "metrics_cyclomatic.csv"))

    misra_classes_raw = [r for r in misra if not r["MethodName"] and r["ClassName"]]
    codebase_rows = [r for r in misra if not r["MethodName"] and not r["ClassName"]]
    file_level = lambda rows: [r for r in rows if r["FilePath"] and not r["ClassName"] and not r["MethodName"]]

    return {
        "dir": name,
        "pid": pid,
        "num": int(pid[1:]),
        "position": POSITIONS.get(pos, pos),
        "level": LEVELS.get(lvl, lvl),
        "classes_raw": ck_raw,
        "classes": last_per_key(ck_raw, ["FilePath", "ClassName"]),
        "misra_classes": last_per_key(misra_classes_raw, ["FilePath", "ClassName"]),
        "misra_classes_raw": misra_classes_raw,
        "methods": [r for r in misra if r["MethodName"]],
        # Tamanho = LLOC (Park, 1992); resultados v1 sem a coluna caem na LOC antiga
        "loc": int(sum(num(r, "LLOC" if "LLOC" in r else "LOC") for r in file_level(loc))),
        "cyclomatic": int(sum(num(r, "Complexity") for r in file_level(cyc))),
        "coc": int(num(codebase_rows[0], "CC")) if codebase_rows else 0,
    }


def load_results(results_dir, exclude):
    participants = []
    for entry in os.listdir(results_dir):
        path = os.path.join(results_dir, entry)
        if os.path.isdir(path):
            p = load_participant(path)
            if p and p["pid"] not in exclude:
                participants.append(p)
    return sorted(participants, key=lambda p: p["num"])


# --------------------------------------------------------------------------- analysis

def participant_summary(p):
    n = len(p["classes"])
    col = lambda rows, f: [num(r, f) for r in rows]
    return {
        "pid": p["pid"],
        "position": p["position"],
        "level": p["level"],
        "loc": p["loc"],
        "classes": n,
        # Row count before consolidation (the "Classes" column of results/index.md)
        "classes_raw": len(p["classes_raw"]),
        "methods": len(p["methods"]),
        "wmc_avg": mean(col(p["classes"], "WMC")),
        "dit_avg": mean(col(p["classes"], "DIT")),
        "noc_avg": mean(col(p["classes"], "NOC")),
        "cbo_avg": mean(col(p["classes"], "CBO")),
        "rfc_avg": mean(col(p["classes"], "RFC")),
        "lcom_avg": mean(col(p["classes"], "LCOM")),
        # Misra metrics normalised by the number of classes analysed (tab:per_class_averages)
        "mc_per_class": sum(col(p["methods"], "MC")) / n,
        "ac_per_class": sum(col(p["misra_classes"], "AC")) / n,
        "cwc_per_class": sum(col(p["misra_classes"], "CWC")) / n,
        # Codebase totals used in the redundancy analysis
        "mc_total": sum(col(p["methods"], "MC")),
        "ac_total": sum(col(p["misra_classes"], "AC")),
        "clc_total": sum(col(p["misra_classes"], "CLC")),
        "cwc_total": sum(col(p["misra_classes"], "CWC")),
        "coc": p["coc"],
        "cyclomatic": p["cyclomatic"],
    }


def thresholds(participants):
    result = {}
    for metric, population in CBTAM_METRICS:
        values = [num(r, metric) for p in participants for r in p[population]]
        result[metric] = {"p50": percentile(values, 0.5), "p90": percentile(values, 0.9), "n": len(values)}
    return result


def classify(value, th):
    if value <= th["p50"]:
        return "Normal"
    return "High" if value <= th["p90"] else "Anomalous"


def staged_level(pct_normal):
    level = "Beginner"
    for name, min_metrics, min_pct in STAGES:
        if sum(1 for v in pct_normal.values() if v >= min_pct) >= min_metrics:
            level = name
        else:
            break
    return level


def per_participant_results(participants, th):
    rows = []
    for p in participants:
        pct = {}
        for metric, population in CBTAM_METRICS:
            values = [num(r, metric) for r in p[population]]
            pct[metric] = 100 * sum(1 for v in values if classify(v, th[metric]) == "Normal") / len(values)
        predicted = staged_level(pct)
        distance = LEVEL_ORDER.index(predicted) - LEVEL_ORDER.index(p["level"])
        if distance == 0:
            discrepancy = "-"
        elif abs(distance) == 1:
            discrepancy = "Adjacente"
        else:
            discrepancy = f"Discrepante ({distance:+d})"
        rows.append({
            "pid": p["pid"], "level": p["level"], **{m: pct[m] for m, _ in CBTAM_METRICS},
            "predicted": predicted, "correct": distance == 0, "distance": distance,
            "discrepancy": discrepancy, "mean_pct_normal": mean(list(pct.values())),
        })
    return rows


def group_stats(summaries, key, group, fields):
    members = [s for s in summaries if s[key] == group]
    return {"n": len(members), **{f: (mean([s[f] for s in members]), median([s[f] for s in members])) for f in fields}}


def analyse(participants):
    summaries = [participant_summary(p) for p in participants]
    all_classes = [r for p in participants for r in p["classes"]]
    th = thresholds(participants)
    ppr = per_participant_results(participants, th)

    by_level = {lvl: group_stats(summaries, "level", lvl,
                                 ["wmc_avg", "lcom_avg", "mc_per_class", "ac_per_class", "cwc_per_class", "loc", "classes"])
                for lvl in LEVEL_ORDER if any(s["level"] == lvl for s in summaries)}
    by_position = {pos: group_stats(summaries, "position", pos, ["loc", "wmc_avg", "lcom_avg"])
                   for pos in ["Architect", "Developer"]}

    corr_fields = ["wmc_avg", "lcom_avg", "dit_avg", "noc_avg", "cbo_avg", "mc_total", "ac_total",
                   "clc_total", "cwc_total", "coc", "cyclomatic", "loc", "classes", "classes_raw"]
    correlations = {(a, b): pearson([s[a] for s in summaries], [s[b] for s in summaries])
                    for i, a in enumerate(corr_fields) for b in corr_fields[i + 1:]}

    # LCOM is at most n(n−1)/2 over its own methods; v1 results have no LcomMethods column and
    # computed LCOM over the same methods WMC counted
    def lcom_methods(r):
        return num(r, "LcomMethods" if "LcomMethods" in r else "WMC")

    lcom_equals_max_pairs = sum(1 for r in all_classes
                                if num(r, "LCOM") == lcom_methods(r) * (lcom_methods(r) - 1) / 2)

    # DIT x NOC in metric_redundancy is a class-level correlation, unlike the others
    dit_noc_class_level = pearson([num(r, "DIT") for r in all_classes], [num(r, "NOC") for r in all_classes])

    absolute_normal = {m: 100 * sum(1 for r in all_classes if num(r, m) <= limit) / len(all_classes)
                       for m, limit in ABSOLUTE_NORMAL_MAX.items()}

    # Confusion matrix and accuracy exclude nothing; accuracy/baseline exclude the Beginner level
    confusion = {k: {pr: 0 for pr in LEVEL_ORDER} for k in LEVEL_ORDER}
    for r in ppr:
        confusion[r["level"]][r["predicted"]] += 1
    evaluated = [r for r in ppr if r["level"] != "Beginner"]
    most_frequent = max(LEVEL_ORDER, key=lambda lvl: sum(1 for r in evaluated if r["level"] == lvl))
    known_rank = [LEVEL_ORDER.index(r["level"]) for r in ppr]
    errors = [r for r in ppr if not r["correct"]]

    return {
        "summaries": summaries,
        "thresholds": th,
        "per_participant": ppr,
        "by_level": by_level,
        "by_position": by_position,
        "correlations": correlations,
        "counts": {
            "participants": len(participants),
            "classes_raw": sum(len(p["classes_raw"]) for p in participants),
            "classes": len(all_classes),
            "methods": sum(len(p["methods"]) for p in participants),
            # Records and structs are measured since INC-13; v1 results have no Kind column (only classes)
            "by_kind": {k: sum(1 for r in all_classes if r.get("Kind", "class") == k)
                        for k in sorted({r.get("Kind", "class") for r in all_classes})},
        },
        "dit_noc_class_level": dit_noc_class_level,
        "lcom_equals_max_pairs_pct": 100 * lcom_equals_max_pairs / len(all_classes),
        "dit_eq_1_pct": 100 * sum(1 for r in all_classes if num(r, "DIT") == 1) / len(all_classes),
        "noc_eq_0_pct": 100 * sum(1 for r in all_classes if num(r, "NOC") == 0) / len(all_classes),
        "lcom_avg_range": (min(s["lcom_avg"] for s in summaries), max(s["lcom_avg"] for s in summaries)),
        "absolute_normal_pct": absolute_normal,
        "confusion": confusion,
        "accuracy": {
            "evaluated": len(evaluated),
            "correct": sum(1 for r in evaluated if r["correct"]),
            "baseline_level": most_frequent,
            "baseline_correct": sum(1 for r in evaluated if r["level"] == most_frequent),
        },
        "spearman_staged": spearman([LEVEL_ORDER.index(r["predicted"]) for r in ppr], known_rank),
        "spearman_mean_pct": spearman([r["mean_pct_normal"] for r in ppr], known_rank),
        "errors": {
            "total": len(errors),
            "adjacent": sum(1 for r in errors if abs(r["distance"]) == 1),
            "two_or_more": sum(1 for r in errors if abs(r["distance"]) >= 2),
        },
    }


# --------------------------------------------------------------------------- output

def fmt(value, digits=2):
    """Brazilian decimal format, as in the dissertation text."""
    return f"{value:.{digits}f}".replace(".", ",")


def write_csv(path, rows, fields):
    with open(path, "w", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore", lineterminator="\n")
        w.writeheader()
        for r in rows:
            w.writerow({k: (round(v, 4) if isinstance(v, float) else v) for k, v in r.items()})


def write_outputs(result, participants, out_dir, label):
    os.makedirs(out_dir, exist_ok=True)

    write_csv(os.path.join(out_dir, "participants.csv"), result["summaries"], list(result["summaries"][0].keys()))
    metric_names = [m for m, _ in CBTAM_METRICS]
    write_csv(os.path.join(out_dir, "per_participant_results.csv"), result["per_participant"],
              ["pid", "level"] + metric_names + ["predicted", "correct", "distance", "discrepancy", "mean_pct_normal"])

    # Stable per-class key (participant + file + class) for the v1 x v2 comparison
    per_class = []
    misra_by_key = {}
    for p in participants:
        for r in p["misra_classes"]:
            misra_by_key[(p["pid"], r["FilePath"], r["ClassName"])] = r
    for p in participants:
        for r in p["classes"]:
            m = misra_by_key.get((p["pid"], r["FilePath"], r["ClassName"]), {})
            per_class.append({
                "pid": p["pid"], "level": p["level"], "file": r["FilePath"], "class": r["ClassName"],
                **{k: r[k] for k in ["WMC", "DIT", "NOC", "CBO", "RFC", "LCOM"]},
                **{k: m.get(k, "") for k in ["AC", "CLC", "CWC"]},
            })
    write_csv(os.path.join(out_dir, "per_class.csv"), per_class, list(per_class[0].keys()))

    with open(os.path.join(out_dir, "report.md"), "w") as f:
        f.write(render_report(result, label))

    with open(os.path.join(out_dir, "summary.json"), "w") as f:
        json.dump(summary(result, label), f, indent=1, ensure_ascii=False)


def summary(r, label):
    """Headline numbers, used by compare.py."""
    a = r["accuracy"]
    return {
        "label": label,
        **r["counts"],
        "lcom_equals_max_pairs_pct": r["lcom_equals_max_pairs_pct"],
        "accuracy_pct": 100 * a["correct"] / a["evaluated"],
        "baseline_pct": 100 * a["baseline_correct"] / a["evaluated"],
        "spearman_staged": r["spearman_staged"],
        "spearman_mean_pct": r["spearman_mean_pct"],
        "errors": r["errors"],
        "thresholds": {m: [t["p50"], t["p90"]] for m, t in r["thresholds"].items()},
        "by_level": {lvl: {k: v[0] for k, v in g.items() if k != "n"} for lvl, g in r["by_level"].items()},
        "confusion": r["confusion"],
    }


def render_report(r, label):
    L = []
    c = r["counts"]
    L += [f"# Análise CBTAM — {label}", "",
          f"Participantes: {c['participants']} · classes: {c['classes']} "
          f"(de {c['classes_raw']} linhas, um registro por arquivo + classe) · métodos: {c['methods']}", ""]

    L += ["## tab:per_class_averages", "",
          "| Nível | N | WMC | LCOM | MC/classe | AC/classe | CWC/classe |", "|---|---:|---:|---:|---:|---:|---:|"]
    for lvl, g in r["by_level"].items():
        cells = [f"{fmt(g[k][0])} ({fmt(g[k][1])})" for k in ["wmc_avg", "lcom_avg", "mc_per_class", "ac_per_class", "cwc_per_class"]]
        L.append(f"| {lvl} | {g['n']} | " + " | ".join(cells) + " |")

    L += ["", "## tab:codebase_size", "", "| Nível | N | LOC média (mediana) | Classes média (mediana) |", "|---|---:|---:|---:|"]
    for lvl, g in r["by_level"].items():
        L.append(f"| {lvl} | {g['n']} | {g['loc'][0]:.0f} ({g['loc'][1]:.0f}) | {fmt(g['classes'][0], 1)} ({fmt(g['classes'][1], 1)}) |")

    corr = lambda a, b: r["correlations"].get((a, b), r["correlations"].get((b, a)))
    L += ["", "## metric_redundancy", "",
          "Pearson entre participantes:", ""]
    for a, b in [("clc_total", "loc"), ("cwc_total", "loc"), ("coc", "loc"), ("cyclomatic", "loc"),
                 ("clc_total", "classes"), ("cwc_total", "classes"), ("coc", "classes"), ("cyclomatic", "classes"),
                 ("cwc_total", "cyclomatic"), ("clc_total", "cyclomatic"), ("cwc_total", "coc"), ("clc_total", "coc"),
                 ("mc_total", "clc_total"), ("wmc_avg", "lcom_avg"), ("cbo_avg", "loc"), ("cbo_avg", "classes"),
                 ("cbo_avg", "classes_raw"), ("dit_avg", "noc_avg")]:
        L.append(f"- {a} × {b}: r = {fmt(corr(a, b), 3)}")
    L.append(f"- DIT × NOC por classe (n = {c['classes']}): r = {fmt(r['dit_noc_class_level'], 3)}")
    L += ["", f"- Classes com DIT = 1: {fmt(r['dit_eq_1_pct'], 1)}%",
          f"- Classes com NOC = 0: {fmt(r['noc_eq_0_pct'], 1)}%",
          f"- Amplitude da LCOM média por participante: {fmt(r['lcom_avg_range'][0])} – {fmt(r['lcom_avg_range'][1])}"]

    L += ["", "## position_analysis", "", "| Cargo | N | LOC média | WMC média | LCOM média |", "|---|---:|---:|---:|---:|"]
    for pos, g in r["by_position"].items():
        L.append(f"| {pos} | {g['n']} | {g['loc'][0]:.0f} | {fmt(g['wmc_avg'][0])} | {fmt(g['lcom_avg'][0])} |")

    L += ["", "## empirical_application", "",
          "Limiares absolutos (% Normal sobre todas as classes): " +
          ", ".join(f"{m} {fmt(v, 1)}%" for m, v in r["absolute_normal_pct"].items()), "",
          f"LCOM = n(n−1)/2 (n = métodos da LCOM; WMC na v1) em {fmt(r['lcom_equals_max_pairs_pct'])}% das classes.", "",
          "Tipos medidos por Kind: " + ", ".join(f"{k} {n}" for k, n in r["counts"]["by_kind"].items()) + ".", "",
          "Limiares relativos (amostra combinada):", "", "| Métrica | População | P50 | P90 |", "|---|---:|---:|---:|"]
    for m, th in r["thresholds"].items():
        L.append(f"| {m} | {th['n']} | {fmt(th['p50'])} | {fmt(th['p90'])} |")

    L += ["", "## tab:confusion_matrix", "", "| Conhecido \\ Previsto | " + " | ".join(LEVEL_ORDER) + " |",
          "|---|" + "---:|" * len(LEVEL_ORDER)]
    for known in LEVEL_ORDER:
        n = sum(r["confusion"][known].values())
        if n:
            L.append(f"| {known} (n={n}) | " + " | ".join(str(r["confusion"][known][p]) for p in LEVEL_ORDER) + " |")
    a = r["accuracy"]
    L += ["", f"- Acurácia (sem Beginner): {a['correct']}/{a['evaluated']} = {fmt(100 * a['correct'] / a['evaluated'], 1)}%",
          f"- Linha de base (sempre {a['baseline_level']}): {a['baseline_correct']}/{a['evaluated']} = "
          f"{fmt(100 * a['baseline_correct'] / a['evaluated'], 1)}%",
          f"- ρ de Spearman (regra em estágios × nível conhecido, n = {c['participants']}): {fmt(r['spearman_staged'], 3)}",
          f"- ρ de Spearman (média simples do % Normal × nível conhecido): {fmt(r['spearman_mean_pct'], 3)}",
          f"- Erros: {r['errors']['total']} ({r['errors']['adjacent']} adjacentes, {r['errors']['two_or_more']} a dois ou mais níveis)"]

    L += ["", "## tab:per_participant_results", "",
          "| ID | Nível | " + " | ".join(m for m, _ in CBTAM_METRICS) + " | Previsto | Corr. | Discrepância |",
          "|---|---|" + "---:|" * len(CBTAM_METRICS) + "---|---|---|"]
    for p in r["per_participant"]:
        L.append(f"| {p['pid']} | {p['level']} | " + " | ".join(fmt(p[m], 1) for m, _ in CBTAM_METRICS) +
                 f" | {p['predicted']} | {'Sim' if p['correct'] else 'Não'} | {p['discrepancy']} |")
    return "\n".join(L) + "\n"


# --------------------------------------------------------------------------- golden check

def check_published(result, path):
    """Compare against the numbers published in the dissertation.

    Returns (unexpected, known): mismatches not explained by the "errata" section of
    the JSON, and those that are (key -> explanation).
    """
    with open(path) as f:
        pub = json.load(f)
    tol = pub.get("tolerance", {})
    mismatches = []

    def cmp(name, got, expected, digits):
        if round(got, digits) != round(expected, digits):
            mismatches.append(f"{name}: calculado {round(got, digits)}, publicado {expected}")

    c = result["counts"]
    for k in ["participants", "classes", "methods"]:
        cmp(k, c[k], pub["counts"][k], 0)
    cmp("lcom_equals_max_pairs_pct", result["lcom_equals_max_pairs_pct"], pub["lcom_equals_max_pairs_pct"], 2)
    cmp("dit_eq_1_pct", result["dit_eq_1_pct"], pub["dit_eq_1_pct"], 1)
    cmp("noc_eq_0_pct", result["noc_eq_0_pct"], pub["noc_eq_0_pct"], 1)
    for m, v in pub["absolute_normal_pct"].items():
        cmp(f"absolute_normal_pct.{m}", result["absolute_normal_pct"][m], v, 1)
    cmp("dit_noc_class_level", result["dit_noc_class_level"], pub["dit_noc_class_level"], 2)
    cmp("lcom_avg_range.min", result["lcom_avg_range"][0], pub["lcom_avg_range"][0], 2)
    cmp("lcom_avg_range.max", result["lcom_avg_range"][1], pub["lcom_avg_range"][1], 2)

    for lvl, exp in pub["by_level"].items():
        g = result["by_level"][lvl]
        cmp(f"by_level.{lvl}.n", g["n"], exp["n"], 0)
        for field, (m_exp, md_exp) in exp["fields"].items():
            digits = 0 if field == "loc" else (1 if field == "classes" else 2)
            cmp(f"by_level.{lvl}.{field}.mean", g[field][0], m_exp, digits)
            cmp(f"by_level.{lvl}.{field}.median", g[field][1], md_exp, digits)

    for pos, exp in pub["by_position"].items():
        g = result["by_position"][pos]
        cmp(f"by_position.{pos}.n", g["n"], exp["n"], 0)
        cmp(f"by_position.{pos}.loc", g["loc"][0], exp["loc"], 0)
        cmp(f"by_position.{pos}.wmc_avg", g["wmc_avg"][0], exp["wmc_avg"], 2)
        cmp(f"by_position.{pos}.lcom_avg", g["lcom_avg"][0], exp["lcom_avg"], 2)

    for key, v in pub["correlations"].items():
        a, b = key.split("|")
        got = result["correlations"].get((a, b), result["correlations"].get((b, a)))
        digits = len(str(v).split(".")[1]) if "." in str(v) else 2
        cmp(f"pearson.{key}", got, v, digits)

    for known, row in pub["confusion"].items():
        for predicted, v in row.items():
            cmp(f"confusion.{known}.{predicted}", result["confusion"][known][predicted], v, 0)
    a = result["accuracy"]
    cmp("accuracy.correct", a["correct"], pub["accuracy"]["correct"], 0)
    cmp("accuracy.evaluated", a["evaluated"], pub["accuracy"]["evaluated"], 0)
    cmp("accuracy.baseline_correct", a["baseline_correct"], pub["accuracy"]["baseline_correct"], 0)
    cmp("spearman_staged", result["spearman_staged"], pub["spearman_staged"], 3)
    cmp("spearman_mean_pct", result["spearman_mean_pct"], pub["spearman_mean_pct"], 3)
    for k in ["total", "adjacent", "two_or_more"]:
        cmp(f"errors.{k}", result["errors"][k], pub["errors"][k], 0)

    got_pp = {r["pid"]: r for r in result["per_participant"]}
    for pid, exp in pub["per_participant"].items():
        if pid not in got_pp:
            mismatches.append(f"per_participant.{pid}: ausente nos resultados analisados")
            continue
        got = got_pp[pid]
        for m, _ in CBTAM_METRICS:
            cmp(f"per_participant.{pid}.{m}", got[m], exp[m], 1)
        if got["predicted"] != exp["predicted"]:
            mismatches.append(f"per_participant.{pid}.predicted: calculado {got['predicted']}, publicado {exp['predicted']}")
        if got["correct"] != exp["correct"]:
            mismatches.append(f"per_participant.{pid}.correct: calculado {got['correct']}, publicado {exp['correct']}")
        if got["discrepancy"] != exp["discrepancy"]:
            mismatches.append(f"per_participant.{pid}.discrepancy: calculado {got['discrepancy']}, publicado {exp['discrepancy']}")

    errata = pub.get("errata", {})
    key = lambda m: m.split(":")[0]
    unexpected = [m for m in mismatches if key(m) not in errata]
    known = [(m, errata[key(m)]) for m in mismatches if key(m) in errata]
    stale = [k for k in errata if k not in {key(m) for m in mismatches}]
    unexpected += [f"{k}: errata registrada, mas o valor agora confere" for k in stale]
    return unexpected, known


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--results", required=True, help="results directory (e.g. results-v1)")
    parser.add_argument("--out", required=True, help="output directory (e.g. analysis/v1)")
    parser.add_argument("--label", help="label shown in report.md (default: results directory name)")
    parser.add_argument("--exclude", nargs="*", default=[], help="participant ids to leave out (e.g. p33)")
    parser.add_argument("--check", help="JSON with the published numbers; exits 1 on any mismatch")
    args = parser.parse_args()

    participants = load_results(args.results, set(args.exclude))
    if not participants:
        sys.exit(f"no participant folders found in {args.results}")
    result = analyse(participants)
    write_outputs(result, participants, args.out, args.label or os.path.basename(os.path.normpath(args.results)))
    print(f"wrote {args.out}/report.md ({result['counts']['participants']} participants)")

    if args.check:
        unexpected, known = check_published(result, args.check)
        report = os.path.join(args.out, "check.md")
        with open(report, "w") as f:
            f.write(f"# Conferência contra {os.path.basename(args.check)}\n\n")
            f.write("## Divergências não explicadas\n\n")
            f.write("\n".join(f"- {m}" for m in unexpected) if unexpected else "Nenhuma.")
            f.write("\n\n## Erratas conhecidas da versão publicada\n\n")
            f.write("\n".join(f"- {m} — {why}" for m, why in known) if known else "Nenhuma.")
            f.write("\n")
        print(f"{len(unexpected)} divergência(s) não explicada(s), {len(known)} errata(s) conhecida(s) — ver {report}")
        sys.exit(1 if unexpected else 0)


if __name__ == "__main__":
    main()
