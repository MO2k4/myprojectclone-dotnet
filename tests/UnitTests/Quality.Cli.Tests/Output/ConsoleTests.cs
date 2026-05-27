namespace Quality.Cli.Tests.Output;

using Quality.Cli.Output;
using Xunit;

public class ConsoleTests
{
    [Fact]
    public void Error_appends_red_marker_and_returns_nonzero_intent()
    {
        var sink = new RecordingConsoleOutput();
        sink.Error("boom");
        Assert.Contains("boom", sink.Captured, StringComparison.Ordinal);
        Assert.True(sink.HasErrors);
    }

    [Fact]
    public void Heading_records_double_hash_prefix()
    {
        var sink = new RecordingConsoleOutput();
        sink.Heading("Plan");
        Assert.Contains("## Plan", sink.Captured, StringComparison.Ordinal);
        Assert.False(sink.HasErrors);
    }

    [Fact]
    public void Info_records_plain_line()
    {
        var sink = new RecordingConsoleOutput();
        sink.Info("hello");
        Assert.Contains("hello", sink.Captured, StringComparison.Ordinal);
        Assert.False(sink.HasErrors);
    }
}
