# Lines of Code (LOC)

**Lines of Code** is the oldest and most widely used size metric. It is used both to estimate development effort and cost (it is the main input of Boehm's COCOMO model) and as a size *proxy* in empirical software-engineering studies. Unlike the other metrics this tool collects, LOC says nothing about logical, structural, or cognitive complexity. It measures **volume** only, which is why it is mostly used alongside complexity metrics, either as a control variable or to normalise them.

This page explains the metric, shows a worked example in modern C#, and states how **Codimetry** counts lines. The definitions follow the master's dissertation by Silva Junior (2026), Section 2.3.

## Collecting the metric

```bash
dotnet run --project src/Codimetry/Codimetry.csproj -- --path ./src --loc
```

This produces `metrics_loc.csv`, which has rows at three levels:

```csv
FilePath,ClassName,MethodName,LOC,LLOC,PhysicalLOC
src/Calculator.cs,Calculator,Add,5,3,       <- method or constructor
src/Calculator.cs,Calculator,,5,4,          <- class
src/Calculator.cs,,,5,4,10                  <- file
```

Three counts are written side by side. `LLOC` (logical LOC) and `PhysicalLOC` follow Park (1992); `LOC` is the tool's original count, kept for comparison with earlier results. `PhysicalLOC` is only filled on file rows.

See [COLLECTING_METRICS.md](COLLECTING_METRICS.md) for the remaining options.

---

## Definition

Despite its apparent simplicity, LOC has no single agreed definition. Park (1992), in a Software Engineering Institute report, proposed a framework that separates two measures that are often confused:

- **Physical LOC (physical SLOC)** counts every physical line of the source file, including blank lines, comments, and declaration lines. It depends on formatting conventions and coding style.
- **Logical LOC (LLOC)** counts only executable statements (in C-family languages, those terminated by `;`). It ignores blank lines, comments, and block-delimiting braces, so it is robust to purely stylistic variation.

Any comparison based on LOC must say which of the two was used.

## Interpretation

More LOC means more code to read, test, and maintain. At the method level, LOC is a useful maintainability signal: Chowdhury, Uddin & Holmes (2022) mined about 785 thousand Java methods and found that methods kept under 24 lines needed less maintenance effort later. LOC should **not** be used on its own as a measure of quality or productivity. It varies with language expressiveness and formatting, and it can be inflated simply by splitting statements across lines.

## Example

```csharp
public class Calculator
{
    // Adds two integers
    public int Add(int a, int b)
    {

        int result = a + b;
        return result;
    }
}
```

- **Physical LOC = 10**: all ten lines, including the comment, the blank line, and the braces.
- **Logical LOC = 4**: the class declaration, the method declaration, the assignment, and the `return`.

## How this tool computes it

### LLOC (logical LOC)

Computed on the syntax tree, so formatting has no effect: a statement split over four lines counts 1, and two statements on one line count 2. Counted:

- type declarations (class, record, struct, interface, enum, delegate);
- method, constructor, destructor and operator declarations;
- field declarations (once per declaration: `int a, b;` counts 1) and enum members;
- property, indexer and event declarations; auto-accessors (`get; set;`) are not counted, accessors with a body (`get { ... }` or `get => ...`) are;
- every statement, including local functions, statements inside block lambdas and top-level statements.

Not counted: braces and blocks, empty statements, `using` directives, namespace declarations, attributes, comments and blank lines. A labeled statement counts once.

Each row is measured on its own syntax node: a method row on the method declaration, a class row on the whole class (fields, properties and nested types included), and a file row on the whole file (records, structs, enums and interfaces included). A DTO made only of auto-properties therefore has LLOC > 0. Every analyzed file gets a file row, even one without classes. The project size used in the analysis is the sum of the file rows' LLOC.

### PhysicalLOC

Every line of the file, including blank lines and comments. The empty line after the final line break is not counted, so a file ending in a newline is not one line longer. Only file rows carry this value.

### LOC (original count)

The tool counts **non-blank, non-comment physical lines**. That sits between the two definitions above: braces and declaration lines are counted, as in physical LOC, while blank lines and comments are not, as in logical LOC.

A line is **skipped** if it is:

- empty or whitespace only;
- a `//` comment or a `///` XML documentation comment;
- entirely inside a `/* ... */` block comment.

A line that mixes code and a comment, such as `return x; // done` or `a * b; /* trivial */`, **is counted**.

What gets counted at each level:

- **Method and constructor rows** count the lines from the first line of the declaration to its closing brace (or the final `;` of an expression-bodied member). Comments placed *above* the method, including XML documentation, belong to the line before the declaration and are **not** counted. Attributes such as `[HttpGet]` are part of the declaration and **are** counted. An abstract method has no body, so its row counts the single line of its declaration.
- **Class rows** are the **sum of the class's methods and constructors**. Records and structs get class rows too, and a method belongs to the nearest enclosing type (see [which types are measured](README.md#which-types-are-measured)). Fields, properties, nested type declarations, and the class's own declaration and braces are not included.
- **File rows** are the sum of the classes in the file. `using` directives and namespace declarations are not included. A file without classes has LOC = 0.

Applied to the example above, the tool reports:

- `Add`: **5**. That is the signature, `{`, the assignment, the `return`, and `}`. The comment above the method and the blank line are excluded.
- `Calculator` (class row): **5**, the sum of its only method.

Because properties and fields are excluded, a class made only of auto-properties (a DTO, for example) has LOC = 0. Use LOC to measure **behavioural code**; use LLOC for size.

## Limitations

- **Language and style dependent.** The same functionality can take very different numbers of lines depending on language expressiveness. This is what motivated language-independent size measures such as Function Point Analysis (Albrecht, 1979).
- **Easy to manipulate.** Splitting statements across lines inflates LOC without changing behaviour.
- **Size, not complexity.** At the method level, LOC is not redundant with [cyclomatic complexity](cyclomatic.md) (Landman et al., 2016), so use the two together.
- **Line-based comment detection (LOC only).** The original LOC detects comments line by line, without a C# lexer. A `//` or `/*` inside a string literal can therefore be mistaken for a comment. LLOC and PhysicalLOC do not have this problem.

## References

- PARK, R. E. *Software Size Measurement: A Framework for Counting Source Statements*. CMU/SEI-92-TR-020, Software Engineering Institute, 1992.
- BOEHM, B. W. *Software Engineering Economics*. Prentice-Hall, 1981.
- ALBRECHT, A. J. *Measuring application development productivity*. Proceedings of the Joint SHARE/GUIDE/IBM Application Development Symposium, p. 83–92, 1979.
- CHOWDHURY, S. A.; UDDIN, G.; HOLMES, R. *An empirical study on maintainable method size in Java*. Proceedings of the 19th International Conference on Mining Software Repositories (MSR), p. 252–264, 2022.
- LANDMAN, D. et al. *Empirical analysis of the relationship between CC and SLOC in a large corpus of Java methods and C functions*. Journal of Software: Evolution and Process, v. 28, n. 7, p. 589–618, 2016.
- SILVA JUNIOR, J. L. F. *Avaliação da Experiência do Desenvolvedor de Software Por Meio de Métricas Orientadas a Objetos*. Master's dissertation, Universidade Federal de Sergipe, 2026.
