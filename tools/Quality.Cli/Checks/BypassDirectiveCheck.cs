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
                // Match the directive/attribute at line start (after whitespace) only:
                // the C# tokens `#pragma warning disable` and `[SuppressMessage` are
                // line-leading constructs; finding them mid-line means we're looking
                // at a string literal or comment, not the actual language feature.
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("#pragma warning disable", StringComparison.Ordinal))
                {
                    var prev = i > 0 ? lines[i - 1].Trim() : string.Empty;
                    if (!prev.StartsWith("// Justification:", StringComparison.Ordinal))
                    {
                        findings.Add(string.Create(
                            CultureInfo.InvariantCulture,
                            $"{path}:{i + 1}: #pragma warning disable without preceding `// Justification:`"));
                    }
                }

                if (trimmed.StartsWith("[SuppressMessage", StringComparison.Ordinal))
                {
                    // Accumulate the attribute across lines until parens balance —
                    // multi-line `[SuppressMessage(... \n Justification = "…")]` forms
                    // are idiomatic and must be evaluated as one logical unit.
                    var joined = JoinAttributeLines(lines, i);
                    if (!SuppressMessageWithJustification().IsMatch(joined))
                    {
                        findings.Add(string.Create(
                            CultureInfo.InvariantCulture,
                            $"{path}:{i + 1}: [SuppressMessage] without non-empty Justification"));
                    }
                }
            }
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }

    [GeneratedRegex(@"\[SuppressMessage\([^)]*Justification\s*=\s*""[^""]+""", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SuppressMessageWithJustification();

    private static string JoinAttributeLines(string[] lines, int start)
    {
        var sb = new System.Text.StringBuilder();
        int depth = 0;
        bool seenOpen = false;
        for (int j = start; j < lines.Length; j++)
        {
            if (j > start)
            {
                sb.Append(' ');
            }

            var line = lines[j];
            sb.Append(line);
            foreach (var ch in line)
            {
                if (ch == '(')
                {
                    depth++;
                    seenOpen = true;
                }
                else if (ch == ')')
                {
                    depth--;
                }
            }

            if (seenOpen && depth <= 0)
            {
                break;
            }
        }

        return sb.ToString();
    }

    private static bool IsBuildArtifact(string root, string path)
    {
        var rel = Path.GetRelativePath(root, path);
        foreach (var segment in rel.Split(PathSeparators))
        {
            if (string.Equals(segment, "bin", StringComparison.Ordinal)
                || string.Equals(segment, "obj", StringComparison.Ordinal)
                || string.Equals(segment, "_fixtures", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
