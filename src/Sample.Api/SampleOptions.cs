namespace Sample.Api;

using System.Diagnostics.CodeAnalysis;

/// <summary>Binds the <c>Sample</c> configuration section.</summary>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by Microsoft.Extensions.Options via reflection.")]
internal sealed class SampleOptions
{
    /// <summary>Gets the greeting prefix returned by the sample endpoint.</summary>
    public string Greeting { get; init; } = string.Empty;
}
