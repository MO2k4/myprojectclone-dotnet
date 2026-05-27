namespace Quality.Cli.Output;

using System.Globalization;
using System.Text;

internal sealed class RecordingConsoleOutput : IConsoleOutput
{
    private readonly StringBuilder buffer = new();

    public string Captured => this.buffer.ToString();

    public bool HasErrors { get; private set; }

    public void Heading(string text) =>
        this.buffer.AppendLine(CultureInfo.InvariantCulture, $"## {text}");

    public void Info(string text) => this.buffer.AppendLine(text);

    public void Error(string text)
    {
        this.buffer.AppendLine(CultureInfo.InvariantCulture, $"ERR {text}");
        this.HasErrors = true;
    }
}
