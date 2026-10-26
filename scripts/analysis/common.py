"""Loading of a results directory, shared by cbtam_analysis.py and generate_index.py.

A results directory holds one folder per participant, named p{n}-{position}-{level},
each with the CSVs exported by the tool. Every rule about which rows and columns
count (file-level rows, LLOC, CoC) lives here, so both scripts report the same numbers.

Standard library only (Python >= 3.9).
"""
import csv
import math
import os
import re
import statistics

LEVELS = {"jr": "Beginner", "pl": "Mid-Level", "sn": "Senior", "esp": "Expert"}
POSITIONS = {"dev": "Developer", "arch": "Architect"}


# --------------------------------------------------------------------------- statistics

def pearson(xs, ys):
    mx, my = statistics.fmean(xs), statistics.fmean(ys)
    num = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    dx = math.sqrt(sum((x - mx) ** 2 for x in xs))
    dy = math.sqrt(sum((y - my) ** 2 for y in ys))
    return float("nan") if dx == 0 or dy == 0 else num / (dx * dy)


# --------------------------------------------------------------------------- loading

def read_csv(path):
    if not os.path.exists(path):
        return []
    with open(path, newline="") as f:
        return list(csv.DictReader(f))


def last_per_key(rows, keys):
    """One record per key, keeping the last occurrence (rule of the CBTAM analysis).

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
        "position_code": pos,
        "level_code": lvl,
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
        # CoC since INC-20; older collections only have the CC column
        "coc": int(num(codebase_rows[0], "CoC" if "CoC" in codebase_rows[0] else "CC")) if codebase_rows else 0,
    }


def load_results(results_dir, exclude=()):
    participants = []
    for entry in os.listdir(results_dir):
        path = os.path.join(results_dir, entry)
        if os.path.isdir(path):
            p = load_participant(path)
            if p and p["pid"] not in exclude:
                participants.append(p)
    return sorted(participants, key=lambda p: p["num"])
