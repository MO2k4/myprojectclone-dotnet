namespace Quality.Cli.Commands;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

[ExcludeFromCodeCoverage(Justification = "Probes external toolchain via Process; exercised manually via `quality doctor`, not unit-asserted here.")]
internal static class DoctorCommand
{
    public static int Run()
    {
        var ok = true;
        ok &= Probe("dotnet", "--version");
        ok &= Probe("dotnet", "tool list --local");
        ok &= Probe("pre-commit", "--version");
        ok &= Probe("docker", "--version");
        return ok ? 0 : 1;
    }

    private static bool Probe(string exe, string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });
            if (p is null)
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"✗ {exe} not found"));
                return false;
            }

            p.WaitForExit();
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"✓ {exe} {args}"));
            return p.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"✗ {exe} not found"));
            return false;
        }
    }
}
