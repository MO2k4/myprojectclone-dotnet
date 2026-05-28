namespace Quality.Cli.Tests.Config;

using Quality.Cli.Config;
using Xunit;

public class ConfigValidatorTests
{
    [Fact]
    public void Disabled_check_without_reason_is_an_error()
    {
        var cfg = new QualityConfig
        {
            Checks = new(StringComparer.Ordinal)
            {
                ["max-lines"] = new CheckEntry { Enabled = false, Reason = string.Empty },
            },
        };
        var errors = ConfigValidator.Validate(cfg).ToList();
        Assert.Single(errors);
        Assert.Contains("max-lines", errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Disabled_check_with_reason_passes()
    {
        var cfg = new QualityConfig
        {
            Checks = new(StringComparer.Ordinal)
            {
                ["max-lines"] = new CheckEntry { Enabled = false, Reason = "tracked in #123" },
            },
        };
        Assert.Empty(ConfigValidator.Validate(cfg));
    }

    [Fact]
    public void Unknown_check_id_is_an_error()
    {
        var cfg = new QualityConfig
        {
            Checks = new(StringComparer.Ordinal)
            {
                ["nonsense-check"] = new CheckEntry { Enabled = true },
            },
        };
        var errors = ConfigValidator.Validate(cfg).ToList();
        Assert.Single(errors);
        Assert.Contains("unknown check 'nonsense-check'", errors[0], StringComparison.Ordinal);
    }
}
