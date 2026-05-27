namespace Quality.Cli.Tests.Checks;

using Quality.Cli.Checks;
using Quality.Cli.Config;
using Xunit;

public class LockfileIntegrityCheckTests
{
    [Fact]
    public void Locked_restore_succeeds_on_clean_repo()
    {
        var repoRoot = LocateRepoRoot();

        var result = new LockfileIntegrityCheck().Run(new CheckContext(repoRoot, new QualityConfig()));

        Assert.True(result.Ok, string.Join('\n', result.Findings));
    }

    private static string LocateRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MyProjectClone.Dotnet.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
