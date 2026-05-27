namespace Quality.Cli.Config;

internal sealed class LicenseCheckEntry : CheckEntry
{
    public List<string> Denylist { get; init; } = [];
}
