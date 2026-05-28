namespace Quality.Cli.Tests.Checks;

using Quality.Cli.Checks;
using Quality.Cli.Config;
using Xunit;

public class MaxLinesCheckTests
{
    [Fact]
    public void Flags_files_over_threshold()
    {
        var result = new MaxLinesCheck().Run(Ctx(threshold: 400));

        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("too_long.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Findings, f => f.Contains("ok.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Passes_when_no_file_exceeds_threshold()
    {
        var result = new MaxLinesCheck().Run(Ctx(threshold: 10_000));

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

            var cfg = new QualityConfig
            {
                Checks = new(StringComparer.Ordinal)
                {
                    ["max-lines"] = new CheckEntry { Threshold = 10 },
                },
            };
            var result = new MaxLinesCheck().Run(new CheckContext(tmp, cfg));

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

    private static CheckContext Ctx(int threshold) => new(FixDir(), new QualityConfig
    {
        Checks = new(StringComparer.Ordinal)
        {
            ["max-lines"] = new CheckEntry { Threshold = threshold },
        },
    });
}
