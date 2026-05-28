namespace Quality.Cli.Tests.Checks;

using Quality.Cli.Checks;
using Quality.Cli.Config;
using Xunit;

public class EfMigrationsDriftCheckTests
{
    [Fact]
    public void Skips_when_no_project_references_ef_design()
    {
        var ctx = new CheckContext(
            Path.Combine(AppContext.BaseDirectory, "_fixtures", "ef-drift", "no-ef"),
            new QualityConfig());

        var result = new EfMigrationsDriftCheck().Run(ctx);

        Assert.True(result.Ok);
        Assert.Empty(result.Findings);
    }

    [Theory]
    [InlineData("Could not execute because the specified command or file was not found.")]
    [InlineData("System.Exception: Could not execute the design-time tool.")]
    [InlineData("Build FAILED.")]
    [InlineData("error MSB1009: Build failed.")]
    public void LooksLikeToolFailure_matches_known_tool_error_signatures(string stderr)
    {
        Assert.True(EfMigrationsDriftCheck.LooksLikeToolFailure(stderr));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Migrations are pending.")]
    [InlineData("Some other diagnostic that doesn't match.")]
    public void LooksLikeToolFailure_does_not_match_real_drift_or_unrelated_output(string stderr)
    {
        Assert.False(EfMigrationsDriftCheck.LooksLikeToolFailure(stderr));
    }
}
