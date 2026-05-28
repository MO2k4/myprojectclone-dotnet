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
        foreach (var csproj in EnumerateProjectFiles(ctx.RepoRoot))
        {
            var projectDir = Path.GetDirectoryName(csproj)!;
            var packages = ProjectInspector.PackageReferencesWithMetadata(csproj);
            var sources = Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .SelectMany(File.ReadAllLines)
                .ToArray();

            foreach (var pkg in packages)
            {
                if (IsBuildOnly(pkg))
                {
                    continue;
                }

                var root = pkg.Name.Split('.')[0];
                var probe = $"using {root}";

                // Case-insensitive: NuGet package IDs (e.g. `xunit`) don't always
                // share casing with their root namespace (e.g. `Xunit`).
                var used = sources.Any(l => l.StartsWith(probe, StringComparison.OrdinalIgnoreCase));
                if (!used)
                {
                    findings.Add(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{csproj}: PackageReference '{pkg.Name}' has no `using {root}` in any source file"));
                }
            }
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }

    // Skip csproj files under `_fixtures/` relative to the scanned root.
    // The path-segment check is on the RELATIVE path so callers can still
    // scan inside a fixture directory directly (used by unit tests).
    private static IEnumerable<string> EnumerateProjectFiles(string root)
    {
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        foreach (var csproj in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(root, csproj);
            var segments = rel.Split(separators, StringSplitOptions.None);
            if (Array.Exists(segments, s => s.Equals("_fixtures", StringComparison.Ordinal)))
            {
                continue;
            }

            yield return csproj;
        }
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
