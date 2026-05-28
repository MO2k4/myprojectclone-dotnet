namespace Fixtures;

public class Justified
{
    // Justification: external API requires unused parameter.
#pragma warning disable CA1822
    public void Method(int unused) { }
#pragma warning restore CA1822

    // Justification: documented in ticket #42.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA9999", Justification = "documented in ticket #42")]
    public void Other() { }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated via reflection by the framework.")]
    public void MultiLineJustified() { }
}
