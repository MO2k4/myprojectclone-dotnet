namespace Quality.Cli.Output;

using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

[ExcludeFromCodeCoverage(Justification = "Thin delegation to AnsiConsole; output is verified by Spectre integration, not by this project's unit tests.")]
internal sealed class SpectreConsoleOutput : IConsoleOutput
{
    public bool HasErrors { get; private set; }

    public void Heading(string text) =>
        AnsiConsole.MarkupLine($"[bold cyan]── {Markup.Escape(text)} ──[/]");

    public void Info(string text) =>
        AnsiConsole.MarkupLine(Markup.Escape(text));

    public void Error(string text)
    {
        AnsiConsole.MarkupLine($"[red]✗ {Markup.Escape(text)}[/]");
        this.HasErrors = true;
    }
}
