using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Parsers;

/// <summary>
/// The semantic model only tells calls apart when the framework is referenced and the implicit
/// global usings that the SDK would generate are present.
/// </summary>
public class CodebaseLoaderTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private static string Project(string sdk, bool implicitUsings) => $"""
        <Project Sdk="{sdk}">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>{(implicitUsings ? "enable" : "disable")}</ImplicitUsings>
          </PropertyGroup>
        </Project>
        """;

    private const string UsesBaseLibrary = """
        public class C
        {
            private readonly List<int> _items = new();
            public int M() { _items.Add(1); return _items.Where(i => i > 0).ToList().Count; }
        }
        """;

    private const string UsesAspNetCore = """
        public class Host
        {
            public void Run(WebApplicationBuilder builder) { builder.Build(); }
        }
        """;

    private (int Resolved, int Total) Resolution() =>
        new CodebaseLoader(new RoslynParser())
            .Load(Directory.EnumerateFiles(_dir.Path, "*.cs", SearchOption.AllDirectories), _dir.Path)
            .InvocationResolution();

    [Fact]
    public void Project_IsFound_WhenAllItsFilesAreInOneSubfolder()
    {
        _dir.WriteFile("api/Api.csproj", Project("Microsoft.NET.Sdk", implicitUsings: true));
        _dir.WriteFile("api/Controllers/C.cs", UsesBaseLibrary);

        var codebase = new CodebaseLoader(new RoslynParser())
            .Load(Directory.EnumerateFiles(_dir.Path, "*.cs", SearchOption.AllDirectories), _dir.Path);

        Assert.EndsWith("Api.csproj", Assert.Single(codebase.Documents).Project);
        Assert.Equal((3, 3), codebase.InvocationResolution());
    }

    [Fact]
    public void ImplicitUsings_ResolveBaseLibraryCalls()
    {
        _dir.WriteFile("lib/Lib.csproj", Project("Microsoft.NET.Sdk", implicitUsings: true));
        _dir.WriteFile("lib/C.cs", UsesBaseLibrary);

        Assert.Equal((3, 3), Resolution());
    }

    [Fact]
    public void WithoutImplicitUsings_BaseLibraryCallsNeedExplicitUsings()
    {
        _dir.WriteFile("lib/Lib.csproj", Project("Microsoft.NET.Sdk", implicitUsings: false));
        _dir.WriteFile("lib/C.cs", UsesBaseLibrary);

        Assert.Equal((0, 3), Resolution());
    }

    [Fact]
    public void WebSdk_AddsAspNetCoreImplicitUsings()
    {
        _dir.WriteFile("api/Api.csproj", Project("Microsoft.NET.Sdk.Web", implicitUsings: true));
        _dir.WriteFile("api/Host.cs", UsesAspNetCore);

        Assert.Equal((1, 1), Resolution());
    }

    [Fact]
    public void PlainSdk_DoesNotGetAspNetCoreImplicitUsings()
    {
        _dir.WriteFile("lib/Lib.csproj", Project("Microsoft.NET.Sdk", implicitUsings: true));
        _dir.WriteFile("lib/Host.cs", UsesAspNetCore);

        Assert.Equal((0, 1), Resolution());
    }

    [Fact]
    public void ImplicitUsings_CanComeFromDirectoryBuildProps_AndUsingItems()
    {
        _dir.WriteFile("Directory.Build.props", """
            <Project><PropertyGroup><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>
            """);
        _dir.WriteFile("lib/Lib.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup><Using Include="System.Text" /></ItemGroup>
            </Project>
            """);
        _dir.WriteFile("lib/C.cs", """
            public class C { public string M() { var sb = new StringBuilder(); sb.Append(new List<int>().Count); return sb.ToString(); } }
            """);

        Assert.Equal((2, 2), Resolution());
    }
}
