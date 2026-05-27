namespace Quality.Cli.Config;

internal class CheckEntry
{
    public bool Enabled { get; init; } = true;

    public string Reason { get; init; } = string.Empty;

    public string? Severity { get; init; }
}
