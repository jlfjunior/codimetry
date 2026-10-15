namespace MetricsExporter.Net.Tests.Helpers;

/// <summary>
/// The C# examples published in docs/*.md. The integration tests assert the values the docs state,
/// so a failure means either the documentation or the implementation is out of date.
/// </summary>
public static class Samples
{
    // ---------- docs/loc.md ----------

    public const string LocCalculator = """
        public class Calculator
        {
            // Adds two integers
            public int Add(int a, int b)
            {

                int result = a + b;
                return result;
            }
        }
        """;

    // ---------- docs/cyclomatic.md ----------

    public const string DiscountService = """
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
        """;

    public const string ShippingCalculator = """
        namespace Samples.Cyclomatic;

        public enum Region { North, South, Abroad }

        public class ShippingCalculator
        {
            public decimal Fee(Region region, decimal weight, string? coupon) =>
                region switch
                {
                    Region.North => 10m,
                    Region.South => 15m,
                    _ => weight > 5 && coupon is null ? 80m : 50m
                } - Discount(coupon ?? "");

            private static decimal Discount(string coupon) => coupon == "FREE" ? 10m : 0m;
        }
        """;

    // ---------- docs/ck.md ----------

    public const string CkWmc = """
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
        """;

    public const string CkRfc = """
        namespace Samples.Rfc;

        public class OrderService
        {
            public void Complete(Order order)
            {
                order.UpdateStatus(OrderStatus.Completed);
                order.NotifyCustomer();
            }
        }
        """;

    public const string CkLcom = """
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
        """;

    /// <summary>Cap. 2, exemplo de LCOM alto: LCOM = 3.</summary>
    public const string Cap2Usuario = """
        using System;

        public class Usuario
        {
            public string Nome;
            public string Email;

            public void ExibirNome()
            {
                Console.WriteLine(Nome);
            }

            public void EnviarEmail(string mensagem)
            {
                Console.WriteLine($"Enviando para {Email}: {mensagem}");
            }

            public void CadastrarProduto()
            {
                Console.WriteLine("Produto cadastrado!");
            }
        }
        """;

    /// <summary>Sonda INC-07: nenhum par de métodos compartilha atributo do objeto (LCOM = 3).</summary>
    public const string LcomFalse = """
        public class Dto { public string Nome = ""; }

        public class LcomFalse
        {
            public string Nome = "";
            public int Idade;
            public Dto A() => new Dto { Nome = "x" };
            public string? B(Dto? d) => d?.Nome;
            public int C() => Idade;
        }
        """;

    /// <summary>Sonda INC-08: o construtor estático não entra no WMC nem na LCOM (WMC = 1, LCOM = 0).</summary>
    public const string StaticCtor = """
        public class StaticCtor
        {
            static int x;
            int y;
            static StaticCtor() { x = 1; }
            public int Get() => y;
        }
        """;

    public const string CkCbo = """
        namespace Samples.Cbo;

        public class EmailService
        {
            public void Send(string message) => Console.WriteLine(message);
        }

        public class Logger
        {
            public void Log(string message) => Console.WriteLine(message);
        }

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
        """;

    public const string CkDit = """
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
        """;

    public const string CkNoc = """
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
        """;

    // ---------- docs/misra.md ----------

    public const string MisraMc = """
        namespace Samples.Mc;

        public class Processor
        {
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
        }

        public class PriceCalculator
        {
            public decimal Total(IEnumerable<decimal> prices)
            {
                var total = prices.Sum();
                if (total > 100) total *= 0.9m;
                return Math.Round(total, 2);
            }
        }
        """;

    public const string MisraAc = """
        namespace Samples.Ac;

        public class Address { }

        public class Customer
        {
            public required string Name { get; init; }
            public required string Cpf { get; init; }
            public required string Email { get; init; }
            public int Age { get; init; }
            public Address? Address { get; init; }
        }
        """;

    public const string MisraClc = """
        namespace Samples.Clc;

        public class Order
        {
            public int Id { get; init; }
            public decimal Value { get; init; }
            public DateTime Date { get; init; }

            public decimal CalculateDiscount() => Value > 500 ? Value * 0.1m : 0;
        }
        """;

    public const string MisraCwc = """
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
        """;
}
