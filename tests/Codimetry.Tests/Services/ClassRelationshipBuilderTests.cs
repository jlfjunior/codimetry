using Codimetry.Parsers;
using Codimetry.Services;

namespace Codimetry.Tests.Services;

public class ClassRelationshipBuilderTests
{
    private static CkCodeElement Class(string name, string? baseClass = null, string? ns = null, string? fqn = null) =>
        new() { Name = name, BaseClass = baseClass, Namespace = ns, FullyQualifiedName = fqn };

    [Fact]
    public void InheritanceChain_GoesFromClassToRoot()
    {
        var builder = new ClassRelationshipBuilder();
        builder.AddClass(Class("A"), "a.cs");
        builder.AddClass(Class("B", baseClass: "A"), "b.cs");
        builder.AddClass(Class("C", baseClass: "B"), "c.cs");

        Assert.Equal(["C", "B", "A"], builder.GetInheritanceChain("C"));
        Assert.Equal("B", builder.GetBaseClass("C"));
    }

    [Fact]
    public void InheritanceChain_IncludesUnresolvedExternalBase()
    {
        var builder = new ClassRelationshipBuilder();
        builder.AddClass(Class("HomeController", baseClass: "ControllerBase"), "h.cs");

        Assert.Equal(["HomeController", "ControllerBase"], builder.GetInheritanceChain("HomeController"));
    }

    [Fact]
    public void InheritanceChain_StopsOnCycle()
    {
        var builder = new ClassRelationshipBuilder();
        builder.AddClass(Class("A", baseClass: "B"), "a.cs");
        builder.AddClass(Class("B", baseClass: "A"), "b.cs");

        Assert.Equal(["A", "B"], builder.GetInheritanceChain("A"));
    }

    [Fact]
    public void Children_AreDirectSubclassesWithoutDuplicates()
    {
        var builder = new ClassRelationshipBuilder();
        builder.AddClass(Class("Animal"), "a.cs");
        builder.AddClass(Class("Cat", baseClass: "Animal"), "c.cs");
        builder.AddClass(Class("Cat", baseClass: "Animal"), "c2.cs");
        builder.AddClass(Class("Dog", baseClass: "Animal"), "d.cs");

        Assert.Equal(["Cat", "Dog"], builder.GetChildren("Animal"));
        Assert.Empty(builder.GetChildren("Cat"));
    }

    [Fact]
    public void GenericArgumentsAreIgnoredForMatching()
    {
        var builder = new ClassRelationshipBuilder();
        builder.AddClass(Class("Repository<T>"), "r.cs");
        builder.AddClass(Class("UserRepository", baseClass: "Repository<User>"), "u.cs");

        Assert.Equal(["UserRepository"], builder.GetChildren("Repository<int>"));
    }

    [Fact]
    public void Key_PrefersFullyQualifiedName_ThenNamespace_ThenName()
    {
        var builder = new ClassRelationshipBuilder();
        builder.AddClass(Class("A", fqn: "global::X.A"), "a.cs");
        builder.AddClass(Class("B", ns: "Y"), "b.cs");
        builder.AddClass(Class("C"), "c.cs");

        Assert.Equal(["global::X.A", "Y.B", "C"], builder.GetAllClasses().Keys);
        Assert.Equal("b.cs", builder.GetAllClasses()["Y.B"].FilePath);
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        var builder = new ClassRelationshipBuilder();
        builder.AddClass(Class("A"), "a.cs");
        builder.AddClass(Class("B", baseClass: "A"), "b.cs");

        builder.Clear();

        Assert.Empty(builder.GetAllClasses());
        Assert.Empty(builder.GetChildren("A"));
        Assert.Null(builder.GetBaseClass("B"));
    }
}
