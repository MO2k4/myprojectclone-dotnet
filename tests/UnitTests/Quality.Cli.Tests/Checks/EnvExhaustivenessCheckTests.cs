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

    [Theory]
    [InlineData("[]")] // array root
    [InlineData("\"foo\"")] // string root
    [InlineData("42")] // number root
    public void Does_not_emit_empty_key_finding_for_non_object_appsettings_root(string appsettingsJson)
    {
        // A non-object root flattens to an empty prefix; the check must skip it
        // rather than report `.env.example missing key for `` `` (finding #9).
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tmp, ".env.example"), "SOME_KEY=value\n");
            File.WriteAllText(Path.Combine(tmp, "appsettings.json"), appsettingsJson);

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
