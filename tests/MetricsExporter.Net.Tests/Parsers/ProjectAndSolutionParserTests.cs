using MetricsExporter.Net.Parsers;
using MetricsExporter.Net.Tests.Helpers;

namespace MetricsExporter.Net.Tests.Parsers;

public class ProjectAndSolutionParserTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private const string SdkProject = """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""";

    // ---------- ProjectParser ----------

    [Fact]
    public void SdkProject_ScansDirectory_ExcludingBinAndObj()
    {
        var project = _dir.WriteFile("App/App.csproj", SdkProject);
        _dir.WriteFile("App/Program.cs", "class P { }");
        _dir.WriteFile("App/Sub/Service.cs", "class S { }");
        _dir.WriteFile("App/bin/Debug/Gen.cs", "class G { }");
        _dir.WriteFile("App/obj/Debug/Gen.cs", "class G { }");

        var files = new ProjectParser().ExtractSourceFilePaths(project).Select(Path.GetFileName).Order();

        Assert.Equal(["Program.cs", "Service.cs"], files);
    }

    [Fact]
    public void ExplicitCompileItems_AreIncludedOnce()
    {
        var project = _dir.WriteFile("App/App.csproj", """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemGroup>
                <Compile Include="Code\A.cs" />
                <Compile Include="Missing.cs" />
              </ItemGroup>
            </Project>
            """);
        _dir.WriteFile("App/Code/A.cs", "class A { }");

        var files = new ProjectParser().ExtractSourceFilePaths(project).ToList();

        Assert.Single(files);
        Assert.EndsWith(Path.Combine("Code", "A.cs"), files[0]);
    }

    [Fact]
    public void WildcardCompileItems_AreExpanded()
    {
        var project = _dir.WriteFile("App/App.csproj", """
            <Project><ItemGroup><Compile Include="Src\**\*.cs" /></ItemGroup></Project>
            """);
        _dir.WriteFile("App/Src/A.cs", "class A { }");
        _dir.WriteFile("App/Src/Deep/B.cs", "class B { }");

        var files = new ProjectParser().ExtractSourceFilePaths(project).Select(Path.GetFileName).Order();

        Assert.Equal(["A.cs", "B.cs"], files);
    }

    [Fact]
    public void InvalidXml_FallsBackToDirectoryScan()
    {
        var project = _dir.WriteFile("App/App.csproj", "<Project><not-closed>");
        _dir.WriteFile("App/A.cs", "class A { }");

        Assert.Equal(["A.cs"], new ProjectParser().ExtractSourceFilePaths(project).Select(Path.GetFileName));
    }

    [Fact]
    public void MissingProject_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => new ProjectParser().ExtractSourceFilePaths(_dir.Combine("Nope.csproj")).ToList());
    }

    // ---------- SolutionParser ----------

    [Fact]
    public void Solution_ReturnsExistingCsprojPaths_WithWindowsSeparators()
    {
        _dir.WriteFile("src/App/App.csproj", SdkProject);
        var solution = _dir.WriteFile("All.sln", """
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App", "src\App\App.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Gone", "src\Gone\Gone.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "docs", "docs", "{33333333-3333-3333-3333-333333333333}"
            EndProject
            """);

        var projects = new SolutionParser().ExtractProjectPaths(solution).ToList();

        Assert.Equal([_dir.Combine("src", "App", "App.csproj")], projects.Select(Path.GetFullPath));
    }

    [Fact]
    public void MissingSolution_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => new SolutionParser().ExtractProjectPaths(_dir.Combine("Nope.sln")));
    }

    // ---------- RoslynParser ----------

    [Fact]
    public void RoslynParser_ParsesFile_EvenWithSyntaxErrors()
    {
        var valid = _dir.WriteFile("Valid.cs", "class A { }");
        var broken = _dir.WriteFile("Broken.cs", "class A { void M( }");

        var parser = new RoslynParser();

        Assert.NotNull(parser.ParseFile(valid));
        Assert.NotNull(parser.ParseFile(broken));
        Assert.Equal(valid, parser.ParseFile(valid)!.FilePath);
    }

    [Fact]
    public void RoslynParser_MissingFile_ReturnsNull()
    {
        Assert.Null(new RoslynParser().ParseFile(_dir.Combine("Missing.cs")));
    }
}
