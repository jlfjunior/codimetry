using Codimetry.Core.Loading;
using Codimetry.Tests.Helpers;

namespace Codimetry.Core.Tests;

public class FileDiscoveryServiceTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly FileDiscoveryService _service = new();

    public void Dispose() => _dir.Dispose();

    private IEnumerable<string?> Names(string path, bool recursive = true) =>
        _service.DiscoverFiles(path, recursive).Select(Path.GetFileName).Order();

    [Fact]
    public void Directory_Recursive_FindsNestedCsFilesOnly()
    {
        _dir.WriteFile("A.cs", "");
        _dir.WriteFile("Sub/B.cs", "");
        _dir.WriteFile("notes.txt", "");

        Assert.Equal(["A.cs", "B.cs"], Names(_dir.Path));
    }

    [Fact]
    public void Directory_NonRecursive_FindsTopLevelOnly()
    {
        _dir.WriteFile("A.cs", "");
        _dir.WriteFile("Sub/B.cs", "");

        Assert.Equal(["A.cs"], Names(_dir.Path, recursive: false));
    }

    [Fact]
    public void ProjectFile_DelegatesToProjectParser()
    {
        var project = _dir.WriteFile("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _dir.WriteFile("App/A.cs", "");

        Assert.Equal(["A.cs"], Names(project));
    }

    [Fact]
    public void SolutionFile_CollectsFilesOfAllProjects()
    {
        _dir.WriteFile("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _dir.WriteFile("App/A.cs", "");
        _dir.WriteFile("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _dir.WriteFile("Lib/B.cs", "");
        var solution = _dir.WriteFile("All.sln", """
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App", "App\App.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Lib", "Lib\Lib.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            """);

        Assert.Equal(["A.cs", "B.cs"], Names(solution));
    }

    [Fact]
    public void UnsupportedFileType_Throws()
    {
        var file = _dir.WriteFile("readme.md", "");

        Assert.Throws<NotSupportedException>(() => _service.DiscoverFiles(file).ToList());
    }

    [Fact]
    public void MissingPath_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => _service.DiscoverFiles(_dir.Combine("missing")).ToList());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyPath_Throws(string path)
    {
        Assert.Throws<ArgumentException>(() => _service.DiscoverFiles(path).ToList());
    }
}
