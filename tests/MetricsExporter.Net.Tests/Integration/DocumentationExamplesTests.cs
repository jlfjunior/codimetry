using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Integration;

/// <summary>
/// Runs each analysis service over the examples published in docs/*.md and checks the values the docs state.
/// If one of these tests fails, either the documentation or the implementation needs to be updated.
/// </summary>
public class DocumentationExamplesTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    // ---------- docs/loc.md ----------

    [Fact]
    public async Task Loc_Calculator_Add_Is5_And_ClassAndFileSumMethods()
    {
        _dir.WriteFile("Calculator.cs", Samples.LocCalculator);

        var metrics = await Analyzers.Loc(_dir.Path);

        Assert.Equal(5, metrics.Single(m => m.MethodName == "Add").Loc);
        Assert.Equal(5, metrics.Single(m => m.ClassName == "Calculator" && m.MethodName == "").Loc);
        Assert.Equal(5, metrics.Single(m => m.ClassName == "" && m.MethodName == "").Loc);
    }

    [Fact]
    public async Task Loc_Calculator_LogicalIs4_PhysicalIs10()
    {
        _dir.WriteFile("Calculator.cs", Samples.LocCalculator);

        var metrics = await Analyzers.Loc(_dir.Path);
        var file = metrics.Single(m => m.ClassName == "" && m.MethodName == "");

        Assert.Equal(4, file.Lloc);
        Assert.Equal(10, file.PhysicalLoc);
        Assert.Equal(3, metrics.Single(m => m.MethodName == "Add").Lloc);
        Assert.Equal(4, metrics.Single(m => m.ClassName == "Calculator" && m.MethodName == "").Lloc);
    }

    // ---------- docs/cyclomatic.md ----------

    [Fact]
    public async Task Cyclomatic_DiscountService_Is4()
    {
        _dir.WriteFile("Discount.cs", Samples.DiscountService);

        var metrics = await Analyzers.Cyclomatic(_dir.Path);

        Assert.Equal(4, metrics.Single(m => m.MethodName == "CalculateDiscount").Complexity);
    }

    [Fact]
    public async Task Cyclomatic_ShippingCalculator_Fee_Is7()
    {
        _dir.WriteFile("Shipping.cs", Samples.ShippingCalculator);

        var metrics = await Analyzers.Cyclomatic(_dir.Path);

        Assert.Equal(7, metrics.Single(m => m.MethodName == "Fee").Complexity);
        Assert.Equal(2, metrics.Single(m => m.MethodName == "Discount").Complexity);
    }

    // ---------- docs/ck.md ----------

    [Fact]
    public async Task Ck_DocumentedValues()
    {
        _dir.WriteFile("Wmc.cs", Samples.CkWmc);
        _dir.WriteFile("Rfc.cs", Samples.CkRfc);
        _dir.WriteFile("Lcom.cs", Samples.CkLcom);
        _dir.WriteFile("Cbo.cs", Samples.CkCbo);
        _dir.WriteFile("Dit.cs", Samples.CkDit);
        _dir.WriteFile("Noc.cs", Samples.CkNoc);

        var metrics = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);

        Assert.Equal(3, metrics["Calculator"].WMC);
        Assert.Equal(3, metrics["OrderService"].RFC);
        Assert.Equal(4, metrics["User"].LCOM);
        Assert.Equal(2, metrics["PaymentService"].CBO);
        Assert.Equal(0, metrics["PaymentService"].LCOM);
        Assert.Equal(1, metrics["Entity"].DIT);
        Assert.Equal(2, metrics["Person"].DIT);
        Assert.Equal(3, metrics["Employee"].DIT);
        Assert.Equal(2, metrics["Animal"].NOC);
        Assert.Equal(0, metrics["Cat"].NOC);
    }

    [Fact]
    public async Task Ck_Cap2Usuario_LcomIs3_AndAttributesOfOtherObjectsDoNotCount()
    {
        _dir.WriteFile("Usuario.cs", Samples.Cap2Usuario);
        _dir.WriteFile("LcomFalse.cs", Samples.LcomFalse);

        var metrics = (await Analyzers.Ck(_dir.Path)).ToDictionary(m => m.ClassName);

        Assert.Equal(3, metrics["Usuario"].LCOM);
        Assert.Equal(3, metrics["LcomFalse"].LCOM);   // INC-07: Nome of a Dto is not an attribute of LcomFalse
    }

    // ---------- docs/misra.md ----------

    [Fact]
    public async Task Misra_DocumentedValues()
    {
        _dir.WriteFile("Mc.cs", Samples.MisraMc);
        _dir.WriteFile("Ac.cs", Samples.MisraAc);
        _dir.WriteFile("Clc.cs", Samples.MisraClc);
        _dir.WriteFile("Cwc.cs", Samples.MisraCwc);

        var metrics = await Analyzers.Misra(_dir.Path);
        var method = metrics.Where(m => m.MethodName != "").ToDictionary(m => m.MethodName);
        var @class = metrics.Where(m => m.ClassName != "" && m.MethodName == "").ToDictionary(m => m.ClassName);
        var codebase = metrics.Single(m => m.ClassName == "" && m.MethodName == "");

        Assert.Equal(10, method["Process"].MC);
        Assert.Equal(9, method["Total"].MC);
        Assert.Equal(3, method["CalculateDiscount"].MC);
        Assert.Equal(6, @class["Customer"].AC);
        Assert.Equal(3, @class["Order"].AC);
        Assert.Equal(6, @class["Order"].CLC);
        Assert.Equal(8, @class["CartService"].CWC);
        Assert.Equal(55, codebase.CC);
    }
}
