namespace Quality.Cli.Checks;

using System.Globalization;
using System.Text.Json;

internal sealed class EnvExhaustivenessCheck : ICheck
{
    public string Id => "env-exhaustiveness";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var settingsPath = Path.Combine(ctx.RepoRoot, "appsettings.json");
        var envExample = Path.Combine(ctx.RepoRoot, ".env.example");
        if (!File.Exists(settingsPath) || !File.Exists(envExample))
        {
            return new CheckResult(this.Id, true, Array.Empty<string>());
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(settingsPath));
        var settingsKeys = FlattenJson(doc.RootElement, string.Empty)
            .Select(k => k.Replace(":", "__", StringComparison.Ordinal).ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);

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
