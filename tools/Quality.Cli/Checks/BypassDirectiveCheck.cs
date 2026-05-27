namespace Quality.Cli.Checks;

using System.Globalization;
using System.Text.RegularExpressions;

internal sealed partial class BypassDirectiveCheck : ICheck
{
    private static readonly char[] PathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public string Id => "bypass-directive-check";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var findings = new List<string>();
        foreach (var path in Directory.EnumerateFiles(ctx.RepoRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(ctx.RepoRoot, path))
            {
                continue;
            }

            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("#pragma warning disable", StringComparison.Ordinal))
                {
                    var prev = i > 0 ? lines[i - 1].Trim() : string.Empty;
                    if (!prev.StartsWith("// Justification:", StringComparison.Ordinal))
                    {
                        findings.Add(string.Create(
                            CultureInfo.InvariantCulture,
                            $"{path}:{i + 1}: #pragma warning disable without preceding `// Justification:`"));
                    }
                }

                if (line.Contains("[SuppressMessage", StringComparison.Ordinal)
                    && !SuppressMessageWithJustification().IsMatch(line))
                {
                    findings.Add(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{path}:{i + 1}: [SuppressMessage] without non-empty Justification"));
                }
            }
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }

    [GeneratedRegex(@"\[SuppressMessage\([^)]*Justification\s*=\s*""[^""]+""", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SuppressMessageWithJustification();

    private static bool IsBuildArtifact(string root, string path)
    {
        var rel = Path.GetRelativePath(root, path);
        foreach (var segment in rel.Split(PathSeparators))
        {
            if (string.Equals(segment, "bin", StringComparison.Ordinal)
                || string.Equals(segment, "obj", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
