namespace Quality.Cli.Checks;

using Quality.Cli.Config;

internal sealed record CheckContext(string RepoRoot, QualityConfig Config);
