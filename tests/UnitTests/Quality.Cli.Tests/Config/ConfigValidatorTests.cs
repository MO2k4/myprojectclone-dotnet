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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_threshold_is_an_error(int threshold)
    {
        // finding #14: max-lines compares `lineCount > threshold`, so a 0 or negative
        // threshold (e.g. a typo of -1 for 100) flags every file. ConfigValidator must
        // reject it rather than let the check silently invert its semantics.
        var cfg = new QualityConfig
        {
            Checks = new(StringComparer.Ordinal)
            {
                ["max-lines"] = new CheckEntry { Enabled = true, Threshold = threshold },
            },
        };
        var errors = ConfigValidator.Validate(cfg).ToList();
        Assert.Single(errors);
        Assert.Contains("threshold must be a positive integer", errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Positive_threshold_passes()
    {
        var cfg = new QualityConfig
        {
            Checks = new(StringComparer.Ordinal)
            {
                ["max-lines"] = new CheckEntry { Enabled = true, Threshold = 400 },
            },
        };
        Assert.Empty(ConfigValidator.Validate(cfg));
    }
}
