# Documentation

Documentation for **MetricsExporter.Net**, a command-line tool that extracts software metrics from .NET (C#) code and exports them to CSV.

## Using the tool

| Document | Contents |
|----------|----------|
| [../README.md](../README.md) | Overview, installation, CLI options, and CSV output formats |
| [COLLECTING_METRICS.md](COLLECTING_METRICS.md) | Building and running the tool, advanced scenarios (CI, before/after comparisons, analysis with Python), troubleshooting |

## Metric reference

Each page explains what the metric measures, gives its formal definition, works through an example in modern C#, and has a **"How this tool computes it"** section that describes exactly what the values in the CSV mean.

| Document | Metrics | CLI flag | Output file | Level |
|----------|---------|----------|-------------|-------|
| [loc.md](loc.md) | Lines of Code | `--loc` | `metrics_loc.csv` | method, class, file |
| [cyclomatic.md](cyclomatic.md) | Cyclomatic Complexity | `--cyclomatic` / `--cc` | `metrics_cyclomatic.csv` | method, class, file |
| [ck.md](ck.md) | WMC, RFC, LCOM, CBO, DIT, NOC | `--ck` | `metrics_ck.csv` | class |
| [misra.md](misra.md) | MC, AC, CLC, CWC, CoC | `--misra` | `metrics_misra.csv` | method, class, codebase |

## Which types are measured

Every metric that works per class measures **classes, records (`record`, `record class`), structs and record structs**, and all four CSV files agree on that scope:

- a `record` is a class: it has a base class, so it enters DIT, NOC and CoC like any class;
- a `struct` or `record struct` is a value type with no base class: DIT = NOC = 0, and it enters every other metric;
- the positional parameters of a record (`record Person(string Name)`) are public properties, so they count as attributes for AC and as instance variables for LCOM;
- interfaces and enums are not measured; default interface members are left out too;
- members synthesized by the compiler (`Equals`, `ToString`, `Deconstruct`...) are not counted, since nobody wrote them.

A method belongs to the **nearest** enclosing type: the methods of a record nested in a class are measured for the record, never for the class. `metrics_ck.csv` and `metrics_misra.csv` have a `Kind` column (`class`, `record`, `struct`, `record struct`) to filter by type.

`--classes-only` restores the scope of v1: only classes get rows. Methods of records and structs then still get rows in `metrics_cyclomatic.csv` and `metrics_loc.csv`, with an empty `ClassName`.

**Which metric for which question?**

- *How big is it?* → [LOC](loc.md)
- *How many paths must be tested?* → [Cyclomatic Complexity](cyclomatic.md)
- *How is the object-oriented design structured (coupling, cohesion, inheritance)?* → [CK](ck.md)
- *How hard is it to understand?* → [Misra](misra.md)

The metric families overlap: MC and cyclomatic complexity both describe control flow, and CLC and WMC both aggregate complexity per class. Expect them to be correlated, and use them together rather than in isolation.

## Research

| Document | Contents |
|----------|----------|
| [research/reproducing-dissertation.md](research/reproducing-dissertation.md) | How the data for the master's dissertation case study was collected and summarised |
| [research/descricao-ferramenta.md](research/descricao-ferramenta.md) | Description of the tool for the dissertation (in Portuguese): motivation, technical overview, suggested paragraphs |
