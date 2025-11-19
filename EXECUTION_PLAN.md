# Execution Plan — MetricsExporter.Net

## Overview
This document outlines the step-by-step execution plan to complete the MetricsExporter.Net MVP according to the PRD requirements.

---

## Phase 1: Project Setup & Clean Architecture Foundation

### 1.1 Project Structure Setup
**Objective:** Establish Clean Architecture folder structure for extensibility (NFR04, NFR05)

**Tasks:**
- Create folder structure:
  ```
  src/MetricsExporter.Net/
    ├── Domain/           # Core business entities and interfaces
    ├── Application/      # Use cases and application logic
    ├── Infrastructure/   # External concerns (file I/O, Roslyn)
    └── Presentation/     # CLI interface
  ```
- Move existing files to appropriate layers
- Update namespaces accordingly

**Deliverables:**
- Clean folder structure
- Namespace organization

---

### 1.2 Dependencies & NuGet Packages
**Objective:** Add required packages for functionality

**Tasks:**
- Add to `.csproj`:
  - `Microsoft.CodeAnalysis.CSharp` (Roslyn for code analysis)
  - `System.CommandLine` (CLI argument parsing)
  - `Microsoft.Extensions.Logging.Console` (Logging infrastructure)
  - `CsvHelper` OR implement simple CSV writer (lightweight approach)

**Deliverables:**
- Updated `.csproj` with dependencies
- Restored packages

---

## Phase 2: Core Infrastructure

### 2.1 CLI Argument Parsing (RF04, NFR03)
**Objective:** Implement robust command-line interface

**Tasks:**
- Implement `System.CommandLine` setup:
  - `--path <directory>` (required): Target repository path
  - `--out <directory>` (optional, default: `./metrics`): Output directory
  - `--no-recursive` (flag): Disable recursive scanning
  - `--verbose` (flag): Enable detailed logging
  - `--help`: Display help message
- Validate path exists and is accessible
- Create command handler structure

**Deliverables:**
- Functional CLI with all parameters
- Help message implementation
- Input validation

---

### 2.2 File Discovery Service (RF01, RF05)
**Objective:** Traverse directories and identify `.cs` files

**Tasks:**
- Create `IFileDiscoveryService` interface (Domain layer)
- Implement `FileDiscoveryService` (Infrastructure layer):
  - Recursive directory traversal (unless `--no-recursive`)
  - Filter `.cs` files only
  - Handle exceptions (permissions, invalid paths)
  - Return collection of file paths
- Add logging for discovered files

**Deliverables:**
- File discovery service
- Error handling for file system operations

---

### 2.3 Logging Infrastructure (RF06)
**Objective:** Provide informative execution logs

**Tasks:**
- Create `ILogger` abstraction (Domain layer)
- Implement console logger (Infrastructure layer):
  - Support verbosity levels (normal, verbose)
  - Progress indicators (files processed, current file)
  - Error and warning messages
- Integrate with Microsoft.Extensions.Logging

**Deliverables:**
- Logging infrastructure
- Verbose mode support

---

## Phase 3: Code Analysis Engine

### 3.1 Roslyn Syntax Analysis Setup
**Objective:** Parse C# files into syntax trees

**Tasks:**
- Create `ICodeAnalyzer` interface (Domain layer)
- Implement `RoslynCodeAnalyzer` (Infrastructure layer):
  - Load `.cs` file content
  - Parse using `CSharpSyntaxTree.ParseText()`
  - Handle parse errors gracefully (log and skip)
  - Return `SyntaxTree` or null on failure

**Deliverables:**
- Code analyzer service
- Error handling for malformed code

---

### 3.2 Syntax Tree Traversal
**Objective:** Extract classes and methods from syntax trees

**Tasks:**
- Create custom `SyntaxWalker`:
  - Visit `ClassDeclarationSyntax` nodes
  - Visit `MethodDeclarationSyntax` nodes
  - Extract:
    - Class name
    - Method name
    - File path
    - Text span (line numbers) for LOC calculation
- Handle edge cases:
  - Nested classes
  - Partial classes
  - Interface methods
  - Properties/constructors (if needed)

**Deliverables:**
- Syntax visitor implementation
- Data extraction for classes and methods

---

## Phase 4: LOC Calculation Logic

### 4.1 LOC Counter Implementation (RF02, NFR06)
**Objective:** Count effective lines of code excluding comments and empty lines

**Tasks:**
- Create `ILocCalculator` interface (Domain layer)
- Implement `LocCalculator` (Application layer):
  - Accept text span and source text
  - Exclude:
    - Single-line comments (`//`)
    - Multi-line comments (`/* */`)
    - XML documentation comments (`///`)
    - Empty lines (whitespace only)
  - Count remaining lines
  - Handle edge cases (comments at end of code lines)

**Deliverables:**
- LOC calculation logic
- Deterministic counting (NFR06)

---

### 4.2 LOC Aggregation (RF02)
**Objective:** Calculate LOC at file, class, and method levels

**Tasks:**
- Create aggregation service:
  - **Method-level LOC:** Direct calculation from method span
  - **Class-level LOC:** Sum of all method LOCs in class
  - **File-level LOC:** Sum of all class LOCs in file (or direct file calculation)
- Create data model:
  ```csharp
  public class LocMetric
  {
      public string FilePath { get; set; }
      public string ClassName { get; set; }
      public string MethodName { get; set; }
      public int Loc { get; set; }
  }
  ```

**Deliverables:**
- LOC aggregation logic
- Data model for metrics

---

## Phase 5: CSV Export

### 5.1 CSV Data Model (RF03)
**Objective:** Define structure for CSV output

**Tasks:**
- Create `MetricRecord` class:
  - `FilePath` (string)
  - `ClassName` (string, empty for file-level)
  - `MethodName` (string, empty for class/file-level)
  - `LOC` (int)
- Handle special cases:
  - File-level entries (ClassName = "", MethodName = "")
  - Class-level entries (MethodName = "")

**Deliverables:**
- Metric record model

---

### 5.2 CSV Writer Service (RF03)
**Objective:** Generate `metrics_loc.csv` file

**Tasks:**
- Create `ICsvExporter` interface (Domain layer)
- Implement `CsvExporter` (Infrastructure layer):
  - Write CSV header: `FilePath,ClassName,MethodName,LOC`
  - Write metric records
  - Handle special characters (escape quotes, commas)
  - Ensure output directory exists
  - Create file: `metrics_loc.csv` in output directory

**Deliverables:**
- CSV export functionality
- Proper file handling

---

## Phase 6: Integration & Orchestration

### 6.1 Main Orchestration Logic
**Objective:** Wire up all components

**Tasks:**
- Update `Program.cs`:
  - Parse CLI arguments
  - Initialize services (dependency injection or manual)
  - Orchestrate workflow:
    1. Discover `.cs` files
    2. For each file:
       - Parse with Roslyn
       - Extract classes and methods
       - Calculate LOC for each method
       - Aggregate class and file LOC
    3. Collect all metrics
    4. Export to CSV
  - Handle errors gracefully
  - Return appropriate exit codes

**Deliverables:**
- Complete end-to-end workflow
- Error handling

---

### 6.2 Performance Optimization (NFR01)
**Objective:** Ensure < 2 minutes for 100k LOC

**Tasks:**
- Implement parallel file processing (where thread-safe):
  - Use `Parallel.ForEach` for file analysis
  - Ensure thread-safe collection of results
- Optimize Roslyn usage:
  - Reuse `CSharpParseOptions`
  - Efficient syntax tree traversal
- Memory management:
  - Stream large files if needed
  - Dispose resources properly

**Deliverables:**
- Performance optimizations
- Benchmarking results

---

## Phase 7: Testing, Validation & Documentation

### 7.1 Testing
**Objective:** Validate functionality against PRD

**Tasks:**
- Create test scenarios:
  - Simple class with methods
  - Nested classes
  - Files with various comment types
  - Empty files
  - Files with parse errors
- Manual testing:
  - Test CLI arguments
  - Verify CSV output format
  - Test recursive vs non-recursive
  - Test on different OS (Windows, Linux, macOS)

**Deliverables:**
- Test cases
- Validation results

---

### 7.2 Documentation (NFR03)
**Objective:** Provide clear usage instructions

**Tasks:**
- Update `README.md`:
  - Installation instructions
  - Usage examples
  - Command-line options
  - Output format description
  - Example CSV output
- Add inline code documentation (XML comments)

**Deliverables:**
- Complete README
- Code documentation

---

## Phase 8: Final Polish

### 8.1 Code Quality
**Objective:** Ensure Clean Architecture and best practices (NFR05)

**Tasks:**
- Code review checklist:
  - SOLID principles
  - Proper separation of concerns
  - Error handling
  - Resource disposal
  - Naming conventions
- Run code analysis tools
- Fix any warnings

**Deliverables:**
- Clean, maintainable code

---

### 8.2 Final Validation
**Objective:** Verify all PRD requirements met

**Tasks:**
- Checklist against PRD:
  - ✅ RF01: Source code analysis
  - ✅ RF02: LOC calculation (file/class/method)
  - ✅ RF03: CSV export
  - ✅ RF04: CLI interface
  - ✅ RF05: Recursive analysis
  - ✅ RF06: Logging
  - ✅ NFR01: Performance
  - ✅ NFR02: Portability
  - ✅ NFR03: Usability
  - ✅ NFR04: Extensibility
  - ✅ NFR05: Maintainability
  - ✅ NFR06: Reliability

**Deliverables:**
- PRD compliance verification
- Ready for release

---

## Estimated Timeline

| Phase | Estimated Time |
|-------|----------------|
| Phase 1: Setup | 1-2 hours |
| Phase 2: Infrastructure | 3-4 hours |
| Phase 3: Code Analysis | 4-5 hours |
| Phase 4: LOC Calculation | 3-4 hours |
| Phase 5: CSV Export | 2-3 hours |
| Phase 6: Integration | 3-4 hours |
| Phase 7: Testing & Docs | 2-3 hours |
| Phase 8: Polish | 1-2 hours |
| **Total** | **19-27 hours** |

---

## Risk Mitigation

1. **Roslyn Complexity:** Start with simple syntax tree traversal, iterate
2. **Performance:** Profile early, optimize bottlenecks
3. **Edge Cases:** Test with various code patterns (nested classes, partial classes)
4. **Cross-platform:** Test on all target platforms early

---

## Success Criteria

- ✅ All functional requirements (RF01-RF06) implemented
- ✅ All non-functional requirements (NFR01-NFR06) met
- ✅ CSV output matches PRD specification
- ✅ CLI is intuitive and well-documented
- ✅ Code follows Clean Architecture principles
- ✅ Performance target met (< 2 min for 100k LOC)

