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
            Phase4 = new Phase4 { TrivyFs = new CheckEntry { Enabled = false, Reason = string.Empty } },
        };
        var errors = ConfigValidator.Validate(cfg).ToList();
        Assert.Single(errors);
        Assert.Contains("trivy_fs", errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Disabled_check_with_reason_passes()
    {
        var cfg = new QualityConfig
        {
            Phase4 = new Phase4 { TrivyFs = new CheckEntry { Enabled = false, Reason = "tracked in #123" } },
        };
        Assert.Empty(ConfigValidator.Validate(cfg));
    }
}
