# CK Metrics

The **CK metrics** were proposed by Shyam R. Chidamber and Chris F. Kemerer in 1994 as a suite of six measures for object-oriented design. McCabe's cyclomatic complexity works at the method level and ignores how classes relate to each other. The CK suite looks at the structure of the **class** instead: its internal complexity, its coupling to other classes, its cohesion, and where it sits in the inheritance hierarchy.

This page explains what each metric means, gives its formal definition, shows a worked example in modern C#, and states how **MetricsExporter.Net** computes the value in its output. The definitions follow the master's dissertation by Silva Junior (2026), Section 2.5 (see [References](#references)).

| Metric | Name | Dimension | Interpretation |
|--------|------|-----------|----------------|
| [WMC](#wmc--weighted-methods-per-class) | Weighted Methods per Class | Complexity | Lower is simpler |
| [RFC](#rfc--response-for-a-class) | Response For a Class | Complexity / Coupling | Lower is easier to test |
| [LCOM](#lcom--lack-of-cohesion-in-methods) | Lack of Cohesion in Methods | Cohesion | Lower is more cohesive |
| [CBO](#cbo--coupling-between-objects) | Coupling Between Objects | Coupling | Lower is more independent |
| [DIT](#dit--depth-of-inheritance-tree) | Depth of Inheritance Tree | Inheritance | Higher means more reuse, but harder to understand |
| [NOC](#noc--number-of-children) | Number of Children | Inheritance | Higher means more reuse, but greater impact of changes |

## Collecting the metrics

```bash
dotnet run --project src/MetricsExporter.Net/MetricsExporter.Net.csproj -- --path ./src --ck
```

This produces `metrics_ck.csv` with **one row per class**:

```csv
FilePath,ClassName,WMC,DIT,NOC,CBO,RFC,LCOM
```

See [COLLECTING_METRICS.md](COLLECTING_METRICS.md) for the remaining options (solutions, projects, output directory).

---

## WMC — Weighted Methods per Class

### Definition

WMC measures the complexity of a class from the methods it **declares**. Inherited methods are not counted.

$$
WMC = \sum_{i=1}^{n} c_i
$$

where $n$ is the number of methods defined in the class and $c_i$ is the complexity of method $i$. Chidamber and Kemerer deliberately left the choice of $c_i$ open. A common choice, and the one the dissertation adopts, is the method's **cyclomatic complexity** ($V(G) = D + 1$, where $D$ is the number of decision points). If every $c_i = 1$, WMC is simply the **number of methods** in the class.

### Interpretation

A high WMC means the class concentrates a lot of behaviour. It takes more effort to understand, test, and change, and it is less likely to be reusable. A lower WMC means less aggregated complexity.

### Example

```csharp
namespace Samples.Wmc;

public class Calculator
{
    public int Add(int a, int b) => a + b;

    public int Subtract(int a, int b) => a - b;

    public int Multiply(int a, int b)
    {
        var result = 0;
        for (var i = 0; i < b; i++)
        {
            result += a;
        }
        return result;
    }
}
```

| Method | Decision points | $c_i$ (cyclomatic) |
|--------|-----------------|--------------------|
| `Add` | 0 | 1 |
| `Subtract` | 0 | 1 |
| `Multiply` | 1 (`for`) | 2 |

- **WMC (cyclomatic-weighted) = 1 + 1 + 2 = 4**
- **WMC (unweighted, $c_i = 1$) = 3**

### How this tool computes it

The `WMC` column uses **$c_i = 1$**. It counts the **non-static methods and constructors** declared in the class. Properties, static methods, static constructors, and local functions are not counted. For `Calculator` the tool reports **WMC = 3**.

To get a complexity-weighted view, run with `--cyclomatic` and look at the class-level rows of `metrics_cyclomatic.csv`. That value sums the cyclomatic complexity of every method in the class, which gives 4 for `Calculator`. Note that it also includes constructors and property accessors.

---

## RFC — Response For a Class

### Definition

RFC is the number of methods that can be executed in response to a message received by an object of the class. It is the size of the class's **response set**:

$$
RFC = |RS|, \qquad RS = \{M\} \cup \bigcup_{i} \{R_i\}
$$

where $M$ is the set of methods of the class and $R_i$ is the set of methods **directly** invoked by method $i$. Because it is a union of sets, a method that is invoked several times, even from different methods of the class, is counted only once.

### Interpretation

A high RFC means a single call into the class can trigger a lot of behaviour. That makes the class harder to understand and test, because more paths and collaborators must be considered.

### Example

```csharp
namespace Samples.Rfc;

public class OrderService
{
    public void Complete(Order order)
    {
        order.UpdateStatus(OrderStatus.Completed);
        order.NotifyCustomer();
    }
}
```

- Methods of the class: `Complete` → $|M| = 1$
- Methods invoked: `UpdateStatus`, `NotifyCustomer` → $|R| = 2$
- **RFC = 1 + 2 = 3**

### How this tool computes it

- $M$ includes every method and constructor declared in the class, static ones included.
- $R$ is the set of **distinct method names** invoked inside those bodies. Methods are matched by simple name, so `a.Save()` and `b.Save()` count once.
- $R$ includes calls to framework methods (e.g. `Console.WriteLine` contributes `WriteLine`) and calls to the class's own methods.
- Only direct calls are considered; the tool does not follow calls transitively.

For `OrderService` the tool reports **RFC = 3**.

---

## LCOM — Lack of Cohesion in Methods

### Definition

LCOM measures how **unrelated** the methods of a class are, judged by the instance variables they share. Let $P$ be the set of method pairs that share **no** instance variable, and $Q$ the set of pairs that share **at least one**:

$$
LCOM =
\begin{cases}
|P| - |Q|, & \text{if } |P| > |Q| \\
0, & \text{otherwise}
\end{cases}
$$

This original formulation is usually called **LCOM1**. Later work proposed several alternatives, and there is no consensus on a single one:

- **LCOM2** (Li & Henry, 1993) is the proportion of non-sharing pairs, normalised to $[0, 1]$.
- **LCOM3/LCOM4** (Hitz & Montazeri, 1995) builds a graph of methods connected by shared attributes and counts its **connected components**.
- **LCOM5** (Henderson-Sellers, 1996) is a normalised value based on the average number of methods that access each attribute.

Studies that use LCOM should always state which variant they used. This tool implements **LCOM1**.

### Interpretation

A high LCOM indicates low cohesion: the class behaves like a container of loosely related responsibilities and is a candidate to be split. A known weakness of LCOM1 is that a method that uses no instance variable, such as a utility or logging method, shares nothing with any other method by definition. That inflates $|P|$ even when the class is well designed.

### Example

```csharp
namespace Samples.Lcom;

public class User
{
    public required string Name { get; init; }
    public required string Email { get; init; }

    public void ShowName() => Console.WriteLine(Name);

    public string Greet() => $"Hello, {Name}!";

    public void SendEmail(string message) =>
        Console.WriteLine($"Sending to {Email}: {message}");

    public void RegisterProduct() => Console.WriteLine("Product registered!");
}
```

| Pair | Shared state | |
|------|--------------|---|
| (`ShowName`, `Greet`) | `Name` | $Q$ |
| (`ShowName`, `SendEmail`) | — | $P$ |
| (`ShowName`, `RegisterProduct`) | — | $P$ |
| (`Greet`, `SendEmail`) | — | $P$ |
| (`Greet`, `RegisterProduct`) | — | $P$ |
| (`SendEmail`, `RegisterProduct`) | — | $P$ |

- $|P| = 5$, $|Q| = 1$
- **LCOM = 5 − 1 = 4**

`RegisterProduct` has nothing to do with users. Moving it to a separate class would remove three of the non-sharing pairs.

### How this tool computes it

The tool applies the LCOM1 formula above to the **non-static methods** declared in the class (in any of its `partial` declarations). Classes with zero or one method have LCOM = 0. These do not enter:

- constructors, static or not: a constructor usually initializes every field, so it would share state with almost every method regardless of how cohesive the methods are;
- property accessors, operators, and local functions.

A primary constructor has no body of its own, so it never enters either.

The instance variables of a class are the ones **declared in it** (in any of its `partial` declarations):

- non-static fields and properties, including computed properties such as `int Total => a + b`;
- non-static field-like events (`public event EventHandler Changed;`);
- the parameters of its primary constructor (`class C(Repo repo)`).

Inherited members do not count.

A method **uses** an instance variable when the semantic model binds an identifier in its body to one of them, either as a simple name (`Name`, `_handler()`) or as `this.Name`. These do not count, even when the name matches an instance variable:

- members of another object: `other.Name`, `d?.Name`, `new Dto { Name = "x" }` (the left-hand side of an object initializer), `x is C { Name: "x" }`;
- argument names (`Create(name: x)`) and `nameof(Name)`;
- locals, parameters of the method or of an ordinary constructor, and types (`Status.Active` where the class also has a `Status` property).

When the semantic model is not available, identifiers are matched by name with the same syntactic exclusions. The log reports how many methods were analysed that way.

---

## CBO — Coupling Between Objects

### Definition

CBO is the number of **other classes** a class is coupled to. Two classes are coupled when one uses the other's methods or attributes, inherits from it, or instantiates it. Each coupled class is counted once, however many times it is used.

### Interpretation

A high CBO means the class depends on many others. Changes elsewhere are more likely to ripple into it, it is harder to test in isolation, and it is harder to reuse.

### Example

```csharp
namespace Samples.Cbo;

public class PaymentService
{
    private readonly EmailService _email;
    private readonly Logger _logger;

    public PaymentService(EmailService email, Logger logger)
    {
        _email = email;
        _logger = logger;
    }

    public void Pay()
    {
        _logger.Log("Payment started");
        _email.Send("Payment completed");
    }
}
```

- Classes used: `EmailService`, `Logger`
- **CBO = 2**

### How this tool computes it

The tool collects the types that appear in the class's **field types, property types, method return types, and method parameter types**. It keeps only the types **declared in the analysed code**, so framework types such as `string`, `List<T>`, or `ILogger<T>` are ignored, and it excludes the class itself. Generic arguments, array brackets, and `?` are stripped before matching.

The following do **not** contribute to the value:

- types used only in local variables, `new` expressions, or static calls;
- constructor parameters, including **primary constructor** parameters (`class PaymentService(EmailService email)`) that are not stored in a declared field;
- incoming coupling, meaning classes that depend on this class.

For `PaymentService` the tool reports **CBO = 2** (through the two fields).

---

## DIT — Depth of Inheritance Tree

### Definition

DIT is the length of the path from a class to the **root** of its inheritance tree. The deeper a class is, the more methods and state it inherits.

### Interpretation

A higher DIT means more reuse of inherited code. It also means more effort to understand the class, because its full behaviour is spread across all of its ancestors. Modern C# favours composition over deep hierarchies, so DIT tends to be low in idiomatic code.

### Example

```csharp
namespace Samples.Dit;

public class Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();
}

public class Person : Entity
{
    public required string Name { get; init; }
}

public class Employee : Person
{
    public decimal Salary { get; init; }
}
```

- `Entity` → `object` (implicit)
- `Person` → `Entity` → `object`
- `Employee` → `Person` → `Entity` → `object`
- **DIT(Entity) = 1, DIT(Person) = 2, DIT(Employee) = 3**

### How this tool computes it

The tool counts how many levels there are above the class, following `BaseList` entries in the analysed code. When Roslyn's semantic model resolves the base type, the implicit `System.Object` counts as one level. That is why a class with no explicit base gets **DIT = 1**, and the tool reports the values shown above.

A base class from outside the analysed code, such as `ControllerBase` or `DbContext`, counts as one level, but its own ancestors are not resolved. So DIT is a lower bound for classes that extend framework types. Interfaces are not part of the inheritance tree.

---

## NOC — Number of Children

### Definition

NOC is the number of **immediate subclasses** of a class. Indirect descendants (grandchildren) are not counted.

### Interpretation

A high NOC indicates that the base class is widely reused through inheritance. It also means that a change to it affects many classes. And if the subclasses don't share a coherent behaviour, it may point to a poorly chosen abstraction or misuse of inheritance.

### Example

```csharp
namespace Samples.Noc;

public abstract class Animal
{
    public abstract string Speak();
}

public sealed class Cat : Animal
{
    public override string Speak() => "Meow";
}

public sealed class Dog : Animal
{
    public override string Speak() => "Woof";
}
```

- Direct subclasses of `Animal`: `Cat`, `Dog`
- **NOC(Animal) = 2**

### How this tool computes it

The tool counts the classes **in the analysed code** whose direct base is the class. Subclasses that live in other repositories or assemblies are not visible to it. For `Animal` the tool reports **NOC = 2**.

---

## Limitations and caveats

- **No universal thresholds.** Unlike cyclomatic complexity, which has McCabe's reference of $V(G) \le 10$, Chidamber and Kemerer defined no reference values. Later studies (Kulkarni et al., 2010; Mei et al., 2023) show that different threshold-derivation methods produce very different values. Compare classes within the same codebase rather than against absolute numbers.
- **Definitions vary between tools.** The choice of $c_i$ in WMC, the LCOM variant, and what counts as a coupling in CBO all differ between tools. Values from different tools are not directly comparable.
- **LCOM1 is fragile.** Methods that use no instance state inflate the metric even in cohesive classes.
- **Inheritance metrics matter less in modern code.** Composition, interfaces, and dependency injection reduce the relevance of DIT and NOC.
- **Metrics complement each other.** CK metrics capture *structure*. Use them together with cyclomatic complexity (control flow) and the cognitive metrics in [misra.md](misra.md) (understandability), not in isolation.
- **Tool-specific scope.** Analysis is syntactic, and calls are matched by simple name. Only `class` declarations are measured: `record`, `struct`, and `interface` types do not produce rows.

## References

- CHIDAMBER, S. R.; KEMERER, C. F. *A metrics suite for object oriented design*. IEEE Transactions on Software Engineering, v. 20, n. 6, p. 476–493, 1994.
- McCABE, T. J. *A complexity measure*. IEEE Transactions on Software Engineering, SE-2, n. 4, p. 308–320, 1976.
- LI, W.; HENRY, S. *Object-oriented metrics that predict maintainability*. Journal of Systems and Software, v. 23, n. 2, p. 111–122, 1993.
- HITZ, M.; MONTAZERI, B. *Measuring coupling and cohesion in object-oriented systems*. Proceedings of the International Symposium on Applied Corporate Computing (ISACC'95), Monterrey, Mexico, 1995.
- HENDERSON-SELLERS, B. *Object-Oriented Metrics: Measures of Complexity*. Prentice Hall, 1996.
- KULKARNI, U. L.; KALSHETTY, Y.; ARDE, V. G. *Validation of CK metrics for object oriented design measurement*. 3rd International Conference on Emerging Trends in Engineering and Technology, IEEE, p. 646–651, 2010.
- MEI, Y. et al. *Deriving thresholds of object-oriented metrics to predict defect-proneness of classes: A large-scale meta-analysis*. International Journal of Software Engineering and Knowledge Engineering, v. 33, p. 651–695, 2023.
- SILVA JUNIOR, J. L. F. *Avaliação da Experiência do Desenvolvedor de Software Por Meio de Métricas Orientadas a Objetos*. Master's dissertation, Universidade Federal de Sergipe, 2026.
