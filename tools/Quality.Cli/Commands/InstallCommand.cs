namespace Quality.Cli.Commands;

using System.Globalization;
using System.Reflection;
using Quality.Cli.Msbuild;

internal static class InstallCommand
{
    private static readonly (string ResourceName, string TargetRelativePath)[] Files =
    [
        ("Directory.Build.props", "Directory.Build.props"),
        ("Directory.Packages.props", "Directory.Packages.props"),
        ("Directory.Build.targets", "Directory.Build.targets"),
        (".editorconfig", ".editorconfig"),
        (".quality.toml", ".quality.toml"),
        (".pre-commit-config.yaml", ".pre-commit-config.yaml"),
        ("semgrep.arch.yml", ".semgrep/arch.yml"),
        ("semgrep.security.yml", ".semgrep/security.yml"),
        ("semgrep.logging.yml", ".semgrep/logging.yml"),
    ];

    private enum FileAction
    {
        Write,
        Skip,
        Overwrite,
    }

    public static int Run(string targetRoot, bool force = false)
    {
        ArgumentNullException.ThrowIfNull(targetRoot);

        var asm = typeof(InstallCommand).Assembly;

        // Plan phase — no disk writes. Resolve every embedded resource up front so a
        // packaging defect fails before any file is touched.
        var plans = new List<FilePlan>(Files.Length);
        foreach (var (res, rel) in Files)
        {
            using (var probe = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"missing embedded resource '{res}' — packaging defect, see tools/Quality.Cli/Quality.Cli.csproj")))
            {
                // Stream is disposed immediately; re-opened in the apply phase.
            }

            var target = Path.Combine(targetRoot, rel);
            FileAction action;
            if (!File.Exists(target))
            {
                action = FileAction.Write;
            }
            else
            {
                action = force ? FileAction.Overwrite : FileAction.Skip;
            }

            plans.Add(new FilePlan(res, target, action));
        }

        // Apply phase.
        var written = 0;
        var skipped = 0;
        foreach (var plan in plans)
        {
            var rel = Path.GetRelativePath(targetRoot, plan.Target);
            if (plan.Action == FileAction.Skip)
            {
                skipped++;
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  skipped  {rel} (exists)"));
                continue;
            }

            WriteResource(asm, plan.Resource, plan.Target);
            written++;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  wrote    {rel}"));
        }

        AutoAttachSerilogAnalyzer(targetRoot);

        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{written} written, {skipped} skipped. Re-run with --force to overwrite skipped files."));
        return 0;
    }

    private static void WriteResource(Assembly asm, string resource, string target)
    {
        var dir = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var s = asm.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"missing embedded resource '{resource}' — packaging defect, see tools/Quality.Cli/Quality.Cli.csproj"));

        var temp = target + ".tmp-install";
        try
        {
            using (var fs = File.Create(temp))
            {
                s.CopyTo(fs);
            }

            File.Move(temp, target, overwrite: true);
        }
        catch
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }

            throw;
        }
    }

    private static void AutoAttachSerilogAnalyzer(string root)
    {
        foreach (var csproj in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            var pkgs = ProjectInspector.PackageReferences(csproj);
            if (pkgs.Any(p => p.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase)))
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

    private sealed record FilePlan(string Resource, string Target, FileAction Action);
}
