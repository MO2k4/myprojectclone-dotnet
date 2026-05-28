namespace Quality.Cli.Tests.Checks;

using Quality.Cli.Checks;
using Quality.Cli.Config;
using Xunit;

public class BypassDirectiveCheckTests
{
    [Fact]
    public void Flags_unjustified_pragma_and_passes_justified_one()
    {
        var ctx = new CheckContext(
            Path.Combine(AppContext.BaseDirectory, "_fixtures", "bypass"),
            new QualityConfig());

        var result = new BypassDirectiveCheck().Run(ctx);

        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("unjustified.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Findings, f =>
        {
            var name = Path.GetFileName(f.Split(':')[0]);
            return string.Equals(name, "justified.cs", StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Passes_when_no_files_have_directives()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tmp, "clean.cs"), "namespace X; public class Y {}\n");

            var result = new BypassDirectiveCheck().Run(new CheckContext(tmp, new QualityConfig()));

            Assert.True(result.Ok);
            Assert.Empty(result.Findings);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Handles_multiline_SuppressMessage_attribute_forms()
    {
        var ctx = new CheckContext(
            Path.Combine(AppContext.BaseDirectory, "_fixtures", "bypass-multiline"),
            new QualityConfig());

        var result = new BypassDirectiveCheck().Run(ctx);

        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("bad.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Findings, f => f.Contains("good.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Skips_files_under_bin_or_obj_directories()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var objDir = Path.Combine(tmp, "obj");
            Directory.CreateDirectory(objDir);
            File.WriteAllText(Path.Combine(objDir, "gen.cs"), "#pragma warning disable CA1822\n");

            var result = new BypassDirectiveCheck().Run(new CheckContext(tmp, new QualityConfig()));

            Assert.True(result.Ok);
            Assert.Empty(result.Findings);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }
}
