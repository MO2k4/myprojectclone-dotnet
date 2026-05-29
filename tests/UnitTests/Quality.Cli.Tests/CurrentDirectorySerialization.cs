namespace Quality.Cli.Tests;

using System.Diagnostics.CodeAnalysis;
using Xunit;

// Serializes tests that mutate process-global state via
// Directory.SetCurrentDirectory. xUnit silently no-ops [Collection("X")]
// when no matching [CollectionDefinition("X")] exists, which is why this
// class is required even though it carries no shared fixture.
[CollectionDefinition("CurrentDirectory", DisableParallelization = true)]
[SuppressMessage(
    "Design",
    "CA1515:Consider making public types internal",
    Justification = "xUnit1027 mandates that collection-definition classes are public so xUnit can discover them via reflection. Direct conflict with CA1515 — xUnit's rule wins.")]
public sealed class CurrentDirectorySerialization
{
}
