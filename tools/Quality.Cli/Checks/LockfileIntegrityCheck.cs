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

        var findings = new List<string>();
        if (stdout.Length > 0)
        {
            findings.Add(stdout);
        }

        if (stderr.Length > 0)
        {
            findings.Add(stderr);
        }

        return new CheckResult(this.Id, false, findings);
    }
}
