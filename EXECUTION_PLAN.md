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

---

# Execution Plan — MISRA Metrics Support

## Overview
This document outlines the step-by-step execution plan to add MISRA (Misra's Cognitive Complexity) metrics support to MetricsExporter.Net. MISRA metrics are cognitive complexity metrics that measure code complexity based on cognitive weights assigned to basic control structures (BCSs).

## MISRA Metrics Overview

The five MISRA metrics to be implemented:

1. **MC (Method Complexity):** Cognitive complexity of a method calculated by summing the cognitive weights of all Basic Control Structures (BCSs) within the method. Weights:
   - Sequence: 1
   - Branch: 2
   - Iteration: 3
   - Call: 2

2. **AC (Attribute Complexity):** Measures the complexity introduced by attributes (fields/properties) within a class. Considers the number and types of attributes.

3. **CLC (Class Complexity):** Overall complexity of a class, aggregating complexities from all methods and attributes within the class.

4. **CWC (Coupling Weight for a Class):** Evaluates the complexity due to inter-class interactions, calculated by summing the weights of calls to other classes' methods (including the call weight of 2).

5. **CC (Code Complexity):** Overall complexity of the codebase, aggregating complexities from all classes and their interactions.

---

## Phase 17: Data Model Extension

### 17.1 Create MISRA Metric Model
**Objective:** Create data model to support MISRA metrics alongside LOC and CK metrics

**Tasks:**
- Create new `MisraMetric` class in `Core/Models/MisraMetric.cs`:
  ```csharp
  public class MisraMetric
  {
      public string FilePath { get; set; } = string.Empty;
      public string ClassName { get; set; } = string.Empty;
      public string MethodName { get; set; } = string.Empty; // Empty for class-level
      public int MC { get; set; }  // Method Complexity (method-level only)
      public int AC { get; set; }  // Attribute Complexity (class-level)
      public int CLC { get; set; } // Class Complexity (class-level)
      public int CWC { get; set; } // Coupling Weight for a Class (class-level)
      public int CC { get; set; }  // Code Complexity (aggregated, optional)
  }
  ```
- Consider creating a unified metric export that can include all metric types
- Maintain backward compatibility with existing LOC and CK metrics

**Deliverables:**
- `MisraMetric` data model
- Backward compatibility maintained

---

## Phase 18: Enhanced Syntax Analysis for MISRA

### 18.1 Basic Control Structure (BCS) Detection
**Objective:** Identify and classify Basic Control Structures in method bodies

**Tasks:**
- Create `MisraSyntaxVisitor` or extend existing visitor:
  - Detect **Sequence** structures (sequential statements)
  - Detect **Branch** structures:
    - `if`, `else if`, `else`
    - `switch`, `case`
    - `?:` (ternary operator)
    - `??` (null-coalescing operator)
  - Detect **Iteration** structures:
    - `for`, `foreach`, `while`, `do-while`
  - Detect **Call** structures:
    - Method invocations
    - Constructor calls
    - Property accessors (if considered)
  - Track nesting levels (nested structures may have different weights)
- Handle edge cases:
  - Expression-bodied methods
  - Lambda expressions
  - LINQ queries
  - Async/await patterns
  - Null-conditional operators (`?.`)

**Deliverables:**
- BCS detection logic
- Syntax visitor for MISRA metrics

---

### 18.2 Method Complexity Calculator
**Objective:** Calculate MC (Method Complexity) for each method

**Tasks:**
- Create `McCalculator` class:
  - Parse method body to identify BCSs
  - Assign cognitive weights:
    - Sequence: 1 (each sequential statement)
    - Branch: 2 (each if, switch, ternary)
    - Iteration: 3 (each for, while, foreach)
    - Call: 2 (each method/constructor call)
  - Sum all weights to get MC
  - Handle nested structures:
    - Nested branches/iterations may require additional complexity
    - Consider nesting depth in calculation
  - Handle edge cases:
    - Empty methods (MC = 0)
    - Methods with only declarations (MC = 1)
    - Expression-bodied methods
    - Abstract methods (MC = 0 or N/A)

**Deliverables:**
- MC calculation logic
- Unit tests for various method structures

---

### 18.3 Attribute Complexity Calculator
**Objective:** Calculate AC (Attribute Complexity) for each class

**Tasks:**
- Create `AcCalculator` class:
  - Count all attributes (fields and properties) in a class
  - Assign complexity weights based on:
    - Simple types (int, string, bool): weight 1
    - Complex types (custom classes): weight 2
    - Collections/arrays: weight 2
    - Generic types: weight 2
  - Alternative approach: Simple count of attributes (AC = number of attributes)
  - Consider accessibility modifiers (public vs private)
  - Handle edge cases:
    - Static fields (may be excluded or counted separately)
    - Constants
    - Auto-properties vs full properties

**Deliverables:**
- AC calculation logic
- Type classification for attributes

---

### 18.4 Class Complexity Calculator
**Objective:** Calculate CLC (Class Complexity) for each class

**Tasks:**
- Create `ClcCalculator` class:
  - Aggregate method complexities:
    - Sum all MC values for methods in the class
  - Add attribute complexity:
    - Add AC value
  - Formula: `CLC = AC + Σ(MC for all methods)`
  - Handle edge cases:
    - Classes with no methods (CLC = AC)
    - Classes with no attributes (CLC = Σ(MC))
    - Abstract classes
    - Static classes

**Deliverables:**
- CLC calculation logic
- Integration with MC and AC calculators

---

### 18.5 Coupling Weight Calculator
**Objective:** Calculate CWC (Coupling Weight for a Class)

**Tasks:**
- Create `CwcCalculator` class:
  - Identify all external method calls made by the class:
    - Calls to methods in other classes
    - Constructor calls to other classes
    - Property accessors in other classes (optional)
  - For each external call:
    - Get the MC (Method Complexity) of the called method
    - Add call weight (2) to the method's MC
    - Sum: `CWC = Σ(MC_called_method + 2) for all external calls`
  - Handle edge cases:
    - Calls to system/library methods (may exclude or count with default MC)
    - Calls to methods in the same class (exclude)
    - Calls to base class methods (may count or exclude)
    - Virtual/override method calls
    - Interface method calls
  - Integration with class relationship graph (from CK metrics):
    - Use existing dependency tracking
    - Resolve method calls to their declaring classes

**Deliverables:**
- CWC calculation logic
- External call detection and resolution

---

### 18.6 Code Complexity Calculator
**Objective:** Calculate CC (Code Complexity) for the entire codebase

**Tasks:**
- Create `CcCalculator` class:
  - Aggregate all class complexities:
    - Sum all CLC values for all classes
  - Add coupling complexities:
    - Sum all CWC values (or a portion of them to avoid double-counting)
  - Formula options:
    - `CC = Σ(CLC for all classes) + Σ(CWC for all classes) / 2` (to avoid double-counting)
    - `CC = Σ(CLC for all classes)` (simpler approach)
    - `CC = Σ(CLC for all classes) + Σ(CWC for all classes)` (full complexity)
  - Handle edge cases:
    - Empty codebase
    - Single class codebase
    - Circular dependencies

**Deliverables:**
- CC calculation logic
- Aggregation service for codebase-level metrics

---

## Phase 19: MISRA Metrics Orchestration

### 19.1 MISRA Metrics Analysis Service
**Objective:** Orchestrate MISRA metrics calculation across all classes and methods

**Tasks:**
- Create `MisraMetricsAnalysisService`:
  - First pass: Calculate method-level metrics (MC) for all methods
  - Second pass: Calculate class-level metrics:
    - AC (Attribute Complexity) per class
    - CLC (Class Complexity) per class
    - CWC (Coupling Weight) per class
  - Third pass: Calculate codebase-level metrics:
    - CC (Code Complexity) for entire codebase
  - Reuse existing infrastructure:
    - `FileDiscoveryService` for file discovery
    - `RoslynParser` for syntax parsing
    - `ClassRelationshipBuilder` for class relationships (for CWC)
  - Optimize performance:
    - Cache method complexity calculations
    - Parallel processing where possible
    - Incremental updates if needed

**Deliverables:**
- MISRA metrics analysis service
- Three-phase analysis approach

---

### 19.2 Integration with Existing Analysis
**Objective:** Integrate MISRA metrics with existing LOC and CK metrics workflow

**Tasks:**
- Update `RootCommandHandler`:
  - Add `--misra` flag (optional) to enable MISRA metrics
  - Add `--misra-only` flag to calculate only MISRA metrics
  - Calculate MISRA metrics when flag is set
  - Export MISRA metrics to separate CSV file
- Update orchestration logic:
  - Maintain backward compatibility
  - Allow combination of metrics (e.g., `--loc --ck --misra`)
  - Handle performance implications of calculating all metrics
- Update logging:
  - Log MISRA metrics calculation progress
  - Include MISRA metrics in summary output

**Deliverables:**
- Integrated analysis workflow
- Optional MISRA metrics feature
- CLI options for MISRA metrics

---

## Phase 20: CSV Export Extension

### 20.1 Extended CSV Format for MISRA Metrics
**Objective:** Export MISRA metrics to CSV format

**Tasks:**
- Create separate CSV file (`metrics_misra.csv`):
  - Method-level entries: `FilePath,ClassName,MethodName,MC`
  - Class-level entries: `FilePath,ClassName,,AC,CLC,CWC`
  - Optional codebase-level entry: `,,,CC` (or separate file)
- Alternative: Combined format with all metrics:
  - `FilePath,ClassName,MethodName,MC,AC,CLC,CWC`
  - Method rows: MC populated, AC/CLC/CWC empty
  - Class rows: AC/CLC/CWC populated, MC empty
- Update `CsvExporter`:
  - Add `ExportMisraMetricsAsync` method
  - Handle optional codebase-level CC metric
  - Maintain backward compatibility with existing export methods

**Deliverables:**
- Extended CSV export functionality
- Documentation of MISRA CSV format

---

## Phase 21: CLI Enhancement

### 21.1 Add MISRA Metrics Options
**Objective:** Allow users to enable MISRA metrics calculation

**Tasks:**
- Add CLI options:
  - `--misra` (flag): Enable MISRA metrics calculation
  - `--misra-only` (flag): Calculate only MISRA metrics (skip LOC and CK)
- Update help text:
  - Document MISRA metrics
  - Explain what each metric measures
  - Provide usage examples
- Update validation logic:
  - Ensure at least one metric type is selected
  - Handle conflicting options appropriately
- Update command handler:
  - Process MISRA metrics flag
  - Integrate with existing metric calculation flow

**Deliverables:**
- CLI options for MISRA metrics
- Updated help documentation

---

## Phase 22: Testing & Validation

### 22.1 Unit Tests for MISRA Calculators
**Objective:** Validate correctness of each MISRA metric calculation

**Tasks:**
- Create test cases for each calculator:
  - **MC:** Methods with varying BCS types and nesting levels
  - **AC:** Classes with varying attribute counts and types
  - **CLC:** Classes with different method and attribute combinations
  - **CWC:** Classes with varying external call patterns
  - **CC:** Codebases of different sizes and complexities
- Test edge cases:
  - Empty methods/classes
  - Methods with only declarations
  - Classes with no attributes
  - Classes with no methods
  - Nested control structures
  - Complex LINQ queries
  - Async/await patterns
  - Expression-bodied members
  - Abstract classes and interfaces

**Deliverables:**
- Comprehensive unit test suite
- Test coverage report (>80% target)

---

### 22.2 Integration Tests
**Objective:** Validate end-to-end MISRA metrics workflow

**Tasks:**
- Create test scenarios:
  - Simple codebase with basic classes
  - Complex codebase with inheritance and coupling
  - Codebase with various control structures
  - Multi-file projects
  - Solution with multiple projects
- Validate CSV output format
- Compare results with known MISRA metrics tools (if available)
- Performance testing:
  - Ensure acceptable performance (< 5 minutes for 100k LOC)
  - Memory usage validation

**Deliverables:**
- Integration test suite
- Validation results
- Performance benchmarks

---

## Phase 23: Documentation & Examples

### 23.1 Update Documentation
**Objective:** Document MISRA metrics feature

**Tasks:**
- Update `README.md`:
  - Add MISRA metrics section
  - Explain each metric and its significance
  - Usage examples with `--misra` flag
- Update `COLLECTING_METRICS.md`:
  - Add MISRA metrics collection instructions
  - Example CSV outputs
  - Interpretation guidelines
  - Threshold recommendations (if applicable)
- Add XML documentation comments:
  - Document each calculator class
  - Explain metric formulas and weights
  - Provide calculation examples
- Update `EXECUTION_PLAN.md`:
  - Document completed phases
  - Update timeline estimates

**Deliverables:**
- Updated documentation
- Code comments
- Usage examples

---

### 23.2 Create Examples
**Objective:** Provide example code demonstrating MISRA metrics

**Tasks:**
- Create example C# projects:
  - Simple method (baseline MC)
  - Method with branches (if/else)
  - Method with iterations (for/while)
  - Method with nested structures
  - Class with various attribute types (AC examples)
  - Class with method complexity aggregation (CLC examples)
  - Classes with external dependencies (CWC examples)
- Document expected metrics for each example
- Include in documentation or separate examples folder
- Create comparison examples:
  - Low complexity vs high complexity methods
  - Well-structured vs poorly-structured classes

**Deliverables:**
- Example projects
- Expected metrics documentation
- Comparison examples

---

## Estimated Timeline

| Phase | Estimated Time |
|-------|----------------|
| Phase 17: Data Model Extension | 1-2 hours |
| Phase 18: Enhanced Syntax Analysis & Calculators | 16-20 hours |
| Phase 19: MISRA Metrics Orchestration | 4-6 hours |
| Phase 20: CSV Export Extension | 2-3 hours |
| Phase 21: CLI Enhancement | 1-2 hours |
| Phase 22: Testing & Validation | 8-10 hours |
| Phase 23: Documentation & Examples | 3-4 hours |
| **Total** | **35-47 hours** |

---

## Risk Mitigation

1. **BCS Detection Complexity:** Identifying all control structures accurately can be challenging. Start with basic structures, iterate to handle edge cases.
2. **Nesting Depth Calculation:** Nested structures may require special handling. Consider using a simplified approach initially.
3. **External Call Resolution:** Resolving method calls to their declaring classes requires semantic analysis. Leverage Roslyn semantic model.
4. **Performance:** MISRA metrics require detailed syntax analysis. Optimize BCS detection and caching.
5. **MC Calculation for External Methods:** When calculating CWC, we need MC for called methods. May need to analyze entire codebase first or use default values for external methods.
6. **Weight Assignment:** The cognitive weights (1, 2, 3, 2) may need adjustment based on validation. Document rationale and allow for future configuration.

---

## Success Criteria

- ✅ All five MISRA metrics (MC, AC, CLC, CWC, CC) calculated correctly
- ✅ MISRA metrics exported to CSV format
- ✅ Backward compatibility maintained with existing LOC and CK metrics
- ✅ CLI option to enable/disable MISRA metrics
- ✅ Comprehensive test coverage (>80%)
- ✅ Documentation updated with MISRA metrics information
- ✅ Performance acceptable (< 5 minutes for 100k LOC with MISRA metrics)
- ✅ Handles edge cases (nested structures, async/await, LINQ, etc.)
- ✅ BCS detection accurately identifies all control structures

---

## Implementation Priority

**High Priority (MVP):**
1. MC (Method Complexity) - Core metric, foundation for others
2. AC (Attribute Complexity) - Relatively straightforward
3. CLC (Class Complexity) - Aggregates MC and AC

**Medium Priority:**
4. CWC (Coupling Weight) - Requires external call resolution and MC for called methods

**Lower Priority (Complex):**
5. CC (Code Complexity) - Aggregates all class-level metrics, may be optional

**Recommendation:** Implement in priority order, validate each metric before moving to the next. Start with MC as it's the foundation for CLC and CWC.

---

## Dependencies on Existing Infrastructure

- **Reuse:** `FileDiscoveryService`, `RoslynParser`, `ClassRelationshipBuilder` (for CWC)
- **Extend:** `CsvExporter` for MISRA metrics export
- **Integrate:** `RootCommandHandler` for CLI options
- **Leverage:** Existing syntax visitor patterns from CK metrics implementation

---

## References

- Misra's Cognitive Complexity Paper: [users.exa.unicen.edu.ar](https://users.exa.unicen.edu.ar/~cmateos/files/journals/misra2012-iccsacognitive.pdf)
- Cognitive weights: Sequence=1, Branch=2, Iteration=3, Call=2
- BCS (Basic Control Structure) classification methodology

