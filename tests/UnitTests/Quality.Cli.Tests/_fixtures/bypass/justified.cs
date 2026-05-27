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
}
