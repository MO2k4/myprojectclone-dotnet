namespace Quality.Cli.Checks;

using System.Globalization;
using Quality.Cli.Msbuild;

internal sealed class UnusedNuGetPackagesCheck : ICheck
{
    public string Id => "unused-nuget-packages";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var findings = new List<string>();
        foreach (var csproj in RepoFileFilter.EnumerateFiles(ctx.RepoRoot, "*.csproj"))
        {
            var projectDir = Path.GetDirectoryName(csproj)!;
            var packages = ProjectInspector.PackageReferencesWithMetadata(csproj);
            var sources = RepoFileFilter.EnumerateFiles(projectDir, "*.cs")
                .SelectMany(File.ReadAllLines)
                .ToArray();

            foreach (var pkg in packages)
            {
                if (IsBuildOnly(pkg))
                {
                    continue;
                }

                // Probe with the full package id as the namespace prefix. Matches
                // both `using Pkg.Name;` and any sub-namespace `using Pkg.Name.X;`
                // via StartsWith. `Split('.')[0]` would collapse Microsoft.* and
                // System.* packages to the same `using Microsoft` / `using System`
                // probe, which any sibling package's usings would falsely satisfy.
                var probe = string.Create(CultureInfo.InvariantCulture, $"using {pkg.Name}");

                // Case-insensitive: NuGet package IDs (e.g. `xunit`) don't always
                // share casing with their root namespace (e.g. `Xunit`).
                var used = sources.Any(l => l.StartsWith(probe, StringComparison.OrdinalIgnoreCase));
                if (!used)
                {
                    findings.Add(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{csproj}: PackageReference '{pkg.Name}' has no `using {pkg.Name}` in any source file"));
                }
            }
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }

    // Skip packages that ship analyzers or build-time MSBuild integration:
    // CPM's GlobalPackageReference sets PrivateAssets to all, and a name-suffix
    // fallback covers cases where PrivateAssets is omitted. The small allowlist
    // catches build-only test infrastructure that does not advertise itself
    // through metadata.
    private static bool IsBuildOnly(PackageReferenceInfo pkg)
    {
        if (pkg.PrivateAssets.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (pkg.Name.EndsWith(".Analyzers", StringComparison.OrdinalIgnoreCase)
            || pkg.Name.EndsWith(".Analyzer", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return pkg.Name.StartsWith("coverlet.", StringComparison.OrdinalIgnoreCase)
            || pkg.Name.Equals("xunit.runner.visualstudio", StringComparison.OrdinalIgnoreCase)
            || pkg.Name.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase);
    }
}
