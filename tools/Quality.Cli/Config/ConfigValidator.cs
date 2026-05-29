namespace Quality.Cli.Config;

using System.Globalization;
using Quality.Cli.Checks;

internal static class ConfigValidator
{
    public static IEnumerable<string> Validate(QualityConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        return ValidateIterator(cfg);
    }

    private static IEnumerable<string> ValidateIterator(QualityConfig cfg)
    {
        var knownIds = string.Join(
            ", ",
            CheckRegistry.Ids.OrderBy(x => x, StringComparer.Ordinal));

        foreach (var kv in cfg.Checks)
        {
            var id = kv.Key;
            var entry = kv.Value;

            if (!CheckRegistry.Ids.Contains(id))
            {
                yield return string.Create(CultureInfo.InvariantCulture, $"unknown check '{id}' in .quality.toml — valid ids: {knownIds}");
                continue;
            }

            if (!entry.Enabled && string.IsNullOrWhiteSpace(entry.Reason))
            {
                yield return string.Create(CultureInfo.InvariantCulture, $"check '{id}' is disabled but has no `reason`. Add a non-empty reason in .quality.toml.");
            }

            // A non-positive threshold is always a misconfiguration: checks compare with
            // `lineCount > threshold`, so `threshold = 0` flags every non-empty file and a
            // negative value (e.g. a typo of `-1` for `100`) flags every file outright. Catch
            // it here rather than letting the check silently invert its own semantics.
            if (entry.Threshold is <= 0)
            {
                yield return string.Create(CultureInfo.InvariantCulture, $"check '{id}' has threshold {entry.Threshold} in .quality.toml — threshold must be a positive integer.");
            }
        }
    }
}
