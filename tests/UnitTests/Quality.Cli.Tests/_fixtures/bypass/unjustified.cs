namespace Fixtures;

public class Unjustified
{
#pragma warning disable CA1822
    public void Method(int unused) { }
#pragma warning restore CA1822

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA9999")]
    public void NoJustification() { }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "")]
    public void MultiLineEmptyJustification() { }
}
