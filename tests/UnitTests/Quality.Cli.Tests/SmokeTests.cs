namespace Quality.Cli.Tests;

using Xunit;

public class SmokeTests
{
    [Fact]
    public void Program_type_is_resolvable_from_test_project()
    {
        var t = typeof(Quality.Cli.Program);
        Assert.Equal("Quality.Cli", t.Namespace);
    }
}
