namespace Quality.Cli.Config;

internal sealed class Phase5
{
    public MaxLinesEntry MaxLines { get; init; } = new();

    public CheckEntry SemgrepLogging { get; init; } = new();
}
