namespace Quality.Cli.Config;

internal sealed class QualityConfig
{
    public Dictionary<string, CheckEntry> Checks { get; init; } = new(StringComparer.Ordinal);
}
