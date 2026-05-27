namespace Quality.Cli.Commands;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage(Justification = "Thin Process wrapper around `dotnet format`; covered by the Phase G pre-commit smoke test, not unit-asserted here.")]
internal static class FmtCommand
{
    public static int Run()
    {
        var psi = new ProcessStartInfo("dotnet", "format --verify-no-changes --severity error");
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }
}
