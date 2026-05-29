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

    [Fact]
    public void Splits_each_stdout_line_into_its_own_finding()
    {
        // finding #12: a failed locked restore emits one diagnostic per project; each must
        // land on its own row rather than being dumped as a single giant finding.
        const string stdout = "Project A is out of date\nProject B is out of date\nProject C is out of date\n";

        var findings = LockfileIntegrityCheck.SplitStreamsIntoLines(stdout, string.Empty);

        Assert.Equal(
            ["Project A is out of date", "Project B is out of date", "Project C is out of date"],
            findings);
    }

    [Fact]
    public void Merges_stdout_and_stderr_and_skips_blank_lines()
    {
        // CRLF endings, blank/whitespace-only lines, and both streams combine into a flat,
        // blank-free list — leading indentation NuGet uses is preserved.
        const string stdout = "out line 1\r\n\r\n   \r\nout line 2\r\n";
        const string stderr = "err line 1\n";

        var findings = LockfileIntegrityCheck.SplitStreamsIntoLines(stdout, stderr);

        Assert.Equal(["out line 1", "out line 2", "err line 1"], findings);
    }

    [Fact]
    public void Returns_empty_when_both_streams_are_empty()
    {
        Assert.Empty(LockfileIntegrityCheck.SplitStreamsIntoLines(string.Empty, string.Empty));
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
