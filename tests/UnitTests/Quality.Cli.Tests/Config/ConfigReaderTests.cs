namespace Quality.Cli.Tests.Config;

using Quality.Cli.Config;
using Xunit;

public class ConfigReaderTests
{
    [Fact]
    public void Reads_enabled_checks_with_defaults()
    {
        var cfg = ConfigReader.Read(Fixture("full.toml"));
        Assert.True(cfg.Checks["max-lines"].Enabled);
        Assert.Equal(400, cfg.Checks["max-lines"].Threshold);
        Assert.Contains("GPL-3.0", cfg.Checks["license-check"].Denylist!);
    }

    [Fact]
    public void Disabled_without_reason_round_trips_with_empty_reason()
    {
        var cfg = ConfigReader.Read(Fixture("disabled-without-reason.toml"));
        Assert.False(cfg.Checks["max-lines"].Enabled);
        Assert.Equal(string.Empty, cfg.Checks["max-lines"].Reason);
    }

    [Fact]
    public void Missing_config_file_throws_friendly_exception()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".toml");
        var ex = Assert.Throws<ConfigReadException>(() => ConfigReader.Read(path));
        Assert.Contains("not found", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Malformed_toml_throws_friendly_exception()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "this is = not valid toml [[[");
            var ex = Assert.Throws<ConfigReadException>(() => ConfigReader.Read(path));
            Assert.Contains("could not parse", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string Fixture(string name)
        => Path.Combine(AppContext.BaseDirectory, "_fixtures", "quality", name);
}
