namespace Quality.Cli.Checks;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Quality.Cli.Msbuild;

internal sealed class EfMigrationsDriftCheck : ICheck
{
    private const string EfDesignPackage = "Microsoft.EntityFrameworkCore.Design";

    public string Id => "ef-migrations-drift";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var findings = new List<string>();
        foreach (var csproj in Directory.EnumerateFiles(ctx.RepoRoot, "*.csproj", SearchOption.AllDirectories))
        {
            EvaluateProject(csproj, findings);
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }

    [ExcludeFromCodeCoverage(Justification = "Branches into `dotnet ef` shell-out path; exercised by integration tests in Phase H, not unit-asserted here.")]
    private static void EvaluateProject(string csprojPath, List<string> findings)
    {
        var packages = ProjectInspector.PackageReferences(csprojPath);
        if (!packages.Contains(EfDesignPackage, StringComparer.Ordinal))
        {
            return;
        }

        if (HasPendingModelChanges(csprojPath))
        {
            findings.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{csprojPath}: pending model changes (run `dotnet ef migrations add`)"));
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Shells out to `dotnet ef`; exercised by integration tests in Phase H, not unit-asserted here.")]
    private static bool HasPendingModelChanges(string csprojPath)
    {
        var psi = new ProcessStartInfo("dotnet", $"ef migrations has-pending-model-changes --project \"{csprojPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        return proc.ExitCode != 0;
    }
}
