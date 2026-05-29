namespace Quality.Cli.Checks;

using System.Diagnostics.CodeAnalysis;
using Quality.Cli.Process;

internal sealed class LockfileIntegrityCheck : ICheck
{
    public string Id => "lockfile-integrity";

    [ExcludeFromCodeCoverage(Justification = "Shells out to `dotnet restore --locked-mode`; exercised by the integration smoke test against the real repo root, not branch-asserted here.")]
    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (exitCode, stdout, stderr, timedOut) = ProcessRunner.Run(
            "dotnet", "restore --locked-mode", ctx.RepoRoot, TimeSpan.FromMinutes(5));

        if (timedOut)
        {
            return new CheckResult(this.Id, false, ["dotnet restore --locked-mode timed out after 5 minutes"]);
        }

        if (exitCode == 0)
        {
            return new CheckResult(this.Id, true, Array.Empty<string>());
        }

        return new CheckResult(this.Id, false, SplitStreamsIntoLines(stdout, stderr));
    }

    // `dotnet restore --locked-mode` emits one diagnostic per offending project plus
    // NuGet chatter. Adding the whole buffer as a single finding makes Spectre render it
    // as one giant row that wraps/truncates to terminal width, burying the project that
    // actually drifted. Split both streams into individual lines so each shows on its own
    // row. Pure + internal so it can be unit-asserted without shelling out (the Run path
    // stays ExcludeFromCodeCoverage).
    internal static IReadOnlyList<string> SplitStreamsIntoLines(string stdout, string stderr)
    {
        var findings = new List<string>();
        AddNonEmptyLines(findings, stdout);
        AddNonEmptyLines(findings, stderr);
        return findings;
    }

    private static void AddNonEmptyLines(List<string> findings, string stream)
    {
        foreach (var line in stream.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                findings.Add(trimmed);
            }
        }
    }
}
