# Cyclomatic Complexity

**Cyclomatic complexity** was proposed by Thomas J. McCabe in 1976. It measures the logical complexity of a method from the structure of its **control flow**. It is the number of linearly independent paths through the method, and therefore the minimum number of test cases needed to exercise every decision at least once (*basis path testing*).

This page explains the metric, gives its formal definition, shows a worked example in modern C#, and states how **MetricsExporter.Net** computes the values in its output. The definitions follow the master's dissertation by Silva Junior (2026), Section 2.4.

## Collecting the metric

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --cyclomatic   # or --cc
```

This produces `metrics_cyclomatic.csv`, which has rows at three levels:

```csv
FilePath,ClassName,MethodName,Complexity,ClassKey
src/Discount.cs,DiscountService,CalculateDiscount,4,App|Samples.Cyclomatic.DiscountService   <- member
src/Discount.cs,DiscountService,,4,App|Samples.Cyclomatic.DiscountService                    <- class (sum of its members)
src/Discount.cs,,,4,                                                                         <- file (sum of its members)
```

See [COLLECTING_METRICS.md](COLLECTING_METRICS.md) for the remaining options.

---

## Definition

The method's code is represented as a **control-flow graph** $G$. Nodes are blocks of sequential code, and edges are transfers of control between them caused by conditions, loops, and branches. The cyclomatic complexity is:

$$
V(G) = E - N + 2P
$$

where $E$ is the number of edges, $N$ the number of nodes, and $P$ the number of connected components ($P = 1$ for a single method).

Building the graph explicitly is expensive. McCabe showed that, for a structured program with one entry and one exit point, the formula is equivalent to counting **decision points** directly in the source code:

$$
V(G) = D + 1
$$

A decision point is any construct that can branch the linear flow. In C# these are:

- `if` and `else if`;
- each `case` of a `switch`;
- the loops `for`, `foreach`, `while`, and `do-while`;
- `catch` clauses;
- the ternary operator `?:`;
- each short-circuit logical operator `&&` and `||`.

A plain `else` is **not** a decision point: it has no condition of its own, and it only represents the complementary path of its `if`.

## Interpretation

McCabe proposed $V(G) \le 10$ per method as the reference for a testable, manageable module. Above that, each additional independent path increases the testing effort and the risk that a change introduces an undetected defect. A commonly used banding is the one popularised by the Software Engineering Institute (SEI, 1997):

| $V(G)$ | Risk |
|--------|------|
| 1–10 | Simple, low risk |
| 11–20 | Moderately complex, moderate risk |
| 21–50 | Complex, high risk |
| > 50 | Untestable in practice, very high risk |

Like all thresholds, these are a starting point. Compare methods within the same codebase rather than treating the bands as absolute rules.

## Example

```csharp
namespace Samples.Cyclomatic;

public class DiscountService
{
    public decimal CalculateDiscount(decimal value, bool isVip, bool isFirstPurchase)
    {
        if (value > 500)
        {
            if (isVip)
            {
                if (isFirstPurchase)
                {
                    return value * 0.20m;
                }
                return value * 0.15m;
            }
            else
            {
                return value * 0.10m;
            }
        }

        return 0;
    }
}
```

- `if (value > 500)` → 1 decision
- `if (isVip)` → 1 decision
- `if (isFirstPurchase)` → 1 decision
- The `else` is not counted.
- **$V(G)$ = 1 + 3 = 4**

Cyclomatic complexity gives every decision the same weight, however deeply it is nested. The [cognitive complexity](misra.md#background-cognitive-complexity) of the same method is higher, because it penalises the three levels of nesting.

## How this tool computes it

The tool starts each method at **1** and adds **1** for each of the following:

| Construct | Counted |
|-----------|---------|
| `if`, `else if` | each one |
| `switch` statement | each `case` label, constant or pattern (`case 1: case 2:` counts 2); **not** `default`, nor a catch-all `case var x:` without `when` |
| `switch` expression | each arm, **except** the catch-all arm (`_`, or `var x` without `when`) |
| `for`, `foreach`, `while`, `do-while` | each loop |
| `catch` | each clause |
| `?:` | each ternary |
| `&&`, `\|\|` | each operator, in any expression (short-circuiting creates the path even in an assignment) |
| `or`, `and` (pattern combinators) | each combinator, like `\|\|` and `&&` |

`default` and the catch-all pattern are not counted for the same reason as a plain `else`: they are the complementary path of the other labels. A `when` clause is part of its `case` or arm and adds nothing of its own, but `&&` and `\|\|` inside it count. A `when` does make `_` or `var x` conditional, so `_ when ready => …` counts.

**Not counted:**

- `goto`: an unconditional jump has a single outgoing edge, so it creates no independent path.
- `??`, `??=` and `?.`: the dissertation's definition (Section 2.4) does not list them. They branch on null, so the three follow one rule: none counts by default, and `--cc-null-operators` counts each one.

`--cc-legacy-operators` restores the operator rule of v1: `??` and `goto` count; `??=`, `?.`, `or` and `and` do not. The two options cannot be used together. The `switch` rules above apply in every mode: the v1 count of `default` and `_` was a defect. The options also apply to WMC with `--wmc-weight cyclomatic`, which uses this same count, and the log records which rule was used.

An example using these modern C# constructs:

```csharp
public decimal Fee(Region region, decimal weight, string? coupon) =>
    region switch
    {
        Region.North => 10m,
        Region.South => 15m,
        _ => weight > 5 && coupon is null ? 80m : 50m
    } - Discount(coupon ?? "");
```

- base 1
- 2 switch arms (+2); the discard arm `_` is not counted
- `&&` (+1)
- `?:` (+1)
- `??` is not counted
- **Complexity = 5**, which is what the tool reports (6 with `--cc-null-operators`)

What gets measured: every member whose control flow the developer wrote. Each gets a row:

| Member | `MethodName` |
|--------|--------------|
| method, constructor (expression-bodied ones included) | its name |
| destructor | `~Name` |
| operator, conversion | metadata name: `op_Addition`, `op_Implicit`... |
| property accessor with a body (block or `=>`) | `Name.get`, `Name.set`, `Name.init` |
| expression-bodied property (`int X => ...`) | `X.get` |
| indexer accessor with a body | `this[].get`, `this[].set` |
| event accessor | `Name.add`, `Name.remove` |

**Not measured:** automatic accessors (`{ get; set; }`), which have no flow of their own, and members without a body (abstract, interface, `extern`, partial definitions). A class made only of automatic properties has V(G) = 0. `--cc-include-auto-accessors` restores the rule of v1: each automatic property accessor gets a row with V(G) = 1 and enters the sums.

Lambdas and local functions are not members: their decisions are added to the member that contains them.

- **Class rows** sum the members of the class. A class is identified by `ClassKey`, its project and fully qualified name, so the declarations of a `partial` class in different files give **one** row (in the file of the first declaration), and homonymous nested classes (`A.Builder`, `B.Builder`) give separate rows. Records and structs get class rows too, and a member belongs to the nearest enclosing type, so the methods of a nested record are not added to the enclosing class (see [which types are measured](README.md#which-types-are-measured)).
- **File rows** sum the members of measured types declared in the file. With a partial class, each file contributes its own members, so the sum of the file rows is the project total, with nothing counted twice.
- Members of interfaces (default implementations) get a row with an empty `ClassName` and enter no sum.

The class-level value is a cyclomatic-weighted [WMC](ck.md#wmc--weighted-methods-per-class). It differs from the `WMC` column of `metrics_ck.csv`, which counts only methods and constructors in both variants: `--wmc-weight cyclomatic` leaves out destructors, operators and accessors.

## Limitations

- **Nesting is ignored.** Two methods with the same number of decisions get the same value, whether the code is flat or deeply nested (Shepperd, 1988). Use [MC](misra.md#mc--method-complexity) for a nesting-aware view.
- **Data complexity is ignored.** Only control flow is measured.
- **Correlation with size.** $V(G)$ tends to grow with LOC, but Landman et al. (2016) show that the correlation is only moderate at the **method** level, so the two metrics are not redundant there. The correlation gets stronger when values are aggregated to classes or systems.
- **Exhaustive `switch` expressions without a catch-all.** `flag switch { true => a, false => b }` has two paths, but the tool counts both arms (V(G) = 3, not 2): telling that the arms are exhaustive needs the semantic model. Write the last arm as `_` to get the exact value.
- **Weak predictor of understandability on its own.** Esposito et al. (2024) found only a moderate correlation with the understandability perceived by early-career developers. Use it together with the [CK](ck.md) and [Misra](misra.md) metrics.

## References

- McCABE, T. J. *A complexity measure*. IEEE Transactions on Software Engineering, SE-2, n. 4, p. 308–320, 1976.
- SHEPPERD, M. *A critique of cyclomatic complexity as a software metric*. Software Engineering Journal, v. 3, n. 2, p. 30–36, 1988.
- LANDMAN, D. et al. *Empirical analysis of the relationship between CC and SLOC in a large corpus of Java methods and C functions*. Journal of Software: Evolution and Process, v. 28, n. 7, p. 589–618, 2016.
- ESPOSITO, M. et al. *Early career developers' perceptions of code understandability: A study of complexity metrics*. Information and Software Technology, v. 170, 107492, 2024.
- SOFTWARE ENGINEERING INSTITUTE. *C4 Software Technology Reference Guide — A Prototype*. CMU/SEI-97-HB-001, Carnegie Mellon University, 1997.
- SILVA JUNIOR, J. L. F. *Avaliação da Experiência do Desenvolvedor de Software Por Meio de Métricas Orientadas a Objetos*. Master's dissertation, Universidade Federal de Sergipe, 2026.
