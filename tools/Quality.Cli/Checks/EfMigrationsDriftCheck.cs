namespace Quality.Cli.Checks;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Quality.Cli.Msbuild;
using Quality.Cli.Process;

internal sealed class EfMigrationsDriftCheck : ICheck
{
    private const string EfDesignPackage = "Microsoft.EntityFrameworkCore.Design";

    public string Id => "ef-migrations-drift";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var findings = new List<string>();
        foreach (var csproj in RepoFileFilter.EnumerateFiles(ctx.RepoRoot, "*.csproj"))
        {
            EvaluateProject(csproj, findings);
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }

    // internal for unit tests — the surrounding shell-out is excluded from
    // coverage but the pattern set itself is load-bearing for drift/error
    // disambiguation, so test it directly.
    internal static bool LooksLikeToolFailure(string stderr) =>
        stderr.Contains("Could not execute", StringComparison.Ordinal)
        || stderr.Contains("specified command or file was not found", StringComparison.Ordinal)
        || stderr.Contains("Build FAILED", StringComparison.Ordinal)
        || stderr.Contains("Build failed", StringComparison.Ordinal);

    [ExcludeFromCodeCoverage(Justification = "Branches into `dotnet ef` shell-out path; exercised by integration tests in Phase H, not unit-asserted here.")]
    private static void EvaluateProject(string csprojPath, List<string> findings)
    {
        var packages = ProjectInspector.PackageReferences(csprojPath);
        if (!packages.Contains(EfDesignPackage, StringComparer.Ordinal))
        {
            return;
        }

        var (drift, error) = CheckPendingModelChanges(csprojPath);
        if (error is not null)
        {
            findings.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{csprojPath}: ef-migrations-drift tool error: {error}"));
            return;
        }

        if (drift)
        {
            findings.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{csprojPath}: pending model changes (run `dotnet ef migrations add`)"));
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Shells out to `dotnet ef`; exercised by integration tests in Phase H, not unit-asserted here.")]
    private static (bool Drift, string? Error) CheckPendingModelChanges(string csprojPath)
    {
        var args = string.Create(
            CultureInfo.InvariantCulture,
            $"ef migrations has-pending-model-changes --project \"{csprojPath}\"");

        var (exitCode, _, stderr, timedOut) = ProcessRunner.Run(
            "dotnet", args, workingDir: null, TimeSpan.FromMinutes(5));

        if (timedOut)
        {
            return (false, "dotnet ef timed out after 5 minutes");
        }

        if (exitCode == 0)
        {
            return (false, null);
        }

        // `dotnet ef has-pending-model-changes` exits non-zero either on real drift
        // or on tool/build failure. Distinguish via stderr signatures so consumers
        // don't chase false drift when dotnet-ef isn't installed or the project
        // doesn't build.
        if (LooksLikeToolFailure(stderr))
        {
            return (false, stderr.Trim());
        }

        return (true, null);
    }
}
