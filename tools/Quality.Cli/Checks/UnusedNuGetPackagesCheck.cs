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
        foreach (var csproj in Directory.EnumerateFiles(ctx.RepoRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var projectDir = Path.GetDirectoryName(csproj)!;
            var packages = ProjectInspector.PackageReferences(csproj);
            var sources = Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .SelectMany(File.ReadAllLines)
                .ToArray();

            foreach (var pkg in packages)
            {
                var root = pkg.Split('.')[0];
                var probe = $"using {root}";
                var used = sources.Any(l => l.StartsWith(probe, StringComparison.Ordinal));
                if (!used)
                {
                    findings.Add(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{csproj}: PackageReference '{pkg}' has no `using {root}` in any source file"));
                }
            }
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }
}
