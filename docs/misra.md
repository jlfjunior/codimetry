# Misra Cognitive Complexity Metrics

The **Suite of Object-Oriented Cognitive Complexity Metrics** was proposed by Sanjay Misra et al. in 2018. Its goal is to assess object-oriented designs from the reader's point of view: how much **mental effort** it takes to understand methods, classes, and the whole system. It is complementary to the [CK metrics](ck.md), which describe the *structure* of a design, and to cyclomatic complexity, which counts *paths* for testing purposes.

> **Naming note.** "Misra" here is the surname of the first author. These metrics are **not** related to the MISRA C/C++ coding guidelines. The tool's CLI flag and output file still use the uppercase `--misra` / `metrics_misra.csv`.

This page explains what each metric means, gives its definition, shows a worked example in modern C#, and states how **Codimetry** computes the value in its output. The definitions follow the master's dissertation by Silva Junior (2026), Section 2.6 (see [References](#references)).

| Metric | Name | Level | Interpretation |
|--------|------|-------|----------------|
| [MC](#mc--method-complexity) | Method Complexity | Method | Lower is easier to understand |
| [AC](#ac--attribute-complexity) | Attribute Complexity | Class | Lower means less state to keep in mind |
| [CLC](#clc--class-complexity) | Class Complexity | Class | Lower is easier to understand |
| [CWC](#cwc--coupling-weight-for-a-class) | Coupling Weight for a Class | Class | Lower means fewer and simpler external dependencies |
| [CoC](#coc--code-complexity) | Code Complexity | System | Lower is easier to understand as a whole |

The dissertation uses the acronym **CoC** for *Code Complexity* to avoid confusing it with McCabe's cyclomatic complexity (CC). The tool's CSV still names that column `CC`.

## Collecting the metrics

```bash
dotnet run --project src/Codimetry/Codimetry.csproj -- --path ./src --misra
```

This produces `metrics_misra.csv`, which has rows at three levels:

```csv
FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CoC,CC,Kind
src/Order.cs,Order,CalculateDiscount,2,0,0,0,0,0,class   <- method row: only MC is filled
src/Order.cs,Order,,0,3,5,0,0,0,class                    <- class row: AC, CLC and CWC
,,,0,0,0,0,34,34,                                        <- one codebase row: CoC (CC repeats it)
```

It also produces `metrics_coc.csv`, with the CoC of each inheritance hierarchy (see [CoC](#coc--code-complexity)).

`Kind` is the kind of the type of the row: `class`, `record`, `struct` or `record struct` (see [which types are measured](README.md#which-types-are-measured)).

See [COLLECTING_METRICS.md](COLLECTING_METRICS.md) for the remaining options (solutions, projects, output directory).

---

## Background: cognitive complexity

Cognitive complexity was proposed by G. Ann Campbell (SonarSource) as an alternative to cyclomatic complexity. Cyclomatic complexity measures *testability*; cognitive complexity targets *understandability*. Campbell defines three rules:

1. **Ignore shorthand.** Structures that let several statements be written readably as one, such as trivial accessors or chained null-coalescing operators, add nothing.
2. **Increment for each break in the linear flow.** This covers `if`, `else if`, `else`, each `case` of a `switch`, `for`, `while`, `do-while`, `catch`, the ternary operator, jumps to labels, recursion, and each sequence of `&&` or `||` operators of the same kind.
3. **Increment for nesting.** Each structure from rule 2 adds an extra increment equal to its nesting depth: 0 directly in the method body, 1 inside one structure, and so on.

Rule 3 is the key difference from cyclomatic complexity. Cyclomatic complexity adds +1 per decision regardless of depth, so flat code and deeply nested code score the same. Cognitive complexity penalises nesting.

---

## MC — Method Complexity

### Definition

MC is the cognitive complexity of a single method. Following Campbell's rules, each control structure $s$ contributes a basic increment plus an increment equal to its nesting depth:

$$
MC = \sum_{s \,\in\, \text{structures}} \left(1 + \text{nesting}(s)\right)
$$

### Interpretation

A high MC means the method is hard to read. It has many decisions, and especially many decisions stacked inside one another. Extracting methods and using guard clauses (early `return`s) to flatten the nesting both lower MC.

### Example

```csharp
public void Process(int x)
{
    if (x > 0)
    {
        for (var i = 0; i < x; i++)
        {
            if (i % 2 == 0)
                Console.WriteLine("Even");
        }
    }
}
```

| Structure | Nesting | Increment |
|-----------|---------|-----------|
| `if (x > 0)` | 0 | 1 + 0 = 1 |
| `for` | 1 | 1 + 1 = 2 |
| `if (i % 2 == 0)` | 2 | 1 + 2 = 3 |

- **MC = 1 + 2 + 3 = 6**

For the same method, cyclomatic complexity is $V(G) = 1 + 3 = 4$. The difference of two points is exactly the extra weight cognitive complexity gives to nesting.

### How this tool computes it

The tool does **not** use Campbell's nesting increments. It uses the **cognitive weights of Basic Control Structures (BCS)** (Wang & Shao, 2003), composed as in Eq. 23 of Misra et al. (2018): weights in the same layer are **added**, nested layers are **multiplied**.

| BCS | Examples | Weight |
|-----|----------|--------|
| Sequence | the simple statements of a layer (assignment, declaration, `return`, `throw`, call statement…), all together | 1 |
| Branch (if-then-else) | `if` (with or without `else`), ternary `?:` | 2 |
| Case | `switch` statement, `switch` expression | 3 |
| Iteration | `for`, `foreach`, `while`, `do-while` | 3 |

The rule is recursive over statement lists:

- weight(list) = Σ weight(structures of the list) + 1 if the list has at least one simple statement; an empty list weighs 0;
- weight(structure) = W × weight(inner layer). The inner layer joins all branches of the structure (the `then` and `else` of an `if`, every section of a `switch`) and counts at least 1, so a structure with an empty body keeps its own weight;
- all simple statements of a layer form **one** sequence, even when a structure separates them (`a(); if (c) { x(); } b();` = 2 + 1 = 3);
- blocks, `try`/`catch`/`finally`, `using`, `lock`, `checked`, `unsafe`, `fixed` and labels are transparent: their statements belong to the enclosing list;
- `else if` is an `if` nested in the `else` branch, as written: `if (a) x(); else if (b) y(); else z();` = 2 × (1 + 2 × 1) = 6;
- a ternary is an if-then-else over its two expressions, and a `switch` expression a Case over its arms: a branch or arm that is itself a ternary or a `switch` expression is a structure of that layer, any other one a simple statement. The structure adds to the layer where it appears, and the statement that contains it **is** that structure, not one more sequence: `return c ? 1 : 0;` = 2 × 1 = 2, while `var v = c ? 1 : 0; x();` = 2 + 1 because `x();` is a sequence. The governing expression and the `when` clauses of a `switch` expression belong to the enclosing layer;
- `catch` adds no weight (Table 2.1 has no weight for exceptions), and `??`, `?.` and patterns (`is`, `and`, `or`) are not BCSs;
- an expression-bodied method (`=> ...`) is a list with one simple statement, or the structure itself when the expression holds a ternary or a `switch` expression; an empty body (`{ }`) weighs 0; methods without a body (abstract, interface) have no MC row.

For the `Process` method above, the tool reports **MC = 2 × (3 × (2 × 1)) = 12** (`Console.WriteLine` is a library call and adds nothing): the outer `if` (2) contains the `for` (3), which contains the inner `if` (2), whose body is one sequence (1).

**Calls** (Cap. 2: "a call to another method contributes the call weight (2) plus the MC of the called method"). A call adds to the layer where it appears, like a structure; a call in the condition of an `if` belongs to the layer that contains the `if`:

| Call | Adds |
|------|------|
| to a method or constructor of the analysed code, own class included | 2 + MC of the called method |
| inside a recursion, direct (`F` calls `F`) or mutual (a cycle in the call graph) | 2 |
| to an abstract or interface method | 2 + MC of its implementation when the analysed code has exactly one; otherwise 2 + 1 |
| to a library method (`Console.WriteLine`, LINQ, `string.Compare`...) | 0 (`--mc-library-call-weight N` sets it) |
| unresolved by the semantic model | 2 + 1 |
| invoking a delegate, a lambda or a local function; `nameof` | 0 |

Object creation (`new X()`) is not a call. The MC of the called method is computed with the same rules, so it includes its own calls. All the methods of a recursion are treated alike, whichever is computed first. In the article's example, `CheckSupportedSw` = 1 + (2 + MC(`IsAppropriate`) = 7) + 2 × 1 = **12**. The log reports how many calls fell in each row of the table.

**Lambdas and local functions**. The code of a lambda, anonymous method or local function written in a method is read as part of it: its body is a layer nested where it is written, of weight 1 × that layer, added to the structures of the enclosing layer. An expression body has no sequence of its own, so `l.Any(x => x > 0)` adds nothing, while `x => x > 0 ? 1 : 0` adds its ternary. The statement that holds the lambda is still a simple statement, and a local function declaration is not one. Calls inside follow the table above; a call to the local function adds nothing, since its body is already counted. For example, `l.ForEach(x => { if (x > 0) Console.WriteLine(x); });` = 1 + (2 × 1) = **3**, and a local function with an `if`, called once, also gives 3.

`--mc-lambdas` chooses another treatment: `calls` (the rule before this change: a lambda contributes only its calls, a local function body is inlined into the enclosing layer) or `ignore` (their bodies are left out).

Not modelled: parallelism and interrupts (weight 4) have no reliable syntactic marker in C#.

Example of the tool's calculation:

```csharp
public class PriceCalculator
{
    public decimal Total(IEnumerable<decimal> prices)
    {
        var total = prices.Sum();          // simple statement
        if (total > 100) total *= 0.9m;    // branch: 2 × 1
        return Math.Round(total, 2);       // simple statement
    }
}
```

- the two simple statements of the method body form one sequence (1), and the `if` adds 2 × 1
- **MC = 2 + 1 = 3**, which is what the tool reports

`--mc-additive` restores the rule of v1 (every call, library ones included, weighs 2 and the MC of the called method does not enter), which adds the weights at any depth and counts each statement and each call: $MC = 1 \cdot N_{seq} + 2 \cdot N_{branch} + 3 \cdot N_{iter} + 2 \cdot N_{call}$, where each `else` is one more branch. With it, `Process` is 10 and `Total` is 9.

Because the two models differ, **MC values from this tool are not comparable** with Campbell/SonarQube cognitive complexity values.

---

## AC — Attribute Complexity

### Definition

AC measures the cognitive load created by the **attributes** (instance state) a class declares. Each attribute is something the reader has to learn and keep in mind to understand the object's state:

$$
AC = \sum_{i=1}^{n} w(a_i)
$$

where $n$ is the number of attributes of the class. In the dissertation's formulation every attribute weighs $w(a_i) = 1$, so AC is the **number of attributes**.

### Interpretation

A high AC means the object carries a lot of state. That is often a sign that the class aggregates more than one concept, such as a "god class" or a data bag.

### Example

```csharp
namespace Samples.Ac;

public class Customer
{
    public required string Name { get; init; }
    public required string Cpf { get; init; }
    public required string Email { get; init; }
    public int Age { get; init; }
    public Address? Address { get; init; }
}
```

- 5 attributes
- **AC = 5**

### How this tool computes it

The tool counts the **instance attributes** the class declares, **1 each**, whatever their type:

- instance fields, one per variable (`int a, b;` is 2);
- field-like events (`public event EventHandler Changed;`);
- instance properties with their own storage: auto-properties (`{ get; set; }`, `{ get; init; }`);
- primary constructor parameters that a member uses, so the object keeps them as state;
- the positional parameters of a record (`record Person(string Name, int Age)` has AC = 2), which are public properties, unless the record declares a member with the same name (then that member is the attribute).

These do **not** count:

- `static` and `const` members, which are not instance state;
- computed properties (`int Total => a + b;`) and properties that read or write a field of the class: the field is the attribute, so it is counted once;
- events with `add`/`remove` accessors, indexers, and abstract properties;
- primary constructor parameters used only to initialize a field or property (the field counts);
- inherited attributes, which belong to the base class.

For `Customer`, that is five auto-properties, so the tool reports **AC = 5**.

Records, structs and record structs follow the same rules. `--ac-type-weights` restores the rule of v1: every field and property, `static` and `const` included, weighted 1 for simple types (numeric primitives, `bool`, `char`, `string`, `object`, `DateTime`, `TimeSpan`, `Guid`) and 2 for any other type. With it, `Customer` is 1 + 1 + 1 + 1 + 2 = 6.

---

## CLC — Class Complexity

### Definition

CLC is the **total cognitive complexity of a class**. It combines the static part (its attributes) with the behavioural part (its methods):

$$
CLC = AC + \sum_{j=1}^{m} MC_j
$$

where $m$ is the number of methods of the class.

### Interpretation

CLC estimates the effort needed to understand a class as a whole. It overlaps conceptually with [WMC](ck.md#wmc--weighted-methods-per-class), since both aggregate complexity at the class level, so expect the two to correlate.

### Example

```csharp
namespace Samples.Clc;

public class Order
{
    public int Id { get; init; }
    public decimal Value { get; init; }
    public DateTime Date { get; init; }

    public decimal CalculateDiscount() => Value > 500 ? Value * 0.1m : 0;
}
```

- AC = 3 (three attributes)
- MC(`CalculateDiscount`) = 1 (one ternary, at nesting 0)
- **CLC = 3 + 1 = 4**

### How this tool computes it

The formula is the same, $CLC = AC + \sum MC$, but it uses the tool's AC and MC:

- AC = 3, one per auto-property;
- MC(`CalculateDiscount`) = 2: the expression body is the ternary, one branch over two simple expressions (2 × 1);
- **CLC = 3 + 2 = 5**, which is what the tool reports.

Constructors are included in the sum. Methods are identified by `Class::Name`, so all overloads of a method share a single MC value.

---

## CWC — Coupling Weight for a Class

### Definition

CWC measures the **cognitive coupling** between classes. To understand a class, the reader also has to follow the external methods it calls, and each of those is harder to follow the more complex it is. So each external call contributes a fixed coupling weight **plus the complexity of the method being called**:

$$
CWC = \sum_{c \,\in\, \text{external calls}} \left(w_{call} + MC(c)\right)
$$

In the dissertation's didactic formulation, $w_{call} = 1$.

### Interpretation

A high CWC means the class depends on many external methods, or on complex ones. Changes in those dependencies are more likely to affect it, and understanding it requires reading code elsewhere.

### Example

```csharp
namespace Samples.Cwc;

public record Product(string Sku);

public class StockService
{
    private readonly Dictionary<string, int> _quantities = [];

    public bool IsAvailable(Product product) =>
        _quantities.TryGetValue(product.Sku, out var quantity) && quantity > 0;
}

public class CartService
{
    private readonly StockService _stock = new();

    public void Checkout(Product product)
    {
        if (_stock.IsAvailable(product))
            Console.WriteLine("Purchase completed");
    }
}
```

For `CartService`:

- 1 call to another class of the system: `_stock.IsAvailable` (framework calls such as `Console.WriteLine` are not considered)
- MC(`IsAvailable`) = 1 (one `&&` sequence)
- **CWC = 1 + 1 = 2**

The original formulation by Misra et al. includes further weightings, such as the number of distinct coupled classes. The dissertation omits these from its didactic examples.

### How this tool computes it

The tool follows Eq. 24: only calls to methods of **other types of the analysed code** count. Each call is resolved by the semantic model, and each one counts, repeated ones included:

| Call | Adds |
|------|------|
| to a method of another class, record or struct of the analysed code, including a method inherited from a base class (`base.M()` or `M()`) | 2 + MC of the called method |
| to an abstract or interface method declared in the analysed code | 2 + MC of its only implementation; 2 + 1 when there are none or several |
| to the class's own methods (any `partial` declaration) or its local functions | 0 |
| to a library method (`Console.WriteLine`, LINQ, `ILogger`...) or a delegate | 0 (`--cwc-library-weight N` sets it) |
| unresolved by the semantic model | 0, and the log reports how many there are |

The MC of the called method is the one in the method rows, so it already includes that method's own calls (see [MC](#mc--method-complexity)). A call to a type outside the [measured types](README.md#which-types-are-measured), such as a record with `--classes-only`, does not count, and neither does a constructor call (`new X()`, `new()`, `: base(...)`): a constructor is not a method call of Eq. 24, and the coupling it creates is measured by CBO.

For `CartService`:

- `IsAvailable`: a method of `StockService` with MC 1, so it adds 2 + 1 = 3;
- `WriteLine`: a library method, so it adds nothing;
- **CWC = 3**, which is what the tool reports.

`--cwc-library-weight 3` restores the library rule of v1, which added 3 for every library or delegate call; with `--mc-additive` as well, it reproduces v1 except for one defect that is not restored: v1 matched an unresolved call with the first method of the same name in any class.

---

## CoC — Code Complexity

### Definition

CoC estimates the **overall cognitive complexity of an object-oriented system**, taking the **inheritance hierarchy** into account. For each level of the hierarchy, the CLCs of the classes at that level are summed, and the levels are combined by multiplication. The idea is that complexity propagates down the hierarchy: to understand a subclass, you must also understand what it inherits.

### Interpretation

CoC is a single system-wide number. It is useful for following how a codebase evolves over time, or for comparing systems of similar size. It says nothing about *where* the complexity is; use CLC and MC for that.

### Example

This is the example of the dissertation (Cap. 2):

```csharp
public class User
{
    public required string Name { get; init; }
    public required string Email { get; init; }
}

public class Customer : User
{
    public required string Cpf { get; init; }

    public void Purchase() => Console.WriteLine("Purchase completed");
}
```

- `User` (level 1): AC = 2, MC = 0 → CLC = 2
- `Customer` (level 2): AC = 1, MC = 1 → CLC = 2
- **CoC = CLC(User) × CLC(Customer) = 2 × 2 = 4**

When a project has more than one hierarchy, their values are added.

### How this tool computes it

Eq. 27 of Misra et al. (2018) multiplies, over the levels of a hierarchy, the sum of the CLC of each level. The worked example of the same paper, however, does not follow it literally: for COMPUTER → {SOFTWARE, HARDWARE → {DESKTOP, NOTEBOOK}}, with CLC 4, 14, 20, 3 and 3, it gets

$$
CoC = 4 \times (14 + 20 \times (3 + 3)) = 536
$$

which multiplies each class by its own subclasses only. Read level by level, Eq. 27 would give $4 \times (14 + 20) \times (3 + 3) = 816$, multiplying `Software` by the subclasses of `Hardware`. The tool follows the paper's example by default:

$$
CoC(c) = \begin{cases} CLC(c) & \text{if } c \text{ has no subclasses in the analysed code} \\ CLC(c) \times \sum_{d \,\in\, \text{subclasses}(c)} CoC(d) & \text{otherwise} \end{cases}
\qquad
CoC = \sum_{r \,\in\, \text{roots}} CoC(r)
$$

- A **root** is a type whose base class is not in the analysed code: no base class, `System.Object`, or a base class from the framework or a package (`class E : Exception`), as for [DIT](ck.md#dit--depth-of-inheritance-tree). A type without subclasses or base class in the code is a hierarchy of its own, so CoC is $\sum CLC$ when nothing inherits.
- Records and structs are hierarchies like classes (records can inherit from records; structs cannot inherit).
- CWC does not enter.
- The value is a 64-bit integer: with CLC in the hundreds and hierarchies of 3–4 levels, CoC reaches $10^8$–$10^{10}$, so compare codebases on a logarithmic scale. If a hierarchy overflows 64 bits, the log warns and the value is reported as the largest 64-bit integer.

The value goes in the `CoC` column of the codebase row of `metrics_misra.csv`. The `CC` column repeats it for one version, so that scripts written for v1 keep working. `metrics_coc.csv` has one row per hierarchy, named by its root:

```csv
FilePath,ClassName,Classes,Levels,CoC,Kind
src/Computer.cs,Computer,5,3,536,class
```

`--coc-formula levels` uses Eq. 27 read level by level (816 for the example of the paper). The two agree when every level above the last has a single class, as in `User`/`Customer` (4) or a class with two subclasses that have none; they differ as soon as a level has several classes and one of them has subclasses.

`--coc-legacy` restores the rule of v1, which ignored the hierarchy and added half of the CWC: $\sum_k CLC_k + \lfloor \sum_k CWC_k / 2 \rfloor$.

The samples on this page (`Address`, `Customer`, `Order`, the record `Product`, `StockService`, `CartService`, `Processor`, `PriceCalculator`) do not inherit from each other, so each is a hierarchy of its own and **CoC = $\sum CLC$ = 34**. `Product` adds 1: its positional parameter `Sku` is an attribute, and it has no methods. `CartService.Checkout` has MC 5: its `if` (2) plus the call to `IsAvailable` in the condition (2 + MC 1).

---

## Limitations and caveats

- **The rules are heuristic.** Cyclomatic complexity derives from graph theory. Cognitive-complexity increments come from tool designers' intuition about what makes code hard to read, not from a formal model of cognition.
- **Mixed empirical evidence.** Barón, Wyrich & Wagner (2020, 2023) found that cognitive complexity correlates with comprehension *time* and perceived difficulty. They found no equally robust correlation with *correctness* in comprehension tasks.
- **Less validated than CK.** The Misra suite, especially CWC and CoC, is much more recent and has far fewer independent replications and public datasets than the CK metrics or cyclomatic complexity.
- **Possible redundancy.** MC overlaps with cyclomatic complexity (both describe a method's control flow), and CLC overlaps with [WMC](ck.md#wmc--weighted-methods-per-class). Watch for high correlations when you combine these metrics.
- **Formulations differ between tools.** As this page shows, the dissertation's didactic formulas and the tool's BCS-weighted implementation give different numbers for the same code. Always state which formulation you used, and don't compare absolute values across tools.
- **Tool-specific scope.** Analysis is syntactic, and calls are matched by simple name. Classes, records and structs are measured; interfaces and enums do not produce rows (see [which types are measured](README.md#which-types-are-measured)).

## References

- MISRA, S. et al. *A suite of object oriented cognitive complexity metrics*. IEEE Access, v. 6, p. 8782–8796, 2018.
- CAMPBELL, G. A. *Cognitive Complexity — An Overview and Evaluation*. 2018 IEEE/ACM International Conference on Technical Debt (TechDebt), p. 57–58, 2018.
- WANG, Y.; SHAO, J. *A new measure of software complexity based on cognitive weights*. Canadian Journal of Electrical and Computer Engineering, v. 28, n. 2, p. 69–74, 2003.
- BARÓN, M. M.; WYRICH, M.; WAGNER, S. *An empirical validation of cognitive complexity as a measure of source code understandability*. 14th ACM/IEEE International Symposium on Empirical Software Engineering and Measurement (ESEM), p. 1–12, 2020.
- BARÓN, M. M.; WYRICH, M.; WAGNER, S. *An empirical evaluation of the "cognitive complexity" measure as a predictor of code understandability*. Journal of Systems and Software, v. 197, 111561, 2023.
- McCABE, T. J. *A complexity measure*. IEEE Transactions on Software Engineering, SE-2, n. 4, p. 308–320, 1976.
- SILVA JUNIOR, J. L. F. *Avaliação da Experiência do Desenvolvedor de Software Por Meio de Métricas Orientadas a Objetos*. Master's dissertation, Universidade Federal de Sergipe, 2026.
