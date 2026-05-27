namespace Quality.Cli.Config;

internal sealed class QualityConfig
{
    public Phase4 Phase4 { get; init; } = new();

    public Phase5 Phase5 { get; init; } = new();

    public Phase9 Phase9 { get; init; } = new();
}
