namespace Quality.Cli.Commands;

using System.Globalization;
using Quality.Cli.Msbuild;

internal static class InstallCommand
{
    private static readonly (string ResourceName, string TargetRelativePath)[] Files =
    [
        ("Directory.Build.props", "Directory.Build.props"),
        ("Directory.Packages.props", "Directory.Packages.props"),
        (".editorconfig", ".editorconfig"),
        (".quality.toml", ".quality.toml"),
    ];

    public static int Run(string targetRoot)
    {
        ArgumentNullException.ThrowIfNull(targetRoot);

        var asm = typeof(InstallCommand).Assembly;
        foreach (var (res, rel) in Files)
        {
            var target = Path.Combine(targetRoot, rel);
            var dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var s = asm.GetManifestResourceStream(res)!;
            using var fs = File.Create(target);
            s.CopyTo(fs);
        }

        AutoAttachSerilogAnalyzer(targetRoot);
        return 0;
    }

    private static void AutoAttachSerilogAnalyzer(string root)
    {
        foreach (var csproj in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            var pkgs = ProjectInspector.PackageReferences(csproj);
            if (pkgs.Any(p => p.StartsWith("Serilog", StringComparison.Ordinal)))
            {
                AppendPackageVersionToPackagesProps(root, "SerilogAnalyzer", "0.15.0");
                return;
            }
        }
    }

    private static void AppendPackageVersionToPackagesProps(string root, string id, string version)
    {
        var path = Path.Combine(root, "Directory.Packages.props");
        var text = File.ReadAllText(path);
        if (text.Contains(string.Create(CultureInfo.InvariantCulture, $"Include=\"{id}\""), StringComparison.Ordinal))
        {
            return;
        }

        var injection = string.Create(
            CultureInfo.InvariantCulture,
            $"  <ItemGroup>\n    <GlobalPackageReference Include=\"{id}\" Version=\"{version}\" />\n  </ItemGroup>\n</Project>");
        text = text.Replace("</Project>", injection, StringComparison.Ordinal);
        File.WriteAllText(path, text);
    }
}
