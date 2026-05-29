namespace Quality.Cli.Commands;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Quality.Cli.Process;

internal static class DoctorCommand
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);

    [ExcludeFromCodeCoverage(Justification = "Probes external toolchain via Process; exercised manually via `quality doctor`. The exit-code policy is unit-asserted via ExitCode.")]
    public static int Run()
    {
        // dotnet, its local tools, and pre-commit back the native pr-check path and are
        // required. docker only backs the docker-quality CI job, so a local-only dev
        // without it should still get a green doctor — probe it but treat it as advisory.
        var outcomes = new[]
        {
            Probe("dotnet", "--version"),
            Probe("dotnet", "tool list --local"),
            Probe("pre-commit", "--version"),
            Probe("docker", "--version", required: false, optionalNote: " (optional — only the docker CI path needs it)"),
        };
        return ExitCode(outcomes);
    }

    // Pure exit-code policy: green iff every *required* probe succeeded. Optional probes
    // (docker) surface a ⚠ warning but never flip the exit code. Extracted so the policy
    // is unit-asserted without shelling out (the Run/Probe paths stay ExcludeFromCodeCoverage).
    internal static int ExitCode(IReadOnlyList<ProbeOutcome> outcomes)
        => outcomes.Any(o => o.Required && !o.Ok) ? 1 : 0;

    [ExcludeFromCodeCoverage(Justification = "Shells out via ProcessRunner; the testable decision lives in ExitCode.")]
    private static ProbeOutcome Probe(string exe, string args, bool required = true, string optionalNote = "")
    {
        var fail = required ? '✗' : '⚠';
        try
        {
            // captureOutput: false inherits parent stdio — no pipe-buffer deadlock, and the
            // user sees the version banner directly under the ✓/⚠/✗ summary line.
            var (exitCode, _, _, timedOut) = ProcessRunner.Run(
                exe, args, workingDir: null, ProbeTimeout, captureOutput: false);

            if (timedOut)
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{fail} {exe} {args} timed out{optionalNote}"));
                return new ProbeOutcome(required, false);
            }

            var ok = exitCode == 0;
            Console.WriteLine(ok
                ? string.Create(CultureInfo.InvariantCulture, $"✓ {exe} {args}")
                : string.Create(CultureInfo.InvariantCulture, $"{fail} {exe} {args}{optionalNote}"));
            return new ProbeOutcome(required, ok);
        }
        catch (Win32Exception)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{fail} {exe} not found{optionalNote}"));
            return new ProbeOutcome(required, false);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Trivial DTO; generated record members are not exercised by the ExitCode policy tests.")]
    internal sealed record ProbeOutcome(bool Required, bool Ok);
}
