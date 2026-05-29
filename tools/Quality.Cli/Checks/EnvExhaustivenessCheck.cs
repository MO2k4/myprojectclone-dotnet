namespace Quality.Cli.Checks;

using System.Globalization;
using System.Text.Json;

internal sealed class EnvExhaustivenessCheck : ICheck
{
    public string Id => "env-exhaustiveness";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var envExample = Path.Combine(ctx.RepoRoot, ".env.example");
        if (!File.Exists(envExample))
        {
            return new CheckResult(this.Id, true, Array.Empty<string>());
        }

        // appsettings.json is conventionally per-project (src/<Proj>/appsettings.json),
        // not repo-root. Walk every appsettings.json the file filter exposes and union
        // their flattened keys. RepoFileFilter excludes bin/obj/_fixtures so a target
        // repo's build output is not scanned.
        var settingsFiles = RepoFileFilter
            .EnumerateFiles(ctx.RepoRoot, "appsettings.json")
            .ToList();
        if (settingsFiles.Count == 0)
        {
            return new CheckResult(this.Id, true, Array.Empty<string>());
        }

        var settingsKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var settingsPath in settingsFiles)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(settingsPath));
            foreach (var key in FlattenJson(doc.RootElement, string.Empty))
            {
                if (key.Length == 0)
                {
                    // Non-object root (array, string, number) yields an empty prefix —
                    // skip rather than emit a spurious finding for an empty key.
                    continue;
                }

                settingsKeys.Add(key.Replace(":", "__", StringComparison.Ordinal).ToUpperInvariant());
            }
        }

        var envKeys = File.ReadAllLines(envExample)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
            .Select(l => l.Split('=', 2)[0].Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var missing = settingsKeys
            .Except(envKeys, StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .Select(k => string.Create(CultureInfo.InvariantCulture, $".env.example missing key for `{k}`"))
            .ToList();

        return new CheckResult(this.Id, missing.Count == 0, missing);
    }

    private static IEnumerable<string> FlattenJson(JsonElement el, string prefix)
    {
        if (el.ValueKind != JsonValueKind.Object)
        {
            yield return prefix;
            yield break;
        }

        using var enumerator = el.EnumerateObject();
        while (enumerator.MoveNext())
        {
            var p = enumerator.Current;
            var childPrefix = prefix.Length == 0 ? p.Name : $"{prefix}:{p.Name}";
            foreach (var k in FlattenJson(p.Value, childPrefix))
            {
                yield return k;
            }
        }
    }
}
