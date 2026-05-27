namespace Quality.Cli;

using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Locator;

[ExcludeFromCodeCoverage(Justification = "Entry-point wiring; behavior is covered by per-command and per-check unit tests.")]
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        var root = new RootCommand("dotnet quality — strict-by-default quality framework for .NET");
        root.SetHandler(() => Console.WriteLine("quality 0.1.0"));

        var statusCmd = new Command("status", "Print the table of every check + enabled/reason");
        statusCmd.SetHandler(() => Commands.StatusCommand.Run(".quality.toml"));
        root.AddCommand(statusCmd);

        var fmt = new Command("fmt", "dotnet format --verify-no-changes");
        fmt.SetHandler(() => Environment.Exit(Commands.FmtCommand.Run()));
        root.AddCommand(fmt);

        var check = new Command("check", "Run one named check or 'all'");
        var idArg = new Argument<string>("id");
        check.AddArgument(idArg);
        check.SetHandler(
            (string id) => Environment.Exit(Commands.CheckCommand.Run(id, ".quality.toml")),
            idArg);
        root.AddCommand(check);

        var pr = new Command("pr-check", "Run every phase (fmt, check, build, test)");
        pr.SetHandler(() => Environment.Exit(Commands.PrCheckCommand.Run()));
        root.AddCommand(pr);

        var doctor = new Command("doctor", "Diagnose toolchain");
        doctor.SetHandler(() => Environment.Exit(Commands.DoctorCommand.Run()));
        root.AddCommand(doctor);

        return await root.InvokeAsync(args).ConfigureAwait(false);
    }
}
