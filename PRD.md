# Product Requirement Definition (PRD) — MetricsExporter.Net
## 1. Overview

### 1.1. Product Name
**MetricsExporter.Net**

### 1.2. Summary
**MetricsExporter.Net** is a .NET command-line tool designed to extract software metrics from local .NET repositories.  
In this initial version (MVP), the system focuses exclusively on calculating **Lines of Code (LOC)** to support maintainability analysis and codebase characterization.

### 1.3. Product Objective
Provide a lightweight, extensible, and automated mechanism for extracting standardized code metrics, starting with **LOC**, paving the way for future inclusion of structural metrics (CK, MISRA).

---

## 2. Product Scope

### 2.1. In-Scope (Current Version)
- Reading and analyzing **.NET projects and solutions**.
- Calculating **Lines of Code (LOC)** at:
    - Class level
    - Method level
    - File level
- Exporting results into a **CSV file**.
- Command-line execution with no dependency on IDEs.
- Local recursive scanning of multiple repositories.

### 2.2. Out of Scope
- IDE integrations (VS Code, Visual Studio, Rider)
- CI/CD pipeline automation
- Additional metrics (CK, MISRA, etc.)
- Graphical dashboards or visual reports

---

## 3. Functional Requirements

| ID | Requirement | Description |
|----|-------------|-------------|
| RF01 | Source Code Analysis | Traverse repository directories and identify valid `.cs` files. |
| RF02 | LOC Calculation | Count effective lines of code per file, class, and method, excluding comments and empty lines. |
| RF03 | CSV Export | Generate a `metrics_loc.csv` file with fields: `FilePath`, `ClassName`, `MethodName`, `LOC`. |
| RF04 | CLI Interface | The tool must accept terminal parameters, e.g.: `metrics-exporter --path ./repo --out ./out`. |
| RF05 | Recursive Analysis | Support scanning subdirectories recursively. |
| RF06 | Logging | Tool must display informative logs through execution. |

---

## 4. Non-Functional Requirements

| ID | Category | Description |
|----|----------|-------------|
| NFR01 | Performance | Should analyze medium repositories (up to 100k LOC) in under 2 minutes. |
| NFR02 | Portability | Must run on Windows, Linux, and macOS with .NET 8+. |
| NFR03 | Usability | CLI must be intuitive, with clear help messages (`--help`). |
| NFR04 | Extensibility | Architecture must support easy addition of new metrics. |
| NFR05 | Maintainability | Code must follow Clean Architecture and C# best practices. |
| NFR06 | Reliability | LOC counting must be deterministic and consistent across runs. |

---

## 5. Expected Output Structure

**File:** `metrics_loc.csv`

**CSV Format:**
