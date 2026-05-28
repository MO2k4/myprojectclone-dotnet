namespace Quality.Cli.Commands;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Quality.Cli.Process;

[ExcludeFromCodeCoverage(Justification = "Sequences out-of-process steps (fmt/check/build/test); covered by the Phase G pre-commit smoke test, not unit-asserted here.")]
internal static class PrCheckCommand
{
    private static readonly TimeSpan StepTimeout = TimeSpan.FromMinutes(15);

    public static int Run()
    {
        var steps = new (string Name, Func<int> Fn)[]
        {
            ("fmt", FmtCommand.Run),
            ("check", () => CheckCommand.Run("all", ".quality.toml")),
            ("build", () => Run("dotnet", "build --no-restore -warnaserror")),
            ("test", () => Run("dotnet", "test --no-build")),
        };

        // Short-circuit on first failure is intentional: pr-check is meant to
        // mirror the developer's local commit gate, where fixing fmt before
        // chasing test failures gives a cleaner feedback loop.
        foreach (var (name, fn) in steps)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"── {name} ──"));
            var rc = fn();
            if (rc != 0)
            {
                return rc;
            }
        }

        return 0;
    }

    private static int Run(string exe, string args)
    {
        var (exitCode, _, _, timedOut) = ProcessRunner.Run(
            exe, args, workingDir: null, StepTimeout, captureOutput: false);
        return timedOut ? -1 : exitCode;
    }
}
