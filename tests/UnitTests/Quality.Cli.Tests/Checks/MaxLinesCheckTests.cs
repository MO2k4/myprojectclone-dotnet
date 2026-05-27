namespace Quality.Cli.Tests.Checks;

using Quality.Cli.Checks;
using Quality.Cli.Config;
using Xunit;

public class MaxLinesCheckTests
{
    [Fact]
    public void Flags_files_over_threshold()
    {
        var result = new MaxLinesCheck().Run(Ctx());

        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("too_long.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Findings, f => f.Contains("ok.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Passes_when_no_file_exceeds_threshold()
    {
        var cfg = new QualityConfig
        {
            Phase5 = new Phase5 { MaxLines = new MaxLinesEntry { Threshold = 10_000 } },
        };
        var ctx = new CheckContext(FixDir(), cfg);

        var result = new MaxLinesCheck().Run(ctx);

        Assert.True(result.Ok);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Skips_files_under_bin_or_obj_directories()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var binDir = Path.Combine(tmp, "bin", "Debug");
            var objDir = Path.Combine(tmp, "obj");
            Directory.CreateDirectory(binDir);
            Directory.CreateDirectory(objDir);

            File.WriteAllLines(Path.Combine(binDir, "huge.cs"), Enumerable.Repeat("//", 1000));
            File.WriteAllLines(Path.Combine(objDir, "huge.cs"), Enumerable.Repeat("//", 1000));

            var ctx = new CheckContext(tmp, new QualityConfig
            {
                Phase5 = new Phase5 { MaxLines = new MaxLinesEntry { Threshold = 10 } },
            });

            var result = new MaxLinesCheck().Run(ctx);

            Assert.True(result.Ok);
            Assert.Empty(result.Findings);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    private static string FixDir() =>
        Path.Combine(AppContext.BaseDirectory, "_fixtures", "max-lines");

    private static CheckContext Ctx() => new(FixDir(), new QualityConfig
    {
        Phase5 = new Phase5 { MaxLines = new MaxLinesEntry { Threshold = 400 } },
    });
}
