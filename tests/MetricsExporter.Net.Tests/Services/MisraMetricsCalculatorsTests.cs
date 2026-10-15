using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Services;

namespace MetricsExporter.Net.Tests.Services;

public class MisraMetricsCalculatorsTests
{
    // MC values used throughout: C::A = 3, C::B = 5, Other::Ext = 4
    private static readonly Dictionary<string, BcsInfo> Bcs = new()
    {
        ["C::A"] = new BcsInfo { SequenceCount = 1, CallCount = 1 },
        ["C::B"] = new BcsInfo { SequenceCount = 1, BranchCount = 2 },
        ["Other::Ext"] = new BcsInfo { SequenceCount = 2, BranchCount = 1 }
    };

    private static readonly Dictionary<string, List<string>> Attributes = new()
    {
        ["C"] = ["_id", "_name", "_address", "_items"],
        ["Other"] = ["_x"]
    };

    private static readonly Dictionary<string, List<string>> AttributeTypes = new()
    {
        ["C"] = ["int", "string?", "Address", "List<int>"],
        ["Other"] = ["Guid"]
    };

    private static CkMethodInfo Method(string name, string @class, params string[] calls) =>
        new() { Name = name, Key = name, ContainingClass = @class, ContainingClassKey = @class, CalledMethods = [.. calls] };

    // ---------- MC ----------

    [Fact]
    public void Mc_ReturnsWeightedSum_OrZeroForUnknownMethod()
    {
        var calculator = new McCalculator();

        Assert.Equal(3, calculator.CalculateMc("C", "A", Bcs));
        Assert.Equal(0, calculator.CalculateMc("C", "Missing", Bcs));
    }

    [Fact]
    public void Mc_GetMethodComplexities_ReturnsOnlyTheClassMethods()
    {
        var result = new McCalculator().GetMethodComplexities("C", Bcs);

        Assert.Equal(new Dictionary<string, int> { ["A"] = 3, ["B"] = 5 }, result);
    }

    // ---------- AC ----------

    [Fact]
    public void Ac_CountsEachAttributeOnce_WhateverItsType()
    {
        Assert.Equal(4, new AcCalculator().CalculateAc("C", Attributes, AttributeTypes));
    }

    [Fact]
    public void Ac_WithTypeWeights_WeightsSimpleTypesAs1_AndOthersAs2()
    {
        // int (1) + string? (1) + Address (2) + List<int> (2)
        Assert.Equal(6, new AcCalculator(typeWeights: true).CalculateAc("C", Attributes, AttributeTypes));
    }

    [Fact]
    public void Ac_IsZero_WithoutAttributes()
    {
        Assert.Equal(0, new AcCalculator().CalculateAc("Empty", Attributes, AttributeTypes));
    }

    [Fact]
    public void Ac_FallsBackToCount_WithoutTypeInformation()
    {
        Assert.Equal(4, new AcCalculator(typeWeights: true).CalculateAc("C", Attributes, new Dictionary<string, List<string>>()));
    }

    // ---------- CLC ----------

    [Fact]
    public void Clc_IsAcPlusSumOfMc()
    {
        Assert.Equal(4 + 3 + 5, new ClcCalculator().CalculateClc("C", Bcs, Attributes, AttributeTypes));
    }

    // ---------- CWC ----------

    [Fact]
    public void Cwc_ExternalCall_AddsCalledMcPlus2()
    {
        List<CkMethodInfo> methods = [Method("A", "C", "Ext"), Method("Ext", "Other")];

        Assert.Equal(4 + 2, new CwcCalculator().CalculateCwc("C", methods, Bcs, new Dictionary<string, ClassRelationshipBuilder.ClassInfo>()));
    }

    [Fact]
    public void Cwc_UnresolvedCall_Adds3()
    {
        List<CkMethodInfo> methods = [Method("A", "C", "WriteLine")];

        Assert.Equal(3, new CwcCalculator().CalculateCwc("C", methods, Bcs, new Dictionary<string, ClassRelationshipBuilder.ClassInfo>()));
    }

    [Fact]
    public void Cwc_CallToOwnMethod_IsIgnored()
    {
        // Regression: internal calls used to be counted as unresolved external calls (+3)
        List<CkMethodInfo> methods = [Method("A", "C", "B"), Method("B", "C")];

        Assert.Equal(0, new CwcCalculator().CalculateCwc("C", methods, Bcs, new Dictionary<string, ClassRelationshipBuilder.ClassInfo>()));
    }

    [Fact]
    public void Cwc_RepeatedCalls_AreCountedEachTime()
    {
        List<CkMethodInfo> methods = [Method("A", "C", "Ext", "Ext"), Method("Ext", "Other")];

        Assert.Equal(2 * (4 + 2), new CwcCalculator().CalculateCwc("C", methods, Bcs, new Dictionary<string, ClassRelationshipBuilder.ClassInfo>()));
    }

    // ---------- CC (CoC) ----------

    [Fact]
    public void Cc_IsSumOfClcPlusHalfTheSumOfCwc()
    {
        List<CkMethodInfo> methods = [Method("A", "C", "Ext", "WriteLine"), Method("B", "C"), Method("Ext", "Other")];

        // CLC(C) = 4 + 3 + 5 = 12, CLC(Other) = 1 + 4 = 5; CWC(C) = 6 + 3 = 9, CWC(Other) = 0
        var cc = new CcCalculator().CalculateCc(["C", "Other"], Bcs, Attributes, AttributeTypes, methods,
            new Dictionary<string, ClassRelationshipBuilder.ClassInfo>());

        Assert.Equal(12 + 5 + 9 / 2, cc);
    }
}
