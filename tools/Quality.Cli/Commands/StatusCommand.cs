namespace Quality.Cli.Commands;

using System.Diagnostics.CodeAnalysis;
using Quality.Cli.Checks;
using Quality.Cli.Config;
using Spectre.Console;

[ExcludeFromCodeCoverage(Justification = "Thin Spectre delegation; output shape is exercised by the status smoke-test (Phase C task 11 step 9), not unit-asserted.")]
internal static class StatusCommand
{
    public static int Run(string configPath)
    {
        QualityConfig cfg;
        try
        {
            cfg = ConfigReader.Read(configPath);
        }
        catch (ConfigReadException ex)
        {
            AnsiConsole.MarkupLine($"[red]quality: {Markup.Escape(ex.Message)}[/]");
            return 2;
        }

        var table = new Table().AddColumns("Check", "Enabled", "Reason");
        foreach (var id in CheckRegistry.All.Select(c => c.Id))
        {
            var enabled = !cfg.Checks.TryGetValue(id, out var entry) || entry.Enabled;
            var reason = entry?.Reason ?? string.Empty;
            table.AddRow(id, enabled ? "yes" : "no", reason);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
