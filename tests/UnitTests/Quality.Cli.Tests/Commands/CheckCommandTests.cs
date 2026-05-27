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
        var cfg = Path.GetTempFileName();
        try
        {
            File.WriteAllText(cfg, "[phase5]\nmax_lines = { enabled = true, threshold = 1 }\n");
            var originalCwd = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(Path.Combine(AppContext.BaseDirectory, "_fixtures", "max-lines"));
                var sink = new RecordingConsoleOutput();
                var code = CheckCommand.Run("max-lines", cfg, sink);
                Assert.NotEqual(0, code);
                Assert.True(sink.HasErrors);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCwd);
            }
        }
        finally
        {
            File.Delete(cfg);
        }
    }

    [Fact]
    public void Returns_2_for_unknown_check_id()
    {
        var cfg = Path.GetTempFileName();
        try
        {
            File.WriteAllText(cfg, "[phase5]\nmax_lines = { enabled = true }\n");
            var sink = new RecordingConsoleOutput();
            var code = CheckCommand.Run("nonsense-id", cfg, sink);
            Assert.Equal(2, code);
            Assert.True(sink.HasErrors);
        }
        finally
        {
            File.Delete(cfg);
        }
    }

    [Fact]
    public void Returns_2_when_config_is_invalid()
    {
        var cfg = Path.GetTempFileName();
        try
        {
            File.WriteAllText(cfg, "[phase5]\nmax_lines = { enabled = false }\n");
            var sink = new RecordingConsoleOutput();
            var code = CheckCommand.Run("max-lines", cfg, sink);
            Assert.Equal(2, code);
            Assert.True(sink.HasErrors);
        }
        finally
        {
            File.Delete(cfg);
        }
    }

    [Fact]
    public void Throws_when_id_is_null()
    {
        var cfg = Path.GetTempFileName();
        try
        {
            File.WriteAllText(cfg, "[phase5]\nmax_lines = { enabled = true }\n");
            Assert.Throws<ArgumentNullException>(() => CheckCommand.Run(null!, cfg));
        }
        finally
        {
            File.Delete(cfg);
        }
    }

    [Fact]
    public void Runs_only_max_lines_when_id_is_all_with_threshold_high()
    {
        // Use id == "all" to exercise the all-branch, but with a benign config:
        // only max-lines is configured here. The other checks (env, unused-nuget,
        // ef-drift, lockfile, license) run against the fixtures dir and either no-op
        // (no appsettings/csproj) or fail; we only assert the "all" path is taken
        // and the call returns either 0 or 1 without throwing.
        var cfg = Path.GetTempFileName();
        try
        {
            File.WriteAllText(cfg, "[phase5]\nmax_lines = { enabled = true, threshold = 10000 }\n");
            var fixturesRoot = Directory.CreateTempSubdirectory().FullName;
            var originalCwd = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(fixturesRoot);
                var sink = new RecordingConsoleOutput();
                var code = CheckCommand.Run("all", cfg, sink);
                Assert.InRange(code, 0, 1);
                Assert.Contains("max-lines", sink.Captured, StringComparison.Ordinal);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCwd);
                Directory.Delete(fixturesRoot, recursive: true);
            }
        }
        finally
        {
            File.Delete(cfg);
        }
    }

    [Fact]
    public void Returns_0_when_check_passes()
    {
        var cfg = Path.GetTempFileName();
        try
        {
            File.WriteAllText(cfg, "[phase5]\nmax_lines = { enabled = true, threshold = 10000 }\n");
            var originalCwd = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(Path.Combine(AppContext.BaseDirectory, "_fixtures", "max-lines"));
                var sink = new RecordingConsoleOutput();
                var code = CheckCommand.Run("max-lines", cfg, sink);
                Assert.Equal(0, code);
                Assert.False(sink.HasErrors);
                Assert.Contains("ok", sink.Captured, StringComparison.Ordinal);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCwd);
            }
        }
        finally
        {
            File.Delete(cfg);
        }
    }
}
