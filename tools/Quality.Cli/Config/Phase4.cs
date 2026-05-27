namespace Quality.Cli.Config;

internal sealed class Phase4
{
    public CheckEntry TrivyFs { get; init; } = new();

    public LicenseCheckEntry LicenseCheck { get; init; } = new();
}
