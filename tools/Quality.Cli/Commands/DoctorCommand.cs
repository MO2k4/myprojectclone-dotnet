namespace Quality.Cli.Commands;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Quality.Cli.Process;

[ExcludeFromCodeCoverage(Justification = "Probes external toolchain via Process; exercised manually via `quality doctor`, not unit-asserted here.")]
internal static class DoctorCommand
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);

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
            // captureOutput: false inherits parent stdio — no pipe-buffer deadlock,
            // and the user sees the version banner directly under the ✓/✗ summary.
            var (exitCode, _, _, timedOut) = ProcessRunner.Run(
                exe, args, workingDir: null, ProbeTimeout, captureOutput: false);

            if (timedOut)
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"✗ {exe} {args} timed out"));
                return false;
            }

            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{(exitCode == 0 ? '✓' : '✗')} {exe} {args}"));
            return exitCode == 0;
        }
        catch (Win32Exception)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"✗ {exe} not found"));
            return false;
        }
    }
}
