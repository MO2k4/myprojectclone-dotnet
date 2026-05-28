namespace Quality.Cli.Checks;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Quality.Cli.Process;

internal sealed class LicenseCheck : ICheck
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string Id => "license-check";

    public static IReadOnlyList<string> Evaluate(string json, IReadOnlyList<string> denylist)
    {
        ArgumentNullException.ThrowIfNull(denylist);

        var packages = JsonSerializer.Deserialize<List<PackageLicense>>(json, JsonOptions) ?? [];
        var denied = new HashSet<string>(denylist, StringComparer.OrdinalIgnoreCase);

        return packages
            .Where(p => p.LicenseType is not null && denied.Contains(p.LicenseType))
            .Select(p => string.Create(
                CultureInfo.InvariantCulture,
                $"{p.PackageName} {p.PackageVersion}: {p.LicenseType} is on the denylist"))
            .ToList();
    }

    [ExcludeFromCodeCoverage(Justification = "Shells out to `dotnet tool run dotnet-project-licenses`; exercised by the integration smoke test against the real repo root, not branch-asserted here.")]
    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var jsonPath = Path.Combine(Path.GetTempPath(), string.Create(CultureInfo.InvariantCulture, $"licenses-{Guid.NewGuid():N}.json"));
        try
        {
            var args = string.Create(
                CultureInfo.InvariantCulture,
                $"tool run dotnet-project-licenses -- --input \"{ctx.RepoRoot}\" --json --outfile \"{jsonPath}\" --include-transitive");

            var (exitCode, _, stderr, timedOut) = ProcessRunner.Run(
                "dotnet", args, ctx.RepoRoot, TimeSpan.FromMinutes(5));

            if (timedOut)
            {
                return new CheckResult(this.Id, false, ["dotnet-project-licenses timed out after 5 minutes"]);
            }

            if (exitCode != 0)
            {
                return new CheckResult(this.Id, false, ["dotnet-project-licenses failed", stderr]);
            }

            var denylist = ctx.Config.Checks.TryGetValue(this.Id, out var entry)
                ? (IReadOnlyList<string>)(entry.Denylist ?? [])
                : [];
            var findings = Evaluate(File.ReadAllText(jsonPath), denylist);
            return new CheckResult(this.Id, findings.Count == 0, findings);
        }
        finally
        {
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }
        }
    }
}
