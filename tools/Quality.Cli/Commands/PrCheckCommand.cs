namespace Quality.Cli.Commands;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

[ExcludeFromCodeCoverage(Justification = "Sequences out-of-process steps (fmt/check/build/test); covered by the Phase G pre-commit smoke test, not unit-asserted here.")]
internal static class PrCheckCommand
{
    public static int Run()
    {
        var steps = new (string Name, Func<int> Fn)[]
        {
            ("fmt", FmtCommand.Run),
            ("check", () => CheckCommand.Run("all", ".quality.toml")),
            ("build", () => Shell("dotnet build --no-restore -warnaserror")),
            ("test", () => Shell("dotnet test --no-build")),
        };

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

    private static int Shell(string cmdline)
    {
        var parts = cmdline.Split(' ', 2);
        var psi = new ProcessStartInfo(parts[0], parts.Length > 1 ? parts[1] : string.Empty);
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }
}
