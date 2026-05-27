namespace Quality.Cli.Tests.Checks;

using Quality.Cli.Checks;
using Quality.Cli.Config;
using Xunit;

public class UnusedNuGetPackagesCheckTests
{
    [Fact]
    public void Flags_PackageReference_with_no_using_in_any_file()
    {
        var ctx = new CheckContext(
            Path.Combine(AppContext.BaseDirectory, "_fixtures", "unused-nuget"),
            new QualityConfig());

        var result = new UnusedNuGetPackagesCheck().Run(ctx);

        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("Serilog", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Findings, f => f.Contains("Newtonsoft.Json", StringComparison.Ordinal));
    }
}
