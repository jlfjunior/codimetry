using Codimetry.Parsers;
using Codimetry.Services;

namespace Codimetry.Tests.Services;

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

    // Measured types of the analysed code
    private static readonly Dictionary<string, ClassRelationshipBuilder.ClassInfo> Classes = new()
    {
        ["C"] = new() { Name = "C" },
        ["Other"] = new() { Name = "Other" }
    };

    private static CallInfo ToSystem(string @class, string method, bool isAbstract = false) =>
        new(method, $"{@class}::{method}()", @class, $"{@class}::{method}", isAbstract);

    private static CallInfo ToLibrary(string method) => new(method, $"System.Console::{method}()", "System.Console");

    private static CkMethodInfo Calling(string name, string @class, params CallInfo[] calls) =>
        new() { Name = name, Key = name, ContainingClass = @class, ContainingClassKey = @class, Calls = [.. calls] };

    private static int Cwc(string @class, IReadOnlyList<CkMethodInfo> methods, int libraryWeight = 0,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? implementations = null) =>
        new CwcCalculator(libraryWeight).CalculateCwc(@class, methods, Bcs, Classes, implementations);

    [Fact]
    public void Cwc_CallToAnotherTypeOfTheCode_AddsTwoPlusItsMc()
    {
        Assert.Equal(2 + 4, Cwc("C", [Calling("A", "C", ToSystem("Other", "Ext"))]));
    }

    [Fact]
    public void Cwc_RepeatedCalls_AreCountedEachTime()
    {
        Assert.Equal(2 * (2 + 4), Cwc("C", [Calling("A", "C", ToSystem("Other", "Ext"), ToSystem("Other", "Ext"))]));
    }

    [Fact]
    public void Cwc_CallToOwnMethod_IsIgnored()
    {
        Assert.Equal(0, Cwc("C", [Calling("A", "C", ToSystem("C", "B"))]));
    }

    [Fact]
    public void Cwc_LibraryCalls_AddTheLibraryWeight_ZeroByDefault()
    {
        List<CkMethodInfo> methods = [Calling("A", "C", ToLibrary("WriteLine"), ToLibrary("Write"))];

        Assert.Equal(0, Cwc("C", methods));
        Assert.Equal(2 * 3, Cwc("C", methods, libraryWeight: 3));   // the legacy library rule
    }

    [Fact]
    public void Cwc_UnresolvedCalls_AreNotCounted_NorMatchedByName()
    {
        // Earlier versions matched "Ext" with the first method of that name in any class
        List<CkMethodInfo> methods = [Method("A", "C", "Ext", "WriteLine")];

        Assert.Equal(0, Cwc("C", methods));
        Assert.Equal(2, CwcCalculator.UnresolvedCalls("C", methods));
    }

    [Fact]
    public void Cwc_AbstractCall_UsesTheOnlyImplementation_Else2Plus1()
    {
        var call = ToSystem("IRepo", "Save", isAbstract: true);
        List<CkMethodInfo> methods = [Calling("A", "C", call)];

        Assert.Equal(2 + 4, Cwc("C", methods, implementations: new Dictionary<string, IReadOnlyList<string>> { ["IRepo::Save"] = ["Other::Ext"] }));
        Assert.Equal(2 + 1, Cwc("C", methods, implementations: new Dictionary<string, IReadOnlyList<string>> { ["IRepo::Save"] = ["Other::Ext", "C::B"] }));
        Assert.Equal(2 + 1, Cwc("C", methods));
    }

    [Fact]
    public void Cwc_CallToATypeOutsideTheScope_IsNotCounted()
    {
        // e.g. a record method with --classes-only
        Assert.Equal(0, Cwc("C", [Calling("A", "C", ToSystem("Record", "M"))]));
    }

    // ---------- CC (CoC) ----------

    [Fact]
    public void Cc_IsSumOfClcPlusHalfTheSumOfCwc()
    {
        List<CkMethodInfo> methods = [Calling("A", "C", ToSystem("Other", "Ext"), ToLibrary("WriteLine")), Calling("B", "C")];

        // CLC(C) = 4 + 3 + 5 = 12, CLC(Other) = 1 + 4 = 5; CWC(C) = 2 + 4 = 6, CWC(Other) = 0
        var cc = new CcCalculator().CalculateCc(["C", "Other"], Bcs, Attributes, AttributeTypes, methods, Classes);

        Assert.Equal(12 + 5 + 6 / 2, cc);
    }
}
