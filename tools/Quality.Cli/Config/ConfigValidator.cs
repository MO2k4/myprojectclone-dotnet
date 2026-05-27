namespace Quality.Cli.Config;

internal static class ConfigValidator
{
    public static IEnumerable<string> Validate(QualityConfig cfg)
    {
        foreach (var (id, entry) in AllEntries(cfg))
        {
            if (!entry.Enabled && string.IsNullOrWhiteSpace(entry.Reason))
            {
                yield return $"check '{id}' is disabled but has no `reason`. " +
                             "Add a non-empty reason in .quality.toml.";
            }
        }
    }

    public static IEnumerable<(string Id, CheckEntry Entry)> AllEntries(QualityConfig c) =>
    [
        ("phase4.trivy_fs", c.Phase4.TrivyFs),
        ("phase4.license_check", c.Phase4.LicenseCheck),
        ("phase5.max_lines", c.Phase5.MaxLines),
        ("phase5.semgrep_logging", c.Phase5.SemgrepLogging),
        ("phase9.coverage", c.Phase9.Coverage),
    ];
}
