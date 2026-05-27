namespace Quality.Cli.Tests.Config;

using Quality.Cli.Config;
using Xunit;

public class ConfigReaderTests
{
    [Fact]
    public void Reads_enabled_checks_with_defaults()
    {
        var cfg = ConfigReader.Read(Fixture("full.toml"));
        Assert.True(cfg.Phase5.MaxLines.Enabled);
        Assert.Equal(400, cfg.Phase5.MaxLines.Threshold);
        Assert.Equal(90, cfg.Phase9.Coverage.Line);
    }

    [Fact]
    public void Disabled_without_reason_round_trips_with_empty_reason()
    {
        var cfg = ConfigReader.Read(Fixture("disabled-without-reason.toml"));
        Assert.False(cfg.Phase4.TrivyFs.Enabled);
        Assert.Equal(string.Empty, cfg.Phase4.TrivyFs.Reason);
    }

    private static string Fixture(string name)
        => Path.Combine(AppContext.BaseDirectory, "_fixtures", "quality", name);
}
