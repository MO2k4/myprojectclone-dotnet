namespace Quality.Cli.Config;

internal sealed class CoverageEntry : CheckEntry
{
    public int Line { get; init; } = 90;

    public int Branch { get; init; } = 90;
}
