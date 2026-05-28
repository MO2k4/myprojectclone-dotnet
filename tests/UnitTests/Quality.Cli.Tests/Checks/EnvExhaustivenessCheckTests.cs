namespace Quality.Cli.Tests.Checks;

using Quality.Cli.Checks;
using Quality.Cli.Config;
using Xunit;

public class EnvExhaustivenessCheckTests
{
    [Fact]
    public void Flags_keys_missing_from_dotenv_example()
    {
        var ctx = new CheckContext(
            Path.Combine(AppContext.BaseDirectory, "_fixtures", "env"),
            new QualityConfig());

        var result = new EnvExhaustivenessCheck().Run(ctx);

        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("AUTH__ISSUER", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Findings, f => f.Contains("DATABASE__CONNECTIONSTRING", StringComparison.Ordinal));
    }

    [Fact]
    public void Passes_when_appsettings_or_envexample_missing()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var result = new EnvExhaustivenessCheck().Run(new CheckContext(tmp, new QualityConfig()));
            Assert.True(result.Ok);
            Assert.Empty(result.Findings);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Flags_keys_from_appsettings_files_under_project_subdirectories()
    {
        var ctx = new CheckContext(
            Path.Combine(AppContext.BaseDirectory, "_fixtures", "env-multi"),
            new QualityConfig());

        var result = new EnvExhaustivenessCheck().Run(ctx);

        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("API__TIMEOUT", StringComparison.Ordinal));
        Assert.Contains(result.Findings, f => f.Contains("WORKER__QUEUENAME", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Findings, f => f.Contains("API__BASEURL", StringComparison.Ordinal));
    }
}
