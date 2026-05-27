namespace Quality.Cli.Tests.Checks;

using Quality.Cli.Checks;
using Xunit;

public class LicenseCheckTests
{
    [Fact]
    public void Flags_packages_with_denylisted_licenses()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "_fixtures", "licenses", "licenses.json");
        var denylist = new[] { "GPL-3.0", "AGPL-3.0" };

        var findings = LicenseCheck.Evaluate(File.ReadAllText(fixture), denylist);

        Assert.Contains(findings, f => f.Contains("Some.Copyleft", StringComparison.Ordinal)
            && f.Contains("GPL-3.0", StringComparison.Ordinal));
    }

    [Fact]
    public void Returns_empty_when_all_licenses_allowed()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "_fixtures", "licenses", "licenses.json");
        var denylist = new[] { "AGPL-3.0" };

        var findings = LicenseCheck.Evaluate(File.ReadAllText(fixture), denylist);

        Assert.Empty(findings);
    }

    [Fact]
    public void Denylist_matching_is_case_insensitive()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "_fixtures", "licenses", "licenses.json");
        var denylist = new[] { "gpl-3.0" };

        var findings = LicenseCheck.Evaluate(File.ReadAllText(fixture), denylist);

        Assert.Single(findings);
    }
}
