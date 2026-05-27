namespace Quality.Cli.Checks;

internal sealed record CheckResult(string Id, bool Ok, IReadOnlyList<string> Findings);
