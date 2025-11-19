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

---

# Execution Plan — CK Metrics Support

## Overview
This document outlines the step-by-step execution plan to add Chidamber and Kemerer (CK) metrics support to MetricsExporter.Net. CK metrics are object-oriented design metrics that measure various aspects of class design quality.

## CK Metrics Overview

The six CK metrics to be implemented:

1. **WMC (Weighted Methods per Class):** Sum of complexity of all methods in a class. For simplicity, we'll use method count (unweighted) or cyclomatic complexity.
2. **DIT (Depth of Inheritance Tree):** Maximum depth of a class in the inheritance hierarchy (distance from root class).
3. **NOC (Number of Children):** Count of immediate subclasses of a class.
4. **CBO (Coupling Between Objects):** Number of classes to which a class is coupled (uses or is used by).
5. **RFC (Response For a Class):** Number of methods that can be invoked in response to a message (methods in class + methods called by those methods).
6. **LCOM (Lack of Cohesion of Methods):** Measures the dissimilarity of methods in a class based on instance variable usage.

---

## Phase 9: Data Model Extension

### 9.1 Extend Metric Model
**Objective:** Create data model to support CK metrics alongside LOC metrics

**Tasks:**
- Create new `CkMetric` class:
  ```csharp
  public class CkMetric
  {
      public string FilePath { get; set; } = string.Empty;
      public string ClassName { get; set; } = string.Empty;
      public int WMC { get; set; }  // Weighted Methods per Class
      public int DIT { get; set; }  // Depth of Inheritance Tree
      public int NOC { get; set; }  // Number of Children
      public int CBO { get; set; }  // Coupling Between Objects
      public int RFC { get; set; }  // Response For a Class
      public int LCOM { get; set; } // Lack of Cohesion of Methods
  }
  ```
- Consider creating unified `Metric` class that includes both LOC and CK metrics
- Or keep separate models and aggregate in export layer

**Deliverables:**
- Extended or new metric data models
- Backward compatibility with existing LOC metrics

---

## Phase 10: Enhanced Syntax Analysis

### 10.1 Extended Syntax Visitor
**Objective:** Collect additional information needed for CK metrics calculation

**Tasks:**
- Extend `SyntaxVisitor` or create `CkSyntaxVisitor`:
  - Track class inheritance relationships:
    - Base class names
    - Derived class names
    - Build inheritance tree
  - Track method information:
    - Method names and signatures
    - Method complexity (for WMC)
    - Method calls within methods (for RFC)
  - Track class dependencies:
    - Type references (for CBO)
    - Field declarations with types
    - Method parameters with types
  - Track instance variable usage:
    - Fields/properties accessed by each method (for LCOM)
- Handle edge cases:
  - Multiple inheritance (interfaces)
  - Partial classes
  - Generic types
  - Nested classes

**Deliverables:**
- Enhanced syntax visitor with CK-relevant data extraction
- Data structures to represent class relationships

---

### 10.2 Class Relationship Builder
**Objective:** Build cross-file class relationship graph

**Tasks:**
- Create `ClassRelationshipBuilder` service:
  - Build inheritance tree across all files:
    - Map class → base class
    - Map class → derived classes
  - Build dependency graph:
    - Map class → classes it depends on (uses)
    - Map class → classes that depend on it (used by)
  - Handle namespace resolution:
    - Fully qualified names
    - Using statements
    - Aliases
- Store relationships in memory-efficient structure:
  - Dictionary-based graph
  - Support for partial analysis (per-file vs. full solution)

**Deliverables:**
- Class relationship graph builder
- Efficient data structures for relationships

---

## Phase 11: CK Metrics Calculators

### 11.1 WMC Calculator (Weighted Methods per Class)
**Objective:** Calculate number of methods in a class

**Tasks:**
- Create `WmcCalculator`:
  - Count all methods in a class:
    - Instance methods
    - Static methods
    - Constructors
    - Properties (getters/setters count as methods)
  - Optional: Weight by cyclomatic complexity
  - Handle edge cases:
    - Abstract methods
    - Interface implementations
    - Partial class methods

**Deliverables:**
- WMC calculation logic
- Unit tests for various class structures

---

### 11.2 DIT Calculator (Depth of Inheritance Tree)
**Objective:** Calculate inheritance depth of a class

**Tasks:**
- Create `DitCalculator`:
  - Traverse inheritance chain upward:
    - Start from target class
    - Follow base class references
    - Count levels until `System.Object` (or no base class)
  - Handle multiple inheritance (interfaces):
    - Count only class inheritance (not interfaces)
    - Or count interface depth separately
  - Cache results for performance
  - Handle edge cases:
    - Classes without explicit base class (implicitly inherit Object)
    - Circular references (should not occur, but handle gracefully)

**Deliverables:**
- DIT calculation logic
- Efficient traversal algorithm

---

### 11.3 NOC Calculator (Number of Children)
**Objective:** Count immediate subclasses of a class

**Tasks:**
- Create `NocCalculator`:
  - Use class relationship graph:
    - For each class, count direct children
    - Only count immediate subclasses (not grandchildren)
  - Handle edge cases:
    - Classes with no children (NOC = 0)
    - Abstract classes (may have many children)
    - Sealed classes (NOC = 0)

**Deliverables:**
- NOC calculation logic
- Integration with class relationship graph

---

### 11.4 CBO Calculator (Coupling Between Objects)
**Objective:** Count classes coupled to a given class

**Tasks:**
- Create `CboCalculator`:
  - Analyze class dependencies:
    - Field types (instance variables)
    - Method parameter types
    - Method return types
    - Local variable types (optional, less common)
    - Type references in method bodies
  - Count unique classes:
    - Exclude primitive types
    - Exclude system types (optional, configurable)
    - Count both incoming and outgoing coupling
  - Handle edge cases:
    - Generic types (count generic type, not parameterized instances)
    - Arrays and collections
    - Nullable types

**Deliverables:**
- CBO calculation logic
- Type reference extraction from syntax tree

---

### 11.5 RFC Calculator (Response For a Class)
**Objective:** Count methods that can be invoked in response to a message

**Tasks:**
- Create `RfcCalculator`:
  - Count methods in the class (M)
  - Count methods called by class methods (R):
    - Extract method calls from method bodies
    - Resolve method calls to their declaring classes
    - Count unique methods
  - RFC = M + R
  - Handle edge cases:
    - Recursive calls
    - Virtual method calls (count all possible targets)
    - Extension methods
    - Lambda expressions and delegates

**Deliverables:**
- RFC calculation logic
- Method call extraction from syntax trees

---

### 11.6 LCOM Calculator (Lack of Cohesion of Methods)
**Objective:** Measure cohesion based on instance variable usage

**Tasks:**
- Create `LcomCalculator`:
  - For each method, identify accessed instance variables:
    - Field reads
    - Field writes
    - Property accesses
  - Calculate LCOM using formula:
    - LCOM = P - Q (if P > Q, else 0)
    - P = number of method pairs that don't share instance variables
    - Q = number of method pairs that share instance variables
  - Alternative: LCOM* (Henderson-Sellers variant)
  - Handle edge cases:
    - Static methods (exclude from calculation)
    - Methods with no instance variable access
    - Properties (getters/setters)

**Deliverables:**
- LCOM calculation logic
- Instance variable access tracking

---

## Phase 12: CK Metrics Orchestration

### 12.1 CK Metrics Analysis Service
**Objective:** Orchestrate CK metrics calculation across all classes

**Tasks:**
- Create `CkMetricsAnalysisService`:
  - First pass: Build class relationship graph (all files)
  - Second pass: Calculate CK metrics for each class:
    - Use relationship graph for DIT, NOC
    - Use syntax analysis for WMC, CBO, RFC, LCOM
  - Handle multi-file analysis:
    - Process all files to build complete graph
    - Then calculate metrics per class
  - Optimize performance:
    - Cache intermediate results
    - Parallel processing where possible
    - Incremental updates if needed

**Deliverables:**
- CK metrics analysis service
- Two-phase analysis approach

---

### 12.2 Integration with Existing Analysis
**Objective:** Integrate CK metrics with existing LOC metrics workflow

**Tasks:**
- Update `MetricsAnalysisService`:
  - Add CK metrics calculation alongside LOC
  - Maintain backward compatibility
  - Option: Make CK metrics optional (flag)
- Update orchestration in `RootCommandHandler`:
  - Add `--ck-metrics` flag (optional)
  - Calculate CK metrics when flag is set
  - Export both LOC and CK metrics

**Deliverables:**
- Integrated analysis workflow
- Optional CK metrics feature

---

## Phase 13: CSV Export Extension

### 13.1 Extended CSV Format
**Objective:** Export CK metrics to CSV alongside or separate from LOC metrics

**Tasks:**
- Option A: Separate CSV file (`metrics_ck.csv`):
  - Header: `FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM`
  - One row per class
- Option B: Combined CSV file:
  - Extend existing format with CK columns
  - Only populate CK metrics at class level
  - LOC metrics remain at method/class/file levels
- Update `CsvExporter`:
  - Add method to export CK metrics
  - Handle optional CK metrics columns
  - Maintain backward compatibility

**Deliverables:**
- Extended CSV export functionality
- Documentation of new CSV format

---

## Phase 14: CLI Enhancement

### 14.1 Add CK Metrics Option
**Objective:** Allow users to enable CK metrics calculation

**Tasks:**
- Add CLI option:
  - `--ck-metrics` (flag): Enable CK metrics calculation
  - `--ck-only` (flag): Calculate only CK metrics (skip LOC)
- Update help text
- Update validation logic
- Update command handler

**Deliverables:**
- CLI option for CK metrics
- Updated help documentation

---

## Phase 15: Testing & Validation

### 15.1 Unit Tests for CK Calculators
**Objective:** Validate correctness of each CK metric calculation

**Tasks:**
- Create test cases for each calculator:
  - **WMC:** Classes with varying method counts
  - **DIT:** Inheritance hierarchies of different depths
  - **NOC:** Classes with 0, 1, and multiple children
  - **CBO:** Classes with varying coupling levels
  - **RFC:** Classes with different method call patterns
  - **LCOM:** Classes with high/low cohesion
- Test edge cases:
  - Empty classes
  - Abstract classes
  - Interfaces
  - Partial classes
  - Nested classes
  - Generic classes

**Deliverables:**
- Comprehensive unit test suite
- Test coverage report

---

### 15.2 Integration Tests
**Objective:** Validate end-to-end CK metrics workflow

**Tasks:**
- Create test scenarios:
  - Simple inheritance hierarchy
  - Complex multi-file project
  - Solution with multiple projects
  - Projects with circular dependencies (if possible)
- Validate CSV output format
- Compare results with known CK metrics tools (if available)

**Deliverables:**
- Integration test suite
- Validation results

---

## Phase 16: Documentation & Examples

### 16.1 Update Documentation
**Objective:** Document CK metrics feature

**Tasks:**
- Update `README.md`:
  - Add CK metrics section
  - Explain each metric
  - Usage examples
- Update `COLLECTING_METRICS.md`:
  - Add CK metrics collection instructions
  - Example CSV outputs
  - Interpretation guidelines
- Add XML documentation comments:
  - Document each calculator
  - Explain metric formulas
  - Provide examples

**Deliverables:**
- Updated documentation
- Code comments

---

### 16.2 Create Examples
**Objective:** Provide example code demonstrating CK metrics

**Tasks:**
- Create example C# projects:
  - Simple class (baseline metrics)
  - Inheritance hierarchy (DIT, NOC examples)
  - Coupled classes (CBO examples)
  - Cohesive vs. non-cohesive classes (LCOM examples)
- Document expected metrics for each example
- Include in documentation or separate examples folder

**Deliverables:**
- Example projects
- Expected metrics documentation

---

## Estimated Timeline

| Phase | Estimated Time |
|-------|----------------|
| Phase 9: Data Model Extension | 1-2 hours |
| Phase 10: Enhanced Syntax Analysis | 6-8 hours |
| Phase 11: CK Metrics Calculators | 12-16 hours |
| Phase 12: CK Metrics Orchestration | 4-6 hours |
| Phase 13: CSV Export Extension | 2-3 hours |
| Phase 14: CLI Enhancement | 1-2 hours |
| Phase 15: Testing & Validation | 6-8 hours |
| Phase 16: Documentation & Examples | 3-4 hours |
| **Total** | **35-49 hours** |

---

## Risk Mitigation

1. **Complexity of CBO and RFC:** These require deep syntax analysis. Start with simplified versions, iterate.
2. **Performance:** CK metrics require full solution analysis. Optimize relationship graph building.
3. **Namespace Resolution:** Complex in C#. Use Roslyn semantic model for accurate resolution.
4. **Partial Classes:** Handle classes split across multiple files correctly.
5. **Generic Types:** Ensure proper handling of generic type parameters and constraints.

---

## Success Criteria

- ✅ All six CK metrics (WMC, DIT, NOC, CBO, RFC, LCOM) calculated correctly
- ✅ CK metrics exported to CSV format
- ✅ Backward compatibility maintained with existing LOC metrics
- ✅ CLI option to enable/disable CK metrics
- ✅ Comprehensive test coverage (>80%)
- ✅ Documentation updated with CK metrics information
- ✅ Performance acceptable (< 5 minutes for 100k LOC with CK metrics)
- ✅ Handles edge cases (partial classes, generics, interfaces, etc.)

---

## Implementation Priority

**High Priority (MVP):**
1. WMC (simplest)
2. DIT (straightforward inheritance traversal)
3. NOC (uses same data as DIT)

**Medium Priority:**
4. CBO (requires type analysis)
5. RFC (requires method call extraction)

**Lower Priority (Complex):**
6. LCOM (most complex, requires instance variable tracking)

**Recommendation:** Implement in priority order, validate each metric before moving to the next.

