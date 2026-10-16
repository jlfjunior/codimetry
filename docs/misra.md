# Misra Cognitive Complexity Metrics

The **Suite of Object-Oriented Cognitive Complexity Metrics** was proposed by Sanjay Misra et al. in 2018. Its goal is to assess object-oriented designs from the reader's point of view: how much **mental effort** it takes to understand methods, classes, and the whole system. It is complementary to the [CK metrics](ck.md), which describe the *structure* of a design, and to cyclomatic complexity, which counts *paths* for testing purposes.

> **Naming note.** "Misra" here is the surname of the first author. These metrics are **not** related to the MISRA C/C++ coding guidelines. The tool's CLI flag and output file still use the uppercase `--misra` / `metrics_misra.csv`.

This page explains what each metric means, gives its definition, shows a worked example in modern C#, and states how **MetricsExporter.Net** computes the value in its output. The definitions follow the master's dissertation by Silva Junior (2026), Section 2.6 (see [References](#references)).

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
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --misra
```

This produces `metrics_misra.csv`, which has rows at three levels:

```csv
FilePath,ClassName,MethodName,MC,AC,CLC,CWC,CC,Kind
src/Order.cs,Order,CalculateDiscount,3,0,0,0,0,class   <- method row: only MC is filled
src/Order.cs,Order,,0,3,6,0,0,class                    <- class row: AC, CLC and CWC
,,,0,0,0,0,44,                                         <- one codebase row: CC (= CoC)
```

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
| Branch | `if` (with or without `else`), `switch`, ternary `?:` | 2 |
| Iteration | `for`, `foreach`, `while`, `do-while` | 3 |

The rule is recursive over statement lists:

- weight(list) = Σ weight(structures of the list) + 1 if the list has at least one simple statement; an empty list weighs 0;
- weight(structure) = W × weight(inner layer). The inner layer joins all branches of the structure (the `then` and `else` of an `if`, every section of a `switch`) and counts at least 1, so a structure with an empty body keeps its own weight;
- all simple statements of a layer form **one** sequence, even when a structure separates them (`a(); if (c) { x(); } b();` = 2 + 1 = 3);
- blocks, `try`/`catch`/`finally`, `using`, `lock`, `checked`, `unsafe`, `fixed` and labels are transparent: their statements belong to the enclosing list;
- `else if` is an `if` nested in the `else` branch, as written: `if (a) x(); else if (b) y(); else z();` = 2 × (1 + 2 × 1) = 6;
- a ternary is a branch over its two expressions (a nested ternary is a structure of that layer) and adds to the layer where it appears; the statement that contains it still counts as a simple statement;
- an expression-bodied method (`=> ...`) is a list with one simple statement; an empty body (`{ }`) weighs 0; methods without a body (abstract, interface) have no MC row.

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

Not modelled yet: lambda bodies are not looked into; `switch` weighs 2 instead of the Case weight 3; parallelism and interrupts (weight 4) have no reliable syntactic marker in C#.

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
- MC(`CalculateDiscount`) = 3: the expression body counts as one sequence (1) and the ternary as one branch over two simple expressions (2 × 1);
- **CLC = 3 + 3 = 6**, which is what the tool reports.

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

The tool walks **every invocation** in every method of the class. Repeated calls are counted each time.

- If the class itself declares a method with that name, the call is internal and is ignored.

- If a method with the same name is declared in **another class** of the analysed code, the call adds $MC_{tool}(\text{called}) + 2$, where **2** is the call weight of the BCS model.
- If no such method is found, the call adds a flat **3** ($MC = 1$ assumed, plus 2). This applies to framework and library methods such as `Console.WriteLine` or `Dictionary.TryGetValue`.

For `CartService`:

- `IsAvailable`: its tool MC is 1 (an expression body is one sequence), so it adds 1 + 2 = 3;
- `WriteLine`: a library method, so it adds 3;
- **CWC = 3 + 3 = 6**, which is what the tool reports.

Methods are matched by simple name only. Counting every library call makes the tool's CWC grow with the overall amount of code, not just with coupling to other classes in the system.

---

## CoC — Code Complexity

### Definition

CoC estimates the **overall cognitive complexity of an object-oriented system**, taking the **inheritance hierarchy** into account. For each level of the hierarchy, the CLCs of the classes at that level are summed, and the levels are combined by multiplication. The idea is that complexity propagates down the hierarchy: to understand a subclass, you must also understand what it inherits.

### Interpretation

CoC is a single system-wide number. It is useful for following how a codebase evolves over time, or for comparing systems of similar size. It says nothing about *where* the complexity is; use CLC and MC for that.

### Example

This is the dissertation's simplified example:

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

- `User`: AC = 2, MC = 0 → CLC = 2
- `Customer`: AC = 1, MC = 1 → CLC = 2, at level 2 of the hierarchy
- **CoC = CLC(User) + 2 × CLC(Customer) = 2 + 4 = 6**

The full formulation by Misra et al. accounts for the complete depth of the inheritance tree. The example above trades that detail for clarity.

### How this tool computes it

The tool **ignores the inheritance hierarchy**. It produces one codebase-level value, in the `CC` column:

$$
CC_{tool} = \sum_{k} CLC_k + \left\lfloor \frac{\sum_{k} CWC_k}{2} \right\rfloor
$$

summed over every class $k$ in the analysed code. The CWC total is halved because each coupling involves two classes, so halving avoids counting it twice. For example, the samples on this page (`Address`, `Customer`, `Order`, the record `Product`, `StockService`, `CartService`, `Processor`, `PriceCalculator`) give $\sum CLC = 35$ and $\sum CWC = 18$, so **CC = 35 + 9 = 44**. `Product` adds 1: its positional parameter `Sku` is an attribute, and it has no methods. `CartService.Checkout` has MC 5: its `if` (2) plus the call to `IsAvailable` in the condition (2 + MC 1).

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
