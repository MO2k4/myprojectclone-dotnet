namespace Quality.Cli.Tests.Resources;

using System.Reflection;
using Quality.Cli.Commands;
using Xunit;

public class EmbeddedResourcesDriftTests
{
    // Each pair: (embedded resource logical name, repo-relative path to the root copy).
    // The set mirrors the Files table in InstallCommand.cs — keep in sync when adding
    // a new template.
    public static TheoryData<string, string> ResourcePairs() => new()
    {
        { "Directory.Build.props", "Directory.Build.props" },
        { "Directory.Packages.props", "Directory.Packages.props" },
        { "Directory.Build.targets", "Directory.Build.targets" },
        { ".editorconfig", ".editorconfig" },
        { ".quality.toml", ".quality.toml" },
        { ".pre-commit-config.yaml", ".pre-commit-config.yaml" },
        { "semgrep.arch.yml", ".semgrep/arch.yml" },
        { "semgrep.security.yml", ".semgrep/security.yml" },
        { "semgrep.logging.yml", ".semgrep/logging.yml" },
    };

    [Theory]
    [MemberData(nameof(ResourcePairs))]
    public void Embedded_resource_matches_root_copy(string logicalName, string rootRelativePath)
    {
        var asm = typeof(InstallCommand).Assembly;
        using var stream = asm.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"missing embedded resource '{logicalName}' — check Quality.Cli.csproj <EmbeddedResource> LogicalName attributes");
        using var reader = new StreamReader(stream);
        var embedded = reader.ReadToEnd();

        var rootPath = Path.Combine(LocateRepoRoot(), rootRelativePath);
        var root = File.ReadAllText(rootPath);

        Assert.Equal(root, embedded);
    }

    private static string LocateRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MyProjectClone.Dotnet.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("MyProjectClone.Dotnet.sln not found above AppContext.BaseDirectory");
    }
}
