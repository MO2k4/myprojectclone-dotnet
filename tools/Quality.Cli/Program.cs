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

        return await root.InvokeAsync(args).ConfigureAwait(false);
    }
}
