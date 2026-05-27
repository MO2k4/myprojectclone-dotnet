namespace Quality.Cli.Commands;

using System.Diagnostics.CodeAnalysis;
using Quality.Cli.Config;
using Spectre.Console;

[ExcludeFromCodeCoverage(Justification = "Thin Spectre delegation; output shape is exercised by the status smoke-test (Phase C task 11 step 9), not unit-asserted.")]
internal static class StatusCommand
{
    public static int Run(string configPath)
    {
        var cfg = ConfigReader.Read(configPath);
        var table = new Table().AddColumns("Check", "Enabled", "Reason");
        foreach (var (id, entry) in ConfigValidator.AllEntries(cfg))
        {
            table.AddRow(id, entry.Enabled ? "yes" : "no", entry.Reason);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
