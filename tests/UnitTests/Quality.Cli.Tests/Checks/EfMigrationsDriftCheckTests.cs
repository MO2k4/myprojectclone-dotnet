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
}
