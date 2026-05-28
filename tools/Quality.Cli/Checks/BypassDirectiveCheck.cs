namespace Quality.Cli.Checks;

using System.Globalization;
using System.Text.RegularExpressions;

internal sealed partial class BypassDirectiveCheck : ICheck
{
    public string Id => "bypass-directive-check";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var findings = new List<string>();
        foreach (var path in RepoFileFilter.EnumerateFiles(ctx.RepoRoot, "*.cs"))
        {
            ScanFile(path, findings);
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }

    private static void ScanFile(string path, List<string> findings)
    {
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

                // After joining, search for any non-empty Justification clause.
                // The earlier line-leading `[SuppressMessage` gate already filters
                // out string-literal occurrences, so a permissive scan of the
                // joined attribute is safe and avoids the previous regex's
                // pitfall: `[^)]*` truncated at any `)` inside a string argument
                // (e.g. `"IDE0011:Add braces (multi-line)"`), false-flagging
                // legitimately justified attributes.
                if (!JustificationClause().IsMatch(joined))
                {
                    findings.Add(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{path}:{i + 1}: [SuppressMessage] without non-empty Justification"));
                }
            }
        }
    }

    [GeneratedRegex(@"Justification\s*=\s*""[^""]+""", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex JustificationClause();

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
}
