namespace Quality.Cli.Tests.Commands;

using Quality.Cli.Commands;
using Quality.Cli.Output;
using Xunit;

[Collection("CurrentDirectory")]
public class CheckCommandTests
{
    [Fact]
    public void Reports_failure_when_a_check_fails()
    {
        WithConfig("[checks.\"max-lines\"]\nenabled = true\nthreshold = 1\n", cfg =>
        {
            WithCwd(Path.Combine(AppContext.BaseDirectory, "_fixtures", "max-lines"), () =>
            {
                var sink = new RecordingConsoleOutput();
                var code = CheckCommand.Run("max-lines", cfg, sink);
                Assert.NotEqual(0, code);
                Assert.True(sink.HasErrors);
            });
        });
    }

    [Fact]
    public void Returns_2_for_unknown_check_id()
    {
        WithConfig("[checks.\"max-lines\"]\nenabled = true\n", cfg =>
        {
            var sink = new RecordingConsoleOutput();
            var code = CheckCommand.Run("nonsense-id", cfg, sink);
            Assert.Equal(2, code);
            Assert.True(sink.HasErrors);
        });
    }

    [Fact]
    public void Returns_2_when_config_is_invalid()
    {
        WithConfig("[checks.\"max-lines\"]\nenabled = false\n", cfg =>
        {
            var sink = new RecordingConsoleOutput();
            var code = CheckCommand.Run("max-lines", cfg, sink);
            Assert.Equal(2, code);
            Assert.True(sink.HasErrors);
        });
    }

    [Fact]
    public void Returns_2_when_config_file_is_missing()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".toml");
        var sink = new RecordingConsoleOutput();
        var code = CheckCommand.Run("max-lines", missing, sink);
        Assert.Equal(2, code);
        Assert.True(sink.HasErrors);
    }

    [Fact]
    public void Throws_when_id_is_null()
    {
        WithConfig("[checks.\"max-lines\"]\nenabled = true\n", cfg =>
        {
            Assert.Throws<ArgumentNullException>(() => CheckCommand.Run(null!, cfg));
        });
    }

    [Fact]
    public void Runs_only_max_lines_when_id_is_all_with_threshold_high()
    {
        WithConfig("[checks.\"max-lines\"]\nenabled = true\nthreshold = 10000\n", cfg =>
        {
            var fixturesRoot = Directory.CreateTempSubdirectory().FullName;
            try
            {
                WithCwd(fixturesRoot, () =>
                {
                    var sink = new RecordingConsoleOutput();
                    var code = CheckCommand.Run("all", cfg, sink);
                    Assert.InRange(code, 0, 1);
                    Assert.Contains("max-lines", sink.Captured, StringComparison.Ordinal);
                });
            }
            finally
            {
                Directory.Delete(fixturesRoot, recursive: true);
            }
        });
    }

    [Fact]
    public void Returns_0_when_check_passes()
    {
        WithConfig("[checks.\"max-lines\"]\nenabled = true\nthreshold = 10000\n", cfg =>
        {
            WithCwd(Path.Combine(AppContext.BaseDirectory, "_fixtures", "max-lines"), () =>
            {
                var sink = new RecordingConsoleOutput();
                var code = CheckCommand.Run("max-lines", cfg, sink);
                Assert.Equal(0, code);
                Assert.False(sink.HasErrors);
                Assert.Contains("ok", sink.Captured, StringComparison.Ordinal);
            });
        });
    }

    [Fact]
    public void Skips_disabled_check_without_running_it()
    {
        WithConfig(
            "[checks.\"max-lines\"]\nenabled = false\nreason = \"tracked in #123\"\nthreshold = 1\n",
            cfg =>
            {
                WithCwd(Path.Combine(AppContext.BaseDirectory, "_fixtures", "max-lines"), () =>
                {
                    var sink = new RecordingConsoleOutput();
                    var code = CheckCommand.Run("max-lines", cfg, sink);
                    Assert.Equal(0, code);
                    Assert.False(sink.HasErrors);
                    Assert.Contains("skipped", sink.Captured, StringComparison.Ordinal);
                    Assert.Contains("tracked in #123", sink.Captured, StringComparison.Ordinal);
                });
            });
    }

    private static void WithConfig(string contents, Action<string> body)
    {
        var cfg = Path.GetTempFileName();
        try
        {
            File.WriteAllText(cfg, contents);
            body(cfg);
        }
        finally
        {
            File.Delete(cfg);
        }
    }

    private static void WithCwd(string path, Action body)
    {
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(path);
            body();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }
}
