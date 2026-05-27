namespace Quality.Cli.Config;

internal sealed class MaxLinesEntry : CheckEntry
{
    public int Threshold { get; init; } = 400;
}
