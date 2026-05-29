namespace Quality.Cli.Commands;

using System.Diagnostics.CodeAnalysis;
using Quality.Cli.Process;

[ExcludeFromCodeCoverage(Justification = "Thin Process wrapper around `dotnet format`; covered by the Phase G pre-commit smoke test, not unit-asserted here.")]
internal static class FmtCommand
{
    // dotnet format verify is light but not instant on a full solution; 10 min is
    // ample headroom while still bounding a hung MSBuild worker (see ProcessRunner).
    private static readonly TimeSpan FmtTimeout = TimeSpan.FromMinutes(10);

    public static int Run()
    {
        // Route through ProcessRunner like every other out-of-process call: it forces
        // UseShellExecute=false (so Process.Start never returns null — no more `!` and
        // the NRE it could mask), disables MSBuild node reuse, and bounds the run.
        // captureOutput: false inherits parent stdio so the format diff streams live.
        const string args = "format --verify-no-changes --severity error";
        var (exitCode, _, _, timedOut) = ProcessRunner.Run(
            "dotnet", args, workingDir: null, FmtTimeout, captureOutput: false);
        return timedOut ? -1 : exitCode;
    }
}
